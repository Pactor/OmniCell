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
    using System.Linq;


    using SmokeLounge.AOtomation.Messaging.GameData;

    #endregion

    /// <summary>
    /// What a mission terminal answers with.
    /// </summary>
    /// <remarks>
    /// Every number in here is measured, and the places where the measurement
    /// does not reach are marked as such rather than filled with a guess. See
    /// Documentation/Missions.md for where each came from.
    /// </remarks>
    public static class MissionRoller
    {
        #region The shape of an answer

        /// <summary>
        /// A terminal offers five, and the client refuses more.
        /// </summary>
        public const int Offers = 5;

        /// <summary>
        /// 48 hours, in minutes, which is what every captured offer carries and
        /// what the assignment texts say.
        /// </summary>
        public const int TimeLimitMinutes = 2880;

        /// <summary>
        /// The mission's quality is the character's level times this.
        /// </summary>
        /// <remarks>
        /// Six of the eleven are measured and the rest are interpolated
        /// between them:
        ///
        ///   1   0.688   a level 48 character rolled QL 33 (20260926-135805)
        ///   2   0.737   interpolated
        ///   3   0.790   qlmap.json, levels 40 to 42 against QL 32 and 33
        ///   4   0.840   qlmap.json, levels 44 to 47 against QL 37 to 39
        ///   5   0.900   qlmap.json, level 40 against QL 36
        ///   6   1.000   qlmap.json level 39 / QL 39, and a level 30 character
        ///               rolling QL 30 twice (20260910-200346)
        ///   7   1.091   interpolated
        ///   8   1.191   interpolated
        ///   9   1.300   the same level 30 character rolled QL 39
        ///   10  1.516   interpolated
        ///   11  1.767   the same level 30 character rolled QL 53
        ///
        /// The interpolation is geometric between the measured neighbours,
        /// which is what the measured steps look like - each is a few percent
        /// on the last, and the step grows towards the top of the range.
        /// </remarks>
        private static readonly double[] QualityMultiplier =
        {
            0.688, 0.737, 0.790, 0.840, 0.900, 1.000, 1.091, 1.191, 1.300, 1.516, 1.767
        };

        /// <summary>
        /// The mixes measured at a setting, keyed by the six dimension bytes
        /// the server answered with.
        /// </summary>
        /// <remarks>
        /// The type function is not known - see Missions.md. What is known is
        /// the answer at seven settings, and this is them, so a server rolled
        /// at one of those settings gives the answer retail gives. Everywhere
        /// else the draw is random, which is what retail does when it picks the
        /// dimensions itself and is the honest answer where nothing was
        /// measured.
        ///
        /// Each entry is the type that comes up three times followed by the two
        /// singles. The key is "bad/chaos/hidden/myst/stealth/xp" as
        /// percentages.
        /// </remarks>
        private static readonly Dictionary<string, MissionType[]> Measured =
            new Dictionary<string, MissionType[]>
            {
                // Tools/Capture/MissionRolls.tsv, and the bot's 1,059 logged rolls.
                { "0/100/0/0/0/0", new[] { MissionType.FindItem, MissionType.KillPerson, MissionType.FindPerson } },
                { "0/100/0/50/50/0", new[] { MissionType.FindItem, MissionType.KillPerson, MissionType.FindPerson } },
                { "50/50/50/50/50/0", new[] { MissionType.FindPerson, MissionType.ReturnItem, MissionType.FindItem } },
                { "100/0/0/100/0/0", new[] { MissionType.KillPerson, MissionType.FindPerson, MissionType.ReturnItem } },
                { "0/100/100/0/100/100", new[] { MissionType.ReturnItem, MissionType.FindItem, MissionType.FindPerson } },
                { "100/0/0/0/0/0", new[] { MissionType.FindPerson, MissionType.KillPerson, MissionType.ReturnItem } },
                { "55/85/50/33/96/49", new[] { MissionType.FindItem, MissionType.KillPerson, MissionType.ReturnItem } },
                { "76/83/7/9/53/2", new[] { MissionType.KillPerson, MissionType.FindPerson, MissionType.Repair } },
                { "71/62/81/89/77/11", new[] { MissionType.Repair, MissionType.FindPerson, MissionType.ReturnItem } },
                { "37/47/43/26/80/88", new[] { MissionType.Repair, MissionType.KillPerson, MissionType.ReturnItem } }
            };

        /// <summary>
        /// The settings a mix has been measured at, for anything that wants to
        /// check the roller still answers them the way retail did.
        /// </summary>
        public static IEnumerable<KeyValuePair<string, MissionType[]>> MeasuredSettings
        {
            get { return Measured; }
        }

        /// <summary>
        /// Whether an originator is the team version of a booth.
        /// </summary>
        /// <remarks>
        /// IsTeamOriginator at GameData.dll 0x10002D23: the even values are
        /// the team version of the odd one below, all the way up. 0 is not an
        /// originator at all and the client drops a packet carrying it.
        /// </remarks>
        public static bool IsTeam(int originator)
        {
            return originator > 0 && originator % 2 == 0;
        }

        private static readonly MissionType[] AllTypes =
        {
            MissionType.ReturnItem, MissionType.KillPerson, MissionType.FindPerson,
            MissionType.FindItem, MissionType.Repair
        };

        #endregion

        #region Rolling

        /// <summary>
        /// Five missions, and the six dimensions they were drawn at.
        /// </summary>
        /// <param name="level">The character's level, which sets the quality.</param>
        /// <param name="playfield">The playfield the terminal stands in.</param>
        /// <param name="difficulty">1 to 11, as the client sent it.</param>
        /// <param name="asked">The six dimension bytes the client sent.</param>
        /// <param name="terminal">The terminal that was used.</param>
        /// <param name="team">
        /// Whether the terminal was asked for team missions, which the
        /// originator says and which decides the building's shape.
        /// </param>
        /// <param name="seed">The seed to answer with.</param>
        /// <param name="answered">
        /// The six dimensions the draw actually ran at, which are the ones
        /// asked for unless the client left every slider alone.
        /// </param>
        public static List<MissionOffer> Roll(
            int level,
            int difficulty,
            sbyte[] asked,
            Identity terminal,
            bool team,
            int playfield,
            string playfieldName,
            int seed,
            out sbyte[] answered)
        {
            var random = new Random(seed);
            answered = Dimensions(asked, random);

            int quality = Quality(Math.Max(1, level), difficulty);

            var offers = new List<MissionOffer>(Offers);
            foreach (MissionType type in Mix(answered, random))
            {
                offers.Add(
                    One(type, quality, difficulty, terminal, team, playfield, playfieldName, random));
            }

            return offers;
        }

        /// <summary>
        /// The dimensions the draw runs at.
        /// </summary>
        /// <remarks>
        /// With all six bytes zero - an untouched slider panel - retail draws
        /// them itself and answers with what it drew. Eight of the nine rolls
        /// read out of captures are like that, and the values that came back
        /// are spread across the range and land on percentages no slider notch
        /// produces. Anything else is used exactly as sent, which the other
        /// five rolls show.
        /// </remarks>
        private static sbyte[] Dimensions(sbyte[] asked, Random random)
        {
            if (asked != null && asked.Length == 6 && asked.Any(x => x != 0))
            {
                return (sbyte[])asked.Clone();
            }

            var drawn = new sbyte[6];
            for (int i = 0; i < 6; i++)
            {
                drawn[i] = (sbyte)(random.Next(-100, 101));
            }

            return drawn;
        }

        /// <summary>
        /// Which five types come up.
        /// </summary>
        /// <remarks>
        /// 1,053 of 1,059 logged rolls are three of one type and one each of
        /// two others, with the three first, so that is the shape. Which types
        /// is a measured lookup where there is a measurement and a draw where
        /// there is not.
        /// </remarks>
        private static IEnumerable<MissionType> Mix(sbyte[] dimensions, Random random)
        {
            MissionType[] measured;
            var chosen = new List<MissionType>(3);
            if (Measured.TryGetValue(Key(dimensions), out measured))
            {
                chosen.AddRange(measured);
            }
            else
            {
                List<MissionType> bag = AllTypes.OrderBy(x => random.Next()).ToList();
                chosen.AddRange(bag.Take(3));
            }

            yield return chosen[0];
            yield return chosen[0];
            yield return chosen[0];
            yield return chosen[1];
            yield return chosen[2];
        }

        /// <summary>
        /// The dimensions as the percentages Missions.md's tables are written
        /// in: the client maps a percentage to (percent - 50) * 2.
        /// </summary>
        private static string Key(sbyte[] dimensions)
        {
            return string.Join(
                "/",
                dimensions.Select(x => ((x / 2) + 50).ToString(CultureInfo.InvariantCulture)));
        }

        /// <summary>
        /// The mission's quality.
        /// </summary>
        public static int Quality(int level, int difficulty)
        {
            int index = Math.Min(QualityMultiplier.Length, Math.Max(1, difficulty)) - 1;
            return Math.Max(1, (int)Math.Round(level * QualityMultiplier[index]));
        }

        #endregion

        #region One mission

        private static MissionOffer One(
            MissionType type,
            int quality,
            int difficulty,
            Identity terminal,
            bool team,
            int playfield,
            string playfieldName,
            Random random)
        {
            var offer = new MissionOffer
                        {
                            Type = type,
                            Quality = quality,
                            Difficulty = difficulty,
                            Terminal = terminal,
                            Team = team,

                            // Eleven of the sixteen captured team buildings
                            // have three floors and five have four.
                            Floors = team ? (random.Next(16) < 11 ? 3 : 4) : 1,
                            Playfield = playfield,
                            Pool = Pool(random, team),
                            PlayfieldName = string.IsNullOrEmpty(playfieldName)
                                                ? "this area"
                                                : playfieldName,
                            Building = Building(random) + " building",
                            Code = Code(random),
                            ObjectiveCode = random.Next()
                        };

            offer.Building = offer.Building + " in " + offer.PlayfieldName;
            offer.Objective = MissionObjectives.Name(offer, random);
            Reward(offer, random);
            MissionText.Write(offer, random, out string shortInfo, out string info);
            offer.ShortInfo = shortInfo;
            offer.Info = info;
            return offer;
        }

        /// <summary>
        /// Which pool the building comes from.
        /// </summary>
        /// <remarks>
        /// At random, because 28 recorded runs rolled at one setting came back
        /// as seven different pools - so the dimensions do not pick it, and
        /// nothing measured says what does.
        /// </remarks>
        private static int Pool(Random random, bool team)
        {
            // A team building ends in a boss room on a floor of its own, and
            // one of the ten pools has no boss room in it at all - so a team
            // mission cannot be built there.
            List<int> pools = MissionPoolLoader.Pools
                .Where(p => !team || p.Value.Rooms.Any(r => r.Role == MissionRoomRole.BossRoom))
                .Select(p => p.Key)
                .ToList();
            return pools.Count == 0 ? 0 : pools[random.Next(pools.Count)];
        }

        /// <summary>
        /// Buildings are lettered, and the captured key names one "A building
        /// in Borealis".
        /// </summary>
        private static string Building(Random random)
        {
            return ((char)('A' + random.Next(6))).ToString(CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// The reference the assignment quotes itself by. Two captured shapes:
        /// digits in groups, and four letters.
        /// </summary>
        private static string Code(Random random)
        {
            if (random.Next(2) == 0)
            {
                var letters = new char[4];
                for (int i = 0; i < 4; i++) letters[i] = (char)('A' + random.Next(26));
                return new string(letters);
            }

            return random.Next(1000, 99999).ToString(CultureInfo.InvariantCulture) + "-"
                   + random.Next(100, 999).ToString(CultureInfo.InvariantCulture) + "-"
                   + random.Next(1, 9).ToString(CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// What the terminal pays.
        /// </summary>
        /// <remarks>
        /// This is the weakest thing on the page. 25 captured offers at four
        /// qualities give experience of roughly 910 at QL 30 and 33, 2,250 at
        /// 39 and 4,860 at 53, and credits that scatter from 2,500 to 13,300
        /// with no shape - which is what a credits/experience slider trading
        /// one against the other would do. So experience is interpolated
        /// between the measured qualities and credits are a spread around the
        /// measured mean, and neither is a formula anyone has read out of the
        /// client.
        /// </remarks>
        private static void Reward(MissionOffer offer, Random random)
        {
            double xp = Interpolate(offer.Quality, RewardQualities, RewardExperience);
            double cash = Interpolate(offer.Quality, RewardQualities, RewardCredits);

            // Roll to roll the captured offers vary by about a fifth.
            double jitter = 0.8 + (random.NextDouble() * 0.4);
            offer.ExperienceReward = Math.Max(1, (int)Math.Round(xp * jitter));
            offer.CashReward = Math.Max(1, (int)Math.Round(cash * (0.8 + (random.NextDouble() * 0.4))));

            offer.RewardLowId = 0;
            offer.RewardHighId = 0;
        }

        private static readonly double[] RewardQualities = { 30, 33, 39, 53 };

        private static readonly double[] RewardExperience = { 914, 910, 2249, 4857 };

        private static readonly double[] RewardCredits = { 5807, 4379, 4305, 9384 };

        private static double Interpolate(double at, double[] xs, double[] ys)
        {
            if (at <= xs[0]) return ys[0] * (at / xs[0]);
            for (int i = 1; i < xs.Length; i++)
            {
                if (at > xs[i]) continue;
                double t = (at - xs[i - 1]) / (xs[i] - xs[i - 1]);
                return ys[i - 1] + (t * (ys[i] - ys[i - 1]));
            }

            // Above the highest measured quality, carry the last slope on.
            int last = xs.Length - 1;
            double slope = (ys[last] - ys[last - 1]) / (xs[last] - xs[last - 1]);
            return ys[last] + ((at - xs[last]) * slope);
        }

        #endregion
    }
}
