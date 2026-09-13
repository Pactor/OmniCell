// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ReflectAttackMessage.cs" company="OmniCell">
//   Copyright © 2026 OmniCell contributors.
//   Added to SmokeLounge.AOtomation.Messaging, which is distributed under the
//   Do What The Fuck You Want To Public License, Version 2, as published by
//   Sam Hocevar. See http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the ReflectAttackMessage type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace SmokeLounge.AOtomation.Messaging.Messages.N3Messages
{
    using SmokeLounge.AOtomation.Messaging.GameData;
    using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

    [AoContract((int)N3MessageType.ReflectAttack)]
    public class ReflectAttackMessage : N3Message
    {
        public ReflectAttackMessage()
        {
            this.N3MessageType = N3MessageType.ReflectAttack;
        }

        [AoMember(0)]
        public int Damage { get; set; }

        [AoMember(1)]
        public Identity Reflector { get; set; }

        [AoMember(2)]
        public int VisualEffectId { get; set; }
    }
}
