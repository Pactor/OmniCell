// --------------------------------------------------------------------------------------------------------------------
// <copyright file="WorldMobs.cs" company="OmniCell">
//   Copyright © 2026 OmniCell contributors.
// </copyright>
// <summary>
//   Every creature the retail server described in the captures: its body, its size, its health.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace OmniCell.Tools.Capture
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Text;

    using ICSharpCode.SharpZipLib.Zip.Compression;

    using SmokeLounge.AOtomation.Messaging.Messages;
    using SmokeLounge.AOtomation.Messaging.Messages.N3Messages;
    using SmokeLounge.AOtomation.Messaging.Serialization;

    /// <summary>
    /// What a creature looks like, read off the wire rather than guessed at.
    /// </summary>
    /// <remarks>
    /// The AOBuddy10 bot has walked 366 playfields and written down where it
    /// saw each named creature, which is most of a world to populate - but it
    /// records a name, a level and a place, and nothing about the body. A
    /// spawn with no body is a creature that cannot be drawn.
    ///
    /// The captures do carry it. Every
    /// <see cref="SimpleCharFullUpdateMessage"/> the retail server sent names
    /// the character and describes it: MonsterData is the body, and the head
    /// mesh, the scale, the visual flags and the run speed go with it. This
    /// walks every capture and writes one row per creature, which
    /// <see cref="WorldSpawns"/> then joins to the bot's places by name.
    ///
    ///     WorldMobs &lt;output.tsv&gt; &lt;capture directory&gt; [more...]
    ///
    /// Two things it will not write down.
    ///
    /// A character belonging to a player - a pet - is skipped on PetMaster,
    /// which carries the owner's dynel. Not on the IsPet flag: that bit reads
    /// set on ordinary creatures too.
    ///
    /// A player is skipped on MonsterData being zero, which is the body a
    /// person wears. That also drops the handful of NPCs built on a player
    /// body, and they are better taken from a capture of their own playfield
    /// by AreaExtract, which keeps their meshes and textures too.
    ///
    /// The health is kept as the scale in <c>MissionCreature.Health</c>, not
    /// as health over level: the health ramp does not pass through the origin,
    /// so a rate per level only holds at the level it was measured at.
    /// </remarks>
    internal static class WorldMobs
    {
        private const int HeaderLength = 16;

        private const int SizeOffset = 6;

        /// <summary>
        /// One named creature, as the captures have it.
        /// </summary>
        private sealed class Creature
        {
            public string Name;

            public uint Monster;

            public int HeadMesh;

            public int Scale;

            public int Visual;

            public int RunSpeed;

            public int Seen;

            public int LowLevel = int.MaxValue;

            public int HighLevel = int.MinValue;

            public double Scaled;

            public int ScaledSeen;

            /// <summary>
            /// Whether every sighting was in the range the ramp was measured
            /// over, which is the only case the scale means anything.
            /// </summary>
            public bool InBand = true;

            /// <summary>
            /// Whether this was ever somebody's pet.
            /// </summary>
            /// <remarks>
            /// Kept rather than dropped, because the name is what lets the
            /// world spawn list throw the bot's own pets out: the bot records
            /// every NPC it sees and its three follow it everywhere, so they
            /// smear across the places it has walked.
            /// </remarks>
            public bool Pet;

            public readonly HashSet<int> Playfields = new HashSet<int>();

            public readonly List<KeyValuePair<int, int>> Health = new List<KeyValuePair<int, int>>();
        }

        private static int Main(string[] args)
        {
            if (args.Length < 2)
            {
                Console.Error.WriteLine("usage: WorldMobs <output.tsv> <capture directory> [more...]");
                Console.Error.WriteLine();
                Console.Error.WriteLine("  a capture directory is one holding the decoded *.csv streams");
                return 1;
            }

            string output = args[0];
            var serializer = new MessageSerializer();
            var table = new Dictionary<string, Creature>(StringComparer.OrdinalIgnoreCase);
            int files = 0, updates = 0, players = 0, pets = 0;

            foreach (string directory in args.Skip(1))
            {
                if (!Directory.Exists(directory))
                {
                    Console.Error.WriteLine("missing: " + directory);
                    return 1;
                }

                foreach (string path in Directory.GetFiles(directory, "*.csv").OrderBy(p => p))
                {
                    files++;
                    foreach (List<Chunk> chunks in ReadStreams(path).Values)
                    {
                        foreach (Packet packet in Frame(chunks))
                        {
                            if (!packet.FromServer)
                            {
                                continue;
                            }

                            var character = Decode(serializer, packet.Data) as SimpleCharFullUpdateMessage;
                            if (character == null || string.IsNullOrEmpty(character.Name)
                                || character.Level <= 0)
                            {
                                continue;
                            }

                            if (character.MonsterData == 0)
                            {
                                players++;
                                continue;
                            }

                            if (character.PetMaster != null)
                            {
                                pets++;
                            }

                            updates++;
                            Remember(table, character);
                        }
                    }
                }
            }

            if (table.Count == 0)
            {
                Console.Error.WriteLine("no creatures found");
                return 1;
            }

            Write(output, table.Values);

            Console.WriteLine(
                "{0} capture files: {1} creature updates, {2} skipped as players, {3} of them pets",
                files, updates, players, pets);
            Console.WriteLine(
                "  pet names: {0}",
                string.Join(", ", table.Values.Where(c => c.Pet).Select(c => c.Name).OrderBy(n => n)));
            Console.WriteLine(
                "{0} named creatures over {1} bodies and {2} playfields",
                table.Count,
                table.Values.Select(c => c.Monster).Distinct().Count(),
                table.Values.SelectMany(c => c.Playfields).Distinct().Count());

            Console.WriteLine();
            Console.WriteLine("=== how close the health model lands");
            var off = new List<int>();
            int exact = 0;
            int outside = 0;
            foreach (Creature creature in table.Values)
            {
                double scale = creature.ScaledSeen == 0 ? 0.0 : creature.Scaled / creature.ScaledSeen;
                foreach (KeyValuePair<int, int> point in creature.Health)
                {
                    if (point.Key < MissionCreatureHealth.LowestMeasured
                        || point.Key > MissionCreatureHealth.HighestMeasured)
                    {
                        // Outside the range the ramp was measured over, where
                        // it is not claimed to hold.
                        outside++;
                        continue;
                    }

                    int miss = Math.Abs(MissionCreatureHealth.Health(scale, point.Key) - point.Value);
                    if (miss == 0)
                    {
                        exact++;
                    }

                    off.Add(miss);
                }
            }

            Console.WriteLine(
                "  {0} sightings in levels {1}-{2}: {3} exact, {4} within one point, worst {5}",
                off.Count,
                MissionCreatureHealth.LowestMeasured,
                MissionCreatureHealth.HighestMeasured,
                exact,
                off.Count(m => m <= 1),
                off.Count == 0 ? 0 : off.Max());
            Console.WriteLine(
                "  {0} more sightings are outside that range, where the ramp is not claimed to hold",
                outside);

            Console.WriteLine();
            Console.WriteLine("=== the most seen");
            foreach (Creature creature in table.Values.OrderByDescending(c => c.Seen).Take(12))
            {
                Console.WriteLine(
                    "  {0,-34} body {1,-8} lvl {2,3}-{3,-3} x{4} in {5} playfields",
                    creature.Name.Length > 34 ? creature.Name.Substring(0, 34) : creature.Name,
                    creature.Monster,
                    creature.LowLevel,
                    creature.HighLevel,
                    creature.Seen,
                    creature.Playfields.Count);
            }

            Console.WriteLine();
            Console.WriteLine("written to " + Path.GetFullPath(output));
            return 0;
        }

        /// <summary>
        /// Folds one sighting into the creature's row.
        /// </summary>
        /// <remarks>
        /// A creature is its name. One body carries several names - 17649 is
        /// "34 - Automatic", "34-V worker" and "32-V Docker" - so the body
        /// cannot be the key, and the name is what the bot wrote down.
        /// </remarks>
        private static void Remember(Dictionary<string, Creature> table, SimpleCharFullUpdateMessage character)
        {
            Creature creature;
            if (!table.TryGetValue(character.Name, out creature))
            {
                creature = new Creature { Name = character.Name };
                table.Add(character.Name, creature);
            }

            creature.Seen++;
            if (character.PetMaster != null)
            {
                creature.Pet = true;
            }

            creature.Monster = character.MonsterData;
            creature.HeadMesh = character.HeadMesh.HasValue ? (int)character.HeadMesh.Value : 0;
            creature.Scale = character.MonsterScale;
            creature.Visual = character.VisualFlags;
            creature.RunSpeed = character.RunSpeedBase;
            if (character.PlayfieldId.HasValue)
            {
                creature.Playfields.Add(character.PlayfieldId.Value);
            }

            if (character.Level < creature.LowLevel)
            {
                creature.LowLevel = character.Level;
            }

            if (character.Level > creature.HighLevel)
            {
                creature.HighLevel = character.Level;
            }

            if (character.Health > 0)
            {
                // The ramp was measured over levels 19 to 44, so a scale
                // fitted anywhere else is meaningless - below level 3 the ramp
                // is negative and the scale comes out negative with it. The
                // points are kept either way, because those are the measurement.
                if (character.Level >= MissionCreatureHealth.LowestMeasured
                    && character.Level <= MissionCreatureHealth.HighestMeasured)
                {
                    creature.Scaled += character.Health / MissionCreatureHealth.Ramp(character.Level);
                    creature.ScaledSeen++;
                }
                else
                {
                    creature.InBand = false;
                }

                creature.Health.Add(new KeyValuePair<int, int>(character.Level, (int)character.Health));
            }
        }

        private static void Write(string output, IEnumerable<Creature> creatures)
        {
            var text = new StringBuilder();
            text.AppendLine("# What every creature in the captures looks like, from Tools/Capture/WorldMobs.");
            text.AppendLine("# Do not edit by hand - re-run the tool.");
            text.AppendLine("#");
            text.AppendLine("# monster is SimpleCharFullUpdate.MonsterData, the body. One body carries several");
            text.AppendLine("# names, so the name is the key here and the body is not.");
            text.AppendLine("# healthScale is what maximum health is a multiple of - see MissionCreature.Health,");
            text.AppendLine("# which holds the ramp it multiplies. That ramp was measured over levels 19 to 44");
            text.AppendLine("# only, so the scale is written for creatures seen inside it and left at zero for");
            text.AppendLine("# the rest rather than fitted where it would mean nothing.");
            text.AppendLine("# health is every level and maximum health this creature was actually seen with,");
            text.AppendLine("# as level:health. That is the measurement; the scale is a summary of it.");
            text.AppendLine("# Players are not here - a person wears MonsterData zero. Pets are, with pet=1:");
            text.AppendLine("# a pet carries its owner in PetMaster, and knowing the names is what lets a world");
            text.AppendLine("# spawn list throw out the ones that only followed somebody around.");
            text.AppendLine(
                "# name\tmonster\theadmesh\tscale\tvisual\trunspeed\tseen\tminLevel\tmaxLevel"
                + "\thealthScale\thealth\tpet\tplayfields");

            foreach (Creature creature in creatures.OrderBy(c => c.Name, StringComparer.OrdinalIgnoreCase))
            {
                text.AppendLine(string.Join(
                    "\t",
                    creature.Name.Replace('\t', ' '),
                    creature.Monster.ToString(CultureInfo.InvariantCulture),
                    creature.HeadMesh.ToString(CultureInfo.InvariantCulture),
                    creature.Scale.ToString(CultureInfo.InvariantCulture),
                    creature.Visual.ToString(CultureInfo.InvariantCulture),
                    creature.RunSpeed.ToString(CultureInfo.InvariantCulture),
                    creature.Seen.ToString(CultureInfo.InvariantCulture),
                    creature.LowLevel.ToString(CultureInfo.InvariantCulture),
                    creature.HighLevel.ToString(CultureInfo.InvariantCulture),
                    (creature.ScaledSeen == 0 ? 0.0 : creature.Scaled / creature.ScaledSeen)
                        .ToString("0.0000", CultureInfo.InvariantCulture),
                    string.Join(
                        " ",
                        creature.Health.Distinct()
                            .OrderBy(h => h.Key)
                            .Select(h => h.Key.ToString(CultureInfo.InvariantCulture) + ":"
                                         + h.Value.ToString(CultureInfo.InvariantCulture))),
                    creature.Pet ? "1" : "0",
                    string.Join(
                        " ",
                        creature.Playfields.OrderBy(p => p)
                            .Select(p => p.ToString(CultureInfo.InvariantCulture)))));
            }

            File.WriteAllText(output, text.ToString());
        }

        #region reading captures

        // The same reader QuestExtract and MissionRolls use: a capture csv holds each connection's
        // chunks in the order they crossed the wire, the server's half is one zlib stream after a
        // short handshake, and the client's is plaintext padded to a four byte boundary.

        private sealed class Chunk
        {
            public bool FromServer;

            public byte[] Bytes;
        }

        private sealed class Packet
        {
            public int Chunk;

            public int Order;

            public bool FromServer;

            public byte[] Data;
        }

        private static Dictionary<string, List<Chunk>> ReadStreams(string path)
        {
            var streams = new Dictionary<string, List<Chunk>>();
            foreach (string line in File.ReadLines(path))
            {
                string[] parts = line.Split(',');
                if (parts.Length < 3 || (parts[1] != "server" && parts[1] != "client"))
                {
                    continue;
                }

                string hex = parts[2].Replace(":", string.Empty).Trim();
                if (hex.Length < 2)
                {
                    continue;
                }

                List<Chunk> chunks;
                if (!streams.TryGetValue(parts[0], out chunks))
                {
                    chunks = new List<Chunk>();
                    streams[parts[0]] = chunks;
                }

                chunks.Add(new Chunk
                           {
                               FromServer = parts[1] == "server",
                               Bytes = Convert.FromHexString(hex.Substring(0, hex.Length & ~1))
                           });
            }

            return streams;
        }

        private static List<Packet> Frame(List<Chunk> chunks)
        {
            var packets = new List<Packet>();
            foreach (bool server in new[] { true, false })
            {
                var raw = new List<byte>();
                var starts = new List<long>();
                var chunkIds = new List<int>();
                for (int i = 0; i < chunks.Count; i++)
                {
                    if (chunks[i].FromServer == server)
                    {
                        starts.Add(raw.Count);
                        chunkIds.Add(i);
                        raw.AddRange(chunks[i].Bytes);
                    }
                }

                if (raw.Count == 0)
                {
                    continue;
                }

                byte[] data = raw.ToArray();
                List<long[]> map = null;

                bool plain = !server && CountFramable(data, 3, DetectAlignment(data)) >= 1;
                for (int start = 0; !plain && start + 1 < Math.Min(data.Length, 4096); start++)
                {
                    if (!IsZlibHeader(data, start))
                    {
                        continue;
                    }

                    var candidateMap = new List<long[]>();
                    byte[] inflated = Inflate(data, start, candidateMap);
                    if (inflated.Length > 0 && CountFramable(inflated, 3, 1) >= 1)
                    {
                        data = inflated;
                        map = candidateMap;
                        break;
                    }
                }

                int alignment = DetectAlignment(data);
                int pos = 0;
                while (pos + HeaderLength <= data.Length)
                {
                    int size = (data[pos + SizeOffset] << 8) | data[pos + SizeOffset + 1];
                    if (size < HeaderLength || pos + size > data.Length)
                    {
                        pos++;
                        continue;
                    }

                    var packet = new byte[size];
                    Array.Copy(data, pos, packet, 0, size);
                    long inputOffset = map == null ? pos : MapBack(map, pos);
                    int chunk = chunkIds[Math.Max(0, UpperBound(starts, inputOffset))];
                    packets.Add(new Packet
                                {
                                    Chunk = chunk, Order = packets.Count,
                                    FromServer = server, Data = packet
                                });
                    pos += size + Padding(size, alignment);
                }
            }

            return packets.OrderBy(p => p.Chunk).ThenBy(p => p.Order).ToList();
        }

        private static MessageBody Decode(MessageSerializer serializer, byte[] data)
        {
            try
            {
                using (var stream = new MemoryStream(data))
                {
                    Message message = serializer.Deserialize(stream);
                    return message == null ? null : message.Body;
                }
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static bool IsZlibHeader(byte[] d, int i)
        {
            return i + 1 < d.Length && (d[i] & 0x0F) == 8 && (((d[i] << 8) | d[i + 1]) % 31) == 0;
        }

        private static byte[] Inflate(byte[] input, int start, List<long[]> map)
        {
            var inflater = new Inflater(false);
            inflater.SetInput(input, start, input.Length - start);
            var output = new MemoryStream();
            var buffer = new byte[512];
            try
            {
                while (!inflater.IsFinished && !inflater.IsNeedingInput)
                {
                    map.Add(new[] { output.Length, start + inflater.TotalIn });
                    int produced = inflater.Inflate(buffer);
                    if (produced <= 0)
                    {
                        break;
                    }

                    output.Write(buffer, 0, produced);
                }
            }
            catch (Exception)
            {
            }

            return output.ToArray();
        }

        private static long MapBack(List<long[]> map, long outputOffset)
        {
            int lo = 0;
            int hi = map.Count - 1;
            while (lo < hi)
            {
                int mid = (lo + hi + 1) / 2;
                if (map[mid][0] <= outputOffset)
                {
                    lo = mid;
                }
                else
                {
                    hi = mid - 1;
                }
            }

            return map.Count == 0 ? outputOffset : map[lo][1];
        }

        private static int UpperBound(List<long> starts, long offset)
        {
            int lo = 0;
            int hi = starts.Count - 1;
            while (lo < hi)
            {
                int mid = (lo + hi + 1) / 2;
                if (starts[mid] <= offset)
                {
                    lo = mid;
                }
                else
                {
                    hi = mid - 1;
                }
            }

            return lo;
        }

        private static int Padding(int size, int alignment)
        {
            return alignment <= 1 ? 0 : (alignment - (size % alignment)) % alignment;
        }

        /// <summary>
        /// The client pads its packets to a four byte boundary and the server
        /// does not, so the alignment is whichever frames more of the stream.
        /// </summary>
        private static int DetectAlignment(byte[] data)
        {
            return CountFramable(data, 64, 4) > CountFramable(data, 64, 1) ? 4 : 1;
        }

        private static int CountFramable(byte[] data, int want, int alignment)
        {
            int pos = 0;
            int found = 0;
            while (pos + HeaderLength <= data.Length && found < want)
            {
                int size = (data[pos + SizeOffset] << 8) | data[pos + SizeOffset + 1];
                if (size < HeaderLength || pos + size > data.Length)
                {
                    return found;
                }

                found++;
                pos += size + Padding(size, alignment);
            }

            return found;
        }

        #endregion
    }

    /// <summary>
    /// The health ramp, kept the same as OmniCell.Core's MissionCreature.
    /// </summary>
    /// <remarks>
    /// The capture tools link the message models only, not the server
    /// libraries, so the ramp is written here as well. The "how close the
    /// health model lands" line of every run is what catches the copies
    /// drifting apart.
    /// </remarks>
    internal static class MissionCreatureHealth
    {
        private const int Knee = 25;

        private const double LowSlope = 33.0;

        private const double LowBase = 101.0;

        private const double HighSlope = 185.0 / 3.0;

        private const double HighBase = 2452.0 / 3.0;

        /// <summary>
        /// The lowest level the ramp was measured at.
        /// </summary>
        public const int LowestMeasured = 19;

        /// <summary>
        /// The highest level the ramp was measured at.
        /// </summary>
        public const int HighestMeasured = 44;

        public static double Ramp(int level)
        {
            level = Math.Max(1, level);
            return level <= Knee ? (LowSlope * level) - LowBase : (HighSlope * level) - HighBase;
        }

        public static int Health(double scale, int level)
        {
            if (scale <= 0)
            {
                scale = 1.0;
            }

            return Math.Max(1, (int)Math.Round(scale * Ramp(level), MidpointRounding.AwayFromZero));
        }
    }
}
