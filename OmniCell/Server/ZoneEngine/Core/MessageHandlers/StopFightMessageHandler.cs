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
    using OmniCell.Core.Network;

    using SmokeLounge.AOtomation.Messaging.Messages.N3Messages;

    using ZoneEngine.Core.Combat;

    #endregion

    /// <summary>
    /// Ends a fight, in either direction.
    /// </summary>
    /// <remarks>
    /// Inbound the client sends this when the player breaks off. Outbound the
    /// server sends it when the fight ends for a reason the client did not
    /// choose - the target died, or left.
    /// </remarks>
    [MessageHandler(MessageHandlerDirection.All)]
    public class StopFightMessageHandler : BaseMessageHandler<StopFightMessage, StopFightMessageHandler>
    {
        #region Inbound

        /// <summary>
        /// </summary>
        protected override void Read(StopFightMessage message, IZoneClient client)
        {
            if (client == null || client.Controller == null || client.Controller.Character == null)
            {
                return;
            }

            Combat.Stop(client.Controller.Character);
        }

        #endregion

        #region Outbound

        /// <summary>
        /// Tells <paramref name="character"/> that their fight has ended.
        /// </summary>
        public void Send(ICharacter character)
        {
            this.Send(character, this.Filler(character), false);
        }

        /// <summary>
        /// </summary>
        private MessageDataFiller Filler(ICharacter character)
        {
            return x =>
            {
                x.Identity = character.Identity;
                // Every captured copy carries 1. The client ignores it either
                // way - see StopFightMessage - but there is no reason to differ.
                x.StopFighting = 1;
            };
        }

        #endregion
    }
}
