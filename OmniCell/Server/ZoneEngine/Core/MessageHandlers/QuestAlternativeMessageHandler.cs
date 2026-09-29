#region License

// Copyright (c) 2026, OmniCell contributors
//
// This file is part of OmniCell and is distributed under the terms the project
// is licensed under. See the repository root for details.

#endregion

namespace ZoneEngine.Core.MessageHandlers
{
    #region Usings ...

    using System;
    using System.Collections.Generic;
    using System.Linq;

    using OmniCell.Core.Components;
    using OmniCell.Core.Entities;
    using OmniCell.Core.Network;
    using OmniCell.Enums;

    using Utility;
    using OmniCell.ObjectManager;

    using SmokeLounge.AOtomation.Messaging.GameData;
    using SmokeLounge.AOtomation.Messaging.Messages.N3Messages;

    using OmniCell.Core.Missions;

    using ZoneEngine.Core.Playfields;

    #endregion

    /// <summary>
    /// A mission terminal, asked what it has.
    /// </summary>
    /// <remarks>
    /// The same message in both directions. The client sends the difficulty,
    /// six dimension bytes and the terminal with an empty list; the server
    /// answers with the same message carrying up to five missions. The client
    /// refuses more than five - "Number of quests = %u" at Gamecode
    /// 0x100CB27C.
    ///
    /// Two things the captures settle that this has to get right, both in
    /// Documentation/Missions.md:
    ///
    ///   * the reply carries the six dimensions the draw actually ran at, and
    ///     they are not always the ones asked for. With every byte zero - an
    ///     untouched slider panel - retail picks them itself and says which it
    ///     picked. <see cref="MissionRoller"/> does the picking; this puts what
    ///     it picked on the wire.
    ///   * the difficulty is echoed unchanged, and has been in all nine
    ///     captured rolls.
    /// </remarks>
    [MessageHandler(MessageHandlerDirection.All)]
    public class QuestAlternativeMessageHandler :
        BaseMessageHandler<QuestAlternativeMessage, QuestAlternativeMessageHandler>
    {
        #region Constants

        /// <summary>
        /// 4 in every captured copy, and checked by the client against its own
        /// class version through the vtable at Gamecode 0x100C9F88.
        /// </summary>
        private const byte VersionId = 4;

        /// <summary>
        /// The client refuses a difficulty outside 1 to 11 and an originator
        /// outside 1 to 8, and drops the whole packet when either is out of
        /// range. See QuestAlternativeMessage's remarks.
        /// </summary>
        private const int LowestDifficulty = 1;

        /// <summary>
        /// The client refuses a difficulty outside 1 to 11.
        /// </summary>
        private const int HighestDifficulty = 11;

        #endregion

        public QuestAlternativeMessageHandler()
        {
            this.UpdateCharacterStatsOnReceive = false;
        }

        #region Inbound

        protected override void Read(QuestAlternativeMessage message, IZoneClient client)
        {
            if (client == null || client.Controller == null || client.Controller.Character == null)
            {
                return;
            }

            ICharacter character = client.Controller.Character;

            // A copy carrying missions is an answer, not a question. Nothing
            // should send the server one, and answering it would be a loop.
            if (message.QuestInfos != null && message.QuestInfos.Length > 0)
            {
                LogUtil.Debug(
                    DebugInfoDetail.Engine,
                    "Mission roll ignored: it already carries " + message.QuestInfos.Length + " missions.");
                return;
            }

            LogUtil.Debug(
                DebugInfoDetail.Engine,
                "Mission roll asked for by " + character.Identity.Instance + " at terminal "
                + message.MissionTerminalIdentity.Instance + ", difficulty " + message.Difficulty
                + ", originator " + message.Originator + ".");

            int difficulty = Math.Min(HighestDifficulty, Math.Max(LowestDifficulty, (int)message.Difficulty));

            var asked = new[]
                        {
                            (sbyte)message.GoodBad, (sbyte)message.ControlledLackingControl,
                            (sbyte)message.OpenHidden, (sbyte)message.PhysicalMystical,
                            (sbyte)message.ExplosivePatient, (sbyte)message.MoneyExperience
                        };

            // The seed is the server's. Retail sends a different one every
            // roll and the client only carries it into the mission list.
            int seed = Seed();
            sbyte[] answered;
            List<MissionOffer> offers = MissionRoller.Roll(
                character.Stats[StatIds.level].Value,
                difficulty,
                asked,
                message.MissionTerminalIdentity,
                MissionRoller.IsTeam((int)message.Originator),
                character.Playfield == null ? 0 : character.Playfield.Identity.Instance,
                PlayfieldName(character),
                seed,
                out answered);

            // The identities the client will name a mission back by. They are
            // the server's to allocate and retail's are consecutive.
            foreach (MissionOffer offer in offers)
            {
                offer.Instance = Pool.Instance.GetFreeInstance<QuestAlternativeMessage>(
                    1, IdentityType.Quest);
            }

            MissionBook.Offer(character, offers);
            LogUtil.Debug(
                DebugInfoDetail.Engine,
                "Mission roll answered with " + offers.Count + " offers for level "
                + character.Stats[StatIds.level].Value + " at difficulty " + difficulty + ".");
            this.Send(character, offers, difficulty, answered, seed, message.Originator, message.MissionTerminalIdentity);
        }

        /// <summary>
        /// The playfield's own name, which the assignment text quotes.
        /// </summary>
        private static string PlayfieldName(ICharacter character)
        {
            if (character == null || character.Playfield == null)
            {
                return null;
            }

            OmniCell.Core.Playfields.PlayfieldData data;
            return PlayfieldLoader.PFData.TryGetValue(
                       character.Playfield.Identity.Instance, out data) && data.Name != null
                       ? data.Name.Trim()
                       : null;
        }

        /// <summary>
        /// A fresh seed per roll, as retail sends.
        /// </summary>
        /// <remarks>
        /// Kept positive: the field is read as an unsigned int by the client's
        /// own diagnostic, and every captured value is inside int range.
        /// </remarks>
        private static int Seed()
        {
            return Random.Shared.Next(1, int.MaxValue);
        }

        #endregion

        #region Outbound

        /// <summary>
        /// What the terminal has.
        /// </summary>
        public void Send(
            ICharacter character,
            IList<MissionOffer> offers,
            int difficulty,
            sbyte[] dimensions,
            int seed,
            QuestOriginator originator,
            Identity terminal)
        {
            this.Send(
                character,
                message =>
                {
                    message.Identity = character.Identity;
                    message.Unknown = 0;
                    message.VersionId = VersionId;
                    message.Difficulty = (byte)difficulty;
                    message.GoodBad = (byte)dimensions[0];
                    message.ControlledLackingControl = (byte)dimensions[1];
                    message.OpenHidden = (byte)dimensions[2];
                    message.PhysicalMystical = (byte)dimensions[3];
                    message.ExplosivePatient = (byte)dimensions[4];
                    message.MoneyExperience = (byte)dimensions[5];
                    message.Seed = seed;
                    message.Originator = originator;
                    message.MissionTerminalIdentity = terminal;

                    // The trailer byte is per mission and the client carries it
                    // as far as the mission window without reading it back. The
                    // four captured copies read index-like at the front, so the
                    // index is what goes there.
                    message.QuestInfos = offers.Select(
                        (offer, index) => new QuestAlternativeEntry
                                          {
                                              Quest = MissionWire.Info(offer, Identity.None),
                                              Trailer = (byte)index
                                          }).ToArray();
                },
                false);
        }

        #endregion
    }
}
