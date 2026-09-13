// --------------------------------------------------------------------------------------------------------------------
// <copyright file="FullCharacterEntry.cs" company="OmniCell">
//   Copyright © 2026 OmniCell contributors.
//   Added to SmokeLounge.AOtomation.Messaging, which is distributed under the
//   Do What The Fuck You Want To Public License, Version 2, as published by
//   Sam Hocevar. See http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the FullCharacterEntry type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace SmokeLounge.AOtomation.Messaging.GameData
{
    using SmokeLounge.AOtomation.Messaging.Serialization;
    using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

    /// <summary>
    /// One entry in the three trailing tables of FullCharacterMessage.
    /// </summary>
    /// <remarks>
    /// Sixteen bytes. In the only populated example captured so far - a
    /// subscribed account, 147 entries - Id and IdRepeated always held the same
    /// value, Marker was 0xFFFFFF02 in every row, and Value was 0 in 146 of
    /// them.
    ///
    /// The ids run from 160 to 10010 and are not contiguous, which is what
    /// skill and stat ids look like rather than a dense index.
    ///
    /// The repetition and the constant marker are recorded as they appear
    /// rather than assumed away: one populated capture is not enough to say
    /// whether Marker is a discriminator that takes other values elsewhere.
    /// </remarks>
    public class FullCharacterEntry
    {
        #region AoMember Properties

        /// <summary>
        /// The research or perk this entry is about.
        /// </summary>
        /// <remarks>
        /// Read by the list loop at 0x10053D93, ahead of the record. The client
        /// keeps the entry only when this matches the id inside the record,
        /// which is why the captures show the same number twice.
        /// </remarks>
        [AoMember(0)]
        public int Id { get; set; }

        /// <summary>
        /// The record's first word, which decides the rest of it.
        /// </summary>
        /// <remarks>
        /// 0x10052D9D reads this and masks it with 0xFFFFFF00. When every one
        /// of those bits is set the word is a marker, its low byte is a form
        /// tag, and two int32s follow. When any of them is clear the word is an
        /// id in its own right and three int32s follow instead.
        ///
        /// Every captured entry is the marker form and carries 0xFFFFFF02.
        /// </remarks>
        [AoMember(1)]
        [AoFlags("researchentry")]
        public int Marker { get; set; }

        /// <summary>
        /// The id again, in the marker form.
        /// </summary>
        [AoMember(2)]
        [AoUsesFlags("researchentry", typeof(int), FlagsCriteria.HasAll, unchecked((int)0xFFFFFF00))]
        public int? IdRepeated { get; set; }

        /// <summary>
        /// The experience still owed on this perk or research line, in the
        /// marker form.
        /// </summary>
        /// <remarks>
        /// 0x10052DE7 reads it straight into + 0x1C of the thirty two byte
        /// slot - the same word ResearchUpdate and PerkUpdate write experience
        /// remaining into, and the eighth, which is why it survives the rep
        /// movsd at 0x10052E3E that copies the seven word definition over
        /// everything before it.
        ///
        /// What it is was confirmed from the reading end on 2026-09-12.
        /// N3Msg_GetPerkProgress at 0x1002781F looks the slot up and returns
        ///
        ///     (definition + 0x18 - slot + 0x1C) / definition + 0x18
        ///
        /// so the definition's + 0x18 is the total and this is what is left of
        /// it. Zero means finished, which is what 146 of the 147 captured
        /// entries carry.
        ///
        /// One qualification, at 0x10052E2F: when the definition's flags at
        /// + 0xC have neither bit 0x40 nor bit 0x80 the client zeroes the field
        /// before anything reads it.
        /// </remarks>
        [AoMember(3)]
        [AoUsesFlags("researchentry", typeof(int), FlagsCriteria.HasAll, unchecked((int)0xFFFFFF00))]
        public int? Value { get; set; }

        /// <summary>
        /// The first of three int32s the plain form carries, and drops.
        /// </summary>
        /// <remarks>
        /// 0x1002BC4D reads three into + 4, + 8 and + 0xC of the slot, and then
        /// both forms converge on 0x10052E1A, which looks the id up in the
        /// research table and copies the seven word definition over the top of
        /// the slot with a rep movsd at 0x10052E3E. So these three are on the
        /// wire and gone before anything can read them.
        ///
        /// No capture contains a plain-form entry - every one of the 147 on the
        /// only account that had any is the marker form - so this is modelled
        /// from the reader alone. Until 2026-09-11 the class read four int32s
        /// unconditionally, which is the marker form; a plain entry would have
        /// left one int32 unread and every entry after it would have been
        /// nonsense.
        /// </remarks>
        [AoMember(4)]
        [AoUsesFlags("researchentry", typeof(int), FlagsCriteria.NotHasAll, unchecked((int)0xFFFFFF00))]
        public int? PlainUnread1 { get; set; }

        /// <summary>
        /// See <see cref="PlainUnread1"/>.
        /// </summary>
        [AoMember(5)]
        [AoUsesFlags("researchentry", typeof(int), FlagsCriteria.NotHasAll, unchecked((int)0xFFFFFF00))]
        public int? PlainUnread2 { get; set; }

        /// <summary>
        /// See <see cref="PlainUnread1"/>.
        /// </summary>
        [AoMember(6)]
        [AoUsesFlags("researchentry", typeof(int), FlagsCriteria.NotHasAll, unchecked((int)0xFFFFFF00))]
        public int? PlainUnread3 { get; set; }

        #endregion
    }
}
