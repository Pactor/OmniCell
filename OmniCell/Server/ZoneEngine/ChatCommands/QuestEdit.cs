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

namespace ZoneEngine.ChatCommands
{
    #region Usings

    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Text;

    using OmniCell.Core.Entities;
    using OmniCell.Core.Items;
    using OmniCell.Core.Vector;
    using OmniCell.Database.Dao;
    using OmniCell.Database.Entities;
    using OmniCell.Enums;
    using OmniCell.ObjectManager;

    using SmokeLounge.AOtomation.Messaging.GameData;

    using ZoneEngine.Core.Controllers;
    using ZoneEngine.Core.KnuBot;
    using ZoneEngine.Core.MessageHandlers;
    using ZoneEngine.Core.Quests;
    using ZoneEngine.Core;

    #endregion

    /// <summary>
    /// Writing quests and conversations from inside the game.
    /// </summary>
    /// <remarks>
    /// This writes the quest, objective and dialogue tables. There is no second
    /// kind of quest and no second kind of conversation: what comes out of a
    /// session with this command uses the same emulator-owned schema and
    /// ScriptedKnuBot interpreter as converted content, and survives a restart
    /// because the playfield reads those tables when it loads.
    ///
    /// That is the point of doing it this way round. A quest written here goes
    /// into the mission window through the same three messages the live server
    /// sends, counts down the same way as you do it, and pays out at the same
    /// moment - because it is the same code path, not a copy of it.
    ///
    /// A worked example, standing where the character should be:
    ///
    ///   /npc create Sergeant Blank
    ///   /npc model 26151                      target him first
    ///   /questedit new Clear the yard
    ///   /questedit desc Six of them got in through the fence.
    ///   /questedit kill 6 Malfunctioning Cleaning Robot
    ///   /questedit reward 500 250
    ///   /questedit say Good, someone competent. The yard is crawling.
    ///   /questedit say Six of them. Deal with it.
    ///   /questedit option I'll take care of it.
    ///   /questedit accept                     that sentence files the mission
    ///   /questedit option Goodbye.
    ///   /questedit step
    ///   /questedit say That's the lot of them. Here.
    ///   /questedit option I finished the job.
    ///   /questedit turnin                     that sentence pays out
    ///   /questedit option Goodbye.
    ///
    /// And for one that wants something handed over rather than something
    /// killed, "handin 1 150923" in place of the kill line, followed by
    /// "option I brought it." and "trade Put it in the box." on the turn-in
    /// step.
    /// </remarks>
    public class QuestEdit : AOChatCommand
    {
        #region Fields

        /// <summary>
        /// What each author is part way through writing.
        /// </summary>
        /// <remarks>
        /// Static because the command object does not outlive one line typed
        /// into the chat window - ScriptCompiler makes a new one every time -
        /// and keyed by author because two of them may be writing at once.
        /// </remarks>
        private static readonly ConcurrentDictionary<int, Draft> Drafts = new ConcurrentDictionary<int, Draft>();

        #endregion

        #region Public Methods and Operators

        public override bool CheckCommandArguments(string[] args)
        {
            return args.Length >= 2;
        }

        public override void CommandHelp(ICharacter character)
        {
            Tell(
                character,
                "Target a character, then:\n"
                + "  /questedit new <name>          start a quest, given by that character\n"
                + "  /questedit edit <id>           carry on with one that exists\n"
                + "  /questedit desc <text>         what the mission window says\n"
                + "  /questedit reward <cash> <xp>  what handing it in pays\n"
                + "  /questedit itemreward <id> <n> <accept|turnin>  add an inventory reward\n"
                + "  /questedit unitemreward <row>  remove an inventory reward shown by 'show'\n"
                + "  /questedit requires <id>       the quest that has to be done first\n"
                + "\nWhat has to be done - as many as you like, all of them at once:\n"
                + "  /questedit kill <n> <name>     kill that many of them\n"
                + "  /questedit talk <name>         go and speak to somebody\n"
                + "  /questedit collect <n> <item>  end up holding them\n"
                + "  /questedit purchase <n> <item> buy them from a vendor\n"
                + "  /questedit tradeskill <n> <item> build them in the tradeskill kit\n"
                + "  /questedit handin <n> <id|unique item name>  put that exact item in the giver's hands\n"
                + "  /questedit equip <item>        wear it\n"
                + "  /questedit use <id>            use a fixture of the playfield\n"
                + "  /questedit useon <id|name>     use an inventory item on it\n"
                + "  /questedit reach               come to where you are standing now\n"
                + "  /questedit unobjective <n>     remove an objective shown by 'show'\n"
                + "\nWhat is said, a step at a time:\n"
                + "  /questedit say <text>          a line the character says\n"
                + "  /questedit option <text>       a line you can click\n"
                + "  /questedit accept              that option accepts this quest\n"
                + "  /questedit turnin              that option completes and rewards it\n"
                + "  /questedit trade <text>        that option opens its item box\n"
                + "  /questedit gives               legacy step-level accept/turn-in\n"
                + "  /questedit wants <text>        legacy step-level item box\n"
                + "  /questedit step                done with this step, on to the next\n"
                + "\n  /questedit show                read back what you have written\n"
                + "  /questedit undo                take out the last line\n"
                + "  /questedit unline <row>        remove any dialogue row shown by 'show'\n"
                + "  /questedit done                stop editing\n"
                + "  /questedit delete              throw the whole quest away");
        }

