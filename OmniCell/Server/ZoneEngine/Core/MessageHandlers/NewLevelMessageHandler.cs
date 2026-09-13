#region License

// Copyright (c) 2005-2014, CellAO Team
// Copyright (c) 2026, OmniCell contributors
//
// This file is part of OmniCell and is distributed under the terms the project
// is licensed under. See the repository root for details.

#endregion

namespace ZoneEngine.Core.MessageHandlers
{
    #region Usings ...

    using OmniCell.Core.Components;
    using OmniCell.Core.Entities;
    using OmniCell.Enums;

    using SmokeLounge.AOtomation.Messaging.Messages.N3Messages;

    #endregion

    /// <summary>
    /// Tells a character it has gained a level.
    /// </summary>
    /// <remarks>
    /// All eight fields are named as of 2026-09-12, and this remark used to say
    /// two of them could not be. Seven captures could not separate them, which
    /// was true and was the wrong place to look: the dispatcher writes seven of
    /// the eight into named character stats, and reading it settles them at
    /// once. See NewLevelMessage for the offsets and stat ids.
    ///
    /// Two consequences for what this handler sends. Improvement points went
    /// out as zero, and the client writes the field straight into stat 53 - so
    /// levelling up set the player's IP to nothing. It now sends the
    /// character's actual balance, and the title level likewise. The kill range
    /// stays at the 4 every capture carries, since no stat of ours holds it.
    ///
    /// The experience award is still zero and that is now a known gap rather
    /// than an unknown field: it is the experience the triggering event gave,
    /// the client shows it in Feedback_NewLevel, and Leveling.Tick runs on a
    /// heartbeat and does not know the delta. Threading it through is a change
    /// to the caller, not to this file.
    /// </remarks>
    [MessageHandler(MessageHandlerDirection.OutboundOnly)]
    public class NewLevelMessageHandler : BaseMessageHandler<NewLevelMessage, NewLevelMessageHandler>
    {
        #region Outbound

        /// <summary>
        /// </summary>
        public void Send(ICharacter character, int level, int experience, int thisLevel, int nextLevel)
        {
            this.Send(character, Filler(character, level, experience, thisLevel, nextLevel), false);
        }

        private static MessageDataFiller Filler(
            ICharacter character,
            int level,
            int experience,
            int thisLevel,
            int nextLevel)
        {
            return message =>
            {
                message.Identity = character.Identity;
                message.Unknown = 0;
                message.Level = level;
                message.ImprovementPoints = character.Stats[StatIds.ip].Value;
                message.Experience = experience;
                message.ExperienceThisLevel = thisLevel;
                message.ExperienceNextLevel = nextLevel;
                message.TitleLevel = character.Stats[StatIds.titlelevel].Value;
                message.ExperienceKillRange = 4;
                message.ExperienceAward = 0;
            };
        }

        #endregion
    }
}
