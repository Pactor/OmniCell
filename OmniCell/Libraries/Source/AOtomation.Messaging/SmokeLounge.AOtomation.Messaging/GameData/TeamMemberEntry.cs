// --------------------------------------------------------------------------------------------------------------------
// <copyright file="TeamMemberEntry.cs" company="OmniCell">
//   Copyright (c) 2026 OmniCell contributors.
//   Added to SmokeLounge.AOtomation.Messaging, which is distributed under the
//   Do What The Fuck You Want To Public License, Version 2, as published by
//   Sam Hocevar. See http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the TeamMemberEntry type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace SmokeLounge.AOtomation.Messaging.GameData
{
    using SmokeLounge.AOtomation.Messaging.Serialization;
    using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

    /// <summary>
    /// One member of the team a character is in, as FullCharacter carries it.
    /// </summary>
    /// <remarks>
    /// Read by Gamecode 0x1012634F into a 0x30-byte record. The wire order is
    /// the order below: an Identity into + 0x1C, a counted string into + 0, an
    /// int32 into + 0x24, a byte into + 0x28, an int16 into + 0x2A and a second
    /// int16 widened into + 0x2C.
    ///
    /// This record had never been seen until 2026-09-11. FullCharacter's team
    /// block is gated on a flag that is zero unless the character is actually in
    /// a team, and no capture had one - so four FullCharacters from that day's
    /// two-account session could not be read at all.
    /// </remarks>
    public class TeamMemberEntry
    {
        /// <summary>
        /// Who the member is.
        /// </summary>
        [AoMember(0)]
        public Identity Identity { get; set; }

        /// <summary>
        /// Their name.
        /// </summary>
        /// <remarks>
        /// The shared counted-string helper at 0x10038AF8: an int16 length and
        /// that many bytes, no terminator.
        /// </remarks>
        [AoMember(1, SerializeSize = ArraySizeType.Int16)]
        public string Name { get; set; }

        /// <summary>
        /// The organisation the member belongs to.
        /// </summary>
        /// <remarks>
        /// The client names it. Every entry the reader at 0x1012634F finishes
        /// goes through the validator at 0x101262E6, and when a record fails
        /// that check the client logs it with the format string at 0x101712E0:
        ///
        ///     ERROR: invalid team entry:
        ///     ERROR: - player       "%s" (%u:%u)
        ///     ERROR: - level        %u
        ///     ERROR: - side         %u
        ///     ERROR: - organisation %u
        ///
        /// The six arguments are pushed at 0x1012632B onward in reverse, so
        /// they are the name, the two words of the Identity, the int16 at
        /// + 0x2A, the byte at + 0x28 and this int32 at + 0x24 - which puts
        /// this one under "organisation" and <see cref="Side"/> under "side".
        ///
        /// The validator agrees with the names: it wants the Identity's type
        /// to be 0xC350, the side no greater than 7, the level below 1000 and
        /// the profession below 16.
        /// </remarks>
        [AoMember(2)]
        public int OrganizationId { get; set; }

        /// <summary>
        /// Which side the member is on - stat 33. Zero to seven.
        /// </summary>
        /// <remarks>
        /// The byte at + 0x28. Named by the same error string as
        /// <see cref="OrganizationId"/>, and bounded by the same validator:
        /// 0x101262FC rejects the whole entry when it is above 7.
        /// </remarks>
        [AoMember(3)]
        public byte Side { get; set; }

        /// <summary>
        /// The member's level.
        /// </summary>
        /// <remarks>
        /// Measured, from the session this record was first captured in. Two
        /// characters levelling together over four snapshots, and this field
        /// holds 5, 6 and 7 - it rises as they level, and it differs between
        /// the two at the same moment. Nothing else in a team member entry
        /// behaves like that.
        /// </remarks>
        [AoMember(4)]
        public short Level { get; set; }

        /// <summary>
        /// The member's profession. See <see cref="GameData.Profession"/>.
        /// </summary>
        /// <remarks>
        /// 14 for one member of the captured team and 8 for the other, constant
        /// across every snapshot while <see cref="Level"/> moved. In this
        /// model's own Profession enum 14 is Keeper and 8 is Bureaucrat, and the
        /// account the capture was taken on is a Keeper - which is the check
        /// that makes this a reading rather than a guess at a small number.
        ///
        /// Left as a short rather than typed to the enum: the wire field is an
        /// int16 and the enum is int-backed, and nothing is gained by forcing
        /// the two together here.
        /// </remarks>
        [AoMember(5)]
        public short Profession { get; set; }
    }
}
