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
    /// Reports a landed hit to everyone who can see it.
    /// </summary>
    /// <remarks>The field roles are proven by the extracted-client combat dispatcher.</remarks>
    [MessageHandler(MessageHandlerDirection.OutboundOnly)]
    public class AttackInfoMessageHandler : BaseMessageHandler<AttackInfoMessage, AttackInfoMessageHandler>
    {
        #region Outbound

        /// <summary>
        /// Announces that <paramref name="attacker"/> hit <paramref name="target"/>.
        /// </summary>
        /// <param name="attacker">
        /// </param>
        /// <param name="target">
        /// </param>
        /// <param name="damage">
        /// Damage dealt, as the client should display it.
        /// </param>
        public void Send(ICharacter attacker, Identity target, int damage, int weaponSlot)
        {
            this.SendToPlayfield(attacker, this.Filler(attacker, target, damage, weaponSlot));
        }

        /// <summary>
        /// </summary>
        private MessageDataFiller Filler(ICharacter attacker, Identity target, int damage, int weaponSlot)
        {
            return x =>
            {
                x.Identity = attacker.Identity;
                x.Damage = damage;
                x.WeaponEnergy = -1;
                x.WeaponSlot = weaponSlot;
                x.Target = target;
                x.VisualEffectId = 0;
                x.DamageType = 3;
                x.WeaponInstance = 0;
            };
        }

        #endregion
    }
}
