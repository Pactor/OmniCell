// --------------------------------------------------------------------------------------------------------------------
// <copyright file="GameTimeMessage.cs" company="SmokeLounge">
//   Copyright © 2013 SmokeLounge.
//   This program is free software. It comes without any warranty, to
//   the extent permitted by applicable law. You can redistribute it
//   and/or modify it under the terms of the Do What The Fuck You Want
//   To Public License, Version 2, as published by Sam Hocevar. See
//   http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the GameTimeMessage type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace SmokeLounge.AOtomation.Messaging.Messages.N3Messages
{
    using SmokeLounge.AOtomation.Messaging.GameData;
    using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

    [AoContract((int)N3MessageType.GameTime)]
    public class GameTimeMessage : N3Message
    {
        #region Constructors and Destructors

        public GameTimeMessage()
        {
            this.N3MessageType = N3MessageType.GameTime;
        }

        #endregion

        #region AoMember Properties

        /// <summary>
        /// Where the clock is within the game day, in seconds.
        /// </summary>
        /// <remarks>
        /// The one float of the four. The reader takes it at Gamecode
        /// 0x1003978B through the stream's float operator, and the dispatcher
        /// at 0x10039800 loads it with fld and passes it as the first argument
        /// of GameTime_t::Update(float, DayPeriod_e, int, int), which
        /// decomposes it into hour, minute and second.
        /// </remarks>
        [AoMember(0)]
        public float CurrentGameTime { get; set; }

        /// <summary>
        /// Which part of the day it is.
        /// </summary>
        [AoMember(1)]
        public DayPeriod DayPeriod { get; set; }

        /// <summary>
        /// The game day number.
        /// </summary>
        /// <remarks>
        /// Returned afterwards by GameTime_t::GetCurrentDay.
        /// </remarks>
        [AoMember(2)]
        public int CurrentGameDay { get; set; }

        /// <summary>
        /// The system-time reference the client advances the clock against.
        /// </summary>
        /// <remarks>
        /// An int32, and this class had it as a float until 2026-09-12. The
        /// reader settles it: 0x1003977A takes one float through the stream's
        /// float operator at 0x101540CC and then three int32s through
        /// 0x101540BC, into + 0x18, + 0x1C, + 0x20 and + 0x24, and the
        /// dispatcher pushes + 0x24 as the last of Update's three int
        /// parameters. Four bytes either way, so every capture round trips
        /// through the wrong type without complaint - which is why this page
        /// noted the mistake a while ago and the class kept it anyway.
        ///
        /// What the value should be is a separate question and is not settled
        /// here. Our own server was filling it with 80183.3125f; the bytes of
        /// that float read as the int 1201445800, which is what the client has
        /// been receiving.
        /// </remarks>
        [AoMember(3)]
        public int SystemTimeReference { get; set; }

        #endregion
    }
}