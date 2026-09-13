using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SmokeLounge.AOtomation.Messaging.GameData
{
    using SmokeLounge.AOtomation.Messaging.Serialization;
    using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;
    
    /// <summary>
    /// One quest in a QuestFullUpdate.
    /// </summary>
    /// <remarks>
    /// <b>The shape of this record is confirmed.</b> This remark used to open by
    /// saying the model was not right - that three fields held values which
    /// decode as text, so a boundary must be misplaced and the numbers after it
    /// must be string content. That was wrong, and the test it asked for is the
    /// thing that settled it.
    ///
    /// A standalone decoder reads the message with the client's
    /// reader written out as code - 0x100ABEA7 for the quest, 0x100870A8 for
    /// the reward box, 0x100ACBA0 for one action - and knows nothing about this
    /// class. Run over every QuestFullUpdate in the captures, all 32 of them
    /// consume to the byte: the last field of the last quest ends exactly where
    /// the message does. A round trip cannot show that, because a reader and a
    /// writer that are wrong in the same place agree with each other.
    ///
    /// It also found the one real error in the trace: the version 15 block's
    /// count is an X3F1 sentinel, where the version 13 block four fields
    /// earlier uses a plain int32.
    ///
    /// And the four-character values are field values, not fragments of a
    /// string read at the wrong offset. They land exactly on boundaries of the
    /// client's own shape: in the one 666 byte capture the first is the reward
    /// box's version 4 field and the second is its version 5 field, which are
    /// <see cref="QuestCode"/> and <see cref="UnknownHash"/> here. Across the
    /// captures there are dozens of them - OTGD, QSOR, 2OGZ, 3JRQ, R2GL, ALHA,
    /// LE01, HATC, VRTR, ZHS5 - all in the uppercase-and-digit alphabet, which
    /// is a population, not a coincidence. GameData exports ACGHash_t with a
    /// constructor from an unsigned int, GetHashAsInt and GetHashAsText, so a
    /// hash is exactly this: an int32 with a four-character text form. None of
    /// the codes is in the resource database, in either byte order, which fits
    /// a hash generated for a mission rather than one stored in the data.
    ///
    /// So what is left on this record is meaning, not shape.
    ///
    /// The client reads a quest at Gamecode 0x100ABEA7 and the record is
    /// version gated. The first int32 is the version and the reader refuses
    /// anything outside 7 to 15; every captured copy is 15, so every optional
    /// block is present and none of them can be told apart by the captures
    /// alone. The gates, in order:
    ///
    ///   always      version, an int32 the reader drops, an int32, an int32,
    ///               a NUL terminated string, an int32 length and that many
    ///               bytes of description, two identities with something
    ///               between them, two int32s, and three counted lists
    ///   &gt;= 8       an int32; below 8 the client uses 6 instead
    ///   &gt;= 9       a sub-record
    ///   &gt;= 10      an int32
    ///   &gt;= 11      an int32
    ///   &gt;= 12      an identity and two int32s
    ///   &gt;= 13      a counted list of identity and int32 pairs
    ///   &gt;= 14      an int32
    ///   &gt;= 15      a sub-record read at 0x100ABDE6
    ///
    /// Rewriting it against that is the job, and it wants a decoder written
    /// from the trace and run over every captured copy to check that each one
    /// consumes to the byte - the same way InfoPacket's four counted strings
    /// were found. A round trip cannot catch a boundary in the wrong place;
    /// consuming to the byte against an independent trace can.
    ///
    /// Two things came out of the reader at Gamecode 0x100ABEA7 on 2026-09-11
    /// and are written down here so the rewrite starts from them.
    ///
    /// The whole reader, in order, taken instruction by instruction on
    /// 2026-09-12 so the rewrite has a trace to check itself against rather
    /// than a summary. Destinations are the quest object's offsets; "local"
    /// means the reader keeps the value on its own stack.
    ///
    ///   i32                     local     the version, refused outside 7 to 15
    ///   i32                     local     read and dropped
    ///   i32                     local     read and dropped
    ///   i32                     + 0xA4    the flags
    ///   NUL-terminated string   + 0x08    the name - N3Msg_GetName
    ///   i32 length, then that many bytes
    ///                           + 0x88    the description - N3Msg_GetDesc.
    ///                                     The length is refused above 0xFFF
    ///   Identity                + 0x38
    ///   RewardBox_t             + 0x34    0x100870A8, laid out below
    ///   Identity                + 0x40
    ///   i32                     + 0x48    the mission icon - N3Msg_GetQuestIcon
    ///   i32                     + 0x70
    ///   i32                     + 0x74
    ///   X3F1 list of 0x8C byte actions
    ///                           + 0x28    0x100ACCDF, entry 0x100ACAFC -
    ///                                     N3Msg_GetQuestActionList
    ///   X3F1 Identity list                0x1002BA77
    ///   i32 count, then that many i32s, each masked with 0x07FFFFFF
    ///                           + 0x60
    ///   i32 count, then that many i32s, each masked with 0x07FFFFFF
    ///                           + 0x50
    ///   i32 count, then that many (Identity, i32 length, that many bytes)
    ///
    ///   v &gt;= 8   i32            + 0xA8    below 8 the client writes 6 instead
    ///   v &gt;= 9   X3F1 Identity list
    ///   v &gt;= 10  i32            + 0x4C
    ///   v &gt;= 11  i32            + 0xB0
    ///   v &gt;= 12  Identity       + 0xB4, then i32 + 0xBC and i32 + 0xC0.
    ///                                     Below 12 all four are zeroed
    ///   v &gt;= 13  i32 count, then that many (Identity, i32) into the map at
    ///                           + 0xC8
    ///   v &gt;= 14  i32            + 0xD4
    ///   v &gt;= 15  i32 count, then that many (i32, i32) into the list at
    ///                           + 0xD8, through 0x100ABDE6
    ///
    /// And RewardBox_t, at 0x100870A8, which has a version of its own:
    ///
    ///   i32 version
    ///   i32                     box + 0   the cash - N3Msg_QuestGetCashReward
    ///   i32                     box + 4   the experience - N3Msg_QuestGetXPReward
    ///   i32
    ///   X3F1 Identity list
    ///   X3F1 Identity list
    ///   X3F1 ACGItem_t list               0x10046894, the same list reader
    ///                                     InfoPacket's ACG items use
    ///   v &gt;= 4   i32, i32, i32
    ///   v &gt;= 5   i32, i32
    ///   v &gt;= 6   a single ACGItem_t
    ///
    /// Two things to know before using it. The counts on the three inner lists
    /// in the middle are plain int32s, not X3F1 - only the ones marked X3F1
    /// divide by 0x3F1. And the masking on the two int32 lists is done after
    /// the read, so the top five bits are on the wire and thrown away.
    ///
    /// The first is that <b>this record is version gated and this class is
    /// not</b>. The reader refuses a version outside 7 to 15 and then reads a
    /// different set of fields for each: at 8 an int32 into +0xA8, which is
    /// forced to 6 below that; at 9 an X3F1 identity list; at 10 an int32 into
    /// +0x4C; at 11 one into +0xB0; at 12 an Identity into +0xB4 and two int32s
    /// after it, all four zeroed below that; at 13 a further block; at 14 an
    /// int32 into +0xD4; at 15 a sub-object into +0xD8. This class reads every
    /// one of them unconditionally, which is right for exactly one version -
    /// and that is the one the live server sends. All 135 records across every
    /// capture carry 15. Anything older would be misread from the first gate
    /// on, silently.
    ///
    /// The second is that the client's own exports name several members, which
    /// is where the rewrite should get its names rather than from guesswork.
    /// Each takes the quest's Identity, finds the record and returns one
    /// member:
    ///
    ///   +0x08  a NUL-terminated string          (see <see cref="ShortInfo"/>)
    ///   +0x28  N3Msg_GetQuestActionList         (see <see cref="QuestActions"/>)
    ///   +0x34  a RewardBox_t - the client names it in the diagnostic "bad
    ///          stream in RewardBox_t" - whose +0 is N3Msg_QuestGetCashReward
    ///          and +4 is N3Msg_QuestGetXPReward, confirming
    ///          <see cref="CashReward"/> and <see cref="ExperienceReward"/>
    ///   +0x48  N3Msg_GetQuestIcon
    ///   +0x88  the int32-counted string         (see <see cref="Info"/>)
    ///   +0xA4  <see cref="Flags"/>, and two of its bits are named: bit 8 is
    ///          N3Msg_IsTeamMission and bit 9 is N3Msg_IsTeamMissionCopy
    ///
    /// N3Msg_GetQuestWorldPos does not read the quest directly - it goes
    /// through the action list at +0x28 to the first action's +0x6C, which
    /// holds an Identity and then a Vector3. So the quest marker the server
    /// sends lives on the action, not on the quest.
    ///
    /// The two int32 lists this class calls Unknown18 and Unknown19 are read
    /// with each element masked to its low 27 bits before it is stored, at
    /// 0x100ABFE6 and 0x100AC022.
    /// </remarks>
    public class QuestInfo
    {
        [AoMember(0)]
        public Identity QuestIdentity { get; set; }

        /// <summary>
        /// The record's version. 15 in every captured copy.
        /// </summary>
        /// <remarks>
        /// The reader at Gamecode 0x100ABEA7 refuses anything outside 7 to 15,
        /// and then uses the value to decide which of eight optional tails to
        /// read:
        ///
        ///   8 or more   an int32; below 8 the client uses 6 instead
        ///   9 or more   a sub-record
        ///   10 or more  an int32
        ///   11 or more  an int32
        ///   12 or more  an identity and two int32s
        ///   13 or more  a counted list of identity and int32 pairs
        ///   14 or more  an int32
        ///   15          a sub-record read at 0x100ABDE6
        ///
        /// The fields below read all of those without asking, which is right
        /// for 15 and wrong for anything less. Nothing has contradicted it
        /// because no capture carries less - and nothing would, until a server
        /// sent an older one.
        /// </remarks>
        [AoMember(1)]
        [AoFlags("questversion")]
        public int Version { get; set; }

        /// <summary>
        /// An int32 the client reads and drops. Zero throughout.
        /// </summary>
        /// <remarks>
        /// It lands in a stack slot at 0x100ABEDA that the very next read
        /// overwrites at 0x100ABEE2, so nothing can use it.
        /// </remarks>
        [AoMember(2)]
        public int Unread1 { get; set; }

        /// <summary>
        /// A second int32 the client reads and drops. Zero throughout.
        /// </summary>
        /// <remarks>
        /// It takes the slot the one above was overwritten out of, and the
        /// count at 0x100ABFC5 writes over that slot before anything reads it.
        /// </remarks>
        [AoMember(3)]
        public int Unread2 { get; set; }

        /// <summary>
        /// Flags on the quest. 2 in every captured copy.
        /// </summary>
        /// <remarks>
        /// The client keeps this at the quest's +0xA4 and works on it a bit at
        /// a time: 0x100AB64D tests bit 0x10 and sets bit 0x04 when it finds
        /// it, 0x100AB669 sets 0x04 on its own, 0x100AB9BE tests bit 0x100, and
        /// 0x100ABA99 sets 0x100 and 0x200 together when a quest is copied.
        ///
        /// The bit every captured copy carries is 0x02, and that is not one of
        /// the ones the client touches.
        ///
        /// Two of the bits have names, from the exports that read them:
        /// N3Msg_IsTeamMission returns bit 8 - 0x100 - and
        /// N3Msg_IsTeamMissionCopy returns bit 9 - 0x200. That matches what
        /// 0x100ABA99 does, which is set both together when a quest is copied.
        /// </remarks>
        [AoMember(4)]
        public int Flags { get; set; }

        [AoMember(5, SerializeSize = ArraySizeType.NullTerminated)]
        public string ShortInfo { get; set; }

        /// <summary>
        /// The quest text the client shows.
        /// </summary>
        /// <remarks>
        /// Length-prefixed and null-terminated, with the length counting the
        /// terminator - the same convention as an item name, and the reason
        /// every captured QuestFullUpdate came out a byte short. The quest for
        /// Desmond Calitri carries 416 characters and a length of 417.
        ///
        /// ShortInfo above it is the title and is a different shape again: no
        /// length at all, just characters and a terminator.
        /// </remarks>
        [AoMember(6, SerializeSize = ArraySizeType.Int32Terminated)]
        public string Info { get; set; }
        /// <summary>
        /// Who gave the quest.
        /// </summary>
        /// <remarks>
        /// Five distinct values across the 62 distinct quests captured, and
        /// four of the five are identities the client opened a KnuBot
        /// conversation with in the same sessions - the NPCs the character
        /// actually talked to. The fifth is not, which is what a quest handed
        /// over by a terminal rather than a conversation would look like.
        ///
        /// This server was already sending the giver here on a guess; the
        /// captures agree with it.
        /// </remarks>
        [AoMember(7)]
        public Identity QuestGiver { get; set; }

        // We need to distinguish later for the older versions (actual version=6)
        /// <summary>
        /// The reward descriptor's own version. 6 in every captured copy.
        /// </summary>
        /// <remarks>
        /// The client calls the descriptor RewardBox_t - that is the name in
        /// the error it prints when one arrives malformed - and two of its
        /// fields are confirmed by exported getters: N3Msg_QuestGetCashReward
        /// reads the quest's +0x34 and then +0x00, and N3Msg_QuestGetXPReward
        /// reads the same box and then +0x04.
        ///
        /// Read at 0x10087112 and refused outside 3 to 6, and it gates the
        /// descriptor's tail the way the record's version gates the record: 4
        /// or more adds three int32s, 5 or more adds two more, 6 adds the
        /// ACGItem. Below those the client zeroes the fields instead of reading
        /// them, so an older descriptor is shorter on the wire and the fields
        /// below would read past its end.
        /// </remarks>
        /// <summary>
        /// The reward box's own version, gated 3 to 6. Six in all 193 captured
        /// records.
        /// </summary>
        /// <remarks>
        /// Everything from here down to <see cref="RewardItem"/> belongs to a
        /// separate record - the box at Quest_t + 0x34, read by Gamecode
        /// 0x100870A8 - and most of it is conditional on this number.
        ///
        /// What the box reader does, in order:
        ///
        ///   always   this version, gated 3 to 6
        ///   always   an int32 to box + 0, the cash - N3Msg_QuestGetCashReward
        ///            returns Quest_t + 0x34 then + 0
        ///   always   an int32 into a discarded stack local
        ///   always   an int32 to box + 4, the experience - N3Msg_QuestGetXPReward
        ///            returns + 0x34 then + 4
        ///   always   two X3F1 Identity lists
        ///   always   an item list at box + 0x1C
        ///   v &gt;= 4   three int32s, to box + 8, + 0x14 and + 0x18
        ///   v &gt;= 5   two int32s, to box + 0xC and + 0x10
        ///   v &gt;= 6   a GameData::ACGItem_t at box + 0x28, through the client's
        ///            own stream operator
        ///
        /// So members 15 to 17 here are the version 4 tail, 18 and 19 the
        /// version 5 tail, and 20 the one ACGItem_t. Until 2026-09-11 this class
        /// flattened all of that as unconditional, which is right for version 6
        /// and wrong for anything older: a server sending version 3 would put
        /// none of it on the wire and this class would read past the end. Every
        /// captured record is version 6, so all four branches were taken and the
        /// flattened read was byte-exact - the same way InfoPacket's seven
        /// enumerated flag values were byte-exact on everything anyone had
        /// caught.
        ///
        /// The conditions below are written as EqualsToAny over the versions
        /// that qualify rather than as a comparison, because the criteria this
        /// serializer has do not include one. It is exact rather than
        /// approximate: the reader refuses anything outside 3 to 6 outright, at
        /// 0x10087117, so {4,5,6} is the whole of "4 or more" and {5,6} the
        /// whole of "5 or more".
        ///
        /// Walking this is what fixed the alignment of everything after it: the
        /// box swallows nine of this class's members, and counting them wrong put
        /// <see cref="MissionIconId"/> three places off.
        /// </remarks>
        [AoMember(8)]
        [AoFlags("questrewardversion")]
        public int RewardDescriptorVersion { get; set; }

        [AoMember(9)]
        public int CashReward { get; set; }
        // a null again?
        /// <summary>
        /// An int32 the reward descriptor reads and drops. Zero throughout.
        /// </summary>
        /// <remarks>
        /// The descriptor at 0x100870A8 chains three reads through the stream's
        /// operator&gt;&gt;: the first lands on the cash, the second on a stack
        /// slot nothing looks at, the third on the experience.
        /// </remarks>
        /// <summary>
        /// Read and thrown away. Zero in all 241 captured records.
        /// </summary>
        /// <remarks>
        /// Not "unknown" - unread. The reward box reader at Gamecode 0x1008713D
        /// takes three int32s in a row through the same chained calls, and the
        /// out-parameters are the box's + 0, a stack local at [ebp - 0x24], and
        /// the box's + 4. The first is the cash and the third is the experience;
        /// the middle one goes into a local that nothing else in the function
        /// touches.
        /// </remarks>
        [AoMember(10)]
        public int RewardUnread { get; set; }
        [AoMember(11)]
        public int ExperienceReward { get; set; }

        [AoMember(12, SerializeSize = ArraySizeType.X3F1)]
        public Identity[] UnknownIdentities1 { get; set; }
        [AoMember(13, SerializeSize = ArraySizeType.X3F1)]
        public Identity[] UnknownIdentities2 { get; set; }

        [AoMember(14, SerializeSize = ArraySizeType.X3F1)]
        public QuestItemShort[] ItemRewards { get; set; }

        /// <summary>
        /// The quest's four character code.
        /// </summary>
        /// <remarks>
        /// An int32 on the wire and four characters in it. Of the 62 distinct
        /// quests captured, 60 carry a value whose four bytes are all
        /// printable - O4V0, 7KJ5, HDOG, C3K3, V9LK and so on - and the other
        /// two carry zero. That is the code the mission journal shows beside a
        /// quest, packed big-endian.
        ///
        /// It stays an int32 here rather than becoming a string: zero is a
        /// real value and would not survive the round trip as one.
        /// </remarks>
        [AoMember(15)]
        [AoUsesFlags("questrewardversion", typeof(int), FlagsCriteria.EqualsToAny, new[] { 4, 5, 6 })]
        public int? QuestCode { get; set; }
        /// <summary>
        /// Box + 0x14. Zero in all 241 captured records.
        /// </summary>
        /// <remarks>
        /// This used to be read beside <see cref="QuestCode"/> as the two
        /// halves of an Identity - a printable tag in one and an unused zero in
        /// the other - and that is now ruled out rather than merely unclaimed.
        /// The two are consecutive on the wire but the reader puts them in
        /// box + 8 and box + 0x14, eight bytes apart with box + 0xC and + 0x10
        /// in between, and an Identity is read as a unit into eight contiguous
        /// bytes. Whatever these two are, they are not one value.
        /// </remarks>
        /// <summary>
        /// Box + 0x14. Present from box version 4, with
        /// <see cref="QuestCode"/> and <see cref="Unknown9"/>.
        /// </summary>
        [AoMember(16)]
        [AoUsesFlags("questrewardversion", typeof(int), FlagsCriteria.EqualsToAny, new[] { 4, 5, 6 })]
        public int? Unknown8 { get; set; }

        /// <summary>
        /// Box + 0x18. Present from box version 4.
        /// </summary>
        [AoMember(17)]
        [AoUsesFlags("questrewardversion", typeof(int), FlagsCriteria.EqualsToAny, new[] { 4, 5, 6 })]
        public int? Unknown9 { get; set; }

        /// <summary>
        /// A second four character code. Box + 0xC, present from box version 5.
        /// </summary>
        /// <remarks>
        /// This was recorded as zero in all 193 captured records, and that has
        /// stopped being true: the corpus is 241 now and 73 of them carry a
        /// value. Every one of the seventeen distinct values is four printable
        /// characters, the same shape as <see cref="QuestCode"/> - 925D, 5UFZ,
        /// XX76, N6MO, 58YD, IGMR, E7AC, 12Y8, MMSU, L5H7, 8YIR, QSOR, 3JRQ,
        /// DIRM, MSRE, YNRL, IWQB. That is the shape of a mission key.
        ///
        /// It moves with <see cref="Quality"/>: both are zero in 168 of the 241
        /// and non-zero in the other 73, which is what a pair belonging to one
        /// kind of quest looks like. Which kind is not settled. Splitting the
        /// captures by quest name does not separate them, but a packet carries
        /// up to three quests and the test that produced that split could not
        /// say which record inside a packet the code belonged to. Anyone
        /// picking this up should parse the records rather than the packets.
        /// </remarks>
        [AoMember(18)]
        [AoUsesFlags("questrewardversion", typeof(int), FlagsCriteria.EqualsToAny, new[] { 5, 6 })]
        public int? UnknownHash { get; set; }

        /// <summary>
        /// Box + 0x10. Present from box version 5, and moves with
        /// <see cref="UnknownHash"/>.
        /// </summary>
        /// <remarks>
        /// Also recorded as zero in all 193 records once, and also no longer:
        /// 0 in 168 of 241, then 1, 2, 3, 4, 5, 7, 9, 10, 11, 39, 212 and 250.
        /// Small numbers with two large ones, on the same 73 records that carry
        /// a code above. A quest level would look like this and so would half a
        /// dozen other things; it is not named on thirteen values.
        /// </remarks>
        [AoMember(19)]
        [AoUsesFlags("questrewardversion", typeof(int), FlagsCriteria.EqualsToAny, new[] { 5, 6 })]
        public int? Quality { get; set; }

        /// <summary>
        /// The item the quest rewards, when the box is version 6.
        /// </summary>
        /// <remarks>
        /// One GameData::ACGItem_t, read as a unit at 0x100871CF through
        /// GameData's own stream operator into the descriptor's + 0x28 - the
        /// same four numbers InventoryEntry carries:
        /// two templates, a level, and a fourth int ACGItem_t serializes and
        /// never uses. Zero in every captured copy, which is what a quest with
        /// no item reward looks like.
        ///
        /// That it is the reward item rather than some other item is the
        /// client's word: N3Msg_GetItemRewardList at 0x1001B50A takes a quest,
        /// walks to the reward box at the quest's +0x34, and builds its list
        /// from +0x28 - this - checking each against the same null item global
        /// InventoryEntry checks against.
        /// </remarks>
        [AoMember(20)]
        [AoUsesFlags("questrewardversion", typeof(AcgItem), FlagsCriteria.EqualsToAny, new[] { 6 })]
        public AcgItem RewardItem { get; set; }

        /// <summary>
        /// The character whose quest this is.
        /// </summary>
        /// <remarks>
        /// Equal to the message's own identity in all 62 captured records,
        /// without exception, across four different characters.
        /// </remarks>
        /// <summary>
        /// Quest_t + 0x40. Equals the message identity in every captured record.
        /// </summary>
        /// <remarks>
        /// The offset is now pinned rather than counted: the per-quest reader at
        /// 0x100ABEA7 reads the reward box, then this Identity into + 0x40, then
        /// <see cref="MissionIconId"/> into + 0x48. See the class remarks for how
        /// the box's version-gated tail was walked to get there.
        /// </remarks>
        [AoMember(24)]
        public Identity Owner { get; set; }
        /// <summary>
        /// The quest's icon. Quest_t + 0x48.
        /// </summary>
        /// <remarks>
        /// Proven by the client's own export rather than by the shape of the
        /// values: N3Msg_GetQuestIcon at Gamecode 0x10017448 resolves the quest
        /// and returns its + 0x48, and the per-quest reader fills + 0x48 with the
        /// int32 that follows <see cref="Owner"/>.
        ///
        /// Getting here needed the reward box's version-gated tail walked first -
        /// the naive alignment put this three members earlier, on a field that is
        /// zero in all 193 records, which would have meant a client that never
        /// draws a quest icon. Six values across the captures: 244818, 158429,
        /// 11330, 11340, 11342 and 11335.
        /// </remarks>
        [AoMember(25)]
        public int MissionIconId { get; set; }
        /// <summary>
        /// A time limit, in seconds. Always equal to
        /// <see cref="TimeLimitCopy"/>.
        /// </summary>
        /// <remarks>
        /// Measured, and the measurement is unusually clean. Across all 193
        /// captured quest records this field and the one after it hold the same
        /// value every single time - 185 pairs of zero, four pairs of 2880 and
        /// four pairs of 7200. Two independent int32s do not do that.
        ///
        /// 2880 seconds is forty-eight minutes and 7200 is two hours, which are
        /// mission time limits and not much else. Zero on a quest with no limit,
        /// which is 185 of the 193.
        ///
        /// Why there are two of them is not settled. A limit beside a remaining
        /// time would be equal at the moment a mission is granted, and every
        /// captured record is a zone-in snapshot rather than a mid-mission one,
        /// so the captures cannot tell the two apart. The client's own
        /// N3Msg_QuestRemainingTime does not read either: it walks the quest's
        /// action list and takes a deadline off QuestAction_t + 0x5C, which is a
        /// different field in a different record.
        /// </remarks>
        [AoMember(26)]
        public int TimeLimit { get; set; }
        /// <summary>
        /// The second half of the pair. See <see cref="TimeLimit"/>.
        /// </summary>
        [AoMember(27)]
        public int TimeLimitCopy { get; set; }
        [AoMember(28, SerializeSize = ArraySizeType.X3F1)]
        public QuestActionList[] QuestActions { get; set; }
        [AoMember(29, SerializeSize = ArraySizeType.X3F1)]
        public Identity[] Unknown17 { get; set; }

        /// <remarks>
        /// A plain int32 count and then that many int32s - not X3F1. Each one
        /// is masked with 0x07FFFFFF at 0x100ABFE6 before the client keeps it,
        /// so the top five bits of every entry are on the wire and thrown away.
        /// Empty in every captured record.
        /// </remarks>
        [AoMember(30, SerializeSize = ArraySizeType.Int32)]
        public int[] Unknown18 { get; set; }
        /// <remarks>
        /// The same shape and the same 0x07FFFFFF mask as
        /// <see cref="Unknown18"/>, at 0x100AC022, into a different vector.
        /// </remarks>
        [AoMember(31, SerializeSize = ArraySizeType.Int32)]
        public int[] Unknown19 { get; set; }

        /// <remarks>
        /// A plain int32 count, and then per entry an Identity and an int32
        /// length followed by that many bytes. The client reads the run into a
        /// stack buffer at 0x100AC07B and does nothing with it, so the name is
        /// on the wire and dropped where this reader is concerned.
        /// </remarks>
        [AoMember(32, SerializeSize = ArraySizeType.Int32)]
        public QuestCharInfo[] CharInfos { get; set; }

        /// <summary>
        /// Everything from here down is gated on <see cref="Version"/>, one
        /// member or small group per version from 8 up.
        /// </summary>
        /// <remarks>
        /// The reader walks it at 0x100AC08C and after, and it is a staircase:
        /// each block is a jge against the next version number, with the else
        /// arm zeroing the member rather than reading it. This one is the
        /// exception that gives the pattern away - below version 8 the client
        /// does not zero it, it writes 6. Our own server sends 6 here, from a
        /// constant named for the field, which is the client's own default
        /// arrived at independently.
        ///
        /// All ten of these were unconditional until 2026-09-11. Every captured
        /// record is version 15, so every arm is taken and the flat read is
        /// byte exact on all 241 - and a server sending 14 would have left four
        /// bytes on the wire, 13 a list, and so on down.
        ///
        /// The value lists are exact rather than approximate: the reader
        /// refuses any version outside 7 to 15 at 0x100ABECE, so "8 or more" is
        /// the whole of 8 to 15.
        /// </remarks>
        [AoMember(33)]
        [AoUsesFlags("questversion", typeof(int), FlagsCriteria.EqualsToAny, new[] { 8, 9, 10, 11, 12, 13, 14, 15 })]
        public int? Unknown20 { get; set; }

        /// <summary>
        /// Version 9 and up. An X3F1 Identity list.
        /// </summary>
        [AoMember(34, SerializeSize = ArraySizeType.X3F1)]
        [AoUsesFlags("questversion", typeof(Identity[]), FlagsCriteria.EqualsToAny, new[] { 9, 10, 11, 12, 13, 14, 15 })]
        public Identity[] UnknownIdentities20 { get; set; }

        /// <summary>
        /// Version 10 and up.
        /// </summary>
        [AoMember(35)]
        [AoUsesFlags("questversion", typeof(int), FlagsCriteria.EqualsToAny, new[] { 10, 11, 12, 13, 14, 15 })]
        public int? Unknown21 { get; set; }

        /// <summary>
        /// Version 11 and up.
        /// </summary>
        [AoMember(36)]
        [AoUsesFlags("questversion", typeof(int), FlagsCriteria.EqualsToAny, new[] { 11, 12, 13, 14, 15 })]
        public int? Unknown22 { get; set; }

        /// <summary>
        /// Version 12 and up, with <see cref="Unknown24"/> and
        /// <see cref="Unknown25"/> - the three are one block in the reader.
        /// </summary>
        [AoMember(37)]
        [AoUsesFlags("questversion", typeof(Identity), FlagsCriteria.EqualsToAny, new[] { 12, 13, 14, 15 })]
        public Identity? Unknown23 { get; set; }

        /// <summary>
        /// Version 12 and up. See <see cref="Unknown23"/>.
        /// </summary>
        [AoMember(38)]
        [AoUsesFlags("questversion", typeof(int), FlagsCriteria.EqualsToAny, new[] { 12, 13, 14, 15 })]
        public int? Unknown24 { get; set; }

        /// <summary>
        /// Version 12 and up. See <see cref="Unknown23"/>.
        /// </summary>
        [AoMember(39)]
        [AoUsesFlags("questversion", typeof(int), FlagsCriteria.EqualsToAny, new[] { 12, 13, 14, 15 })]
        public int? Unknown25 { get; set; }

        /// <summary>
        /// Version 13 and up. An int32 count and then that many entries, each
        /// an Identity and an int32, filed in the map at the quest's + 0xC8.
        /// </summary>
        [AoMember(40, SerializeSize = ArraySizeType.Int32)]
        [AoUsesFlags("questversion", typeof(QuestIdentity[]), FlagsCriteria.EqualsToAny, new[] { 13, 14, 15 })]
        public QuestIdentity[] QuestIdentities { get; set; }

        /// <summary>
        /// Version 14 and up.
        /// </summary>
        [AoMember(41)]
        [AoUsesFlags("questversion", typeof(int), FlagsCriteria.EqualsToAny, new[] { 14, 15 })]
        public int? Unknown26 { get; set; }

        /// <summary>
        /// Version 15 only, which is every captured record.
        /// </summary>
        [AoMember(42, SerializeSize = ArraySizeType.X3F1)]
        [AoUsesFlags("questversion", typeof(QuestFaction[]), FlagsCriteria.EqualsToAny, new[] { 15 })]
        public QuestFaction[] FactionInfo { get; set; }
    }
}
