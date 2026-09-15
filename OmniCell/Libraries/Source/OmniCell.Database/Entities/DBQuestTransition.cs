namespace OmniCell.Database.Entities
{
    using OmniCell.Database.Dao;

    /// <summary>
    /// A quest stage granted the moment another one finishes - the next stage of a chain.
    /// </summary>
    [Tablename("questtransitions")]
    public class DBQuestTransition : IDBEntity
    {
        public int Id { get; set; }

        public int FromQuest { get; set; }

        public int ToQuest { get; set; }

        public int Ordinal { get; set; }
    }
}
