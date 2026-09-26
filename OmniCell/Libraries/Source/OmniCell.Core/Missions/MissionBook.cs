#region License

// Copyright (c) 2026, OmniCell contributors
//
// This file is part of OmniCell and is distributed under the terms the project
// is licensed under. See the repository root for details.

#endregion

namespace OmniCell.Core.Missions
{
    #region Usings ...

    using System.Collections.Generic;
    using System.Linq;

    using OmniCell.Core.Entities;

    using SmokeLounge.AOtomation.Messaging.GameData;

    #endregion

    /// <summary>
    /// What each character has been offered, and what they have taken.
    /// </summary>
    /// <remarks>
    /// In memory and nowhere else. A mission is generated when the terminal is
    /// asked and the whole of it - the building, what stands in it, the key -
    /// follows from the offer, so the offer is the only thing that has to
    /// survive between the roll and the accept. Surviving a restart is a
    /// separate job and wants a table; until there is one, a restart loses
    /// missions in progress and this says so rather than pretending otherwise.
    /// </remarks>
    public static class MissionBook
    {
        private static readonly Dictionary<Identity, List<MissionOffer>> Offered =
            new Dictionary<Identity, List<MissionOffer>>();

        private static readonly Dictionary<Identity, List<MissionOffer>> Taken =
            new Dictionary<Identity, List<MissionOffer>>();

        private static readonly object Gate = new object();

        /// <summary>
        /// How many missions one character may be on at once.
        /// </summary>
        /// <remarks>
        /// The client's own mission window holds this many. No capture shows
        /// the server refusing a sixth, so this is the interface's limit rather
        /// than a measured server rule.
        /// </remarks>
        public const int MostAtOnce = 5;

        /// <summary>
        /// Remember what a terminal just offered, replacing any earlier list.
        /// </summary>
        public static void Offer(ICharacter character, List<MissionOffer> offers)
        {
            if (character == null || offers == null)
            {
                return;
            }

            lock (Gate)
            {
                Offered[character.Identity] = offers;
            }
        }

        /// <summary>
        /// One of the missions a terminal last offered this character.
        /// </summary>
        public static MissionOffer OfferedTo(ICharacter character, int instance)
        {
            if (character == null)
            {
                return null;
            }

            lock (Gate)
            {
                List<MissionOffer> list;
                return Offered.TryGetValue(character.Identity, out list)
                           ? list.FirstOrDefault(x => x.Instance == instance)
                           : null;
            }
        }

        /// <summary>
        /// The missions this character is on.
        /// </summary>
        public static List<MissionOffer> Active(ICharacter character)
        {
            if (character == null)
            {
                return new List<MissionOffer>();
            }

            lock (Gate)
            {
                List<MissionOffer> list;
                return Taken.TryGetValue(character.Identity, out list)
                           ? new List<MissionOffer>(list)
                           : new List<MissionOffer>();
            }
        }

        /// <summary>
        /// The mission this character is on with that identity, or null.
        /// </summary>
        public static MissionOffer ActiveOne(ICharacter character, int instance)
        {
            return Active(character).FirstOrDefault(x => x.Instance == instance);
        }

        /// <summary>
        /// Take one on. The offer list it came from is dropped, which is what
        /// the client does with the window.
        /// </summary>
        public static void Take(ICharacter character, MissionOffer offer)
        {
            if (character == null || offer == null)
            {
                return;
            }

            lock (Gate)
            {
                List<MissionOffer> list;
                if (!Taken.TryGetValue(character.Identity, out list))
                {
                    list = new List<MissionOffer>();
                    Taken[character.Identity] = list;
                }

                list.Add(offer);
                Offered.Remove(character.Identity);
            }
        }

        /// <summary>
        /// Give one up, or finish it.
        /// </summary>
        public static bool Drop(ICharacter character, int instance)
        {
            if (character == null)
            {
                return false;
            }

            lock (Gate)
            {
                List<MissionOffer> list;
                if (!Taken.TryGetValue(character.Identity, out list))
                {
                    return false;
                }

                int removed = list.RemoveAll(x => x.Instance == instance);
                return removed > 0;
            }
        }

        /// <summary>
        /// Forget a character who has left.
        /// </summary>
        public static void Forget(Identity character)
        {
            lock (Gate)
            {
                Offered.Remove(character);
                Taken.Remove(character);
            }
        }
    }
}
