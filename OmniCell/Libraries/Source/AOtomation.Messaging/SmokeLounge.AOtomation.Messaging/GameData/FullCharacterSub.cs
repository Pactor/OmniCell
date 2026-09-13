using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SmokeLounge.AOtomation.Messaging.GameData
{
    using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

    public class FullCharacterSub
    {
        /// <summary>
        /// The first of three bytes.
        /// </summary>
        /// <remarks>
        /// The element reader at Gamecode 0x1002D1A6 takes three bytes and
        /// nothing else. Where they end up - an eight byte holder at character
        /// +0x1CC - has no exported reader, so none of the three is named yet.
        /// Every captured FullCharacter has an empty list.
        /// </remarks>
        [AoMember(1)]
        public byte Unknown1 { get; set; }
        [AoMember(2)]
        public byte Unknown2 { get; set; }
        [AoMember(3)]
        public byte Unknown3 { get; set; }
    }
}