        public override void ExecuteCommand(ICharacter character, Identity target, string[] args)
        {
            string verb = args[1].ToLower();
            string tail = string.Join(" ", args.Skip(2)).Trim();

            if (verb == "new")
            {
                this.Begin(character, target, tail);
                return;
            }

            if (verb == "edit")
            {
                this.Resume(character, tail);
                return;
            }

            Draft draft;
            if (!Drafts.TryGetValue(character.Identity.Instance, out draft))
            {
                Tell(character, "Nothing is being written. /questedit new <name> with a character targeted.");
                return;
            }

            DBQuest quest = QuestManager.Get(draft.Quest);
            if (quest == null)
            {
                Drafts.TryRemove(character.Identity.Instance, out draft);
                Tell(character, "That quest is gone.");
                return;
            }

            switch (verb)
            {
                case "desc":
                    quest.Description = tail;
                    this.Keep(character, quest, "Description set.");
                    break;

                case "reward":
                    this.Reward(character, quest, args);
                    break;

                case "itemreward":
                    this.ItemReward(character, quest, args);
                    break;

                case "unitemreward":
                    this.RemoveItemReward(character, quest, tail);
                    break;

                case "requires":
                    this.Requires(character, quest, tail);
                    break;

                case "kill":
                    this.Counted(character, draft, QuestObjectiveType.Kill, args, tail);
                    break;

                case "collect":
                    this.Counted(character, draft, QuestObjectiveType.Collect, args, tail);
                    break;

                case "purchase":
                    this.Counted(character, draft, QuestObjectiveType.Purchase, args, tail);
                    break;

                case "tradeskill":
                    this.Counted(character, draft, QuestObjectiveType.TradeSkill, args, tail);
                    break;

                case "handin":
                    this.Counted(character, draft, QuestObjectiveType.HandIn, args, tail);
                    break;

                case "talk":
                    this.Objective(character, draft, QuestObjectiveType.TalkTo, tail, 1);
                    break;

                case "equip":
                    this.Objective(character, draft, QuestObjectiveType.Equip, tail, 1);
                    break;

                case "use":
                    this.Objective(character, draft, QuestObjectiveType.Use, tail, 1);
                    break;

                case "useon":
                    this.Objective(character, draft, QuestObjectiveType.UseItemOn, tail, 1);
                    break;

                case "reach":
                    this.Reach(character, draft);
                    break;

                case "unobjective":
                    this.RemoveObjective(character, draft, tail);
                    break;

                case "say":
                    this.Line(character, draft, ScriptLine.Says, tail);
                    break;

                case "option":
                    this.Line(character, draft, ScriptLine.Answer, tail);
                    break;

                case "accept":
                    this.AnswerAction(character, draft, ScriptAction.AcceptQuest);
                    break;

                case "turnin":
                    this.AnswerAction(character, draft, ScriptAction.TurnInQuest);
                    break;

                case "trade":
                    if (this.AnswerAction(character, draft, ScriptAction.OpenQuestTrade)
                        && tail.Length > 0)
                    {
                        this.Line(character, draft, ScriptLine.WantsItems, tail);
                    }
                    break;

                case "wants":
                    this.Line(character, draft, ScriptLine.WantsItems, tail);
                    break;

                case "gives":
                    this.Gives(character, draft);
                    break;

                case "step":
                    this.Step(character, draft);
                    break;

                case "show":
                    this.Show(character, draft, quest);
                    break;

                case "undo":
                    this.Undo(character, draft);
                    break;

                case "unline":
                    this.RemoveLine(character, draft, tail);
                    break;

                case "done":
                    Drafts.TryRemove(character.Identity.Instance, out draft);
                    Tell(character, "Done with \"" + quest.Name + "\".");
                    break;

                case "delete":
                    this.Delete(character, draft, quest);
                    break;

                default:
                    this.CommandHelp(character);
                    break;
            }
        }

