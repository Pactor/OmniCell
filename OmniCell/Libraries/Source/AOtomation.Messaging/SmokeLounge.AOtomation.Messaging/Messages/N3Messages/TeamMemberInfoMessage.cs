// --------------------------------------------------------------------------------------------------------------------
// <copyright file="TeamMemberInfoMessage.cs" company="SmokeLounge">
//   Copyright © 2013 SmokeLounge.
//   This program is free software. It comes without any warranty, to
//   the extent permitted by applicable law. You can redistribute it
//   and/or modify it under the terms of the Do What The Fuck You Want
//   To Public License, Version 2, as published by Sam Hocevar. See
//   http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the TeamMemberInfoMessage type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace SmokeLounge.AOtomation.Messaging.Messages.N3Messages
{
    using SmokeLounge.AOtomation.Messaging.GameData;
    using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

    /// <summary>
    /// A team member's health and nano, for the bars in the team window.
    /// </summary>
    /// <remarks>
    /// This model had nine fields where the client's reader at 0x1007A745 has
    /// five: an identity and four int32s, 24 bytes and no more. It opened with
    /// a byte and an int16 and closed with an int16, all three invented, and
    /// nothing had ever contradicted them because nothing had ever captured
    /// this message - it only appears when a second character is in the team.
    ///
    /// The four numbers are four stats, and the dispatcher at 0x1007A7DD says
    /// which: it resolves the identity, refuses if the character is flagged at
    /// +0x140, and then calls the character's stat setter four times with 221,
    /// 214, 1 and 27 - maxnanoenergy, currentnano, life and health. It sets
    /// each maximum before the current value that goes with it, which is why
    /// the order it reads them in and the order it applies them in differ.
    /// Then it refreshes the team window twice, for stats 1 and 221.
    /// </remarks>
    [AoContract((int)N3MessageType.TeamMemberInfo)]
    public class TeamMemberInfoMessage : N3Message
    {
        #region Constructors and Destructors

        public TeamMemberInfoMessage()
        {
            this.N3MessageType = N3MessageType.TeamMemberInfo;
        }

        #endregion

        #region AoMember Properties

        /// <summary>
        /// The member these numbers belong to.
        /// </summary>
        [AoMember(0)]
        public Identity Character { get; set; }

        /// <summary>
        /// Stat 214, currentnano.
        /// </summary>
        [AoMember(1)]
        public int CurrentNano { get; set; }

        /// <summary>
        /// Stat 221, maxnanoenergy.
        /// </summary>
        [AoMember(2)]
        public int MaxNano { get; set; }

        /// <summary>
        /// Stat 1, life - the health maximum.
        /// </summary>
        [AoMember(3)]
        public int MaxHealth { get; set; }

        /// <summary>
        /// Stat 27, health.
        /// </summary>
        [AoMember(4)]
        public int CurrentHealth { get; set; }

        #endregion
    }
}