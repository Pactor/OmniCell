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
    /// A quest definition.
    /// </summary>
    /// <remarks>
    /// Ids are the quest identity instances the live server uses, so a quest
    /// imported from a capture keeps the number the client already knows it by.
    /// </remarks>
    [Tablename("quests")]
    public class DBQuest : IDBEntity
    {
        /// <summary>
        /// The quest identity's instance, as the live server uses it.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// The short name shown in the quest list.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The full text, markup included, exactly as the client expects it.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Instance of a character the quest is associated with. NOT reliably
        /// the one who hands it out.
        /// </summary>
        /// <remarks>
        /// Taken from QuestInfo.Unknown5, which was assumed to be the giver when
        /// the quests were imported. Loading them proved that wrong: 13 of the
        /// Arete Landing quests come back attributed to Rex Larsson, including
        /// "Talk to Marcus Stone" and "Report to Alex", which are plainly not
        /// his. Four distinct values cover all 55 quests, so it is something
        /// coarser - most likely whoever begins the chain, since Rex begins the
        /// Identity Crisis arc that most of those belong to.
        ///
        /// AreaExtract now correlates a newly appearing QuestFullUpdate entry
        /// with an open KnuBot conversation where that sequence was captured.
        /// Entries already present in the first log of a session still have no
        /// capture-backed handover NPC.
        ///
        /// Until then anything that treats this as the giver is approximately
        /// right at the level of a quest chain and wrong at the level of a
        /// quest.
        /// </remarks>
        public int GiverId { get; set; }

        /// <summary>
        /// Mission icon.
        /// </summary>
        public int IconId { get; set; }

        /// <summary>
        /// </summary>
        public int CashReward { get; set; }

        /// <summary>
        /// </summary>
        public int ExperienceReward { get; set; }

        /// <summary>
        /// Where the quest belongs. Not a constraint, just a way to find them.
        /// </summary>
        public int Playfield { get; set; }

        /// <summary>
        /// The quest that has to be handed in before this one is offered, or
        /// zero when it is the first of its chain.
        /// </summary>
        /// <remarks>
        /// A giver holds a chain, not a menu. Rex Larsson has you kill his
        /// robots and then open his cargo box, and offering both at once - and
        /// the twenty two errands that follow from other people - is not how any
        /// of it reads.
        ///
        /// Within one captured run, the order comes from the sequence in which
        /// quests appeared in the log. Separate runs can leave unordered gaps;
        /// AreaExtract uses a documented OmniCell reachability tie-break for
        /// those gaps. Requires is therefore emulator-owned representation,
        /// not a field claimed to come directly from the retail protocol.
        /// </remarks>
        public int Requires { get; set; }
    }
}
