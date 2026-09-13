#region License

// Copyright (c) 2025, OmniCell
//
// All rights reserved.
//
// Redistribution and use in source and binary forms, with or without modification, are permitted provided that the following conditions are met:
//
//     * Redistributions of source code must retain the above copyright notice, this list of conditions and the following disclaimer.
//     * Redistributions in binary form must reproduce the above copyright notice, this list of conditions and the following disclaimer in the documentation and/or other materials provided with the distribution.
//     * Neither the name of the OmniCell Team nor the names of its contributors may be used to endorse or promote products derived from this software without specific prior written permission.
//
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
// "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT
// LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR
// A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR
// CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL,
// EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO,
// PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR
// PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF
// LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING
// NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
// SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

#endregion

namespace ZoneEngine.Core.KnuBot
{
    #region Usings ...

    using System;
    using System.Collections.Generic;
    using System.Linq;

    using OmniCell.Core.Entities;
    using OmniCell.Core.Inventory;
    using OmniCell.Core.Items;
    using OmniCell.Database.Entities;
    using OmniCell.Enums;
    using OmniCell.ObjectManager;

    using SmokeLounge.AOtomation.Messaging.GameData;

    using ZoneEngine.Core.Quests;
    using ZoneEngine.Core.MessageHandlers;

    #endregion

    /// <summary>
    /// A character reading a database-backed captured or authored conversation.
    /// </summary>
    /// <remarks>
    /// Capture-derived rows retain their captured text; clearly labelled
    /// fallback rows and GM-authored rows are OmniCell content. See the
    /// knubotscript table, ordered Arete patches and Tools/Capture/AreaExtract.
    ///
    /// The conversation is a walk. Each step is what the character says
    /// followed by the answers the window offers; the last answer always
    /// leaves and any other moves on one step. Quest actions come from explicit
    /// action rows; capture-adjacent log transitions alone are not treated as
    /// proof of whether an answer accepted or handed in a quest.
    ///
    /// It is a walk and not the tree the live server has, because a capture
    /// only ever records the path somebody actually took. An answer nobody
    /// clicked is in no capture and cannot be here. What that costs is the
    /// branches: pick the first answer and you get what the captured player
    /// got.
    ///
    /// The same class plays conversations written in the game with /questedit,
    /// which is why it is worth the table being the only thing it reads. An
    /// authored conversation and a captured one are the same rows, and a
    /// character built by hand behaves the way Rex Larsson does because it is
    /// running the same code over the same shape of data.
    /// </remarks>
    public class ScriptedKnuBot : BaseKnuBot
    {
        #region Fields

        private readonly List<Chapter> chapters = new List<Chapter>();

        private readonly List<DBKnuBotScript> scriptLines;

        private int at;

        /// <summary>
        /// The conversation this bot is part way through, so a new one can be
        /// told from a continuation of the old.
        /// </summary>
        private object conversation;

        private readonly List<Response> visibleAnswers = new List<Response>();

        private int tradingQuest;

        #endregion

        #region Constructors and Destructors

        public ScriptedKnuBot(Identity knubotIdentity, IEnumerable<DBKnuBotScript> lines)
            : base(knubotIdentity)
        {
            this.scriptLines = lines.ToList();
            foreach (var step in this.scriptLines.OrderBy(l => l.Ordinal).GroupBy(l => l.Step).OrderBy(g => g.Key))
            {
                var chapter = new Chapter
                                  {
                                      LegacyGrants = step.All(l => l.Action == 0)
                                                     && step.Any(l => l.Grants != 0),
                                      Quest = step.Where(l => l.Grants != 0).Select(l => l.Grants).FirstOrDefault()
                                  };

                foreach (DBKnuBotScript line in step.OrderBy(l => l.Ordinal))
                {
                    switch ((ScriptLine)line.Kind)
                    {
                        case ScriptLine.Says:
                            chapter.Says.Add(line.Text);
                            break;
                        case ScriptLine.WantsItems:
                            chapter.Wants = line.Text;
                            break;
                        default:
                            chapter.Answers.Add(
                                new Response
                                    {
                                        Text = line.Text,
                                        Action = (ScriptAction)line.Action,
                                        Quest = line.Grants
                                    });
                            break;
                    }
                }

                this.chapters.Add(chapter);
            }

            var root = new KnuBotDialogTree(
                "root",
                this.Choose,
                new[]
                    {
                        this.CAS(this.Say, "self"),
                        this.CAS(this.Onwards, "self"),
                        this.CAS(this.Leave, "self"),
                        this.CAS(this.Selected, "self")
                    });

            this.SetRootNode(root);
        }

