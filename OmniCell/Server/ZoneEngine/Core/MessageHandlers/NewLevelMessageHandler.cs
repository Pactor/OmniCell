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
    /// is the one XML Data\Experience.xml gives the new level (4 at levels 3-7,
    /// 5 at 12, 6 at 16 and 18 in the captures).
    ///
    /// The experience award is the experience the triggering event gave; the
    /// client shows it in Feedback_NewLevel. A kill passes it; the heartbeat
    /// check, which does not know what changed, passes zero.
    /// </remarks>
    [MessageHandler(MessageHandlerDirection.OutboundOnly)]
    public class NewLevelMessageHandler : BaseMessageHandler<NewLevelMessage, NewLevelMessageHandler>
    {
        #region Outbound

        /// <summary>
        /// </summary>
        public void Send(
            ICharacter character,
            int level,
            int experience,
            int thisLevel,
            int nextLevel,
            int experienceAward = 0)
        {
            this.Send(character, Filler(character, level, experience, thisLevel, nextLevel, experienceAward), false);
        }

        private static MessageDataFiller Filler(
            ICharacter character,
            int level,
            int experience,
            int thisLevel,
            int nextLevel,
            int experienceAward)
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
                message.ExperienceKillRange = Experience.KillRange(level);
                message.ExperienceAward = experienceAward;
            };
        }

        #endregion
    }
}
