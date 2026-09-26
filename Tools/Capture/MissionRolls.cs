// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MissionRolls.cs" company="OmniCell">
//   Copyright © 2026 OmniCell contributors.
// </copyright>
// <summary>
//   Reads every mission roll out of a capture and says what the terminal offered.
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

    using SmokeLounge.AOtomation.Messaging.GameData;
    using SmokeLounge.AOtomation.Messaging.Messages;
    using SmokeLounge.AOtomation.Messaging.Messages.N3Messages;
    using SmokeLounge.AOtomation.Messaging.Serialization;

    /// <summary>
    /// What a mission terminal answers, at the slider setting it was asked at.
    /// </summary>
    /// <remarks>
    /// The one thing about missions that is still guesswork is the function
    /// from the six dimension sliders to the five mission types offered. The
    /// evidence for it is one row per setting, and a row costs a trip to a
    /// terminal, so every roll in every capture is worth having.
    ///
    /// The client sends a <see cref="QuestAlternativeMessage"/> with its
    /// sliders and an empty list; the server answers with the same message
    /// carrying up to five missions. Both halves are needed - the answer alone
    /// does not say what was asked, because the seed differs - so this puts the
    /// two directions back into capture order and pairs each request with the
    /// reply that follows it.
    ///
    ///     MissionRolls &lt;streams.csv&gt; [...] [--tsv rolls.tsv]
    ///
    /// Sliders are printed as the percentages the client's own interface shows.
    /// The wire byte is signed and the client maps a percentage to
    /// (percent - 50) * 2, so -100 is 0%, 0 is 50% and +100 is 100%.
    ///
    /// With --tsv the rolls are appended to a table rather than replacing it,
    /// and a roll already in it - same seed, same terminal - is not added
    /// twice. That is so the table can accumulate over captures the way
    /// Documentation/Missions.md's slider table has to.
    /// </remarks>
    internal static class MissionRolls
    {
        private const int HeaderLength = 16;

        private const int SizeOffset = 6;

        private static int Main(string[] args)
        {
            var captures = new List<string>();
            string tsv = null;
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "--tsv" && i + 1 < args.Length)
                {
                    tsv = args[++i];
                }
                else
                {
                    captures.Add(args[i]);
                }
            }

            if (captures.Count == 0)
            {
                Console.Error.WriteLine("usage: MissionRolls <streams.csv> [...] [--tsv rolls.tsv]");
                return 1;
            }

            var serializer = new MessageSerializer();
            var rolls = new List<Roll>();

            foreach (string pattern in captures)
            {
                var matched = 0;
                foreach (string path in Expand(pattern))
                {
                    if (!File.Exists(path))
                    {
                        Console.Error.WriteLine("no such capture: " + path);
                        continue;
                    }

                    matched++;
                    rolls.AddRange(Read(serializer, path));
                }

                if (matched == 0)
                {
                    Console.Error.WriteLine("nothing matched: " + pattern);
                }
            }

            if (rolls.Count == 0)
            {
                Console.WriteLine("no mission rolls in those captures.");
                return 0;
            }

            Report(rolls);

            if (tsv != null)
            {
                int added = Append(tsv, rolls);
                Console.WriteLine();
                Console.WriteLine("{0}: {1} new roll(s) of {2}", tsv, added, rolls.Count);
            }

            return 0;
        }

        private static IEnumerable<string> Expand(string pattern)
        {
            string dir = Path.GetDirectoryName(pattern);
            string name = Path.GetFileName(pattern);
            if (name.IndexOf('*') < 0 && name.IndexOf('?') < 0)
            {
                return new[] { pattern };
            }

            return Directory
                .GetFiles(string.IsNullOrEmpty(dir) ? "." : dir, name)
                .OrderBy(p => p, StringComparer.OrdinalIgnoreCase);
        }

        #region what a roll is

        private sealed class Roll
        {
            public string Capture;

            public string Stream;

            public int Difficulty;

            /// <summary>The six sliders the client asked at, as percentages.</summary>
            public int[] Sliders;

            /// <summary>
            /// The six the server answered with, which are not always the ones
            /// it was asked at - see the remarks on the class.
            /// </summary>
            public int[] Answered;

            /// <summary>
            /// Whether the answer came back at the setting it was asked at.
            /// </summary>
            public bool Echoed
            {
                get { return this.Sliders.SequenceEqual(this.Answered); }
            }

            public long Seed;

            public string Originator;

            public string Terminal;

            public List<Offer> Offers = new List<Offer>();

            /// <summary>
            /// The setting, which is what rolls are grouped by.
            /// </summary>
            public string Setting
            {
                get
                {
                    return this.Difficulty.ToString(CultureInfo.InvariantCulture)
                           + "\t" + string.Join("/", this.Sliders);
                }
            }

            /// <summary>
            /// The mix, counted and written the way Missions.md writes it.
            /// </summary>
            public string Mix
            {
                get
                {
                    return string.Join(
                        ", ",
                        this.Offers
                            .GroupBy(o => o.Type)
                            .OrderByDescending(g => g.Count())
                            .ThenBy(g => g.Key, StringComparer.Ordinal)
                            .Select(g => g.Count() + " " + g.Key));
                }
            }
        }

        private sealed class Offer
        {
            public string Type;

            public int Icon;

            public int Quality;

            public int Cash;

            public int Experience;

            public string Short;
        }

        #endregion

        #region reading

        private static List<Roll> Read(MessageSerializer serializer, string path)
        {
            var rolls = new List<Roll>();
            string capture = Path.GetFileNameWithoutExtension(path);

            foreach (KeyValuePair<string, List<Chunk>> stream in ReadStreams(path))
            {
                // The request and the reply are the same message in opposite
                // directions, so both halves have to be collected before either
                // can be read. Which of them comes out first is not reliable:
                // the two directions are reassembled separately and put back
                // together by the chunk each started in, and a reply that
                // shared a chunk boundary with its request can land ahead of
                // it. So they are paired by turn rather than by order - the
                // n'th request with the n'th answer - which is what a terminal
                // session is anyway.
                var asked = new List<QuestAlternativeMessage>();
                var answered = new List<QuestAlternativeMessage>();
                foreach (Packet packet in Frame(stream.Value))
                {
                    var alt = Decode(serializer, packet.Data) as QuestAlternativeMessage;
                    if (alt == null)
                    {
                        continue;
                    }

                    // A copy with nothing in it is the asking; one carrying
                    // missions is the answer. The direction says the same
                    // thing, and disagreement means this is not a roll.
                    bool empty = alt.QuestInfos == null || alt.QuestInfos.Length == 0;
                    if (!packet.FromServer && empty)
                    {
                        asked.Add(alt);
                    }
                    else if (packet.FromServer && !empty)
                    {
                        answered.Add(alt);
                    }
                }

                for (int i = 0; i < Math.Min(asked.Count, answered.Count); i++)
                {
                    QuestAlternativeMessage request = asked[i];
                    QuestAlternativeMessage reply = answered[i];

                    // The difficulty is echoed and has matched in every
                    // capture, so a pair that disagrees on it is a mispairing
                    // rather than a finding, and is not recorded. The six
                    // dimension bytes are a different matter and are kept as
                    // they came.
                    if (request.Difficulty != reply.Difficulty)
                    {
                        Console.Error.WriteLine(
                            "{0} stream {1}: request and reply {2} disagree on the difficulty"
                            + " ({3} against {4}), skipped",
                            capture, stream.Key, i, request.Difficulty, reply.Difficulty);
                        continue;
                    }

                    var roll = new Roll
                               {
                                   Capture = capture,
                                   Stream = stream.Key,
                                   Difficulty = request.Difficulty,
                                   Sliders = Sliders(request),
                                   Answered = Sliders(reply),
                                   Seed = (uint)reply.Seed,
                                   Originator = Convert.ToString(request.Originator),
                                   Terminal = Convert.ToString(request.MissionTerminalIdentity)
                               };

                    foreach (QuestAlternativeEntry entry in reply.QuestInfos)
                    {
                        QuestInfo info = entry.Quest;
                        if (info == null)
                        {
                            continue;
                        }

                        roll.Offers.Add(new Offer
                                        {
                                            Icon = info.MissionIconId,
                                            Type = Name(info.MissionIconId),
                                            Quality = info.Quality ?? 0,
                                            Cash = info.CashReward,
                                            Experience = info.ExperienceReward,
                                            Short = Clean(info.ShortInfo)
                                        });
                    }

                    rolls.Add(roll);
                }
            }

            return rolls;
        }

        /// <summary>
        /// The six sliders as the percentages the client shows, in wire order.
        /// </summary>
        private static int[] Sliders(QuestAlternativeMessage alt)
        {
            return new[]
                   {
                       Percent(alt.GoodBad), Percent(alt.ControlledLackingControl),
                       Percent(alt.OpenHidden), Percent(alt.PhysicalMystical),
                       Percent(alt.ExplosivePatient), Percent(alt.MoneyExperience)
                   };
        }

        /// <summary>
        /// The wire byte is signed, and the client maps a percentage to
        /// (percent - 50) * 2. This is that backwards.
        /// </summary>
        private static int Percent(byte wire)
        {
            return (((sbyte)wire) / 2) + 50;
        }

        private static string Name(int icon)
        {
            return Enum.IsDefined(typeof(MissionType), icon)
                       ? ((MissionType)icon).ToString()
                       : "icon " + icon.ToString(CultureInfo.InvariantCulture);
        }

        private static string Clean(string s)
        {
            if (string.IsNullOrEmpty(s))
            {
                return string.Empty;
            }

            var sb = new StringBuilder(s.Length);
            foreach (char c in s)
            {
                sb.Append(c == '\t' || c == '\r' || c == '\n' ? ' ' : c);
            }

            return sb.ToString().Trim();
        }

        #endregion

        #region report

        private static void Report(List<Roll> rolls)
        {
            Console.WriteLine("{0} roll(s)", rolls.Count);
            Console.WriteLine();

            foreach (Roll roll in rolls)
            {
                Console.WriteLine(
                    "{0} stream {1}   difficulty {2}   asked {3}{4}   seed {5}",
                    roll.Capture, roll.Stream, roll.Difficulty,
                    string.Join("/", roll.Sliders),
                    roll.Echoed ? string.Empty : "   ANSWERED " + string.Join("/", roll.Answered),
                    roll.Seed);
                Console.WriteLine("  {0} from {1}", roll.Originator, roll.Terminal);
                foreach (Offer offer in roll.Offers)
                {
                    Console.WriteLine(
                        "    {0,-12} QL {1,-4} {2,8} credits  {3,8} xp   {4}",
                        offer.Type, offer.Quality, offer.Cash, offer.Experience, offer.Short);
                }

                Console.WriteLine("  = {0}", roll.Mix);
                Console.WriteLine();
            }

            Console.WriteLine(
                "answered at the setting asked in {0} of {1} rolls",
                rolls.Count(r => r.Echoed), rolls.Count);
            Console.WriteLine();

            Console.WriteLine("=== by setting asked ===");
            Console.WriteLine("difficulty\tbad/chaos/hidden/myst/stealth/xp\trolls\toffered");
            foreach (IGrouping<string, Roll> setting in rolls
                .GroupBy(r => r.Setting)
                .OrderBy(g => g.Key, StringComparer.Ordinal))
            {
                List<IGrouping<string, Roll>> mixes = setting
                    .GroupBy(r => r.Mix)
                    .OrderByDescending(g => g.Count())
                    .ToList();

                Console.WriteLine(
                    "{0}\t{1}\t{2}",
                    setting.Key, setting.Count(),
                    mixes.Count == 1
                        ? mixes[0].Key
                        : mixes.Count + " different mixes: "
                          + string.Join(" | ", mixes.Select(m => m.Count() + "x " + m.Key)));
            }
        }

        /// <summary>
        /// Adds these rolls to a table that outlives the capture, skipping any
        /// already in it.
        /// </summary>
        private static int Append(string path, List<Roll> rolls)
        {
            var seen = new HashSet<string>(StringComparer.Ordinal);
            var lines = new List<string>();
            if (File.Exists(path))
            {
                foreach (string line in File.ReadLines(path))
                {
                    lines.Add(line);
                    string[] parts = line.Split('\t');
                    if (parts.Length > 4)
                    {
                        seen.Add(parts[2] + "|" + parts[3] + "|" + parts[4]);
                    }
                }
            }

            if (lines.Count == 0)
            {
                lines.Add("capture\tstream\tdifficulty\tasked\tseed\tterminal\tanswered\toffered");
            }

            int added = 0;
            foreach (Roll roll in rolls)
            {
                string sliders = string.Join("/", roll.Sliders);
                string key = roll.Difficulty.ToString(CultureInfo.InvariantCulture)
                             + "|" + sliders + "|" + roll.Seed.ToString(CultureInfo.InvariantCulture);
                if (!seen.Add(key))
                {
                    continue;
                }

                lines.Add(string.Join(
                    "\t",
                    roll.Capture, roll.Stream, roll.Difficulty, sliders, roll.Seed,
                    roll.Terminal, string.Join("/", roll.Answered), roll.Mix));
                added++;
            }

            File.WriteAllLines(path, lines);
            return added;
        }

        #endregion

        #region reading captures

        // The same reader QuestExtract uses: a capture csv holds each connection's chunks in the
        // order they crossed the wire, the server's half is one zlib stream after a short
        // handshake, and the client's is plaintext padded to a four byte boundary.

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
}
