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
    /// Tells the playfield that a nano is running on a character, or has stopped.
    /// </summary>
    /// <remarks>
    /// 965 of these in the captures, and OmniCell sent none, so a buff on
    /// anybody else was invisible - no icon over them, no way to see what they
    /// were running.
    ///
    /// Action is 0 in all 306 captured in one session, which is a nano starting.
    /// Nothing in the captures shows one ending, because nothing captured ran
    /// long enough to expire, so the value used when a nano wears off is a
    /// reading rather than an observation: 1, the obvious counterpart, and it is
    /// named rather than left as a bare number so that a later capture can
    /// correct it in one place.
    /// </remarks>
    [MessageHandler(MessageHandlerDirection.OutboundOnly)]
    public class BuffMessageHandler :
        BaseMessageHandler<BuffMessage, BuffMessageHandler>
    {
        #region Outbound

        /// <summary>
        /// A nano has started running on this character.
        /// </summary>
        public void Applied(ICharacter character, int nanoId)
        {
            this.Send(character, Filler(character, nanoId, BuffStarted), true);
        }

        /// <summary>
        /// A nano has stopped running on this character.
        /// </summary>
        public void Removed(ICharacter character, int nanoId)
        {
            this.Send(character, Filler(character, nanoId, BuffEnded), true);
        }

        /// <summary>
        /// Action of a nano starting. Zero in every capture.
        /// </summary>
        private const short BuffStarted = 0;

        /// <summary>
        /// Action of a nano ending. Not seen in any capture - see the remarks
        /// on this class.
        /// </summary>
        private const short BuffEnded = 1;

        private static MessageDataFiller Filler(ICharacter character, int nanoId, short action)
        {
            return message =>
            {
                message.Identity = character.Identity;
                message.Unknown = 0;
                message.Action = action;
                message.Instance = character.Identity.Instance;
                message.NanoId = nanoId;
            };
        }
        #endregion
    }
}
