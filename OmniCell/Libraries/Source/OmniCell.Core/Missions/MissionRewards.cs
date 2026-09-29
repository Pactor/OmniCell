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

    #endregion

    /// <summary>
    /// The item a mission is offered with.
    /// </summary>
    /// <remarks>
    /// Every offer retail makes carries one. All 25 offers across the captures
    /// have <c>ItemRewards</c> of length one, and an offer built without it -
    /// which is what OmniCell sent until now - has a shape the client does not
    /// answer: the terminal window stays empty even though the server rolled
    /// and replied.
    ///
    /// **A band is a pair of templates.** An item family is the pack's own
    /// <c>Relations</c> list; sorted by quality and taken two at a time it
    /// gives the (low, high) pairs the wire carries. The Mini Axe family runs
    /// 1-20 Unbalanced, 21-40 Cast-Off, 41-60 Loose, 61-80 Blunt, and a QL 30
    /// mission offered the Cast-Off pair.
    ///
    /// **The quality is the mission's, clamped into the band.** That is why
    /// the reward quality is usually the mission quality and sometimes is not:
    /// a family of one template has only the quality it has, so a QL 39
    /// mission was seen handing out a QL 30 Nano Crystal (Quell Anger) and a
    /// QL 1 Trenchcoat. Band and quality together reproduce all 25 captured
    /// rewards exactly.
    ///
    /// **Which items are rollable is measured, not derived.** Nothing in the
    /// pack says. The pool is what the AOBuddy10 bot recorded being offered
    /// over thousands of rolls, in <c>Datafiles/missionrewards.tsv</c>.
    ///
    /// What is **not** settled, and is therefore arbitrary here: which of the
    /// eligible items a given roll picks. Retail's choice is not derivable
    /// from anything we hold, so this takes one at random from the bands whose
    /// range covers the mission, weighted by nothing. It is a real item,
    /// offered at a quality retail would offer it at, and the choice is ours.
    /// </remarks>
    public static class MissionRewards
    {
        /// <summary>
        /// One (low, high) template pair and the quality range it covers.
        /// </summary>
        public class Band
        {
            public int LowId { get; set; }

            public int HighId { get; set; }

            public int LowQuality { get; set; }

            public int HighQuality { get; set; }

            /// <summary>
            /// Whether a mission of this quality can be offered this band.
            /// </summary>
            /// <remarks>
            /// A band that spans the quality covers it outright. A family of
            /// one template cannot move, so it is offered near its own
            /// quality instead - the captures have a QL 30 crystal in a QL 39
            /// mission, and arpa3's rollability notes put nano crystals
            /// within ten either way. <see cref="FixedReach"/> is that ten.
            /// A QL 1 Trenchcoat in a QL 39 mission says the reach is wider
            /// for some things, so this is a floor on what is offered rather
            /// than a claim about what retail allows.
            /// </remarks>
            public bool Covers(int quality)
            {
                if (quality >= this.LowQuality && quality <= this.HighQuality)
                {
                    return true;
                }

                return this.LowId == this.HighId
                       && Math.Abs(this.LowQuality - quality) <= FixedReach;
            }

            /// <summary>
            /// The quality this band is offered at for that mission.
            /// </summary>
            public int Quality(int quality)
            {
                return Math.Min(Math.Max(quality, this.LowQuality), this.HighQuality);
            }
        }

        /// <summary>
        /// How far from its own quality a single-template item is offered.
        /// </summary>
        private const int FixedReach = 10;

        private static readonly List<Band> All = new List<Band>();

        private static readonly Dictionary<int, List<Band>> ByQuality = new Dictionary<int, List<Band>>();

        /// <summary>
        /// How many bands are loaded.
        /// </summary>
        public static int Count
        {
            get
            {
                return All.Count;
            }
        }

        /// <summary>
        /// Reads the pool. Missions still roll without it, just without a
        /// reward, which is what the client will not display.
        /// </summary>
        public static int Load()
        {
            return Load("missionrewards.tsv");
        }

        public static int Load(string filename)
        {
            All.Clear();
            ByQuality.Clear();

            string path = filename;
            if (!Path.IsPathRooted(path) && !File.Exists(path))
            {
                path = Path.Combine(AppContext.BaseDirectory, filename);
            }

            if (!File.Exists(path))
            {
                return 0;
            }

            foreach (string line in File.ReadLines(path))
            {
                if (line.Length == 0 || line[0] == '#')
                {
                    continue;
                }

                string[] parts = line.Split('\t');
                if (parts.Length < 4)
                {
                    continue;
                }

                int low, high, lowQuality, highQuality;
                if (!int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out low)
                    || !int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out high)
                    || !int.TryParse(parts[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out lowQuality)
                    || !int.TryParse(parts[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out highQuality))
                {
                    continue;
                }

                All.Add(
                    new Band
                        {
                            LowId = low,
                            HighId = high,
                            LowQuality = lowQuality,
                            HighQuality = highQuality,
                        });
            }

            return All.Count;
        }

        /// <summary>
        /// An item to offer a mission of this quality with, or null.
        /// </summary>
        public static Band Pick(int quality, Random random)
        {
            if (All.Count == 0)
            {
                return null;
            }

            List<Band> eligible;
            lock (ByQuality)
            {
                if (!ByQuality.TryGetValue(quality, out eligible))
                {
                    eligible = All.Where(b => b.Covers(quality)).ToList();
                    ByQuality[quality] = eligible;
                }
            }

            if (eligible.Count == 0)
            {
                // Above the top of Rubi-Ka. Only Rubi-Ka items are rollable -
                // no Shadowlands - and Rubi-Ka stops at quality 200, so a
                // mission above that still pays out, in the best thing there
                // is. Every captured offer carries a reward and an offer
                // without one is not displayed at all, so this hands over the
                // highest band at or below the quality rather than nothing,
                // and the clamp offers it at its own top.
                int best = All.Where(b => b.HighQuality <= quality)
                    .Select(b => b.HighQuality)
                    .DefaultIfEmpty(0)
                    .Max();
                if (best == 0)
                {
                    return null;
                }

                List<Band> top = All.Where(b => b.HighQuality == best).ToList();
                lock (ByQuality)
                {
                    ByQuality[quality] = top;
                }

                eligible = top;
            }

            return eligible[(random ?? new Random()).Next(eligible.Count)];
        }
    }
}
