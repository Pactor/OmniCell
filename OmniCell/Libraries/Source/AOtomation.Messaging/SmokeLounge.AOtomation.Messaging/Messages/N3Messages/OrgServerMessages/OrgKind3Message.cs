// --------------------------------------------------------------------------------------------------------------------
// <copyright file="OrgKind3Message.cs" company="OmniCell">
//   Copyright (c) 2026 OmniCell contributors.
//   Added to SmokeLounge.AOtomation.Messaging, which is distributed under the
//   Do What The Fuck You Want To Public License, Version 2, as published by
//   Sam Hocevar. See http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the OrgKind3Message type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace SmokeLounge.AOtomation.Messaging.Messages.N3Messages.OrgServerMessages
{
    using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;
    /// <summary>
    /// OrgServer kind 3.
    /// </summary>
    /// <remarks>
    /// This kind carries no body. The switch at 0x10126E05 sends 3, 4 and 9 straight to the success exit at 0x10126F3C, which returns the stream's own error flag - so they are complete messages that stop after the kind and the two Identities, not reads the client failed.
    ///
    /// Added on 2026-09-11 with the other five kinds nothing had modelled. The
    /// reader at Gamecode 0x10126D95 takes the kind byte and refuses the whole
    /// message unless it is 1 to 9; the subclass table here held 2, 5 and 6, so
    /// a copy of any other kind would have matched no subclass and its body
    /// would not have been read at all. Every one of the 404 captured copies is
    /// a kind 6, so nothing had ever shown it.
    /// </remarks>
    [AoContract((byte)OrgServerMessageType.OrgKind3)]
    public class OrgKind3Message : OrgServerMessage
    {
        #region Constructors and Destructors

        public OrgKind3Message()
        {
            this.OrgServerMessageType = OrgServerMessageType.OrgKind3;
        }

        #endregion
    }
}
