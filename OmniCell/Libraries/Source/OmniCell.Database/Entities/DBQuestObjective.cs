#region License

// Copyright (c) 2005-2014, CellAO Team
// Copyright (c) 2026, OmniCell contributors
// 
// 
// All rights reserved.
// 
// 
// Redistribution and use in source and binary forms, with or without modification, are permitted provided that the following conditions are met:
// 
// 
//     * Redistributions of source code must retain the above copyright notice, this list of conditions and the following disclaimer.
//     * Redistributions in binary form must reproduce the above copyright notice, this list of conditions and the following disclaimer in the documentation and/or other materials provided with the distribution.
//     * Neither the name of the CellAO Team nor the names of its contributors may be used to endorse or promote products derived from this software without specific prior written permission.
// 
// 
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
// "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT
// LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR
// A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR
// CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL,
// EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO,
// PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR
// PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF
// LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING
// NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
// SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
// 

#endregion

namespace OmniCell.Database.Entities
{
    #region Usings ...

    using OmniCell.Database.Dao;

    #endregion

    /// <summary>
    /// One thing a quest asks for.
    /// </summary>
    /// <remarks>
    /// Objectives are held separately from the quest so that a quest can ask
    /// for more than one thing, and so that progress can be counted per
    /// objective rather than per quest.
    /// </remarks>
    [Tablename("questobjectives")]
    public class DBQuestObjective : IDBEntity
    {
        /// <summary>
        /// Surrogate key. The natural key is QuestId and Ordinal.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// The quest this belongs to.
        /// </summary>
        public int QuestId { get; set; }

        /// <summary>
        /// Position within the quest, from 0.
        /// </summary>
        public int Ordinal { get; set; }

        /// <summary>
        /// What has to be done. See QuestObjectiveType.
        /// </summary>
        public int ObjectiveType { get; set; }

        /// <summary>
        /// What it has to be done to - a mob name for a kill objective.
        /// </summary>
        public string Target { get; set; }

        /// <summary>
        /// Exact low template identity for an item objective, or zero when the
        /// objective predates exact item matching.
        /// </summary>
        public int TargetLowId { get; set; }

        /// <summary>
        /// Exact high template identity for an item objective, or zero when the
        /// objective predates exact item matching.
        /// </summary>
        public int TargetHighId { get; set; }

        /// <summary>
        /// Exact item quality required by an item objective, or zero when the
        /// objective predates exact item matching.
        /// </summary>
        public int TargetQuality { get; set; }

        /// <summary>
        /// How many times.
        /// </summary>
        public int Required { get; set; }
    }
}
