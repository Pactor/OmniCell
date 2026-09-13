// --------------------------------------------------------------------------------------------------------------------
// <copyright file="KnuBotAppendTextMessage.cs" company="SmokeLounge">
//   Copyright © 2013 SmokeLounge.
//   This program is free software. It comes without any warranty, to
//   the extent permitted by applicable law. You can redistribute it
//   and/or modify it under the terms of the Do What The Fuck You Want
//   To Public License, Version 2, as published by Sam Hocevar. See
//   http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the KnuBotAppendTextMessage type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace SmokeLounge.AOtomation.Messaging.Messages.N3Messages
{
    using SmokeLounge.AOtomation.Messaging.GameData;
    using SmokeLounge.AOtomation.Messaging.Serialization;
    using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

    /// <summary>
    /// A line of NPC dialogue.
    /// </summary>
    /// <remarks>
    /// This carried a trailing int32, Unknown3, which is not on the wire. Every
    /// one of these failed to deserialise as a result: the reader ran four bytes
    /// past the end of the message.
    ///
    /// Checked against 50 of them from a live 18.8.x session. The short, the
    /// Identity, the int and the length prefixed text account for the payload
    /// exactly in all 50, with nothing left over in any of them.
    /// </remarks>
    [AoContract((int)N3MessageType.KnuBotAppendText)]
    public class KnuBotAppendTextMessage : N3Message
    {
        #region Constructors and Destructors

        public KnuBotAppendTextMessage()
        {
            this.N3MessageType = N3MessageType.KnuBotAppendText;
        }

        #endregion

        #region AoMember Properties

        /// <summary>
        /// The KnuBot protocol version, 2 in every captured copy.
        /// </summary>
        /// <remarks>
        /// Not this message's own field: KnubotBaseIIR_c reads and checks it,
        /// so every KnuBot message carries it in front of its own body. Named
        /// on 2026-09-12 to match the three classes in the family that already
        /// called it Version - it had been Unknown1 in the other six, and our
        /// own handlers were setting it to 2 through that name.
        /// </remarks>
        [AoMember(0)]
        public short Version { get; set; }

        [AoMember(1)]
        public Identity Target { get; set; }

        [AoMember(2)]
        public int Unknown2 { get; set; }

        [AoMember(3, SerializeSize = ArraySizeType.Int32)]
        public string Text { get; set; }

        #endregion
    }
}