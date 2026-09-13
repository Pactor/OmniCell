using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SmokeLounge.AOtomation.Messaging.GameData
{
    using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

    /// <summary>
    /// One thing the character cannot use yet, and for how much longer.
    /// </summary>
    /// <remarks>
    /// The same twenty byte record fills all three of FullCharacter's lists -
    /// skills, perks and nanos - and each list goes straight into its own
    /// subsystem, at 0x100741F6, 0x1007423D and 0x1007426F, through the same
    /// range-insert at 0x10074898. That insert divides by sixteen, so the
    /// version at the front is not kept: in memory an entry is the Identity and
    /// the two int32s.
    ///
    /// The client does read an entry back, which was found on 2026-09-12 and
    /// names the second number outright. 0x1006414F walks the skill
    /// subsystem's vector sixteen bytes at a time, matches an entry's + 4 -
    /// the Identity's instance half - against the stat it was asked about, and
    /// returns the entry's + 0xC. Its caller at 0x10044F9A passes that to the
    /// lock-message formatter at 0x10064847, which divides by 3600 and 60 and
    /// prints the sentence keyed UnableToPerformActionSkill:
    ///
    ///     Unable to perform action, %s skill is locked, able in %02d:%02d:%02d
    ///
    /// So + 0xC is seconds remaining. The perk list is read the same way -
    /// 0x10052F2E over the vector at the perk subsystem's + 8, into the same
    /// formatter with UnableToPerformActionPerk - and the nano list is the same
    /// record again.
    ///
    /// The sentences are not in the binary. Gamecode holds the key and the
    /// category and the text lives in cd_image/text/ctext.ldb under the ELF
    /// hash of the key.
    /// </remarks>
    public class FullCharacterSub2
    {
        /// <summary>
        /// The record's version, 1.
        /// </summary>
        /// <remarks>
        /// The reader at Gamecode 0x1003BD40 takes this int32 into a local and
        /// never looks at it again; the matching writer at 0x1003BCFA pushes a
        /// literal 1 in its place. All captured copies carry 1.
        /// </remarks>
        [AoMember(1)]
        public int Version { get; set; }

        /// <summary>
        /// What the entry is about - a skill, a perk or a nano program,
        /// depending on which of FullCharacter's three lists it came from.
        /// </summary>
        /// <remarks>
        /// In the skill list the type half is zero and the instance is a stat
        /// id: the three captured entries are 123, 140 and 566 - firstaid,
        /// mapnavigation and gos. In the nano list the type half is 1 and the
        /// instance 527 in all four captured entries.
        /// </remarks>
        [AoMember(2)]
        public Identity Identity { get; set; }

        /// <summary>
        /// How long the lock lasts, in seconds.
        /// </summary>
        /// <remarks>
        /// Measured, not read out of the client - the entry goes into its
        /// subsystem's vector without either int32 being touched on the way,
        /// and no reader of that vector could be found.
        ///
        /// What the captures say: this number is constant for a given id and
        /// the one beside it is not, and the three skill ids carry the lock
        /// times Anarchy Online is known for - firstaid 40, mapnavigation 60
        /// and gos 600, the ten minute Grid recharge. The four nano entries all
        /// carry 180.
        /// </remarks>
        [AoMember(3)]
        public int LockDuration { get; set; }

        /// <summary>
        /// How much of the lock is left, in seconds.
        /// </summary>
        /// <remarks>
        /// The same measurement the other way round: this one varies while
        /// <see cref="LockDuration"/> holds, and in all seven captured entries
        /// it is strictly less than it - 17 and 12 against firstaid's 40, 372
        /// against gos's 600, 3 against mapnavigation's 60, and 29, 86, 139 and
        /// 176 against the nanos' 180.
        ///
        /// Two entries and their durations would prove nothing; seven, across
        /// four ids and two lists, with the smaller number never once exceeding
        /// the larger, is what the name rests on.
        /// </remarks>
        [AoMember(4)]
        public int LockRemaining { get; set; }
    }
}