        #endregion

        #region Methods

        public override BaseKnuBot CreateSession()
        {
            return new ScriptedKnuBot(this.KnuBotIdentity, this.scriptLines);
        }

        /// <summary>
        /// Whether this character has anything to say.
        /// </summary>
        public bool HasScript
        {
            get
            {
                return this.chapters.Count > 0;
            }
        }

        private KnuBotAction Choose(KnuBotOptionId optionId)
        {
            if (optionId == KnuBotOptionId.DialogStart)
            {
                // Start at the top whenever the window is a new one.
                //
                // A bot belongs to the character, not to whoever is talking to
                // it, so the step it is on outlives the conversation. Closing
                // the window - by saying goodbye or by shutting it - replaces
                // the reference the base class holds its talker in, so a
                // different reference means a different conversation, whether
                // that is a second player or the same one clicking again.
                if (!ReferenceEquals(this.Character, this.conversation))
                {
                    this.conversation = this.Character;
                    this.at = 0;
                }

                return this.Say;
            }

            // The last answer of a step is the one that leaves. Every captured
            // step ends with one - "Goodbye" in all of Arete Landing - and
            // anything past the end of the list is a client saying something it
            // was never offered.
            Chapter here = this.Here();
            var chosen = (int)optionId;
            if (here == null || chosen < 0 || chosen >= this.visibleAnswers.Count)
            {
                return this.Leave;
            }

            Response response = this.visibleAnswers[chosen];
            if (response.Action != ScriptAction.None)
            {
                return this.Selected;
            }

            // Legacy captured scripts have no explicit actions. Preserve their
            // observed convention: the final response leaves; others advance.
            return chosen < this.visibleAnswers.Count - 1 ? (KnuBotAction)this.Onwards : this.Leave;
        }

        private Chapter Here()
        {
            return this.at >= 0 && this.at < this.chapters.Count ? this.chapters[this.at] : null;
        }

        /// <summary>
        /// Says this step and offers its answers.
        /// </summary>
        private void Say()
        {
            Chapter here = this.Here();
            if (here == null)
            {
                this.CloseChatWindow();
                return;
            }

            foreach (string line in here.Says)
            {
                // The captures carry Funcom's own line break, written as the
                // two characters backslash and n rather than a newline.
                this.WriteLine(this.Personal(line).Replace("\\n", "\r\n"));
            }

            if (here.LegacyGrants)
            {
                this.HandOver(here.Quest);
            }

            if (here.Wants != null
                && !here.Answers.Any(a => a.Action == ScriptAction.OpenQuestTrade))
            {
                this.AskForItems(here, here.Quest);
            }

            // A step with nothing to answer would leave the window open with no
            // way out of it. The extract drops those, and this is here so that
            // a hand-written row cannot reintroduce one.
            this.visibleAnswers.Clear();
            this.visibleAnswers.AddRange(here.Answers.Where(this.IsAvailable));
            if (this.visibleAnswers.Count == 0)
            {
                this.CloseChatWindow();
                return;
            }

            this.SendAnswerList(this.visibleAnswers.Select(a => this.Personal(a.Text)).ToArray());
        }

        /// <summary>
        /// The live server puts the listening player's own name into some
        /// lines. A script keeps a placeholder there instead, filled in here
        /// with whoever is talking, so no player's name is stored in a script.
        /// </summary>
        private string Personal(string text)
        {
            // Nobody to name when the talker has gone away mid-conversation;
            // the line goes nowhere then, so it is left as it is.
            ICharacter talker = this.GetCharacter();
            return talker == null ? text : text.Replace(NamePlaceholder, talker.Name);
        }

        private const string NamePlaceholder = "{name}";

        private bool IsAvailable(Response response)
        {
            ICharacter talker = this.GetCharacter();
            DBQuest quest = response.Quest == 0 ? null : QuestManager.Get(response.Quest);
            switch (response.Action)
            {
                case ScriptAction.AcceptQuest:
                    return talker != null && quest != null
                           && !QuestManager.ProgressOf(talker, quest.Id).Any()
                           && QuestManager.ObjectivesOf(quest.Id).Count > 0
                           && QuestManager.IsUnlocked(talker, quest);
                case ScriptAction.TurnInQuest:
                    return talker != null && quest != null
                           && QuestManager.IsComplete(talker, quest.Id)
                           && QuestManager.Wants(talker, quest.Id) == null
                           && QuestManager.ProgressOf(talker, quest.Id)
                               .Any(r => r.State == (int)QuestState.Complete);
                case ScriptAction.OpenQuestTrade:
                    return talker != null && quest != null && QuestManager.Wants(talker, quest.Id) != null;
                default:
                    return true;
            }
        }

