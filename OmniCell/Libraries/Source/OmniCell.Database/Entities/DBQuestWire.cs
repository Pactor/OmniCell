namespace OmniCell.Database.Entities
{
    using OmniCell.Database.Dao;

    /// <summary>
    /// Quest-specific fields used by the client's QuestInfo record.
    /// </summary>
    [Tablename("questwire")]
    public class DBQuestWire : IDBEntity
    {
        public int Id { get { return this.QuestId; } set { this.QuestId = value; } }
        public int QuestId { get; set; }
        public string Source { get; set; }
        public int GiverType { get; set; }
        public int GiverInstance { get; set; }
        public int QuestCode { get; set; }
        public int UnknownHash { get; set; }
        public int Quality { get; set; }
        public int TimeLimit { get; set; }
        public int Unknown20 { get; set; }
        public int Unknown21 { get; set; }
        public int Unknown22 { get; set; }
        public int Unknown23Type { get; set; }
        public int Unknown23Instance { get; set; }
        public int Unknown25 { get; set; }
        public int Unknown26 { get; set; }
    }
}
