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
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Xml.Linq;

    using OmniCell.Core.Entities;
    using OmniCell.Enums;
    using OmniCell.Stats.SpecialStats;

    using ZoneEngine.Core.Controllers;

    #endregion

    /// <summary>
    /// Experience for killing a creature.
    /// </summary>
    /// <remarks>
    /// Nothing gave any. A kill added to no stat, so the only experience in the
    /// game came from quests and tradeskills.
    ///
    /// What a kill is worth, and why, is written up in XML Data\Experience.xml:
    /// a share of the player's cap - a tenth of what the player's level needs -
    /// that grows with how far above the player the creature is, and nothing
    /// at all for a creature grey to that player.
    ///
    /// What goes over the wire follows the live server (newchar_s20 seq 726-738,
    /// 10858-10861): after the death, a NewLevel only if the kill levelled the
    /// player - carrying this kill's experience - and then the player's new
    /// experience total as an ordinary stat, before the corpse. The live server
    /// sends the total, not the amount, and no "you received" line.
    /// </remarks>
    public static class Experience
    {
        #region Fields

        /// <summary>
        /// The share rows, by the player level each was fitted at.
        /// </summary>
        private static readonly SortedDictionary<int, Share> Shares = new SortedDictionary<int, Share>();

        /// <summary>
        /// How far below a player a creature can be before it is grey, from the level each range
        /// starts at.
        /// </summary>
        private static readonly SortedDictionary<int, int> KillRanges = new SortedDictionary<int, int>();

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Reads the experience table. Called once, at startup.
        /// </summary>
        /// <returns>
        /// How many player-level rows are known.
        /// </returns>
        public static int Load()
        {
            Shares.Clear();
            KillRanges.Clear();

            string path = Path.Combine(Directory.GetCurrentDirectory(), "XML Data", "Experience.xml");
            if (!File.Exists(path))
            {
                Console.WriteLine("No XML Data\\Experience.xml, so kills give no experience.");
                return 0;
            }

            XDocument document = XDocument.Load(path);
            foreach (XElement row in document.Descendants("PlayerLevel"))
            {
                int level;
                int shareBase;
                int step;
                int fullAt;
                if (TryInt(row, "level", out level)
                    && TryInt(row, "base", out shareBase)
                    && TryInt(row, "step", out step)
                    && TryInt(row, "fullAt", out fullAt))
                {
                    Shares[level] = new Share(shareBase, step, fullAt);
                }
            }

            foreach (XElement entry in document.Descendants("KillRange"))
            {
                int level;
                int range;
                if (TryInt(entry, "level", out level) && TryInt(entry, "range", out range))
                {
                    KillRanges[level] = range;
                }
            }

            return Shares.Count;
        }

        /// <summary>
        /// How many levels below a player of this level a creature can be and still give experience.
        /// </summary>
        public static int KillRange(int level)
        {
            int range = KillRanges.Count == 0 ? 4 : KillRanges.First().Value;
            foreach (KeyValuePair<int, int> entry in KillRanges)
            {
                if (entry.Key > level)
                {
                    break;
                }

                range = entry.Value;
            }

            return range;
        }

        /// <summary>
        /// Whether a creature is grey - worth nothing - to a player of this level.
        /// </summary>
        public static bool IsGrey(int playerLevel, int mobLevel)
        {
            return mobLevel < playerLevel - KillRange(playerLevel);
        }

        /// <summary>
        /// The most one kill can give a player of this level: a tenth of what the level needs.
        /// </summary>
        public static int Cap(int level)
        {
            int row = level - 1;
            if (row < 0 || row >= XPTable.TableRKXP.GetLength(0))
            {
                return 0;
            }

            return (int)(XPTable.TableRKXP[row, 2] / 10);
        }

        /// <summary>
        /// The share of the cap, in thousandths, a creature of mobLevel is worth to a player of
        /// playerLevel.
        /// </summary>
        public static int ShareOf(int playerLevel, int mobLevel)
        {
            Share share = ShareRow(playerLevel);
            if (share == null)
            {
                return 0;
            }

            int above = mobLevel - playerLevel;
            if (above >= share.FullAt)
            {
                return 1000;
            }

            long value = share.Base + ((long)share.Step * above);
            return (int)Math.Max(0, Math.Min(1000, value));
        }

        /// <summary>
        /// What killing a creature of mobLevel is worth to a player of playerLevel.
        /// </summary>
        public static int For(int playerLevel, int mobLevel)
        {
            if (playerLevel < 1 || mobLevel < 1 || IsGrey(playerLevel, mobLevel))
            {
                return 0;
            }

            return (int)((long)Cap(playerLevel) * ShareOf(playerLevel, mobLevel) / 1000);
        }

        /// <summary>
        /// Gives the killer the experience a kill is worth.
        /// </summary>
        /// <remarks>
        /// Only players earn it, and only for creatures. Teams share nothing yet because the server
        /// has no teams; when it does, each member is judged on their own level - grey to one
        /// member is not grey to another - and the captured two-player team got its full amount
        /// each.
        /// </remarks>
        public static void OnKill(ICharacter killer, ICharacter victim)
        {
            if (killer == null
                || victim == null
                || ReferenceEquals(killer, victim)
                || !(killer.Controller is PlayerController)
                || victim.Controller is PlayerController)
            {
                return;
            }

            int award = For(killer.Stats[StatIds.level].Value, victim.Stats[StatIds.level].Value);
            if (award <= 0)
            {
                return;
            }

            int before = killer.Stats[StatIds.xp].Value;
            killer.Stats[StatIds.xp].Value = before > int.MaxValue - award ? int.MaxValue : before + award;

            // A level first, then the new total: the order the live server sends them in.
            Leveling.Tick(killer, award);
            killer.SendChangedStats();
        }

        #endregion

        #region Methods

        /// <summary>
        /// The share row for a player level: a row fitted at that level, a straight line between the
        /// rows either side of it, or the last row for every level past it.
        /// </summary>
        private static Share ShareRow(int level)
        {
            if (Shares.Count == 0)
            {
                return null;
            }

            Share exact;
            if (Shares.TryGetValue(level, out exact))
            {
                return exact;
            }

            KeyValuePair<int, Share>[] rows = Shares.ToArray();
            if (level < rows[0].Key)
            {
                return rows[0].Value;
            }

            if (level > rows[rows.Length - 1].Key)
            {
                return rows[rows.Length - 1].Value;
            }

            KeyValuePair<int, Share> below = rows.Last(r => r.Key < level);
            KeyValuePair<int, Share> above = rows.First(r => r.Key > level);
            int span = above.Key - below.Key;
            int into = level - below.Key;
            return new Share(
                below.Value.Base + ((above.Value.Base - below.Value.Base) * into / span),
                below.Value.Step + ((above.Value.Step - below.Value.Step) * into / span),
                (int)Math.Round(below.Value.FullAt + ((above.Value.FullAt - below.Value.FullAt) * (double)into / span)));
        }

        private static bool TryInt(XElement element, string attribute, out int value)
        {
            return int.TryParse(
                (string)element.Attribute(attribute),
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out value);
        }

        #endregion

        /// <summary>
        /// One share row: thousandths of the cap at equal level, what each level above adds, and
        /// how many levels above gives the whole cap.
        /// </summary>
        private sealed class Share
        {
            public readonly int Base;

            public readonly int Step;

            public readonly int FullAt;

            public Share(int shareBase, int step, int fullAt)
            {
                this.Base = shareBase;
                this.Step = step;
                this.FullAt = fullAt;
            }
        }
    }
}
