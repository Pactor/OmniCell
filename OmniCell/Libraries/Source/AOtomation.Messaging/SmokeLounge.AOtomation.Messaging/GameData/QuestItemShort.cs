using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SmokeLounge.AOtomation.Messaging.GameData
{
    using System.Dynamic;

    using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

    public class QuestItemShort
    {
        [AoMember(0)]
        public int LowId { get; set; }
        [AoMember(1)]
        public int HighId { get; set; }
    [AoMember(2)]
        public int Quality { get; set; }

        /// <summary>
        /// The ACGItem_t's reserved fourth number: the writer pushes a literal zero and the reader drops
        /// it (see AcgItem.Unused).
        /// </summary>
        [AoMember(3)]
        public int Unused { get; set; }
    }
}
