// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CharacterStatUpdate.cs" company="OmniCell">
//   Copyright © 2026 OmniCell contributors.
//   Added to SmokeLounge.AOtomation.Messaging, which is distributed under the
//   Do What The Fuck You Want To Public License, Version 2, as published by
//   Sam Hocevar. See http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the CharacterStatUpdate type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace SmokeLounge.AOtomation.Messaging.GameData
{
    using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

    /// <summary>
    /// The block SimpleCharFullUpdate carries behind bit 1 of its second flag
    /// word.
    /// </summary>
    /// <remarks>
    /// A plain int32 count, then that many (stat, value) int32 pairs, then a
    /// further int32 and an Identity. The object the client builds for it is
    /// constructed with 1234567890 - the stat "unset" sentinel that turns up all
    /// over this protocol - so the pairs are stat assignments and the stats left
    /// out keep that sentinel.
    ///
    /// Gamecode.dll 0x10009DAD reads the list; 0x10009D97 is the constructor
    /// that seeds it with the sentinel.
    /// </remarks>
    public class CharacterStatUpdate
    {
        #region AoMember Properties

        [AoMember(0)]
        public ItemStatPair[] Stats { get; set; }

        /// <summary>
        /// Mech data - stat 662.
        /// </summary>
        /// <remarks>
        /// The reader puts this at the message's + 0x31C and the dispatcher
        /// takes it from there at 0x10078B02, pushing it with 0x296 into the
        /// stat table at [character + 0xE8] through vtable + 0x40. 0x296 is
        /// 662, and MechInfo's own field was named from the same stat by the
        /// same route.
        /// </remarks>
        [AoMember(1)]
        public int MechData { get; set; }

        [AoMember(2)]
        public Identity Source { get; set; }

        #endregion
    }
}
