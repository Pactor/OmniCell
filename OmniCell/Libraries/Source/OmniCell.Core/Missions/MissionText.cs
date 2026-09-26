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
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Text;

    using SmokeLounge.AOtomation.Messaging.GameData;

    #endregion

    /// <summary>
    /// The prose a mission terminal writes.
    /// </summary>
    /// <remarks>
    /// An offer is an opener bolted onto a body, and the body is chosen by the
    /// mission's type. Both come out of 105 captured offers and live in
    /// Datafiles/missiontext.tsv; nothing here invents a sentence.
    ///
    /// What it does invent is what goes in the slots. The bodies have braces in
    /// them - {item}, {name}, {place}, {playfield}, {fixture}, {org}, {code},
    /// {credits}, {xp} - and only some of those can be filled from data the
    /// repository holds. Where a slot cannot, this says so at the top of the
    /// filler rather than quietly leaving a brace in the text a player reads.
    /// </remarks>
    public static class MissionText
    {
        #region Loaded text

        private static readonly List<string> Openers = new List<string>();

        private static readonly List<string> Closers = new List<string>();

        private static readonly Dictionary<MissionType, List<string>> Bodies =
            new Dictionary<MissionType, List<string>>();

        /// <summary>
        /// Read the table from beside the engine.
        /// </summary>
        /// <returns>The number of lines of prose loaded.</returns>
        public static int Load()
        {
            return Load("missiontext.tsv");
        }

        /// <summary>
        /// Read a named table.
        /// </summary>
        public static int Load(string filename)
        {
            Openers.Clear();
            Closers.Clear();
            Bodies.Clear();

            // Beside the engine is where it ships, but a tool run from
            // anywhere should be able to name it directly.
            string path = filename;
            if (!Path.IsPathRooted(path) && !File.Exists(path))
            {
                path = Path.Combine(AppContext.BaseDirectory, filename);
            }

            if (!File.Exists(path))
            {
                return 0;
            }

            foreach (string line in File.ReadAllLines(path))
            {
                if (line.Length == 0 || line[0] == '#')
                {
                    continue;
                }

                string[] parts = line.Split('\t');
                if (parts.Length < 3)
                {
                    continue;
                }

                string text = parts[2].Trim();
                if (text.Length == 0)
                {
                    continue;
                }

                switch (parts[0])
                {
                    case "opener":
                        Openers.Add(text);
                        break;
                    case "closer":
                        Closers.Add(text);
                        break;
                    default:
                        MissionType type;
                        if (!Types.TryGetValue(parts[0], out type))
                        {
                            continue;
                        }

                        List<string> bodies;
                        if (!Bodies.TryGetValue(type, out bodies))
                        {
                            bodies = new List<string>();
                            Bodies[type] = bodies;
                        }

                        bodies.Add(text);
                        break;
                }
            }

            return Openers.Count + Closers.Count + Bodies.Sum(x => x.Value.Count);
        }

        /// <summary>
        /// The table's own names for the types.
        /// </summary>
        private static readonly Dictionary<string, MissionType> Types =
            new Dictionary<string, MissionType>
            {
                { "return item", MissionType.ReturnItem },
                { "kill person", MissionType.KillPerson },
                { "find person", MissionType.FindPerson },
                { "find item", MissionType.FindItem },
                { "repair", MissionType.Repair }
            };

        /// <summary>
        /// Whether there is a body for every type this server offers.
        /// </summary>
        public static bool Complete
        {
            get { return Openers.Count > 0 && Types.Values.All(t => Bodies.ContainsKey(t)); }
        }

        #endregion

        #region Writing one

        /// <summary>
        /// The whole assignment, and the one line the mission list shows.
        /// </summary>
        /// <remarks>
        /// The short line is the first twenty eight characters of the long one
        /// followed by an ellipsis, which is what every captured offer's
        /// ShortInfo is.
        /// </remarks>
        public static void Write(MissionOffer offer, Random random, out string shortInfo, out string info)
        {
            string body = Body(offer.Type, random);
            string opener = Openers.Count == 0 ? string.Empty : Openers[random.Next(Openers.Count)];
            string closer = Closers.Count == 0 ? string.Empty : Closers[random.Next(Closers.Count)];

            var sb = new StringBuilder();
            if (opener.Length > 0)
            {
                sb.Append(Fill(opener, offer)).Append(' ');
            }

            sb.Append(Fill(body, offer));
            if (closer.Length > 0)
            {
                sb.Append(' ').Append(closer);
            }

            info = sb.ToString();
            shortInfo = info.Length <= 31 ? info : info.Substring(0, 28) + "...";
        }

        private static string Body(MissionType type, Random random)
        {
            List<string> bodies;
            if (!Bodies.TryGetValue(type, out bodies) || bodies.Count == 0)
            {
                // Not a sentence this server made up: it says plainly that the
                // table has no body for this type rather than writing one.
                return "Assignment: " + type + ". No briefing is on file for this kind of work.";
            }

            return bodies[random.Next(bodies.Count)];
        }

        /// <summary>
        /// Puts the mission into the body's slots.
        /// </summary>
        private static string Fill(string text, MissionOffer offer)
        {
            return text.Replace("{item}", offer.Objective ?? "component")
                       .Replace("{name}", offer.Objective ?? "the target")
                       .Replace("{fixture}", offer.Objective ?? "machine")
                       .Replace("{place}", offer.Building ?? "the building")
                       .Replace("{playfield}", offer.PlayfieldName ?? "the area")
                       .Replace("{org}", "the department")
                       .Replace("{code}", offer.Code ?? string.Empty)
                       .Replace(
                           "{credits}",
                           offer.CashReward.ToString(CultureInfo.InvariantCulture))
                       .Replace(
                           "{xp}",
                           offer.ExperienceReward.ToString(CultureInfo.InvariantCulture));
        }

        #endregion
    }
}
