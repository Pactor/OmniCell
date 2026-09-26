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
    using OmniCell.Core.Missions;
    using OmniCell.Core.Network;

    using SmokeLounge.AOtomation.Messaging.GameData;
    using SmokeLounge.AOtomation.Messaging.Messages.N3Messages;

    using ZoneEngine.Core.Quests;

    #endregion

    /// <summary>
    /// Names one quest, rather than sending the whole list again.
    /// </summary>
    /// <remarks>
    /// QuestFullUpdate carries everything a character is on. This carries one
    /// quest identity and nothing else, and the live server sends it alongside
    /// the full update rather than instead of it - 58 of these against 61 full
    /// updates, which is close enough to one each to read as "and this is the
    /// one that changed".
    ///
    /// The other fields hold the same value in all 58: Version is 1, and the two
    /// still unnamed, Unknown2 and Unknown3, are 0. With no sample showing anything
    /// else there is nothing to say about what they would mean otherwise, so they
    /// are sent as seen rather than dressed up as an action code they may not be.
    /// </remarks>
    [MessageHandler(MessageHandlerDirection.All)]
    public class QuestMessageHandler : BaseMessageHandler<QuestMessage, QuestMessageHandler>
    {
        public QuestMessageHandler()
        {
            this.UpdateCharacterStatsOnReceive = false;
        }

        #region Constants

        /// <summary>
        /// The version the client checks this message against, and the only
        /// value it will accept.
        /// </summary>
        /// <remarks>
        /// Compared against the static at Gamecode.dll 0x101C1364, and 1 in all
        /// 102 captured copies.
        /// </remarks>
        private const int Version = 1;

        #endregion

        #region Inbound

        /// <summary>
        /// The client sends this to give a mission or a quest up.
        /// </summary>
        /// <remarks>
        /// The same message in both directions: the server sends it to say a
        /// quest has left the window, and the client sends it to ask for that.
        /// A generated mission is dropped from the book and an authored quest
        /// goes to the quest manager, which is the only one of the two that
        /// has anything written down to undo.
        /// </remarks>
        protected override void Read(QuestMessage message, IZoneClient client)
        {
            if (client == null || client.Controller == null || client.Controller.Character == null)
            {
                return;
            }

            ICharacter character = client.Controller.Character;
            int quest = message.QuestIdentity.Instance;

            if (MissionBook.Drop(character, quest))
            {
                this.Send(character, quest);
                QuestFullUpdateMessageHandler.Default.SendMissions(
                    character, MissionBook.Active(character), false);
                return;
            }

            QuestManager.Abandon(character, quest);
        }

        #endregion

        #region Outbound

        /// <summary>
        /// </summary>
        public void Send(ICharacter character, int questId)
        {
            this.Send(character, this.FillData(character, questId), false);
        }

        public MessageDataFiller FillData(ICharacter character, int questId)
        {
            return message =>
            {
                message.Identity = character.Identity;
                message.Unknown = 0;
                message.Version = Version;
                message.Unknown2 = 0;
                message.QuestIdentity = new Identity { Type = IdentityType.Quest, Instance = questId };
                message.Unknown3 = Identity.None;
            };
        }

        #endregion
    }
}