        public override int GMLevelNeeded()
        {
            return 1;
        }

        public override List<string> ListCommands()
        {
            return new List<string> { "questedit" };
        }

        #endregion

        #region Methods

        private void Begin(ICharacter character, Identity target, string name)
        {
            if (name.Length == 0)
            {
                Tell(character, "It needs a name: /questedit new Clear the yard");
                return;
            }

            ICharacter npc = Pool.Instance.GetObject<ICharacter>(character.Playfield.Identity, target);
            if (npc == null || !(npc.Controller is NPCController))
            {
                Tell(character, "Target the character who should give it out first.");
                return;
            }

            if (MobSpawnDao.Instance.Get(npc.Identity.Instance) == null)
            {
                Tell(character, npc.Name + " has no spawn point, so nothing would survive a restart. /npc save first.");
                return;
            }

            int playfield = character.Playfield.Identity.Instance;
            var quest = new DBQuest
                            {
                                Id = NextQuestId(),
                                Name = name.Length > 255 ? name.Substring(0, 255) : name,
                                Description = string.Empty,
                                GiverId = npc.Identity.Instance,
                                Playfield = playfield,
                                IconId = 0,
                                CashReward = 0,
                                ExperienceReward = 0,
                                Requires = 0
                            };

            QuestDao.Instance.Add(quest);
            QuestWireAuthoring.EnsureQuest(quest);
            QuestManager.Remember(quest.Id);

            Drafts[character.Identity.Instance] = new Draft
                                                      {
                                                          Quest = quest.Id,
                                                          Npc = npc.Identity.Instance,
                                                          Playfield = playfield,
                                                          Step = NextStep(npc.Identity.Instance, playfield),
                                                          Ordinal = 0
                                                      };

            Tell(
                character,
                "\"" + quest.Name + "\" is " + quest.Id + ", given by " + npc.Name + ".\n"
                + "Say what has to be done, then write what he says. /questedit help for the list.");
        }

        private void Resume(ICharacter character, string id)
        {
            int questId;
            if (!int.TryParse(id, out questId))
            {
                Tell(character, "/questedit edit <quest id>");
                return;
            }

            DBQuest quest = QuestManager.Get(questId);
            if (quest == null)
            {
                Tell(character, "There is no quest " + questId + ".");
                return;
            }

            DBMobSpawn giver = MobSpawnDao.Instance.Get(quest.GiverId);
            int playfield = giver == null ? character.Playfield.Identity.Instance : giver.Playfield;

            Drafts[character.Identity.Instance] = new Draft
                                                      {
                                                          Quest = quest.Id,
                                                          Npc = quest.GiverId,
                                                          Playfield = playfield,
                                                          Step = NextStep(quest.GiverId, playfield),
                                                          Ordinal = 0
                                                      };

            Tell(
                character,
                "Editing \"" + quest.Name + "\" (" + quest.Id + "), given by "
                + (giver == null ? "nobody" : giver.Name) + ". Anything said now is a new step.");
        }

        private void Reward(ICharacter character, DBQuest quest, string[] args)
        {
            int cash;
            int xp;
            if (args.Length < 4 || !int.TryParse(args[2], out cash) || !int.TryParse(args[3], out xp))
            {
                Tell(character, "/questedit reward <credits> <experience>");
                return;
            }

            if (cash < 0 || xp < 0)
            {
                Tell(character, "Quest rewards cannot be negative.");
                return;
            }

            quest.CashReward = cash;
            quest.ExperienceReward = xp;
            this.Keep(character, quest, "Pays " + cash + " credits and " + xp + " experience.");
        }

        private void ItemReward(ICharacter character, DBQuest quest, string[] args)
        {
            int itemId;
            int quantity;
            string when = args.Length > 4 ? args[4].ToLowerInvariant() : string.Empty;
            if (args.Length < 5
                || !int.TryParse(args[2], out itemId)
                || !int.TryParse(args[3], out quantity)
                || quantity < 1
                || (when != "accept" && when != "turnin"))
            {
                Tell(character, "/questedit itemreward <item id> <quantity> <accept|turnin>");
                return;
            }

            if (!ItemLoader.ItemList.ContainsKey(itemId))
            {
                Tell(character, "There is no converted item template " + itemId + ".");
                return;
            }

            bool onAccept = when == "accept";
            QuestItemRewardDao.Instance.Add(
                new DBQuestItemReward
                    {
                        QuestId = quest.Id,
                        ItemId = itemId,
                        Quantity = quantity,
                        GrantOnAccept = onAccept ? 1 : 0
                    });
            QuestWireAuthoring.RebuildRewards(quest.Id);
            QuestManager.Remember(quest.Id);

            DBItemName itemName = ItemNamesDao.Instance.Get(itemId);
            Tell(
                character,
                "Added " + quantity + " x " + (itemName == null ? itemId.ToString() : itemName.Name)
                + (onAccept ? " when accepted." : " when turned in."));
        }

