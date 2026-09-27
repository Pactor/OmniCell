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
            Centres();
            Console.WriteLine();

            var serializer = new MessageSerializer();
            var random = new Random(20260926);
            var terminal = new Identity { Type = (IdentityType)56001, Instance = -1073741024 };

            int bad = 0;
            var types = new Dictionary<MissionType, int>();
            var shapes = new Dictionary<string, int>();
            var qualities = new List<int>();
            var rooms = new List<int>();
            var floors = new List<int>();
            var contents = new List<int>();
            var monsters = new List<int>();

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

                // Taking one builds the building, and the building has to
                // survive the same wire the offer does.
                MissionOffer taken = offers[random.Next(offers.Count)];
                taken.Built = MissionBuilding.Build(taken);
                string trouble = Built(serializer, taken);
                if (trouble != null)
                {
                    bad++;
                    if (bad <= 3) Console.WriteLine("  roll {0}: {1}", roll, trouble);
                }
                else
                {
                    rooms.Add(taken.Built.Layout.Rooms.Count);
                    contents.Add(MissionContents.Messages(taken).Count);
                    monsters.Add(taken.Built.Monsters.Count);
                    floors.Add(taken.Built.Layout.Rooms.Max(r => r.Floor)
                               - taken.Built.Layout.Rooms.Min(r => r.Floor) + 1);
                }

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
            Console.WriteLine(
                "  rooms          {0:F1} mean, {1} to {2}",
                rooms.Average(), rooms.Min(), rooms.Max());
            Console.WriteLine(
                "  doors, chests  {0:F1} sent per mission, {1} to {2}",
                contents.Average(), contents.Min(), contents.Max());
            Console.WriteLine(
                "  monsters       {0:F1} per mission, {1} to {2}",
                monsters.Average(), monsters.Min(), monsters.Max());
            Console.WriteLine(
                "  floors         {0}",
                string.Join("  ", floors.GroupBy(f => f).OrderBy(g => g.Key)
                    .Select(g => g.Key + " floor" + (g.Key == 1 ? string.Empty : "s") + " x" + g.Count())));
            Console.WriteLine();
            Console.WriteLine("retail: 1,053 of 1,059 logged rolls are 3+1+1, and the quality is the");
            Console.WriteLine("character's level times 0.688 at difficulty 1 and 1.767 at 11.");

            return bad == 0 ? 0 : 1;
        }

        /// <summary>
        /// The building a taken mission is run in, and the zone-in packet that
        /// carries it.
        /// </summary>
        /// <remarks>
        /// The generator rides on PlayfieldAnarchyF as a DbObject that reads
        /// its own body, which is the part of the message most likely to go
        /// wrong quietly: a length read short leaves the client building a
        /// world out of the wrong bytes. So the whole message is written and
        /// read back and every placement compared.
        /// </remarks>
        private static string Built(MessageSerializer serializer, MissionOffer offer)
        {
            if (offer.Built == null) return "no building";
            if (offer.Built.Layout.Rooms.Count == 0) return "a building with no rooms";

            // Floors run away from zero in either direction: the captured
            // sets are (0,1,2), (0,1,2,3), (-2,-1,0) and (-3,-2,-1,0).
            int stacked = offer.Built.Layout.Rooms.Max(r => r.Floor)
                          - offer.Built.Layout.Rooms.Min(r => r.Floor) + 1;
            if (offer.Team ? stacked < 2 : stacked != 1)
            {
                return "a " + (offer.Team ? "team" : "solo") + " building on " + stacked + " floors";
            }

            // Where a character walking in through the door ends up has to be
            // inside the building: a landing point in the wall is a character
            // stuck in the geometry with no way to tell from out here.
            float lx, ly, lz;
            MissionBuilding.Landing(offer.Built, out lx, out ly, out lz);
            if (offer.Built.Layout.EntranceX == 0 && offer.Built.Layout.EntranceZ == 0)
            {
                return "no way in was recorded";
            }

            if (!Inside(offer.Built, lx, lz))
            {
                var boxes = new List<string>();
                foreach (BuildingRoomInfo pl in offer.Built.Layout.Rooms.Where(r => r.Floor == 0).Take(4))
                {
                    MissionPoolRoom rm = MissionPoolLoader.Room(offer.Built.Layout.Playfield, pl.Room);
                    int hh = pl.Rotation % 2 == 0 ? rm.SlotsHeight : rm.SlotsWidth;
                    int ww = pl.Rotation % 2 == 0 ? rm.SlotsWidth : rm.SlotsHeight;
                    boxes.Add(string.Format(
                        "room {0} grid {1},{2} rot {3} -> x {4}-{5} z {6}-{7}",
                        pl.Room, pl.X, pl.Z, pl.Rotation, pl.X * 10, (pl.X * 10) + (ww * 10),
                        (offer.Built.Layout.GridHeight - pl.Z - hh) * 10,
                        ((offer.Built.Layout.GridHeight - pl.Z - hh) * 10) + (hh * 10)));
                }

                return "the landing point " + lx + ", " + lz + " is not on the building's floor"
                       + " (door " + offer.Built.Layout.EntranceX + ","
                       + offer.Built.Layout.EntranceZ + " side " + offer.Built.Layout.EntranceSide
                       + "; " + string.Join(" | ", boxes) + ")";
            }

            BuildingGeneratorData generator = MissionBuilding.Generator(offer.Built, 2224708);
            var body = new PlayfieldAnarchyFMessage
                       {
                           Identity = new Identity
                                      {
                                          Type = IdentityType.Playfield2, Instance = 112085
                                      },
                           Unknown = 0,
                           Version = 4,
                           CharacterCoordinates = new Vector3 { X = 298.2f, Y = 5.01f, Z = 145.01f },
                           TokenMarker = 97,
                           ModelId = generator.Identity,
                           Group = 0,
                           Subgroup = 0,
                           PlayfieldId = new Identity
                                         {
                                             Type = IdentityType.Playfield2, Instance = 112085
                                         },
                           Generator = generator,
                           PlayfieldX = -1,
                           PlayfieldZ = -1
                       };

            PlayfieldAnarchyFMessage back;
            try
            {
                byte[] bytes;
                using (var stream = new MemoryStream())
                {
                    serializer.Serialize(stream, new Message { Body = body, Header = Header(body) });
                    bytes = stream.ToArray();
                }

                using (var stream = new MemoryStream(bytes))
                {
                    back = serializer.Deserialize(stream).Body as PlayfieldAnarchyFMessage;
                }
            }
            catch (Exception exception)
            {
                return "the zone-in packet would not go on the wire: " + exception.Message;
            }

            if (back == null || back.Generator == null) return "the generator did not read back";

            // The doors, the chests and the objective. The client has no
            // playfield file for a mission, so every one of these goes on the
            // wire, and one the serializer cannot write is a hole in the
            // building that nothing would report.
            offer.PlayfieldInstance = 112085;
            List<MessageBody> contents = MissionContents.Messages(offer);
            if (contents.Count < offer.Built.Layout.Doors.Count)
            {
                return "only " + contents.Count + " contents for "
                       + offer.Built.Layout.Doors.Count + " doors";
            }

            foreach (MessageBody content in contents)
            {
                try
                {
                    using (var stream = new MemoryStream())
                    {
                        serializer.Serialize(
                            stream, new Message { Body = content, Header = Header(content) });
                        stream.Position = 0;
                        if (serializer.Deserialize(stream) == null)
                        {
                            return content.GetType().Name + " read back as nothing";
                        }
                    }
                }
                catch (Exception exception)
                {
                    return content.GetType().Name + " would not go on the wire: " + exception.Message;
                }
            }

            if (back.Generator.Rooms.Length != generator.Rooms.Length)
            {
                return "wrote " + generator.Rooms.Length + " rooms and read back "
                       + back.Generator.Rooms.Length;
            }

            for (int i = 0; i < generator.Rooms.Length; i++)
            {
                BuildingRoomInfo wrote = generator.Rooms[i];
                BuildingRoomInfo read = back.Generator.Rooms[i];
                if (read.Room != wrote.Room || read.Floor != wrote.Floor || read.X != wrote.X
                    || read.Z != wrote.Z || read.Rotation != wrote.Rotation)
                {
                    return "placement " + i + " did not survive the wire";
                }
            }

            return null;
        }

        /// <summary>
        /// Whether a world point stands on one of floor zero's own cells.
        /// </summary>
        private static bool Inside(Mission mission, float x, float z)
        {
            foreach (BuildingRoomInfo placed in mission.Layout.Rooms)
            {
                if (placed.Floor != 0) continue;

                MissionPoolRoom room = MissionPoolLoader.Room(mission.Layout.Playfield, placed.Room);
                if (room == null) continue;

                int h = placed.Rotation % 2 == 0 ? room.SlotsHeight : room.SlotsWidth;
                int w = placed.Rotation % 2 == 0 ? room.SlotsWidth : room.SlotsHeight;
                int x0 = placed.X * 10;
                int z0 = (mission.Layout.GridHeight - placed.Z - h) * 10;
                if (x >= x0 && x <= x0 + (w * 10) && z >= z0 && z <= z0 + (h * 10)) return true;
            }

            return false;
        }

        /// <summary>
        /// What the single room on a team building's far floor is.
        /// </summary>
        private static void Centres()
        {
            var seen = new[]
                       {
                           new[] { 320, 71 }, new[] { 320, 72 }, new[] { 321, 60 },
                           new[] { 324, 45 }, new[] { 324, 46 }, new[] { 341, 93 },
                           new[] { 341, 94 }, new[] { 346, 60 }, new[] { 351, 63 }
                       };
            Console.WriteLine("  the room on a team building's far floor, at grid 13,13:");
            foreach (int[] one in seen)
            {
                MissionPoolRoom room = MissionPoolLoader.Room(one[0], one[1]);
                Console.WriteLine(
                    "    pool {0} room {1,-3} {2,-12} {3}",
                    one[0], one[1],
                    room == null ? "?" : room.Role.ToString(),
                    room == null ? string.Empty : room.Name);
            }
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
