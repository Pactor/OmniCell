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
    /// Tells the playfield a character has stopped where it is.
    /// </summary>
    /// <remarks>
    /// Sent when something that was moving stops - a patrolling mob reaching the
    /// end of its route, or a character told to stand still. Without it the
    /// client keeps animating a walk that the server has already ended.
    ///
    /// Unknown1 is 1 and Unknown3 is 1 in the captured samples; Unknown2 varies
    /// and is passed through from the caller, since 190 in one sample is not
    /// enough to name it.
    /// </remarks>
    [MessageHandler(MessageHandlerDirection.OutboundOnly)]
    public class StopMovingCmdMessageHandler :
        BaseMessageHandler<StopMovingCmdMessage, StopMovingCmdMessageHandler>
    {
        #region Outbound

        /// <summary>
        /// </summary>
        public void Send(ICharacter character, int unknown2 = 0)
        {
            this.Send(character, Filler(character, unknown2), true);
        }

        private static MessageDataFiller Filler(ICharacter character, int unknown2)
        {
            return message =>
            {
                message.Identity = character.Identity;
                message.Unknown = 0;
                message.Unknown1 = 1;
                message.Unknown2 = unknown2;
                message.Unknown3 = 1;
            };
        }
        #endregion
    }
}
