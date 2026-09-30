#region License

// Copyright (c) 2026, OmniCell contributors
//
// This file is part of OmniCell and is distributed under the terms the project
// is licensed under. See the repository root for details.

#endregion

namespace OmniCell.Database.Entities
{
    using OmniCell.Database.Dao;

    /// <summary>
    /// A perk a character has trained.
    /// </summary>
    /// <remarks>
    /// Retail tells the client which perks a character owns in
    /// <c>FullCharacter.ResearchGoals</c>, one entry per perk, and
    /// <b>not</b> in <c>PerkEntries</c> - that array was empty in a capture of
    /// a Keeper who spent 48 minutes pressing five of them. Each entry's Id is
    /// the perk's short id, the same number the client sends back plus ten
    /// thousand when the perk is pressed, and the same one the perk's action
    /// item asks for with <c>HasPerk</c>.
    ///
    /// So one row here is one owned perk, and the short id is the only thing
    /// that has to be stored. See Documentation/Perks-And-Looting.md.
    /// </remarks>
    [Tablename("characterperks")]
    public class DBCharacterPerk : IDBEntity
    {
        public int Id { get; set; }

        /// <summary>
        /// The character that owns it.
        /// </summary>
        public int CharacterId { get; set; }

        /// <summary>
        /// The perk's short id, as ResearchGoals carries it.
        /// </summary>
        public int PerkId { get; set; }
    }
}
