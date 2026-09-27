#region License

// Copyright (c) 2026, OmniCell contributors
//
// This file is part of OmniCell and is distributed under the terms the project
// is licensed under. See the repository root for details.

#endregion

namespace ZoneEngine.Core.Missions
{
    #region Usings ...

    using System;
    using System.Collections.Generic;
    using System.Linq;

    using OmniCell.Core.Entities;
    using OmniCell.Core.Missions;
    using OmniCell.Core.NPCHandler;
    using OmniCell.Core.Vector;
    using OmniCell.Database.Entities;
    using OmniCell.Enums;
    using OmniCell.ObjectManager;

    using SmokeLounge.AOtomation.Messaging.GameData;

    using Utility;

    using ZoneEngine.Core.Controllers;

    using Playfield = OmniCell.Core.Playfields.Playfield;
    using Quaternion = OmniCell.Core.Vector.Quaternion;

    #endregion

    /// <summary>
    /// Puts a mission's monsters into its playfield.
    /// </summary>
    /// <remarks>
    /// The building is the client's to draw; everything standing in it is the
    /// server's to spawn, and a monster is an ordinary character so that
    /// everything the server already does to characters - fighting them,
    /// killing them, looting them - works without knowing a mission from a
    /// playfield.
    ///
    /// They are made from an in-memory spawn record rather than a row: a
    /// mission's monsters exist for as long as the mission does and writing
    /// them down would leave the table growing forever. Nothing here writes to
    /// the database, and an NPC controller does not save.
    /// </remarks>
    public static class MissionSpawner
    {
        /// <summary>
        /// The stats a mission monster needs to stand up and fight.
        /// </summary>
        /// <remarks>
        /// The values that do not come from the creature are the ones the
        /// captured monsters share: visual flags 31, monster scale about 100,
        /// and the flag word an NPC carries. A captured mission monster reads
        /// Level 32, Health 1156, MonsterData 26143, MonsterScale 102,
        /// HeadMesh 40137 and npcfamily 113 (20260926-135805 stream 10).
        /// </remarks>
        private static readonly Dictionary<int, int> Common = new Dictionary<int, int>
                                                              {
                                                                  { (int)StatIds.flags, 277615105 },
                                                                  { (int)StatIds.breed, 1 },
                                                                  { (int)StatIds.fatness, 1 },
                                                                  { (int)StatIds.sex, 2 },
                                                                  { (int)StatIds.race, 1 },
                                                                  { (int)StatIds.headmesh, 40137 },
                                                                  { (int)StatIds.runspeed, 110 },
                                                                  { (int)StatIds.monsterscale, 102 },
                                                                  { (int)StatIds.npcfamily, 113 },
                                                                  { (int)StatIds.visualflags, 31 }
                                                              };

        /// <summary>
        /// Which side a mission's monsters are on.
        /// </summary>
        /// <remarks>
        /// 3 in the captures, which is neutral - a mission monster attacks
        /// whoever walks in whatever they belong to.
        /// </remarks>
        private const int Side = 3;

        /// <summary>
        /// Fill a mission's playfield.
        /// </summary>
        /// <returns>How many monsters were put in it.</returns>
        public static int Fill(Playfield playfield, MissionOffer mission)
        {
            if (playfield == null || mission == null || mission.Built == null)
            {
                return 0;
            }

            int made = 0;
            foreach (MissionMonster monster in mission.Built.Monsters)
            {
                if (Spawn(playfield, monster) != null)
                {
                    made++;
                }
            }

            return made;
        }

        private static ICharacter Spawn(Playfield playfield, MissionMonster monster)
        {
            try
            {
                int instance = Pool.Instance.GetFreeInstance<Character>(
                    playfield.Identity.Instance, IdentityType.CanbeAffected);

                float y = MissionBuilding.HeightOf(monster.Floor);
                var spawn = new DBMobSpawn
                            {
                                Id = instance,
                                Name = monster.Name ?? "Monster",
                                Playfield = playfield.Identity.Instance,
                                X = monster.X,
                                Y = y,
                                Z = monster.Z,
                                HeadingX = 0,
                                HeadingY = 0,
                                HeadingZ = 0,
                                HeadingW = 1,
                                KnuBotScriptName = string.Empty
                            };

                var stats = new List<DBMobSpawnStat>();
                foreach (KeyValuePair<int, int> stat in Common)
                {
                    stats.Add(Stat(instance, playfield, stat.Key, stat.Value));
                }

                stats.Add(Stat(instance, playfield, (int)StatIds.level, monster.Level));
                stats.Add(Stat(instance, playfield, (int)StatIds.life, monster.Health));
                stats.Add(Stat(instance, playfield, (int)StatIds.health, monster.Health));
                stats.Add(Stat(instance, playfield, (int)StatIds.side, Side));
                stats.Add(Stat(instance, playfield, (int)StatIds.monsterdata, monster.Monster));

                // Damage is not measured. A monster that cannot hurt anybody is
                // no more honest than one that guesses, so it swings for about
                // what its level would suggest and this says so.
                stats.Add(Stat(instance, playfield, (int)StatIds.mindamage, Math.Max(1, monster.Level)));
                stats.Add(Stat(instance, playfield, (int)StatIds.maxdamage, Math.Max(2, monster.Level * 2)));

                ICharacter made = NonPlayerCharacterHandler.InstantiateMobSpawn(
                    spawn, stats.ToArray(), new NPCController(), playfield);
                if (made == null)
                {
                    return null;
                }

                // Where it belongs, and whether it wanders or picks fights.
                playfield.RememberSpawn(
                    made.Identity,
                    new Coordinate { x = monster.X, y = y, z = monster.Z },
                    new Quaternion(0, 0, 0, 1));
                NpcLife.Spawned(made, new Coordinate { x = monster.X, y = y, z = monster.Z });
                return made;
            }
            catch (Exception exception)
            {
                LogUtil.ErrorException(exception);
                return null;
            }
        }

        private static DBMobSpawnStat Stat(int instance, Playfield playfield, int stat, int value)
        {
            return new DBMobSpawnStat
                   {
                       Id = instance,
                       Playfield = playfield.Identity.Instance,
                       Stat = stat,
                       Value = value
                   };
        }
    }
}
