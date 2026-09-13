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
    using OmniCell.Core.Network;

    using SmokeLounge.AOtomation.Messaging.GameData;
    using SmokeLounge.AOtomation.Messaging.Messages;
    using SmokeLounge.AOtomation.Messaging.Messages.N3Messages;

    using ZoneEngine.Core.Combat;

    #endregion

    /// <summary>
    /// A special attack - burst, fling, aimed shot, brawl.
    /// </summary>
    /// <remarks>
    /// This one goes both ways. The client sends it to ask for a special against
    /// a target, and the server sends it back to say the special is happening,
    /// which is why the captures hold four going up and eight coming down.
    ///
    /// SpecialAttackSkill is the skill the special uses - 151 in the captured samples,
    /// which is aimedshot. That is what makes it a special rather than a swing:
    /// the same message carries burst, fling and the rest by changing that
    /// number.
    ///
    /// SpecialAttackInfo follows with what it did, the way AttackInfo follows a
    /// swing.
    /// </remarks>
    [MessageHandler(MessageHandlerDirection.All)]
    public class CharSecSpecAttackMessageHandler :
        BaseMessageHandler<CharSecSpecAttackMessage, CharSecSpecAttackMessageHandler>
    {
        #region Inbound

        /// <summary>
        /// The client is asking to use a special.
        /// </summary>
        protected override void Read(CharSecSpecAttackMessage message, IZoneClient client)
        {
            if (client == null || client.Controller == null || client.Controller.Character == null)
            {
                return;
            }

            Combat.UseSpecial(client.Controller.Character, message.Target, message.SpecialAttackSkill);
        }

        #endregion

        #region Outbound

        /// <summary>
        /// Tells the playfield a special is being used.
        /// </summary>
        public void Send(ICharacter attacker, Identity target, int skill)
        {
            this.Send(attacker, Filler(attacker, target, skill), true);
        }

        private static MessageDataFiller Filler(ICharacter attacker, Identity target, int skill)
        {
            return message =>
            {
                message.Identity = attacker.Identity;
                message.Unknown = 0;
                message.Target = target;
                message.SpecialAttackSkill = skill;
            };
        }

        #endregion
    }
}