        private void RemoveItemReward(ICharacter character, DBQuest quest, string rowText)
        {
            int rowId;
            if (!int.TryParse(rowText, out rowId))
            {
                Tell(character, "/questedit unitemreward <reward row shown by /questedit show>");
                return;
            }

            DBQuestItemReward reward = QuestManager.ItemRewardsOf(quest.Id).FirstOrDefault(r => r.Id == rowId);
            if (reward == null)
            {
                Tell(character, "That item reward does not belong to this quest.");
                return;
            }

            QuestItemRewardDao.Instance.Delete(rowId);
            QuestWireAuthoring.RebuildRewards(quest.Id);
            QuestManager.Remember(quest.Id);
            Tell(character, "Removed item reward " + rowId + ".");
        }

        private void Requires(ICharacter character, DBQuest quest, string tail)
        {
            int before;
            if (!int.TryParse(tail, out before))
            {
                Tell(character, "/questedit requires <quest id>, or 0 for none");
                return;
            }

            if (before != 0 && QuestManager.Get(before) == null)
            {
                Tell(character, "There is no quest " + before + ".");
                return;
            }

            quest.Requires = before;
            this.Keep(
                character,
                quest,
                before == 0
                    ? "Can be taken straight away."
                    : "Only offered once " + QuestManager.Get(before).Name + " is handed in.");
        }

        private void Keep(ICharacter character, DBQuest quest, string said)
        {
            QuestDao.Instance.Save(quest);
            QuestManager.Remember(quest.Id);
            Tell(character, said);
        }

        private void Counted(
            ICharacter character,
            Draft draft,
            QuestObjectiveType kind,
            string[] args,
            string tail)
        {
            int count;
            if (args.Length < 4 || !int.TryParse(args[2], out count) || count < 1)
            {
                Tell(character, "/questedit " + kind.ToString().ToLower() + " <how many> <what>");
                return;
            }

            this.Objective(character, draft, kind, string.Join(" ", args.Skip(3)).Trim(), count);
        }

        private void Reach(ICharacter character, Draft draft)
        {
            Coordinate here = character.Coordinates();
            this.Objective(
                character,
                draft,
                QuestObjectiveType.Reach,
                here.x.ToString("0", CultureInfo.InvariantCulture) + ","
                + here.z.ToString("0", CultureInfo.InvariantCulture),
                1);
        }

        private void RemoveObjective(ICharacter character, Draft draft, string ordinalText)
        {
            int ordinal;
            if (!int.TryParse(ordinalText, out ordinal))
            {
                Tell(character, "/questedit unobjective <objective number shown by /questedit show>");
                return;
            }

            DBQuestObjective objective = QuestManager.ObjectivesOf(draft.Quest)
                .FirstOrDefault(o => o.Ordinal == ordinal);
            if (objective == null)
            {
                Tell(character, "That objective does not belong to this quest.");
                return;
            }

            QuestObjectiveDao.Instance.Delete(new { QuestId = draft.Quest, Ordinal = ordinal });
            DBQuest authoredQuest = QuestDao.Instance.Get(draft.Quest);
            DBQuestObjective nextObjective = QuestObjectiveDao.Instance.GetWhere(new { QuestId = draft.Quest })
                .OrderBy(o => o.Ordinal).FirstOrDefault();
            QuestWireAuthoring.RebuildAction(authoredQuest, nextObjective);
            QuestManager.Remember(draft.Quest);
            Tell(character, "Removed objective " + ordinal + ": " + objective.Target + ".");
        }

