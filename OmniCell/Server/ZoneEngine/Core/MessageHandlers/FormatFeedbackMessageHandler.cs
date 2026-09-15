#region License

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

    using SmokeLounge.AOtomation.Messaging.Messages.N3Messages;

    #endregion

    /// <summary>
    /// A client-side formatted feedback line: a quest's kill counter and reward, a fixture's
    /// "You extinguish the Gas Fire."
    /// </summary>
    [MessageHandler(MessageHandlerDirection.OutboundOnly)]
    public class FormatFeedbackMessageHandler : BaseMessageHandler<FormatFeedbackMessage, FormatFeedbackMessageHandler>
    {
        #region Outbound

        public void Send(ICharacter character, string formattedMessage)
        {
            this.Send(character, Filler(character, formattedMessage));
        }

        private static MessageDataFiller Filler(ICharacter character, string formattedMessage)
        {
            // As the live server sends them to the player they are for (20260914-124401 #2122, #3072).
            return x =>
            {
                x.Identity = character.Identity;
                x.Unknown = 1;
                x.ChatCategory = 0;
                x.FormattedMessage = formattedMessage;
                x.PayloadKind = 0;
            };
        }

        #endregion
    }
}
