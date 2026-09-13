#region License

// Copyright (c) 2005-2014, CellAO Team
// Copyright (c) 2026, OmniCell contributors
//
// This file is part of OmniCell and is distributed under the terms the project
// is licensed under. See the repository root for details.

#endregion

namespace ZoneEngine.Core.MessageHandlers
{
    #region Usings

    using OmniCell.Core.Components;
    using OmniCell.Core.Entities;

    using SmokeLounge.AOtomation.Messaging.GameData;
    using SmokeLounge.AOtomation.Messaging.Messages.N3Messages;

    #endregion

    /// <summary>
    /// Reports a swing that missed.
    /// </summary>
    /// <remarks>
    /// The layout is proven by the extracted client reader/writer and dispatcher.
    /// WeaponEnergy updates CharacterStat.Energy on the selected attacker weapon;
    /// -1 is the captured sentinel for attacks without an ammunition update.
    /// </remarks>
    [MessageHandler(MessageHandlerDirection.OutboundOnly)]
    public class MissedAttackInfoMessageHandler :
        BaseMessageHandler<MissedAttackInfoMessage, MissedAttackInfoMessageHandler>
    {
        #region Outbound

        /// <summary>
        /// Announces that <paramref name="attacker"/> missed <paramref name="target"/>.
        /// </summary>
        public void Send(ICharacter attacker, Identity target, int weaponSlot = 0, int attackSkillStat = 0)
        {
            this.SendToPlayfield(attacker, this.Filler(attacker, target, weaponSlot, attackSkillStat));
        }

        /// <summary>
        /// </summary>
        private MessageDataFiller Filler(
            ICharacter attacker,
            Identity target,
            int weaponSlot,
            int attackSkillStat)
        {
            return x =>
            {
                x.Identity = attacker.Identity;
                x.WeaponEnergy = -1;
                x.WeaponSlot = weaponSlot;
                x.Attacker = attacker.Identity;
                x.Target = target;
                x.AttackSkillStat = attackSkillStat;
            };
        }

        #endregion
    }
}