        private void Objective(ICharacter character, Draft draft, QuestObjectiveType kind, string what, int count)
        {
            if (what.Length == 0)
            {
                Tell(character, "It needs something to point at.");
                return;
            }

            if ((kind == QuestObjectiveType.Kill || kind == QuestObjectiveType.TalkTo)
                && !MobSpawnDao.Instance.GetWhere(new { Playfield = draft.Playfield, Name = what }).Any())
            {
                Tell(character, "Nothing called \"" + what + "\" has a spawn in this playfield.");
                return;
            }

            int targetLowId = 0;
            int targetHighId = 0;
            int targetQuality = 0;
            if (kind == QuestObjectiveType.HandIn)
            {
                if (!this.ResolveHandInItem(
                        character,
                        what,
                        out what,
                        out targetLowId,
                        out targetHighId,
                        out targetQuality))
                {
                    return;
                }
            }
            else if ((kind == QuestObjectiveType.Collect
                      || kind == QuestObjectiveType.Purchase
                      || kind == QuestObjectiveType.TradeSkill
                      || kind == QuestObjectiveType.Equip)
                && !ItemNamesDao.Instance.GetWhere(new { Name = what, ItemType = "Item" })
                    .Any(n => ItemLoader.ItemList.ContainsKey(n.Id)))
            {
                Tell(character, "No converted inventory item is named \"" + what + "\".");
                return;
            }

            int fixtureId;
            if (kind == QuestObjectiveType.Use
                && (!int.TryParse(what, out fixtureId)
                    || !StaticDynelDao.Instance.GetWhere(
                        new { Instance = fixtureId, Playfield = draft.Playfield }).Any()))
            {
                Tell(character, "There is no fixture " + what + " in this playfield.");
                return;
            }

            if (kind == QuestObjectiveType.UseItemOn && !this.HasUseTarget(character, draft, what))
            {
                Tell(character, "There is nothing called \"" + what + "\" here to use an item on.");
                return;
            }

            if (kind == QuestObjectiveType.HandIn
                && QuestManager.ObjectivesOf(draft.Quest)
                       .Where(o => o.ObjectiveType == (int)QuestObjectiveType.HandIn)
                       .Sum(o => o.Required) + count > 6)
            {
                Tell(character, "The client hand-in box supports at most six required item slots.");
                return;
            }

            int ordinal = QuestManager.ObjectivesOf(draft.Quest).Select(o => o.Ordinal).DefaultIfEmpty(-1).Max() + 1;

            QuestObjectiveDao.Instance.Add(
                new DBQuestObjective
                    {
                        QuestId = draft.Quest,
                        Ordinal = ordinal,
                        ObjectiveType = (int)kind,
                        Target = what.Length > 255 ? what.Substring(0, 255) : what,
                        TargetLowId = targetLowId,
                        TargetHighId = targetHighId,
                        TargetQuality = targetQuality,
                        Required = count
                    });

            DBQuest authoredQuest = QuestDao.Instance.Get(draft.Quest);
            DBQuestObjective firstObjective = QuestObjectiveDao.Instance.GetWhere(new { QuestId = draft.Quest })
                .OrderBy(o => o.Ordinal).FirstOrDefault();
            QuestWireAuthoring.RebuildAction(authoredQuest, firstObjective);
            QuestManager.Remember(draft.Quest);
            Tell(character, "Added: " + kind + " " + what + (count > 1 ? " x" + count : string.Empty));

            if (kind == QuestObjectiveType.HandIn)
            {
                Tell(character, "Put a /questedit wants line on the step where he should ask for it.");
            }
        }

        private bool ResolveHandInItem(
            ICharacter character,
            string supplied,
            out string name,
            out int lowId,
            out int highId,
            out int quality)
        {
            name = supplied;
            lowId = 0;
            highId = 0;
            quality = 0;

            int selectedId;
            if (!int.TryParse(supplied, out selectedId))
            {
                List<DBItemName> matches = ItemNamesDao.Instance
                    .GetWhere(new { Name = supplied, ItemType = "Item" })
                    .Where(n => ItemLoader.ItemList.ContainsKey(n.Id))
                    .OrderBy(n => n.Id)
                    .ToList();
                if (matches.Count == 0)
                {
                    Tell(character, "No converted inventory item is named \"" + supplied + "\".");
                    return false;
                }

                if (matches.Count != 1)
                {
                    Tell(
                        character,
                        "That name has more than one converted template. Use one exact item ID: "
                        + string.Join(", ", matches.Select(n => n.Id.ToString()).ToArray()) + ".");
                    return false;
                }

                selectedId = matches[0].Id;
            }

            ItemTemplate selected;
            if (!ItemLoader.ItemList.TryGetValue(selectedId, out selected))
            {
                Tell(character, "There is no converted item template " + selectedId + ".");
                return false;
            }

            DBItemName selectedName = ItemNamesDao.Instance.Get(selectedId);
            if (selectedName == null || !string.Equals(selectedName.ItemType, "Item", StringComparison.OrdinalIgnoreCase))
            {
                Tell(character, "Converted item template " + selectedId + " has no inventory-item name.");
                return false;
            }

            List<ItemTemplate> family = selected.Relations
                .Where(ItemLoader.ItemList.ContainsKey)
                .Select(id => ItemLoader.ItemList[id])
                .OrderBy(t => t.Quality)
                .ThenBy(t => t.ID)
                .ToList();
            if (family.Count == 0)
            {
                Tell(character, "Converted item template " + selectedId + " has no usable relation family.");
                return false;
            }

            name = selectedName.Name;
            lowId = family.First().ID;
            highId = family.Last().ID;
            quality = selected.Quality;
            return lowId > 0 && highId > 0 && quality > 0;
        }

