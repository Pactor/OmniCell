// --------------------------------------------------------------------------------------------------------------------
// <copyright file="N3TeleportMessage.cs" company="SmokeLounge">
//   Copyright © 2013 SmokeLounge.
//   This program is free software. It comes without any warranty, to
//   the extent permitted by applicable law. You can redistribute it
//   and/or modify it under the terms of the Do What The Fuck You Want
//   To Public License, Version 2, as published by Sam Hocevar. See
//   http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the N3TeleportMessage type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace SmokeLounge.AOtomation.Messaging.Messages.N3Messages
{
    using SmokeLounge.AOtomation.Messaging.GameData;
    using SmokeLounge.AOtomation.Messaging.Serialization;
    using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

    [AoContract((int)N3MessageType.N3Teleport)]
    public class N3TeleportMessage : N3Message
    {
        #region Constructors and Destructors

        public N3TeleportMessage()
        {
            this.N3MessageType = N3MessageType.N3Teleport;
        }

        #endregion

        #region AoMember Properties

        [AoMember(0)]
        public Vector3 Destination { get; set; }

        [AoMember(1)]
        public Quaternion Heading { get; set; }

        /// <summary>
        /// Ninety seven, always, because the client writes it as a literal.
        /// </summary>
        /// <remarks>
        /// Read out of the client rather than inferred from captures.
        /// n3TeleportIIR_t::WriteSubClass is one of only five message writers
        /// N3.dll exports by name (RVA 0x29F1B), and the helper it calls for
        /// this field begins "push $0x61" - the constant is in the instruction
        /// stream, not in any data the client is working from.
        ///
        /// That settles what it is not. It is not a length, a count, a flag or
        /// anything derived from the teleport: it is a tag the client stamps on
        /// every one of these. What it means to the reader on the other side is
        /// still unknown, so this message is not green.
        ///
        /// The same disassembly gives the order the client writes: a Vector3, a
        /// Quaternion, then this byte with an Identity and an int32 beside it,
        /// then two more Identities, then an int32 length and that many raw
        /// bytes. The last two are what the comment below is reaching for, and
        /// they are the client-to-server shape only - six of the nine captured
        /// copies are server-to-client and carry a trailing coordinate triple
        /// instead. Two of those six leave our reader at a different offset
        /// again, so the server-to-client form needs its own source and cannot
        /// be appended to this one.
        /// </remarks>
        [AoMember(2)]
        public byte Unknown1 { get; set; }

        [AoMember(3)]
        public Identity Playfield { get; set; }

        [AoMember(4)]
        public int GameServerId { get; set; }

        [AoMember(5)]
        public int SgId { get; set; }

        [AoMember(6)]
        public Identity ChangePlayfield { get; set; }

        [AoMember(7)]
        public int Unknown4 { get; set; }

        [AoMember(8)]
        public int Unknown5 { get; set; }

        [AoMember(9)]
        public Identity Playfield2 { get; set; }

        /// <summary>
        /// A trailing block, behind its own byte count.
        /// </summary>
        /// <remarks>
        /// The int32 here was read as a field of its own and whatever followed
        /// it was dropped. It is a byte count: across all thirty three copies
        /// in every capture set it equals the number of bytes left in the
        /// message exactly, with no exceptions, and it is always a multiple of
        /// four. Twenty one of those are in the 2026-09-11 captures and the six
        /// that carry a zero count are precisely the six that used to pass the
        /// round trip.
        ///
        /// The counts seen are 0, 4, 8 and 12. What is inside varies with the
        /// count and is not settled: the twelve byte form is three floats that
        /// read as a position, the eight byte form is two zero int32s, and the
        /// four byte form is a single int32 of 1.
        /// </remarks>
        [AoMember(10, SerializeSize = ArraySizeType.Int32)]
        public byte[] Trailer { get; set; }
        #endregion
    }
}