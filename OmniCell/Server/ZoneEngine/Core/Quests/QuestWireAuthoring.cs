namespace ZoneEngine.Core.Quests
{
    using System;
    using System.Globalization;
    using System.Linq;

    using OmniCell.Core.Items;
    using OmniCell.Database.Dao;
    using OmniCell.Database.Entities;

    /// <summary>
    /// Creates an explicitly OmniCell-defined client representation for quests
    /// authored by a GM. Captured quests never pass through this class.
    /// </summary>
    public static class QuestWireAuthoring
    {
        private const int GenericTrackingType = 54001;
        private const int PlayfieldIdentityType = 40016;

        public static void EnsureQuest(DBQuest quest)
        {
            if (quest == null || QuestWireDao.Instance.GetWhere(new { QuestId = quest.Id }).Any())
            {
                return;
            }

            QuestWireDao.Instance.Add(
                new DBQuestWire
                    {
                        QuestId = quest.Id,
                        Source = "OmniCellDefined",
                        GiverType = 50000,
                        GiverInstance = quest.GiverId,
                        QuestCode = FourCharacterCode(quest.Id),
                        UnknownHash = 0,
                        Quality = 0,
                        TimeLimit = 0,
                        Unknown20 = 6,
                        Unknown21 = 0,
                        Unknown22 = 0,
                        Unknown23Type = 0,
                        Unknown23Instance = 0,
                        Unknown25 = 0,
                        Unknown26 = 7
                    });
        }

        public static void RebuildAction(DBQuest quest, DBQuestObjective objective)
        {
            if (quest == null)
            {
                return;
            }

            EnsureQuest(quest);
            DBQuestWire existing = QuestWireDao.Instance.GetWhere(new { QuestId = quest.Id }).FirstOrDefault();
            if (existing == null || !string.Equals(
                    existing.Source,
                    "OmniCellDefined",
                    StringComparison.Ordinal))
            {
                return;
            }

            QuestWireActionDao.Instance.Delete(new { QuestId = quest.Id });
            if (objective == null)
            {
                return;
            }

            float x = 0;
            float y = 0;
            float z = 0;
            DBMobSpawn giver = MobSpawnDao.Instance.Get(quest.GiverId);
            if (giver != null)
            {
                x = giver.X;
                y = giver.Y;
                z = giver.Z;
            }

            if (objective.ObjectiveType == (int)OmniCell.Enums.QuestObjectiveType.Reach)
            {
                string[] parts = (objective.Target ?? string.Empty).Split(',');
                float parsedX;
                float parsedZ;
                if (parts.Length == 2
                    && float.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out parsedX)
                    && float.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out parsedZ))
                {
                    x = parsedX;
                    y = 0;
                    z = parsedZ;
                }
            }
            else if (objective.ObjectiveType == (int)OmniCell.Enums.QuestObjectiveType.Kill
                     || objective.ObjectiveType == (int)OmniCell.Enums.QuestObjectiveType.TalkTo)
            {
                DBMobSpawn target = MobSpawnDao.Instance.GetWhere(
                    new { Playfield = quest.Playfield, Name = objective.Target }).FirstOrDefault();
                if (target != null)
                {
                    x = target.X;
                    y = target.Y;
                    z = target.Z;
                }
            }
            else if (objective.ObjectiveType == (int)OmniCell.Enums.QuestObjectiveType.Use)
            {
                int instance;
                if (int.TryParse(objective.Target, out instance))
                {
                    DBStaticDynel target = StaticDynelDao.Instance.GetWhere(
                        new { Playfield = quest.Playfield, Instance = instance }).FirstOrDefault();
                    if (target != null)
                    {
                        x = target.X;
                        y = target.Y;
                        z = target.Z;
                    }
                }
            }

            QuestWireActionDao.Instance.Add(
                new DBQuestWireAction
                    {
                        QuestId = quest.Id,
                        Ordinal = 0,
                        Version = 24,
                        TrackingType = GenericTrackingType,
                        PlayfieldType = PlayfieldIdentityType,
                        PlayfieldInstance = quest.Playfield,
                        Unknown18 = 100000,
                        Unknown19 = 100000,
                        X = x,
                        Y = y,
                        Z = z
                    });

            DBQuestWire wire = QuestWireDao.Instance.GetWhere(new { QuestId = quest.Id }).First();
            wire.Unknown21 = objective.Required > 1 ? objective.Required : 0;
            OmniCell.Database.SqlMapperUtil.InsertUpdateOrDeleteSql(
                "UPDATE questwire SET Unknown21=@Unknown21 WHERE QuestId=@QuestId",
                new { wire.QuestId, wire.Unknown21 });
        }

        public static void RebuildRewards(int questId)
        {
            DBQuestWire wire = QuestWireDao.Instance.GetWhere(new { QuestId = questId }).FirstOrDefault();
            if (wire == null || !string.Equals(wire.Source, "OmniCellDefined", StringComparison.Ordinal))
            {
                return;
            }

            QuestWireRewardDao.Instance.Delete(new { QuestId = questId });
            int ordinal = 0;
            foreach (DBQuestItemReward reward in QuestItemRewardDao.Instance.GetWhere(new { QuestId = questId })
                .Where(r => r.GrantOnAccept == 0)
                .OrderBy(r => r.Id))
            {
                ItemTemplate selected;
                if (!ItemLoader.ItemList.TryGetValue(reward.ItemId, out selected))
                {
                    continue;
                }

                int lowId = selected.GetLowId(selected.Quality);
                int highId = selected.GetHighId(selected.Quality);
                if (selected.Quality < 1 || !ItemLoader.ItemList.ContainsKey(lowId)
                    || !ItemLoader.ItemList.ContainsKey(highId))
                {
                    continue;
                }

                for (int count = 0; count < reward.Quantity; count++)
                {
                    QuestWireRewardDao.Instance.Add(
                        new DBQuestWireReward
                            {
                                QuestId = questId,
                                Ordinal = ordinal++,
                                LowId = lowId,
                                HighId = highId,
                                Quality = selected.Quality,
                                Unknown1 = 0
                            });
                }
            }
        }

        private static int FourCharacterCode(int questId)
        {
            const string Alphabet = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            uint value = unchecked((uint)questId);
            var characters = new char[4];
            for (int i = characters.Length - 1; i >= 0; i--)
            {
                characters[i] = Alphabet[(int)(value % 36)];
                value /= 36;
            }

            return (characters[0] << 24) | (characters[1] << 16) | (characters[2] << 8) | characters[3];
        }
    }
}
