// --------------------------------------------------------------------------------------------------------------------
// <copyright file="PingMessage.cs" company="SmokeLounge">
//   Copyright © 2013 SmokeLounge.
//   This program is free software. It comes without any warranty, to
//   the extent permitted by applicable law. You can redistribute it
//   and/or modify it under the terms of the Do What The Fuck You Want
//   To Public License, Version 2, as published by Sam Hocevar. See
//   http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the PingMessage type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace SmokeLounge.AOtomation.Messaging.Messages
{
    using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

    /// <summary>
    /// A ping, in either direction.
    /// </summary>
    /// <remarks>
    /// This had no fields at all until 2026-09-10, so it went out as a bare
    /// sixteen byte header where the live server sends forty. The six integers
    /// that were missing are named by the client itself: MessageProtocol.dll
    /// exports PingObjTypeGet, HopCountGet, OriginatorGetStamp, ReceiveGetStamp,
    /// TransmitGetStamp and SequenceGet on PingMessage_t, and
    /// PingMessage_t::CreateDataBlock writes exactly those six, each through
    /// htonl, in the order below.
    ///
    /// The client also carries the two names this message can have, in an array
    /// it calls PingMessageType: "Ping Echo Request" and "Ping Echo Reply".
    /// Which is which was settled from the captures rather than assumed - see
    /// PingObjType.
    ///
    /// A seventh field exists and has never been seen: CreateDataBlock appends
    /// a trailing body after the six integers when MessageBodyLen is non-zero.
    /// Every one of the 454 captured pings is forty bytes, so it is always
    /// empty, and nothing here writes it.
    /// </remarks>
    [AoContract((int)PacketType.PingMessage)]
    public class PingMessage : MessageBody
    {
        #region Constants

        /// <summary>
        /// A ping going out, asking for an echo.
        /// </summary>
        public const int Request = 1;

        /// <summary>
        /// The echo coming back.
        /// </summary>
        public const int Reply = 2;

        #endregion

        #region AoMember Properties

        /// <summary>
        /// Whether this is a request or the echo of one.
        /// </summary>
        /// <remarks>
        /// One or two, and the captures say which is which without any need to
        /// guess. Of 63 copies carrying 1, every one has both later stamps at
        /// zero: nothing has received it or sent it back yet, which is what a
        /// request looks like. Of 104 carrying 2, every one has both stamps set
        /// and equal, and 63 of them carry an originator stamp that also
        /// appears on a captured 1 - the echo, quoting the request it answers.
        ///
        /// The client indexes its own name array with this less one, so 1 lands
        /// on "Ping Echo Request" and 2 on "Ping Echo Reply".
        /// </remarks>
        [AoMember(0)]
        public int PingObjType { get; set; }

        /// <summary>
        /// Zero in all 454 captured pings.
        /// </summary>
        /// <remarks>
        /// The client exports HopCountInc alongside the getter and setter, so it
        /// is meant to count something on the way through. Nothing in the
        /// captures ever increments it.
        /// </remarks>
        [AoMember(1)]
        public int HopCount { get; set; }

        /// <summary>
        /// When whoever started this ping started it.
        /// </summary>
        /// <remarks>
        /// A millisecond clock: it advances about 30,274 between pings that are
        /// thirty seconds apart. A reply carries the stamp of the request it
        /// answers, unchanged, which is what makes the round trip measurable at
        /// the far end.
        /// </remarks>
        [AoMember(2)]
        public int OriginatorStamp { get; set; }

        /// <summary>
        /// When the answering side received the request. Zero in a request.
        /// </summary>
        [AoMember(3)]
        public int ReceiveStamp { get; set; }

        /// <summary>
        /// When the answering side sent the echo. Zero in a request.
        /// </summary>
        /// <remarks>
        /// Equal to ReceiveStamp in all 104 captured replies - the answering
        /// side turns a ping around inside its own clock resolution.
        /// </remarks>
        [AoMember(4)]
        public int TransmitStamp { get; set; }

        /// <summary>
        /// The sender's message sequence at the time of sending.
        /// </summary>
        [AoMember(5)]
        public int Sequence { get; set; }

        #endregion

        #region Public Properties

        public override PacketType PacketType
        {
            get
            {
                return PacketType.PingMessage;
            }
        }

        #endregion
    }
}