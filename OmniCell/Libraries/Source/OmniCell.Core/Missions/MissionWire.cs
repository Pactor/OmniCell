#region License

// Copyright (c) 2026, OmniCell contributors
//
// This file is part of OmniCell and is distributed under the terms the project
// is licensed under. See the repository root for details.

#endregion

namespace OmniCell.Core.Missions
{
    #region Usings ...

    using System;

    using SmokeLounge.AOtomation.Messaging.GameData;

    #endregion

    /// <summary>
    /// A generated mission as the client is shown it.
    /// </summary>
    /// <remarks>
    /// Every constant below is what all 25 offers decoded out of
    /// 20260910-200346 and 20260926-135805 carry. Where the offers differ from
    /// each other the value comes off the mission; where they do not, it is a
    /// constant here and the remark says what it was measured on.
    /// </remarks>
    public static class MissionWire
    {
        #region Constants every captured offer carries

        /// <summary>15 in all 25 captured offers - the same as an authored quest.</summary>
        private const int Version = 15;

        /// <summary>
        /// 0 in all 25 captured offers, where an authored quest carries 2.
        /// </summary>
        /// <remarks>
        /// This is the one field that tells a generated mission from an
        /// authored quest on the wire.
        /// </remarks>
        private const int OfferedFlags = 0;

        /// <summary>6 in all 25, and in all 218 authored quest records.</summary>
        private const int RewardDescriptorVersion = 6;

        /// <summary>
        /// 1297306181 in all 25, which is 0x4D535245 - the four characters
        /// "MSRE" laid down as an int32.
        /// </summary>
        /// <remarks>
        /// The field was named UnknownHash before the offers were decoded. It
        /// is a tag rather than a hash: every offer at every terminal and both
        /// captures carry the same four letters.
        /// </remarks>
        private const int Tag = 0x4D535245;

        /// <summary>6 in all 25 captured offers.</summary>
        private const int Unknown20 = 6;

        /// <summary>1 in all 25 captured offers.</summary>
        private const int Unknown26 = 1;

        /// <summary>
        /// The identity type the quest action names its target with.
        /// </summary>
        /// <remarks>
        /// 70099 in every captured offer, and the instance is four characters
        /// - "MRBO", "SL13", "SP05". A resource key of some kind; what it keys
        /// is not known.
        /// </remarks>
        private const IdentityType ObjectiveCodeType = (IdentityType)70099;

        #endregion

        /// <summary>
        /// The mission, as a QuestInfo.
        /// </summary>
        /// <param name="offer">The mission.</param>
        /// <param name="owner">
        /// Who it belongs to, or None while it is only on offer - a captured
        /// offer's Owner is None and an accepted mission's is the character.
        /// </param>
        public static QuestInfo Info(MissionOffer offer, Identity owner)
        {
            return new QuestInfo
                   {
                       QuestIdentity = new Identity
                                       {
                                           Type = IdentityType.Quest,
                                           Instance = offer.Instance
                                       },
                       Version = Version,
                       Unread1 = 0,
                       Unread2 = 0,
                       Flags = OfferedFlags,
                       ShortInfo = offer.ShortInfo ?? string.Empty,
                       Info = offer.Info ?? string.Empty,
                       QuestGiver = offer.Terminal,
                       RewardDescriptorVersion = RewardDescriptorVersion,
                       CashReward = offer.CashReward,
                       RewardUnread = 0,
                       ExperienceReward = offer.ExperienceReward,
                       UnknownIdentities1 = new Identity[0],
                       UnknownIdentities2 = new Identity[0],
                       ItemRewards = offer.RewardLowId == 0
                                         ? new QuestItemShort[0]
                                         : new[]
                                           {
                                               new QuestItemShort
                                               {
                                                   LowId = offer.RewardLowId,
                                                   HighId = offer.RewardHighId,
                                                   Quality = offer.RewardQuality,
                                                   Unused = 0
                                               }
                                           },
                       QuestCode = 0,
                       Unknown8 = 0,
                       Unknown9 = 0,
                       UnknownHash = Tag,
                       Quality = offer.Quality,
                       RewardItem = new AcgItem(),
                       Owner = owner,
                       MissionIconId = (int)offer.Type,
                       TimeLimit = 0,
                       TimeLimitCopy = MissionRoller.TimeLimitMinutes,
                       QuestActions = new[] { Action(offer) },
                       Unknown17 = new Identity[0],
                       ActionTrackingInstances = new int[0],
                       Unknown19 = new int[0],
                       CharInfos = new QuestCharInfo[0],
                       Unknown20 = Unknown20,
                       UnknownIdentities20 = new Identity[0],
                       RequiredCount = 0,
                       Unknown22 = 0,
                       Unknown23 = Identity.None,
                       Unknown24 = 0,
                       Unknown25 = 0,
                       QuestIdentities = new QuestIdentity[0],
                       Unknown26 = Unknown26,
                       FactionInfo = new QuestFaction[0]
                   };
        }

