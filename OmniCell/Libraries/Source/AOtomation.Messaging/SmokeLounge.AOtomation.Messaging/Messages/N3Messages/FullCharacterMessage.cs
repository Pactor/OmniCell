// --------------------------------------------------------------------------------------------------------------------
// <copyright file="FullCharacterMessage.cs" company="SmokeLounge">
//   Copyright © 2013 SmokeLounge.
//   This program is free software. It comes without any warranty, to
//   the extent permitted by applicable law. You can redistribute it
//   and/or modify it under the terms of the Do What The Fuck You Want
//   To Public License, Version 2, as published by Sam Hocevar. See
//   http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the FullCharacterMessage type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace SmokeLounge.AOtomation.Messaging.Messages.N3Messages
{
    using SmokeLounge.AOtomation.Messaging.GameData;
    using SmokeLounge.AOtomation.Messaging.Serialization;
    using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

    /// <summary>
    /// Everything about the character that is not a stat update: what they are
    /// carrying, what they have uploaded, what they know, and what is running
    /// on them. Sent once, when the character enters the world.
    /// </summary>
    /// <remarks>
    /// The reader is FullCharacterIIR_t's, at Gamecode 0x10073DCA, and it is a
    /// flat run of calls into typed sub-readers - so the shape of this message
    /// is the shape of that run. What each list is for is not guessed at: the
    /// dispatcher at 0x10073F78 hands every one of them to a subsystem hanging
    /// off the character object, and the exported N3Msg_ getters say what each
    /// of those subsystems is, because they read the same members back out.
    ///
    ///   character +0x1B8   the inventory - N3Msg_GetContainerInventoryList,
    ///                      N3Msg_GetItemProgress, N3Msg_DropItem
    ///   character +0x1BC   skills - N3Msg_GetSkill, N3Msg_SetSkillTmp,
    ///                      N3Msg_SecondarySpecialAttack, GetSKForXP
    ///   character +0x1C0   nano - N3Msg_GetNanoSpellList, GetFormulaProgress,
    ///                      N3Msg_CastNanoSpell, N3Msg_GetBuffTotalTime
    ///   character +0x1CC   an eight byte holder, reached only through its own
    ///                      lazy getter at 0x10058C70
    ///   character +0x1D8   pets - N3Msg_GetClientPetID, N3Msg_IsMyPetID
    ///   character +0x1E0   the team - N3Msg_GetTeamMemberList, N3Msg_IsInTeam,
    ///                      N3Msg_GetRaidTeamIndex, N3Msg_CreateRaid
    ///   0x10058FF8         perks and research - N3Msg_GetNumberOfUsedPerks,
    ///                      N3Msg_GlobalResearchGoals, N3Msg_PersonalResearchGoals
    ///
    /// The client has no writer for this message - FullCharacterIIR_t's write
    /// slot at 0x10073D37 is a bare return - so where a field's meaning rests
    /// on what the client would emit, it is the shared GameData writers that
    /// say it, not this class.
    ///
    /// Three things here are known to be wrong and cannot be shown wrong by the
    /// captures, because every captured copy leaves them empty. See
    /// <see cref="TeamFlags"/>, <see cref="Pets"/> and <see cref="Buffs"/>.
    /// </remarks>
    [AoContract((int)N3MessageType.FullCharacter)]
    public class FullCharacterMessage : N3Message
    {
        #region Constructors and Destructors

        public FullCharacterMessage()
        {
            this.N3MessageType = N3MessageType.FullCharacter;
            this.Unknown = 0x00;
        }

        #endregion

        #region AoMember Properties

        /// <summary>
        /// The message's version, 26, and the reader stops if it is anything
        /// else.
        /// </summary>
        /// <remarks>
        /// Read first at 0x10073DF1 and compared against the static at
        /// 0x101C11EC; a mismatch returns without touching the rest.
        /// </remarks>
        [AoMember(0)]
        public int MsgVersion { get; set; }

        /// <summary>
        /// Everything the character is carrying or wearing.
        /// </summary>
        /// <remarks>
        /// Read by the shared container reader at 0x1002A5DB and handed to the
        /// inventory subsystem at 0x10074125. Each entry is a placement, then
        /// the record the shared reader at 0x1002A20B reads: two int16s, an
        /// Identity, and a GameData::ACGItem_t.
        /// </remarks>
        [AoMember(1, SerializeSize = ArraySizeType.X3F1)]
        public InventorySlot[] InventorySlots { get; set; }

        /// <summary>
        /// The nano programs the character has uploaded.
        /// </summary>
        /// <remarks>
        /// The one list in this message whose name comes straight out of the
        /// client. The dispatcher appends it to the nano subsystem's +0x14 at
        /// 0x10074150, and N3Msg_GetNanoSpellList at 0x100174E7 does nothing
        /// but return that same +0x14 - as a list of int.
        /// </remarks>
        [AoMember(2, SerializeSize = ArraySizeType.X3F1)]
        public int[] UploadedNanoIds { get; set; }

        /// <summary>
        /// Three bytes each, and empty in all sixteen captured copies.
        /// </summary>
        /// <remarks>
        /// Read by 0x1002D20F, whose element reader at 0x1002D1A6 takes exactly
        /// three bytes. They go into the eight byte holder the lazy getter at
        /// 0x10058C70 makes at character +0x1CC, and nothing exported reads
        /// that member back, so what the three bytes are is still open. The
        /// shared character-block reader at 0x1002B458 reaches the same list
        /// under its type id 0x10, which is the only other place it appears.
        /// </remarks>
        [AoMember(3, SerializeSize = ArraySizeType.X3F1)]
        public FullCharacterSub[] Unknown2 { get; set; }

        /// <summary>
        /// The version of the list below, 1.
        /// </summary>
        /// <remarks>
        /// This and the two versions below are the same field three times over:
        /// the client has one reader for all three lists, 0x1003BDC0, and it
        /// opens by reading an int32 into a local it then never looks at. The
        /// matching writer at 0x1003BD7F pushes a literal 1 in that position
        /// before the count, which is what says it is a version and what says
        /// the value is 1. All sixteen captured copies carry 1.
        /// </remarks>
        [AoMember(4)]
        public int SkillEntriesVersion { get; set; }

        /// <summary>
        /// Entries belonging to the skill subsystem.
        /// </summary>
        /// <remarks>
        /// Read by 0x1003BDC0 - a plain int32 count, not the X3F1 form - and
        /// appended at 0x100741F6 to the vector at the front of the skill
        /// subsystem, character +0x1BC, which is the object N3Msg_GetSkill and
        /// N3Msg_SetSkillTmp work through.
        ///
        /// What one entry means is not settled. It is an Identity and two
        /// int32s, which is the shape N3Msg_GetActionProgress asks for - an
        /// Identity and two int pointers - but that call goes through a
        /// different member, so the match is suggestive and no more. Empty in
        /// all sixteen captured copies.
        /// </remarks>
        [AoMember(6, SerializeSize=ArraySizeType.Int32)]
        public FullCharacterSub2[] SkillEntries { get; set; }

        /// <summary>
        /// The version of the list below, 1. See
        /// <see cref="SkillEntriesVersion"/>.
        /// </summary>
        [AoMember(7)]
        public int PerkEntriesVersion { get; set; }

        /// <summary>
        /// Entries belonging to the perk and research subsystem.
        /// </summary>
        /// <remarks>
        /// The same reader and the same shape as <see cref="SkillEntries"/>,
        /// appended at 0x1007423D to +8 of the object 0x10058FF8 returns - the
        /// one N3Msg_GetNumberOfUsedPerks, N3Msg_GetPerkProgress and both
        /// research-goal getters work through. Empty in all sixteen captured
        /// copies.
        /// </remarks>
        [AoMember(8, SerializeSize = ArraySizeType.Int32)]
        public FullCharacterSub2[] PerkEntries { get; set; }

        
        /// <summary>
        /// The version of the list below, 1. See
        /// <see cref="SkillEntriesVersion"/>.
        /// </summary>
        [AoMember(9)]
        public int NanoEntriesVersion { get; set; }

        /// <summary>
        /// Entries belonging to the nano subsystem.
        /// </summary>
        /// <remarks>
        /// The same reader and shape again, appended at 0x1007426F to +0x40 of
        /// the nano subsystem at character +0x1C0. Empty in all sixteen
        /// captured copies.
        /// </remarks>
        [AoMember(10, SerializeSize = ArraySizeType.Int32)]
        public FullCharacterSub2[] NanoEntries { get; set; }


        /// <summary>
        /// Stats whose id and value both need a full int32.
        /// </summary>
        /// <remarks>
        /// All four stat lists end up in the same place. The dispatcher walks
        /// each one from 0x10073FC2, 0x10074015, 0x1007406B and 0x100740C2, and
        /// every entry goes through the same three calls on the character's
        /// stat table at character +0xE8: slot 0x48 asks whether the stat is
        /// already known, slot 0x44 adds it if not, and slot 0x40 sets it. An
        /// entry whose value is 1234567890 is skipped - that is the sentinel
        /// the shared stat readers seed an untold stat with.
        ///
        /// What separates the four is how wide the numbers are on the wire, not
        /// what they mean; this pair is read by 0x1002E7D9 as two int32s. Why
        /// the client sends two int32 lists rather than one is not answered by
        /// anything it does with them - the two loops are identical.
        ///
        /// 76 entries in every captured copy.
        /// </remarks>
        [AoMember(11, SerializeSize = ArraySizeType.X3F1)]
        public GameTuple<int, uint>[] Stats1 { get; set; }

        /// <summary>
        /// The second int32 stat list. See <see cref="Stats1"/>. 146 entries in
        /// every captured copy.
        /// </summary>
        [AoMember(12, SerializeSize = ArraySizeType.X3F1)]
        public GameTuple<int, uint>[] Stats2 { get; set; }

        /// <summary>
        /// Stats whose id and value both fit in a byte.
        /// </summary>
        /// <remarks>
        /// Read by 0x1002ED68, element reader 0x1002EC75: one byte for the id,
        /// one for the value, widened to int32 in memory. See
        /// <see cref="Stats1"/> for where they go.
        /// </remarks>
        [AoMember(13, SerializeSize = ArraySizeType.X3F1)]
        public GameTuple<byte, byte>[] Stats3 { get; set; }

        /// <summary>
        /// Stats whose id fits in a byte and whose value needs a signed int16.
        /// </summary>
        /// <remarks>
        /// Read by 0x1002E9FD, element reader 0x1002E905: a byte then an int16.
        /// The dispatcher sign-extends the int16 at 0x100740C9, so the value is
        /// signed. See <see cref="Stats1"/>.
        /// </remarks>
        [AoMember(14, SerializeSize = ArraySizeType.X3F1)]
        public GameTuple<byte, short>[] Stats4 { get; set; }

        /// <summary>
        /// A map of int to int handed to the skill subsystem.
        /// </summary>
        /// <remarks>
        /// Read by 0x10009E3C: a plain int32 count rather than the X3F1 form
        /// most of this message uses, then a key and a value for each entry.
        /// The dispatcher hands the finished map to the skill subsystem at
        /// character +0x1BC, at 0x10073FB7, before anything else is applied.
        ///
        /// Empty on every free account captured. The one subscribed account
        /// had four, holding 516, 540, 553 and 557 with a zero beside each -
        /// so the keys are ids of something and the values were all zero, which
        /// is not enough to say what either is.
        /// </remarks>
        [AoMember(15, SerializeSize = ArraySizeType.Int32)]
        public GameTuple<int, int>[] SkillMap { get; set; }

        /// <summary>
        /// Two bits: whether the character is in a team, and whether that team
        /// is part of a raid. Zero in all sixteen captured copies.
        /// </summary>
        /// <remarks>
        /// Read as an int32 at 0x10073E7B and split immediately: bit 0 into the
        /// message's +0x74 and bit 1 into +0x75. Nothing else in the int32 is
        /// looked at.
        ///
        /// This is the message's one conditional, and this model does not
        /// implement it. When bit 0 is set the reader goes on to take an
        /// Identity, and then, if bit 1 is also set, an int32 and six team
        /// objects; if bit 1 is clear, one team object and no int32. The
        /// dispatcher says what they are: at 0x10074166 the int32 is written to
        /// +0x38 of the team subsystem - the same subsystem
        /// N3Msg_GetRaidTeamIndex reads - and the six objects are pushed into
        /// it one at a time at 0x1007417C, which is what a raid is: six teams.
        /// The Identity goes in last, at 0x100741B4.
        ///
        /// That prediction came true on 2026-09-11. A session with two accounts
        /// teaming produced four FullCharacters that could not be read at all -
        /// they sat in the audit as an unreadable message id, because the block
        /// below was on the wire and this model stopped here. It is implemented
        /// now, and those four read out and round-trip.
        ///
        /// One limitation worth stating. The client extracts bits 0 and 1 and
        /// ignores the rest of the int32; the conditionals below key on the
        /// whole value being 1 or 3. That is exact for every value the client
        /// can distinguish - 0, 1, 2 and 3 all behave identically either way -
        /// but a server that set a bit the client ignores would be read
        /// differently by the two. Nothing has ever sent one.
        /// </remarks>
        [AoMember(16)]
        [AoFlags("team")]
        public int TeamFlags { get; set; }

        /// <summary>
        /// Which team, when <see cref="TeamFlags"/> says the character is in
        /// one.
        /// </summary>
        /// <remarks>
        /// Read at 0x10073E98 and put into the team subsystem last, at
        /// 0x100741B4.
        /// </remarks>
        [AoMember(17)]
        [AoUsesFlags("team", typeof(Identity), FlagsCriteria.EqualsToAny, new[] { 1, 3 })]
        public Identity? TeamIdentity { get; set; }

        /// <summary>
        /// Only on a raid: an int32 the dispatcher writes to + 0x38 of the team
        /// subsystem, which is the member N3Msg_GetRaidTeamIndex reads.
        /// </summary>
        /// <remarks>
        /// No capture contains one. The 2026-09-11 session teamed but did not
        /// raid, so the flags there are 1 and this field is absent.
        /// </remarks>
        [AoMember(18)]
        [AoUsesFlags("team", typeof(int), FlagsCriteria.EqualsToAny, new[] { 3 })]
        public int? RaidTeamIndex { get; set; }

        /// <summary>
        /// The team, when the character is in one and it is not a raid.
        /// </summary>
        /// <remarks>
        /// One X3F1-counted list of members, read by 0x101261AC. This is the
        /// branch the captured copies take.
        /// </remarks>
        [AoMember(19, SerializeSize = ArraySizeType.X3F1)]
        [AoUsesFlags("team", typeof(TeamMemberEntry[]), FlagsCriteria.EqualsToAny, new[] { 1 })]
        public TeamMemberEntry[] Team { get; set; }


        /// <summary>
        /// Raid team 0 of six. Only when <see cref="TeamFlags"/> is 3.
        /// </summary>
        /// <remarks>
        /// A raid is six teams: the reader loops 0x10073EB3 exactly six times
        /// and the dispatcher pushes each into the team subsystem at
        /// 0x1007417C. Six separate members rather than an array of arrays
        /// because the count is fixed in the client and each is its own X3F1
        /// list. No capture contains a raid.
        /// </remarks>
        [AoMember(20, SerializeSize = ArraySizeType.X3F1)]
        [AoUsesFlags("team", typeof(TeamMemberEntry[]), FlagsCriteria.EqualsToAny, new[] { 3 })]
        public TeamMemberEntry[] RaidTeam0 { get; set; }

        /// <summary>
        /// Raid team 1 of six. Only when <see cref="TeamFlags"/> is 3.
        /// </summary>
        /// <remarks>
        /// A raid is six teams: the reader loops 0x10073EB3 exactly six times
        /// and the dispatcher pushes each into the team subsystem at
        /// 0x1007417C. Six separate members rather than an array of arrays
        /// because the count is fixed in the client and each is its own X3F1
        /// list. No capture contains a raid.
        /// </remarks>
        [AoMember(21, SerializeSize = ArraySizeType.X3F1)]
        [AoUsesFlags("team", typeof(TeamMemberEntry[]), FlagsCriteria.EqualsToAny, new[] { 3 })]
        public TeamMemberEntry[] RaidTeam1 { get; set; }

        /// <summary>
        /// Raid team 2 of six. Only when <see cref="TeamFlags"/> is 3.
        /// </summary>
        /// <remarks>
        /// A raid is six teams: the reader loops 0x10073EB3 exactly six times
        /// and the dispatcher pushes each into the team subsystem at
        /// 0x1007417C. Six separate members rather than an array of arrays
        /// because the count is fixed in the client and each is its own X3F1
        /// list. No capture contains a raid.
        /// </remarks>
        [AoMember(22, SerializeSize = ArraySizeType.X3F1)]
        [AoUsesFlags("team", typeof(TeamMemberEntry[]), FlagsCriteria.EqualsToAny, new[] { 3 })]
        public TeamMemberEntry[] RaidTeam2 { get; set; }

        /// <summary>
        /// Raid team 3 of six. Only when <see cref="TeamFlags"/> is 3.
        /// </summary>
        /// <remarks>
        /// A raid is six teams: the reader loops 0x10073EB3 exactly six times
        /// and the dispatcher pushes each into the team subsystem at
        /// 0x1007417C. Six separate members rather than an array of arrays
        /// because the count is fixed in the client and each is its own X3F1
        /// list. No capture contains a raid.
        /// </remarks>
        [AoMember(23, SerializeSize = ArraySizeType.X3F1)]
        [AoUsesFlags("team", typeof(TeamMemberEntry[]), FlagsCriteria.EqualsToAny, new[] { 3 })]
        public TeamMemberEntry[] RaidTeam3 { get; set; }

        /// <summary>
        /// Raid team 4 of six. Only when <see cref="TeamFlags"/> is 3.
        /// </summary>
        /// <remarks>
        /// A raid is six teams: the reader loops 0x10073EB3 exactly six times
        /// and the dispatcher pushes each into the team subsystem at
        /// 0x1007417C. Six separate members rather than an array of arrays
        /// because the count is fixed in the client and each is its own X3F1
        /// list. No capture contains a raid.
        /// </remarks>
        [AoMember(24, SerializeSize = ArraySizeType.X3F1)]
        [AoUsesFlags("team", typeof(TeamMemberEntry[]), FlagsCriteria.EqualsToAny, new[] { 3 })]
        public TeamMemberEntry[] RaidTeam4 { get; set; }

        /// <summary>
        /// Raid team 5 of six. Only when <see cref="TeamFlags"/> is 3.
        /// </summary>
        /// <remarks>
        /// A raid is six teams: the reader loops 0x10073EB3 exactly six times
        /// and the dispatcher pushes each into the team subsystem at
        /// 0x1007417C. Six separate members rather than an array of arrays
        /// because the count is fixed in the client and each is its own X3F1
        /// list. No capture contains a raid.
        /// </remarks>
        [AoMember(25, SerializeSize = ArraySizeType.X3F1)]
        [AoUsesFlags("team", typeof(TeamMemberEntry[]), FlagsCriteria.EqualsToAny, new[] { 3 })]
        public TeamMemberEntry[] RaidTeam5 { get; set; }

        /// <summary>
        /// The character's pets.
        /// </summary>
        /// <remarks>
        /// The reader at 0x1002BA77 takes an X3F1 count and then one Identity
        /// per entry - eight bytes, not the sixteen this model reads - and the
        /// dispatcher walks them into the pet subsystem at character +0x1D8 at
        /// 0x100742B0, which is the member N3Msg_GetClientPetID and
        /// N3Msg_IsMyPetID read back.
        ///
        /// The element type was wrong here until 2026-09-11, and no capture
        /// could show it: every captured copy has an empty list, and an empty
        /// list of the wrong type is byte for byte an empty list of the right
        /// one.
        /// </remarks>
        [AoMember(30, SerializeSize = ArraySizeType.X3F1)]
        public Identity[] Pets { get; set; }

        /// <summary>
        /// What is currently running on the character.
        /// </summary>
        /// <remarks>
        /// The reader at 0x100A71AE takes an X3F1 count, refuses more than 999
        /// entries, and reads each one with GameData's own
        /// operator&gt;&gt;(BinaryStream&amp;, SpellData_t&amp;) - so an entry
        /// is a whole spell record, not four int32s. The dispatcher hands the
        /// list to the character's stat table at 0x100742FE.
        ///
        /// Wrong for the same reason, and invisible for the same reason, as
        /// <see cref="Pets"/> was, and fixed a few hours later once it turned
        /// out the type already existed. GameData::SpellData_t is
        /// <see cref="NanoEffect"/>: ApplySpells, SpellList and CorpseFullUpdate
        /// have carried lists of them all along, and the client's own format table
        /// is already read - the thing that decides how
        /// many arguments an effect carries - straight out of GameData.dll.
        ///
        /// What was missing was only the plumbing. Those three messages reach
        /// NanoEffects.Read from hand-written serializers for the whole
        /// message, so the type was unreachable from a message described by
        /// attributes, and writing a custom serializer for a forty member
        /// message to get at it would have been absurd. NanoEffectSerializer
        /// registers the type instead, and the array serializer resolves its
        /// element through the same registry, so this member is now an X3F1
        /// counted list of the right thing.
        ///
        /// Empty in all 99 captured copies either way.
        /// </remarks>
        [AoMember(31, SerializeSize = ArraySizeType.X3F1)]
        public NanoEffect[] Buffs { get; set; }

        /// <summary>
        /// The character's research and perk progress.
        /// </summary>
        /// <remarks>
        /// Read by 0x10053CE9, which refuses more than a thousand entries, and
        /// handed to the perk and research subsystem at 0x100742CB - the object
        /// 0x10058FF8 returns, which is where N3Msg_PersonalResearchGoals and
        /// N3Msg_GlobalResearchGoals read from.
        ///
        /// An entry is an int32 and then a record read by 0x10052D9D, and that
        /// record has two forms, both of them modelled since 2026-09-11. The
        /// first int32 the record reads is masked with 0xFFFFFF00. If every one
        /// of those bits is set it is a marker whose low byte is a form tag,
        /// and two int32s follow - four in the entry all told, which is the
        /// only form any capture holds and what this class used to read
        /// unconditionally. If any of them is clear it is an id, and three
        /// int32s follow through 0x1002BC4D - five in the entry. A plain entry
        /// would have left one int32 unread and turned every entry after it
        /// into nonsense.
        ///
        /// Those three are read and thrown away. Both forms converge on
        /// 0x10052E1A, which looks the id up in the research table and copies
        /// the seven word definition over the top of the slot with a rep movsd
        /// at 0x10052E3E, so nothing can read them back.
        ///
        /// The client keeps an entry only when the id ahead of the record
        /// matches the id inside it, which is why the captures show the same
        /// number twice.
        ///
        /// Empty on 78 of the 99 captured copies. The rest carry 1, 3, 4, 140
        /// or 147 entries, with ids 160, 161, 330 and 331 among them, and every
        /// single one is the marker form 0xFFFFFF02 with a value of zero.
        ///
        /// A perk lands here, and the captures of 2026-09-11 caught one
        /// arriving. Twenty four copies of this message across three sessions
        /// of one character: fifteen while it was levelling from 1 to 6, all
        /// empty; four during the session in which it reached level 10 and
        /// spent its first perk point, all still empty; and five on the next
        /// login, every one of them carrying a single entry with Id 160,
        /// Marker 0xFFFFFF02, IdRepeated 160 and Value 0.
        ///
        /// Two things follow. The perk does not reach the client at the moment
        /// it is bought - it appears on the next FullCharacter, which is why
        /// nothing changed in the session that bought it. And there is no
        /// PerkUpdate involved: that message does not appear once in the
        /// 215,000 packets of those three captures, which include two logins
        /// and the purchase itself, so whatever PerkUpdate is for, this is not
        /// it.
        ///
        /// The name on this field is the one it came with. It is at least
        /// partly wrong - the subsystem it feeds is the one
        /// N3Msg_GetNumberOfUsedPerks counts through, at 0x10058FF8 - but what
        /// the 147 entries on the subscribed account were is unknown, so
        /// renaming it on one perk would be trading one wrong name for
        /// another.
        /// </remarks>
        [AoMember(32, SerializeSize = ArraySizeType.X3F1)]
        public FullCharacterEntry[] ResearchGoals { get; set; }


        #endregion
    }
}