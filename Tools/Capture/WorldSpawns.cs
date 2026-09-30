// --------------------------------------------------------------------------------------------------------------------
// <copyright file="WorldSpawns.cs" company="OmniCell">
//   Copyright © 2026 OmniCell contributors.
// </copyright>
// <summary>
//   Turns the bot's record of where it saw things into mob spawns for the whole world.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace OmniCell.Tools.Capture
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Text;

    using OmniCell.Database.Entities;

    using Utility;

    /// <summary>
    /// Everything alive outside Arete, from what the bot has walked past.
    /// </summary>
    /// <remarks>
    /// <c>mobspawns</c> held one playfield - Arete, from captures. The
    /// AOBuddy10 bot has since walked 366 of them and written down what it
    /// saw, which is the only record there is: monster placement is the
    /// server's, not the client's, so no amount of reading the client data
    /// produces it. This turns that record into spawns.
    ///
    ///     WorldSpawns &lt;output.sql&gt; &lt;bot plugin directory&gt;
    ///         [--creatures &lt;tsv&gt;]... [--skip &lt;playfield&gt;]...
    ///
    /// **Where a spawn is** comes from two places, in that order of trust.
    ///
    /// The bot's per-playfield sighting log names every creature it watched
    /// by the identity the live server gave it, and that identity is what
    /// makes a count possible: one identity is one creature, however many
    /// times it was seen. How many of a name there are is the most that were
    /// ever in view at one moment, and where each one walks is the track it
    /// was watched walking.
    ///
    /// Where a playfield and creature have no such moment, its nav entries
    /// are used instead. That is weaker, and weaker in a particular way: the
    /// bot records every NPC it passes and no identity with it, so one
    /// creature walking its patrol leaves several entries and ten standing
    /// still leave the same. **Those entries say a creature is in a playfield;
    /// they do not say how many there are.** So they give one spawn, and the
    /// places become the route it walks - the scatter is its pathing, not a
    /// head count. Read as a count it made twelve of Levon Karubian, who is
    /// one man.
    ///
    /// **Pets are thrown out by name.** The bot is a Meta-Physicist and its
    /// pets follow it everywhere, so they smear across every place it has
    /// walked - "Salvinous" has 293 nav entries and "Rage Materialization"
    /// 282, more than any real creature. The names come from the captures,
    /// where a pet carries its owner in PetMaster, and not from guessing at
    /// which names sound like pets.
    ///
    /// **What a creature looks like** is joined by name from the tables
    /// <see cref="WorldMobs"/> and <see cref="MobExtract"/> build. The bot
    /// records no body at all, so a creature never seen in a capture or a
    /// mission gets no body, and the run counts them. They are written
    /// anyway: a spawn in the right place with the right name and level is
    /// worth having, and it gains a body the moment one is recorded.
    ///
    /// **What is not measured** is said here rather than hidden. Headings are
    /// not recorded, so everything faces the same way. Damage is not recorded
    /// outside Arete, so it follows the level the way a mission creature's
    /// does. Health is the creature's own where the captures caught it, and
    /// the measured ramp where they did not.
    /// </remarks>
    internal static class WorldSpawns
    {
        /// <summary>
        /// Where generated spawn ids start.
        /// </summary>
        /// <remarks>
        /// Arete's are the dynel ids the live server used, around seven
        /// million. These are allocated in a fixed order so that running the
        /// tool again gives the same row the same id.
        /// </remarks>
        private const int FirstId = 200000000;

        /// <summary>
        /// One past the last id this tool will use, so a re-run can clear
        /// exactly what it wrote and nothing else.
        /// </summary>
        private const int LastId = 300000000;

        /// <summary>
        /// Two waypoints are a character walking on the spot, so a path needs
        /// more, and they have to be far enough apart to be a path at all.
        /// </summary>
        private const double WaypointSpacing = 6.0;

        private const int MostWaypoints = 16;

        /// <summary>
        /// How far from a spawn a recorded walk can start and still be that
        /// creature's round.
        /// </summary>
        private const double RouteRadius = 60.0;

        /// <summary>
        /// How many waypoints were packed and read back unchanged.
        /// </summary>
        private static int RoundTripped;

        private const int LowestMeasuredLevel = 19;

        private const int HighestMeasuredLevel = 44;

        /// <summary>
        /// What a creature is, joined by name.
        /// </summary>
        private sealed class Creature
        {
            public string Name;

            public int Monster;

            public int HeadMesh;

            public int Scale;

            public int Visual;

            public int RunSpeed;

            public double HealthScale;

            public bool Pet;

            /// <summary>
            /// A person, not scenery.
            /// </summary>
            /// <remarks>
            /// From the captures, where a player wears MonsterData zero. The
            /// bot's own record cannot tell one from a creature - it writes a
            /// name, a level and a place, and a person standing in a city
            /// looks exactly like an NPC standing in a city - so this list is
            /// the only thing that can, and it is only as complete as the
            /// captures are.
            /// </remarks>
            public bool Player;

            /// <summary>
            /// Level and maximum health, as caught on the wire.
            /// </summary>
            public readonly List<KeyValuePair<int, int>> Health = new List<KeyValuePair<int, int>>();
        }

        /// <summary>
        /// One place one creature stands.
        /// </summary>
        private sealed class Spawn
        {
            public int Playfield;

            public string Name;

            public double X;

            public double Y;

            public double Z;

            public int Level;

            public int Side = -1;

            public bool Watched;

            public List<double[]> Path;

            /// <summary>
            /// The body this one was wearing, when the bot wrote it down.
            /// </summary>
            /// <remarks>
            /// The bot records MonsterData per sighting now, so a spawn no
            /// longer has to borrow a body from another creature of the same
            /// name. Zero means it was seen before that landed.
            /// </remarks>
            public int Monster;

            public int Mesh;

            public int Scale;

            /// <summary>
            /// The way it faces, in degrees from +Z toward +X, or NaN.
            /// </summary>
            public double Heading = double.NaN;
        }

        private static int Main(string[] args)
        {
            if (args.Length < 2)
            {
                Console.Error.WriteLine(
                    "usage: WorldSpawns <output.sql> <bot plugin directory> [--creatures <tsv>]... [--skip <playfield>]...");
                Console.Error.WriteLine();
                Console.Error.WriteLine("  the bot plugin directory is the one holding mobs\\ and nav\\");
                Console.Error.WriteLine("  --creatures takes WorldMobs.tsv and MissionMobs.tsv, in any order");
                Console.Error.WriteLine("  --skip leaves a playfield alone; Arete (6553) has its own patch");
                return 1;
            }

            string output = args[0];
            string bot = args[1];
            var tables = new List<string>();
            var skip = new HashSet<int>();
            for (int i = 2; i < args.Length; i++)
            {
                if (args[i] == "--creatures" && i + 1 < args.Length)
                {
                    tables.Add(args[++i]);
                }
                else if (args[i] == "--skip" && i + 1 < args.Length)
                {
                    skip.Add(int.Parse(args[++i], CultureInfo.InvariantCulture));
                }
                else
                {
                    Console.Error.WriteLine("unknown argument: " + args[i]);
                    return 1;
                }
            }

            if (!Directory.Exists(Path.Combine(bot, "mobs")) && !Directory.Exists(Path.Combine(bot, "nav")))
            {
                Console.Error.WriteLine("no mobs\\ or nav\\ under " + bot);
                return 1;
            }

            Dictionary<string, Creature> creatures = ReadCreatures(tables);
            Console.WriteLine(
                "{0} creatures known, {1} of them with a body, {2} of them somebody's pet",
                creatures.Count,
                creatures.Values.Count(c => c.Monster != 0),
                creatures.Values.Count(c => c.Pet));

            var routes = new List<Spawn>();
            List<Spawn> watched = ReadSightings(Path.Combine(bot, "mobs"), creatures, skip, routes);
            Console.WriteLine(
                "{0} spawn points watched appearing, and {1} recorded walks to draw routes from",
                watched.Count,
                routes.Count);

            List<Spawn> guessed = ReadNav(Path.Combine(bot, "nav"), creatures, skip, watched);
            Console.WriteLine("{0} more from the nav list, where nothing was watched appearing", guessed.Count);

            List<Spawn> all = watched.Concat(guessed)
                .OrderBy(s => s.Playfield)
                .ThenBy(s => s.Name, StringComparer.Ordinal)
                .ThenBy(s => s.X)
                .ThenBy(s => s.Z)
                .ToList();

            if (all.Count == 0)
            {
                Console.Error.WriteLine("nothing to write");
                return 1;
            }

            Console.WriteLine("{0} spawns given a route from a walk nearby", Route(all, routes));

            Write(output, all, creatures);

            // A body the bot logged for this very spawn, or failing that one
            // borrowed from another creature of the same name.
            var logged = new Dictionary<string, Spawn>(StringComparer.OrdinalIgnoreCase);
            foreach (Spawn s in all.Where(s => s.Monster > 0))
            {
                if (!logged.ContainsKey(s.Name)) logged[s.Name] = s;
            }

            int borrowed = 0;
            foreach (Spawn s in all.Where(s => s.Monster == 0))
            {
                Spawn like;
                if (!logged.TryGetValue(s.Name, out like)) continue;
                s.Monster = like.Monster;
                s.Mesh = like.Mesh;
                s.Scale = like.Scale;
                borrowed++;
            }

            Console.WriteLine(
                "{0} spawns wear a body the bot logged, {1} more borrowed one from the same name",
                all.Count(s => s.Monster > 0) - borrowed,
                borrowed);

            int bodied = all.Count(s => s.Monster > 0 || Known(creatures, s.Name).Monster != 0);
            int pathed = all.Count(s => s.Path != null);
            Console.WriteLine();
            Console.WriteLine(
                "{0} spawns over {1} playfields: {2} with a body ({3:0}%), {4} with a path, {5} watched appearing",
                all.Count,
                all.Select(s => s.Playfield).Distinct().Count(),
                bodied,
                100.0 * bodied / all.Count,
                pathed,
                all.Count(s => s.Watched));
            Console.WriteLine(
                "  health: {0} measured on this creature, {1} from the ramp",
                all.Count(s => Known(creatures, s.Name).Health.Count > 0),
                all.Count(s => Known(creatures, s.Name).Health.Count == 0));

            Console.WriteLine(
                "  {0} waypoints packed and read back identical with the server's own codec",
                RoundTripped);
            Console.WriteLine("  {0} spawns face a measured direction, the rest face north", Headed);

            Console.WriteLine();
            Console.WriteLine("=== the fullest playfields");
            foreach (var g in all.GroupBy(s => s.Playfield).OrderByDescending(g => g.Count()).Take(12))
            {
                Console.WriteLine(
                    "  {0,-8} {1,4} spawns, {2,3} kinds, {3,4} with a body, {4,3} with a path",
                    g.Key,
                    g.Count(),
                    g.Select(s => s.Name).Distinct().Count(),
                    g.Count(s => Known(creatures, s.Name).Monster != 0),
                    g.Count(s => s.Path != null));
            }

            Console.WriteLine();
            Console.WriteLine("written to " + Path.GetFullPath(output));
            return 0;
        }

        private static Creature Known(Dictionary<string, Creature> creatures, string name)
        {
            Creature creature;
            return creatures.TryGetValue(name, out creature) ? creature : new Creature { Name = name };
        }

        #region what a creature is

        /// <summary>
        /// Joins the creature tables the two harvesters write.
        /// </summary>
        /// <remarks>
        /// They have different columns, so they are told apart by their
        /// header rather than by which file was named: WorldMobs leads with a
        /// name and MobExtract with a pool.
        /// </remarks>
        private static Dictionary<string, Creature> ReadCreatures(IEnumerable<string> paths)
        {
            var creatures = new Dictionary<string, Creature>(StringComparer.OrdinalIgnoreCase);
            foreach (string path in paths)
            {
                if (!File.Exists(path))
                {
                    Console.Error.WriteLine("missing, skipped: " + path);
                    continue;
                }

                foreach (string line in File.ReadLines(path))
                {
                    if (line.Length == 0 || line[0] == '#')
                    {
                        continue;
                    }

                    string[] parts = line.Split('\t');

                    // WorldMobs: name monster headmesh scale visual runspeed seen min max healthScale health pet pfs
                    if (parts.Length >= 13 && !int.TryParse(parts[0], out _))
                    {
                        Creature creature = Slot(creatures, parts[0]);
                        creature.Monster = Int(parts[1]);
                        creature.HeadMesh = Int(parts[2]);
                        creature.Scale = Int(parts[3]);
                        creature.Visual = Int(parts[4]);
                        creature.RunSpeed = Int(parts[5]);
                        if (creature.HealthScale <= 0)
                        {
                            creature.HealthScale = Double(parts[9]);
                        }

                        foreach (string point in parts[10].Split(' '))
                        {
                            int colon = point.IndexOf(':');
                            if (colon > 0)
                            {
                                creature.Health.Add(new KeyValuePair<int, int>(
                                    Int(point.Substring(0, colon)), Int(point.Substring(colon + 1))));
                            }
                        }

                        creature.Pet |= parts[11] == "1";
                        creature.Player |= parts.Length > 13 && parts[13] == "1";
                        continue;
                    }

                    // MobExtract: pool monster name seen minOffset maxOffset healthScale pet
                    if (parts.Length >= 7 && int.TryParse(parts[0], out _))
                    {
                        Creature creature = Slot(creatures, parts[2]);
                        if (creature.Monster == 0)
                        {
                            creature.Monster = Int(parts[1]);
                        }

                        if (creature.HealthScale <= 0)
                        {
                            creature.HealthScale = Double(parts[6]);
                        }

                        creature.Pet |= parts.Length > 7 && parts[7] == "1";
                    }
                }
            }

            return creatures;
        }

        private static Creature Slot(Dictionary<string, Creature> creatures, string name)
        {
            Creature creature;
            if (!creatures.TryGetValue(name, out creature))
            {
                creature = new Creature { Name = name };
                creatures.Add(name, creature);
            }

            return creature;
        }

        #endregion

        #region where things stand

        /// <summary>
        /// How many of each creature there are, and where each one walks.
        /// </summary>
        /// <remarks>
        /// One file per playfield, one json object per line, written as the
        /// bot loses sight of a creature. The field that matters is
        /// <c>id</c> - the identity the live server gave that creature - and
        /// it is what makes a count possible at all. Without it a name seen
        /// in twelve places is either one creature on its round or twelve
        /// standing still, and no amount of measuring distances tells them
        /// apart.
        ///
        /// **What was here before counted camps, and camps are not
        /// creatures.** It clustered <c>popIn</c> sightings at fifteen
        /// metres, on the belief that popIn means the server created
        /// something. It does not: it means the creature came into the bot's
        /// awareness, which happens every time the bot walks back into range.
        /// Levon Karubian popped in 140 times in Borealis. He is one man, and
        /// he was in the database twelve times.
        ///
        /// So the count is the most identities of that name the bot ever saw
        /// **at one moment** - the sweeps share a timestamp, so one sweep is
        /// one look at the playfield, and if five Young Scab Hyenas were
        /// visible together then there are at least five. That is a floor and
        /// it is honest as one: the bot sees what is near it, so a creature
        /// on the far side of a playfield is not counted. Forty distinct
        /// hyenas were seen in pf 795 over days of walking, five together;
        /// forty is how many times that camp respawned, not how many stand
        /// there.
        ///
        /// Each one gets a <c>track</c> - where that identity was actually
        /// watched moving - as its route, longest first, so the creature that
        /// walks furthest is the one whose round is best known.
        /// </remarks>
        private static List<Spawn> ReadSightings(
            string directory,
            Dictionary<string, Creature> creatures,
            HashSet<int> skip,
            List<Spawn> routes)
        {
            var spawns = new List<Spawn>();
            if (!Directory.Exists(directory))
            {
                return spawns;
            }

            foreach (string path in Directory.GetFiles(directory, "*.jsonl").OrderBy(p => p))
            {
                int playfield;
                if (!int.TryParse(Path.GetFileNameWithoutExtension(path), out playfield) || skip.Contains(playfield))
                {
                    continue;
                }

                // One sighting per identity, best of what was seen of it, and
                // how many of that name were ever in view together.
                var each = new Dictionary<string, Spawn>(StringComparer.Ordinal);
                var together = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                var sweep = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);

                foreach (string line in File.ReadLines(path))
                {
                    if (line.Length < 2)
                    {
                        continue;
                    }

                    string name = Json.String(line, "name");
                    if (string.IsNullOrEmpty(name) || Skip(Known(creatures, name)))
                    {
                        continue;
                    }

                    double[] first = Json.Numbers(line, "first");
                    if (first == null || first.Length < 3)
                    {
                        continue;
                    }

                    List<double[]> walked = Path_(line);

                    // Every walk is worth keeping, whoever it belonged to: a
                    // creature standing in the nav list has a route if one of
                    // these was it.
                    if (walked != null)
                    {
                        routes.Add(new Spawn
                                   {
                                       Playfield = playfield,
                                       Name = name,
                                       X = first[0],
                                       Y = first[1],
                                       Z = first[2],
                                       Path = walked,
                                   });
                    }

                    string who = Json.String(line, "id");
                    if (string.IsNullOrEmpty(who))
                    {
                        continue;
                    }

                    // How many stood there at once. The sweeps share a
                    // timestamp, so a timestamp is one look at the playfield.
                    string when = Json.String(line, "t");
                    if (!string.IsNullOrEmpty(when))
                    {
                        string moment = name.ToLowerInvariant() + "@" + when;
                        HashSet<string> seen;
                        if (!sweep.TryGetValue(moment, out seen))
                        {
                            seen = new HashSet<string>(StringComparer.Ordinal);
                            sweep[moment] = seen;
                        }

                        seen.Add(who);
                        int most;
                        if (!together.TryGetValue(name, out most) || seen.Count > most)
                        {
                            together[name] = seen.Count;
                        }
                    }

                    var sighting = new Spawn
                                   {
                                       Playfield = playfield,
                                       Name = name,
                                       X = first[0],
                                       Y = first[1],
                                       Z = first[2],
                                       Level = (int)Json.Number(line, "lvl"),
                                       Side = (int)Json.Number(line, "side"),
                                       Watched = true,
                                       Path = walked,
                                       Monster = (int)Json.Number(line, "monsterData"),
                                       Mesh = (int)Json.Number(line, "headMesh"),
                                       Scale = (int)Json.Number(line, "monsterScale"),
                                       Heading = Facing(line),
                                   };

                    Spawn known;
                    if (!each.TryGetValue(who, out known))
                    {
                        each[who] = sighting;
                        continue;
                    }

                    // The same creature, watched again. Keep the longest walk
                    // and the most that was ever learned about it.
                    if (sighting.Level > known.Level)
                    {
                        known.Level = sighting.Level;
                    }

                    if (known.Path == null
                        || (sighting.Path != null && sighting.Path.Count > known.Path.Count))
                    {
                        known.Path = sighting.Path;
                    }

                    if (known.Monster == 0 && sighting.Monster > 0)
                    {
                        known.Monster = sighting.Monster;
                        known.Mesh = sighting.Mesh;
                        known.Scale = sighting.Scale;
                    }

                    if (double.IsNaN(known.Heading))
                    {
                        known.Heading = sighting.Heading;
                    }
                }

                // As many of each name as were ever in view together, and the
                // ones with the most walking behind them. An identity is one
                // creature's life, and a camp that respawned forty times over
                // a week is not forty creatures standing in it.
                foreach (IGrouping<string, Spawn> kind in
                    each.Values.Where(s => s.Level > 0)
                        .GroupBy(s => s.Name, StringComparer.OrdinalIgnoreCase))
                {
                    int most;
                    if (!together.TryGetValue(kind.Key, out most) || most < 1)
                    {
                        most = 1;
                    }

                    spawns.AddRange(
                        kind.OrderByDescending(s => s.Path == null ? 0 : s.Path.Count).Take(most));
                }
            }

            return spawns;
        }

        /// <summary>
        /// The nav list, for creatures and playfields nothing was watched in.
        /// </summary>
        private static List<Spawn> ReadNav(
            string directory,
            Dictionary<string, Creature> creatures,
            HashSet<int> skip,
            List<Spawn> already)
        {
            var spawns = new List<Spawn>();
            if (!Directory.Exists(directory))
            {
                return spawns;
            }

            var covered = new HashSet<string>(
                already.Select(s => s.Playfield + "|" + s.Name.ToLowerInvariant()), StringComparer.Ordinal);

            foreach (string path in Directory.GetFiles(directory, "*.json").OrderBy(p => p))
            {
                int playfield;
                if (!int.TryParse(Path.GetFileNameWithoutExtension(path), out playfield) || skip.Contains(playfield))
                {
                    continue;
                }

                string text;
                try
                {
                    text = File.ReadAllText(path);
                }
                catch (IOException)
                {
                    continue;
                }

                var here = new List<Spawn>();
                foreach (string entry in Json.Objects(text, "Mobs"))
                {
                    string name = Json.String(entry, "Name");
                    if (string.IsNullOrEmpty(name) || Skip(Known(creatures, name)))
                    {
                        continue;
                    }

                    if (covered.Contains(playfield + "|" + name.ToLowerInvariant()))
                    {
                        continue;
                    }

                    int level = (int)Json.Number(entry, "Level");
                    if (level <= 0)
                    {
                        continue;
                    }

                    here.Add(
                        new Spawn
                            {
                                Playfield = playfield,
                                Name = name,
                                X = Json.Number(entry, "X"),
                                Y = Json.Number(entry, "Y"),
                                Z = Json.Number(entry, "Z"),
                                Level = level,
                                Monster = (int)Json.Number(entry, "MonsterData"),
                                Mesh = (int)Json.Number(entry, "Mesh"),
                            });
                }

                spawns.AddRange(One(here));
            }

            return spawns;
        }

        /// <summary>
        /// Whether this is something the world should stand, rather than
        /// somebody who was passing.
        /// </summary>
        /// <remarks>
        /// A player is not scenery and neither is a player's pet. Both were
        /// written into the world spawn table because the bot records a name
        /// and a place and nothing that says which is which, and the result
        /// was Healsalot standing in Borealis as an NPC, with pets that pop
        /// in and walk a path looking exactly like a spawn doing the same.
        /// </remarks>
        private static bool Skip(Creature creature)
        {
            return creature.Pet || creature.Player;
        }

        /// <summary>
        /// One spawn per creature per playfield, walking the places it was
        /// seen.
        /// </summary>
        /// <remarks>
        /// **A sighting says a creature was there. It does not say how many
        /// there were.** The nav list records a name, a level and a place and
        /// no identity at all, so one creature walking its round and ten
        /// standing still leave the same trail, and nothing in the file tells
        /// them apart. Collapsing at a radius guessed at how far a creature
        /// wanders is guessing, and it guessed badly: Levon Karubian is one
        /// named character and became twelve people in Borealis, the ICC
        /// Peacekeeper twenty-four, and the bot logged Young Scab Hyena 201
        /// times in one playfield.
        ///
        /// **The scattered sightings are the pathing, so they are kept as
        /// pathing.** One creature seen in eight places is not eight
        /// creatures; it is one that walks through eight places, and those
        /// eight places are the most that is known about where it goes. So a
        /// name gives one spawn and the sightings become its route, and
        /// nothing that was measured is thrown away - it only stops being
        /// read as a head count.
        ///
        /// It stands at the sighting nearest the middle of them, which for
        /// something on patrol is a place along its round rather than
        /// whichever end happened to be seen first, and it is a place the
        /// creature was actually seen rather than an average of places.
        ///
        /// Two things this cannot do. The order is the order the bot wrote
        /// them down, which is the order it passed them and not necessarily
        /// the order the creature walks them - a real walk, where one was
        /// watched, is better and <see cref="Route"/> still prefers it. And
        /// counting properly needs the identity the server puts on each
        /// creature, which is on the wire and is not written down; until the
        /// bot records it, there is no count to be had.
        /// </remarks>
        private static List<Spawn> One(List<Spawn> sightings)
        {
            var kept = new List<Spawn>();
            foreach (IGrouping<string, Spawn> creature in
                sightings.GroupBy(s => s.Name.ToLowerInvariant()))
            {
                List<Spawn> seen = creature.ToList();
                double x = seen.Average(s => s.X);
                double y = seen.Average(s => s.Y);
                double z = seen.Average(s => s.Z);

                Spawn middle = seen.OrderBy(
                    s => ((s.X - x) * (s.X - x)) + ((s.Y - y) * (s.Y - y)) + ((s.Z - z) * (s.Z - z))).First();

                // A camp holds a range and the spawn should be able to cover
                // it, the same as a watched one.
                middle.Level = seen.Max(s => s.Level);

                // Where it was seen is where it goes. Kept in the order they
                // were written down, starting from the one it stands on, so
                // the round begins where the creature does.
                if (seen.Count > 1)
                {
                    var route = new List<double[]> { new[] { middle.X, middle.Y, middle.Z } };
                    route.AddRange(
                        seen.Where(s => !ReferenceEquals(s, middle))
                            .Select(s => new[] { s.X, s.Y, s.Z }));
                    middle.Path = route;
                }

                kept.Add(middle);
            }

            return kept;
        }

        /// <summary>
        /// Gives a spawn with no route one that was walked from near it.
        /// </summary>
        /// <remarks>
        /// A walk belongs to a spawn when it is the same creature in the same
        /// playfield and it started within <see cref="RouteRadius"/> - which
        /// is wider than a spawn point, because a creature on patrol is
        /// somewhere along it when the bot first sees it. The longest walk
        /// wins, being the one that shows most of the round.
        /// </remarks>
        private static int Route(List<Spawn> spawns, List<Spawn> routes)
        {
            var byPlayfield = routes.GroupBy(r => r.Playfield)
                .ToDictionary(g => g.Key, g => g.ToList());

            int given = 0;
            foreach (Spawn spawn in spawns)
            {
                // A walk the bot actually followed beats a line drawn through
                // the places a creature was noticed, so only a route that was
                // itself walked is left alone here.
                if (spawn.Path != null && spawn.Watched)
                {
                    continue;
                }

                List<Spawn> here;
                if (!byPlayfield.TryGetValue(spawn.Playfield, out here))
                {
                    continue;
                }

                Spawn best = null;
                foreach (Spawn route in here)
                {
                    if (!string.Equals(route.Name, spawn.Name, StringComparison.OrdinalIgnoreCase)
                        || Apart(route, spawn) > RouteRadius)
                    {
                        continue;
                    }

                    if (best == null || route.Path.Count > best.Path.Count)
                    {
                        best = route;
                    }
                }

                if (best == null)
                {
                    continue;
                }

                spawn.Path = best.Path;
                given++;
            }

            return given;
        }

        /// <summary>
        /// The way a creature was facing when the bot first saw it.
        /// </summary>
        /// <remarks>
        /// The bot writes every track point as [secs, x, y, z, heading], the
        /// heading in degrees from +Z toward +X - atan2 of the forward
        /// vector's x over its z. The first point is the one that goes with
        /// the first position, which is where the spawn is put.
        /// </remarks>
        private static double Facing(string line)
        {
            double[][] track = Json.Rows(line, "track");
            if (track == null || track.Length == 0 || track[0].Length < 5)
            {
                return double.NaN;
            }

            return track[0][4];
        }

        /// <summary>
        /// A heading in degrees as the quaternion the spawn table holds.
        /// </summary>
        /// <remarks>
        /// Yaw only, which is what every captured spawn in Arete carries: its
        /// heading reads (0, y, 0, w) and nothing else. For a yaw of theta the
        /// forward vector is (sin theta, 0, cos theta), which is the atan2 the
        /// bot took, so the way back is halving the angle.
        /// </remarks>
        private static string Quaternion(double degrees)
        {
            if (double.IsNaN(degrees))
            {
                return "0,0,0,1";
            }

            double half = degrees * Math.PI / 360.0;
            double y = Math.Sin(half);
            double w = Math.Cos(half);

            // Prove the round trip rather than trust it: the forward vector of
            // this quaternion has to point back at the angle we were given.
            double back = Math.Atan2(2.0 * y * w, 1.0 - (2.0 * y * y)) * 180.0 / Math.PI;
            double want = degrees % 360.0;
            if (want > 180.0) want -= 360.0;
            if (want < -180.0) want += 360.0;
            if (Math.Abs(back - want) > 0.01)
            {
                throw new InvalidOperationException(
                    "heading " + degrees + " came back as " + back);
            }

            Headed++;
            return string.Format(
                CultureInfo.InvariantCulture, "0,{0:0.#####},0,{1:0.#####}", y, w);
        }

        /// <summary>How many spawns were given a measured facing.</summary>
        private static int Headed;

        private static double Apart(Spawn a, Spawn b)
        {
            double dx = a.X - b.X;
            double dz = a.Z - b.Z;
            return Math.Sqrt((dx * dx) + (dz * dz));
        }

        /// <summary>
        /// The route a creature was watched walking, thinned to the corners.
        /// </summary>
        /// <remarks>
        /// The log holds a point every second or so, which is far more than a
        /// route needs and more than the walk controller will follow. Points
        /// closer together than <see cref="WaypointSpacing"/> are dropped, and
        /// anything left that is only a step or two is not a route at all.
        /// </remarks>
        private static List<double[]> Path_(string line)
        {
            double[][] track = Json.Rows(line, "track");
            if (track == null || track.Length < 3)
            {
                return null;
            }

            var kept = new List<double[]>();
            foreach (double[] point in track)
            {
                if (point.Length < 4)
                {
                    continue;
                }

                double[] here = { point[1], point[2], point[3] };
                if (kept.Count > 0)
                {
                    double dx = kept[kept.Count - 1][0] - here[0];
                    double dz = kept[kept.Count - 1][2] - here[2];
                    if (Math.Sqrt((dx * dx) + (dz * dz)) < WaypointSpacing)
                    {
                        continue;
                    }
                }

                kept.Add(here);
                if (kept.Count >= MostWaypoints)
                {
                    break;
                }
            }

            return kept.Count >= 3 ? kept : null;
        }

        #endregion

        #region writing it out

        private static void Write(string output, List<Spawn> spawns, Dictionary<string, Creature> creatures)
        {
            var sql = new List<string>
                      {
                          "-- Mob spawns for the world outside Arete, written by Tools/Capture/WorldSpawns.",
                          "--",
                          "-- Where each one stands is where the AOBuddy10 bot saw it. A spawn marked",
                          "-- watched is one the bot saw the server create - it was not there, and then it",
                          "-- was - which is the strongest evidence a spawn point has. The rest are from",
                          "-- the bot's nav list, which records every NPC it passes and dedupes only at",
                          "-- twenty metres, so a creature walking its patrol leaves several entries; they",
                          "-- are collapsed again here and are weaker for it.",
                          "--",
                          "-- Pets are left out by name, from the captures, where a pet carries its owner.",
                          "-- The bot is a Meta-Physicist and its own follow it everywhere, so without that",
                          "-- they outnumber every real creature.",
                          "--",
                          "-- Bodies are joined by name from what the captures and the mission recordings",
                          "-- caught. A creature neither has seen gets no body, and is still written: the",
                          "-- place, the name and the level are measured, and the body arrives when one is",
                          "-- recorded. Headings are not recorded anywhere, so everything faces the same",
                          "-- way. Damage is not recorded outside Arete and follows the level.",
                          string.Empty,
                      };

            // Only ours. Clearing by playfield would take an NPC a GM placed
            // with /npc down with it, and those are not written anywhere else.
            string mine = "Id >= " + FirstId.ToString(CultureInfo.InvariantCulture) + " AND Id < "
                          + LastId.ToString(CultureInfo.InvariantCulture);
            sql.Add("-- Only the rows this tool owns, so nothing placed by hand is lost.");
            sql.Add("DELETE FROM mobspawnsmeshs WHERE " + mine + ";");
            sql.Add("DELETE FROM mobspawnsweapons WHERE SpawnId >= "
                    + FirstId.ToString(CultureInfo.InvariantCulture) + " AND SpawnId < "
                    + LastId.ToString(CultureInfo.InvariantCulture) + ";");
            sql.Add("DELETE FROM mobspawns_stats WHERE " + mine + ";");
            sql.Add("DELETE FROM mobspawns WHERE " + mine + ";");
            sql.Add(string.Empty);

            int id = FirstId;
            var rows = new List<string>();
            var stats = new List<string>();
            foreach (Spawn spawn in spawns)
            {
                Creature creature = Known(creatures, spawn.Name);
                int health = Health(creature, spawn.Level);
                int side = spawn.Side >= 0 ? spawn.Side : 3;

                rows.Add(string.Format(
                    CultureInfo.InvariantCulture,
                    "({0},{1},{2:0.###},{3:0.###},{4:0.###},{5},'{6}',0,0,0,0,0,{7})",
                    id,
                    spawn.Playfield,
                    spawn.X,
                    spawn.Y,
                    spawn.Z,
                    Quaternion(spawn.Heading),
                    spawn.Name.Replace("'", "''"),
                    Waypoints(id, spawn)));

                Add(stats, id, spawn.Playfield, 0, 277615105);
                Add(stats, id, spawn.Playfield, 1, health);
                Add(stats, id, spawn.Playfield, 4, 1);
                Add(stats, id, spawn.Playfield, 27, health);
                Add(stats, id, spawn.Playfield, 33, side);
                Add(stats, id, spawn.Playfield, 47, 1);
                Add(stats, id, spawn.Playfield, 54, spawn.Level);
                Add(stats, id, spawn.Playfield, 59, 2);
                Add(
                    stats,
                    id,
                    spawn.Playfield,
                    64,
                    spawn.Mesh > 0 ? spawn.Mesh : creature.HeadMesh > 0 ? creature.HeadMesh : 40137);
                Add(stats, id, spawn.Playfield, 89, 1);
                Add(stats, id, spawn.Playfield, 156, creature.RunSpeed > 0 ? creature.RunSpeed : 110);
                Add(stats, id, spawn.Playfield, 285, Math.Max(2, spawn.Level * 2));
                Add(stats, id, spawn.Playfield, 286, Math.Max(1, spawn.Level));
                // The bot's own record of what this one wore beats a body
                // borrowed from another creature that shares its name.
                Add(stats, id, spawn.Playfield, 359, spawn.Monster > 0 ? spawn.Monster : creature.Monster);
                Add(
                    stats,
                    id,
                    spawn.Playfield,
                    360,
                    spawn.Scale > 0 ? spawn.Scale : creature.Scale > 0 ? creature.Scale : 100);
                Add(stats, id, spawn.Playfield, 455, 113);
                Add(stats, id, spawn.Playfield, 673, creature.Visual > 0 ? creature.Visual : 31);
                id++;
            }

            Batch(
                sql,
                "INSERT INTO mobspawns (Id, Playfield, X, Y, Z, HeadingX, HeadingY, HeadingZ, HeadingW, Name,"
                + " Textures0, Textures1, Textures2, Textures3, Textures4, Waypoints) VALUES",
                rows);
            sql.Add(string.Empty);
            Batch(sql, "INSERT INTO mobspawns_stats (Id, Playfield, Stat, Value) VALUES", stats);

            File.WriteAllLines(output, sql);
        }

        private static void Add(List<string> stats, int id, int playfield, int stat, int value)
        {
            stats.Add(string.Format(
                CultureInfo.InvariantCulture, "({0},{1},{2},{3})", id, playfield, stat, value));
        }

        /// <summary>
        /// A thousand rows to an INSERT, so the file stays a file.
        /// </summary>
        private static void Batch(List<string> sql, string head, List<string> rows)
        {
            for (int i = 0; i < rows.Count; i += 1000)
            {
                sql.Add(head);
                int last = Math.Min(i + 1000, rows.Count);
                for (int j = i; j < last; j++)
                {
                    sql.Add("  " + rows[j] + (j == last - 1 ? ";" : ","));
                }
            }
        }

        /// <summary>
        /// The route, packed the way the server packs it.
        /// </summary>
        /// <remarks>
        /// <c>mobspawns.Waypoints</c> is what MessagePackZip makes of a list
        /// of <see cref="MobSpawnWaypoint"/>, which is what the server reads
        /// back, so it is written with the same code rather than by hand.
        /// </remarks>
        private static string Waypoints(int id, Spawn spawn)
        {
            if (spawn.Path == null || spawn.Path.Count < 3)
            {
                return "NULL";
            }

            var path = spawn.Path.Select(p => new MobSpawnWaypoint
                                              {
                                                  Identity = id,
                                                  Playfield = spawn.Playfield,
                                                  WalkMode = 0,
                                                  X = (float)p[0],
                                                  Y = (float)p[1],
                                                  Z = (float)p[2],
                                              }).ToList();

            byte[] packed = MessagePackZip.SerializeData(path);

            // The server reads this back with DeserializeData, so prove the
            // pair round-trips rather than trusting that it does: a blob that
            // does not is a mob standing still and no error anywhere.
            List<MobSpawnWaypoint> read = MessagePackZip.DeserializeData<MobSpawnWaypoint>(packed);
            if (read == null || read.Count != path.Count)
            {
                throw new InvalidOperationException(
                    "waypoints for " + spawn.Name + " in " + spawn.Playfield + " did not survive packing");
            }

            for (int i = 0; i < path.Count; i++)
            {
                if (read[i].Identity != path[i].Identity || read[i].Playfield != path[i].Playfield
                    || Math.Abs(read[i].X - path[i].X) > 0.001f || Math.Abs(read[i].Y - path[i].Y) > 0.001f
                    || Math.Abs(read[i].Z - path[i].Z) > 0.001f)
                {
                    throw new InvalidOperationException(
                        "waypoint " + i + " for " + spawn.Name + " in " + spawn.Playfield + " came back changed");
                }
            }

            RoundTripped += path.Count;
            return "0x" + Convert.ToHexString(packed);
        }

        /// <summary>
        /// What this creature has at this level.
        /// </summary>
        /// <remarks>
        /// Measured health first: if a capture caught this creature at this
        /// level, that is the answer, and if it caught two levels the line
        /// between them is - health is linear in level within a band.
        ///
        /// Otherwise the ramp, which was measured over levels 19 to 44 and is
        /// carried beyond it here for want of anything better. A creature
        /// nobody has seen the health of is given the ramp unscaled.
        /// </remarks>
        private static int Health(Creature creature, int level)
        {
            List<KeyValuePair<int, int>> points =
                creature.Health.GroupBy(h => h.Key)
                    .Select(g => new KeyValuePair<int, int>(g.Key, g.Max(h => h.Value)))
                    .OrderBy(h => h.Key)
                    .ToList();

            KeyValuePair<int, int> exact = points.FirstOrDefault(p => p.Key == level);
            if (exact.Value > 0)
            {
                return exact.Value;
            }

            if (points.Count >= 2)
            {
                KeyValuePair<int, int> low = points[0];
                KeyValuePair<int, int> high = points[points.Count - 1];
                foreach (KeyValuePair<int, int> point in points)
                {
                    if (point.Key < level)
                    {
                        low = point;
                    }
                    else
                    {
                        high = point;
                        break;
                    }
                }

                if (high.Key != low.Key)
                {
                    double at = low.Value
                                + ((high.Value - low.Value) * (level - low.Key) / (double)(high.Key - low.Key));
                    return Math.Max(1, (int)Math.Round(at));
                }
            }

            if (points.Count == 1 && level >= LowestMeasuredLevel && level <= HighestMeasuredLevel
                && points[0].Key >= LowestMeasuredLevel && points[0].Key <= HighestMeasuredLevel)
            {
                return Math.Max(1, (int)Math.Round(points[0].Value * Ramp(level) / Ramp(points[0].Key)));
            }

            double scale = creature.HealthScale > 0 ? creature.HealthScale : 1.0;
            return Math.Max(1, (int)Math.Round(scale * Ramp(level), MidpointRounding.AwayFromZero));
        }

        private static double Ramp(int level)
        {
            level = Math.Max(1, level);
            return level <= 25 ? (33.0 * level) - 101.0 : ((185.0 / 3.0) * level) - (2452.0 / 3.0);
        }

        private static int Int(string s)
        {
            int v;
            return int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out v) ? v : 0;
        }

        private static double Double(string s)
        {
            double v;
            return double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out v) ? v : 0.0;
        }

        #endregion
    }

    /// <summary>
    /// Just enough json to read the bot's files.
    /// </summary>
    /// <remarks>
    /// The capture tools carry no json library and the shapes here are the
    /// bot's own, flat and known: a sighting is one object on one line, and a
    /// nav file is a list of them under a named key. Pulling the handful of
    /// fields out by name is smaller than taking a dependency, and it fails
    /// by returning nothing rather than by guessing.
    /// </remarks>
    internal static class Json
    {
        public static string String(string text, string key)
        {
            int at = Find(text, key);
            if (at < 0)
            {
                return null;
            }

            int open = text.IndexOf('"', at);
            if (open < 0)
            {
                return null;
            }

            var value = new StringBuilder();
            for (int i = open + 1; i < text.Length; i++)
            {
                if (text[i] == '\\' && i + 1 < text.Length)
                {
                    value.Append(text[++i]);
                    continue;
                }

                if (text[i] == '"')
                {
                    return value.ToString();
                }

                value.Append(text[i]);
            }

            return null;
        }

        public static double Number(string text, string key)
        {
            int at = Find(text, key);
            if (at < 0)
            {
                return 0;
            }

            int i = at;
            while (i < text.Length && (text[i] == ' ' || text[i] == ':'))
            {
                i++;
            }

            int start = i;
            while (i < text.Length
                   && (char.IsDigit(text[i]) || text[i] == '-' || text[i] == '+' || text[i] == '.'
                       || text[i] == 'e' || text[i] == 'E'))
            {
                i++;
            }

            double v;
            return double.TryParse(
                text.Substring(start, i - start), NumberStyles.Float, CultureInfo.InvariantCulture, out v)
                       ? v
                       : 0;
        }

        public static bool Flag(string text, string key)
        {
            int at = Find(text, key);
            return at >= 0 && text.IndexOf("true", at, Math.Min(8, text.Length - at), StringComparison.Ordinal) >= 0;
        }

        /// <summary>
        /// A flat array of numbers, as "first": [1, 2, 3].
        /// </summary>
        public static double[] Numbers(string text, string key)
        {
            int at = Find(text, key);
            if (at < 0)
            {
                return null;
            }

            int open = text.IndexOf('[', at);
            int close = open < 0 ? -1 : text.IndexOf(']', open);
            if (open < 0 || close < 0)
            {
                return null;
            }

            return text.Substring(open + 1, close - open - 1)
                .Split(',')
                .Select(p =>
                        {
                            double v;
                            double.TryParse(p, NumberStyles.Float, CultureInfo.InvariantCulture, out v);
                            return v;
                        })
                .ToArray();
        }

        /// <summary>
        /// An array of arrays of numbers, as "track": [[..],[..]].
        /// </summary>
        public static double[][] Rows(string text, string key)
        {
            int at = Find(text, key);
            if (at < 0)
            {
                return null;
            }

            int open = text.IndexOf('[', at);
            if (open < 0)
            {
                return null;
            }

            var rows = new List<double[]>();
            int depth = 0;
            int start = -1;
            for (int i = open; i < text.Length; i++)
            {
                if (text[i] == '[')
                {
                    depth++;
                    if (depth == 2)
                    {
                        start = i + 1;
                    }

                    continue;
                }

                if (text[i] != ']')
                {
                    continue;
                }

                if (depth == 2 && start >= 0)
                {
                    rows.Add(text.Substring(start, i - start)
                                 .Split(',')
                                 .Select(p =>
                                         {
                                             double v;
                                             double.TryParse(
                                                 p, NumberStyles.Float, CultureInfo.InvariantCulture, out v);
                                             return v;
                                         })
                                 .ToArray());
                }

                depth--;
                if (depth == 0)
                {
                    break;
                }
            }

            return rows.ToArray();
        }

        /// <summary>
        /// The objects in a named array, each returned whole.
        /// </summary>
        public static IEnumerable<string> Objects(string text, string key)
        {
            int at = Find(text, key);
            if (at < 0)
            {
                yield break;
            }

            int open = text.IndexOf('[', at);
            if (open < 0)
            {
                yield break;
            }

            int depth = 0;
            int start = -1;
            for (int i = open; i < text.Length; i++)
            {
                char c = text[i];
                if (c == '{')
                {
                    if (depth == 0)
                    {
                        start = i;
                    }

                    depth++;
                    continue;
                }

                if (c == '}')
                {
                    depth--;
                    if (depth == 0 && start >= 0)
                    {
                        yield return text.Substring(start, i - start + 1);
                    }

                    continue;
                }

                if (c == ']' && depth == 0)
                {
                    yield break;
                }
            }
        }

        private static int Find(string text, string key)
        {
            int at = text.IndexOf('"' + key + '"', StringComparison.Ordinal);
            return at < 0 ? -1 : at + key.Length + 2;
        }
    }
}
