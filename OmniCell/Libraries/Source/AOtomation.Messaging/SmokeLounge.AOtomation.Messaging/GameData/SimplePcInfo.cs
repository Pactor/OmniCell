// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SimplePcInfo.cs" company="SmokeLounge">
//   Copyright © 2013 SmokeLounge.
//   This program is free software. It comes without any warranty, to
//   the extent permitted by applicable law. You can redistribute it
//   and/or modify it under the terms of the Do What The Fuck You Want
//   To Public License, Version 2, as published by Sam Hocevar. See
//   http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the SimplePcInfo type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace SmokeLounge.AOtomation.Messaging.GameData
{
    using SmokeLounge.AOtomation.Messaging.Serialization;
    using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

    public class SimplePcInfo : SimpleCharacterInfo
    {
        #region AoMember Properties

        [AoMember(0)]
        public uint CurrentNano { get; set; }

        [AoMember(1)]
        public int Team { get; set; }

        [AoMember(2)]
        public short Swim { get; set; }

        [AoMember(3)]
        public short StrengthBase { get; set; }

        [AoMember(4)]
        public short AgilityBase { get; set; }

        [AoMember(5)]
        public short StaminaBase { get; set; }

        [AoMember(6)]
        public short IntelligenceBase { get; set; }

        [AoMember(7)]
        public short SenseBase { get; set; }

        [AoMember(8)]
        public short PsychicBase { get; set; }

        [AoMember(9, SerializeSize = ArraySizeType.Int16)]
        public string FirstName { get; set; }

        [AoMember(10, SerializeSize = ArraySizeType.Int16)]
        public string LastName { get; set; }

        /// <summary>
        /// An int32 that precedes OrgName, on clients past version 0x39.
        /// </summary>
        /// <remarks>
        /// Gamecode.dll 0x10079396 compares the version byte the message opened
        /// with against 0x39 and reads this only when it is higher. It sits
        /// immediately before the org name and behind the same flag.
        ///
        /// That used to be as far as it went - "an org id is the obvious
        /// reading, and obvious is not confirmed". The corpus confirms it now,
        /// by correspondence. Across every copy that carries the field there
        /// are 46 distinct values here and 46 distinct org names, and the two
        /// count distributions are the same list element for element: 428,
        /// 234, 68, 68, 53, 47, 34, 27, 27, 22, 20, 19, 18, 17, 14, 13, 11,
        /// 11, 10, 7, 7, 5, four 4s, nine 3s, six 2s and five 1s. 2349057
        /// appears 428 times and so does "Async Helpers"; 1931266 appears 234
        /// times and so does "Chewys Buffs".
        ///
        /// A one to one correspondence with the organisation name, on a field
        /// sitting immediately in front of it behind the same flag, is what an
        /// organisation id is. What it does not settle is whether this is the
        /// same number the server keys organisations by - the correspondence
        /// shows the two identify the same thing, not what the thing is called
        /// elsewhere.
        ///
        /// Null when the message carries no org name at all, and also on a
        /// version 0x39 message that does.
        /// </remarks>
        [AoMember(11)]
        public int? OrgId { get; set; }

        [AoMember(12, SerializeSize = ArraySizeType.Int16)]
        public string OrgName { get; set; }

        #endregion
    }
}