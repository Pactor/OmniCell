// --------------------------------------------------------------------------------------------------------------------
// <copyright file="InfoPacketFlags.cs" company="OmniCell">
//   Copyright (c) 2026 OmniCell contributors.
//   Added to SmokeLounge.AOtomation.Messaging, which is distributed under the
//   Do What The Fuck You Want To Public License, Version 2, as published by
//   Sam Hocevar. See http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the InfoPacketFlags type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace SmokeLounge.AOtomation.Messaging.Messages.N3Messages
{
    using System;

    /// <summary>
    /// The first byte of an examine window, which says which optional blocks
    /// the rest of it carries.
    /// </summary>
    /// <remarks>
    /// This replaced an enum called InfoPacketType that listed seven whole
    /// values - 0x40, 0x41, 0x43, 0x47, 0x50, 0x54 and 0x5C - and picked one of
    /// three record shapes from them. The client does no such thing. It tests
    /// the bits one at a time, at 0x10045F6C, and any combination of them is a
    /// legal message. A value that was not in the list of seven selected no
    /// shape at all, and the body would not have been read.
    ///
    /// That is not a hypothetical. It is what FullCharacter did with its team
    /// block and what Mail did with its whole message, and both were found on
    /// 2026-09-11 by a capture carrying a combination nobody had caught before.
    /// </remarks>
    [Flags]
    public enum InfoPacketFlags : byte
    {
        None = 0x00,

        /// <summary>
        /// The target is in an organization: an extra counted string for the
        /// rank, and after it - and only after it - the city playfield id.
        /// </summary>
        /// <remarks>
        /// The int32 belongs to this bit rather than to
        /// <see cref="OrganizationCities"/>. The jump for bit 0 clears the
        /// whole block including it; the jump for bit 1 skips only the list and
        /// lands on it.
        /// </remarks>
        Organization = 0x01,

        /// <summary>
        /// The organization owns city land: an X3F1 counted list of
        /// <see cref="GameData.GridDestination"/>. Read only inside
        /// <see cref="Organization"/>.
        /// </summary>
        OrganizationCities = 0x02,

        /// <summary>
        /// An X3F1 counted list of <see cref="GameData.AcgItem"/>, read at
        /// 0x100468E1 by GameData's own operator for ACGItem_t and handed back
        /// one entry at a time by N3Msg_GetInfoPacketACGItemData. Called a
        /// tower list until 2026-09-12, from the inherited model.
        /// </summary>
        HasAcgItems = 0x04,

        /// <summary>
        /// A control tower's suppression: an int32 and a byte.
        /// </summary>
        Suppression = 0x08,

        /// <summary>
        /// Set on everything that is not a player, and it suppresses rather
        /// than selects: the test at 0x1004618C jumps past the eight PvP int32s
        /// when the bit is set.
        /// </summary>
        /// <remarks>
        /// Named for what carries it rather than for what it does. Every
        /// captured copy with this bit is a monster or a tower, and every
        /// captured copy without it is a player - and only a player has PvP
        /// numbers to send.
        /// </remarks>
        NotAPlayer = 0x10,

        /// <summary>
        /// The twelve faction standings. Never captured; see
        /// <see cref="GameData.FactionStandings"/>, which the reader names by
        /// filing them into stats 561 to 572.
        /// </summary>
        HasFactionStandings = 0x20,

        /// <summary>
        /// The record carries a version byte. Set in all 477 copies on disk.
        /// </summary>
        /// <remarks>
        /// When it is clear the version is taken as zero, and a zero version
        /// makes the client read one more byte at 0x10045FB4 and drop it. The
        /// two are sides of the same condition: the version can only be zero
        /// when this bit is clear.
        /// </remarks>
        Versioned = 0x40
    }
}
