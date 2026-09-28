#region License

// Copyright (c) 2026, OmniCell contributors
//
// This file is part of OmniCell and is distributed under the terms the project
// is licensed under. See the repository root for details.

#endregion

namespace ZoneEngine.ChatCommands
{
    #region Usings ...

    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;

    using OmniCell.Core.Entities;
    using OmniCell.Core.Items;
    using OmniCell.Interfaces;

    using SmokeLounge.AOtomation.Messaging.GameData;

    using ZoneEngine.Core.Combat;
    using ZoneEngine.Core.MessageHandlers;

    #endregion

    /// <summary>
    /// Trains a perk, so there is something to press.
    /// </summary>
    /// <remarks>
    /// Nothing else gives a perk yet - there is no perk window, no training
    /// and no reset - so without this the perk code has nothing to run. A
    /// character has to own a perk before the client will put it on a hotbar,
    /// because owning one is what <c>FullCharacter.ResearchGoals</c> says and
    /// that is the only place the client learns it from.
    ///
    /// The id is the perk's **short** id: 190 Lay On Hands, 193 Devotional
    /// Armor, 773 Blade Whirlwind, 775 Honoring the Ancients, 778 Seppuku
    /// Slash. It takes effect on the next zone-in, when FullCharacter is sent
    /// again.
    ///
    ///     .perk 778            train Seppuku Slash
    ///     .perk list           the perks you have
    /// </remarks>
    public class Perk : AOChatCommand
    {
        #region Public Methods and Operators

        public override bool CheckCommandArguments(string[] args)
        {
            if (args.Length != 2)
            {
                return false;
            }

            int parsed;
            return args[1] == "list"
                   || int.TryParse(args[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out parsed);
        }

        public override void CommandHelp(ICharacter character)
        {
            this.Reply(character, "Syntax: .perk <short id> | .perk list");
            this.Reply(character, "Trains a perk on yourself. Takes effect on the next zone-in.");
            this.Reply(character, "Short ids: 190 Lay On Hands, 193 Devotional Armor, 773 Blade Whirlwind,");
            this.Reply(character, "  775 Honoring the Ancients, 778 Seppuku Slash.");
        }

        public override void ExecuteCommand(ICharacter character, Identity target, string[] args)
        {
            var self = character as Character;
            if (self == null)
            {
                return;
            }

            if (args[1] == "list")
            {
                if (self.Perks.Count == 0)
                {
                    this.Reply(character, "You have no perks.");
                    return;
                }

                foreach (int perk in self.Perks.OrderBy(p => p))
                {
                    this.Reply(character, perk + "  " + Name(perk));
                }

                return;
            }

            int wanted = int.Parse(args[1], CultureInfo.InvariantCulture);
            if (!Perks.Actions.ContainsKey(wanted))
            {
                this.Reply(character, "No perk action item carries perk " + wanted + ".");
                return;
            }

            if (self.HasPerk(wanted))
            {
                this.Reply(character, "You already have " + Name(wanted) + ".");
                return;
            }

            self.GivePerk(wanted);
            this.Reply(character, "Trained " + wanted + " " + Name(wanted) + ". Zone for it to appear.");
        }

        public override int GMLevelNeeded()
        {
            return 1;
        }

        public override List<string> ListCommands()
        {
            return new List<string> { "perk" };
        }

        #endregion

        private void Reply(ICharacter character, string text)
        {
            character.Playfield.Publish(ChatTextMessageHandler.Default.CreateIM(character, text));
        }

        /// <summary>
        /// What the perk's action item is called.
        /// </summary>
        private static string Name(int perk)
        {
            ItemTemplate item;
            if (!Perks.Actions.TryGetValue(perk, out item))
            {
                return "(no action item)";
            }

            // The server has no item-name table - names live in itemnames.sql,
            // which only the capture tools read - so the item id is what there
            // is to show.
            return "item " + item.ID;
        }
    }
}
