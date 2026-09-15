namespace OmniCell.Database.Entities
{
    using OmniCell.Database.Dao;

    /// <summary>
    /// One row of a character's conversation: a line it says, an answer the player can give, or
    /// something the conversation does (grant a quest stage, open a trade window).
    /// </summary>
    /// <remarks>
    /// Rows with the same Node make one step, in Ordinal order. Node -1 holds the character's
    /// farewell - what it says when the player says goodbye. See knubotopeners for which node a
    /// conversation starts on, and Documentation/Quest-System.md for the model.
    /// </remarks>
    [Tablename("knubotdialogue")]
    public class DBKnuBotDialogue : IDBEntity
    {
        public int Id { get; set; }

        public int Playfield { get; set; }

        public string NpcName { get; set; }

        public int Node { get; set; }

        public int Ordinal { get; set; }

        /// <summary>
        /// 0 says Text, 1 offers Text as an answer, 2 grants quest stage ActionValue, 3 opens a trade
        /// window with Flag slots and Text as its message, continuing at Next once it succeeds.
        /// </summary>
        public int Kind { get; set; }

        public string Text { get; set; }

        /// <summary>
        /// For a line, the client's emote flag (a narrated line, "Rex lowers his voice."); for a
        /// trade, the number of slots.
        /// </summary>
        public int Flag { get; set; }

        /// <summary>
        /// For an answer, the node it leads to (0: none captured); for a trade, the node after it.
        /// </summary>
        public int Next { get; set; }

        /// <summary>
        /// For an answer: 0 moves on to Next, 1 is a question - Next holds the reply, and the answer
        /// list comes back without it - and 2 says goodbye.
        /// </summary>
        public int AnswerKind { get; set; }

        public int ActionValue { get; set; }
    }
}
