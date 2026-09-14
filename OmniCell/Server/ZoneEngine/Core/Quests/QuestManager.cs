#region License

// Copyright (c) 2005-2014, CellAO Team
// Copyright (c) 2026, OmniCell contributors
//
// This file is part of OmniCell and is distributed under the terms the project
// is licensed under. See the repository root for details.

#endregion

namespace ZoneEngine.Core.Quests
{
    #region Usings

    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Data;
    using System.Globalization;
    using System.Linq;

    using Dapper;
    using OmniCell.Core.Entities;
    using OmniCell.Core.Inventory;
    using OmniCell.Core.Items;
    using OmniCell.Core.Vector;
    using OmniCell.Database;
    using OmniCell.Database.Dao;
    using OmniCell.Database.Entities;
    using OmniCell.Enums;

    using SmokeLounge.AOtomation.Messaging.GameData;

    using Utility;

    using ZoneEngine.Core.MessageHandlers;

    #endregion

    /// <summary>
    /// Quests: what they ask for, who is part way through one, and what happens
    /// when they finish.
    /// </summary>
    /// <remarks>
    /// Definitions live in the database and are loaded once at startup, because
    /// they never change while the server is up. A character's progress is
    /// written through to the database as it happens rather than at logout, so
    /// a crash does not cost someone their quest.
    /// Scripted KnuBot answers accept and turn quests in. Objective events,
    /// journal updates, item transfers and rewards all come back through this
    /// manager so authored and imported quests use one state machine.
    /// </remarks>
    public static class QuestManager
    {
        #region Fields

        /// <summary>
        /// Quest definitions, by quest id.
        /// </summary>
        /// <remarks>
        /// Concurrent because a quest written in the game with /questedit
        /// appears here while the playfields are running, and a dictionary
        /// grown on one thread while another is walking it does not merely
        /// throw - it can return the wrong entry.
        /// </remarks>
        private static readonly ConcurrentDictionary<int, DBQuest> Definitions =
            new ConcurrentDictionary<int, DBQuest>();

        /// <summary>
        /// Objectives, by quest id, in ordinal order.
        /// </summary>
        private static readonly ConcurrentDictionary<int, List<DBQuestObjective>> Objectives =
            new ConcurrentDictionary<int, List<DBQuestObjective>>();

        private static readonly ConcurrentDictionary<int, List<DBQuestItemReward>> ItemRewards =
            new ConcurrentDictionary<int, List<DBQuestItemReward>>();

        private static readonly ConcurrentDictionary<int, DBQuestWire> WireDefinitions =
            new ConcurrentDictionary<int, DBQuestWire>();

        private static readonly ConcurrentDictionary<int, List<DBQuestWireAction>> WireActions =
            new ConcurrentDictionary<int, List<DBQuestWireAction>>();

        private static readonly ConcurrentDictionary<int, List<DBQuestWireReward>> WireRewards =
            new ConcurrentDictionary<int, List<DBQuestWireReward>>();

        /// <summary>
        /// Progress in memory, by character id. The database has the same rows;
        /// this is here so that a mob dying does not turn into a query per
        /// objective per character.
        /// </summary>
        private static readonly ConcurrentDictionary<int, List<DBCharacterQuest>> Progress =
            new ConcurrentDictionary<int, List<DBCharacterQuest>>();

        private static readonly ConcurrentDictionary<int, object> CharacterLocks =
            new ConcurrentDictionary<int, object>();

        private sealed class PendingItemReward
        {
            public Item Item;

            public IInventoryPage Page;

            public int Slot;
        }

        private sealed class ProgressSnapshot
        {
            public DBCharacterQuest Row;

            public int Progress;

            public int State;
        }

        private sealed class AdvancedObjective
        {
            public DBCharacterQuest Row;

            public DBQuestObjective Objective;

            public ProgressSnapshot Before;
        }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Reads every quest definition. Called once, at startup.
        /// </summary>
        /// <returns>
        /// How many quests were loaded.
        /// </returns>
        public static int Load()
        {
            Definitions.Clear();
            Objectives.Clear();
            ItemRewards.Clear();
            WireDefinitions.Clear();
            WireActions.Clear();
            WireRewards.Clear();

            foreach (DBQuest quest in QuestDao.Instance.GetAll())
            {
                Definitions[quest.Id] = quest;
                Objectives[quest.Id] = new List<DBQuestObjective>();
            }

            foreach (DBQuestObjective objective in QuestObjectiveDao.Instance.GetAll())
            {
                if (Objectives.ContainsKey(objective.QuestId))
                {
                    Objectives[objective.QuestId].Add(objective);
                }
            }

            foreach (DBQuestItemReward reward in QuestItemRewardDao.Instance.GetAll())
            {
                ItemRewards.GetOrAdd(reward.QuestId, id => new List<DBQuestItemReward>()).Add(reward);
            }

            foreach (DBQuestWire wire in QuestWireDao.Instance.GetAll())
            {
                if (Definitions.ContainsKey(wire.QuestId))
                {
                    WireDefinitions[wire.QuestId] = wire;
                }
            }

            foreach (DBQuestWireAction action in QuestWireActionDao.Instance.GetAll())
            {
                if (Definitions.ContainsKey(action.QuestId))
                {
                    WireActions.GetOrAdd(action.QuestId, id => new List<DBQuestWireAction>()).Add(action);
                }
            }

            foreach (DBQuestWireReward reward in QuestWireRewardDao.Instance.GetAll())
            {
                if (Definitions.ContainsKey(reward.QuestId))
                {
                    WireRewards.GetOrAdd(reward.QuestId, id => new List<DBQuestWireReward>()).Add(reward);
                }
            }

            foreach (List<DBQuestObjective> list in Objectives.Values)
            {
                list.Sort((a, b) => a.Ordinal.CompareTo(b.Ordinal));
            }

            foreach (List<DBQuestWireAction> list in WireActions.Values)
            {
                list.Sort((a, b) => a.Ordinal.CompareTo(b.Ordinal));
            }

            foreach (List<DBQuestWireReward> list in WireRewards.Values)
            {
                list.Sort((a, b) => a.Ordinal.CompareTo(b.Ordinal));
            }

            return Definitions.Count;
        }

