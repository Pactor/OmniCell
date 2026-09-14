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

            // Only this mob's profiles. Reading the whole table on every kill filtered it in memory.
            IEnumerable<DBMobLootProfile> profiles = MobLootProfileDao.Instance.GetWhere(new { MobName = victim.Name })
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

                    Item item = CreateItem(template, quality);
                    if (item != null && corpse.BaseInventory.TryAdd(item) == InventoryError.OK)
                    {
                        added++;
                    }
                }
            }

            return added;
        }

        /// <summary>
        /// An item of the template's family at the rolled quality.
        /// </summary>
        /// <remarks>
        /// A drop row's quality range is its own, not the item family's. Rolled below every related
        /// template GetLowId answers -1, rolled above every one GetHighId answers 1234567890, and the
        /// Item constructor throws for either - which, thrown out of a kill, stopped the mob from ever
        /// respawning. The side that was not found takes the other side's template (or this template),
        /// and the Item constructor clamps the quality into that template's range.
        /// </remarks>
        private static Item CreateItem(ItemTemplate template, int quality)
        {
            int lowId = template.GetLowId(quality);
            int highId = template.GetHighId(quality);
            bool lowKnown = ItemLoader.ItemList.ContainsKey(lowId);
            bool highKnown = ItemLoader.ItemList.ContainsKey(highId);

            if (!lowKnown && !highKnown)
            {
                lowId = template.ID;
                highId = template.ID;
            }
            else if (!lowKnown)
            {
                lowId = highId;
            }
            else if (!highKnown)
            {
                highId = lowId;
            }

            return ItemLoader.ItemList.ContainsKey(lowId) ? new Item(quality, lowId, highId) : null;
        }
    }
}
