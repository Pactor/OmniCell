#region License

// Copyright (c) 2005-2014, CellAO Team
// Copyright (c) 2026, OmniCell contributors
//
// This file is part of OmniCell and is distributed under the terms the project
// is licensed under. See the repository root for details.

#endregion

namespace ZoneEngine.Core
{
    #region Usings

    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Xml.Linq;

    using OmniCell.Core.Entities;
    using OmniCell.Enums;

    using ZoneEngine.Core.MessageHandlers;

    #endregion

    /// <summary>
    /// Gaining levels.
    /// </summary>
    /// <remarks>
    /// Nothing checked experience against anything. It was added to a stat by
    /// tradeskilling and, since last night, by handing a quest in, and there it
    /// sat - no level was ever gained, and NewLevel, which the live server sends
    /// when one is, had no code behind it.
    ///
    /// The thresholds come from those captured NewLevel messages, which carry
    /// the experience needed for the level just reached and for the next one.
    /// They chain correctly across seven captures, which is what makes them
    /// thresholds rather than guesses. The proof-of-concept table continues
    /// through level 10 using OmniCell's canonical Rubi-Ka XP table.
    ///
    /// Past the last known threshold a character stops levelling and the server
    /// says so, once. Extending an experience curve by eye is how a server ends
    /// up subtly not being the game it is imitating.
    /// </remarks>
    public static class Leveling
    {
        #region Fields

        /// <summary>
        /// Experience needed for each level, by level.
        /// </summary>
        private static readonly SortedDictionary<int, int> Thresholds = new SortedDictionary<int, int>();

        /// <summary>
        /// Characters already told that the table has run out.
        /// </summary>
        private static readonly HashSet<int> WarnedAboutTheEnd = new HashSet<int>();

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Reads the experience table. Called once, at startup.
        /// </summary>
        /// <returns>
        /// How many levels are known.
        /// </returns>
        public static int Load()
        {
            Thresholds.Clear();

            string path = Path.Combine(Directory.GetCurrentDirectory(), "XML Data", "Levels.xml");
            if (!File.Exists(path))
            {
                Console.WriteLine("No XML Data\\Levels.xml, so nobody will gain a level.");
                return 0;
            }

            foreach (XElement level in XDocument.Load(path).Descendants("Level"))
            {
                int id;
                int experience;
                if (int.TryParse((string)level.Attribute("id"), out id)
                    && int.TryParse((string)level.Attribute("experience"), out experience))
                {
                    Thresholds[id] = experience;
                }
            }

            return Thresholds.Count;
        }

        /// <summary>
        /// The highest level the table knows about.
        /// </summary>
        public static int HighestKnownLevel
        {
            get
            {
                return Thresholds.Count == 0 ? 1 : Thresholds.Keys.Max();
            }
        }

        /// <summary>
        /// Levels a character up if it has earned it. Called once per heartbeat.
        /// </summary>
        public static void Tick(ICharacter character)
        {
            if (character == null || Thresholds.Count == 0)
            {
                return;
            }

            int level = character.Stats[StatIds.level].Value;
            int experience = character.Stats[StatIds.xp].Value;

            int next = level + 1;
            if (!Thresholds.ContainsKey(next))
            {
                if (level >= HighestKnownLevel && !WarnedAboutTheEnd.Contains(character.Identity.Instance))
                {
                    WarnedAboutTheEnd.Add(character.Identity.Instance);
                    Console.WriteLine(
                        "Character " + character.Name + " is level " + level
                        + " and the experience table stops there. Capture a NewLevel message past it to go further.");
                }

                return;
            }

            if (experience < Thresholds[next])
            {
                return;
            }

            character.Stats[StatIds.level].Value = next;
            character.SendChangedStats();

            NewLevelMessageHandler.Default.Send(
                character,
                next,
                experience,
                Thresholds[next],
                Thresholds.ContainsKey(next + 1) ? Thresholds[next + 1] : Thresholds[next]);
        }

        /// <summary>
        /// Forgets that a character was told the table had run out.
        /// </summary>
        public static void Forget(ICharacter character)
        {
            WarnedAboutTheEnd.Remove(character.Identity.Instance);
        }

        #endregion
    }
}
