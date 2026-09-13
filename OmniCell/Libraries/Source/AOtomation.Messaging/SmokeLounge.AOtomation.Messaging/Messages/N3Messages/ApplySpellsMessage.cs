// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ApplySpellsMessage.cs" company="OmniCell">
//   Copyright © 2026 OmniCell contributors.
//   Added to SmokeLounge.AOtomation.Messaging, which is distributed under the
//   Do What The Fuck You Want To Public License, Version 2, as published by
//   Sam Hocevar. See http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the ApplySpellsMessage type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace SmokeLounge.AOtomation.Messaging.Messages.N3Messages
{
    using SmokeLounge.AOtomation.Messaging.GameData;
    using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

    /// <summary>
    /// Effects going on to a character, or coming back off.
    /// </summary>
    /// <remarks>
    /// Three fields, and the third is the one worth having. The list is the
    /// same GameData SpellData_t record SpellList and CorpseFullUpdate carry,
    /// read by the same function - Gamecode.dll 0x100A71AE - so it costs
    /// nothing here beyond calling the shared reader.
    ///
    /// The trailing byte was recorded as an unresolved boolean for as long as
    /// this message had no class at all, and the reason it looked unresolvable
    /// is that the dispatcher at 0x10128E45 inverts it before passing it on,
    /// so reading the dispatcher alone tells you the sense is backwards from
    /// something without telling you what. Two calls further on it settles:
    /// the inverted value becomes bit 0 of each applied effect's flag word,
    /// and an effect with that bit set subtracts its amount from the target's
    /// stat where an effect without it adds - Gamecode.dll 0x100A4F6C is the
    /// whole of it, one branch around a sub and an add. Effects that do
    /// something once rather than modify a stat check the same bit and do
    /// nothing when it is set, at 0x100A52D9.
    ///
    /// So the byte says which direction this message goes in: true puts the
    /// listed effects on, false takes them off.
    /// </remarks>
    [AoContract((int)N3MessageType.ApplySpells)]
    public class ApplySpellsMessage : N3Message
    {
        #region Constructors and Destructors

        public ApplySpellsMessage()
        {
            this.N3MessageType = N3MessageType.ApplySpells;
        }

        #endregion

        #region AoMember Properties

        /// <summary>
        /// The effects being applied or removed.
        /// </summary>
        [AoMember(0)]
        public NanoEffect[] NanoEffects { get; set; }

        /// <summary>
        /// Who they apply to.
        /// </summary>
        /// <remarks>
        /// The dispatcher resolves this identity in the playfield and casts the
        /// result to SimpleChar_t, so this is a character and not an item.
        /// </remarks>
        [AoMember(1)]
        public Identity Target { get; set; }

        /// <summary>
        /// True puts the effects on, false takes them off.
        /// </summary>
        [AoMember(2)]
        public bool Apply { get; set; }

        #endregion
    }
}
