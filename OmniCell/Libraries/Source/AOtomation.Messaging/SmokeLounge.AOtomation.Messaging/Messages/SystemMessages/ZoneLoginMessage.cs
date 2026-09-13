// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ZoneLoginMessage.cs" company="SmokeLounge">
//   Copyright © 2013 SmokeLounge.
//   This program is free software. It comes without any warranty, to
//   the extent permitted by applicable law. You can redistribute it
//   and/or modify it under the terms of the Do What The Fuck You Want
//   To Public License, Version 2, as published by Sam Hocevar. See
//   http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the ZoneLoginMessage type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace SmokeLounge.AOtomation.Messaging.Messages.SystemMessages
{
    using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

    [AoContract((int)SystemMessageType.ZoneLogin)]
    public class ZoneLoginMessage : SystemMessage
    {
        #region Constructors and Destructors

        public ZoneLoginMessage()
        {
            this.SystemMessageType = SystemMessageType.ZoneLogin;
        }

        #endregion

        #region AoMember Properties

        [AoMember(0)]
        public int CharacterId { get; set; }

        /// <summary>
        /// The first half of the ticket the login server issued.
        /// </summary>
        /// <remarks>
        /// Proven by following one value through a capture. At t=32.4s the
        /// login server on port 7505 sends the client a ZoneInfo carrying
        /// Cookie1 0x0A0B0D01 and Cookie2 0x0A0B0D02 (stand-ins for the
        /// session's real ticket); at t=36.6s the client
        /// opens the zone connection on 7512 and presents those same eight
        /// bytes here. The ZoneInfo that carried them also names the port to
        /// connect to - 0x1D58, which is 7512 - so the field alignment is not
        /// in doubt either.
        ///
        /// Nothing read them until 2026-09-10, so every ZoneLogin was modelled
        /// as twenty four bytes where the client sends thirty two.
        /// </remarks>
        [AoMember(1)]
        public uint Cookie1 { get; set; }

        /// <summary>
        /// The second half of the ticket. See Cookie1.
        /// </summary>
        [AoMember(2)]
        public uint Cookie2 { get; set; }

        #endregion
    }
}