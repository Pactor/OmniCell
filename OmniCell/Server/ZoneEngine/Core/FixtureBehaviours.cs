#region License

// Copyright (c) 2026, OmniCell contributors
//
// This file is part of OmniCell and is distributed under the terms the project
// is licensed under. See the repository root for details.

#endregion

namespace ZoneEngine.Core
{
    #region Usings

    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using OmniCell.Core.Entities;
    using OmniCell.Database.Dao;
    using OmniCell.Database.Entities;
    using OmniCell.ObjectManager;

    using SmokeLounge.AOtomation.Messaging.GameData;

    using Utility;

    using ZoneEngine.Core.MessageHandlers;

    #endregion

    /// <summary>
    /// What a kind of fixture does when it is used.
    /// </summary>
    /// <remarks>
    /// A Gas Fire goes out when it is put out and burns again later; the quest Cargo Box opens and
    /// comes back. Retail despawns the used fixture and later sends it again at the same position
    /// (20260914-124401 seq 2154 and 2833), which is what keeps a quest doable for the next player.
    /// Rows come from the quest extraction (fixturebehaviours); how long a fixture stays away is not in
    /// any capture yet, so the extracted rows carry an OmniCell-defined delay.
    /// </remarks>
    public static class FixtureBehaviours
    {
        private static readonly ConcurrentDictionary<int, DBFixtureBehaviour> Behaviours =
            new ConcurrentDictionary<int, DBFixtureBehaviour>();

        /// <summary>
        /// Reads the fixture behaviours. Called once, at startup.
        /// </summary>
        public static int Load()
        {
            Behaviours.Clear();
            foreach (DBFixtureBehaviour behaviour in FixtureBehaviourDao.Instance.GetAll())
            {
                Behaviours[behaviour.Template] = behaviour;
            }

            return Behaviours.Count;
        }

        /// <summary>
        /// A player used a fixture: its feedback, and away it goes if it is the kind that does.
        /// </summary>
        /// <param name="sendFeedback">
        /// False when the fixture's own template event already sent its feedback.
        /// </param>
        public static void Used(ICharacter user, StaticDynel fixture, bool sendFeedback = true)
        {
            DBFixtureBehaviour behaviour;
            if (user == null
                || fixture == null
                || fixture.Template == null
                || fixture.Hidden
                || !Behaviours.TryGetValue(fixture.Template.ID, out behaviour))
            {
                return;
            }

            if (sendFeedback && !string.IsNullOrEmpty(behaviour.Feedback))
            {
                FormatFeedbackMessageHandler.Default.Send(user, behaviour.Feedback);
            }

            if (behaviour.DespawnOnUse == 0 || user.Playfield == null)
            {
                return;
            }

            Identity playfield = user.Playfield.Identity;
            fixture.Hidden = true;
            foreach (Character player in PlayersIn(playfield))
            {
                DespawnMessageHandler.Default.Send(player, fixture.Identity);
            }

            Task.Delay(TimeSpan.FromSeconds(Math.Max(1, behaviour.RespawnSeconds))).ContinueWith(
                delay =>
                {
                    try
                    {
                        fixture.Hidden = false;
                        foreach (Character player in PlayersIn(playfield))
                        {
                            SimpleItemFullUpdateMessageHandler.Default.Send(player, fixture);
                        }
                    }
                    catch (Exception exception)
                    {
                        LogUtil.ErrorException(exception);
                    }
                });
        }

        private static IEnumerable<Character> PlayersIn(Identity playfield)
        {
            return Pool.Instance.GetAll<Character>((int)IdentityType.CanbeAffected)
                .Where(c => c != null && c.InPlayfield(playfield) && c.Controller != null && c.Controller.Client != null)
                .ToList();
        }
    }
}
