#region License

// Copyright (c) 2026, OmniCell contributors
//
// This file is part of OmniCell and is distributed under the terms the project
// is licensed under. See the repository root for details.

#endregion

namespace ZoneEngine.Core.MessageHandlers
{
    using System.Linq;

    using OmniCell.Core.Components;
    using OmniCell.Core.Entities;
    using OmniCell.Enums;
    using OmniCell.Stats;

    using SmokeLounge.AOtomation.Messaging.GameData;
    using SmokeLounge.AOtomation.Messaging.Messages.N3Messages;

    using ZoneEngine.Core.Combat;
    using ZoneEngine.Core.Controllers;

    /// <summary>
    /// Supplies the client weapon holder and current combat initiatives.
    /// </summary>
    [MessageHandler(MessageHandlerDirection.OutboundOnly)]
    public class SpecialAttackWeaponMessageHandler :
        BaseMessageHandler<SpecialAttackWeaponMessage, SpecialAttackWeaponMessageHandler>
    {
        public void SendLogin(ICharacter character)
        {
            this.Send(character, Filler(character, 1), false);
        }

        public void AnnounceCombatStart(ICharacter character)
        {
            this.Send(character, Filler(character, 0), true);
        }

        private static MessageDataFiller Filler(ICharacter character, int unknown)
        {
            return message =>
            {
                message.Identity = character.Identity;
                message.Unknown = (byte)unknown;
                message.Specials = character.Controller is NPCController
                                       ? new SpecialAttack[0]
                                       : CombatWeaponProfiles.PlayerInnate.ToArray();
                message.CloseCombatInitiative =
                    StatValue.OrZero(character.Stats[StatIds.closecombatinitiative].Value);
                message.DistanceWeaponInitiative =
                    StatValue.OrZero(character.Stats[StatIds.distanceweaponinitiative].Value);
                message.PhysicalProwessInitiative =
                    StatValue.OrZero(character.Stats[StatIds.physicalprowessinitiative].Value);
                message.NanoProwessInitiative =
                    StatValue.OrZero(character.Stats[StatIds.nanoprowessinitiative].Value);
                message.AggDef = StatValue.OrZero(character.Stats[StatIds.aggdef].Value);
            };
        }
    }
}
