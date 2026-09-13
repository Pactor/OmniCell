using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SmokeLounge.AOtomation.Messaging.GameData
{
    using System.Dynamic;

    using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

    public class QuestActionList
    {
        // Maybe Version id again?
        [AoMember(0)]
        public int Version { get; set; }
        [AoMember(1)]
        public Identity Action { get; set; }

        [AoMember(2)]
        public Identity Unknown1 { get; set; }
        [AoMember(3)]
        public Identity Unknown2{ get; set; }
        [AoMember(4)]
        public Identity Unknown3 { get; set; }
        [AoMember(5)]
        public Identity Unknown4 { get; set; }

        [AoMember(6)]
        public float Unknown5 { get; set; }
        [AoMember(7)]
        public float Unknown6 { get; set; }
        [AoMember(8)]
        public float Unknown7 { get; set; }
        [AoMember(9)]
        public float Unknown8 { get; set; }
        [AoMember(10)]

        public Identity Unknown9 { get; set; }
        [AoMember(11)]
        public float Unknown10 { get; set; }
        [AoMember(12)]
        public float Unknown11 { get; set; }
        [AoMember(13)]
        public float Unknown12 { get; set; }
        [AoMember(14)]
        public float Unknown13 { get; set; }
        [AoMember(15)]
        public Identity Unknown14 { get; set; }

        /// <summary>
        /// When the action runs out, as a Unix timestamp. Zero when it does not.
        /// </summary>
        /// <remarks>
        /// QuestAction_t + 0x5C, and the client says what it is twice over.
        ///
        /// N3Msg_QuestRemainingTime, exported from Interfaces.dll at 0x1000A8AC,
        /// is the whole of the first argument: it walks the quest's action list,
        /// takes each action's + 0x5C, skips the zeros, and returns the
        /// difference against the server clock. A deadline is the only thing you
        /// subtract a clock from.
        ///
        /// The values agree. Eight of the 193 captured records carry a non-zero
        /// one, and they decode as 2026-09-13 01:09 and 01:26 and 2026-09-16
        /// 12:00, 12:04 and 12:05 - one day and five days after the captures
        /// they arrived in, which were taken on 2026-09-11. The other 185 are
        /// zero, which is an action with no time limit.
        ///
        /// Two further details settle it beyond the values. The clock it is
        /// compared against is N3Msg_GetServerSynceSystemTime - Funcom's
        /// spelling - so this is an absolute wall time and not a tick count,
        /// and the comparison is the plain <c>if (deadline &gt;= now) remaining
        /// = deadline - now</c>.
        ///
        /// And a second export reads the same member: QuestGetCriteriaBefore,
        /// at Interfaces.dll 0x10008385, is a one line <c>return
        /// action-&gt;+ 0x5C</c>. Its name is the other half of the sentence -
        /// the criteria is that the action be done *before* this - and it is
        /// Funcom's word, not ours.
        ///
        /// The old name came from the value looking like a hash, which is what a
        /// timestamp looks like if you do not check the date.
        /// </remarks>
        [AoMember(16)]
        public int Deadline { get; set; }
        [AoMember(17)]
        public int Unknown16 { get; set; }
        [AoMember(18)]
        public Identity Unknown17 { get; set; }

        /// <summary>
        /// The playfield half of the marker. First field of the
        /// GameData::WorldPos_c that closes the record.
        /// </summary>
        /// <remarks>
        /// Everything from here to <see cref="Z"/> is one record, not six loose
        /// fields: the reader at 0x100ACCB4 hands + 0x6C straight to the
        /// client's own stream operator for GameData::WorldPos_c.
        ///
        /// It is the quest marker. N3Msg_GetQuestWorldPos at Gamecode 0x1001ABC9
        /// reaches the quest's action list, takes the first action, adds 0x6C
        /// and hands its caller the Identity at + 0 and vectors from + 8 and
        /// + 0x14 - which is why an earlier note said that function reaches the
        /// marker through the action list rather than off the quest.
        /// </remarks>
        [AoMember(19)]
        public Identity Playfield { get; set; }

        // Probably low and high id of the entrance
        /// <summary>
        /// Two integers the client turns into a second position, with no height.
        /// 100000 in 173 of the 193 captured records.
        /// </summary>
        /// <remarks>
        /// A WorldPos_c holds two Vector3s but only one of them is on the wire.
        /// GameData.dll's reader at 0x1000CA52 takes these two int32s, runs each
        /// through fild, puts the first in component 0 and the second in
        /// component 2, and pushes an fldz into component 1 - so the second
        /// vector is a ground position derived from this pair rather than read,
        /// and its height is always zero. That is why the record is 32 bytes in
        /// memory and 28 on the wire, and why reading 28 here is right.
        ///
        /// N3Msg_GetQuestWorldPos hands its caller both vectors, the one read
        /// and the one derived.
        ///
        /// The action's constructor at Gamecode 0x100ACAFC says the same thing
        /// from the other side: it zeroes every member up to + 0x68 by hand and
        /// then calls GameData::WorldPos_c's own constructor on + 0x6C, and the
        /// record is 0x8C bytes - so the WorldPos occupies + 0x6C to + 0x8C,
        /// which is the 32 bytes in memory against 28 on the wire. The same
        /// constructor zeroes + 0x2C, + 0x30, + 0x34, + 0x38, + 0x44, + 0x48,
        /// + 0x4C and + 0x50 with float stores and everything else with an
        /// integer zero, which is where this class's float and Identity typing
        /// is confirmed rather than inferred from the reader alone.
        ///
        /// What the pair means is not settled. 100000 and 100000 together in 173
        /// of 193 records reads as a sentinel; the ones that are not carry
        /// 41975 with 45030, and 34495 with 29814, which are an order of
        /// magnitude larger than the real coordinates in <see cref="X"/> and
        /// <see cref="Z"/> beside them, so they are not the same position in the
        /// same units.
        /// </remarks>
        [AoMember(20)]
        public int Unknown18 { get; set; }

        /// <summary>
        /// The second of the pair. See <see cref="Unknown18"/>.
        /// </summary>
        [AoMember(21)]
        public int Unknown19 { get; set; }
        /// <summary>
        /// Where the marker is. Read as a float, unlike the pair above.
        /// </summary>
        /// <remarks>
        /// Confirmed against the rest of the capture rather than assumed: the
        /// values are 3519, 3462, 3434, 3621 and 3431, which are Rubi-Ka
        /// coordinates of the same magnitude as the character positions in the
        /// SimpleCharFullUpdates from the same sessions. <see cref="Y"/> is zero
        /// in 192 of 193 and 11.83 in the last, and <see cref="Z"/> runs 797 to
        /// 880 - a marker on the ground.
        /// </remarks>
        [AoMember(22)]
        public float X { get; set; }
        [AoMember(23)]
        public float Y { get; set; }
        [AoMember(24)]
        public float Z { get; set; }

    }
}
