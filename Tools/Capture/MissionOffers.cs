// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MissionOffers.cs" company="OmniCell">
//   Copyright © 2026 OmniCell contributors.
// </copyright>
// <summary>
//   Rolls missions the way a terminal would, and checks the packet survives the wire.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace OmniCell.Tools.Capture
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;

    using OmniCell.Core.Content;
    using OmniCell.Core.Missions;
    using OmniCell.Core.Playfields;
    using OmniCell.Core.Statels;

    using SmokeLounge.AOtomation.Messaging.GameData;
    using SmokeLounge.AOtomation.Messaging.Messages;
    using SmokeLounge.AOtomation.Messaging.Messages.N3Messages;
    using SmokeLounge.AOtomation.Messaging.Serialization;

    /// <summary>
    /// What the server would answer a mission terminal with.
    /// </summary>
    /// <remarks>
    /// The roll is the one part of missions a player reaches first, and a
    /// packet the client drops is indistinguishable from a server that never
    /// answered. So this builds the answer with the same code the zone engine
    /// uses, writes it through the message serializer, reads it back, and
    /// checks the things the client checks before it will show a list:
    ///
    ///   * the version is 4, and the client compares it against its own class
    ///     version through its vtable;
    ///   * the difficulty is inside 1 to 11 and the originator inside 1 to 8,
    ///     and the client drops the packet for either;
    ///   * every dimension byte is inside -100 to 100, which GameData's own
    ///     stream operator enforces;
    ///   * there are no more than five missions - "Number of quests = %u";
    ///   * and every field survives the round trip.
    ///
    ///     MissionOffers [rolls] [--pools missionpools.ocp] [--text missiontext.tsv]
    ///
    /// It needs the pools pack and the text table only to make the missions
    /// look like missions; without them it still checks the packet.
    /// </remarks>
    internal static class MissionOffers
    {
        private static int Main(string[] args)
        {
            int rolls = 200;
            string pools = "missionpools.ocp";
            string text = "missiontext.tsv";
            string playfields = null;

            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "--pools" && i + 1 < args.Length) pools = args[++i];
                else if (args[i] == "--text" && i + 1 < args.Length) text = args[++i];
                else if (args[i] == "--playfields" && i + 1 < args.Length) playfields = args[++i];
                else int.TryParse(args[i], NumberStyles.Integer, CultureInfo.InvariantCulture, out rolls);
            }

            Console.WriteLine(
                "pools: {0}",
                File.Exists(pools)
                    ? MissionPoolLoader.CacheAllMissionPools(pools) + " loaded"
                    : "not found at " + pools);
            Console.WriteLine(
                "text:  {0}",
                File.Exists(text) ? MissionText.Load(text) + " lines" : "not found at " + text);
            if (playfields != null) Terminals(playfields);
            Console.WriteLine();

            var serializer = new MessageSerializer();
            var random = new Random(20260926);
            var terminal = new Identity { Type = (IdentityType)56001, Instance = -1073741024 };

            int bad = 0;
            var types = new Dictionary<MissionType, int>();
            var shapes = new Dictionary<string, int>();
            var qualities = new List<int>();

            for (int roll = 0; roll < rolls; roll++)
            {
                int level = 1 + random.Next(220);
                int difficulty = 1 + random.Next(11);

                // Most rolls at an untouched panel, which is what a player who
                // has not moved a slider sends and what the server answers by
                // drawing the dimensions itself.
                var asked = new sbyte[6];
                if (random.Next(3) == 0)
                {
                    for (int i = 0; i < 6; i++) asked[i] = (sbyte)((random.Next(3) - 1) * 100);
                    if (asked.All(x => x == 0)) asked[0] = 100;
                }

                // One roll in four is a team booth, which is the even
                // originator and the building with floors.
                bool team = random.Next(4) == 0;

                sbyte[] answered;
                List<MissionOffer> offers = MissionRoller.Roll(
                    level, difficulty, asked, terminal, team, 800, "Borealis",
                    random.Next(1, int.MaxValue), out answered);

                foreach (MissionOffer offer in offers)
                {
                    if (offer.Team != team || (team ? offer.Floors < 3 || offer.Floors > 4 : offer.Floors != 1))
                    {
                        Console.WriteLine(
                            "  roll {0}: a {1} mission with {2} floors",
                            roll, team ? "team" : "solo", offer.Floors);
                        bad++;
                    }
                }

                for (int i = 0; i < offers.Count; i++) offers[i].Instance = (roll * 16) + i;

                string why = Check(serializer, offers, difficulty, answered, out QuestAlternativeMessage back);
                if (why != null)
                {
                    bad++;
                    if (bad <= 3) Console.WriteLine("  roll {0}: {1}", roll, why);
                    continue;
                }

                foreach (MissionOffer offer in offers)
                {
                    types.TryGetValue(offer.Type, out int seen);
                    types[offer.Type] = seen + 1;
                }

                qualities.Add(offers[0].Quality);
                string shape = string.Join(
                    "+",
                    offers.GroupBy(o => o.Type).Select(g => g.Count()).OrderByDescending(n => n));
                shapes.TryGetValue(shape, out int count);
                shapes[shape] = count + 1;

                if (roll == 0) Show(offers, difficulty, answered, back);
            }

            bad += Measured(terminal);

            Console.WriteLine();
            Console.WriteLine("{0} rolls, {1} refused or wrong", rolls, bad);
            Console.WriteLine();
            Console.WriteLine("  shape          {0}",
                string.Join("  ", shapes.OrderByDescending(x => x.Value).Select(x => x.Key + " x" + x.Value)));
            Console.WriteLine("  types          {0}",
                string.Join("  ", types.OrderByDescending(x => x.Value).Select(x => x.Key + " " + x.Value)));
            Console.WriteLine("  quality range  {0} to {1}", qualities.Min(), qualities.Max());
            Console.WriteLine();
            Console.WriteLine("retail: 1,053 of 1,059 logged rolls are 3+1+1, and the quality is the");
            Console.WriteLine("character's level times 0.688 at difficulty 1 and 1.767 at 11.");

            return bad == 0 ? 0 : 1;
        }

        /// <summary>
        /// Where a player could click to get a roll at all.
        /// </summary>
        /// <remarks>
        /// A handler that answers a terminal is no use without a terminal, and
        /// the terminals are statels in the playfield pack. They are named by
        /// identity type 56001, which the captures show and which the identity
        /// enum had wrong until 2026-09-26.
        /// </remarks>
        private static void Terminals(string pack)
        {
            if (!File.Exists(pack))
            {
                Console.WriteLine("playfields: not found at " + pack);
                return;
            }

            List<PlayfieldData> data = OmniCellContentPack.ReadPlayfields(pack);
            var found = new List<KeyValuePair<int, int>>();
            foreach (PlayfieldData playfield in data)
            {
                int count = playfield.Statels == null
                                ? 0
                                : playfield.Statels.Count(
                                    s => (int)s.Identity.Type == (int)IdentityType.MissionTerminal);
                if (count > 0) found.Add(new KeyValuePair<int, int>(playfield.PlayfieldId, count));
            }

            Console.WriteLine(
                "terminals: {0} over {1} playfields of {2}",
                found.Sum(x => x.Value), found.Count, data.Count);

            Console.WriteLine("  statel identity types in the pack:");
            foreach (var kind in data.SelectMany(p2 => p2.Statels ?? new List<StatelData>())
                         .GroupBy(s => (int)s.Identity.Type)
                         .OrderByDescending(g => g.Count())
                         .Take(8))
            {
                Console.WriteLine("    {0,-8} {1}", kind.Key, kind.Count());
            }
            foreach (KeyValuePair<int, int> one in found.OrderByDescending(x => x.Value).Take(8))
            {
                Console.WriteLine("    playfield {0,-6} {1}", one.Key, one.Value);
            }
        }

        /// <summary>
        /// Every setting a mix was measured at must still come back that way.
        /// </summary>
        /// <remarks>
        /// The type function is unknown, so the roller answers measured
        /// settings from a table and draws everywhere else. The table is the
        /// only part of it that can be wrong rather than merely unknown, so it
        /// is checked here every time.
        /// </remarks>
        private static int Measured(Identity terminal)
        {
            Console.WriteLine();
            Console.WriteLine("=== the settings a mix was measured at ===");

            int wrong = 0;
            foreach (KeyValuePair<string, MissionType[]> setting in MissionRoller.MeasuredSettings)
            {
                sbyte[] asked = setting.Key.Split('/')
                    .Select(x => (sbyte)((int.Parse(x, CultureInfo.InvariantCulture) - 50) * 2))
                    .ToArray();

                sbyte[] answered;
                List<MissionOffer> offers = MissionRoller.Roll(
                    40, 6, asked, terminal, false, 800, "Borealis", 1, out answered);

                MissionType[] got =
                {
                    offers[0].Type, offers[3].Type, offers[4].Type
                };

                bool ok = offers.Count(o => o.Type == setting.Value[0]) == 3
                          && got.SequenceEqual(setting.Value);
                if (!ok) wrong++;

                Console.WriteLine(
                    "  {0,-22} {1}  {2}",
                    setting.Key,
                    string.Join(", ", got),
                    ok ? "as measured" : "WRONG, measured " + string.Join(", ", setting.Value));
            }

            return wrong;
        }

        /// <summary>
        /// Everything the client refuses a roll for, and the round trip.
        /// </summary>
        private static string Check(
            MessageSerializer serializer,
            List<MissionOffer> offers,
            int difficulty,
            sbyte[] answered,
            out QuestAlternativeMessage back)
        {
            back = null;
            if (offers.Count > 5) return offers.Count + " missions, and the client refuses more than five";
            if (difficulty < 1 || difficulty > 11) return "difficulty " + difficulty + " is outside 1 to 11";
            foreach (sbyte dimension in answered)
            {
                if (dimension < -100 || dimension > 100)
                {
                    return "dimension " + dimension + " is outside -100 to 100";
                }
            }

            var body = new QuestAlternativeMessage
                       {
                           Identity = new Identity { Type = IdentityType.CanbeAffected, Instance = 1 },
                           Unknown = 0,
                           VersionId = 4,
                           Difficulty = (byte)difficulty,
                           GoodBad = (byte)answered[0],
                           ControlledLackingControl = (byte)answered[1],
                           OpenHidden = (byte)answered[2],
                           PhysicalMystical = (byte)answered[3],
                           ExplosivePatient = (byte)answered[4],
                           MoneyExperience = (byte)answered[5],
                           Seed = 1,
                           Originator = QuestOriginator.NeutralBooth,
                           MissionTerminalIdentity = offers[0].Terminal,
                           QuestInfos = offers.Select(
                               (o, i) => new QuestAlternativeEntry
                                         {
                                             Quest = MissionWire.Info(o, Identity.None),
                                             Trailer = (byte)i
                                         }).ToArray()
                       };

            byte[] bytes;
            try
            {
                using (var stream = new MemoryStream())
                {
                    serializer.Serialize(stream, new Message { Body = body, Header = Header(body) });
                    bytes = stream.ToArray();
                }
            }
            catch (Exception exception)
            {
                return "would not serialise: " + exception.Message;
            }

            try
            {
                using (var stream = new MemoryStream(bytes))
                {
                    back = serializer.Deserialize(stream).Body as QuestAlternativeMessage;
                }
            }
            catch (Exception exception)
            {
                return "would not read back: " + exception.Message;
            }

            if (back == null) return "read back as nothing";
            if (back.QuestInfos.Length != offers.Count)
            {
                return "wrote " + offers.Count + " missions and read back " + back.QuestInfos.Length;
            }

            for (int i = 0; i < offers.Count; i++)
            {
                QuestInfo wrote = body.QuestInfos[i].Quest;
                QuestInfo read = back.QuestInfos[i].Quest;
                if (read.Info != wrote.Info) return "mission " + i + "'s text did not survive";
                if (read.MissionIconId != wrote.MissionIconId) return "mission " + i + "'s type did not survive";
                if (read.Quality != wrote.Quality) return "mission " + i + "'s quality did not survive";
                if (read.CashReward != wrote.CashReward) return "mission " + i + "'s reward did not survive";
                if (read.QuestActions.Length != 1) return "mission " + i + " lost its action";
                if (back.QuestInfos[i].Trailer != i) return "mission " + i + " lost its trailer byte";
            }

            return null;
        }

        private static Header Header(MessageBody body)
        {
            return new Header
                   {
                       MessageId = 1,
                       PacketType = body.PacketType,
                       Unknown = 0x0001,
                       Sender = 0x0FDF,
                       Receiver = 1
                   };
        }

        private static void Show(
            List<MissionOffer> offers,
            int difficulty,
            sbyte[] answered,
            QuestAlternativeMessage back)
        {
            Console.WriteLine(
                "one roll, difficulty {0}, dimensions {1}:",
                difficulty,
                string.Join("/", answered.Select(x => (x / 2) + 50)));
            Console.WriteLine();
            foreach (QuestAlternativeEntry entry in back.QuestInfos)
            {
                QuestInfo info = entry.Quest;
                Console.WriteLine(
                    "  {0,-12} QL {1,-4} {2,7} credits {3,7} xp   {4}",
                    Enum.IsDefined(typeof(MissionType), info.MissionIconId)
                        ? ((MissionType)info.MissionIconId).ToString()
                        : info.MissionIconId.ToString(CultureInfo.InvariantCulture),
                    info.Quality,
                    info.CashReward,
                    info.ExperienceReward,
                    info.ShortInfo);
            }

            Console.WriteLine();
            Console.WriteLine("  \"{0}\"", back.QuestInfos[0].Quest.Info);
            Console.WriteLine();
        }
    }
}
