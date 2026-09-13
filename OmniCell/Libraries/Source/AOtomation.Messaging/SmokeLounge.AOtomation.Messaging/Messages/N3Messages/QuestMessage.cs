// --------------------------------------------------------------------------------------------------------------------
// <copyright file="QuestMessage.cs" company="OmniCell">
//   Copyright © 2026 OmniCell contributors.
//   Added to SmokeLounge.AOtomation.Messaging, which is distributed under the
//   Do What The Fuck You Want To Public License, Version 2, as published by
//   Sam Hocevar. See http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the QuestMessage type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace SmokeLounge.AOtomation.Messaging.Messages.N3Messages
{
    using SmokeLounge.AOtomation.Messaging.GameData;
    using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

    /// <summary>
    /// Names a single quest, without its detail.
    /// </summary>
    /// <remarks>
    /// N3MessageType.Quest was in the enum but had no class behind it, so these
    /// could never deserialise. Reconstructed from 14 of them captured during a
    /// newbie run on an 18.8.x client.
    ///
    /// Every one was 53 bytes, and only the identity differed between them -
    /// the four integers were 1, 0, 0, 0 in all fourteen. The identity is the
    /// same value that appears as QuestIdentity inside QuestFullUpdateMessage,
    /// which is what ties the two together: this message refers to a quest the
    /// client already has the detail for.
    ///
    /// The identity type 0x0000DAC3 was likewise absent from IdentityType. It
    /// sits alongside MissionEntrance (0xDAC6) and MissionTerminal (0xDCA1),
    /// the rest of the mission family.
    /// </remarks>
    [AoContract((int)N3MessageType.Quest)]
    public class QuestMessage : N3Message
    {
        #region Constructors and Destructors

        public QuestMessage()
        {
            this.N3MessageType = N3MessageType.Quest;
        }

        #endregion

        #region AoMember Properties

        /// <summary>
        /// 1, and the reader accepts nothing else.
        /// </summary>
        /// <remarks>
        /// Compared against the static at Gamecode.dll 0x101C1364; a mismatch
        /// abandons the message. 1 in all 102 captured copies.
        /// </remarks>
        [AoMember(0)]
        public int Version { get; set; }

        /// <summary>
        /// 0 in every captured copy, and the client cannot send anything else.
        /// </summary>
        /// <remarks>
        /// The second argument of QuestIIR_t's constructor at 0x10076B74, and
        /// the one call site it has - 0x10019FA1 - pushes a literal zero for
        /// it. The apply at 0x10076B36 does not read it either, so nothing in
        /// the client either sets it or acts on it.
        /// </remarks>
        [AoMember(1)]
        public int Unknown2 { get; set; }

        /// <summary>
        /// The quest being referred to, and the only field the client acts on.
        /// </summary>
        /// <remarks>
        /// The apply resolves the message identity to a character, asks it for
        /// its quest list - creating one at 0x10058CBF if it has none - and
        /// looks this identity up in that list with the Identity comparison at
        /// 0x10001DDC. Type is <see cref="IdentityType.Quest"/> in all 102
        /// captured copies, with 85 distinct instances.
        /// </remarks>
        [AoMember(2)]
        public Identity QuestIdentity { get; set; }

        /// <summary>
        /// A second Identity, empty in every captured copy.
        /// </summary>
        /// <remarks>
        /// This was modelled as two int32s, which round-trips because two
        /// int32s and an Identity are the same eight bytes, but the reader at
        /// 0x10076ADF calls the standard Identity reader for it just as it does
        /// for the quest above.
        ///
        /// Like the int32 above it, the client can only ever send it empty: it
        /// is the constructor's fourth argument and the only call site passes
        /// the address of two words it has just zeroed. Nothing reads it back.
        /// </remarks>
        [AoMember(3)]
        public Identity Unknown3 { get; set; }

        #endregion
    }
}
