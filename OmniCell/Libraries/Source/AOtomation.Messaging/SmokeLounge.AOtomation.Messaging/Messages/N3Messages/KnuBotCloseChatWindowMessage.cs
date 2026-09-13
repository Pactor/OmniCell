// --------------------------------------------------------------------------------------------------------------------
// <copyright file="KnuBotCloseChatWindowMessage.cs" company="SmokeLounge">
//   Copyright © 2013 SmokeLounge.
//   This program is free software. It comes without any warranty, to
//   the extent permitted by applicable law. You can redistribute it
//   and/or modify it under the terms of the Do What The Fuck You Want
//   To Public License, Version 2, as published by Sam Hocevar. See
//   http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the KnuBotCloseChatWindowMessage type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace SmokeLounge.AOtomation.Messaging.Messages.N3Messages
{
    using SmokeLounge.AOtomation.Messaging.GameData;
    using SmokeLounge.AOtomation.Messaging.Serialization;
    using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

    [AoContract((int)N3MessageType.KnuBotCloseChatWindow)]
    public class KnuBotCloseChatWindowMessage : N3Message
    {
        #region Constructors and Destructors

        public KnuBotCloseChatWindowMessage()
        {
            this.N3MessageType = N3MessageType.KnuBotCloseChatWindow;
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
        public int Seconds { get; set; }

        /// <summary>
        /// Why the window is closing, when the server says why.
        /// </summary>
        /// <remarks>
        /// This was Unknown3, an integer, and it was the length of a string
        /// nothing read: "You are too far away from ICC Immigration Officer Bill
        /// to continue this conversation." is eighty five characters and the
        /// integer is eighty five. Sixty of the eighty two captured copies
        /// carry no message and a length of zero, which is why the missing
        /// field went unnoticed - the twenty two that do say something were
        /// written as a length with nothing after it.
        ///
        /// No terminator here: the text ends on its full stop and the length
        /// counts exactly the characters. Three string conventions now, and
        /// this is the plain one.
        /// </remarks>
        [AoMember(3, SerializeSize = ArraySizeType.Int32)]
        public string Reason { get; set; }

        #endregion
    }
}