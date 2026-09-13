namespace OmniCell.Database.Dao
{
    using OmniCell.Database.Entities;

    /// <summary>
    /// Connects an exact captured mob name to an ordinary loot pool.
    /// </summary>
    [Tablename("moblootprofiles")]
    public class DBMobLootProfile : IDBEntity
    {
        public int Id { get; set; }

        /// <summary>Zero means every playfield.</summary>
        public int Playfield { get; set; }

        public string MobName { get; set; }

        public string DropHash { get; set; }

        public int Rolls { get; set; }

        /// <summary>Chance per roll in basis points (10000 = 100%).</summary>
        public int Chance { get; set; }
    }
}
