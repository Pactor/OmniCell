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
    /// A stat a quest changes on the character who finishes it.
    /// </summary>
    /// <remarks>
    /// Cash, experience and items were the only things a quest could give,
    /// and some of what the world checks for is neither. Arete Landing's exit
    /// is the case that wanted this: the door out asks for bit 16384 of stat
    /// 685 - the ID card the whole tutorial chain is about making - and
    /// nothing could set it, so the chain ended and the player stayed.
    ///
    /// The shape is deliberately small. A quest names a stat, a value, and
    /// whether the value is bits to set or the whole new value.
    /// </remarks>
    [Tablename("queststatrewards")]
    public class DBQuestStatReward : IDBEntity
    {
        public int Id { get; set; }

        public int QuestId { get; set; }

        /// <summary>
        /// The stat to change.
        /// </summary>
        public int Stat { get; set; }

        /// <summary>
        /// The bits to set, or the value to write.
        /// </summary>
        public int Value { get; set; }

        /// <summary>
        /// 1 to or the value into what is there, 0 to replace it.
        /// </summary>
        /// <remarks>
        /// Setting bits is the usual case, because the stats worth granting
        /// this way are flag words that several things write to.
        /// </remarks>
        public int SetBits { get; set; }

        /// <summary>
        /// 1 to grant when the quest is taken rather than when it is done.
        /// </summary>
        public int GrantOnAccept { get; set; }
    }
}
