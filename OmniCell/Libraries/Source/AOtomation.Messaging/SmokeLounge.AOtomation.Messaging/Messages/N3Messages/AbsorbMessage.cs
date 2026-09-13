// --------------------------------------------------------------------------------------------------------------------
// <copyright file="AbsorbMessage.cs" company="OmniCell">
//   Copyright © 2026 OmniCell contributors.
//   Added to SmokeLounge.AOtomation.Messaging, which is distributed under the
//   Do What The Fuck You Want To Public License, Version 2, as published by
//   Sam Hocevar. See http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the AbsorbMessage type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace SmokeLounge.AOtomation.Messaging.Messages.N3Messages
{
    using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

    [AoContract((int)N3MessageType.Absorb)]
    public class AbsorbMessage : N3Message
    {
        public AbsorbMessage()
        {
            this.N3MessageType = N3MessageType.Absorb;
        }

        [AoMember(0)]
        public int DamageAbsorbed { get; set; }

        [AoMember(1)]
        public int DamageTypeStat { get; set; }
    }
}
