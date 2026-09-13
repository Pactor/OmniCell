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
    /// Which way a character wants to go.
    /// </summary>
    /// <remarks>
    /// A unit vector, sent when something that moves under the server's control
    /// changes direction. Every captured sample is an NPC and every one is
    /// roughly unit length with Y at zero, which is what a heading across flat
    /// ground looks like.
    ///
    /// Without it a patrolling mob turns instantly on the client instead of
    /// turning as it walks.
    /// </remarks>
    [MessageHandler(MessageHandlerDirection.OutboundOnly)]
    public class SetWantedDirectionMessageHandler :
        BaseMessageHandler<SetWantedDirectionMessage, SetWantedDirectionMessageHandler>
    {
        #region Outbound

        /// <summary>
        /// </summary>
        public void Send(ICharacter character, Vector3 direction)
        {
            this.Send(character, Filler(character, direction), true);
        }

        private static MessageDataFiller Filler(ICharacter character, Vector3 direction)
        {
            return message =>
            {
                message.Identity = character.Identity;
                message.Unknown = 0;
                message.DirectinVector = direction;
            };
        }

        #endregion
    }
}
