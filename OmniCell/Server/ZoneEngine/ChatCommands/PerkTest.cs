#region License

// Copyright (c) 2005-2014, CellAO Team
// Copyright (c) 2026, OmniCell contributors
//
// This file is part of OmniCell and is distributed under the terms the project
// is licensed under. See the repository root for details.

#endregion

namespace ZoneEngine.ChatCommands
{
    #region Usings ...

    using System;
    using System.Collections.Generic;

    using OmniCell.Core.Entities;

    using SmokeLounge.AOtomation.Messaging.GameData;
    using SmokeLounge.AOtomation.Messaging.Messages;
    using SmokeLounge.AOtomation.Messaging.Messages.N3Messages;

    using ZoneEngine.Core.Packets;
    using ZoneEngine.Core.MessageHandlers;

    #endregion

    /// <summary>
    /// Diagnostic command for working out the PerkUpdateMessage wire format.
    ///
    /// PerkUpdateMessage carries a personal research project id, the selected
    /// PersonalResearchGoal stat value, and the remaining research XP. A packet
    /// capture cannot settle it: the client never sends a perk message, so the traffic
    /// only ever flows server to client. That makes the server the only possible sender
    /// and the client the oracle - send values, watch what the perk window does.
    ///
    /// Useful candidates, from the decoded perk record format (see
    /// Documentation/DataAudit.md):
    ///   the perk's item id      e.g. 210830 is Accumulator level 1
    ///   the perk record id      e.g. 100 is the head of the Accumulator chain
    ///   a level                 1 to 10
    ///
    /// This is a development aid and is gated behind GM level. It should not survive
    /// into anything player facing.
    /// </summary>
    public class PerkTest : AOChatCommand
    {
        #region Public Methods and Operators

        /// <summary>
        /// </summary>
        /// <param name="args">
        /// </param>
        /// <returns>
        /// </returns>
        public override bool CheckCommandArguments(string[] args)
        {
            // args[0] is the command name itself
            if (args.Length != 4)
            {
                return false;
            }

            int parsed;
            for (int i = 1; i < 4; i++)
            {
                if (!int.TryParse(args[i], out parsed))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// </summary>
        /// <param name="character">
        /// </param>
        public override void CommandHelp(ICharacter character)
        {
            this.Reply(character, "Syntax: .perktest <int1> <int2> <int3>");
            this.Reply(character, "Sends a PerkUpdateMessage with those three values to your own client.");
            this.Reply(character, "First is a research id, second goes to stat 265, third is XP still owed.");
            this.Reply(character, "Keep the research window open - the client sets got_tech, not got_perk.");
        }

        /// <summary>
        /// </summary>
        /// <param name="character">
        /// </param>
        /// <param name="target">
        /// </param>
        /// <param name="args">
        /// </param>
        public override void ExecuteCommand(ICharacter character, Identity target, string[] args)
        {
            int first = int.Parse(args[1]);
            int second = int.Parse(args[2]);
            int third = int.Parse(args[3]);

            var perkUpdate = new PerkUpdateMessage
                             {
                                 ResearchId = first,
                                 PersonalResearchGoal = second,
                                 ResearchXpRemaining = third
                             };

            try
            {
                character.Playfield.Publish(
                    Bulk.CreateIM(character.Controller.Client, new MessageBody[] { perkUpdate }));

                this.Reply(
                    character,
                    string.Format("Sent PerkUpdateMessage({0}, {1}, {2})", first, second, third));
            }
            catch (Exception exception)
            {
                // Report rather than let the bus swallow it, so a bad guess is visible
                // in game instead of looking like the command did nothing.
                this.Reply(character, "PerkUpdateMessage failed: " + exception.Message);
                throw;
            }
        }

        /// <summary>
        /// </summary>
        /// <returns>
        /// </returns>
        public override int GMLevelNeeded()
        {
            return 1;
        }

        /// <summary>
        /// </summary>
        /// <returns>
        /// </returns>
        public override List<string> ListCommands()
        {
            return new List<string> { "perktest" };
        }

        #endregion

        #region Methods

        /// <summary>
        /// </summary>
        /// <param name="character">
        /// </param>
        /// <param name="text">
        /// </param>
        private void Reply(ICharacter character, string text)
        {
            character.Playfield.Publish(ChatTextMessageHandler.Default.CreateIM(character, text));
        }

        #endregion
    }
}