        /// <summary>
        /// Re-reads one quest, for when it has just been written or changed.
        /// </summary>
        /// <remarks>
        /// Rather than Load, which empties both tables before filling them and
        /// would leave every quest in the world missing for as long as that
        /// takes.
        /// </remarks>
        public static void Remember(int questId)
        {
            DBQuest quest = QuestDao.Instance.Get(questId);
            if (quest == null)
            {
                DBQuest gone;
                List<DBQuestObjective> alsoGone;
                List<DBQuestItemReward> rewardsGone;
                DBQuestWire wireGone;
                List<DBQuestWireAction> wireActionsGone;
                List<DBQuestWireReward> wireRewardsGone;
                Definitions.TryRemove(questId, out gone);
                Objectives.TryRemove(questId, out alsoGone);
                ItemRewards.TryRemove(questId, out rewardsGone);
                WireDefinitions.TryRemove(questId, out wireGone);
                WireActions.TryRemove(questId, out wireActionsGone);
                WireRewards.TryRemove(questId, out wireRewardsGone);
                return;
            }

            List<DBQuestObjective> objectives =
                QuestObjectiveDao.Instance.GetWhere(new { QuestId = questId }).ToList();
            objectives.Sort((a, b) => a.Ordinal.CompareTo(b.Ordinal));

            List<DBQuestItemReward> rewards =
                QuestItemRewardDao.Instance.GetWhere(new { QuestId = questId }).ToList();
            DBQuestWire wire = QuestWireDao.Instance.GetWhere(new { QuestId = questId }).FirstOrDefault();
            List<DBQuestWireAction> wireActions =
                QuestWireActionDao.Instance.GetWhere(new { QuestId = questId }).OrderBy(a => a.Ordinal).ToList();
            List<DBQuestWireReward> wireRewards =
                QuestWireRewardDao.Instance.GetWhere(new { QuestId = questId }).OrderBy(r => r.Ordinal).ToList();

            Objectives[questId] = objectives;
            ItemRewards[questId] = rewards;
            if (wire == null)
            {
                DBQuestWire ignored;
                WireDefinitions.TryRemove(questId, out ignored);
            }
            else
            {
                WireDefinitions[questId] = wire;
            }
            WireActions[questId] = wireActions;
            WireRewards[questId] = wireRewards;
            Definitions[questId] = quest;
        }

        /// <summary>
        /// Loads a character's progress. Called when they enter a playfield.
        /// </summary>
        public static void LoadFor(ICharacter character)
        {
            int characterId = character.Identity.Instance;
            Progress[characterId] =
                CharacterQuestDao.Instance.GetWhere(new { CharacterId = characterId }).ToList();

            // An empty update matters: it clears a mission window left over
            // from a previous session just as a non-empty one restores it.
            SendQuestWindow(character);
        }

        /// <summary>
        /// Drops a character's progress from memory. The database keeps it.
        /// </summary>
        public static void Forget(ICharacter character)
        {
            Forget(character.Identity);
        }

        /// <summary>
        /// Drops a character's progress from memory. The database keeps it.
        /// </summary>
        public static void Forget(Identity character)
        {
            List<DBCharacterQuest> ignored;
            Progress.TryRemove(character.Instance, out ignored);
        }

        /// <summary>
        /// Every quest that exists.
        /// </summary>
        public static IEnumerable<DBQuest> All()
        {
            return Definitions.Values;
        }

        /// <summary>
        /// A quest by id, or null.
        /// </summary>
        public static DBQuest Get(int questId)
        {
            DBQuest quest;
            return Definitions.TryGetValue(questId, out quest) ? quest : null;
        }

        /// <summary>
        /// The objectives of a quest, in order.
        /// </summary>
        public static IList<DBQuestObjective> ObjectivesOf(int questId)
        {
            List<DBQuestObjective> list;
            return Objectives.TryGetValue(questId, out list) ? list : new List<DBQuestObjective>();
        }

        /// <summary>
        /// The inventory rewards attached to a quest.
        /// </summary>
        public static IList<DBQuestItemReward> ItemRewardsOf(int questId)
        {
            List<DBQuestItemReward> list;
            return ItemRewards.TryGetValue(questId, out list) ? list : new List<DBQuestItemReward>();
        }

        public static DBQuestWire WireOf(int questId)
        {
            DBQuestWire wire;
            return WireDefinitions.TryGetValue(questId, out wire) ? wire : null;
        }

        public static IList<DBQuestWireAction> WireActionsOf(int questId)
        {
            List<DBQuestWireAction> list;
            return WireActions.TryGetValue(questId, out list) ? list : new List<DBQuestWireAction>();
        }

        public static IList<DBQuestWireReward> WireRewardsOf(int questId)
        {
            List<DBQuestWireReward> list;
            return WireRewards.TryGetValue(questId, out list) ? list : new List<DBQuestWireReward>();
        }

        /// <summary>
        /// What a character is doing on a quest.
        /// </summary>
        public static IEnumerable<DBCharacterQuest> ProgressOf(ICharacter character, int questId)
        {
            lock (Sync(character))
            {
                return Rows(character).Where(r => r.QuestId == questId).ToList();
            }
        }

        /// <summary>
        /// Starts a quest for a character.
        /// </summary>
        /// <returns>
        /// False when the quest does not exist, or the character already has it.
        /// </returns>
        public static bool Accept(ICharacter character, int questId)
        {
            lock (Sync(character))
            {
                return AcceptLocked(character, questId);
            }
        }

