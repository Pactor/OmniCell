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

    using SmokeLounge.AOtomation.Messaging.GameData;
    using SmokeLounge.AOtomation.Messaging.Messages.N3Messages;

    #endregion

    /// <summary>
    /// Reports health changing by something other than a weapon hit.
    /// </summary>
    /// <remarks>
    /// A weapon swing is reported by AttackInfo. This is the other kind: a heal,
    /// a nano's damage, anything that moves health without a hit behind it.
    /// There are only 29 in the captures against 434 AttackInfos, which is what
    /// you would expect from a character that healed itself a few times.
    ///
    /// The two fields that matter read straight off those 29. Unknown1 tracks
    /// the character's health after the change - one sequence runs 2060, 2310,
    /// 2565, 2815, 3065 - and Unknown2 is the change itself, 250 in that same
    /// sequence, negative where health went down.
    ///
    /// Unknown3 is 0 on every heal and 92 or 95 on the two decreases, and two
    /// samples are not enough to say what it is. It is sent as zero, which is
    /// what a heal sends, rather than guessed at.
    /// </remarks>
    [MessageHandler(MessageHandlerDirection.OutboundOnly)]
    public class HealthDamageMessageHandler :
        BaseMessageHandler<HealthDamageMessage, HealthDamageMessageHandler>
    {
        #region Outbound

        /// <summary>
        /// Health changed on this character by <paramref name="delta"/>.
        /// </summary>
        public void Send(ICharacter character, int newHealth, int delta)
        {
            this.Send(character, Filler(character, newHealth, delta), true);
        }

        private static MessageDataFiller Filler(ICharacter character, int newHealth, int delta)
        {
            return message =>
            {
                message.Identity = character.Identity;
                message.Unknown = 0;
                message.Health = newHealth;
                message.Delta = delta;
                message.DamageType = DamageType.None;
                message.DeathCause = DeathCause.None;
                message.Source = character.Identity;
                message.SourceItem = 0;
            };
        }
        #endregion
    }
}
