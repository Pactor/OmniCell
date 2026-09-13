// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ActiveNano.cs" company="SmokeLounge">
//   Copyright © 2013 SmokeLounge.
//   This program is free software. It comes without any warranty, to
//   the extent permitted by applicable law. You can redistribute it
//   and/or modify it under the terms of the Do What The Fuck You Want
//   To Public License, Version 2, as published by Sam Hocevar. See
//   http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the ActiveNano type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace SmokeLounge.AOtomation.Messaging.GameData
{
    using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

    public class ActiveNano
    {
        #region AoMember Properties

        [AoMember(0)]
        public int NanoId { get; set; }

        [AoMember(1)]
        public int NanoInstance { get; set; }

        /// <summary>
        /// An int32 between the identity and the two times, which the client
        /// reads and throws away.
        /// </summary>
        /// <remarks>
        /// Gamecode.dll 0x10052084 reads five int32s here: an Identity, then
        /// this, then Time1 and Time2. It reads this one into the stack slot
        /// holding its own argument, which is to say into scratch - it is on the
        /// wire and the client does not keep it.
        ///
        /// It was missing until the reader was taken out of the client, and it
        /// was the last thing standing between this message and a byte exact
        /// round trip: every character with a nano running on it read four
        /// int32s where there were five, took the fifth for the array header
        /// that follows, and lost the rest of the packet. X3F1Count returning
        /// zero for a value that is not a multiple of 0x3F1 is what let that
        /// pass quietly.
        /// </remarks>
        [AoMember(2)]
        public int Unknown { get; set; }

        /// <summary>
        /// The server time the client subtracts from its own clock.
        /// </summary>
        [AoMember(3)]
        public int Time1 { get; set; }

        /// <summary>
        /// Added to that difference to give the moment the nano runs out.
        /// </summary>
        [AoMember(4)]
        public int Time2 { get; set; }

        #endregion
    }
}