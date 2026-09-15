#region License

// Copyright (c) 2026, OmniCell contributors
//
// This file is part of OmniCell and is distributed under the terms the project
// is licensed under. See the repository root for details.

#endregion

namespace ZoneEngine.Core.KnuBot
{
    #region Usings ...

    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;

    using OmniCell.Core.Entities;
    using OmniCell.Core.Inventory;
    using OmniCell.Core.Items;
    using OmniCell.Database.Entities;
    using OmniCell.Enums;

    using SmokeLounge.AOtomation.Messaging.GameData;

    using ZoneEngine.Core.MessageHandlers;
    using ZoneEngine.Core.Quests;

    #endregion

    /// <summary>
    /// A character's conversation as the live server holds it: where it starts depends on where the
    /// player is in the character's quests.
    /// </summary>
    /// <remarks>
    /// Built from captures by Tools/Capture/QuestExtract (knubotdialogue, knubotopeners; see
    /// Documentation/Quest-System.md). A conversation opens on the node its opener picks for the
    /// player's quest state, says that node's lines, does what the node does - grant a stage, open a
    /// hand-in trade - and offers its answers. An answer moves to its node; a question is answered and
    /// the same answers come back without it (Marcus Stone's "Who are you?"); "Goodbye" is answered
    /// with the character's farewell and the window closes after five seconds, as retail does.
    /// Finishing a stage by talking or by an answer happens in the quest engine, which hears about
    /// the conversation opening and every answer.
    /// </remarks>
    public class ConversationKnuBot : BaseKnuBot
    {
        #region Fields

        /// <summary>
        /// KnuBotCloseChatWindow Seconds, in every captured goodbye.
        /// </summary>
        private const int CloseSeconds = 5;

        private const int FarewellNode = -1;

        private readonly string npcName;

        private readonly List<DBKnuBotDialogue> rows;

        private readonly List<DBKnuBotOpener> openers;

        private readonly Dictionary<int, List<DBKnuBotDialogue>> nodes;

        private readonly HashSet<string> asked = new HashSet<string>(StringComparer.Ordinal);

        private readonly List<DBKnuBotDialogue> visible = new List<DBKnuBotDialogue>();

        private List<DBKnuBotDialogue> answers = new List<DBKnuBotDialogue>();

        private bool started;

        private int tradeNext;

        private int tradingQuest;

        #endregion

        #region Constructors and Destructors

        public ConversationKnuBot(
            Identity knubotIdentity,
            string npcName,
            IEnumerable<DBKnuBotDialogue> rows,
            IEnumerable<DBKnuBotOpener> openers)
            : base(knubotIdentity)
        {
            this.npcName = npcName;
            this.rows = rows.ToList();
            this.openers = (openers ?? Enumerable.Empty<DBKnuBotOpener>()).ToList();
            this.nodes = this.rows.GroupBy(r => r.Node).ToDictionary(g => g.Key, g => g.OrderBy(r => r.Ordinal).ToList());
            this.SetRootNode(new KnuBotDialogTree("root", this.Choose, new[] { this.CAS(this.Handle, "self") }));
        }

        #endregion

        #region Methods

        public override BaseKnuBot CreateSession()
        {
            return new ConversationKnuBot(this.KnuBotIdentity, this.npcName, this.rows, this.openers);
        }

        private KnuBotAction Choose(KnuBotOptionId optionId)
        {
            return this.Handle;
        }

