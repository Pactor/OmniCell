// --------------------------------------------------------------------------------------------------------------------
// <copyright file="OwnedBuildingDynelRun.cs" company="OmniCell">
//   Copyright © 2026 OmniCell contributors.
//   Added to SmokeLounge.AOtomation.Messaging, which is distributed under the
//   Do What The Fuck You Want To Public License, Version 2, as published by
//   Sam Hocevar. See http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the OwnedBuildingDynelRun and OwnedBuildingPlacement types.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace SmokeLounge.AOtomation.Messaging.GameData
{
    using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

    /// <summary>
    /// One kind of thing in an owned building, and where its copies are.
    /// </summary>
    /// <remarks>
    /// The shape is the client's. WriteBlob hands its vector to 0x1013E59F,
    /// which writes the element count and then each element through 0x1013E502;
    /// that writes the dword at the element's + 0 and then a nested vector of
    /// twelve byte entries, three int32s each, at + 4. So a run is a type and a
    /// list of placements rather than a flat record.
    ///
    /// Worth knowing when reading the neighbouring PlayfieldDynelRun, which
    /// 51069 uses and which this project models as five flat int32s: every
    /// placement list in both captured apartments holds exactly one entry, so a
    /// flat reading of five int32s produces identical bytes here. If 51069's own
    /// writer turns out to nest the same way, its second field is a count rather
    /// than the Unknown it is called, and no capture where every count is one
    /// could tell the difference. That has not been checked.
    /// </remarks>
    public class OwnedBuildingDynelRun
    {
        #region AoMember Properties

        /// <summary>
        /// The identity type of the things this run places.
        /// </summary>
        /// <remarks>
        /// 51005, 51059 and 51016 in the captured apartment. 51005 is the type
        /// the terminals and floor objects of a mission also carry.
        /// </remarks>
        [AoMember(0)]
        public IdentityType Type { get; set; }

        /// <summary>
        /// Where the copies of it are.
        /// </summary>
        [AoMember(1)]
        public OwnedBuildingPlacement[] Placements { get; set; }

        #endregion
    }

    /// <summary>
    /// A consecutive run of one kind of thing.
    /// </summary>
    /// <remarks>
    /// Three int32s, and the reading is the same arithmetic that settled the
    /// 51069 table: the start indexes run 0, 3, 4, 5, 6 and the counts 3, 1, 1,
    /// 1, 2, so each start is the previous start plus the previous count, and
    /// the instances are consecutive from the first. The run of three from
    /// 250173593 is what accounts for the bank terminal at 250173594, which has
    /// no record of its own.
    /// </remarks>
    public class OwnedBuildingPlacement
    {
        #region AoMember Properties

        /// <summary>
        /// Where this run starts in the building's own numbering.
        /// </summary>
        [AoMember(0)]
        public int StartIndex { get; set; }

        /// <summary>
        /// How many there are.
        /// </summary>
        [AoMember(1)]
        public int Count { get; set; }

        /// <summary>
        /// The instance of the first, the rest counting up from it.
        /// </summary>
        [AoMember(2)]
        public int FirstInstance { get; set; }

        #endregion
    }
}
