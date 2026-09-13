// --------------------------------------------------------------------------------------------------------------------
// <copyright file="KnuBotOpenChatWindowMessage.cs" company="SmokeLounge">
//   Copyright © 2013 SmokeLounge.
//   This program is free software. It comes without any warranty, to
//   the extent permitted by applicable law. You can redistribute it
//   and/or modify it under the terms of the Do What The Fuck You Want
//   To Public License, Version 2, as published by Sam Hocevar. See
//   http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the KnuBotOpenChatWindowMessage type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace SmokeLounge.AOtomation.Messaging.Messages.N3Messages
{
    using SmokeLounge.AOtomation.Messaging.GameData;
    using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

    [AoContract((int)N3MessageType.KnuBotOpenChatWindow)]
    public class KnuBotOpenChatWindowMessage : N3Message
    {
        #region Constructors and Destructors

        public KnuBotOpenChatWindowMessage()
        {
            this.N3MessageType = N3MessageType.KnuBotOpenChatWindow;
        }

        #endregion

        #region AoMember Properties

        /// <summary>
        /// 2, and the reader accepts nothing else.
        /// </summary>
        /// <remarks>
        /// An int16, not an int32. KnubotBaseIIR_c::ReadSubClass at
        /// Gamecode.dll 0x10127DE6 reads it with the int16 reader and compares
        /// it against a literal 2 before it will read the Identity after it, so
        /// this is shared by every KnuBot message rather than being this one's.
        /// 2 in all 175 captured copies.
        /// </remarks>
        [AoMember(0)]
        public short Version { get; set; }

        /// <summary>
        /// Whose chat window to open.
        /// </summary>
        [AoMember(1)]
        public Identity Target { get; set; }

        /// <summary>
        /// A flag. 1 in 56 of the 175 captured copies.
        /// </summary>
        /// <remarks>
        /// An int32 on the wire and a bool in the client: the reader at
        /// 0x10128375 compares it against 1 and stores the result as a byte, so
        /// anything that is not 1 is false.
        ///
        /// What it turns on is not in this module. The dispatcher at
        /// 0x101283F3 hands both flags to a signal on AFCM.dll's
        /// GlobalSignals_c and each subscriber takes as many arguments as it
        /// declares - identity alone, identity and one flag, or identity and
        /// both - so the names live with whoever subscribes.
        /// </remarks>
        [AoMember(2)]
        public int Unknown2 { get; set; }

        /// <summary>
        /// The second flag, and zero in every captured copy.
        /// </summary>
        /// <remarks>
        /// Read the same way as the one above, at 0x1012838B, and passed to the
        /// same signal as the argument before it. Only the subscribers that
        /// take three arguments ever see it.
        /// </remarks>
        [AoMember(3)]
        public int Unknown3 { get; set; }

        #endregion
    }
}