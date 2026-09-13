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
    using OmniCell.Database.Entities;
    using OmniCell.ObjectManager;

    using SmokeLounge.AOtomation.Messaging.GameData;
    using SmokeLounge.AOtomation.Messaging.Messages.N3Messages;

    #endregion

    /// <summary>
    /// Puts a character's quests in the client's own quest window.
    /// </summary>
    /// <remarks>
    /// OmniCell tracks quests now but had no way to show one. Everything it had
    /// to say about a quest went through the chat window, which is not where a
    /// player looks.
    ///
    /// The current-source capture audit independently decodes all forty fields
    /// in 218 QuestInfo records. Universal values are constants below. Every
    /// quest-specific scalar, action and reward preview comes from normalized
    /// questwire SQL; a quest without that representation is omitted instead
    /// of being filled with a majority value or a guessed marker shape.
    ///
    /// The action tracking identity is the only runtime-created value. Captures
    /// prove its type is quest-specific and that QuestInfo.Unknown18 repeats
    /// the instance with its upper five bits masked. OmniCell allocates a fresh
    /// instance and preserves that exact relationship.
    /// </remarks>
    [MessageHandler(MessageHandlerDirection.OutboundOnly)]
    public class QuestFullUpdateMessageHandler :
        BaseMessageHandler<QuestFullUpdateMessage, QuestFullUpdateMessageHandler>
    {
        #region Constants

        /// <summary>
        /// The same in all 218 captured quest entries.
        /// </summary>
        private const int RewardDescriptorVersion = 6;

        /// <summary>15 in all 218 independently decoded captured entries.</summary>
        private const int VersionCommon = 15;

        /// <summary>2 in all 218 independently decoded captured entries.</summary>
        private const int FlagsCommon = 2;

        #endregion

        #region Outbound

        /// <summary>
        /// Sends every quest this character is on.
        /// </summary>
        /// <remarks>
        /// <paramref name="announceAsNew"/> is the client's announcement flag,
        /// not a continuation marker: the dispatcher raises
        /// Feedback_YouGotANewMission once per quest in the list when it is
        /// set. Only the path that has just started a quest passes true.
        /// </remarks>
        public void Send(
            ICharacter character,
            IEnumerable<QuestFullUpdateEntry> quests,
            bool announceAsNew)
        {
            if (character == null)
            {
                return;
            }

            this.Send(character, this.FillData(character, quests, announceAsNew), false);
        }

        /// <summary>
        /// Materializes the quest-window message without putting it on a socket.
        /// This is public so the exact production packet can pass the headless
        /// protocol gates before it is ever sent to a client.
        /// </summary>
        public MessageDataFiller FillData(
            ICharacter character,
            IEnumerable<QuestFullUpdateEntry> quests,
            bool announceAsNew)
        {
            List<QuestFullUpdateEntry> safeQuests = quests == null
                                                         ? new List<QuestFullUpdateEntry>()
                                                         : quests.Where(
                                                             q => q != null && q.Quest != null && q.HasWireData)
                                                             .ToList();
            return message =>
            {
                message.Identity = character == null ? Identity.None : character.Identity;
                message.Unknown = 0;
                message.QuestInfos = character == null
                                         ? new QuestInfo[0]
                                         : safeQuests.Select(q => Info(character, q)).ToArray();
                message.AnnounceAsNew = announceAsNew ? (byte)1 : (byte)0;
            };
        }

        private static QuestInfo Info(ICharacter character, QuestFullUpdateEntry entry)
        {
            DBQuest quest = entry.Quest;
            DBQuestWire wire = entry.Wire;
            QuestActionList[] actions = entry.WireActions.Select(Action).ToArray();
            int[] tracking = actions.Select(a => a.Unknown17.Instance & 0x07FFFFFF).ToArray();

            return new QuestInfo
                   {
                       QuestIdentity = new Identity { Type = IdentityType.Quest, Instance = quest.Id },
                       Version = VersionCommon,
                       Unread1 = 0,
                       Unread2 = 0,
                       Flags = FlagsCommon,
                       ShortInfo = quest.Name ?? string.Empty,
                       Info = quest.Description ?? string.Empty,
                       QuestGiver = Id(wire.GiverType, wire.GiverInstance),
                       RewardDescriptorVersion = RewardDescriptorVersion,
                       CashReward = quest.CashReward,
                       RewardUnread = 0,
                       ExperienceReward = quest.ExperienceReward,
                       UnknownIdentities1 = new Identity[0],
                       UnknownIdentities2 = new Identity[0],
                       ItemRewards = entry.WireRewards.Select(
                           r => new QuestItemShort
                                    {
                                        LowId = r.LowId,
                                        HighId = r.HighId,
                                        Quality = r.Quality,
                                        Unknown1 = r.Unknown1
                                    }).ToArray(),
                       QuestCode = wire.QuestCode,
                       Unknown8 = 0,
                       Unknown9 = 0,
                       UnknownHash = wire.UnknownHash,
                       Quality = wire.Quality,
                       RewardItem = new AcgItem(),
                       Owner = character.Identity,
                       MissionIconId = quest.IconId,
                       TimeLimit = wire.TimeLimit,
                       TimeLimitCopy = wire.TimeLimit,
                       QuestActions = actions,
                       Unknown17 = new[] { character.Identity },
                       Unknown18 = tracking,
                       Unknown19 = new int[0],
                       CharInfos = new QuestCharInfo[0],
                       Unknown20 = wire.Unknown20,
                       UnknownIdentities20 = new[] { character.Identity },
                       Unknown21 = wire.Unknown21,
                       Unknown22 = wire.Unknown22,
                       Unknown23 = Id(wire.Unknown23Type, wire.Unknown23Instance),
                       Unknown24 = 0,
                       Unknown25 = wire.Unknown25,
                       QuestIdentities = new QuestIdentity[0],
                       Unknown26 = wire.Unknown26,
                       FactionInfo = new QuestFaction[0]
                   };
        }

        private static QuestActionList Action(DBQuestWireAction source)
        {
            var trackingType = (IdentityType)source.TrackingType;
            int trackingInstance = Pool.Instance.GetFreeInstance<QuestActionList>(1, trackingType);
            return new QuestActionList
                   {
                       Version = source.Version,
                       Action = Id(source.ActionType, source.ActionInstance),
                       Unknown1 = Id(source.Unknown1Type, source.Unknown1Instance),
                       Unknown2 = Id(source.Unknown2Type, source.Unknown2Instance),
                       Unknown3 = Id(source.Unknown3Type, source.Unknown3Instance),
                       Unknown4 = Id(source.Unknown4Type, source.Unknown4Instance),
                       Unknown5 = source.Unknown5,
                       Unknown6 = source.Unknown6,
                       Unknown7 = source.Unknown7,
                       Unknown8 = source.Unknown8,
                       Unknown9 = Id(source.Unknown9Type, source.Unknown9Instance),
                       Unknown10 = source.Unknown10,
                       Unknown11 = source.Unknown11,
                       Unknown12 = source.Unknown12,
                       Unknown13 = source.Unknown13,
                       Unknown14 = Id(source.Unknown14Type, source.Unknown14Instance),
                       Deadline = source.Deadline,
                       Unknown16 = source.Unknown16,
                       Unknown17 = Id(source.TrackingType, trackingInstance),
                       Playfield = Id(source.PlayfieldType, source.PlayfieldInstance),
                       Unknown18 = source.Unknown18,
                       Unknown19 = source.Unknown19,
                       X = source.X,
                       Y = source.Y,
                       Z = source.Z
                   };
        }

        private static Identity Id(int type, int instance)
        {
            return new Identity { Type = (IdentityType)type, Instance = instance };
        }

        #endregion
    }

    /// <summary>
    /// A quest, and where its marker goes.
    /// </summary>
    /// <remarks>
    /// Separate from DBQuest because the marker position is not part of a quest
    /// definition - it is looked up at send time from wherever the character who
    /// gives the quest is standing.
    /// </remarks>
    public class QuestFullUpdateEntry
    {
        public DBQuest Quest { get; set; }

        public DBQuestWire Wire { get; set; }

        public IList<DBQuestWireAction> WireActions { get; set; }

        public IList<DBQuestWireReward> WireRewards { get; set; }

        public bool HasWireData
        {
            get
            {
                return this.Wire != null && this.WireActions != null && this.WireActions.Count > 0
                       && this.WireRewards != null;
            }
        }

        public float MarkerX { get; set; }

        public float MarkerY { get; set; }

        public float MarkerZ { get; set; }
    }
}
