// --------------------------------------------------------------------------------------------------------------------
// <copyright file="OrgKind7Message.cs" company="OmniCell">
//   Copyright (c) 2026 OmniCell contributors.
//   Added to SmokeLounge.AOtomation.Messaging, which is distributed under the
//   Do What The Fuck You Want To Public License, Version 2, as published by
//   Sam Hocevar. See http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the OrgKind7Message type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace SmokeLounge.AOtomation.Messaging.Messages.N3Messages.OrgServerMessages
{
    using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;
    /// <summary>
    /// OrgServer kind 7.
    /// </summary>
    /// <remarks>
    /// One int32, into the message's + 0xD4. The switch subtracts 7 and takes anything at or below 1, so kinds 7 and 8 share the branch at 0x10126E73 and read the same single field.
    ///
    /// Added on 2026-09-11 with the other five kinds nothing had modelled. The
    /// reader at Gamecode 0x10126D95 takes the kind byte and refuses the whole
    /// message unless it is 1 to 9; the subclass table here held 2, 5 and 6, so
    /// a copy of any other kind would have matched no subclass and its body
    /// would not have been read at all. Every one of the 404 captured copies is
    /// a kind 6, so nothing had ever shown it.
    /// </remarks>
    [AoContract((byte)OrgServerMessageType.OrgKind7)]
    public class OrgKind7Message : OrgServerMessage
    {
        #region Constructors and Destructors

        public OrgKind7Message()
        {
            this.OrgServerMessageType = OrgServerMessageType.OrgKind7;
        }

        #endregion

        #region AoMember Properties

        /// <summary>
        /// The one int32 this kind carries, into the message's + 0xD4.
        /// </summary>
        [AoMember(0)]
        public int Unknown1 { get; set; }

        #endregion
    }
}
