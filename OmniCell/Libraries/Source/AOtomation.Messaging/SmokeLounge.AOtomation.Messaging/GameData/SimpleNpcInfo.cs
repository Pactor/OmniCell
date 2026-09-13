// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SimpleNpcInfo.cs" company="SmokeLounge">
//   Copyright © 2013 SmokeLounge.
//   This program is free software. It comes without any warranty, to
//   the extent permitted by applicable law. You can redistribute it
//   and/or modify it under the terms of the Do What The Fuck You Want
//   To Public License, Version 2, as published by Sam Hocevar. See
//   http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the SimpleNpcInfo type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace SmokeLounge.AOtomation.Messaging.GameData
{
    using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

    public class SimpleNpcInfo : SimpleCharacterInfo
    {
        #region AoMember Properties

        [AoMember(0)]
        public short Family { get; set; }

        [AoMember(1)]
        public short LosHeight { get; set; }

        /// <summary>
        /// What kind of pet this is - stat 512.
        /// </summary>
        /// <remarks>
        /// A byte when SimpleCharFullUpdateFlags.UnknownDataFlag is set and a
        /// short when it is not, which is the same widening
        /// <see cref="Family"/> and <see cref="LosHeight"/> get from their own
        /// flags.
        ///
        /// Named on 2026-09-11 from what the dispatcher does with it. The three
        /// NPC fields land at the message's + 0xB2, + 0xB4 and + 0xB6 in that
        /// order, and the dispatcher pushes them into the stat table at
        /// [character + 0xE8] with 0x1C7, 0x1D2 and 0x200 - 455 npcfamily, 466
        /// losheight and 512 pettype. The first two confirm the names this
        /// class already had, which is what makes the third worth trusting.
        /// </remarks>
        [AoMember(2)]
        public short PetType { get; set; }

        /// <summary>
        /// Follows <see cref="Unknown1"/>. Zero in every captured NPC so far.
        /// </summary>
        [AoMember(3)]
        public short Unknown2 { get; set; }

        /// <summary>
        /// A byte that follows Unknown2, and only when Unknown2 is positive.
        /// </summary>
        /// <remarks>
        /// Gamecode.dll 0x10079452 compares the short it has just read against
        /// zero and reads one more byte when it is above it. The comment this
        /// replaces said "if greater than 0 add another byte" and then did not
        /// add it, so every NPC that had one came out a byte short from there
        /// on.
        /// </remarks>
        [AoMember(4)]
        public byte? Unknown3 { get; set; }

        #endregion
    }
}