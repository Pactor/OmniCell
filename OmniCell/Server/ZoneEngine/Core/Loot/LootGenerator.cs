namespace ZoneEngine.Core.Loot
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using OmniCell.Core.Entities;
    using OmniCell.Core.Inventory;
    using OmniCell.Core.Items;
    using OmniCell.Database.Dao;
    using OmniCell.Database.Entities;
    using OmniCell.Enums;

    /// <summary>
    /// Builds transient corpse inventories entirely from database rules.
    /// </summary>
    public static class LootGenerator
    {
        private static readonly object RandomLock = new object();

        private static readonly Random Random = new Random();

        public static int Fill(CorpseLoot corpse, ICharacter victim)
        {
            if (corpse == null || victim == null || victim.Playfield == null)
            {
                return 0;
            }

            int added = 0;
            int playfield = victim.Playfield.Identity.Instance;
            IEnumerable<DBMobLootProfile> profiles = MobLootProfileDao.Instance.GetAll()
                .Where(p => (p.Playfield == 0 || p.Playfield == playfield)
                            && p.Rolls > 0
                            && p.Chance > 0
                            && string.Equals(p.MobName, victim.Name, StringComparison.Ordinal));

            foreach (DBMobLootProfile profile in profiles)
            {
                List<DBMobDroptable> pool = MobDroptableDao.Instance.GetWhere(new { Hash = profile.DropHash })
                    .Where(d => d.RangeCheck == 0
                                || (victim.Stats[StatIds.level].Value >= d.MinQl
                                    && victim.Stats[StatIds.level].Value <= d.MaxQl))
                    .ToList();

                for (int roll = 0; roll < profile.Rolls && pool.Count != 0; roll++)
                {
                    DBMobDroptable selected;
                    int quality;
                    lock (RandomLock)
                    {
                        if (Random.Next(10000) >= Math.Min(10000, profile.Chance))
                        {
                            continue;
                        }

                        selected = pool[Random.Next(pool.Count)];
                        int lowQl = Math.Min(selected.MinQl, selected.MaxQl);
                        int highQl = Math.Max(selected.MinQl, selected.MaxQl);
                        quality = lowQl == highQl ? lowQl : Random.Next(lowQl, highQl + 1);
                    }

                    ItemTemplate template;
                    if (!ItemLoader.ItemList.TryGetValue(selected.LowId, out template)
                        && !ItemLoader.ItemList.TryGetValue(selected.HighId, out template))
                    {
                        continue;
                    }

                    var item = new Item(quality, template.GetLowId(quality), template.GetHighId(quality));
                    if (corpse.BaseInventory.TryAdd(item) == InventoryError.OK)
                    {
                        added++;
                    }
                }
            }

            return added;
        }
    }
}
