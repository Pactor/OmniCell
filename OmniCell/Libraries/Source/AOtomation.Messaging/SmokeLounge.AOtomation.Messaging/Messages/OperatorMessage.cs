// --------------------------------------------------------------------------------------------------------------------
// <copyright file="OperatorMessage.cs" company="SmokeLounge">
//   Copyright © 2013 SmokeLounge.
//   This program is free software. It comes without any warranty, to
//   the extent permitted by applicable law. You can redistribute it
//   and/or modify it under the terms of the Do What The Fuck You Want
//   To Public License, Version 2, as published by Sam Hocevar. See
//   http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the OperatorMessage type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace SmokeLounge.AOtomation.Messaging.Messages
{
    using SmokeLounge.AOtomation.Messaging.Serialization;
    using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

    [AoContract((int)PacketType.OperatorMessage)]
        /// <summary>
    /// Server statistics, sent unasked.
    /// </summary>
    /// <remarks>
    /// This class has no fields and the message is not empty. Captured ones are
    /// 548 bytes, 108 of them across the sessions, and the body is regular:
    /// a version, the sender again, and then repeating blocks each introduced
    /// by a count of ten - ten integers, ten more integers, ten floats around
    /// 6.1, and a float that reads as a Unix timestamp matching the capture.
    ///
    /// Sampled series of ten, timestamped, arriving on their own schedule: this
    /// is the server telling the client how it is doing, not anything about the
    /// game world. Nothing a player does depends on it.
    ///
    /// Left undecoded on purpose. It is the largest unread message remaining
    /// and the least worth reading, and saying that plainly is better than
    /// leaving the next person to work out for themselves that the empty class
    /// was a decision rather than an oversight.
    ///
    /// The decision stands; what changed on 2026-09-11 is that the bytes are
    /// now carried instead of dropped, so this stopped being the one message in
    /// the whole corpus that fails the round trip. With every other type
    /// byte-exact, a clean audit is worth having: the next capture's failures
    /// are then all news.
    ///
    /// The shape, for whoever does decode it. All 460 copies across every
    /// capture set are exactly 548 bytes, the version is 11 in every one, and
    /// all twelve count slots hold 10 in every one. The 133 body dwords go: the
    /// version, the sender again, then three identical groups of 41, then eight
    /// dwords of tail. Each group is a counted array of ten int32s, a second
    /// counted array of ten int32s, a counted array of ten floats clustered
    /// around 6 to 10, a float that reads as a Unix timestamp matching the
    /// capture, and a seven dword record whose first dword is the constant 10.
    /// The tail is one non-zero dword and seven zeros.
    ///
    /// Sampled series of ten with a timestamp, three of them, and a summary
    /// record each: that is a server reporting load. The counts are counts, so
    /// a server sending anything other than ten would break the fixed length
    /// below - and it would break loudly, in the audit, which is the right way
    /// round.
    /// </remarks>
public class OperatorMessage : MessageBody
    {
        #region Public Properties

        public override PacketType PacketType
        {
            get
            {
                return PacketType.OperatorMessage;
            }
        }

        /// <summary>
        /// The whole body, carried rather than read.
        /// </summary>
        /// <remarks>
        /// 532 bytes in all 460 captured copies. No count precedes it on the
        /// wire, which is why this is a fixed length rather than a counted
        /// array.
        /// </remarks>
        [AoMember(0, SerializeSize = ArraySizeType.NoSerialization, FixedSizeLength = 532)]
        public byte[] Payload { get; set; }

        #endregion
    }
}