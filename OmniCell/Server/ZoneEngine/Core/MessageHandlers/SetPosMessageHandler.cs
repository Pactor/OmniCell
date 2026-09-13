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
    /// Puts a character where the server says it is.
    /// </summary>
    /// <remarks>
    /// The client moves itself and tells the server afterwards. This is the
    /// server disagreeing - the one message that overrides where the client
    /// thinks it stands, used after a correction rather than as part of ordinary
    /// movement, which is why there are only 59 of them in the captures against
    /// 7806 CharDCMoves.
    ///
    /// All three fields past the coordinates are named as of 2026-09-12 and
    /// what this handler sends is unchanged, because what it sent already
    /// matched every capture: update the last allowed position, no crowd
    /// figure, do not stop the character. See SetPosMessage for what each one
    /// makes the client do.
    /// </remarks>
    [MessageHandler(MessageHandlerDirection.OutboundOnly)]
    public class SetPosMessageHandler :
        BaseMessageHandler<SetPosMessage, SetPosMessageHandler>
    {
        #region Outbound

        /// <summary>
        /// </summary>
        public void Send(ICharacter character, Vector3 coordinates)
        {
            this.Send(character, Filler(character, coordinates), true);
        }

        private static MessageDataFiller Filler(ICharacter character, Vector3 coordinates)
        {
            return message =>
            {
                message.Identity = character.Identity;
                message.Unknown = 0;
                message.Coordinates = coordinates;
                message.UpdateLastAllowedPosition = 1;
                message.CrowdLimitingFeedback = 0;
                message.StopMoving = 0;
            };
        }
        #endregion
    }
}
