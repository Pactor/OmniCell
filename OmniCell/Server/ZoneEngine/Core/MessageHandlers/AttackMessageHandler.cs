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

    using SmokeLounge.AOtomation.Messaging.GameData;
    using SmokeLounge.AOtomation.Messaging.Messages.N3Messages;

    using ZoneEngine.Core.Combat;

    #endregion

    /// <summary>
    /// Handles a player opening a fight.
    /// </summary>
    /// <remarks>
    /// The client sends this once, when the attack starts, and the server sends
    /// it back to the playfield before the first AttackInfo. That outbound
    /// notification is what puts clients into the visible fighting state. The
    /// client does not send anything per swing, so everything after this point
    /// is the server's to drive. See <see cref="Combat"/>.
    ///
    /// Captured from a live session as Identity(CanbeAffected, target) plus a
    /// single action byte, which matches the definition AOtomation already had.
    /// </remarks>
    [MessageHandler(MessageHandlerDirection.All)]
    public class AttackMessageHandler : BaseMessageHandler<AttackMessage, AttackMessageHandler>
    {
        #region Inbound

        /// <summary>
        /// </summary>
        /// <param name="message">
        /// </param>
        /// <param name="client">
        /// </param>
        protected override void Read(AttackMessage message, IZoneClient client)
        {
            if (client == null || client.Controller == null || client.Controller.Character == null)
            {
                return;
            }

            // A target that cannot be attacked is not worth a reply. The live
            // client does not send this for such targets in the first place, so
            // reaching here means either a race with the target's death or a
            // client that is not behaving.
            Combat.Start(client.Controller.Character, message.Target);
        }

        #endregion

        #region Outbound

        /// <summary>
        /// Tells the playfield that <paramref name="attacker"/> has started
        /// attacking <paramref name="target"/>.
        /// </summary>
        public void Send(ICharacter attacker, Identity target)
        {
            this.Send(attacker, Filler(attacker, target), true);
        }

        private static MessageDataFiller Filler(ICharacter attacker, Identity target)
        {
            return message =>
            {
                message.Identity = attacker.Identity;
                message.Unknown = 0;
                message.Target = target;
                message.Action = 0;
            };
        }

        #endregion
    }
}