        private static bool AcceptLocked(ICharacter character, int questId)
        {
            DBQuest quest = Get(questId);
            if (quest == null)
            {
                return false;
            }

            List<DBCharacterQuest> rows = Rows(character);
            IList<DBQuestObjective> objectives = ObjectivesOf(questId);
            if (objectives.Count == 0)
            {
                // A quest with nothing to do would be accepted and never
                // completable. Say so rather than leave it sitting in the log.
                Tell(character, "\"" + quest.Name + "\" has no objectives recorded, so it cannot be tracked.");
                return false;
            }

            foreach (DBQuestObjective objective in objectives)
            {
                string validationError = QuestStateRules.ObjectiveValidationError(objective);
                if (validationError != null)
                {
                    Tell(
                        character,
                        "\"" + quest.Name + "\" cannot be started: objective " + objective.Ordinal + " "
                        + validationError + ".");
                    return false;
                }
            }

            // Do not rely on dialogue presentation to enforce prerequisites or
            // single acceptance. Commands and future protocol paths reach the
            // same shared rule.
            if (!QuestStateRules.CanAccept(quest, rows, objectives.Count))
            {
                return false;
            }

            if (!CanGiveItemRewards(character, questId, true))
            {
                return false;
            }

            List<PendingItemReward> pendingRewards = new List<PendingItemReward>();
            if (!TryPrepareItemRewards(character, questId, true, out pendingRewards))
            {
                return false;
            }

            var added = new List<DBCharacterQuest>();
            try
            {
                using (IDbConnection connection = Connector.GetConnection())
                using (IDbTransaction transaction = connection.BeginTransaction())
                {
                    try
                    {
                        foreach (DBQuestObjective objective in objectives)
                        {
                            var row = new DBCharacterQuest
                                          {
                                              CharacterId = character.Identity.Instance,
                                              QuestId = questId,
                                              Ordinal = objective.Ordinal,
                                              Progress = 0,
                                              State = (int)QuestState.InProgress
                                          };

                            CharacterQuestDao.Instance.Add(row, connection, transaction);
                            added.Add(row);
                        }

                        if (pendingRewards.Count > 0)
                        {
                            PersistInventory(character, connection, transaction);
                        }

                        transaction.Commit();
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
            catch (Exception exception)
            {
                RemovePendingRewards(pendingRewards);
                LogUtil.ErrorException(exception);
                Tell(character, "The quest could not be started because its persistent state was not saved.");
                return false;
            }

            rows.AddRange(added);

            BestEffort(() => NotifyItemRewards(character, pendingRewards));
            BestEffort(() => Tell(character, "Quest started: " + quest.Name));
            BestEffort(() => Describe(character, questId));

            // The order the live server uses, which is three messages and not
            // two: 30512, 30513 and 30516 of one captured session.
            //
            // The first is what was missing. Without it the client is told
            // about a quest it has no window entry for - it says to go and
            // check the mission window and there is nothing in it.
            //
            // And the update carries the one quest that changed rather than
            // the whole log. Live sends QuestInfos=[1] here.
            BestEffort(() => CharacterActionMessageHandler.Default.SendMissionChanged(character, questId));
            BestEffort(() => QuestMessageHandler.Default.Send(character, questId));
            BestEffort(() => SendQuestWindow(character, true, questId));
            return true;
        }

        /// <summary>
        /// Gives up a quest, losing any progress on it.
        /// </summary>
        public static bool Abandon(ICharacter character, int questId)
        {
            lock (Sync(character))
            {
                return AbandonLocked(character, questId);
            }
        }

        private static bool AbandonLocked(ICharacter character, int questId)
        {
            List<DBCharacterQuest> rows = Rows(character);
            List<DBCharacterQuest> mine = rows.Where(r => r.QuestId == questId).ToList();
            // Handed-in rows are the durable receipt that prevents a completed
            // quest from being paid repeatedly. Never let abandon erase it.
            if (!QuestStateRules.CanAbandon(rows, questId))
            {
                return false;
            }

            try
            {
                using (IDbConnection connection = Connector.GetConnection())
                using (IDbTransaction transaction = connection.BeginTransaction())
                {
                    try
                    {
                        foreach (DBCharacterQuest row in mine)
                        {
                            CharacterQuestDao.Instance.Delete(row.Id, connection, transaction);
                        }

                        transaction.Commit();
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
            catch (Exception exception)
            {
                LogUtil.ErrorException(exception);
                BestEffort(() => Tell(character, "The quest was not abandoned because its state was not saved."));
                return false;
            }

            foreach (DBCharacterQuest row in mine)
            {
                rows.Remove(row);
            }

            DBQuest quest = Get(questId);
            BestEffort(
                () => Tell(character, "Quest abandoned: " + (quest == null ? questId.ToString() : quest.Name)));
            BestEffort(() => CharacterActionMessageHandler.Default.SendMissionChanged(character, questId));
            BestEffort(() => QuestMessageHandler.Default.Send(character, questId));
            BestEffort(() => SendQuestWindow(character));
            return true;
        }

        /// <summary>
        /// Counts a kill towards every quest that wants it.
        /// </summary>
        /// <remarks>
        /// Matched on the mob's name, which is what a quest description names and
        /// what the extracted spawns carry. Matching on a template id would be
        /// steadier, but the captures give a name and a spawn instance, not a
        /// template, so a name is what there is to match on.
        /// </remarks>
        public static void OnKill(ICharacter killer, ICharacter victim)
        {
            if (victim == null)
            {
                return;
            }

            Advance(killer, QuestObjectiveType.Kill, victim.Name);
        }

        /// <summary>
        /// Somebody opened a conversation with a character.
        /// </summary>
        public static void OnTalk(ICharacter character, ICharacter spokenTo)
        {
            if (spokenTo == null)
            {
                return;
            }

            Advance(character, QuestObjectiveType.TalkTo, spokenTo.Name);
        }

        /// <summary>
        /// Somebody used one of the playfield's fixtures.
        /// </summary>
        /// <remarks>
        /// Matched on the fixture's instance rather than on a name. A plain-use
        /// objective identifies one particular captured fixture; item-on-target
        /// objectives use <see cref="OnUseItemOn"/> and may deliberately name a
        /// class of fixtures, such as any of Arete's four Gas Fires.
        /// </remarks>
        public static void OnUse(ICharacter character, Identity fixture)
        {
            Advance(
                character,
                QuestObjectiveType.Use,
                fixture.Instance.ToString(CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// Somebody used an inventory item on a world object.
        /// </summary>
        /// <remarks>
        /// Authored objectives may use a readable target name or the stable
        /// world-object instance id. Both are checked when available.
        /// </remarks>
        public static void OnUseItemOn(ICharacter character, Identity target, string targetName = null)
        {
            string instance = target.Instance.ToString(CultureInfo.InvariantCulture);
            if (!string.IsNullOrEmpty(targetName)
                && !string.Equals(targetName, instance, StringComparison.OrdinalIgnoreCase))
            {
                Advance(character, QuestObjectiveType.UseItemOn, targetName);
            }

            Advance(character, QuestObjectiveType.UseItemOn, instance);
        }

        /// <summary>
        /// Somebody picked up, bought or was given an item.
        /// </summary>
        public static void OnCollect(ICharacter character, string itemName)
        {
            if (string.IsNullOrEmpty(itemName))
            {
                return;
            }

            Advance(character, QuestObjectiveType.Collect, itemName);
        }

        /// <summary>
        /// Somebody bought an item from a vendor.
        /// </summary>
        public static void OnPurchase(ICharacter character, string itemName)
        {
            if (string.IsNullOrEmpty(itemName))
            {
                return;
            }

            Advance(character, QuestObjectiveType.Purchase, itemName);
        }

        /// <summary>
        /// Somebody completed a tradeskill build.
        /// </summary>
        public static void OnTradeSkill(ICharacter character, string itemName)
        {
            if (string.IsNullOrEmpty(itemName))
            {
                return;
            }

            Advance(character, QuestObjectiveType.TradeSkill, itemName);
        }

        /// <summary>
        /// Somebody gave an item to the character who asked for it.
        /// </summary>
        public static void OnHandIn(ICharacter character, string itemName, int count = 1)
        {
            if (string.IsNullOrEmpty(itemName))
            {
                return;
            }

            Advance(character, QuestObjectiveType.HandIn, itemName, count);
        }

        /// <summary>
        /// What a quest is waiting to be handed, if it is waiting for anything.
        /// </summary>
        /// <remarks>
        /// The objective the character is on, and only if that objective is one
        /// that wants an item handed over. Used to word the trade window and to
        /// decide what to keep out of what is put in it.
        /// </remarks>
        public static DBQuestObjective Wants(ICharacter character, int questId)
        {
            return WantsAll(character, questId).FirstOrDefault();
        }

        /// <summary>
        /// Every unfinished item-transfer objective for this quest.
        /// </summary>
        public static IList<DBQuestObjective> WantsAll(ICharacter character, int questId)
        {
            var wanted = new List<DBQuestObjective>();
            // Every objective of a quest is in progress at once - Accept writes
            // a row for each - so this is a search rather than a lookup of
            // whichever one happens to come first. A quest that says kill six
            // robots and bring back the parts has the hand-over sitting behind
            // the kill, and taking only the first row found nothing to ask for.
            foreach (DBCharacterQuest row in
                ProgressOf(character, questId).Where(r => r.State == (int)QuestState.InProgress))
            {
                DBQuestObjective objective = ObjectivesOf(questId).FirstOrDefault(o => o.Ordinal == row.Ordinal);

                if (objective != null && objective.ObjectiveType == (int)QuestObjectiveType.HandIn)
                {
                    wanted.Add(objective);
                }
            }

            return wanted;
        }

        /// <summary>
        /// Somebody put an item on.
        /// </summary>
        public static void OnEquip(ICharacter character, string itemName)
        {
            if (string.IsNullOrEmpty(itemName))
            {
                return;
            }

            Advance(character, QuestObjectiveType.Equip, itemName);
        }

        /// <summary>
        /// Somebody moved. Settles any objective that just wanted them there.
        /// </summary>
        /// <remarks>
        /// Checked against the objectives the character is actually on rather
        /// than against every objective in the playfield, because this runs on
        /// every movement message a client sends.
        /// </remarks>
        public static void OnMoved(ICharacter character, Coordinate where)
        {
            if (character == null)
            {
                return;
            }

            foreach (DBCharacterQuest row in Rows(character).ToList())
            {
                if (row.State != (int)QuestState.InProgress)
                {
                    continue;
                }

                DBQuestObjective objective = ObjectivesOf(row.QuestId)
                    .FirstOrDefault(o => o.Ordinal == row.Ordinal);

                if (objective == null || objective.ObjectiveType != (int)QuestObjectiveType.Reach)
                {
                    continue;
                }

                string[] parts = objective.Target.Split(',');
                float x;
                float z;
                if (parts.Length != 2
                    || !float.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out x)
                    || !float.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out z))
                {
                    continue;
                }

                double dx = where.x - x;
                double dz = where.z - z;
                if (Math.Sqrt((dx * dx) + (dz * dz)) <= ReachWithin)
                {
                    Advance(character, QuestObjectiveType.Reach, objective.Target);
                }
            }
        }

        /// <summary>
        /// How near a place counts as being there, in units.
        /// </summary>
        /// <remarks>
        /// Five. A quest marker is the middle of whatever it is about and the
        /// client draws its arrow at the same spot, so a player who has walked
        /// to the arrow is inside this.
        /// </remarks>
        private const double ReachWithin = 5.0;

        /// <summary>
        /// Move every in-progress objective this satisfies one step on.
        /// </summary>
        /// <remarks>
        /// This was the body of OnKill, which was the only thing that could
        /// finish an objective: talking, using and collecting had entries in the
        /// enum and no code, so almost none of the captured quests could be
        /// completed. The counting, the announcement and the
        /// ready-to-hand-in check are the same whatever the objective was, so
        /// they are written once.
        /// </remarks>
        private static void Advance(
            ICharacter character,
            QuestObjectiveType kind,
            string target,
            int amount = 1)
        {
            if (character == null || string.IsNullOrEmpty(target) || amount < 1)
            {
                return;
            }

            lock (Sync(character))
            {
                AdvanceLocked(character, kind, target, amount);
            }
        }

        private static void AdvanceLocked(
            ICharacter character,
            QuestObjectiveType kind,
            string target,
            int amount)
        {
            var advanced = new List<AdvancedObjective>();
            foreach (DBCharacterQuest row in Rows(character).ToList())
            {
                if (row.State != (int)QuestState.InProgress)
                {
                    continue;
                }

                DBQuestObjective objective = ObjectivesOf(row.QuestId)
                    .FirstOrDefault(o => o.Ordinal == row.Ordinal);

                var before = new ProgressSnapshot
                                 {
                                     Row = row,
                                     Progress = row.Progress,
                                     State = row.State
                                 };
                if (!QuestStateRules.TryAdvance(row, objective, kind, target, amount))
                {
                    continue;
                }

                advanced.Add(new AdvancedObjective { Row = row, Objective = objective, Before = before });
            }

            if (advanced.Count == 0)
            {
                return;
            }

            try
            {
                using (IDbConnection connection = Connector.GetConnection())
                using (IDbTransaction transaction = connection.BeginTransaction())
                {
                    try
                    {
                        foreach (AdvancedObjective change in advanced)
                        {
                            DBCharacterQuest row = change.Row;
                            CharacterQuestDao.Instance.Save(
                                row,
                                new { row.Id, row.Progress, row.State },
                                connection,
                                transaction);
                        }

                        transaction.Commit();
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
            catch (Exception exception)
            {
                RestoreProgress(advanced.Select(change => change.Before));
                LogUtil.ErrorException(exception);
                BestEffort(() => Tell(character, "Quest progress was not changed because it could not be saved."));
                return;
            }

            foreach (AdvancedObjective change in advanced)
            {
                DBCharacterQuest row = change.Row;
                DBQuestObjective objective = change.Objective;

                // Tell the client the quest moved, the way the live server
                // does: the whole log, then the one that changed. It sends that
                // pair on every quest event - twenty seven of each in one
                // captured session - and without it the window keeps showing
                // whatever it was told when the quest was taken, however many
                // robots have died since.
                BestEffort(() => CharacterActionMessageHandler.Default.SendMissionChanged(character, row.QuestId));
                BestEffort(() => QuestMessageHandler.Default.Send(character, row.QuestId));
                BestEffort(() => SendQuestWindow(character, false, row.QuestId));

                DBQuest quest = Get(row.QuestId);
                string name = quest == null ? row.QuestId.ToString(CultureInfo.InvariantCulture) : quest.Name;

                // A count is worth reporting; a one-off is not. "Talk to Dr.
                // Mason 1/1" says nothing the next line does not.
                if (objective.Required > 1)
                {
                    BestEffort(
                        () => Tell(
                            character,
                            name + ": " + objective.Target + " " + row.Progress + "/" + objective.Required));
                }

                if (IsComplete(character, row.QuestId))
                {
                    BestEffort(() => Tell(character, name + " is ready to hand in."));
                }
            }
        }

        /// <summary>
        /// Whether the quest before this one in its chain is out of the way.
        /// </summary>
        /// <remarks>
        /// A giver holds a chain, not a menu: the next errand appears when the
        /// last one is handed in. Without this Rex Larsson greets a new
        /// character with every quest he is credited with at once.
        /// </remarks>
        public static bool IsUnlocked(ICharacter character, DBQuest quest)
        {
            return QuestStateRules.IsUnlocked(quest, Rows(character));
        }

        /// <summary>
        /// Whether any quest asks a player to go and speak to this character.
        /// </summary>
        /// <remarks>
        /// By name, because that is what a talk objective holds. A name is not
        /// unique - there are twelve ICC Peacekeepers - and that is the right
        /// behaviour here: if a quest says talk to an ICC Peacekeeper then any
        /// of them will do.
        /// </remarks>
        public static bool IsSpokenTo(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return false;
            }

            foreach (List<DBQuestObjective> list in Objectives.Values)
            {
                foreach (DBQuestObjective objective in list)
                {
                    if (objective.ObjectiveType == (int)QuestObjectiveType.TalkTo
                        && string.Equals(objective.Target, name, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Whether every objective of a quest is done.
        /// </summary>
        public static bool IsComplete(ICharacter character, int questId)
        {
            return QuestStateRules.IsComplete(Rows(character), questId);
        }

        /// <summary>
        /// Applies the items accepted by a KnuBot trade and persists the
        /// resulting inventory, objective state and final rewards together.
        /// </summary>
        public static bool ApplyTradeHandIn(
            ICharacter character,
            int questId,
            IDictionary<int, int> amountsByOrdinal)
        {
            lock (Sync(character))
            {
                return ApplyTradeHandInLocked(character, questId, amountsByOrdinal);
            }
        }

        private static bool ApplyTradeHandInLocked(
            ICharacter character,
            int questId,
            IDictionary<int, int> amountsByOrdinal)
        {
            DBQuest quest = Get(questId);
            List<DBCharacterQuest> rows = Rows(character);
            if (quest == null || amountsByOrdinal == null || amountsByOrdinal.Count == 0)
            {
                return false;
            }

            var snapshots = new List<ProgressSnapshot>();
            foreach (KeyValuePair<int, int> amount in amountsByOrdinal)
            {
                DBCharacterQuest row = rows.FirstOrDefault(
                    r => r.QuestId == questId && r.Ordinal == amount.Key);
                DBQuestObjective objective = ObjectivesOf(questId).FirstOrDefault(o => o.Ordinal == amount.Key);
                if (row == null || objective == null
                    || objective.ObjectiveType != (int)QuestObjectiveType.HandIn
                    || amount.Value < 1)
                {
                    RestoreProgress(snapshots);
                    return false;
                }

                snapshots.Add(
                    new ProgressSnapshot { Row = row, Progress = row.Progress, State = row.State });
                if (!QuestStateRules.TryAdvance(
                        row,
                        objective,
                        QuestObjectiveType.HandIn,
                        objective.Target,
                        amount.Value))
                {
                    RestoreProgress(snapshots);
                    return false;
                }
            }

            bool completed = QuestStateRules.CanHandIn(rows, questId);
            if (completed && !CanGiveItemRewards(character, questId, false))
            {
                RestoreProgress(snapshots);
                return false;
            }

            int cashBefore = character.Stats[StatIds.cash].Value;
            int xpBefore = character.Stats[StatIds.xp].Value;
            int cashAfter;
            int xpAfter;
            try
            {
                cashAfter = completed ? checked(cashBefore + quest.CashReward) : cashBefore;
                xpAfter = completed ? checked(xpBefore + quest.ExperienceReward) : xpBefore;
            }
            catch (OverflowException)
            {
                RestoreProgress(snapshots);
                Tell(character, "The quest reward would exceed the supported credit or experience range.");
                return false;
            }

            List<PendingItemReward> pendingRewards = new List<PendingItemReward>();
            if (completed && !TryPrepareItemRewards(character, questId, false, out pendingRewards))
            {
                RestoreProgress(snapshots);
                return false;
            }

            if (!completed)
            {
                pendingRewards = new List<PendingItemReward>();
            }

            try
            {
                using (IDbConnection connection = Connector.GetConnection())
                using (IDbTransaction transaction = connection.BeginTransaction())
                {
                    try
                    {
                        IEnumerable<DBCharacterQuest> rowsToSave = completed
                                                                          ? rows.Where(r => r.QuestId == questId)
                                                                          : snapshots.Select(s => s.Row);
                        foreach (DBCharacterQuest row in rowsToSave)
                        {
                            CharacterQuestDao.Instance.Save(
                                row,
                                new
                                    {
                                        row.Id,
                                        row.Progress,
                                        State = completed
                                                    ? (int)QuestState.HandedIn
                                                    : row.State
                                    },
                                connection,
                                transaction);
                        }

                        PersistInventory(character, connection, transaction);
                        if (completed && quest.CashReward > 0)
                        {
                            PersistRewardStat(character, (int)StatIds.cash, cashAfter, connection, transaction);
                        }

                        if (completed && quest.ExperienceReward > 0)
                        {
                            PersistRewardStat(character, (int)StatIds.xp, xpAfter, connection, transaction);
                        }

                        transaction.Commit();
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
            catch (Exception exception)
            {
                RemovePendingRewards(pendingRewards);
                RestoreProgress(snapshots);
                LogUtil.ErrorException(exception);
                Tell(character, "The hand-in was not saved. Your offered items were not consumed.");
                return false;
            }

            if (completed)
            {
                foreach (DBCharacterQuest row in rows.Where(r => r.QuestId == questId))
                {
                    row.State = (int)QuestState.HandedIn;
                }

                character.Stats[StatIds.cash].Value = cashAfter;
                character.Stats[StatIds.xp].Value = xpAfter;
                BestEffort(character.SendChangedStats);
                BestEffort(() => NotifyItemRewards(character, pendingRewards));
            }

            BestEffort(() => CharacterActionMessageHandler.Default.SendMissionChanged(character, questId));
            BestEffort(() => QuestMessageHandler.Default.Send(character, questId));
            BestEffort(() => SendQuestWindow(character, false, questId));

            if (completed)
            {
                BestEffort(
                    () => Tell(
                        character,
                        "Quest complete: " + quest.Name + ". " + quest.CashReward + " credits, "
                        + quest.ExperienceReward + " experience."));
            }
            else
            {
                BestEffort(() => Describe(character, questId));
            }

            return true;
        }

        /// <summary>
        /// Hands a finished quest in and pays out.
        /// </summary>
        public static bool HandIn(ICharacter character, int questId)
        {
            lock (Sync(character))
            {
                return HandInLocked(character, questId);
            }
        }

        private static bool HandInLocked(ICharacter character, int questId)
        {
            DBQuest quest = Get(questId);
            List<DBCharacterQuest> rows = Rows(character);
            if (quest == null || !QuestStateRules.CanHandIn(rows, questId))
            {
                return false;
            }

            List<DBCharacterQuest> mine = rows.Where(r => r.QuestId == questId).ToList();

            if (!CanGiveItemRewards(character, questId, false))
            {
                return false;
            }

            int cashBefore = character.Stats[StatIds.cash].Value;
            int xpBefore = character.Stats[StatIds.xp].Value;
            int cashAfter;
            int xpAfter;
            try
            {
                cashAfter = checked(cashBefore + quest.CashReward);
                xpAfter = checked(xpBefore + quest.ExperienceReward);
            }
            catch (OverflowException)
            {
                Tell(character, "The quest reward would exceed the supported credit or experience range.");
                return false;
            }

            List<PendingItemReward> pendingRewards;
            if (!TryPrepareItemRewards(character, questId, false, out pendingRewards))
            {
                return false;
            }

            try
            {
                using (IDbConnection connection = Connector.GetConnection())
                using (IDbTransaction transaction = connection.BeginTransaction())
                {
                    try
                    {
                        foreach (DBCharacterQuest row in mine)
                        {
                            CharacterQuestDao.Instance.Save(
                                row,
                                new { row.Id, State = (int)QuestState.HandedIn },
                                connection,
                                transaction);
                        }

                        if (pendingRewards.Count > 0)
                        {
                            PersistInventory(character, connection, transaction);
                        }

                        if (quest.CashReward > 0)
                        {
                            PersistRewardStat(character, (int)StatIds.cash, cashAfter, connection, transaction);
                        }

                        if (quest.ExperienceReward > 0)
                        {
                            PersistRewardStat(character, (int)StatIds.xp, xpAfter, connection, transaction);
                        }

                        transaction.Commit();
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
            catch (Exception exception)
            {
                RemovePendingRewards(pendingRewards);
                LogUtil.ErrorException(exception);
                Tell(character, "The quest was not handed in because its rewards were not saved.");
                return false;
            }

            foreach (DBCharacterQuest row in mine)
            {
                row.State = (int)QuestState.HandedIn;
            }

            character.Stats[StatIds.cash].Value = cashAfter;
            character.Stats[StatIds.xp].Value = xpAfter;

            BestEffort(character.SendChangedStats);
            BestEffort(() => NotifyItemRewards(character, pendingRewards));

            BestEffort(() => CharacterActionMessageHandler.Default.SendMissionChanged(character, questId));
            BestEffort(() => QuestMessageHandler.Default.Send(character, questId));
            BestEffort(() => SendQuestWindow(character));

            BestEffort(
                () => Tell(
                    character,
                    "Quest complete: " + quest.Name + ". " + quest.CashReward + " credits, "
                    + quest.ExperienceReward + " experience."));

            return true;
        }

        /// <summary>
        /// Sends the client its quest window.
        /// </summary>
        /// <remarks>
        /// The whole list every time. The live server sends QuestFullUpdate with
        /// every quest a character is on rather than a delta, which is why there
        /// are only 61 of them across sessions that started dozens of quests.
        ///
        /// The marker is put on whoever gives the quest, whose position comes
        /// out of the spawn table. A quest whose giver is not spawned gets a
        /// marker at the origin rather than being left out of the window.
        ///
        /// <paramref name="announceAsNew"/> defaults to false because the
        /// client treats it as "tell the player they have just been given
        /// these". Entering a playfield and finishing a quest both refresh the
        /// window and neither is a new mission.
        /// </remarks>
        public static void SendQuestWindow(ICharacter character, bool announceAsNew = false, int only = 0)
        {
            if (character == null)
            {
                return;
            }

            var entries = new List<QuestFullUpdateEntry>();

            foreach (DBQuest quest in All()
                .Where(q => only == 0 || q.Id == only)
                .Where(
                    q => ProgressOf(character, q.Id)
                        .Any(r => r.State != (int)QuestState.HandedIn)))
            {
                entries.Add(MarkerFor(character, quest));
            }

            QuestFullUpdateMessageHandler.Default.Send(character, entries, announceAsNew);
        }

        private static QuestFullUpdateEntry MarkerFor(ICharacter character, DBQuest quest)
        {
            var entry = new QuestFullUpdateEntry
                            {
                                Quest = quest,
                                Wire = WireOf(quest.Id),
                                WireActions = WireActionsOf(quest.Id),
                                WireRewards = WireRewardsOf(quest.Id)
                            };
            DBCharacterQuest progress = ProgressOf(character, quest.Id)
                .Where(r => r.State == (int)QuestState.InProgress)
                .OrderBy(r => r.Ordinal)
                .FirstOrDefault();
            DBQuestObjective objective = progress == null
                                             ? null
                                             : ObjectivesOf(quest.Id)
                                                 .FirstOrDefault(o => o.Ordinal == progress.Ordinal);

            if (objective != null
                && objective.ObjectiveType == (int)QuestObjectiveType.Reach
                && !string.IsNullOrWhiteSpace(objective.Target))
            {
                string[] parts = objective.Target.Split(',');
                float x;
                float z;
                if (parts.Length == 2
                    && float.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out x)
                    && float.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out z))
                {
                    entry.MarkerX = x;
                    entry.MarkerZ = z;
                    return entry;
                }
            }

            int instance;
            if (objective != null
                && (objective.ObjectiveType == (int)QuestObjectiveType.Use
                    || objective.ObjectiveType == (int)QuestObjectiveType.UseItemOn)
                && int.TryParse(objective.Target, out instance))
            {
                DBStaticDynel statel = StaticDynelDao.Instance.GetWhere(
                    new { Instance = instance, Playfield = quest.Playfield }).FirstOrDefault();
                if (statel != null)
                {
                    entry.MarkerX = statel.X;
                    entry.MarkerY = statel.Y;
                    entry.MarkerZ = statel.Z;
                    return entry;
                }
            }

            if (objective != null
                && (objective.ObjectiveType == (int)QuestObjectiveType.Kill
                    || objective.ObjectiveType == (int)QuestObjectiveType.TalkTo))
            {
                DBMobSpawn target = MobSpawnDao.Instance.GetWhere(
                    new { Name = objective.Target, Playfield = quest.Playfield }).FirstOrDefault();
                if (target != null)
                {
                    entry.MarkerX = target.X;
                    entry.MarkerY = target.Y;
                    entry.MarkerZ = target.Z;
                    return entry;
                }
            }

            if (objective != null && objective.ObjectiveType == (int)QuestObjectiveType.HandIn)
            {
                DBKnuBotScript handover = KnuBotScriptDao.Instance.GetWhere(
                    new
                        {
                            Grants = quest.Id,
                            Action = (int)ScriptAction.OpenQuestTrade,
                            Playfield = quest.Playfield
                        }).FirstOrDefault();
                DBMobSpawn recipient = handover == null
                                           ? null
                                           : MobSpawnDao.Instance.GetWhere(
                                               new { Id = handover.Npc, Playfield = quest.Playfield })
                                               .FirstOrDefault();
                if (recipient != null)
                {
                    entry.MarkerX = recipient.X;
                    entry.MarkerY = recipient.Y;
                    entry.MarkerZ = recipient.Z;
                    return entry;
                }
            }

            DBMobSpawn giver = MobSpawnDao.Instance.GetWhere(new { Id = quest.GiverId }).FirstOrDefault();
            if (giver != null)
            {
                entry.MarkerX = giver.X;
                entry.MarkerY = giver.Y;
                entry.MarkerZ = giver.Z;
            }

            return entry;
        }

        /// <summary>
        /// Prints a quest's objectives and how far along they are.
        /// </summary>
        public static void Describe(ICharacter character, int questId)
        {
            foreach (DBQuestObjective objective in ObjectivesOf(questId))
            {
                DBCharacterQuest row = Rows(character)
                    .FirstOrDefault(r => r.QuestId == questId && r.Ordinal == objective.Ordinal);

                int done = row == null ? 0 : row.Progress;
                Tell(
                    character,
                    "  " + Describe(objective) + "  " + done + "/" + objective.Required);
            }
        }

        #endregion

        #region Methods

        private static string Describe(DBQuestObjective objective)
        {
            switch ((QuestObjectiveType)objective.ObjectiveType)
            {
                case QuestObjectiveType.Kill:
                    return "Kill " + objective.Target;
                case QuestObjectiveType.Collect:
                    return "Collect " + objective.Target;
                case QuestObjectiveType.Purchase:
                    return "Buy " + objective.Target;
                case QuestObjectiveType.TradeSkill:
                    return "Build " + objective.Target;
                case QuestObjectiveType.UseItemOn:
                    return "Use an item on " + objective.Target;
                case QuestObjectiveType.TalkTo:
                    return "Talk to " + objective.Target;
                case QuestObjectiveType.Equip:
                    return "Wear " + objective.Target;
                case QuestObjectiveType.HandIn:
                    return "Hand over " + objective.Target;
                case QuestObjectiveType.Reach:
                    return "Go to " + objective.Target;
                default:
                    return objective.Target;
            }
        }

        private static List<DBCharacterQuest> Rows(ICharacter character)
        {
            return Progress.GetOrAdd(
                character.Identity.Instance,
                id => CharacterQuestDao.Instance.GetWhere(new { CharacterId = id }).ToList());
        }

        private static bool TryPrepareItemRewards(
            ICharacter character,
            int questId,
            bool onAccept,
            out List<PendingItemReward> pending)
        {
            pending = new List<PendingItemReward>();
            List<DBQuestItemReward> rewards;
            if (!ItemRewards.TryGetValue(questId, out rewards))
            {
                return true;
            }

            IInventoryPage page = character.BaseInventory[character.BaseInventory.StandardPage];
            foreach (DBQuestItemReward reward in rewards.Where(r => (r.GrantOnAccept != 0) == onAccept))
            {
                ItemTemplate template;
                if (reward.Quantity < 1 || !ItemLoader.ItemList.TryGetValue(reward.ItemId, out template))
                {
                    Tell(character, "Quest item " + reward.ItemId + " is not available in the content pack.");
                    RemovePendingRewards(pending);
                    return false;
                }

                int quality;
                int lowId;
                int highId;
                if (!ResolveRewardTemplate(template, out quality, out lowId, out highId))
                {
                    Tell(character, "Quest item " + reward.ItemId + " has no constructible converted template.");
                    RemovePendingRewards(pending);
                    return false;
                }

                for (int count = 0; count < reward.Quantity; count++)
                {
                    try
                    {
                        var item = new Item(quality, lowId, highId);
                        int slot = page.FindFreeSlot();
                        if (slot < 0 || page.Add(slot, item) != InventoryError.OK)
                        {
                            RemovePendingRewards(pending);
                            Tell(character, "Make room in your inventory for the quest item before continuing.");
                            return false;
                        }

                        pending.Add(new PendingItemReward { Item = item, Page = page, Slot = slot });
                    }
                    catch (Exception exception)
                    {
                        RemovePendingRewards(pending);
                        LogUtil.ErrorException(exception);
                        Tell(character, "The quest item could not be prepared safely.");
                        return false;
                    }
                }
            }

            return true;
        }

        private static void NotifyItemRewards(ICharacter character, IEnumerable<PendingItemReward> pending)
        {
            foreach (PendingItemReward reward in pending)
            {
                AddTemplateMessageHandler.Default.Send(character, reward.Item);
            }
        }

        private static void RemovePendingRewards(IEnumerable<PendingItemReward> pending)
        {
            foreach (PendingItemReward reward in pending.Reverse())
            {
                if (ReferenceEquals(reward.Page[reward.Slot], reward.Item))
                {
                    reward.Page.Remove(reward.Slot);
                }
            }
        }

        private static void RestoreProgress(IEnumerable<ProgressSnapshot> snapshots)
        {
            foreach (ProgressSnapshot snapshot in snapshots)
            {
                snapshot.Row.Progress = snapshot.Progress;
                snapshot.Row.State = snapshot.State;
            }
        }

        private static bool CanGiveItemRewards(ICharacter character, int questId, bool onAccept)
        {
            List<DBQuestItemReward> rewards;
            if (!ItemRewards.TryGetValue(questId, out rewards))
            {
                return true;
            }

            int needed = 0;
            foreach (DBQuestItemReward reward in rewards.Where(r => (r.GrantOnAccept != 0) == onAccept))
            {
                ItemTemplate template;
                int quality;
                int lowId;
                int highId;
                if (reward.Quantity < 1
                    || !ItemLoader.ItemList.TryGetValue(reward.ItemId, out template)
                    || !ResolveRewardTemplate(template, out quality, out lowId, out highId))
                {
                    Tell(character, "Quest item " + reward.ItemId + " is not constructible from the content pack.");
                    return false;
                }

                needed += reward.Quantity;
            }

            var page = character.BaseInventory[character.BaseInventory.StandardPage];
            int free = page.MaxSlots - page.List().Count;
            if (needed > free)
            {
                Tell(
                    character,
                    "Make " + needed + " inventory slot" + (needed == 1 ? string.Empty : "s")
                    + " available before continuing this quest.");
                return false;
            }

            return true;
        }

        private static bool ResolveRewardTemplate(
            ItemTemplate template,
            out int quality,
            out int lowId,
            out int highId)
        {
            quality = template == null ? 0 : template.Quality;
            lowId = template == null ? -1 : template.GetLowId(quality);
            highId = template == null ? -1 : template.GetHighId(quality);
            return quality > 0
                   && lowId > 0
                   && highId > 0
                   && ItemLoader.ItemList.ContainsKey(lowId)
                   && ItemLoader.ItemList.ContainsKey(highId);
        }

        private static void PersistInventory(
            ICharacter character,
            IDbConnection connection,
            IDbTransaction transaction)
        {
            foreach (IInventoryPage page in character.BaseInventory.Pages.Values)
            {
                int containerType = (int)page.Identity.Type;
                int containerInstance = page.Identity.Instance;
                ItemDao.Instance.Delete(
                    new { containertype = containerType, containerinstance = containerInstance },
                    connection,
                    transaction);
                InstancedItemDao.Instance.Delete(
                    new { containertype = containerType, containerinstance = containerInstance },
                    connection,
                    transaction);

                foreach (KeyValuePair<int, OmniCell.Core.Items.IItem> entry in page.List())
                {
                    OmniCell.Core.Items.IItem item = entry.Value;
                    if (item.Identity.Type == IdentityType.None)
                    {
                        ItemDao.Instance.Add(
                            new DBItem
                                {
                                    containerinstance = containerInstance,
                                    containertype = containerType,
                                    containerplacement = entry.Key,
                                    lowid = item.LowID,
                                    highid = item.HighID,
                                    quality = item.Quality,
                                    multiplecount = item.MultipleCount
                                },
                            connection,
                            transaction);
                    }
                    else
                    {
                        InstancedItemDao.Instance.Add(
                            new DBInstancedItem
                                {
                                    containerinstance = containerInstance,
                                    containertype = containerType,
                                    containerplacement = entry.Key,
                                    itemtype = (int)item.Identity.Type,
                                    Id = item.Identity.Instance,
                                    lowid = item.LowID,
                                    highid = item.HighID,
                                    quality = item.Quality,
                                    multiplecount = item.MultipleCount,
                                    stats = item.GetItemAttributes()
                                },
                            connection,
                            transaction,
                            false);
                    }
                }
            }
        }

        private static void PersistRewardStat(
            ICharacter character,
            int statId,
            int value,
            IDbConnection connection,
            IDbTransaction transaction)
        {
            connection.Execute(
                "INSERT INTO stats (Instance, Type, StatId, StatValue) "
                + "VALUES (@Instance, @Type, @StatId, @StatValue) "
                + "ON DUPLICATE KEY UPDATE StatValue=VALUES(StatValue)",
                new
                    {
                        Instance = character.Identity.Instance,
                        Type = (int)character.Identity.Type,
                        StatId = statId,
                        StatValue = value
                    },
                transaction);
        }

        private static object Sync(ICharacter character)
        {
            return CharacterLocks.GetOrAdd(character.Identity.Instance, id => new object());
        }

        private static void BestEffort(Action notification)
        {
            try
            {
                notification();
            }
            catch (Exception exception)
            {
                // Persistence is already committed at every call site. A dead
                // client socket must not turn that success into a second
                // hand-in or an exception escaping the message handler.
                LogUtil.ErrorException(exception);
            }
        }

        private static void Tell(ICharacter character, string text)
        {
            if (character == null || character.Playfield == null || string.IsNullOrEmpty(text))
            {
                return;
            }

            try
            {
                character.Playfield.Publish(ChatTextMessageHandler.Default.CreateIM(character, text));
            }
            catch (Exception exception)
            {
                // A quest state operation must not escape its message handler
                // merely because the client disconnected before its IM could
                // be delivered.
                LogUtil.ErrorException(exception);
            }
        }

        #endregion
    }
}
