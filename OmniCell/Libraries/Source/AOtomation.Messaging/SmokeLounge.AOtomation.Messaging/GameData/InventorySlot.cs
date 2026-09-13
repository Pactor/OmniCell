// --------------------------------------------------------------------------------------------------------------------
// <copyright file="InventorySlot.cs" company="SmokeLounge">
//   Copyright © 2013 SmokeLounge.
//   This program is free software. It comes without any warranty, to
//   the extent permitted by applicable law. You can redistribute it
//   and/or modify it under the terms of the Do What The Fuck You Want
//   To Public License, Version 2, as published by Sam Hocevar. See
//   http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the InventorySlot type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace SmokeLounge.AOtomation.Messaging.GameData
{
    using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

    /// <summary>
    /// One item in a container, as FullCharacter sends it.
    /// </summary>
    /// <remarks>
    /// The client reads a whole container with 0x1002A5DB: an X3F1 count, then
    /// per entry an int32 placement and the record its element reader at
    /// 0x1002A20B takes - two int16s, an Identity, and a GameData::ACGItem_t.
    /// The same record shape turns up as InventoryEntry, which is the client's
    /// own InventoryEntry_t; this is the FullCharacter spelling of it.
    /// </remarks>
    public class InventorySlot
    {
        #region AoMember Properties

        /// <summary>
        /// Where in the container the item sits.
        /// </summary>
        /// <remarks>
        /// Read by the container loop at 0x1002A620, ahead of the record, and
        /// used as the key the entry is filed under at 0x1002A55C.
        /// </remarks>
        [AoMember(0)]
        public int Placement { get; set; }

        /// <summary>
        /// The first of the record's two int16s.
        /// </summary>
        /// <remarks>
        /// Read at 0x1002A21D into the record's +0. See
        /// <see cref="InventoryEntry.Flags"/>.
        /// </remarks>
        [AoMember(1)]
        public short Flags { get; set; }

        /// <summary>
        /// The second, read at 0x1002A225 into the record's +2.
        /// </summary>
        [AoMember(2)]
        public short Count { get; set; }

        /// <summary>
        /// The item.
        /// </summary>
        [AoMember(3)]
        public Identity Identity { get; set; }

        /// <summary>
        /// The item's first template.
        /// </summary>
        /// <remarks>
        /// This and the three below are one GameData::ACGItem_t, read by
        /// GameData's own operator at 0x1000E9D7, which the record reader calls
        /// through the import at Gamecode 0x10154724.
        /// </remarks>
        [AoMember(4)]
        public int ItemLowId { get; set; }

        /// <summary>
        /// The item's second template.
        /// </summary>
        /// <remarks>
        /// The reader substitutes the first template when this arrives zero, at
        /// 0x1000EA18.
        /// </remarks>
        [AoMember(5)]
        public int ItemHighId { get; set; }

        /// <summary>
        /// The item's level, masked to 0x1FF on the way in.
        /// </summary>
        /// <remarks>
        /// ACGItem_t calls it the level - SetLevel, GetLevel - and its reader
        /// ands anything above 511 down at 0x1000EA11.
        /// </remarks>
        [AoMember(6)]
        public int Quality { get; set; }

        /// <summary>
        /// A fourth number that is always zero.
        /// </summary>
        /// <remarks>
        /// ACGItem_t puts four ints on the wire and holds three. The writer at
        /// GameData 0x1000E9C3 pushes a literal zero for this one and the
        /// reader at 0x1000EA01 reads it into a local nothing looks at, so it
        /// is reserved rather than unknown. Zero in every captured entry.
        /// </remarks>
        [AoMember(7)]
        public int Unused { get; set; }

        #endregion
    }
}