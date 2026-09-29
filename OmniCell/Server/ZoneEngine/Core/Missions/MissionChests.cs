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

    using OmniCell.Core.Inventory;
    using OmniCell.Core.Items;
    using OmniCell.Core.Missions;
    using OmniCell.Core.Playfields;
    using OmniCell.Database.Dao;
    using OmniCell.Interfaces;

    using SmokeLounge.AOtomation.Messaging.GameData;

    using Utility;

    using ZoneEngine.Core.Loot;

    #endregion

    /// <summary>
    /// What is inside the boxes a mission building is furnished with.
    /// </summary>
    /// <remarks>
    /// The building already sends its chests - a ChestItemFullUpdate of
    /// identity type 51017 for each - but nothing stood behind them, so using
    /// one did nothing. This puts a container behind each, so the same open,
    /// take and close the corpses use works on them.
    ///
    /// **Most of them are empty.** Seventeen chests were opened in the QL250
    /// mission recorded on 2026-09-28: eleven held nothing, four held one
    /// thing, one held two and one held nine. That is the distribution used
    /// here, and it is the whole of what is known about how full a chest is.
    ///
    /// **What is in them is offered at the mission's quality.** Every entry in
    /// that recording read Quality 250 in a QL 250 mission, and every one was
    /// a template pair - the same (low, high) shape a mission reward uses. So
    /// the contents are drawn the same way a reward is, through
    /// <see cref="MissionRewards"/>.
    ///
    /// What is **not** known is whether a chest draws from the same pool a
    /// reward does. The fourteen items seen are not enough to say, and the
    /// reward pool is the only measured pool of things a mission hands out
    /// that we hold, so it is the one used.
    /// </remarks>
    public static class MissionChests
    {
        /// <summary>
        /// How many things a chest holds, as seventeen openings had it.
        /// </summary>
        /// <remarks>
        /// Eleven empty, four with one, one with two, one with nine. Indexed
        /// at random, so the shape of the draw is the shape that was seen
        /// rather than a curve fitted to it.
        /// </remarks>
        private static readonly int[] Sizes = { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 1, 1, 2, 9 };

        /// <summary>
        /// The most a chest can hold, which is its slot count on the wire.
        /// </summary>
        private const int Slots = 21;

        private static readonly Random Random = new Random();

        /// <summary>
        /// Gives every chest in a freshly built mission something to hold.
        /// </summary>
        /// <returns>How many chests were given contents.</returns>
        public static int Fill(Playfield playfield, MissionOffer mission)
        {
            if (playfield == null || mission == null || mission.Built == null
                || mission.Built.Furniture == null)
            {
                return 0;
            }

            int filled = 0;
            foreach (MissionFurniture thing in mission.Built.Furniture)
            {
                if (thing.Kind != MissionFurnitureKind.Chest || thing.Instance == 0)
                {
                    continue;
                }

                try
                {
                    var identity = new Identity { Type = (IdentityType)ChestType, Instance = thing.Instance };
                    // PooledObject puts itself in the pool, which is how the
                    // open path finds it later by identity.
                    var chest = new CorpseLoot(playfield.Identity, identity);

                    int wanted;
                    lock (Random)
                    {
                        wanted = Sizes[Random.Next(Sizes.Length)];
                    }

                    Stock(chest, mission.Quality, wanted);
                    filled++;
                }
                catch (Exception exception)
                {
                    LogUtil.ErrorException(exception);
                }
            }

            return filled;
        }

        /// <summary>
        /// The identity type a mission chest is sent as.
        /// </summary>
        public const int ChestType = 51017;

        private static void Stock(CorpseLoot chest, int quality, int wanted)
        {
            IInventoryPage page = chest.BaseInventory[chest.BaseInventory.StandardPage];
            for (int i = 0; i < wanted && i < Slots; i++)
            {
                MissionRewards.Band band;
                lock (Random)
                {
                    band = MissionRewards.Pick(quality, Random);
                }

                if (band == null)
                {
                    return;
                }

                int slot = page.FindFreeSlot();
                if (slot < 0)
                {
                    return;
                }

                page.Add(slot, new Item(band.Quality(quality), band.LowId, band.HighId));
            }
        }
    }
}