        /// <summary>
        /// The single action a mission carries, which differs by type.
        /// </summary>
        /// <remarks>
        /// Read off the 25 captured offers. Version is the type's own number
        /// and the target sits in a different member depending on it:
        ///
        ///   find person   16   Unknown2 = the target's code
        ///   kill person    1   Unknown2 = the target's code
        ///   find item     15   Action   = the item's code
        ///   return item    8   Action   = the item's code, Unknown1 = terminal
        ///   repair         8   Action   = the part's code, Unknown1 = fixture
        ///
        /// Deadline is a unix time 48 hours out, which is what TimeLimitCopy
        /// says in minutes.
        /// </remarks>
        private static QuestActionList Action(MissionOffer offer)
        {
            var code = new Identity { Type = ObjectiveCodeType, Instance = offer.ObjectiveCode };
            var action = new QuestActionList
                         {
                             Version = ActionVersion(offer.Type),
                             Action = Identity.None,
                             Unknown1 = Identity.None,
                             Unknown2 = Identity.None,
                             Unknown3 = Identity.None,
                             Unknown4 = Identity.None,
                             Unknown5 = 0,
                             Unknown6 = 0,
                             Unknown7 = 0,
                             Unknown8 = 0,
                             Unknown9 = Identity.None,
                             Unknown10 = 0,
                             Unknown11 = 0,
                             Unknown12 = 0,
                             Unknown13 = 0,
                             Unknown14 = Identity.None,
                             Deadline = Deadline(),
                             Unknown16 = 0,
                             ActionTracking = Identity.None,
                             Playfield = new Identity
                                         {
                                             Type = IdentityType.Playfield2,
                                             Instance = offer.Playfield
                                         },
                             Unknown18 = 0,
                             Unknown19 = 0,
                             X = offer.X,
                             Y = offer.Y,
                             Z = offer.Z
                         };

            switch (offer.Type)
            {
                case MissionType.FindPerson:
                case MissionType.KillPerson:
                    action.Unknown2 = code;
                    break;

                case MissionType.FindItem:
                    action.Action = code;
                    break;

                case MissionType.ReturnItem:
                    action.Action = code;
                    action.Unknown1 = offer.Terminal;
                    break;

                case MissionType.Repair:
                    action.Action = code;
                    action.Unknown1 = new Identity
                                      {
                                          Type = ObjectiveCodeType,
                                          Instance = offer.ObjectiveCode + 1
                                      };
                    break;
            }

            return action;
        }

        private static int ActionVersion(MissionType type)
        {
            switch (type)
            {
                case MissionType.FindPerson: return 16;
                case MissionType.KillPerson: return 1;
                case MissionType.FindItem: return 15;
                case MissionType.ReturnItem: return 8;
                case MissionType.Repair: return 8;
                default: return 1;
            }
        }

        private static int Deadline()
        {
            var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            return (int)(DateTime.UtcNow.AddMinutes(MissionRoller.TimeLimitMinutes) - epoch)
                .TotalSeconds;
        }
    }
}
