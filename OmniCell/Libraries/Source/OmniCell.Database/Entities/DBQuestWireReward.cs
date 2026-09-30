namespace OmniCell.Database.Entities
{
    using OmniCell.Database.Dao;

    /// <summary>
    /// One item displayed in a captured quest reward box.
    /// </summary>
    [Tablename("questwirerewards")]
    public class DBQuestWireReward : IDBEntity
    {
        public int Id { get; set; }
        public int QuestId { get; set; }
        public int Ordinal { get; set; }
        public int LowId { get; set; }
        public int HighId { get; set; }
        public int Quality { get; set; }
        /// <summary>
        /// QuestItemShort.Unused: the ACGItem_t's reserved zero.
        /// </summary>
        public int Unused { get; set; }
    }
}
