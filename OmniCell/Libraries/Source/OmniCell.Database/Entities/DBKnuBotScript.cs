#region License

// Copyright (c) 2025, OmniCell
//
// All rights reserved.
//
// Redistribution and use in source and binary forms, with or without modification, are permitted provided that the following conditions are met:
//
//     * Redistributions of source code must retain the above copyright notice, this list of conditions and the following disclaimer.
//     * Redistributions in binary form must reproduce the above copyright notice, this list of conditions and the following disclaimer in the documentation and/or other materials provided with the distribution.
//     * Neither the name of the OmniCell Team nor the names of its contributors may be used to endorse or promote products derived from this software without specific prior written permission.
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

#endregion

namespace OmniCell.Database.Entities
{
    #region Usings ...

    using OmniCell.Database.Dao;

    #endregion

    /// <summary>
    /// One line of what a character says, or one answer it offers.
    /// </summary>
    /// <remarks>
    /// Recovered from captures of the live server, so the words are Funcom's
    /// and none of them is written by us. That matters more than it sounds:
    /// the first thing anybody noticed about the quest givers was that Rex
    /// Larsson was reading four lines of invented text and a menu of quest
    /// names instead of his own conversation.
    ///
    /// A conversation is a sequence of steps. Inside a step the Kind 0 rows are
    /// what the character says, in Ordinal order, and the Kind 1 rows are the
    /// answers the client is offered. Action 0 preserves the captured rule in
    /// which the final answer leaves and any other advances. Authored answers
    /// can instead carry an explicit quest or trade action.
    /// </remarks>
    [Tablename("knubotscript")]
    public class DBKnuBotScript : IDBEntity
    {
        #region Public Properties

        public int Id { get; set; }

        /// <summary>
        /// The spawn that says it.
        /// </summary>
        public int Npc { get; set; }

        public int Playfield { get; set; }

        /// <summary>
        /// Which step of the conversation this belongs to.
        /// </summary>
        public int Step { get; set; }

        /// <summary>
        /// Where in the whole conversation this line falls. Sorting by it puts
        /// a character's words back in the order they were said.
        /// </summary>
        public int Ordinal { get; set; }

        /// <summary>
        /// 0 for something the character says, 1 for an answer it offers.
        /// </summary>
        public int Kind { get; set; }

        public string Text { get; set; }

        /// <summary>
        /// Non-zero on the step where a capture saw a quest change hands.
        /// </summary>
        /// <remarks>
        /// Which quest is not read from here. A step can hand over more than
        /// one - Rex Larsson's "Excellent choice" step granted three in one
        /// capture - and which one a particular player gets depends on how far
        /// along the chain they are. What this says is where in the
        /// conversation the handover happens.
        /// </remarks>
        public int Grants { get; set; }

        /// <summary>
        /// The ScriptAction performed when this answer is selected. Grants is
        /// the quest id used by quest actions. Zero preserves legacy scripts.
        /// </summary>
        public int Action { get; set; }

        #endregion
    }
}
