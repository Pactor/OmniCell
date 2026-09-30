#region License

// Copyright (c) 2026, OmniCell contributors
//
// This file is part of OmniCell and is distributed under the terms the project
// is licensed under. See the repository root for details.

#endregion

namespace ZoneEngine.Core.Combat
{
    #region Usings ...

    using System;
    using System.Collections.Concurrent;
    using System.Threading;

    using OmniCell.Core.Entities;
    using OmniCell.Core.Vector;
    using OmniCell.Enums;

    using SmokeLounge.AOtomation.Messaging.GameData;

    using Utility;

    using ZoneEngine.Core.Controllers;

    #endregion

    /// <summary>
    /// What happens to a player after the killing blow.
    /// </summary>
    /// <remarks>
    /// Without this a dead player stays dead: the client falls over, waits to
    /// be told where it wakes up, and is never told - a white screen.
    ///
    /// Two seconds for the death animation to be seen, health back to full,
    /// then a teleport inside the same playfield, which makes the client
    /// reconnect and come up standing.
    ///
    /// This is a stopgap and says so. Where a player wakes up is ours, not
    /// retail's: there are no reclaim terminals here, so it is a fixed point
    /// for the playfields that have one written down and the place they fell
    /// for every other. Nothing is lost on death and nothing is charged for
    /// it. The retail sequence, when somebody records it, belongs here.
    /// </remarks>
    public static class PlayerDeath
    {
        /// <summary>
        /// How long the body lies there before the player is brought back.
        /// </summary>
        private const int AnimationMilliseconds = 2000;

        /// <summary>
        /// One respawn at a time for each player.
        /// </summary>
        private static readonly ConcurrentDictionary<Identity, bool> Rising =
            new ConcurrentDictionary<Identity, bool>();

        /// <summary>
        /// Brings <paramref name="victim"/> back, if it is a player.
        /// </summary>
        public static void Respawn(ICharacter victim)
        {
            if (victim == null || victim.Controller == null || victim.Controller is NPCController)
            {
                return;
            }

            if (!Rising.TryAdd(victim.Identity, true))
            {
                return;
            }

            ThreadPool.QueueUserWorkItem(
                _ =>
                    {
                        try
                        {
                            Thread.Sleep(AnimationMilliseconds);

                            var character = victim as Character;
                            if (character == null || character.Playfield == null)
                            {
                                return;
                            }

                            character.Stats[StatIds.health].Value = character.Stats[StatIds.life].Value;
                            character.Playfield.Teleport(
                                character,
                                WakesAt(character),
                                character.Heading,
                                character.Playfield.Identity);
                        }
                        catch (Exception exception)
                        {
                            LogUtil.ErrorException(exception);
                        }
                        finally
                        {
                            bool ignored;
                            Rising.TryRemove(victim.Identity, out ignored);
                        }
                    });
        }

        private static Coordinate WakesAt(ICharacter character)
        {
            switch (character.Playfield.Identity.Instance)
            {
                case 4582:
                    return new Coordinate(939, 20, 732);
                case 6553:
                    return new Coordinate(3609, 53, 786);
                default:
                    return character.Coordinates();
            }
        }
    }
}