        private bool HasUseTarget(ICharacter character, Draft draft, string target)
        {
            int instance;
            if (int.TryParse(target, out instance))
            {
                return StaticDynelDao.Instance.GetWhere(
                           new { Instance = instance, Playfield = draft.Playfield }).Any()
                       || MobSpawnDao.Instance.GetWhere(
                           new { Id = instance, Playfield = draft.Playfield }).Any();
            }

            if (MobSpawnDao.Instance.GetWhere(new { Playfield = draft.Playfield, Name = target }).Any())
            {
                return true;
            }

            return Pool.Instance.GetAll<StaticDynel>(character.Playfield.Identity).Any(
                fixture => string.Equals(
                    TradeSkill.Instance.GetItemName(
                        fixture.Template.ID,
                        fixture.Template.ID,
                        fixture.Template.Quality),
                    target,
                    StringComparison.OrdinalIgnoreCase));
        }

        private void Line(ICharacter character, Draft draft, ScriptLine kind, string text)
        {
            if (text.Length == 0)
            {
                Tell(character, "Nothing to add.");
                return;
            }

            KnuBotScriptDao.Instance.Add(
                new DBKnuBotScript
                    {
                        Npc = draft.Npc,
                        Playfield = draft.Playfield,
                        Step = draft.Step,
                        Ordinal = draft.Ordinal++,
                        Kind = (int)kind,
                        Text = text,
                        Grants = draft.Gives ? draft.Quest : 0
                    });

            this.Rehearse(character, draft);

            switch (kind)
            {
                case ScriptLine.Says:
                    Tell(character, "Step " + draft.Step + ", says: " + text);
                    break;
                case ScriptLine.WantsItems:
                    Tell(character, "Step " + draft.Step + " opens the box: " + text);
                    break;
                default:
                    Tell(character, "Step " + draft.Step + ", option: " + text);
                    break;
            }
        }

        /// <summary>
        /// Attaches a quest operation to the most recently written answer.
        /// The action runs only if the player selects that sentence.
        /// </summary>
        private bool AnswerAction(ICharacter character, Draft draft, ScriptAction action)
        {
            DBKnuBotScript answer = this.Lines(draft)
                .Where(l => l.Step == draft.Step && l.Kind == (int)ScriptLine.Answer)
                .OrderBy(l => l.Ordinal)
                .LastOrDefault();

            if (answer == null)
            {
                Tell(character, "Write the player sentence with /questedit option first.");
                return false;
            }

            if (action == ScriptAction.OpenQuestTrade
                && !QuestManager.ObjectivesOf(draft.Quest)
                    .Any(o => o.ObjectiveType == (int)QuestObjectiveType.HandIn))
            {
                Tell(character, "This quest has no handin objective, so it has nothing to request in a box.");
                return false;
            }

            answer.Grants = draft.Quest;
            answer.Action = (int)action;
            KnuBotScriptDao.Instance.Save(answer, new { answer.Id, answer.Grants, answer.Action });
            this.Rehearse(character, draft);
            Tell(character, "Selecting \"" + answer.Text + "\" will " + action + ".");
            return true;
        }

        /// <summary>
        /// Marks the step being written as the one where the quest changes
        /// hands.
        /// </summary>
        /// <remarks>
        /// Both ways round, and deliberately. A step marked this way hands the
        /// quest over to somebody who has not got it and takes it back off
        /// somebody who has finished it, which is how every captured
        /// conversation in Arete Landing behaves: you go back to whoever sent
        /// you, and the same words serve.
        /// </remarks>
        private void Gives(ICharacter character, Draft draft)
        {
            draft.Gives = true;

            // The lines already written for this step get it too, so it does
            // not matter whether this was typed before them or after.
            foreach (DBKnuBotScript line in this.Lines(draft).Where(l => l.Step == draft.Step))
            {
                line.Grants = draft.Quest;
                KnuBotScriptDao.Instance.Save(line, new { line.Id, line.Grants });
            }

            this.Rehearse(character, draft);
            Tell(
                character,
                "Step " + draft.Step + " hands \"" + QuestManager.Get(draft.Quest).Name
                + "\" over, and takes it back when it is done.");
        }

