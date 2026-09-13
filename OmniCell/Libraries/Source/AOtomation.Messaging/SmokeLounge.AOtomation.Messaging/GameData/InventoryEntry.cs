using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SmokeLounge.AOtomation.Messaging.GameData
{
    using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

    /// <summary>
    /// One occupied slot of a container.
    /// </summary>
    /// <remarks>
    /// The client's own name for this is InventoryEntry_t - Interfaces.dll
    /// exports half a dozen functions that return lists of them. It is 40 bytes
    /// there, built at 0x1002A1AD and read at 0x1002A20B, and the reader takes
    /// exactly what is below: two 16 bit fields, an identity, and the item.
    /// </remarks>
    public class InventoryEntry
    {
        /// <summary>
        /// Which slot of the container this entry fills.
        /// </summary>
        /// <remarks>
        /// Read before the entry itself, and used straight away as the index
        /// the entry is filed under.
        /// </remarks>
        [AoMember(0)]
        public int Slotnumber { get; set; }

        /// <summary>
        /// What the slot holds and what may be done with it.
        /// </summary>
        /// <remarks>
        /// Every bit the client reads, and one it does not:
        ///
        /// 1, 2 and 4 are the kind of thing in the slot, and only one of them
        /// is ever set - 0x1002A961 returns 1, 2 or 4 depending on which, and 0
        /// for an empty slot. 1 is a template item, and it is what the client
        /// sets when it puts an item in a slot itself: 0x1002AA8A writes 0x21
        /// and copies the item's three numbers in. 2 is a live dynel, and the
        /// one captured entry carrying it is the only one with a real identity.
        /// 4 has never been seen.
        ///
        /// 1 also gates reading the item at all, at 0x1002ABD0, and 2 gates
        /// taking the entry out of the container, at 0x1002AD81.
        ///
        /// 0x20 decides whether the entry is listed. CorpseEntry_t's list
        /// builder at 0x10125D9D walks the occupied slots and puts each one in
        /// the window only if 0x1002AEA1 agrees - which it does when this bit
        /// is set, or when the item's own stat 30 carries 0x40. The client sets
        /// it on an entry it has just moved into a container, and 0x1002A78D
        /// clears it across every slot at once.
        ///
        /// 0x40 is set and cleared by 0x1002A810 and read by 0x1002A86D, always
        /// against equipment slot numbers.
        ///
        /// 0x10 and 0x80 have a getter and a setter each and nothing calls
        /// either of them in this build. 0x80 is the one that matters, because
        /// the server sets it: nine of the eleven captured entries carry it and
        /// nothing in the client looks at it.
        /// </remarks>
        [AoMember(1)]
        public short Flags { get; set; }

        /// <summary>
        /// How many of the item are in the slot.
        /// </summary>
        /// <remarks>
        /// 0x1002AA8A takes it as an argument when it puts an item in a slot,
        /// and the setter at 0x1002AF06 clamps it to 0 through 0xFFFF and marks
        /// the entry dirty when it changes. 1 in fifty five captured entries,
        /// with a 50 and a 25 among them.
        /// </remarks>
        [AoMember(2)]
        public short Count { get; set; }

        /// <summary>
        /// The dynel in the slot, when the flags say there is one.
        /// </summary>
        [AoMember(3)]
        public Identity Identity { get; set; }

        /// <summary>
        /// The item's first template.
        /// </summary>
        /// <remarks>
        /// This and the two below are GameData's ACGItem_t, which is three
        /// numbers: two templates and a level. The client reads them as one
        /// block into the entry's +0x0C.
        /// </remarks>
        [AoMember(4)]
        public int LowId { get; set; }

        /// <summary>
        /// The item's second template.
        /// </summary>
        [AoMember(5)]
        public int HighId { get; set; }

        /// <summary>
        /// The item's level, which ACGItem_t::SetLevel clamps to 0x1FF.
        /// </summary>
        [AoMember(6)]
        public int Quality { get; set; }

        /// <summary>
        /// A fourth number that is always zero.
        /// </summary>
        /// <remarks>
        /// ACGItem_t serializes four ints and only uses three. The writer at
        /// GameData 0x1000E9A7 emits a literal zero here and the reader at
        /// 0x1000E9D7 reads it into a slot nothing looks at again. Zero in all
        /// fifty seven captured entries.
        /// </remarks>
        [AoMember(7)]
        public int Unused { get; set; }
    }
}
