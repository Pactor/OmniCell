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
    /// What a special attack did.
    /// </summary>
    /// <remarks>The field roles are proven by the extracted-client combat dispatcher.</remarks>
    [MessageHandler(MessageHandlerDirection.OutboundOnly)]
    public class SpecialAttackInfoMessageHandler :
        BaseMessageHandler<SpecialAttackInfo, SpecialAttackInfoMessageHandler>
    {
        #region Constants
        /// <summary>
        /// Minus one in the captured samples.
        /// </summary>
        private const int NoEnergyUpdate = -1;

        #endregion

        #region Outbound

        /// <summary>
        /// </summary>
        public void Send(ICharacter attacker, Identity target, int damage, int skill, int weaponSlot)
        {
            this.Send(attacker, Filler(attacker, target, damage, skill, weaponSlot), true);
        }

        private static MessageDataFiller Filler(
            ICharacter attacker,
            Identity target,
            int damage,
            int skill,
            int weaponSlot)
        {
            return message =>
            {
                message.Identity = attacker.Identity;
                message.Unknown = 0;
                message.WeaponSlot = weaponSlot;
                message.Damage = damage;
                message.WeaponEnergy = NoEnergyUpdate;
                message.Target = target;
                message.SpecialAttackSkill = skill;
                message.VisualEffectId = 0;
            };
        }

        #endregion
    }
}
