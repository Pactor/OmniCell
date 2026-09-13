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
    /// Reports the state of a door.
    /// </summary>
    /// <remarks>
    /// 77 in the captures, in bursts on entering a playfield full of doors - 28
    /// of them on walking into the subway. Every captured one is the same shape:
    /// Unknown1 is 2, everything else zero, and the identity is the door.
    ///
    /// What a value other than 2 means is not known, because no capture holds
    /// one, so this sends what was seen and nothing more. It goes out for the
    /// doors a playfield holds, which is where the live server sends them.
    /// </remarks>
    [MessageHandler(MessageHandlerDirection.OutboundOnly)]
    public class DoorStatusUpdateMessageHandler :
        BaseMessageHandler<DoorStatusUpdateMessage, DoorStatusUpdateMessageHandler>
    {
        #region Outbound

        /// <summary>
        /// </summary>
        public void Send(ICharacter to, Identity door)
        {
            this.Send(to, Filler(door), false);
        }

        /// <summary>
        /// The version the client checks this message against, and the only
        /// value it will accept.
        /// </summary>
        private const int Version = 2;

        private static MessageDataFiller Filler(Identity door)
        {
            return message =>
            {
                // A shut, unlocked, keyless door, which is what every captured
                // copy carries and what the doors we place currently are.
                message.Identity = door;
                message.Unknown = 0;
                message.Version = Version;
                message.Locked = 0;
                message.Open = 0;
                message.AccessKey = 0;
                message.Unknown5 = 0;
                message.Unknown6 = new Identity[0];
            };
        }
        #endregion
    }
}
