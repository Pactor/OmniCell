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

    using System.Collections.Generic;
    using System.Linq;

    using OmniCell.Core.Components;
    using OmniCell.Core.Entities;
    using OmniCell.Enums;

    using SmokeLounge.AOtomation.Messaging.GameData;
    using SmokeLounge.AOtomation.Messaging.Messages.N3Messages;

    #endregion

    /// <summary>
    /// The organisation a character belongs to.
    /// </summary>
    /// <remarks>
    /// One in the captures, for another player standing nearby, carrying nothing
    /// but their org's name. Sent for a character that has one; a character with
    /// no organisation gets nothing, which is what the captures show for the
    /// player being followed.
    /// </remarks>
    [MessageHandler(MessageHandlerDirection.OutboundOnly)]
    public class OrgInfoPacketMessageHandler :
        BaseMessageHandler<OrgInfoPacketMessage, OrgInfoPacketMessageHandler>
    {
        #region Outbound

        /// <summary>
        /// </summary>
        public void Send(ICharacter character)
        {
            if (string.IsNullOrEmpty(character.OrganizationName))
            {
                return;
            }

            this.Send(character, Filler(character), true);
        }

        private static MessageDataFiller Filler(ICharacter character)
        {
            return message =>
            {
                message.Identity = character.Identity;
                message.Unknown = 0;
                message.Name = character.OrganizationName;
            };
        }

        #endregion
    }
}