        private void Step(ICharacter character, Draft draft)
        {
            if (draft.Ordinal == 0)
            {
                Tell(character, "Step " + draft.Step + " has nothing in it yet.");
                return;
            }

            if (!this.Lines(draft).Any(l => l.Step == draft.Step && l.Kind == (int)ScriptLine.Answer))
            {
                // A step with no way out of it shuts the window the moment it
                // is reached. Worth saying now rather than finding out in
                // front of a player.
                Tell(character, "Step " + draft.Step + " has no options, so the window will close on it.");
            }

            draft.Step++;
            draft.Ordinal = 0;
            draft.Gives = false;
            Tell(character, "On to step " + draft.Step + ".");
        }

        private void Undo(ICharacter character, Draft draft)
        {
            DBKnuBotScript last = this.Lines(draft)
                .Where(l => l.Step == draft.Step)
                .OrderBy(l => l.Ordinal)
                .LastOrDefault();

            if (last == null)
            {
                Tell(character, "Nothing written on step " + draft.Step + " to take out.");
                return;
            }

            KnuBotScriptDao.Instance.Delete(last.Id);
            draft.Ordinal = Math.Max(0, draft.Ordinal - 1);
            this.Rehearse(character, draft);
            Tell(character, "Took out: " + last.Text);
        }

        private void RemoveLine(ICharacter character, Draft draft, string rowText)
        {
            int rowId;
            if (!int.TryParse(rowText, out rowId))
            {
                Tell(character, "/questedit unline <dialogue row shown by /questedit show>");
                return;
            }

            DBKnuBotScript line = this.Lines(draft).FirstOrDefault(l => l.Id == rowId);
            if (line == null)
            {
                Tell(character, "That dialogue row does not belong to this character.");
                return;
            }

            KnuBotScriptDao.Instance.Delete(line.Id);
            this.Rehearse(character, draft);
            Tell(character, "Removed dialogue row " + line.Id + ": " + line.Text);
        }

        private void Delete(ICharacter character, Draft draft, DBQuest quest)
        {
            // The conversation stays. It belongs to the character rather than
            // to the quest, and a step that used to hand this one over simply
            // stops handing anything over.
            foreach (DBKnuBotScript line in this.Lines(draft).Where(l => l.Grants == quest.Id))
            {
                line.Grants = 0;
                KnuBotScriptDao.Instance.Save(line, new { line.Id, line.Grants });
            }

            QuestObjectiveDao.Instance.Delete(new { QuestId = quest.Id });
            QuestItemRewardDao.Instance.Delete(new { QuestId = quest.Id });
            QuestDao.Instance.Delete(quest.Id);
            QuestManager.Remember(quest.Id);

            Draft gone;
            Drafts.TryRemove(character.Identity.Instance, out gone);

            this.Rehearse(character, draft);
            Tell(character, "\"" + quest.Name + "\" is gone. What he says is still there.");
        }

