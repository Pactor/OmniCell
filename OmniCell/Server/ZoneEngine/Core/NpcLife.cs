namespace ZoneEngine.Core
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;

    using OmniCell.Core.Entities;
    using OmniCell.Core.Vector;
    using OmniCell.Database.Dao;
    using OmniCell.Database.Entities;
    using OmniCell.Enums;

    using SmokeLounge.AOtomation.Messaging.GameData;

    using Vector3 = OmniCell.Core.Vector.Vector3;

    using Utility;

    using ZoneEngine.Core.Controllers;

    /// <summary>
    /// Creatures that wander about and pick fights, the way the live server's do.
    /// </summary>
    /// <remarks>
    /// Measured from the fifteen 2026-09 retail recordings (npcbehaviour, npcwanderpoints):
    /// <list type="bullet">
    /// <item>A Garbage Flea walks a step of about 6 m every 3 to 5 seconds, all twenty seen; a Cleaning
    /// Robot wanders in 14 of 61 sightings; Waste Collectors, 32-V Dockers, Desert Reets and Dockworkers
    /// never moved in any recording, and stand still here too.</item>
    /// <item>Garbage Fleas started 12 fights against 1 they only answered, Rollerrats 7 against 2; Cleaning
    /// Robots never started one. An aggressive creature sends its attack first, then runs at the player
    /// and lands its first hit on arrival (Garbage Flea: attack, runs of 5 m, hit 2.7 s later).</item>
    /// </list>
    /// A wanderer only walks to places creatures of its name walked to in retail, near its spawn point,
    /// so it stays on ground the live server used. A spawn with its own captured route keeps patrolling
    /// that route as before.
    /// </remarks>
    public static class NpcLife
    {
        /// <summary>
        /// How close a creature gets before it swings. Retail's first hits landed at 3.4 to 4.9 m
        /// (Cultist, Workman Striker, Neural Burnout); 3.5 m keeps it inside all of them.
        /// </summary>
        public const double Reach = 3.5;

        /// <summary>
        /// How far from its spawn point a wanderer picks places to walk to. Retail's wanderers roamed a
        /// median of 14 to 25 m from where they were first seen and 32 to 38 m at the 90th percentile
        /// (Garbage Flea, Cleaning Robot).
        /// </summary>
        private const double WanderRadius = 30;

        /// <summary>
        /// A step goes to a place within this of where the creature stands when one exists, and otherwise
        /// to one of the three nearest, so it wanders rather than crossing its whole range at once
        /// (retail steps: a median of 6.3 m for a Garbage Flea, 1.2 m for a Cleaning Robot).
        /// </summary>
        private const double StepRadius = 8;

        /// <summary>
        /// OmniCell-defined: a creature drawn this far from its spawn point gives up the fight and walks
        /// back. No recording shows a creature being led away far enough to see where retail stops.
        /// </summary>
        private const double LeashRadius = 50;

        private static readonly TimeSpan AggroCheckEvery = TimeSpan.FromMilliseconds(500);

        private static readonly object Sync = new object();

        private static readonly Random Rng = new Random();

        private static readonly ConcurrentDictionary<Identity, Life> Lives = new ConcurrentDictionary<Identity, Life>();

        private static Dictionary<string, DBNpcBehaviour> behaviours;

        private static Dictionary<(int, string), List<Vector3>> wanderPoints;

        /// <summary>
        /// Reads npcbehaviour and npcwanderpoints. Safe to call again.
        /// </summary>
        public static int Load()
        {
            var loadedBehaviours = new Dictionary<string, DBNpcBehaviour>(StringComparer.Ordinal);
            var loadedPoints = new Dictionary<(int, string), List<Vector3>>();
            try
            {
                foreach (DBNpcBehaviour behaviour in NpcBehaviourDao.Instance.GetAll())
                {
                    loadedBehaviours[behaviour.NpcName] = behaviour;
                }

                foreach (DBNpcWanderPoint point in NpcWanderPointDao.Instance.GetAll())
                {
                    List<Vector3> list;
                    if (!loadedPoints.TryGetValue((point.Playfield, point.NpcName), out list))
                    {
                        loadedPoints[(point.Playfield, point.NpcName)] = list = new List<Vector3>();
                    }

                    list.Add(new Vector3(point.X, point.Y, point.Z));
                }
            }
            catch (Exception exception)
            {
                // Without the tables creatures stand still, as they did before.
                LogUtil.ErrorException(exception, "NPC behaviour could not be read");
            }

            lock (Sync)
            {
                behaviours = loadedBehaviours;
                wanderPoints = loadedPoints;
            }

            return loadedBehaviours.Count;
        }

        /// <summary>
        /// A spawn point made its character. Decides once whether this one wanders, and where it may go.
        /// </summary>
        public static void Spawned(ICharacter npc, Coordinate home)
        {
            if (npc == null || npc.Playfield == null)
            {
                return;
            }

            EnsureLoaded();
            DBNpcBehaviour behaviour;
            behaviours.TryGetValue(npc.Name ?? string.Empty, out behaviour);

            var life = new Life { Home = home.coordinate, Behaviour = behaviour };

            // The share of this kind that wanders, decided by the spawn's own id so it is the same spawn
            // every time the zone starts.
            List<Vector3> points;
            if (behaviour != null
                && npc.Waypoints.Count <= 2
                && Math.Abs(npc.Identity.Instance % 1000) < behaviour.WanderShare * 1000
                && wanderPoints.TryGetValue((npc.Playfield.Identity.Instance, npc.Name), out points))
            {
                life.Places = points.Where(p => p.Distance2D(life.Home) <= WanderRadius).ToList();
                life.Wanders = life.Places.Count >= 2;
            }

            life.NextWander = DateTime.UtcNow + TimeSpan.FromSeconds(Pause(behaviour));
            Lives[npc.Identity] = life;
        }

        /// <summary>
        /// One heartbeat of a spawned character that is not going anywhere: look for a fight, wander, or
        /// walk home.
        /// </summary>
        public static void Tick(ICharacter npc, IList<ICharacter> players)
        {
            Life life;
            var controller = npc.Controller as NPCController;
            if (controller == null || !Lives.TryGetValue(npc.Identity, out life))
            {
                return;
            }

            if (npc.Stats[StatIds.health].Value <= 0 || ZoneEngine.Core.Combat.Combat.IsFighting(npc))
            {
                return;
            }

            DateTime now = DateTime.UtcNow;
            if (life.Behaviour != null && life.Behaviour.Aggressive != 0 && now >= life.NextAggroCheck)
            {
                life.NextAggroCheck = now + AggroCheckEvery;
                ICharacter victim = Nearest(npc, players, life.Behaviour.AggroRadius);
                if (victim != null)
                {
                    if (controller.IsFollowing())
                    {
                        controller.Halt();
                    }

                    ZoneEngine.Core.Combat.Combat.Start(npc, victim.Identity);
                    return;
                }
            }

            if (controller.IsFollowing() || controller.InConversation)
            {
                return;
            }

            // Back from a fight: a patrol resumes, anybody else walks home first.
            if (npc.Waypoints.Count > 2)
            {
                controller.State = CharacterState.Patrolling;
                return;
            }

            Vector3 here = npc.Coordinates().coordinate;
            double fromHome = here.Distance2D(life.Home);
            if (fromHome > WanderRadius + 5)
            {
                controller.WalkTo(life.Home, false);
                return;
            }

            if (!life.Wanders || now < life.NextWander)
            {
                return;
            }

            life.NextWander = now + TimeSpan.FromSeconds(Pause(life.Behaviour));
            List<Vector3> near = life.Places.Where(p => p.Distance2D(here) > 1 && p.Distance2D(here) <= StepRadius).ToList();
            List<Vector3> choices = near.Count > 0
                                        ? near
                                        : life.Places.Where(p => p.Distance2D(here) > 1).OrderBy(p => p.Distance2D(here)).Take(3).ToList();
            if (choices.Count == 0)
            {
                return;
            }

            Vector3 destination;
            lock (Rng)
            {
                destination = choices[Rng.Next(choices.Count)];
            }

            controller.WalkTo(destination, life.Behaviour.WanderRuns != 0);
        }

        /// <summary>
        /// Whether a creature in a fight has been drawn too far from home, or its target has got away.
        /// </summary>
        public static bool GivesUp(ICharacter npc, ICharacter victim)
        {
            Life life;
            if (npc == null || victim == null || !Lives.TryGetValue(npc.Identity, out life))
            {
                return false;
            }

            return npc.Coordinates().coordinate.Distance2D(life.Home) > LeashRadius
                   || victim.Coordinates().coordinate.Distance2D(life.Home) > LeashRadius + 10;
        }

        /// <summary>Drops a character that has left for good.</summary>
        public static void Forget(Identity npc)
        {
            Life ignored;
            Lives.TryRemove(npc, out ignored);
        }

        private static ICharacter Nearest(ICharacter npc, IList<ICharacter> players, double radius)
        {
            Vector3 here = npc.Coordinates().coordinate;
            ICharacter best = null;
            double bestDistance = radius;
            foreach (ICharacter player in players)
            {
                if (player.Stats[StatIds.health].Value <= 0)
                {
                    continue;
                }

                Vector3 there = player.Coordinates().coordinate;
                if (Math.Abs(there.y - here.y) > 10)
                {
                    continue;
                }

                double distance = there.Distance2D(here);
                if (distance <= bestDistance)
                {
                    best = player;
                    bestDistance = distance;
                }
            }

            return best;
        }

        private static double Pause(DBNpcBehaviour behaviour)
        {
            double min = behaviour == null ? 3 : Math.Max(0.5, behaviour.WanderPauseMin);
            double max = behaviour == null ? 6 : Math.Max(min, behaviour.WanderPauseMax);
            lock (Rng)
            {
                return min + (Rng.NextDouble() * (max - min));
            }
        }

        private static void EnsureLoaded()
        {
            if (behaviours == null)
            {
                lock (Sync)
                {
                    if (behaviours == null)
                    {
                        Load();
                    }
                }
            }
        }

        private sealed class Life
        {
            public Vector3 Home;

            public DBNpcBehaviour Behaviour;

            public bool Wanders;

            public List<Vector3> Places = new List<Vector3>();

            public DateTime NextWander;

            public DateTime NextAggroCheck;
        }
    }
}
