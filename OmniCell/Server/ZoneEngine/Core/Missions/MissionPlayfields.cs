#region License

// Copyright (c) 2026, OmniCell contributors
//
// This file is part of OmniCell and is distributed under the terms the project
// is licensed under. See the repository root for details.

#endregion

namespace ZoneEngine.Core.Missions
{
    #region Usings ...

    using System.Collections.Generic;
    using System.Linq;

    using OmniCell.Core.Missions;

    using SmokeLounge.AOtomation.Messaging.GameData;

    #endregion

    /// <summary>
    /// The playfields that only exist because somebody took a mission.
    /// </summary>
    /// <remarks>
    /// Every other playfield in the server is in the content pack: it has a
    /// number, a statel list and a file behind it. A mission's playfield has
    /// none of that. It is a pool of rooms and a list of placements, made when
    /// the mission was taken, and it stops existing when the mission does.
    ///
    /// So the rest of the server has to be able to ask two things - is this
    /// playfield a mission, and which one - and this is where it asks. It is
    /// the reason <see cref="Playfields.Playfield"/> no longer insists on
    /// finding its instance in the pack.
    ///
    /// In memory, like the missions themselves. A restart loses them, and a
    /// character standing in one when it goes has to be put back outside; see
    /// <see cref="Escape"/>.
    /// </remarks>
    public static class MissionPlayfields
    {
        private static readonly Dictionary<int, MissionOffer> Live =
            new Dictionary<int, MissionOffer>();

        private static readonly object Gate = new object();

        /// <summary>
        /// Where a character goes when the mission they were in is gone.
        /// </summary>
        /// <remarks>
        /// Borealis, outside the mission terminals. A character logging in to a
        /// playfield that no longer exists would otherwise sit on the loading
        /// screen with nothing the server could send them.
        /// </remarks>
        public const int Escape = 800;

        /// <summary>
        /// Start serving this mission's playfield.
        /// </summary>
        public static void Open(MissionOffer offer)
        {
            if (offer == null || offer.PlayfieldInstance == 0 || offer.Built == null)
            {
                return;
            }

            lock (Gate)
            {
                Live[offer.PlayfieldInstance] = offer;
            }
        }

        /// <summary>
        /// Stop serving it.
        /// </summary>
        public static void Close(MissionOffer offer)
        {
            if (offer == null)
            {
                return;
            }

            lock (Gate)
            {
                Live.Remove(offer.PlayfieldInstance);
            }
        }

        /// <summary>
        /// Whether this playfield is a mission's.
        /// </summary>
        public static bool IsMission(int playfield)
        {
            lock (Gate)
            {
                return Live.ContainsKey(playfield);
            }
        }

        /// <summary>
        /// The mission a playfield belongs to, or null.
        /// </summary>
        public static MissionOffer Of(int playfield)
        {
            lock (Gate)
            {
                MissionOffer offer;
                return Live.TryGetValue(playfield, out offer) ? offer : null;
            }
        }

        /// <summary>
        /// Which mission each mission key opens.
        /// </summary>
        /// <remarks>
        /// A key names a building, not a player: the character handed a
        /// duplicate walks into the building the original opens, which the
        /// 2026-09-26 capture shows outright. Retail can do that because the
        /// key carries the building's name and nothing else; here every key is
        /// template 28577 and they are told apart by their instance, so the
        /// instance is what is written down.
        /// </remarks>
        private static readonly Dictionary<int, MissionOffer> Keys =
            new Dictionary<int, MissionOffer>();

        /// <summary>
        /// This key opens that mission.
        /// </summary>
        public static void Cut(int keyInstance, MissionOffer offer)
        {
            if (keyInstance == 0 || offer == null)
            {
                return;
            }

            lock (Gate)
            {
                Keys[keyInstance] = offer;
            }
        }

        /// <summary>
        /// The mission a character standing at a door may walk into, or null.
        /// </summary>
        /// <param name="keyInstances">
        /// The instances of the mission keys they are carrying.
        /// </param>
        public static MissionOffer OpenedBy(IEnumerable<int> keyInstances)
        {
            if (keyInstances == null)
            {
                return null;
            }

            lock (Gate)
            {
                foreach (int key in keyInstances)
                {
                    MissionOffer offer;
                    if (Keys.TryGetValue(key, out offer) && Live.ContainsKey(offer.PlayfieldInstance))
                    {
                        return offer;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Where a character walked in from, so the same door puts them back.
        /// </summary>
        private static readonly Dictionary<Identity, Return> Outside =
            new Dictionary<Identity, Return>();

        private sealed class Return
        {
            public int Playfield;

            public float X;

            public float Y;

            public float Z;
        }

        /// <summary>
        /// Remember where a character was standing when they went in.
        /// </summary>
        public static void Remember(Identity character, int playfield, float x, float y, float z)
        {
            lock (Gate)
            {
                Outside[character] = new Return { Playfield = playfield, X = x, Y = y, Z = z };
            }
        }

        /// <summary>
        /// Where they came in from, or Borealis if nothing was remembered.
        /// </summary>
        public static bool Recall(
            Identity character, out int playfield, out float x, out float y, out float z)
        {
            lock (Gate)
            {
                Return where;
                if (Outside.TryGetValue(character, out where))
                {
                    playfield = where.Playfield;
                    x = where.X;
                    y = where.Y;
                    z = where.Z;
                    Outside.Remove(character);
                    return true;
                }
            }

            playfield = Escape;
            x = 0;
            y = 0;
            z = 0;
            return false;
        }

        /// <summary>
        /// The playfield identity a mission's building goes by.
        /// </summary>
        public static Identity Identity(MissionOffer offer)
        {
            return new Identity
                   {
                       Type = IdentityType.Playfield,
                       Instance = offer.PlayfieldInstance
                   };
        }
    }
}
