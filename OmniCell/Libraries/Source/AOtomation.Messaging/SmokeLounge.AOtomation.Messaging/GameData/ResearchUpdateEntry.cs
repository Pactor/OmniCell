using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SmokeLounge.AOtomation.Messaging.GameData
{
    using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

    public class ResearchUpdateEntry
    {
        /// <summary>
        /// Which research this entry is about, and the terminator when zero.
        /// </summary>
        /// <remarks>
        /// The list has no count in front of it: the reader at Gamecode.dll
        /// 0x1003A7F8 reads this int32 and stops when it is zero, which is why
        /// the array is declared NullTerminated. A non-zero one is looked up in
        /// the client's research table at 0x1002BD8C, and when the lookup fails
        /// the three values below are read and dropped.
        ///
        /// 54 entries in every captured copy.
        /// </remarks>
        [AoMember(1)]
        public int ResearchId { get; set; }

        /// <summary>
        /// Neutral-side research XP remaining.
        /// </summary>
        /// <remarks>
        /// The reader files the three values under table indexes 0, 1 and 2,
        /// through 0x1002C063, which refuses an index above 2 and picks one of
        /// three lists at table + 4, + 0x14 and + 0x24.
        ///
        /// That those three are sides was settled on 2026-09-11. The accessor
        /// at Gamecode 0x10027820 looks a research id up in the same table and
        /// branches on a flags word at definition + 0xC - bit 7 global, bit 6
        /// personal. The global branch reads stat 0x21, 33, side, off the
        /// character at [dynel + 0xE8] vtable + 0x3C and hands it straight to
        /// 0x1002BE1A as an index; 0x1002BE1A caps it at 2 and computes
        /// [table + index * 16 + 4], the same three lists in the same order.
        /// Side is 0 Neutral, 1 Clan, 2 Omni, and there are exactly three.
        ///
        /// XP remaining rather than anything else: 0x1002C063 writes the value
        /// into the eighth word of the slot, + 0x1C, and 0x1002BE50 tests that
        /// same + 0x1C for being negative to decide whether the entry has been
        /// filled in. PerkUpdate writes experience still owed to + 0x1C of an
        /// identically shaped slot on the character's own personal map and
        /// runs its progress bar off it.
        ///
        /// This message carries only the global half. A bit 6 definition is
        /// not in here at all.
        /// </remarks>
        [AoMember(2)]
        public int NeutralResearchXpRemaining { get; set; }

        /// <summary>
        /// Clan-side research XP remaining (manager/Side index 1).
        /// </summary>
        [AoMember(3)]
        public int ClanResearchXpRemaining { get; set; }

        /// <summary>
        /// Omni-side research XP remaining (manager/Side index 2).
        /// </summary>
        [AoMember(4)]
        public int OmniResearchXpRemaining { get; set; }
    }
}