        /// <summary>
        /// Everything happens here, and nothing is left to the framework's re-entry after an answer.
        /// </summary>
        private void Handle()
        {
            this.SuspendDialogContinuation();
            ICharacter talker = this.GetCharacter();
            if (talker == null)
            {
                return;
            }

            if (this.LastAnswer == (int)KnuBotOptionId.DialogStart)
            {
                if (!this.started)
                {
                    this.started = true;
                    int start = this.Opener(talker);
                    if (start == 0)
                    {
                        this.Farewell();
                    }
                    else
                    {
                        this.Enter(talker, start);
                    }
                }

                return;
            }

            int chosen = this.LastAnswer;
            if (chosen < 0 || chosen >= this.visible.Count)
            {
                this.Farewell();
                return;
            }

            DBKnuBotDialogue answer = this.visible[chosen];
            if (answer.AnswerKind == 2)
            {
                this.Farewell();
                return;
            }

            QuestManager.OnDialogueAnswer(talker, answer.Text);
            if (this.GetCharacter() == null)
            {
                return;
            }

            if (answer.AnswerKind == 1)
            {
                this.asked.Add(answer.Text);
                this.Run(talker, answer.Next, false);
                this.Offer();
                return;
            }

            if (answer.Next == 0 || !this.nodes.ContainsKey(answer.Next))
            {
                // No capture shows where this answer leads.
                this.Farewell();
                return;
            }

            this.Enter(talker, answer.Next);
        }

        private int Opener(ICharacter talker)
        {
            foreach (DBKnuBotOpener opener in this.openers.OrderByDescending(o => o.Priority).ThenBy(o => o.Id))
            {
                if (Ids(opener.RequireActive).All(q => QuestManager.IsActive(talker, q))
                    && Ids(opener.RequireDone).All(q => QuestManager.IsDone(talker, q))
                    && !Ids(opener.ForbidStarted).Any(q => QuestManager.IsStarted(talker, q)))
                {
                    return opener.Node;
                }
            }

            return 0;
        }

