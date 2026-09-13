// --------------------------------------------------------------------------------------------------------------------
// <copyright file="InfoPacket.cs" company="SmokeLounge">
//   Copyright © 2013 SmokeLounge.
//   This program is free software. It comes without any warranty, to
//   the extent permitted by applicable law. You can redistribute it
//   and/or modify it under the terms of the Do What The Fuck You Want
//   To Public License, Version 2, as published by Sam Hocevar. See
//   http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the InfoPacket type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace SmokeLounge.AOtomation.Messaging.GameData
{
    using SmokeLounge.AOtomation.Messaging.Messages.N3Messages;
    using SmokeLounge.AOtomation.Messaging.Serialization;
    using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

    /// <summary>
    /// The contents of an examine window.
    /// </summary>
    /// <remarks>
    /// One record with optional blocks, which is what the client has. Until
    /// 2026-09-11 this was an abstract class with three subclasses -
    /// CharacterInfoPacket, MonsterInfoPacket and TowerInfoPacket - chosen by
    /// matching the flags byte against a list of seven whole values. The client
    /// has one reader, at Gamecode 0x10045F6C, and one structure, and it tests
    /// the flags byte a bit at a time. See <see cref="InfoPacketFlags"/> for
    /// why the difference matters.
    ///
    /// The reader in full, in order, with the record offsets it fills:
    ///
    ///   byte    the flags, kept on the message rather than here
    ///   byte    +1     the version, only when flags has 0x40, else zero
    ///   byte    +0x32, +0x33, +0x34
    ///   byte    read and dropped, only when the version is zero
    ///   byte    +0x35
    ///   int16   +0x30
    ///   int32   +0x20, +0x24, +0x28, +0x2C
    ///   string  +0x48, +0x64, +0x80, +4
    ///   0x01:   a string to +0x9C
    ///           0x02: an X3F1 list through 0x1012916A to +0xB8
    ///           an int32 to +0x38
    ///   0x04:   an X3F1 list through 0x10046894 to +0xCC
    ///   0x08:   an int32 to +0xD8 and a byte to +0xDC
    ///   0x20:   twelve int32s to +0xE0 through +0x10C
    ///   int32   +0x3C, +0x40, +0x44
    ///   0x10 CLEAR: eight int32s to +0x110 through +0x12C
    ///
    /// Two of those the old model had in the wrong place, and both were real.
    /// <see cref="OrganizationId"/> was conditional and is not - the fourth
    /// unconditional int32 is it - and <see cref="CityPlayfieldId"/> was
    /// unconditional and is not. The two errors cancelled in the byte count,
    /// which is why every captured copy still round tripped: a plain character
    /// read three int32s where the client reads four, cut the four counted
    /// strings four bytes early, and came out level again at the end. It worked
    /// only while a plain character's organization id was zero and its four
    /// strings were empty, which is true of all 47 captured copies and is not a
    /// property of the protocol.
    ///
    /// The whole reader was checked against every copy on disk after the
    /// rewrite: 471 from the captures and the six in TestData, 477 in all, and
    /// the walk ends exactly on the last byte of every one.
    /// </remarks>
    public class InfoPacket
    {
        #region AoMember Properties

        /// <summary>
        /// The record's version, read only when the flags byte carries 0x40.
        /// </summary>
        /// <remarks>
        /// 1 in every captured copy. Zero - which can only happen when the bit
        /// is clear - makes the client read one more byte at 0x10045FB4 and
        /// throw it away; that is <see cref="UnversionedPad"/>.
        /// </remarks>
        [AoMember(0)]
        [AoFlags("infoversion")]
        [AoUsesFlags("flags", typeof(byte), FlagsCriteria.HasAll, (int)InfoPacketFlags.Versioned)]
        public byte Version { get; set; }

        /// <summary>
        /// The target's profession. See <see cref="GameData.Profession"/>.
        /// </summary>
        /// <remarks>
        /// A byte rather than the enum because the enum is int backed and a
        /// monster's value here is 0, which is nobody's profession.
        /// </remarks>
        [AoMember(1)]
        public byte Profession { get; set; }

        [AoMember(2)]
        public byte Level { get; set; }

        [AoMember(3)]
        public byte TitleLevel { get; set; }

        /// <summary>
        /// One byte the client reads and drops, when the version is zero.
        /// </summary>
        /// <remarks>
        /// Never on the wire in anything captured, because nothing has ever
        /// sent a record without 0x40. Modelled rather than assumed away: the
        /// read is unconditional inside that branch, so a record without a
        /// version is a byte longer than one with a version of zero would be.
        /// </remarks>
        [AoMember(4)]
        [AoUsesFlags("infoversion", typeof(byte), FlagsCriteria.EqualsToAny, 0)]
        public byte? UnversionedPad { get; set; }

        [AoMember(5)]
        public byte VisualProfession { get; set; }

        /// <summary>
        /// The int16 at the record's +0x30.
        /// </summary>
        /// <remarks>
        /// Side experience for a player. What it means for a monster is not
        /// settled; 29281, 5651, 340 and 500 all appear.
        /// </remarks>
        [AoMember(6)]
        public short SideXp { get; set; }

        [AoMember(7)]
        public int Health { get; set; }

        [AoMember(8)]
        public int MaxHealth { get; set; }

        /// <summary>
        /// Zero in every captured copy, player or not.
        /// </summary>
        [AoMember(9)]
        public int BreedHostility { get; set; }

        /// <summary>
        /// The organization, or zero.
        /// </summary>
        /// <remarks>
        /// The fourth of four unconditional int32s, and unconditional is the
        /// point: it is present on a character with no organization, carrying
        /// zero, and the old model left it out there. Logan Messamore's copy
        /// has 9988 here and "Unit Commander" in
        /// <see cref="OrganizationRank"/>.
        /// </remarks>
        [AoMember(10)]
        public int OrganizationId { get; set; }

        /// <summary>
        /// The target's first name - the record's + 0x48.
        /// </summary>
        /// <remarks>
        /// The reader hands it to the name-slot setter at 0x1005BB92 with slot
        /// 1, at 0x100462FB. That is the same setter SetName drives, and slot 1
        /// is its first name.
        /// </remarks>
        [AoMember(11, SerializeSize = ArraySizeType.Int16)]
        public string FirstName { get; set; }

        /// <summary>
        /// The target's last name - the record's + 0x64, into name slot 2 at
        /// 0x10046336.
        /// </summary>
        [AoMember(12, SerializeSize = ArraySizeType.Int16)]
        public string LastName { get; set; }

        /// <summary>
        /// The record's + 0x80, into name slot 4 at 0x10046382.
        /// </summary>
        /// <remarks>
        /// Slot 4 is the auxiliary heap-backed dynel name - SetName's page has
        /// the four slots and what each one is. Empty in all 532 captured
        /// copies, and called LegacyTitle until 2026-09-12 on the inherited
        /// model's word; the slot is what is actually established.
        /// </remarks>
        [AoMember(13, SerializeSize = ArraySizeType.Int16)]
        public string AuxiliaryName { get; set; }

        /// <summary>
        /// A fourth counted string, empty in all 477 copies.
        /// </summary>
        /// <remarks>
        /// Not a name, which is why it sits at the front of the record at + 4
        /// rather than beside the three at + 0x48, + 0x64 and + 0x80: it is the
        /// examine window's free text, and the client appends its own lines to
        /// whatever the server sent. The suppression line is the one that can
        /// be read whole - 0x1004651D appends the localised TimeUntilGasChanges
        /// text, with the gas percentage and the hours and minutes left, to
        /// exactly this string.
        ///
        /// Empty in all 477 captured copies, because none of them is a target
        /// with anything to add.
        /// </remarks>
        [AoMember(14, SerializeSize = ArraySizeType.Int16)]
        public string DisplayText { get; set; }

        /// <summary>
        /// The target's rank in its organization.
        /// </summary>
        /// <summary>
        /// The member's rank in the organisation, as text.
        /// </summary>
        /// <remarks>
        /// The record's + 0x9C. N3Msg_GetClanLevelString at Gamecode 0x10018EED
        /// resolves the examined dynel, takes its InfoPacket at + 0x7C and
        /// returns this string - the client's own getter for this one member.
        /// The single captured copy that has a rank at all carries Unit
        /// Commander.
        /// </remarks>
        [AoMember(15, SerializeSize = ArraySizeType.Int16)]
        [AoUsesFlags("flags", typeof(string), FlagsCriteria.HasAll, (int)InfoPacketFlags.Organization)]
        public string OrganizationRank { get; set; }

        /// <summary>
        /// The city land the organization holds.
        /// </summary>
        /// <remarks>
        /// Needs both bits, not just 0x02: the client reads this inside the
        /// 0x01 branch, so 0x02 on its own is a bit that does nothing.
        /// </remarks>
        [AoMember(16, SerializeSize = ArraySizeType.X3F1)]
        [AoUsesFlags("flags", typeof(GridDestination[]), FlagsCriteria.HasAll,
            (int)InfoPacketFlags.Organization, (int)InfoPacketFlags.OrganizationCities)]
        public GridDestination[] GridDestinations { get; set; }

        /// <summary>
        /// The playfield the organization's city is in.
        /// </summary>
        /// <remarks>
        /// Part of the 0x01 branch, not unconditional. 6010 in the one captured
        /// copy that has an organization with land.
        /// </remarks>
        [AoMember(17)]
        [AoUsesFlags("flags", typeof(int), FlagsCriteria.HasAll, (int)InfoPacketFlags.Organization)]
        public int? CityPlayfieldId { get; set; }

        /// <summary>
        /// Towers: a character's own, or the one an examined tower is.
        /// </summary>
        [AoMember(18, SerializeSize = ArraySizeType.X3F1)]
        [AoUsesFlags("flags", typeof(AcgItem[]), FlagsCriteria.HasAll, (int)InfoPacketFlags.HasAcgItems)]
        public AcgItem[] AcgItems { get; set; }

        /// <summary>
        /// Time until the suppression level changes.
        /// </summary>
        /// <remarks>
        /// Named from a comment inherited from CellAO on
        /// CharacterInfoPacketMessageHandler, which is the only reading of this
        /// pair there has ever been and which has been right about every other
        /// bit in the flags byte. Only the control tower shape carries it.
        /// </remarks>
        [AoMember(19)]
        [AoUsesFlags("flags", typeof(int), FlagsCriteria.HasAll, (int)InfoPacketFlags.Suppression)]
        public int? SuppressionTimer { get; set; }

        /// <summary>
        /// The suppression level. See <see cref="SuppressionTimer"/>.
        /// </summary>
        [AoMember(20)]
        [AoUsesFlags("flags", typeof(byte), FlagsCriteria.HasAll, (int)InfoPacketFlags.Suppression)]
        public byte? SuppressionLevel { get; set; }

        /// <summary>
        /// Twelve int32s nothing has ever sent. See
        /// <see cref="FactionStandings"/>.
        /// </summary>
        [AoMember(21)]
        [AoUsesFlags("flags", typeof(FactionStandings), FlagsCriteria.HasAll,
            (int)InfoPacketFlags.HasFactionStandings)]
        public FactionStandings FactionStandings { get; set; }

        /// <summary>
        /// The first of the three unconditional int32s at 0x10046174.
        /// </summary>
        /// <remarks>
        /// A monster carries 1234567890 in all three, which is the protocol's
        /// unset marker. The one captured character in an organization has
        /// 15500, 0 and 30, which is what makes these the Alien Invasion
        /// numbers rather than something a monster also has.
        /// </remarks>
        [AoMember(22)]
        public int InvadersKilled { get; set; }

        /// <summary>
        /// See <see cref="InvadersKilled"/>.
        /// </summary>
        [AoMember(23)]
        public int KilledByInvaders { get; set; }

        /// <summary>
        /// See <see cref="InvadersKilled"/>.
        /// </summary>
        [AoMember(24)]
        public int AiLevel { get; set; }

        [AoMember(25)]
        [AoUsesFlags("flags", typeof(int), FlagsCriteria.HasNone, (int)InfoPacketFlags.NotAPlayer)]
        public int? PvpDuelKills { get; set; }

        [AoMember(26)]
        [AoUsesFlags("flags", typeof(int), FlagsCriteria.HasNone, (int)InfoPacketFlags.NotAPlayer)]
        public int? PvpDuelDeaths { get; set; }

        [AoMember(27)]
        [AoUsesFlags("flags", typeof(int), FlagsCriteria.HasNone, (int)InfoPacketFlags.NotAPlayer)]
        public int? PvpProfessionDuelKills { get; set; }

        [AoMember(28)]
        [AoUsesFlags("flags", typeof(int), FlagsCriteria.HasNone, (int)InfoPacketFlags.NotAPlayer)]
        public int? PvpRankedSoloKills { get; set; }

        [AoMember(29)]
        [AoUsesFlags("flags", typeof(int), FlagsCriteria.HasNone, (int)InfoPacketFlags.NotAPlayer)]
        public int? PvpRankedTeamKills { get; set; }

        [AoMember(30)]
        [AoUsesFlags("flags", typeof(int), FlagsCriteria.HasNone, (int)InfoPacketFlags.NotAPlayer)]
        public int? PvpSoloScore { get; set; }

        [AoMember(31)]
        [AoUsesFlags("flags", typeof(int), FlagsCriteria.HasNone, (int)InfoPacketFlags.NotAPlayer)]
        public int? PvpTeamScore { get; set; }

        [AoMember(32)]
        [AoUsesFlags("flags", typeof(int), FlagsCriteria.HasNone, (int)InfoPacketFlags.NotAPlayer)]
        public int? PvpDuelScore { get; set; }

        #endregion
    }
}