        private void Show(ICharacter character, Draft draft, DBQuest quest)
        {
            var text = new StringBuilder();
            DBMobSpawn giver = MobSpawnDao.Instance.Get(quest.GiverId);

            text.AppendLine(quest.Id + "  " + quest.Name);
            text.AppendLine("given by " + (giver == null ? "nobody" : giver.Name) + " on playfield " + quest.Playfield);
            text.AppendLine(quest.Description.Length == 0 ? "no description" : quest.Description);
            text.AppendLine("pays " + quest.CashReward + " credits, " + quest.ExperienceReward + " experience");

            foreach (DBQuestItemReward reward in QuestManager.ItemRewardsOf(quest.Id))
            {
                DBItemName itemName = ItemNamesDao.Instance.Get(reward.ItemId);
                text.AppendLine(
                    "item reward " + reward.Id + ": " + reward.Quantity + " x "
                    + (itemName == null ? reward.ItemId.ToString() : itemName.Name)
                    + (reward.GrantOnAccept != 0 ? " on accept" : " on turn-in"));
            }

            if (quest.Requires != 0)
            {
                DBQuest before = QuestManager.Get(quest.Requires);
                text.AppendLine("after " + (before == null ? quest.Requires.ToString(CultureInfo.InvariantCulture) : before.Name));
            }

            text.AppendLine();
            IList<DBQuestObjective> objectives = QuestManager.ObjectivesOf(quest.Id);
            if (objectives.Count == 0)
            {
                text.AppendLine("Nothing to do yet, so it cannot be taken.");
            }

            foreach (DBQuestObjective objective in objectives)
            {
                string identity = objective.TargetLowId > 0
                                      ? " [" + objective.TargetLowId + "/" + objective.TargetHighId
                                        + " QL " + objective.TargetQuality + "]"
                                      : string.Empty;
                text.AppendLine(
                    "  objective " + objective.Ordinal + ": "
                    + (QuestObjectiveType)objective.ObjectiveType + " " + objective.Target
                    + identity
                    + (objective.Required > 1 ? " x" + objective.Required : string.Empty));
            }

            text.AppendLine();
            foreach (var step in this.Lines(draft).GroupBy(l => l.Step).OrderBy(g => g.Key))
            {
                text.AppendLine(
                    "step " + step.Key + (step.Any(l => l.Grants != 0) ? "  (hands the quest over)" : string.Empty));

                foreach (DBKnuBotScript line in step.OrderBy(l => l.Ordinal))
                {
                    switch ((ScriptLine)line.Kind)
                    {
                        case ScriptLine.Says:
                            text.AppendLine("    row " + line.Id + ": \"" + line.Text + "\"");
                            break;
                        case ScriptLine.WantsItems:
                            text.AppendLine("    row " + line.Id + ": [box] " + line.Text);
                            break;
                        default:
                            text.AppendLine("    row " + line.Id + ": > " + line.Text);
                            break;
                    }
                }
            }

            text.AppendLine();
            text.AppendLine("writing step " + draft.Step);
            Tell(character, text.ToString());
        }

        /// <summary>
        /// Puts what has been written so far into the character's mouth.
        /// </summary>
        /// <remarks>
        /// After every change, so the thing can be walked over to and tried
        /// without a restart. It replaces the conversation rather than editing
        /// it, which loses the step anybody talking to it was on - acceptable
        /// while a quest is being written, and the alternative is authoring
        /// blind.
        /// </remarks>
        private void Rehearse(ICharacter character, Draft draft)
        {
            ICharacter npc = Pool.Instance.GetObject<ICharacter>(
                character.Playfield.Identity,
                new Identity { Type = IdentityType.CanbeAffected, Instance = draft.Npc });

            var controller = npc == null ? null : npc.Controller as NPCController;
            if (controller == null)
            {
                return;
            }

            controller.SetKnuBot(new ScriptedKnuBot(npc.Identity, this.Lines(draft)));
        }

        private List<DBKnuBotScript> Lines(Draft draft)
        {
            return KnuBotScriptDao.Instance.GetWhere(new { Npc = draft.Npc, Playfield = draft.Playfield }).ToList();
        }

        /// <summary>
        /// The step number to start writing at.
        /// </summary>
        /// <remarks>
        /// After everything the character already says, because a conversation
        /// is one walk and a second quest on the same character is more of it
        /// rather than a separate menu.
        /// </remarks>
        private static int NextStep(int npc, int playfield)
        {
            return KnuBotScriptDao.Instance.GetWhere(new { Npc = npc, Playfield = playfield })
                       .Select(l => l.Step)
                       .DefaultIfEmpty(-1)
                       .Max() + 1;
        }

        /// <summary>
        /// The next quest id nothing is using.
        /// </summary>
        /// <remarks>
        /// Above Funcom's own, which for Arete Landing run to 1439635571, and
        /// below what an int will hold.
        /// </remarks>
        private static int NextQuestId()
        {
            int highest = QuestDao.Instance.GetAll().Select(q => q.Id).DefaultIfEmpty(0).Max();
            return Math.Max(highest, 1900000000) + 1;
        }

        private static void Tell(ICharacter character, string text)
        {
            character.Playfield.Publish(ChatTextMessageHandler.Default.CreateIM(character, text));
        }

        #endregion

        /// <summary>
        /// A quest part way through being written.
        /// </summary>
        private sealed class Draft
        {
            public int Quest;

            public int Npc;

            public int Playfield;

            /// <summary>
            /// The step being written.
            /// </summary>
            public int Step;

            /// <summary>
            /// How many lines are on it so far.
            /// </summary>
            public int Ordinal;

            /// <summary>
            /// Whether this step is the one that hands the quest over.
            /// </summary>
            public bool Gives;
        }
    }
}