        private void Selected()
        {
            int chosen = this.LastAnswer;
            if (chosen < 0 || chosen >= this.visibleAnswers.Count)
            {
                this.CloseChatWindow();
                return;
            }

            Response response = this.visibleAnswers[chosen];
            ICharacter talker = this.GetCharacter();
            switch (response.Action)
            {
                case ScriptAction.AcceptQuest:
                    if (QuestManager.Accept(talker, response.Quest))
                    {
                        // A mission may ask the player to speak to the same
                        // NPC whose sentence accepted it. The conversation is
                        // already open, so account for that talk after the
                        // mission exists rather than requiring a close/reopen.
                        QuestManager.OnTalk(
                            talker,
                            Pool.Instance.GetObject<ICharacter>(
                                talker.Playfield.Identity,
                                this.KnuBotIdentity));
                    }
                    this.Onwards();
                    break;
                case ScriptAction.TurnInQuest:
                    QuestManager.HandIn(talker, response.Quest);
                    this.Onwards();
                    break;
                case ScriptAction.OpenQuestTrade:
                    this.AskForItems(this.Here(), response.Quest);
                    this.SuspendDialogContinuation();
                    break;
                case ScriptAction.CloseDialogue:
                    this.CloseChatWindow();
                    break;
            }
        }

        /// <summary>
        /// Moves on a step. Deliberately says nothing.
        /// </summary>
        /// <remarks>
        /// The framework says it. After handling any answer that is not
        /// DialogStart, BaseKnuBot.Answer calls itself once more with
        /// DialogStart, which lands on Say - so a Say here would put every line
        /// of every step on the screen twice, with two copies of the answer
        /// list under it.
        ///
        /// The bot that stood here before never noticed because taking a quest
        /// closed the window, and a closed window is what stops that
        /// re-entry.
        /// </remarks>
        private void Onwards()
        {
            this.at++;
        }

        private void Leave()
        {
            this.CloseChatWindow();
        }

        /// <summary>
        /// Gives out or takes back whatever this character owes the player.
        /// </summary>
        /// <remarks>
        /// Which quest is not read from the script, even though the row names
        /// one. A captured step can have handed over more than one - Rex
        /// Larsson's granted three in one session - and which one a particular
        /// player is due depends on how far along the chain they are. So the
        /// script says where the handover happens and the chain says what
        /// changes hands.
        ///
        /// That holds for quests written in the game too. Two of them on one
        /// character come out in id order, which is the order they were
        /// written; to make the second wait for the first, say so with
        /// /questedit requires.
        ///
        /// Finishing before starting, because a chain is walked by going back
        /// to whoever sent you and the errand you just did is the reason you
        /// are standing there.
        /// </remarks>
        private void HandOver(int questId = 0)
        {
            ICharacter talker = this.GetCharacter();
            if (talker == null)
            {
                return;
            }

            IEnumerable<DBQuest> candidates = questId == 0
                                                  ? this.Mine()
                                                  : this.Mine().Where(q => q.Id == questId);

            DBQuest finished = candidates
                .Where(q => QuestManager.IsComplete(talker, q.Id))
                .FirstOrDefault(
                    q => QuestManager.ProgressOf(talker, q.Id).Any(r => r.State == (int)QuestState.Complete));

            if (finished != null)
            {
                QuestManager.HandIn(talker, finished.Id);
                return;
            }

            DBQuest next = candidates
                .Where(q => !QuestManager.ProgressOf(talker, q.Id).Any())
                .Where(q => QuestManager.ObjectivesOf(q.Id).Count > 0)
                .Where(q => QuestManager.IsUnlocked(talker, q))
                .OrderBy(q => q.Id)
                .FirstOrDefault();

            if (next != null)
            {
                QuestManager.Accept(talker, next.Id);
            }
        }

        /// <summary>
        /// The quests this character is responsible for.
        /// </summary>
        private IEnumerable<DBQuest> Mine()
        {
            return QuestManager.All().Where(q => q.GiverId == this.KnuBotIdentity.Instance);
        }

