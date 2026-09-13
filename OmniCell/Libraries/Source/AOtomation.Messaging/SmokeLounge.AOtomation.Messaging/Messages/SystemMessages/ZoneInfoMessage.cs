// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ZoneInfoMessage.cs" company="SmokeLounge">
//   Copyright © 2013 SmokeLounge.
//   This program is free software. It comes without any warranty, to
//   the extent permitted by applicable law. You can redistribute it
//   and/or modify it under the terms of the Do What The Fuck You Want
//   To Public License, Version 2, as published by Sam Hocevar. See
//   http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the ZoneInfoMessage type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace SmokeLounge.AOtomation.Messaging.Messages.SystemMessages
{
    using System.Net;

    using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

    /// <summary>
    /// Where to go to play the character that was just selected.
    /// </summary>
    /// <remarks>
    /// Three captured copies, all 46 bytes, and this used to read 38 of them
    /// and leave the last eight on the floor. Two logins caught on 2026-09-11
    /// are what settled them, because the same eight bytes could finally be
    /// compared across sessions:
    ///
    ///   character   address     port  cookie             tail
    ///   0A0B0C06    2512C114    1D55  0A0B0D03 0A0B0D04  00000001 59DAD28A
    ///   0A0B0C06    2512C114    1D58  0A0B0D05 0A0B0D06  00000001 59DAD28A
    ///   0A0B0C05    2512C113    1D4D  0A0B0D07 0A0B0D08  00000000 59DAD28A
    ///
    /// So the tail is two int32s, not eight opaque bytes. The second is the
    /// same value in all three - across two different characters, three
    /// different zone ports and captures taken a day apart - and it is not a
    /// literal in Gamecode, Connection, N3, Interfaces or the executable, so
    /// it is the server's own datum rather than anything the client checks.
    /// The first is 0 for the copy that pointed at port 7501 and 1 for the two
    /// that pointed at 7509 and 7512.
    ///
    /// What they mean is still open. What is no longer open is that they are
    /// there and that the client does not need them: this server sends the
    /// short form and the client plays, so it stops reading once it has the
    /// address, the port and the cookie. They are read and written here so a
    /// retail copy survives a round trip instead of being reported as a
    /// failure for the rest of time; this server leaves both zero.
    ///
    /// The cookie is eight bytes and not sixteen: ZoneLogin, which is the
    /// client handing the cookie back to the zone, is thirty two bytes and
    /// carries a character id and two int32s, and it round-trips.
    /// </remarks>
    [AoContract((int)SystemMessageType.ZoneInfo)]
    public class ZoneInfoMessage : SystemMessage
    {
        #region Constructors and Destructors

        public ZoneInfoMessage()
        {
            this.SystemMessageType = SystemMessageType.ZoneInfo;
        }

        #endregion

        #region AoMember Properties

        [AoMember(0)]
        public int CharacterId { get; set; }

        [AoMember(1)]
        public IPAddress ServerIpAddress { get; set; }

        [AoMember(2)]
        public ushort ServerPort { get; set; }

        [AoMember(3)]
        public uint Cookie1 { get; set; }

        [AoMember(4)]
        public uint Cookie2 { get; set; }

        /// <summary>
        /// 0 for the copy pointing at port 7501, 1 for the two pointing at
        /// 7509 and 7512.
        /// </summary>
        [AoMember(5)]
        public int Unknown1 { get; set; }

        /// <summary>
        /// 0x59DAD28A in all three captured copies.
        /// </summary>
        [AoMember(6)]
        public uint Unknown2 { get; set; }

        #endregion
    }
}