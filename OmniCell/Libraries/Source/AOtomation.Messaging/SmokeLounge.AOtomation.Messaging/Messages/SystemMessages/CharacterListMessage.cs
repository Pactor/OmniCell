// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CharacterListMessage.cs" company="SmokeLounge">
//   Copyright © 2013 SmokeLounge.
//   This program is free software. It comes without any warranty, to
//   the extent permitted by applicable law. You can redistribute it
//   and/or modify it under the terms of the Do What The Fuck You Want
//   To Public License, Version 2, as published by Sam Hocevar. See
//   http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the CharacterListMessage type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace SmokeLounge.AOtomation.Messaging.Messages.SystemMessages
{
    using SmokeLounge.AOtomation.Messaging.GameData;
    using SmokeLounge.AOtomation.Messaging.Serialization;
    using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

    /// <summary>
    /// The characters on this account, sent after the credentials are accepted.
    /// </summary>
    /// <remarks>
    /// Retail sends four more bytes than this class read until 2026-09-11,
    /// and all four are at the end: the two captures taken that day carry
    /// copies of 151 and 266 bytes, one character and two, and both finish
    /// 00-00-00-0A 00-00-00-AB 00-00-00-00. The first two are the allowed
    /// count and the expansions; the third is <see cref="Unknown1"/>.
    ///
    /// Every other byte of both copies already matched, which is what says the
    /// shortfall is a trailing field and not a missing one inside the record.
    /// </remarks>
    [AoContract((int)SystemMessageType.CharacterList)]
    public class CharacterListMessage : SystemMessage
    {
        #region Constructors and Destructors

        public CharacterListMessage()
        {
            this.SystemMessageType = SystemMessageType.CharacterList;
        }

        #endregion

        #region AoMember Properties

        [AoMember(0, SerializeSize = ArraySizeType.Int32)]
        public LoginCharacterInfo[] Characters { get; set; }

        [AoMember(1)]
        public int AllowedCharacters { get; set; }

        [AoMember(2)]
        public int Expansions { get; set; }

        /// <summary>
        /// Zero in both captured copies.
        /// </summary>
        /// <remarks>
        /// The client does not require it: this server has always sent the
        /// short form and logging in works. Read and written so a retail copy
        /// survives a round trip.
        /// </remarks>
        [AoMember(3)]
        public int Unknown1 { get; set; }

        #endregion
    }
}