        /// <summary>
        /// Opens the trade window, so the player can hand over what was asked
        /// for.
        /// </summary>
        /// <remarks>
        /// Only when something is actually wanted. A step that asks for an item
        /// is on the same walk as every other step, so a player who has not
        /// taken the quest yet - or who has already handed the thing in - will
        /// reach it too, and an empty box with nothing to put in it is worse
        /// than no box.
        /// </remarks>
        private void AskForItems(Chapter here, int questId = 0)
        {
            ICharacter talker = this.GetCharacter();
            if (talker == null)
            {
                return;
            }

            IList<DBQuestObjective> wanted = this.WantedAll(talker, questId);
            if (wanted.Count == 0)
            {
                return;
            }

            // Emptied rather than cleared: a box left with something in it -
            // by a disconnection, say, which never closes the window - still
            // holds a player's property.
            this.ReturnEverything();
            this.tradingQuest = questId != 0 ? questId : wanted[0].QuestId;
            int remaining = wanted.Sum(o => this.Remaining(talker, o));
            this.StartTrade(
                here != null && !string.IsNullOrEmpty(here.Wants)
                    ? here.Wants
                    : wanted.Count == 1
                          ? "Hand me the " + wanted[0].Target + "."
                          : "Place the requested items in the box.",
                QuestTradeRules.SlotCount(remaining));
        }

        /// <summary>
        /// What this character is waiting to be handed, if anything.
        /// </summary>
        private DBQuestObjective Wanted(ICharacter talker, int questId = 0)
        {
            if (questId != 0)
            {
                return QuestManager.Wants(talker, questId);
            }

            Chapter here = this.Here();
            if (here != null && here.Quest != 0)
            {
                DBQuestObjective named = QuestManager.Wants(talker, here.Quest);
                if (named != null)
                {
                    return named;
                }
            }

            return this.Mine().Select(q => QuestManager.Wants(talker, q.Id)).FirstOrDefault(o => o != null);
        }

        private IList<DBQuestObjective> WantedAll(ICharacter talker, int questId = 0)
        {
            if (questId != 0)
            {
                return QuestManager.WantsAll(talker, questId);
            }

            DBQuestObjective first = this.Wanted(talker);
            return first == null
                       ? new List<DBQuestObjective>()
                       : QuestManager.WantsAll(talker, first.QuestId);
        }

        private int Remaining(ICharacter talker, DBQuestObjective objective)
        {
            DBCharacterQuest row = QuestManager.ProgressOf(talker, objective.QuestId)
                .FirstOrDefault(r => r.Ordinal == objective.Ordinal);
            return Math.Max(0, objective.Required - (row == null ? 0 : row.Progress));
        }