        private static IEnumerable<int> Ids(string list)
        {
            foreach (string part in (list ?? string.Empty).Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
            {
                int id;
                if (int.TryParse(part.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out id))
                {
                    yield return id;
                }
            }
        }

        private void Enter(ICharacter talker, int node)
        {
            this.answers = new List<DBKnuBotDialogue>();
            if (this.Run(talker, node, true))
            {
                this.Offer();
            }
        }

        /// <summary>
        /// Says and does a node's rows in order.
        /// </summary>
        /// <returns>
        /// False when the node handed the conversation over to a trade window.
        /// </returns>
        private bool Run(ICharacter talker, int node, bool collectAnswers)
        {
            List<DBKnuBotDialogue> step;
            if (!this.nodes.TryGetValue(node, out step))
            {
                return true;
            }

            foreach (DBKnuBotDialogue row in step)
            {
                if (this.GetCharacter() == null)
                {
                    return false;
                }

                switch (row.Kind)
                {
                    case 0:
                        this.WriteLine(this.Personal(row.Text).Replace("\\n", "\r\n"), row.Flag);
                        break;
                    case 1:
                        if (collectAnswers)
                        {
                            this.answers.Add(row);
                        }

                        break;
                    case 2:
                        QuestManager.Accept(talker, row.ActionValue);
                        break;
                    case 3:
                        if (this.OpenTrade(talker, row))
                        {
                            return false;
                        }

                        break;
                }
            }

            return true;
        }

        private void Offer()
        {
            this.visible.Clear();
            this.visible.AddRange(this.answers.Where(a => a.AnswerKind != 1 || !this.asked.Contains(a.Text)));
            if (this.visible.Count == 0)
            {
                this.CloseChatWindow(CloseSeconds);
                return;
            }

            this.SendAnswerList(this.visible.Select(a => this.Personal(a.Text)).ToArray());
        }

        private void Farewell()
        {
            List<DBKnuBotDialogue> farewell;
            if (this.nodes.TryGetValue(FarewellNode, out farewell))
            {
                foreach (DBKnuBotDialogue row in farewell.Where(r => r.Kind == 0))
                {
                    this.WriteLine(this.Personal(row.Text).Replace("\\n", "\r\n"), row.Flag);
                }
            }

            this.CloseChatWindow(CloseSeconds);
        }

        private string Personal(string text)
        {
            ICharacter talker = this.GetCharacter();
            return talker == null || text == null ? text : text.Replace("{name}", talker.Name);
        }

        #endregion

        #region Hand-ins

        /// <summary>
        /// Opens the trade window for the stage the player is on that wants something handed over.
        /// </summary>
        private bool OpenTrade(ICharacter talker, DBKnuBotDialogue row)
        {
            DBQuest wanting = QuestManager.All()
                .Where(q => QuestManager.IsActive(talker, q.Id))
                .FirstOrDefault(q => QuestManager.WantsAll(talker, q.Id).Count > 0);
            if (wanting == null)
            {
                return false;
            }

            this.ReturnEverything();
            this.tradingQuest = wanting.Id;
            this.tradeNext = row.Next;
            this.StartTrade(row.Text, Math.Max(1, row.Flag));
            this.SuspendDialogContinuation();
            return true;
        }

        public override void TradeFinish(bool declined)
        {
            ICharacter talker = this.GetCharacter();
            if (talker == null || declined || this.tradingQuest == 0)
            {
                base.TradeFinish(declined);
                return;
            }

            IList<DBQuestObjective> wanted = QuestManager.WantsAll(talker, this.tradingQuest);
            var accepted = new Dictionary<DBQuestObjective, int>();
            var consumed = new Dictionary<Traded, int>();
            foreach (Traded held in this.Escrow.ToList())
            {
                DBQuestObjective objective = wanted.FirstOrDefault(
                    o => this.Remaining(talker, o) - (accepted.ContainsKey(o) ? accepted[o] : 0) > 0
                         && Matches(held.Item, o));
                if (objective == null)
                {
                    continue;
                }

                int alreadyAccepted = accepted.ContainsKey(objective) ? accepted[objective] : 0;
                int take = QuestTradeRules.AmountToConsume(
                    this.Remaining(talker, objective),
                    alreadyAccepted,
                    Math.Max(1, held.Item.MultipleCount));
                if (take == 0)
                {
                    continue;
                }

                accepted[objective] = alreadyAccepted + take;
                consumed[held] = take;
            }

            if (accepted.Count == 0)
            {
                this.ReturnEverything();
                return;
            }

            // Anything not wanted goes back, and whatever part of a stack was not needed.
            foreach (Traded held in this.Escrow.Where(h => !consumed.ContainsKey(h)).ToList())
            {
                this.Escrow.Remove(held);
                this.GiveBack(held, true);
            }

            foreach (KeyValuePair<Traded, int> entry in consumed.ToList())
            {
                int available = Math.Max(1, entry.Key.Item.MultipleCount);
                if (entry.Value < available)
                {
                    this.Escrow.Remove(entry.Key);
                    entry.Key.Item.MultipleCount = available - entry.Value;
                    this.GiveBack(entry.Key, true);
                    consumed.Remove(entry.Key);
                }
            }

            // The live server answers an accepted hand-in with an empty rejected-items list, then pays
            // out (20260914-124401 #3071, #5482).
            this.RejectItems(new Item[0]);

            int questId = this.tradingQuest;
            bool saved = QuestManager.ApplyTradeHandIn(
                talker,
                questId,
                accepted.ToDictionary(x => x.Key.Ordinal, x => x.Value));
            if (!saved)
            {
                foreach (Traded held in consumed.Keys.ToList())
                {
                    this.Escrow.Remove(held);
                    this.GiveBack(held, true);
                }

                return;
            }

            foreach (Traded held in consumed.Keys.ToList())
            {
                this.Escrow.Remove(held);
            }

            if (!QuestManager.IsDone(talker, questId))
            {
                return;
            }

            this.tradingQuest = 0;
            int next = this.tradeNext;
            this.tradeNext = 0;
            if (next != 0 && this.GetCharacter() != null)
            {
                this.Enter(talker, next);
            }
        }

        private int Remaining(ICharacter talker, DBQuestObjective objective)
        {
            DBCharacterQuest row = QuestManager.ProgressOf(talker, objective.QuestId)
                .FirstOrDefault(r => r.Ordinal == objective.Ordinal);
            return Math.Max(0, objective.Required - (row == null ? 0 : row.Progress));
        }

        private static bool Matches(Item item, DBQuestObjective objective)
        {
            return QuestItemRules.Matches(
                objective.TargetLowId,
                objective.TargetHighId,
                objective.TargetQuality,
                objective.Target,
                item.LowID,
                item.HighID,
                item.Quality,
                TradeSkill.Instance.GetItemName(item.LowID, item.HighID, item.Quality));
        }

        #endregion
    }
}
