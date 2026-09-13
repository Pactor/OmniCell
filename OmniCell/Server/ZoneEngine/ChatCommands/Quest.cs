#region License

// Copyright (c) 2005-2014, CellAO Team
// Copyright (c) 2026, OmniCell contributors
//
// This file is part of OmniCell and is distributed under the terms the project
// is licensed under. See the repository root for details.

#endregion

namespace ZoneEngine.ChatCommands
{
    #region Usings

    using System.Collections.Generic;
    using System.Linq;

    using OmniCell.Core.Entities;
    using OmniCell.Database.Entities;
    using OmniCell.Enums;

    using SmokeLounge.AOtomation.Messaging.GameData;

    using ZoneEngine.Core.MessageHandlers;
    using ZoneEngine.Core.Quests;
    using ZoneEngine.Script;

    #endregion

    /// <summary>
    /// Starting, listing and handing in quests from the chat window.
    /// </summary>
    /// <remarks>
    /// Anarchy Online gives quests out through NPC conversation. The server has
    /// the message handlers for those but no conversation engine behind them, so
    /// until it has one there is nothing in the game world that can hand a quest
    /// over. This command is that missing step, and only that step: everything
    /// after accepting - progress, completion, reward - happens on its own.
    ///
    /// It goes away when a KnuBot conversation can do the same thing.
    /// </remarks>
    public class Quest : AOChatCommand
    {
        public override bool CheckCommandArguments(string[] args)
        {
            return args.Length >= 2 && args.Length <= 3;
        }

        public override void CommandHelp(ICharacter character)
        {
            Tell(character, "/quest list            quests you could start here");
            Tell(character, "/quest log             quests you are on");
            Tell(character, "/quest accept <id>     start one");
            Tell(character, "/quest handin <id>     turn a finished one in");
            Tell(character, "/quest abandon <id>    give one up");
        }

        public override void ExecuteCommand(ICharacter character, Identity target, string[] args)
        {
            string verb = args[1].ToLower();

            if (verb == "list")
            {
                int playfield = character.Playfield.Identity.Instance;
                var here = QuestManager.All()
                    .Where(q => q.Playfield == 0 || q.Playfield == playfield)
                    .OrderBy(q => q.Name)
                    .ToList();

                if (here.Count == 0)
                {
                    Tell(character, "No quests are defined for this playfield.");
                    return;
                }

                foreach (DBQuest quest in here)
                {
                    int objectives = QuestManager.ObjectivesOf(quest.Id).Count;
                    Tell(
                        character,
                        quest.Id + "  " + quest.Name
                        + (objectives == 0 ? "   (no objectives recorded)" : string.Empty));
                }

                return;
            }

            if (verb == "log")
            {
                var mine = QuestManager.All()
                    .Where(q => QuestManager.ProgressOf(character, q.Id).Any())
                    .ToList();

                if (mine.Count == 0)
                {
                    Tell(character, "You are not on any quests.");
                    return;
                }

                foreach (DBQuest quest in mine)
                {
                    bool complete = QuestManager.IsComplete(character, quest.Id);
                    bool handedIn = QuestManager.ProgressOf(character, quest.Id)
                        .All(r => r.State == (int)QuestState.HandedIn);

                    string state = handedIn ? "done" : complete ? "ready to hand in" : "in progress";
                    Tell(character, quest.Id + "  " + quest.Name + "  - " + state);
                    QuestManager.Describe(character, quest.Id);
                }

                return;
            }

            int questId;
            if (args.Length < 3 || !int.TryParse(args[2], out questId))
            {
                this.CommandHelp(character);
                return;
            }

            switch (verb)
            {
                case "accept":
                    if (!QuestManager.Accept(character, questId))
                    {
                        Tell(character, "Could not start quest " + questId + ".");
                    }

                    break;

                case "handin":
                    if (!QuestManager.HandIn(character, questId))
                    {
                        Tell(character, "Quest " + questId + " is not finished, or is already handed in.");
                    }

                    break;

                case "abandon":
                    if (!QuestManager.Abandon(character, questId))
                    {
                        Tell(character, "You are not on quest " + questId + ".");
                    }

                    break;

                default:
                    this.CommandHelp(character);
                    break;
            }
        }

        /// <summary>
        /// Anyone may use this.
        /// </summary>
        /// <remarks>
        /// It stands in for an NPC handing a quest over, which is something an
        /// ordinary player does, so gating it behind a GM level would make
        /// quests untestable by the people who would play them. It goes away
        /// once a conversation can start a quest.
        /// </remarks>
        public override int GMLevelNeeded()
        {
            return 0;
        }

        public override List<string> ListCommands()
        {
            return new List<string> { "quest" };
        }

        private static void Tell(ICharacter character, string text)
        {
            character.Playfield.Publish(ChatTextMessageHandler.Default.CreateIM(character, text));
        }
    }
}