        /// <summary>
        /// The player has closed the trade window.
        /// </summary>
        /// <remarks>
        /// Keeps what was asked for, gives back everything else, and pays out
        /// if that finished the quest. Anything not kept goes back into the
        /// inventory it came out of: an item the player is still carrying is
        /// recoverable and one that has been quietly eaten is not.
        ///
        /// The rejected-items message the protocol has for this is not sent.
        /// It is meant for a trade still in progress, the window is gone by the
        /// time this runs, and no capture shows what the client does with one
        /// that arrives afterwards - so the items go back and the player is
        /// told in words.
        /// </remarks>
        public override void TradeFinish(bool declined)
        {
            ICharacter talker = this.GetCharacter();
            if (talker == null || declined)
            {
                base.TradeFinish(declined);
                return;
            }

            IList<DBQuestObjective> wanted = this.WantedAll(talker, this.tradingQuest);
            if (wanted.Count == 0)
            {
                base.TradeFinish(false);
                return;
            }

            var accepted = new Dictionary<DBQuestObjective, int>();
            var consumed = new Dictionary<Traded, int>();
            foreach (Traded held in this.Escrow.ToList())
            {
                DBQuestObjective objective = wanted.FirstOrDefault(
                    o => this.Remaining(talker, o) - (accepted.ContainsKey(o) ? accepted[o] : 0) > 0
                         && this.Matches(held.Item, o));
                if (objective == null)
                {
                    continue;
                }

                int alreadyAccepted = accepted.ContainsKey(objective) ? accepted[objective] : 0;
                int remaining = this.Remaining(talker, objective);
                int available = Math.Max(1, held.Item.MultipleCount);
                int take = QuestTradeRules.AmountToConsume(remaining, alreadyAccepted, available);
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
                this.WriteLine("That is not what I asked you for.");
                return;
            }

            // Wrong items go straight back. Accepted items remain in escrow
            // until the database transaction decides whether they were really
            // consumed.
            int refused = 0;
            foreach (Traded held in this.Escrow.Where(h => !consumed.ContainsKey(h)).ToList())
            {
                this.Escrow.Remove(held);
                this.GiveBack(held, true);
                refused++;
            }

            if (refused > 0)
            {
                this.WriteLine("I returned everything that was not required.");
            }

            var partialReturns = new Dictionary<Traded, KeyValuePair<IInventoryPage, int>>();
            foreach (KeyValuePair<Traded, int> entry in consumed)
            {
                int available = Math.Max(1, entry.Key.Item.MultipleCount);
                if (entry.Value >= available)
                {
                    continue;
                }

                IInventoryPage page = talker.BaseInventory[talker.BaseInventory.StandardPage];
                int slot = page.FindFreeSlot();
                bool returnedPartial = false;
                if (slot >= 0)
                {
                    entry.Key.Item.MultipleCount = available - entry.Value;
                    try
                    {
                        returnedPartial = page.Add(slot, entry.Key.Item) == InventoryError.OK;
                    }
                    catch (ArgumentException)
                    {
                        // Inventory placement can race another operation after
                        // FindFreeSlot. Restore the stack instead of allowing
                        // that race to escape the trade message handler.
                        returnedPartial = false;
                    }

                    if (!returnedPartial)
                    {
                        entry.Key.Item.MultipleCount = available;
                    }
                }

                if (!returnedPartial)
                {
                    foreach (Traded restore in consumed.Keys.ToList())
                    {
                        KeyValuePair<IInventoryPage, int> alreadyReturned;
                        if (partialReturns.TryGetValue(restore, out alreadyReturned))
                        {
                            alreadyReturned.Key.Remove(alreadyReturned.Value);
                            restore.Item.MultipleCount += consumed[restore];
                        }

                        this.Escrow.Remove(restore);
                        this.GiveBack(restore, true);
                    }

                    this.WriteLine("Make room in your inventory before handing over part of a stack.");
                    return;
                }

                partialReturns[entry.Key] = new KeyValuePair<IInventoryPage, int>(page, slot);
            }

            int questId = this.tradingQuest != 0 ? this.tradingQuest : accepted.Keys.First().QuestId;
            var amountsByOrdinal = accepted.ToDictionary(x => x.Key.Ordinal, x => x.Value);
            bool saved = QuestManager.ApplyTradeHandIn(talker, questId, amountsByOrdinal);
            if (!saved)
            {
                foreach (KeyValuePair<Traded, int> entry in consumed)
                {
                    KeyValuePair<IInventoryPage, int> returned;
                    if (partialReturns.TryGetValue(entry.Key, out returned))
                    {
                        returned.Key.Remove(returned.Value);
                        entry.Key.Item.MultipleCount += entry.Value;
                    }

                    this.Escrow.Remove(entry.Key);
                    this.GiveBack(entry.Key, true);
                }

                return;
            }

            foreach (KeyValuePair<Traded, int> entry in consumed)
            {
                KeyValuePair<IInventoryPage, int> returned;
                if (partialReturns.TryGetValue(entry.Key, out returned))
                {
                    ContainerAddItemMessageHandler.Default.Send(
                        talker,
                        new Identity
                            {
                                Type = IdentityType.KnuBotTradeWindow,
                                Instance = entry.Key.Where.Instance
                            },
                        0x6f);
                }

                this.Escrow.Remove(entry.Key);
            }

            bool finished = this.tradingQuest != 0
                            && QuestManager.ProgressOf(talker, this.tradingQuest)
                                .All(r => r.State == (int)QuestState.HandedIn);
            if (!finished)
            {
                IList<DBQuestObjective> stillWanted = this.WantedAll(talker, this.tradingQuest);
                this.WriteLine(
                    "I still need "
                    + string.Join(
                        ", ",
                        stillWanted.Select(o => this.Remaining(talker, o) + " " + o.Target))
                    + ".");
                return;
            }

            // And on with the conversation, so the thank-you is the next thing
            // said. Nothing else will do it: a trade window closing is not an
            // answer, so the framework does not come back through Say the way
            // it does after a click.
            if (this.GetCharacter() != null)
            {
                this.tradingQuest = 0;
                this.Onwards();
                this.Say();
            }
        }

        private bool Matches(Item item, DBQuestObjective objective)
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

        /// <summary>
        /// One step: what is said, what can be said back, and what changes
        /// hands.
        /// </summary>
        private sealed class Chapter
        {
            public readonly List<string> Says = new List<string>();

            public readonly List<Response> Answers = new List<Response>();

            public bool LegacyGrants;

            /// <summary>
            /// The quest this step is about, where the row named one.
            /// </summary>
            public int Quest;

            /// <summary>
            /// The wording on the trade window, where this step opens one.
            /// Null where it does not, which is every captured step.
            /// </summary>
            public string Wants;
        }

        private sealed class Response
        {
            public string Text;

            public ScriptAction Action;

            public int Quest;
        }
    }
}
