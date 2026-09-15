namespace OmniCell.Database.Entities
{
    using OmniCell.Database.Dao;

    /// <summary>
    /// What a kind of playfield fixture does when a player uses it: a Gas Fire goes out and is lit
    /// again later, the quest Cargo Box opens and comes back.
    /// </summary>
    [Tablename("fixturebehaviours")]
    public class DBFixtureBehaviour : IDBEntity
    {
        public int Id
        {
            get
            {
                return this.Template;
            }

            set
            {
                this.Template = value;
            }
        }

        public int Template { get; set; }

        public int DespawnOnUse { get; set; }

        public int RespawnSeconds { get; set; }

        /// <summary>
        /// The FormatFeedback text the player is sent, exactly as captured ("~&..."), or empty.
        /// </summary>
        public string Feedback { get; set; }
    }
}
