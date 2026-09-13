// --------------------------------------------------------------------------------------------------------------------
// <copyright file="NanoEffect.cs" company="SmokeLounge">
//   Copyright © 2013 SmokeLounge.
//   This program is free software. It comes without any warranty, to
//   the extent permitted by applicable law. You can redistribute it
//   and/or modify it under the terms of the Do What The Fuck You Want
//   To Public License, Version 2, as published by Sam Hocevar. See
//   http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the NanoEffect type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace SmokeLounge.AOtomation.Messaging.GameData
{
    /// <summary>
    /// One thing a nano does.
    /// </summary>
    /// <remarks>
    /// The identity is not a nano: its Type is the game function the effect
    /// runs - CastNano, Hit, Set, GfxEffect and so on, the same numbering
    /// FunctionType uses - and its Instance is the nano it belongs to.
    ///
    /// Criteria are the requirements that have to hold for the effect to apply,
    /// and Arguments are what the function was called with. Both were missing
    /// from this class, which is why nothing about SpellList read correctly.
    /// </remarks>
    public class NanoEffect
    {
        #region AoMember Properties

        /// <summary>
        /// Type is the game function, Instance the nano.
        /// </summary>
        /// <remarks>
        /// The client calls the record GameData::SpellData_t, and
        /// SpellData_t::GetIdentity in GameData.dll returns the address of the
        /// object's twelfth byte - so the first two words of the record are one
        /// Identity, which is what this already was. SpellData_t::IsValid
        /// confirms the type half is the function: it compares it against
        /// 0xCF0A, which is 53002, the Hit function.
        /// </remarks>
        public Identity Effect { get; set; }

        /// <summary>
        /// The record's version. 4 in all 2,738 captured effects.
        /// </summary>
        /// <remarks>
        /// SpellData_t keeps its identity and its version as one three-word
        /// block, set and read together by SetSpellHeader and GetSpellHeader at
        /// GameData.dll 0x1000CC16 and 0x1000CC2F. GetVersion at 0x100010DB
        /// returns the third of those words, and the third word is this field -
        /// the Identity above is the first two.
        /// </remarks>
        public int Version { get; set; }

        /// <summary>
        /// How many entries <see cref="Criteria"/> holds.
        /// </summary>
        public int CriterionCount { get; set; }

        /// <summary>
        /// The requirements on the effect, three ints each.
        /// </summary>
        public NanoCriterion[] Criteria { get; set; }

        /// <summary>
        /// How many times the effect happens. 1 in every captured effect.
        /// </summary>
        /// <remarks>
        /// This and the three below it are the four arguments every format
        /// carries, because SpellFormat_c's constructor at GameData.dll
        /// 0x1000FA93 adds them itself before any function's own: SpellStat 3
        /// defaulting to 1, SpellStat 4 to 0, SpellStat 32 to 0 and SpellStat 35
        /// to -1. They are fields here rather than table entries for that
        /// reason - they belong to the record, not to any one function.
        ///
        /// SpellStat 3 is a count, and what settles it is the client working out
        /// what an effect is worth: at Gamecode.dll 0x100A4A8D a function 53006
        /// effect is estimated as the size of <see cref="Amount"/> times this.
        /// </remarks>
        public int Hits { get; set; }

        /// <summary>
        /// How much the effect does. 0 in every captured effect.
        /// </summary>
        /// <remarks>
        /// SpellStat 4. Recorded as a delay for years, and it is not one -
        /// nothing in the client treats it as a time. The two places that read
        /// it both take its absolute value and use it as a magnitude: 0x100A4A8D
        /// multiplies it by <see cref="Hits"/> for function 53006, and
        /// 0x100A4A66 adds it to the effect's own fifth argument for 53014. A
        /// delay would not be negated and would not be multiplied by a count.
        ///
        /// Zero in all 5,171 captured effects, which are buffs and mezzes whose
        /// magnitude is in their own arguments rather than here.
        /// </remarks>
        public int Amount { get; set; }

        /// <summary>
        /// Who the effect applies to. 2 in 3,073 captured effects, 0 in 1,951.
        /// </summary>
        /// <remarks>
        /// SpellStat 32, and the only one of the four the format gives a
        /// ComplexType of its own - 4, where the rest are 0. The client names
        /// the values itself: the item description builder at Gamecode.dll
        /// 0x100215C1 fetches a heading for each new value with
        /// LDBface::GetText(506, value) and prints it above the effects that
        /// follow, and category 506 of the client's text database holds On
        /// Self, On User, On Target, On Item and On Fighting Target at ids 1, 2,
        /// 3, 4 and 14.
        ///
        /// The rest of what the client does with it fits: it sorts spells by it
        /// at 0x100179B1, and it forces every function 53033 effect it finds to
        /// On Target at 0x10080E56.
        /// </remarks>
        public NanoEffectTarget Target { get; set; }

        /// <summary>
        /// Which of the holder's spell lists the effect belongs to, or -1.
        /// </summary>
        /// <remarks>
        /// SpellStat 35, and the one the message is named after. The holder
        /// keeps an array of lists at its own +0x24, indexed by this value -
        /// Gamecode.dll 0x100020FD is the one-line accessor and 0x1000210D the
        /// one that creates the list if it is not there yet. 0x10002637 reads
        /// this field off an effect and skips the effect when it is -1, which is
        /// the default SpellFormat_c gives it, and the client walks indices 0 to
        /// 28 whenever it wants all of them, so there are 29 lists.
        ///
        /// 9 in 3,220 captured effects and 0 in 1,951.
        /// </remarks>
        public int SpellList { get; set; }

        /// <summary>
        /// What the function was called with.
        /// </summary>
        /// <remarks>
        /// Kept as bytes rather than named fields. How many arguments there are
        /// and which of them is a string is settled - it comes from the client's
        /// own format table, <see cref="NanoEffectFormats"/> - but what each one
        /// means is per function, and there are 232 of them.
        /// </remarks>
        public byte[] Arguments { get; set; }

        #endregion
    }
}
