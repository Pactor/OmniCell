namespace OmniCell.Database.Entities
{
    using OmniCell.Database.Dao;

    /// <summary>
    /// An inventory item granted when a quest is accepted or handed in.
    /// </summary>
    [Tablename("questitemrewards")]
    public class DBQuestItemReward : IDBEntity
    {
        public int Id { get; set; }

        public int QuestId { get; set; }

        public int ItemId { get; set; }

        public int Quantity { get; set; }

        public int GrantOnAccept { get; set; }
    }
}
