// --------------------------------------------------------------------------------------------------------------------
// <copyright file="BuildingRoomInfo.cs" company="OmniCell">
//   Copyright © 2026 OmniCell contributors.
//   Added to SmokeLounge.AOtomation.Messaging, which is distributed under the
//   Do What The Fuck You Want To Public License, Version 2, as published by
//   Sam Hocevar. See http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the BuildingRoomInfo type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace SmokeLounge.AOtomation.Messaging.GameData
{
    using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

    /// <summary>
    /// One room of a generated building. Six bytes, always.
    /// </summary>
    /// <remarks>
    /// The client calls it BuildingRoomInfo_t in so many words: the reader at
    /// Gamecode.dll 0x100CAD28 fails with "Found invalid BuildingRoomInfo_t in
    /// stream" at 0x1016BEEC. It reads an int16 and then four bytes, one at a
    /// time, and nothing about it is conditional.
    ///
    /// Every byte is named by what the client does with it rather than by what
    /// the captures happen to contain. The validator at 0x100CACFB bounds all
    /// five fields, the constructor at 0x100CAC83 takes them as arguments, and
    /// the position accessor at 0x100CACBC turns two of them into a Vector3 -
    /// which is what says which of those two is X and which is Z.
    /// </remarks>
    public class BuildingRoomInfo
    {
        #region AoMember Properties

        /// <summary>
        /// Which room to place. The validator refuses anything above 10,000.
        /// </summary>
        /// <remarks>
        /// 0 to 102 across the captured mission, and read back by the accessor
        /// at 0x100CACB8.
        /// </remarks>
        [AoMember(0)]
        public short Room { get; set; }

        /// <summary>
        /// Which floor the room is on, and it is signed: -16 to 16.
        /// </summary>
        /// <remarks>
        /// The validator compares it against 0xF0 and 0x10 with signed
        /// branches, and the accessor at 0x100CACB3 sign-extends it. Zero in
        /// all thirty two rooms of the captured mission, which is a mission on
        /// one floor.
        /// </remarks>
        [AoMember(1)]
        public sbyte Floor { get; set; }

        /// <summary>
        /// The room's column in the grid. The validator refuses anything above
        /// 32.
        /// </summary>
        /// <remarks>
        /// A cell, not a distance: the position accessor at 0x100CACC8 gives
        /// the room a world X of this times ten. 0 to 10 across the captured
        /// mission.
        /// </remarks>
        [AoMember(2)]
        public byte X { get; set; }

        /// <summary>
        /// The room's row in the grid. The validator refuses anything above 32.
        /// </summary>
        /// <remarks>
        /// Also a cell times ten, but subtracted rather than added - the
        /// accessor works out world Z as the playfield extent less this, so the
        /// grid runs the other way along Z than it does along X. 8 to 26 across
        /// the captured mission.
        /// </remarks>
        [AoMember(3)]
        public byte Z { get; set; }

        /// <summary>
        /// A quarter turn: 0, 1, 2 or 3, and the validator refuses 4 and above.
        /// </summary>
        /// <remarks>
        /// The constructor at 0x100CACA5 takes degrees and divides by 90 to get
        /// this, and the accessor at 0x100CACF3 multiplies it back.
        /// </remarks>
        [AoMember(4)]
        public byte Rotation { get; set; }

        #endregion
    }
}
