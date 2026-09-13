// --------------------------------------------------------------------------------------------------------------------
// <copyright file="TradeMessage.cs" company="SmokeLounge">
//   Copyright © 2013 SmokeLounge.
//   This program is free software. It comes without any warranty, to
//   the extent permitted by applicable law. You can redistribute it
//   and/or modify it under the terms of the Do What The Fuck You Want
//   To Public License, Version 2, as published by Sam Hocevar. See
//   http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the TradeMessage type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace SmokeLounge.AOtomation.Messaging.Messages.N3Messages
{
    using SmokeLounge.AOtomation.Messaging.GameData;
    using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

    [AoContract((int)N3MessageType.Trade)]
    public class TradeMessage : N3Message
    {
        #region Constructors and Destructors

        public TradeMessage()
        {
            this.N3MessageType = N3MessageType.Trade;
        }

        #endregion

        #region AoMember Properties

        /// <summary>
        /// A version the client checks, and refuses the message without.
        /// </summary>
        /// <remarks>
        /// The reader at Gamecode.dll 0x1007AB65 reads this int32 first and
        /// compares it against the static at 0x101C169C, which holds 2. If they
        /// differ it returns an error and reads nothing else - no action and no
        /// identities. The writer at 0x1007ABCF emits that same static rather
        /// than any field of its own, so the client can only ever send 2.
        ///
        /// All 124 captured copies carry 2, which is what a constant looks like
        /// from the outside; the client is where it is settled.
        /// </remarks>
        [AoMember(0)]
        public int Version { get; set; }

        [AoMember(1)]
        public TradeAction Action { get; set; }

        [AoMember(2)]
        public Identity Target { get; set; }

        [AoMember(3)]
        public Identity Container { get; set; }

        #endregion
    }
}