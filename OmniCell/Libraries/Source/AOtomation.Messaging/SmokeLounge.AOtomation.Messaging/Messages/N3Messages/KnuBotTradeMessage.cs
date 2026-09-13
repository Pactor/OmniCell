// --------------------------------------------------------------------------------------------------------------------
// <copyright file="KnuBotTradeMessage.cs" company="OmniCell">
//   Copyright © 2026 OmniCell contributors.
//   Added to SmokeLounge.AOtomation.Messaging, which is distributed under the
//   Do What The Fuck You Want To Public License, Version 2, as published by
//   Sam Hocevar. See http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the KnuBotTradeMessage type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace SmokeLounge.AOtomation.Messaging.Messages.N3Messages
{
    using SmokeLounge.AOtomation.Messaging.GameData;
    using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

    /// <summary>
    /// An item put into or taken out of a conversation's trade window.
    /// </summary>
    /// <remarks>
    /// KnubotTradeIIR_c has vtable 0x1017165C, reader 0x10128CDB, writer
    /// 0x10128D26 and a dispatcher at 0x10128CD8 that is two instructions and a
    /// return. So the client only ever sends this, and the field names come
    /// from the senders rather than from any handler.
    ///
    /// There are exactly two of them, and the export table names both:
    /// N3Msg_NPCChatAddTradeItem and N3Msg_NPCChatRemoveTradeItem, each taking
    /// three Identities. Both build the message through the constructor at
    /// 0x10128D7E, and the only difference between the two calls is the third
    /// argument - AddTradeItem pushes 0 at 0x10017F5C and RemoveTradeItem
    /// pushes 1 at 0x10017FD7. That argument is <see cref="Action"/>.
    ///
    /// The reader is the base class's - an int16 version checked against 2 and
    /// an Identity, shared by every KnuBot message - then an int32 and two
    /// Identities.
    ///
    /// No capture contains one.
    /// </remarks>
    [AoContract((int)N3MessageType.KnuBotTrade)]
    public class KnuBotTradeMessage : N3Message
    {
        #region Constructors and Destructors

        public KnuBotTradeMessage()
        {
            this.N3MessageType = N3MessageType.KnuBotTrade;
        }

        #endregion

        #region AoMember Properties

        /// <summary>
        /// 2, and the base class refuses anything else.
        /// </summary>
        /// <remarks>
        /// Read and checked at 0x10127DFB before any other field, the same as
        /// for every other KnuBot message.
        /// </remarks>
        [AoMember(0)]
        public short Version { get; set; }

        /// <summary>
        /// Who the conversation is with.
        /// </summary>
        [AoMember(1)]
        public Identity Target { get; set; }

        /// <summary>
        /// Whether the item is going in or coming out.
        /// </summary>
        [AoMember(2)]
        public KnuBotTradeAction Action { get; set; }

        /// <summary>
        /// An Identity both senders zero.
        /// </summary>
        /// <remarks>
        /// This was two int32s, which is the same eight bytes and the wrong
        /// shape: the reader takes it with the client's Identity reader at
        /// 0x10128D06, in the call before the one that takes the item. Both
        /// exported senders pass the address of two words they have just
        /// cleared, so the client cannot put anything else in it and the
        /// captures cannot help - there are none.
        /// </remarks>
        [AoMember(3)]
        public Identity Unknown1 { get; set; }

        /// <summary>
        /// The item being added or removed.
        /// </summary>
        /// <remarks>
        /// The third argument of both exported senders.
        /// </remarks>
        [AoMember(4)]
        public Identity Item { get; set; }

        #endregion
    }
}
