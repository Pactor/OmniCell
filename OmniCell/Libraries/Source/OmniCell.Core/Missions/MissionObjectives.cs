#region License

// Copyright (c) 2026, OmniCell contributors
//
// This file is part of OmniCell and is distributed under the terms the project
// is licensed under. See the repository root for details.

#endregion

namespace OmniCell.Core.Missions
{
    #region Usings ...

    using System;
    using System.Collections.Generic;
    using System.Linq;

    using OmniCell.Database.Dao;
    using OmniCell.Database.Entities;

    using SmokeLounge.AOtomation.Messaging.GameData;

    using Utility;

    #endregion

    /// <summary>
    /// What a mission sends the player after.
    /// </summary>
    /// <remarks>
    /// Three of the five types name something the repository already holds and
    /// one does not:
    ///
    ///   * **kill person** names a creature, and the creature table in
    ///     missionpools.ocp is 113 of them read off recorded missions, each
    ///     with the pool it appeared in. That is the real list.
    ///   * **find item** and **return item** name an item, and itemnames has
    ///     every item the client knows.
    ///   * **repair** names a part and a fixture, and the part is an item.
    ///   * **find person** names a human NPC, and nothing here has a list of
    ///     those. Four came out of the captures - Donte Clendennen, Mel
    ///     McKesson, Vernon Fahrni, Quinn Buhlig - and they are crossed to make
    ///     sixteen. That is a placeholder and is the one piece of content on
    ///     this path that is not read from data; retail's list is in the
    ///     client's own resource database and extracting it is the fix.
    /// </remarks>
    public static class MissionObjectives
    {
        #region Names

        /// <summary>
        /// The four names the captured find person offers used, split.
        /// </summary>
        private static readonly string[] Forenames = { "Donte", "Mel", "Vernon", "Quinn" };

        /// <summary>
        /// The four names the captured find person offers used, split.
        /// </summary>
        private static readonly string[] Surnames = { "Clendennen", "McKesson", "Fahrni", "Buhlig" };

        /// <summary>
        /// Every item the client has a name for, read once.
        /// </summary>
        private static List<DBItemName> items;

        private static readonly object ItemLock = new object();

        /// <summary>
        /// What this mission sends the player after.
        /// </summary>
        public static string Name(MissionOffer offer, Random random)
        {
            switch (offer.Type)
            {
                case MissionType.KillPerson:
                    return Creature(offer.Pool, random) ?? Person(random);

                case MissionType.FindPerson:
                    return Person(random);

                case MissionType.Repair:
                case MissionType.FindItem:
                case MissionType.ReturnItem:
                    return Item(random) ?? "component";

                default:
                    return "the objective";
            }
        }

        /// <summary>
        /// A creature this pool has been seen to spawn, weighted by how often.
        /// </summary>
        private static string Creature(int pool, Random random)
        {
            MissionPool data;
            if (!MissionPoolLoader.Pools.TryGetValue(pool, out data)
                || data.Creatures == null || data.Creatures.Count == 0)
            {
                // Four of the ten pools have nobody on record. Any creature
                // from a pool that does is closer than a made up name.
                List<MissionCreature> anywhere = MissionPoolLoader.Pools.Values
                    .SelectMany(p => p.Creatures)
                    .ToList();
                if (anywhere.Count == 0)
                {
                    return null;
                }

                return anywhere[random.Next(anywhere.Count)].Name;
            }

            int total = data.Creatures.Sum(c => Math.Max(1, c.Seen));
            int roll = random.Next(total);
            foreach (MissionCreature creature in data.Creatures)
            {
                roll -= Math.Max(1, creature.Seen);
                if (roll < 0)
                {
                    return creature.Name;
                }
            }

            return data.Creatures[data.Creatures.Count - 1].Name;
        }

        private static string Person(Random random)
        {
            return Forenames[random.Next(Forenames.Length)] + " "
                   + Surnames[random.Next(Surnames.Length)];
        }

        private static string Item(Random random)
        {
            DBItemName picked = Pick(random);
            return picked == null ? null : picked.Name;
        }

        /// <summary>
        /// An item the assignment can send somebody after, name and id both.
        /// </summary>
        /// <remarks>
        /// A repair mission needs the identity as well as the name, because
        /// the part it hands over has to be a real item the player can then
        /// use on the fixture.
        /// </remarks>
        public static DBItemName Pick(Random random)
        {
            List<DBItemName> all = Items();
            return all.Count == 0 ? null : all[random.Next(all.Count)];
        }

        /// <summary>
        /// The item names, loaded on first use and kept.
        /// </summary>
        /// <remarks>
        /// Names only - what a mission needs is something to call the thing in
        /// the assignment, and the object itself is not built until the
        /// building is. Nanos are left out: a mission does not send anyone
        /// after a nano crystal in any captured offer.
        /// </remarks>
        private static List<DBItemName> Items()
        {
            lock (ItemLock)
            {
                if (items != null)
                {
                    return items;
                }

                try
                {
                    items = ItemNamesDao.Instance.GetAll()
                        .Where(x => x != null
                                    && !string.IsNullOrEmpty(x.Name)
                                    && x.ItemType == "Item"
                                    && x.Icon != "0")
                        .ToList();
                }
                catch (Exception exception)
                {
                    LogUtil.ErrorException(exception);
                    items = new List<DBItemName>();
                }

                return items;
            }
        }

        #endregion
    }
}
