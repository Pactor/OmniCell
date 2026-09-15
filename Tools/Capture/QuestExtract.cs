// QuestExtract - quest stages, what starts and finishes them, their rewards, and the
// conversations around them, read out of retail captures - and the SQL the server plays them from.
//
//   QuestExtract <out-dir> <streams.csv> [<streams.csv> ...] [--playfield N] [--as N] [--types]
//
// Writes into <out-dir>:
//   quest-model.md     the derived model, for people: stages, conversations, fixtures
//   quest-model.json   the same, for tools
//   quest-events.txt   every event the model was built from, session by session
//   quest-stages.sql   with --as N: the staged quest data for playfield N (Documentation/Quest-System.md)
//
// Why a separate pass from AreaExtract: that tool reads the server's half of a
// session and then, separately, the client's answers, so it can never tell what
// the player did just before the server finished a quest - and that is the whole
// question. Here both halves are merged back into capture order first (a capture
// csv holds its chunks in the order they crossed the wire), and everything else
// is read off that one sequence.
//
// What the live server was seen to do, and what this reads:
//   - a quest stage is one QuestFullUpdate entry; it is granted when one arrives
//     announced as new, and finished by CharacterAction MissionChanged naming it;
//   - the stage granted in the same run as a finish is the next stage;
//   - what finished it is the last thing the player did before: a kill (death, then
//     the loot feedback or a kill counter), using a fixture, using an item on a
//     fixture or on a character, using or buying an item, a tradeskill combine,
//     opening a conversation, choosing an answer, or a hand-in trade;
//   - rewards are a FormatFeedback with experience and credits, and items dropped
//     into the overflow window; items in the same run that are not the finished
//     stage's own rewards are handed over with the stage granted next;
//   - a conversation runs from the window opening to it closing, and what the NPC
//     says after the last answer list, before closing the window, is its farewell.
//
// Every rule in the output lists the capture and message number it came from.
// Nothing here knows about a particular quest.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

using ICSharpCode.SharpZipLib.Zip.Compression;

using SmokeLounge.AOtomation.Messaging.GameData;
using SmokeLounge.AOtomation.Messaging.Messages;
using SmokeLounge.AOtomation.Messaging.Messages.N3Messages;
using SmokeLounge.AOtomation.Messaging.Serialization;

internal static class QuestExtract
{
    private const int HeaderLength = 16;

    private const int SizeOffset = 6;

    // Feedback and FormatFeedback message ids, category 110, as the live server uses them.
    private const int LootRemainsMessage = 249817907;     // "You can loot these remains."

    private const string KillCounterFormat = "$nZiA";     // "You have to kill %d more %s"

    private const string RewardFormat = "$'O\"u";         // experience, credits

    // How soon after being used a fixture has to go for its going to count as used up. Gas Fires go
    // 30-64 messages after being put out (follow_new #4059-#4111, 20260914-124401 #2177-#2224); a Cargo
    // Box gone 3000 messages later was walked away from.
    private const int DespawnWindow = 100;

    private static int Main(string[] args)
    {
        if (args.Length < 2)
        {
            Console.WriteLine("usage: QuestExtract <out-dir> <streams.csv> [...] [--playfield N] [--as N] [--types]");
            return 2;
        }

        string outDir = args[0];
        int wantPlayfield = 0;
        int writeAs = 0;
        bool listTypes = false;
        string find = null;
        var inputs = new List<string>();
        for (int i = 1; i < args.Length; i++)
        {
            if (args[i] == "--playfield" && i + 1 < args.Length)
            {
                wantPlayfield = int.Parse(args[++i], CultureInfo.InvariantCulture);
            }
            else if (args[i] == "--as" && i + 1 < args.Length)
            {
                writeAs = int.Parse(args[++i], CultureInfo.InvariantCulture);
            }
            else if (args[i] == "--find" && i + 1 < args.Length)
            {
                // Every decoded message that mentions a value: an instance nothing else names.
                find = args[++i];
            }
            else if (args[i] == "--types")
            {
                // Which messages each direction of a session decoded into, for finding the one a
                // new kind of trigger arrives as.
                listTypes = true;
            }
            else
            {
                inputs.Add(args[i]);
            }
        }

        Directory.CreateDirectory(outDir);
        var serializer = new MessageSerializer();
        var model = new Model();
        var log = new StringBuilder();
        var seen = new HashSet<string>();

        foreach (string input in inputs)
        {
            // The same recording is often on disk under two names.
            string hash = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(input)));
            if (!seen.Add(hash))
            {
                Console.WriteLine("skipped    " + Path.GetFileName(input) + " (same bytes as an earlier input)");
                continue;
            }

            foreach (KeyValuePair<string, List<Chunk>> stream in ReadStreams(input))
            {
                var bodies = new List<Decoded>();
                int index = 0;
                foreach (Packet packet in Frame(stream.Value))
                {
                    index++;
                    MessageBody body = Decode(serializer, packet.Data);
                    if (body != null)
                    {
                        bodies.Add(new Decoded { Index = index, FromServer = packet.FromServer, Body = body });
                    }
                }

                if (wantPlayfield != 0 && !bodies.Any(b => PlayfieldOf(b.Body) == wantPlayfield))
                {
                    continue;
                }

                string tag = Path.GetFileNameWithoutExtension(input) + "/s" + stream.Key;
                if (find != null)
                {
                    var options = new JsonSerializerOptions { IncludeFields = true };
                    foreach (Decoded decoded in bodies)
                    {
                        string json;
                        try
                        {
                            json = JsonSerializer.Serialize(decoded.Body, decoded.Body.GetType(), options);
                        }
                        catch (Exception)
                        {
                            continue;
                        }

                        if (json.Contains(find, StringComparison.Ordinal))
                        {
                            Console.WriteLine(
                                "find       " + tag + " #" + decoded.Index + " " + (decoded.FromServer ? "server " : "client ")
                                + decoded.Body.GetType().Name + " " + (json.Length > 700 ? json.Substring(0, 700) + "..." : json));
                        }
                    }
                }

                if (!bodies.Any(b => b.Body is QuestFullUpdateMessage || b.Body is KnuBotOpenChatWindowMessage))
                {
                    continue;
                }
                if (listTypes)
                {
                    foreach (IGrouping<string, Decoded> type in bodies
                                 .GroupBy(b => (b.FromServer ? "server " : "client ") + b.Body.GetType().Name)
                                 .OrderBy(g => g.Key, StringComparer.Ordinal))
                    {
                        Console.WriteLine("types      " + tag + "  " + type.Count().ToString(CultureInfo.InvariantCulture).PadLeft(6) + "  " + type.Key);
                    }
                }

                log.AppendLine();
                log.AppendLine("===== " + tag);
                var session = new Session(tag, model, log);
                foreach (Decoded decoded in bodies)
                {
                    session.Take(decoded.Index, decoded.FromServer, decoded.Body);
                }

                session.Finish();
                Console.WriteLine(
                    "session    " + tag + ": " + bodies.Count + " messages, " + session.Grants + " stages granted, "
                    + session.Completions + " finished, " + session.Conversations + " conversations");
            }
        }

        model.ResolveNames();
        File.WriteAllText(Path.Combine(outDir, "quest-events.txt"), log.ToString());
        File.WriteAllText(Path.Combine(outDir, "quest-model.md"), Report.Write(model));
        File.WriteAllText(
            Path.Combine(outDir, "quest-model.json"),
            JsonSerializer.Serialize(model, new JsonSerializerOptions { WriteIndented = true, IncludeFields = true }));
        Console.WriteLine(
            "model      " + model.Stages.Count + " stages, " + model.Conversations.Count + " conversations, "
            + model.Fixtures.Count + " fixture templates -> " + outDir);

        if (writeAs != 0)
        {
            var sql = new SqlWriter(model, writeAs);
            File.WriteAllText(Path.Combine(outDir, "quest-stages.sql"), sql.Write());
            foreach (string note in sql.Notes)
            {
                Console.WriteLine("sql        " + note);
            }
        }

        return 0;
    }

    #region Reading captures

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

    private sealed class Decoded
    {
        public int Index;

        public bool FromServer;

        public MessageBody Body;
    }

    /// <summary>
    /// The chunks of each connection in a capture csv, in the order they were captured.
    /// </summary>
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

            chunks.Add(new Chunk { FromServer = parts[1] == "server", Bytes = Convert.FromHexString(hex.Substring(0, hex.Length & ~1)) });
        }

        return streams;
    }

    /// <summary>
    /// Every message of both directions, each tagged with the chunk it started in, so the two
    /// directions can be put back into capture order.
    /// </summary>
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

            // The server's half is one zlib stream after a short plaintext handshake; the
            // client's is plaintext. Try the stream only where plaintext does not frame.
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
                packets.Add(new Packet { Chunk = chunk, Order = packets.Count, FromServer = server, Data = packet });
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

    private static int PlayfieldOf(MessageBody body)
    {
        var item = body as SimpleItemFullUpdateMessage;
        if (item != null)
        {
            return item.Playfield;
        }

        var character = body as SimpleCharFullUpdateMessage;
        return character != null && character.PlayfieldId.HasValue ? character.PlayfieldId.Value : 0;
    }

    private static bool IsZlibHeader(byte[] d, int i)
    {
        return i + 1 < d.Length && (d[i] & 0x0F) == 8 && (((d[i] << 8) | d[i + 1]) % 31) == 0;
    }

    /// <summary>
    /// Inflates one zlib stream, noting for each piece of output which input byte it came from.
    /// </summary>
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

    private static int CountFramable(byte[] data, int limit, int alignment)
    {
        int pos = 0;
        int found = 0;
        while (pos + HeaderLength <= data.Length && found < limit)
        {
            int size = (data[pos + SizeOffset] << 8) | data[pos + SizeOffset + 1];
            if (size < HeaderLength || pos + size > data.Length)
            {
                break;
            }

            found++;
            pos += size + Padding(size, alignment);
        }

        return found;
    }

    private static int DetectAlignment(byte[] data)
    {
        int best = 1;
        int bestCount = CountFramable(data, int.MaxValue, 1);
        foreach (int candidate in new[] { 2, 4, 8 })
        {
            int count = CountFramable(data, int.MaxValue, candidate);
            if (count > bestCount)
            {
                bestCount = count;
                best = candidate;
            }
        }

        return best;
    }

    #endregion

    #region Feedback text

    /// <summary>
    /// A FormatFeedback message: category, message id, and its integer and string arguments.
    /// </summary>
    internal sealed class Feedback
    {
        public int Category;

        public string MessageId = string.Empty;

        public readonly List<int> Ints = new List<int>();

        public readonly List<string> Strings = new List<string>();

        /// <summary>
        /// "~&" + base-85 category + message id, then 'i' + a base-85 int or 's' + text, "~" to end.
        /// Base-85 digits are characters minus '!', five to a number.
        /// </summary>
        public static Feedback Parse(string text)
        {
            if (string.IsNullOrEmpty(text) || !text.StartsWith("~&", StringComparison.Ordinal) || text.Length < 12)
            {
                return null;
            }

            var result = new Feedback { Category = Base85(text, 2), MessageId = text.Substring(7, 5) };
            int pos = 12;
            while (pos < text.Length && text[pos] != '~')
            {
                char kind = text[pos++];
                if (kind == 'i' && pos + 5 <= text.Length)
                {
                    result.Ints.Add(Base85(text, pos));
                    pos += 5;
                }
                else if (kind == 's')
                {
                    int end = text.IndexOf('~', pos);
                    end = end < 0 ? text.Length : end;
                    // Without the terminator bytes some strings carry: a label compares equal to the name.
                    result.Strings.Add(new string(text.Substring(pos, end - pos).Where(c => !char.IsControl(c)).ToArray()).Trim());
                    pos = end;
                }
                else
                {
                    break;
                }
            }

            return result;
        }

        private static int Base85(string text, int pos)
        {
            long value = 0;
            for (int i = 0; i < 5; i++)
            {
                value = (value * 85) + (text[pos + i] - '!');
            }

            return unchecked((int)value);
        }
    }

    #endregion

    /// <summary>
    /// The items a quest text links to, in order: itemref://low/high/ql, sometimes with spaces.
    /// </summary>
    internal static List<ItemRef> ItemRefs(string text)
    {
        return Regex.Matches(text ?? string.Empty, @"itemref://\s*(\d+)\s*/\s*(\d+)\s*/\s*(\d+)")
            .Select(
                m => new ItemRef
                         {
                             LowId = int.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture),
                             HighId = int.Parse(m.Groups[2].Value, CultureInfo.InvariantCulture),
                             Quality = int.Parse(m.Groups[3].Value, CultureInfo.InvariantCulture),
                             Count = 1
                         })
            .ToList();
    }

    /// <summary>
    /// Whether a quest text names a creature: the whole name, or every word of it - the stage text
    /// writes "The Kneebreaker", Alfonzo Rizzolo for Kneebreaker Alfonzo Rizzolo.
    /// </summary>
    internal static bool Mentions(string text, string name)
    {
        if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(name))
        {
            return false;
        }

        if (text.IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return true;
        }

        string[] words = name.Split(new[] { ' ', '-' }, StringSplitOptions.RemoveEmptyEntries).Where(w => w.Length > 2).ToArray();
        return words.Length > 1 && words.All(w => text.IndexOf(w, StringComparison.OrdinalIgnoreCase) >= 0);
    }

    #region The model

    internal sealed class Model
    {
        public readonly Dictionary<string, Stage> Stages = new Dictionary<string, Stage>();

        public readonly List<Conversation> Conversations = new List<Conversation>();

        public readonly Dictionary<int, FixtureKind> Fixtures = new Dictionary<int, FixtureKind>();

        /// <summary>
        /// Character names by instance, from every session.
        /// </summary>
        public readonly Dictionary<int, string> Names = new Dictionary<int, string>();

        /// <summary>
        /// Gives a name to every "#instance" some other session did see the name of.
        /// </summary>
        public void ResolveNames()
        {
            Func<string, string> resolve = name =>
                {
                    int id;
                    string known;
                    return name != null && name.StartsWith("#", StringComparison.Ordinal)
                           && int.TryParse(name.Substring(1), out id) && this.Names.TryGetValue(id, out known)
                               ? known
                               : name;
                };

            foreach (Conversation conversation in this.Conversations)
            {
                conversation.Npc = resolve(conversation.Npc);
            }

            foreach (Stage stage in this.Stages.Values)
            {
                stage.LogGiver = resolve(stage.LogGiver);
                foreach (Grant grant in stage.Grants)
                {
                    grant.Npc = resolve(grant.Npc);
                }

                foreach (Completion completion in stage.Completions.Where(c => c.Trigger != null))
                {
                    completion.Trigger.Name = resolve(completion.Trigger.Name);
                }
            }
        }

        /// <summary>
        /// Fixture templates by instance, from every session, for a session that uses a fixture it
        /// was never shown.
        /// </summary>
        public readonly Dictionary<int, int> FixtureInstances = new Dictionary<int, int>();

        public int StageOrder;

        public string NameOf(string stageKey)
        {
            Stage stage;
            return stageKey != null && this.Stages.TryGetValue(stageKey, out stage) ? stage.Name : stageKey;
        }
    }

    internal sealed class ItemRef
    {
        public int LowId;

        public int HighId;

        public int Quality;

        public int Count;

        public override string ToString()
        {
            return LowId + (HighId != LowId ? "/" + HighId : string.Empty) + " QL" + Quality + (Count > 1 ? " x" + Count : string.Empty);
        }
    }

    internal sealed class Stage
    {
        public string Key;

        public string Name;

        public string Description;

        public string LogGiver;

        public int? QuestCode;

        public int Credits;

        public int Experience;

        public int IconId;

        public int FirstSeen;

        public int KillsRequired;

        /// <summary>
        /// What the kill counter calls the creatures, and the creatures seen counting towards it.
        /// </summary>
        public string CounterLabel;

        public readonly SortedSet<string> Counted = new SortedSet<string>(StringComparer.Ordinal);

        public readonly List<ItemRef> ItemRewards = new List<ItemRef>();

        public readonly List<string> Ids = new List<string>();

        public readonly List<Grant> Grants = new List<Grant>();

        public readonly List<Completion> Completions = new List<Completion>();

        /// <summary>
        /// The first captured quest log entry, for the wire tables.
        /// </summary>
        [JsonIgnore]
        public QuestInfo Info;

        public int CanonicalId
        {
            get
            {
                return this.Ids.Select(i => int.Parse(i.Substring(i.LastIndexOf(':') + 1), CultureInfo.InvariantCulture)).DefaultIfEmpty(0).Min();
            }
        }
    }

    /// <summary>
    /// Something the player did that could finish a stage.
    /// </summary>
    internal sealed class Trigger
    {
        public string Kind = string.Empty;

        /// <summary>
        /// A creature or character name, where the trigger has one.
        /// </summary>
        public string Name;

        public int Template;

        public ItemRef Item;

        public string ItemNote;

        public string Answer;

        public override string ToString()
        {
            var text = new StringBuilder(this.Kind);
            if (this.Item != null)
            {
                text.Append(" item ").Append(this.Item).Append(this.ItemNote ?? string.Empty);
            }

            if (this.Template != 0)
            {
                text.Append(this.Item != null ? " on" : string.Empty).Append(" fixture template ").Append(this.Template);
            }

            if (this.Name != null)
            {
                text.Append(this.Item != null && this.Template == 0 && this.Kind == "UseItemOnCharacter" ? " on " : " ").Append(this.Name);
            }

            if (this.Answer != null)
            {
                text.Append(": \"").Append(this.Answer).Append('"');
            }

            return text.ToString();
        }

        public Trigger Copy()
        {
            return (Trigger)this.MemberwiseClone();
        }
    }

    internal sealed class Grant
    {
        public string Evidence;

        public string How;

        /// <summary>
        /// "answer" (a dialogue answer granted it), "finish" (another stage finishing did), "open" or "other".
        /// </summary>
        public string Kind;

        public string Npc;

        public string Answer;

        public string FromKey;

        public readonly List<ItemRef> ItemsGiven = new List<ItemRef>();
    }

    internal sealed class Completion
    {
        public string Evidence;

        public Trigger Trigger;

        public int? Experience;

        public int? Credits;

        public readonly List<ItemRef> Items = new List<ItemRef>();

        public readonly List<string> NextKeys = new List<string>();
    }

    /// <summary>
    /// One conversation, everything in the order it happened.
    /// </summary>
    internal sealed class Conversation
    {
        public string Npc;

        public string Evidence;

        public readonly List<string> ActiveKeys = new List<string>();

        public readonly List<string> DoneKeys = new List<string>();

        public readonly List<Entry> Entries = new List<Entry>();

        public readonly List<Entry> Farewell = new List<Entry>();

        public int? CloseSeconds;

        public string Signature()
        {
            return string.Join(" | ", this.Entries.Select(e => e.Kind + ":" + e.Text + (e.Answers.Count > 0 ? "[" + string.Join("/", e.Answers) + "]" : string.Empty)));
        }
    }

    /// <summary>
    /// "says" a line, "offers" answers, "chooses" one, "grants"/"finishes" a stage, "trade" opens a
    /// trade window, "then" anything else that happened.
    /// </summary>
    internal sealed class Entry
    {
        public string Kind;

        public string Text = string.Empty;

        public readonly List<string> Answers = new List<string>();

        public int Flag;

        public string StageKey;
    }

    internal sealed class FixtureKind
    {
        public int Template;

        public readonly SortedSet<string> Positions = new SortedSet<string>();

        public int Uses;

        public int DespawnsAfterUse;

        public int Relights;

        public readonly SortedSet<string> FeedbackTexts = new SortedSet<string>();

        public readonly Dictionary<string, int> FeedbackRaw = new Dictionary<string, int>();

        public readonly SortedSet<string> UsedWith = new SortedSet<string>();
    }

    #endregion

    #region One session

    private sealed class Session
    {
        private readonly string tag;

        private readonly Model model;

        private readonly StringBuilder log;

        private readonly Dictionary<int, string> names = new Dictionary<int, string>();

        private readonly Dictionary<int, int> fixtureTemplates = new Dictionary<int, int>();

        private readonly Dictionary<int, string> fixturePositions = new Dictionary<int, string>();

        private readonly Dictionary<int, int> usedAt = new Dictionary<int, int>();

        private int index;

        private readonly HashSet<string> putOut = new HashSet<string>();

        private readonly Dictionary<int, ItemRef> inventory = new Dictionary<int, ItemRef>();

        private readonly List<ItemRef> overflowRun = new List<ItemRef>();

        private readonly Dictionary<int, string> active = new Dictionary<int, string>();

        private readonly Dictionary<string, List<ItemRef>> grantedWith = new Dictionary<string, List<ItemRef>>();

        private readonly List<string> done = new List<string>();

        private readonly HashSet<string> commandsSeen = new HashSet<string>();

        /// <summary>
        /// Everything the player did since the last stage finished, oldest first.
        /// </summary>
        private readonly List<Trigger> recent = new List<Trigger>();

        /// <summary>
        /// Inventory slots known only from the login snapshot, which moves since have made stale.
        /// </summary>
        private readonly HashSet<int> snapshotSlots = new HashSet<int>();

        private int player;

        private int tradeskillResult;

        private bool atVendor;

        private bool loggedIn;

        private Trigger last = new Trigger();

        private int lastDeath;

        private int? runExperience;

        private int? runCredits;

        private Completion lastCompletion;

        private bool actionSinceCompletion = true;

        private Conversation conversation;

        private int conversationNpc;

        public Session(string tag, Model model, StringBuilder log)
        {
            this.tag = tag;
            this.model = model;
            this.log = log;
        }

        public int Grants { get; private set; }

        public int Completions { get; private set; }

        public int Conversations { get; private set; }

        public void Take(int index, bool fromServer, MessageBody body)
        {
            this.index = index;
            string evidence = this.tag + " #" + index;

            switch (body)
            {
                case FullCharacterMessage full:
                    this.player = full.Identity.Instance;
                    foreach (InventorySlot slot in full.InventorySlots ?? new InventorySlot[0])
                    {
                        this.inventory[slot.Placement] = new ItemRef { LowId = slot.ItemLowId, HighId = slot.ItemHighId, Quality = slot.Quality, Count = slot.Count };
                        this.snapshotSlots.Add(slot.Placement);
                    }

                    break;

                case SimpleCharFullUpdateMessage character:
                    if (!string.IsNullOrEmpty(character.Name))
                    {
                        this.names[character.Identity.Instance] = character.Name;
                        this.model.Names[character.Identity.Instance] = character.Name;
                    }

                    break;

                case SimpleItemFullUpdateMessage item:
                    this.TakeFixture(item);
                    break;

                case CorpseFullUpdateMessage corpse:
                    // A corpse is a death too, whether or not the player loots it.
                    if (!string.IsNullOrEmpty(corpse.Name))
                    {
                        const string Remains = "Remains of ";
                        string dead = corpse.Name.StartsWith(Remains, StringComparison.Ordinal) ? corpse.Name.Substring(Remains.Length) : corpse.Name;
                        this.recent.Add(new Trigger { Kind = "Kill", Name = dead });
                    }

                    break;

                case DespawnMessage despawn:
                    this.TakeDespawn(despawn.Identity.Instance, evidence);
                    break;

                case QuestFullUpdateMessage quests:
                    if (this.player == 0)
                    {
                        this.player = quests.Identity.Instance;
                    }

                    this.TakeQuestLog(quests, evidence);
                    break;

                case CharacterActionMessage action:
                    if (action.Action == CharacterActionType.MissionChanged && action.Target.Type == IdentityType.Quest)
                    {
                        this.Complete(action.Target.Instance, evidence);
                    }
                    else if ((int)action.Action == 99)
                    {
                        this.lastDeath = action.Identity.Instance;

                        // Every death is a candidate, looted or not: a stage whose text names the creature
                        // finishes on its death even when a nearby robot's loot message came last.
                        this.recent.Add(new Trigger { Kind = "Kill", Name = this.NameOf(action.Identity.Instance) });
                    }
                    else if (action.Action == CharacterActionType.TradeskillResult && action.Parameter2 != 0)
                    {
                        // The result also lands in the overflow window; that is not a quest handing
                        // anything over.
                        this.tradeskillResult = action.Parameter2;
                        this.Act(new Trigger { Kind = "Tradeskill", Item = new ItemRef { LowId = action.Parameter2, HighId = action.Parameter2, Quality = 1, Count = 1 } }, evidence);
                    }

                    break;

                case TradeMessage shopping:
                    this.atVendor = shopping.Target.Type == IdentityType.VendingMachine;
                    break;

                case AddTemplateMessage bought:
                    if (this.atVendor)
                    {
                        this.Act(new Trigger { Kind = "Buy", Item = new ItemRef { LowId = bought.LowId, HighId = bought.HighId, Quality = bought.Quality, Count = 1 } }, evidence);
                    }

                    break;

                case FeedbackMessage feedback:
                    if (feedback.CategoryId == 110 && feedback.MessageId == LootRemainsMessage && this.lastDeath != 0)
                    {
                        this.Act(new Trigger { Kind = "Kill", Name = this.NameOf(this.lastDeath) }, evidence);
                        this.lastDeath = 0;
                    }

                    break;

                case FormatFeedbackMessage format:
                    this.TakeFeedback(Feedback.Parse(format.FormattedMessage), format.FormattedMessage, evidence);
                    break;

                case GenericCmdMessage command:
                    this.TakeCommand(command, evidence);
                    break;

                case TemplateActionMessage template:
                    this.TakeTemplate(template, evidence);
                    break;

                case MoveItemMessage move:
                    this.Event(evidence + "  MOVE     " + (fromServer ? "server " : "client ") + move.Source.Type + ":" + move.Source.Instance + " -> slot " + move.Destination);
                    break;

                case ContainerAddItemMessage add:
                    if (add.SourceContainer.Type == IdentityType.Corpse)
                    {
                        this.Act(new Trigger { Kind = "Loot", Name = "a corpse" }, evidence);
                    }

                    break;

                case KnuBotOpenChatWindowMessage open:
                    this.Open(open.Target.Instance, evidence);
                    break;

                case KnuBotAppendTextMessage text:
                    this.Say(text.Target.Instance, text.Text, text.Unknown2);
                    break;

                case KnuBotAnswerListMessage list:
                    this.Offer(list.Target.Instance, (list.DialogOptions ?? new KnuBotDialogOption[0]).Select(o => o.Text).ToList());
                    break;

                case KnuBotAnswerMessage answer:
                    this.Choose(answer.Target.Instance, answer.Answer, evidence);
                    break;

                case KnuBotStartTradeMessage trade:
                    if (this.conversation != null)
                    {
                        this.conversation.Entries.Add(new Entry { Kind = "trade", Text = trade.Message ?? string.Empty, Flag = trade.NumberOfItemSlotsInTradeWindow });
                    }

                    break;

                case KnuBotTradeMessage tradeItem:
                    if (!fromServer)
                    {
                        string note;
                        ItemRef offered = this.ItemIn(tradeItem.Item, true, out note);
                        this.Then("the player puts " + (offered == null ? note : "item " + offered + note) + " in the trade window");
                        this.tradeOffered = offered;
                        this.tradeOfferedNote = note;
                    }

                    break;

                case KnuBotFinishTradeMessage finish:
                    // Not the action itself: the client's half of a trade can be captured after the
                    // server's answer to it. The server's answer, below, is the hand-in.
                    this.Then(finish.Declined == 0 ? "the player accepts the trade" : "the player declines the trade");
                    break;

                case KnuBotRejectedItemsMessage rejected:
                    this.Act(new Trigger { Kind = "TradeHandIn", Name = this.NameOf(rejected.Target.Instance), Item = this.tradeOffered, ItemNote = this.tradeOfferedNote }, evidence);
                    this.Then("the NPC takes the trade, rejecting " + (rejected.Items == null ? 0 : rejected.Items.Length) + " items");
                    break;

                case KnuBotCloseChatWindowMessage close:
                    if (this.conversation != null && close.Target.Instance == this.conversationNpc)
                    {
                        if (fromServer)
                        {
                            this.conversation.CloseSeconds = close.Seconds;
                        }

                        this.EndConversation();
                    }

                    break;
            }
        }

        private ItemRef tradeOffered;

        private string tradeOfferedNote;

        public void Finish()
        {
            this.EndConversation();
        }

        private string NameOf(int instance)
        {
            string name;
            return this.names.TryGetValue(instance, out name) ? name : "#" + instance;
        }

        private void Event(string text)
        {
            this.log.AppendLine(text);
        }

        /// <summary>
        /// Something the player did that could finish a stage.
        /// </summary>
        private void Act(Trigger trigger, string evidence)
        {
            // The same action seen again before anything finished is the server echoing what the
            // client sent (a kill counter after the loot message): not a new run.
            if (trigger.ToString() == this.last.ToString() && this.actionSinceCompletion)
            {
                return;
            }

            this.last = trigger;
            this.actionSinceCompletion = true;
            this.runExperience = null;
            this.runCredits = null;
            this.recent.Add(trigger);
            this.Event(evidence + "  ACTION   " + trigger);
        }

        private void Then(string text)
        {
            if (this.conversation != null)
            {
                this.conversation.Entries.Add(new Entry { Kind = "then", Text = text });
            }
        }

        private void TakeFixture(SimpleItemFullUpdateMessage item)
        {
            int template = 0;
            foreach (GameTuple<CharacterStat, uint> stat in item.Stats ?? new GameTuple<CharacterStat, uint>[0])
            {
                if (stat.Value1.ToString() == "ACGItemTemplateID")
                {
                    template = (int)stat.Value2;
                }
            }

            if (template == 0 || item.Coordinate == null)
            {
                return;
            }

            string position = string.Format(
                CultureInfo.InvariantCulture,
                "{0:0.0},{1:0.0},{2:0.0}",
                item.Coordinate.X,
                item.Coordinate.Y,
                item.Coordinate.Z);
            FixtureKind kind = this.Kind(template);
            kind.Positions.Add(position);
            int instance = item.Identity.Instance;
            if (!this.fixtureTemplates.ContainsKey(instance) && this.putOut.Remove(template + "@" + position))
            {
                kind.Relights++;
                this.Event(this.tag + "  RELIT    fixture template " + template + " at " + position + " as #" + instance);
            }

            this.fixtureTemplates[instance] = template;
            this.fixturePositions[instance] = position;
            this.model.FixtureInstances[instance] = template;
        }

        private void TakeDespawn(int instance, string evidence)
        {
            int template;
            if (!this.fixtureTemplates.TryGetValue(instance, out template))
            {
                return;
            }

            // Only a fixture the player used counts; everything else despawns because the
            // player walked away from it.
            int used;
            if (this.usedAt.TryGetValue(instance, out used) && this.index - used <= DespawnWindow)
            {
                this.Kind(template).DespawnsAfterUse++;
                this.putOut.Add(template + "@" + this.fixturePositions[instance]);
                this.Event(evidence + "  GONE     fixture template " + template + " at " + this.fixturePositions[instance] + " after use");
            }

            this.fixtureTemplates.Remove(instance);
            this.usedAt.Remove(instance);
        }

        private FixtureKind Kind(int template)
        {
            FixtureKind kind;
            if (!this.model.Fixtures.TryGetValue(template, out kind))
            {
                kind = new FixtureKind { Template = template };
                this.model.Fixtures[template] = kind;
            }

            return kind;
        }

        private void TakeQuestLog(QuestFullUpdateMessage update, string evidence)
        {
            foreach (QuestInfo info in update.QuestInfos ?? new QuestInfo[0])
            {
                int id = info.QuestIdentity.Instance;
                Stage stage = this.StageFor(info);
                if (this.active.ContainsKey(id))
                {
                    continue;
                }

                this.active[id] = stage.Key;
                if (!stage.Ids.Contains(this.tag + ":" + id))
                {
                    stage.Ids.Add(this.tag + ":" + id);
                }

                // The first log of a session is what the character already had.
                if (update.AnnounceAsNew == 0 && !this.loggedIn)
                {
                    this.Event(evidence + "  HELD     " + stage.Name);
                    continue;
                }

                var grant = new Grant { Evidence = evidence };
                if (this.lastCompletion != null && !this.actionSinceCompletion)
                {
                    grant.Kind = "finish";
                    grant.How = "finishing " + this.lastCompletion.Trigger;
                    grant.FromKey = this.lastCompletion.Evidence;
                    this.lastCompletion.NextKeys.Add(stage.Key);
                }
                else if (this.conversation != null && this.last.Kind == "Answer")
                {
                    grant.Kind = "answer";
                    grant.Npc = this.conversation.Npc;
                    grant.Answer = this.last.Answer;
                    grant.How = "answer \"" + this.last.Answer + "\" to " + this.conversation.Npc;
                }
                else if (this.conversation != null)
                {
                    grant.Kind = "open";
                    grant.Npc = this.conversation.Npc;
                    grant.How = "opening a conversation with " + this.conversation.Npc;
                }
                else
                {
                    grant.Kind = "other";
                    grant.How = "after " + this.last;
                }

                // Items in the same run that were not the finished stage's own rewards come with
                // the stage granted now - the extinguisher, the stim.
                grant.ItemsGiven.AddRange(this.overflowRun);
                this.grantedWith[stage.Key] = new List<ItemRef>(this.overflowRun);
                this.overflowRun.Clear();
                stage.Grants.Add(grant);
                this.Grants++;
                if (this.conversation != null)
                {
                    this.conversation.Entries.Add(new Entry { Kind = "grants", StageKey = stage.Key, Text = stage.Name });
                }

                this.Event(evidence + "  GRANT    " + stage.Name + "  (" + grant.How + ")" + (grant.ItemsGiven.Count > 0 ? " with " + string.Join(", ", grant.ItemsGiven) : string.Empty));
            }

            this.loggedIn = true;
        }

        private Stage StageFor(QuestInfo info)
        {
            string name = info.ShortInfo ?? string.Empty;
            string description = info.Info ?? string.Empty;
            string key = name + "\n" + description;
            Stage stage;
            if (!this.model.Stages.TryGetValue(key, out stage))
            {
                stage = new Stage
                            {
                                Key = key,
                                Name = name,
                                Description = description,
                                LogGiver = this.NameOf(info.QuestGiver.Instance),
                                QuestCode = info.QuestCode,
                                Credits = info.CashReward,
                                Experience = info.ExperienceReward,
                                IconId = info.MissionIconId,
                                FirstSeen = ++this.model.StageOrder,
                                Info = info
                            };
                foreach (QuestItemShort reward in info.ItemRewards ?? new QuestItemShort[0])
                {
                    stage.ItemRewards.Add(new ItemRef { LowId = reward.LowId, HighId = reward.HighId, Quality = reward.Quality, Count = 1 });
                }

                this.model.Stages[key] = stage;
            }
            else if (stage.LogGiver.StartsWith("#", StringComparison.Ordinal))
            {
                stage.LogGiver = this.NameOf(info.QuestGiver.Instance);
            }

            return stage;
        }

        private void Complete(int id, string evidence)
        {
            string key;
            if (!this.active.TryGetValue(id, out key))
            {
                return;
            }

            Stage stage = this.model.Stages[key];
            Trigger trigger = this.last.Copy();
            if (trigger.Kind == "Open")
            {
                trigger.Kind = "TalkOnOpen";
            }
            else if (trigger.Kind == "Answer")
            {
                trigger.Kind = "DialogueAnswer";
            }

            // Two kills close together: the one the stage's own text names is the one that counted,
            // whichever the capture happened to put last.
            if (trigger.Kind == "Kill" && trigger.Name != null && !Mentions(stage.Description, trigger.Name))
            {
                Trigger named = this.recent.LastOrDefault(
                    r => r.Kind == "Kill" && r.Name != null && Mentions(stage.Description, r.Name));
                if (named != null)
                {
                    trigger.Name = named.Name;
                }
            }

            // An item the stage's own text links to, arriving just before it finished, with nothing else
            // the player did to explain the finish: the stage was to get that item (the Lock Pick,
            // 20260911-163012_s8 #1397-#1408). It is the player's purchase, not something handed over.
            if (trigger.Kind == "UseItem" || trigger.Kind == string.Empty)
            {
                List<ItemRef> linked = ItemRefs(stage.Description);
                ItemRef got = this.overflowRun.FirstOrDefault(i => linked.Any(r => r.LowId == i.LowId || r.HighId == i.HighId));
                if (got != null)
                {
                    trigger = new Trigger { Kind = "Buy", Item = got, ItemNote = " (arrived just before the finish)" };
                    this.overflowRun.Remove(got);
                }
            }

            this.recent.Clear();
            var completion = new Completion
                                 {
                                     Evidence = evidence,
                                     Trigger = trigger,
                                     Experience = this.runExperience,
                                     Credits = this.runCredits
                                 };

            // The finished stage's own item rewards, as its quest log entry lists them, are taken
            // out of the run. Anything else in the run comes with whatever is granted next.
            foreach (ItemRef reward in stage.ItemRewards)
            {
                ItemRef delivered = this.overflowRun.FirstOrDefault(i => i.LowId == reward.LowId || i.HighId == reward.HighId);
                if (delivered != null)
                {
                    completion.Items.Add(delivered);
                    this.overflowRun.Remove(delivered);
                }
            }

            stage.Completions.Add(completion);
            this.active.Remove(id);
            this.done.Add(key);
            this.lastCompletion = completion;
            this.actionSinceCompletion = false;
            this.runExperience = null;
            this.runCredits = null;
            this.Completions++;
            if (this.conversation != null)
            {
                this.conversation.Entries.Add(new Entry { Kind = "finishes", StageKey = key, Text = stage.Name });
            }

            this.Event(evidence + "  FINISH   " + stage.Name + "  <- " + trigger
                       + (completion.Experience.HasValue ? "  reward xp " + completion.Experience + " credits " + completion.Credits : string.Empty)
                       + (completion.Items.Count > 0 ? "  items " + string.Join(", ", completion.Items) : string.Empty));
        }

        private void TakeFeedback(Feedback feedback, string raw, string evidence)
        {
            if (feedback == null)
            {
                return;
            }

            if (feedback.MessageId == KillCounterFormat && feedback.Ints.Count > 0 && feedback.Strings.Count > 0)
            {
                // The counter names a group ("Junkyard Robots"); the creature just killed is what counted.
                string target = feedback.Strings[0];
                Trigger killed = this.recent.LastOrDefault(
                    r => r.Kind == "Kill" && r.Name != null && r.Name != target && !r.Name.StartsWith("#", StringComparison.Ordinal));
                this.Event(evidence + "  COUNTER  " + feedback.Ints[0] + " more " + target + (killed == null ? string.Empty : " (after killing " + killed.Name + ")"));
                List<Stage> counting = this.active.Values.Select(k => this.model.Stages[k]).Where(s => Mentions(s.Description, target)).ToList();
                if (counting.Count == 0)
                {
                    counting = this.active.Values.Select(k => this.model.Stages[k]).ToList();
                }

                foreach (Stage stage in counting)
                {
                    stage.KillsRequired = Math.Max(stage.KillsRequired, feedback.Ints[0] + 1);
                    stage.CounterLabel = target;
                    if (killed != null)
                    {
                        stage.Counted.Add(killed.Name);
                    }
                }

                this.Act(new Trigger { Kind = "Kill", Name = target }, evidence);
                this.lastDeath = 0;
                return;
            }

            if (feedback.MessageId == RewardFormat && feedback.Ints.Count >= 2)
            {
                this.runExperience = feedback.Ints[0];
                this.runCredits = feedback.Ints[1];
                this.Event(evidence + "  REWARD   xp " + feedback.Ints[0] + " credits " + feedback.Ints[1]);
                return;
            }

            string text = string.Join(" ", feedback.Strings);
            if (text.Length > 0)
            {
                this.Event(evidence + "  FEEDBACK " + text);
                if (this.last.Kind.StartsWith("Use", StringComparison.Ordinal) && this.last.Template != 0 && this.last.Answer == null)
                {
                    FixtureKind kind = this.Kind(this.last.Template);
                    kind.FeedbackTexts.Add(text);
                    int count;
                    kind.FeedbackRaw.TryGetValue(raw, out count);
                    kind.FeedbackRaw[raw] = count + 1;
                    this.last.Answer = null;
                }
            }
        }

        private void TakeCommand(GenericCmdMessage command, string evidence)
        {
            if (command.User.Instance != this.player || command.Target == null || command.Target.Length == 0)
            {
                return;
            }

            // The client sends a command and the server echoes it; count it once.
            if (!this.commandsSeen.Add(command.Action + ":" + command.Serial))
            {
                return;
            }

            Identity first = command.Target[0];
            string note;
            switch (command.Action)
            {
                case GenericCmdAction.Use:
                    if (first.Type == IdentityType.Terminal)
                    {
                        this.UseFixture(first.Instance, false, null, null, evidence);
                    }
                    else if (first.Type == IdentityType.Inventory)
                    {
                        ItemRef used = this.ItemIn(first, false, out note);
                        this.Act(new Trigger { Kind = "UseItem", Item = used, ItemNote = note }, evidence);
                    }

                    break;

                case GenericCmdAction.UseItemOnItem:
                    if (command.Target.Length > 1 && command.Target[1].Type == IdentityType.Terminal)
                    {
                        ItemRef used = this.ItemIn(first, false, out note);
                        this.UseFixture(command.Target[1].Instance, true, used, note, evidence);
                    }

                    break;

                case GenericCmdAction.UseItemOnCharacter:
                    if (command.Target.Length > 1)
                    {
                        ItemRef used = this.ItemIn(first, false, out note);
                        this.Act(new Trigger { Kind = "UseItemOnCharacter", Item = used, ItemNote = note, Name = this.NameOf(command.Target[1].Instance) }, evidence);
                    }

                    break;
            }
        }

        private void UseFixture(int instance, bool withItem, ItemRef item, string note, string evidence)
        {
            int template;
            if (!this.fixtureTemplates.TryGetValue(instance, out template) && !this.model.FixtureInstances.TryGetValue(instance, out template))
            {
                this.Act(new Trigger { Kind = withItem ? "UseItemOnFixture" : "UseFixture", Item = item, ItemNote = note, Name = "unknown fixture #" + instance }, evidence);
                return;
            }

            FixtureKind kind = this.Kind(template);
            kind.Uses++;
            this.usedAt[instance] = this.index;
            if (item != null)
            {
                kind.UsedWith.Add(item.ToString());
            }

            this.Act(new Trigger { Kind = withItem ? "UseItemOnFixture" : "UseFixture", Item = item, ItemNote = note, Template = template }, evidence);
        }

        /// <summary>
        /// The item in an inventory slot, or null with a note saying what is known.
        /// </summary>
        /// <remarks>
        /// An item handed over with a stage the player is on is what that stage is used with (the
        /// extinguisher, the stim): the capture seldom shows it being moved out of the overflow
        /// window, so what a slot held earlier is no guide. Handing something back in a trade, it is
        /// what an earlier stage handed over.
        /// </remarks>
        private ItemRef ItemIn(Identity slot, bool handingBack, out string note)
        {
            foreach (string stage in this.active.Values.Reverse())
            {
                List<ItemRef> given;
                if (this.grantedWith.TryGetValue(stage, out given) && given.Count == 1)
                {
                    note = " (handed over with \"" + this.model.NameOf(stage) + "\")";
                    return given[0];
                }
            }

            ItemRef item;
            bool known = slot.Type == IdentityType.Inventory && this.inventory.TryGetValue(slot.Instance, out item);
            if (known && !this.snapshotSlots.Contains(slot.Instance))
            {
                note = string.Empty;
                return this.inventory[slot.Instance];
            }

            if (handingBack)
            {
                foreach (string stage in Enumerable.Reverse(this.done))
                {
                    List<ItemRef> given;
                    if (this.grantedWith.TryGetValue(stage, out given) && given.Count == 1)
                    {
                        note = " (handed over with \"" + this.model.NameOf(stage) + "\")";
                        return given[0];
                    }
                }
            }

            if (known)
            {
                note = " (login snapshot)";
                return this.inventory[slot.Instance];
            }

            note = "the item in " + slot.Type + ":" + slot.Instance;
            return null;
        }

        private void TakeTemplate(TemplateActionMessage template, string evidence)
        {
            var item = new ItemRef { LowId = template.ItemLowId, HighId = template.ItemHighId, Quality = template.Quality, Count = Math.Max(1, template.Unknown1) };
            if (template.Placement.Type == IdentityType.OverflowWindow)
            {
                if (item.LowId == this.tradeskillResult || item.HighId == this.tradeskillResult)
                {
                    this.tradeskillResult = 0;
                    this.Then("the combine result " + item + " lands in the overflow window");
                    return;
                }

                this.overflowRun.Add(item);
                this.Then("gives item " + item);
                this.Event(evidence + "  ITEM     " + item + " into the overflow window");
                return;
            }

            if (template.Identity.Instance != this.player || template.Placement.Type != IdentityType.Inventory)
            {
                return;
            }

            this.inventory[template.Placement.Instance] = item;
            this.snapshotSlots.Remove(template.Placement.Instance);

            // An item used on a character: the target rides along in the last two fields.
            if (template.Unknown3 == (int)IdentityType.CanbeAffected && template.Unknown4 != 0)
            {
                this.Act(new Trigger { Kind = "UseItemOnCharacter", Item = item, Name = this.NameOf(template.Unknown4) }, evidence);
            }
        }

        private void Open(int npc, string evidence)
        {
            // The client asks and the server answers with the same message; one conversation.
            if (this.conversation != null && this.conversationNpc == npc && this.conversation.Entries.Count == 0)
            {
                return;
            }

            this.EndConversation();

            // Whatever was sitting in the overflow window before - the login mail, loot - is not
            // something this conversation hands over.
            this.overflowRun.Clear();
            this.conversation = new Conversation { Npc = this.NameOf(npc), Evidence = evidence };
            this.conversation.ActiveKeys.AddRange(this.active.Values.OrderBy(v => v, StringComparer.Ordinal));
            this.conversation.DoneKeys.AddRange(this.done);
            this.conversationNpc = npc;
            this.Act(new Trigger { Kind = "Open", Name = this.conversation.Npc }, evidence);
        }

        private void Say(int npc, string text, int flag)
        {
            if (this.conversation != null && npc == this.conversationNpc)
            {
                this.conversation.Entries.Add(new Entry { Kind = "says", Text = text, Flag = flag });
            }
        }

        private void Offer(int npc, List<string> answers)
        {
            if (this.conversation != null && npc == this.conversationNpc)
            {
                var entry = new Entry { Kind = "offers" };
                entry.Answers.AddRange(answers);
                this.conversation.Entries.Add(entry);
            }
        }

        private void Choose(int npc, int answer, string evidence)
        {
            if (this.conversation == null || npc != this.conversationNpc)
            {
                return;
            }

            Entry offered = this.conversation.Entries.LastOrDefault(e => e.Kind == "offers");
            string chosen = offered != null && answer >= 0 && answer < offered.Answers.Count ? offered.Answers[answer] : "#" + answer;
            this.conversation.Entries.Add(new Entry { Kind = "chooses", Text = chosen });
            this.Act(new Trigger { Kind = "Answer", Name = this.conversation.Npc, Answer = chosen }, evidence);
        }

        private void EndConversation()
        {
            if (this.conversation == null)
            {
                return;
            }

            // What the NPC says after the last answer, when it then closes the window itself, is its
            // farewell - whether the player chose Goodbye or closed the window.
            if (this.conversation.CloseSeconds.HasValue)
            {
                int lastIndex = this.conversation.Entries.FindLastIndex(e => e.Kind != "says");
                List<Entry> trailing = this.conversation.Entries.Skip(lastIndex + 1).ToList();
                if (trailing.Count > 0)
                {
                    this.conversation.Farewell.AddRange(trailing);
                    this.conversation.Entries.RemoveRange(lastIndex + 1, trailing.Count);
                }
            }

            this.model.Conversations.Add(this.conversation);
            this.Conversations++;
            this.Event(this.conversation.Evidence + "  TALK     " + this.conversation.Npc + " (active: " + string.Join("; ", this.conversation.ActiveKeys.Select(this.model.NameOf)) + ")");
            this.conversation = null;
            this.conversationNpc = 0;
        }
    }

    #endregion

    #region Report

    private static class Report
    {
        public static string Write(Model model)
        {
            var md = new StringBuilder();
            md.AppendLine("# Quest model extracted from captures");
            md.AppendLine();
            md.AppendLine("Generated by Tools/Capture/QuestExtract. Every line names the capture and message it came from.");
            md.AppendLine();
            md.AppendLine("## Stages");

            foreach (Stage stage in model.Stages.Values.OrderBy(s => s.FirstSeen))
            {
                md.AppendLine();
                md.AppendLine("### " + stage.Name);
                md.AppendLine();
                md.AppendLine("- Description: " + Flatten(stage.Description, 240));
                md.AppendLine("- Quest log: giver field " + stage.LogGiver + "; reward " + stage.Credits + " credits, " + stage.Experience + " xp"
                              + (stage.ItemRewards.Count > 0 ? ", items " + string.Join(", ", stage.ItemRewards) : string.Empty)
                              + (stage.KillsRequired > 0 ? "; kills required " + stage.KillsRequired : string.Empty));
                foreach (IGrouping<string, Grant> how in stage.Grants.GroupBy(g => g.How + (g.ItemsGiven.Count > 0 ? ", handing over " + string.Join(", ", g.ItemsGiven) : string.Empty)))
                {
                    md.AppendLine("- Granted by " + how.Key + "  [" + string.Join(", ", how.Select(g => g.Evidence)) + "]");
                }

                foreach (IGrouping<string, Completion> by in stage.Completions.GroupBy(c => Describe(model, c)))
                {
                    md.AppendLine("- Finished by " + by.Key + "  [" + string.Join(", ", by.Select(c => c.Evidence)) + "]");
                }

                if (stage.Grants.Count == 0 && stage.Completions.Count == 0)
                {
                    md.AppendLine("- Only seen already in the quest log.");
                }
            }

            md.AppendLine();
            md.AppendLine("## Conversations");
            foreach (IGrouping<string, Conversation> npc in model.Conversations.GroupBy(c => c.Npc).OrderBy(g => g.Key))
            {
                md.AppendLine();
                md.AppendLine("### " + npc.Key);
                foreach (IGrouping<string, Conversation> state in npc.GroupBy(c => c.ActiveKeys.Count == 0 ? "no stage active" : "active: " + string.Join("; ", c.ActiveKeys.Select(model.NameOf))))
                {
                    md.AppendLine();
                    md.AppendLine("#### " + state.Key);
                    foreach (IGrouping<string, Conversation> walk in state.GroupBy(c => c.Signature()))
                    {
                        Conversation c = walk.First();
                        md.AppendLine();
                        md.AppendLine("Seen " + walk.Count() + "x  [" + string.Join(", ", walk.Select(w => w.Evidence)) + "]");
                        foreach (Entry entry in c.Entries)
                        {
                            switch (entry.Kind)
                            {
                                case "says":
                                    md.AppendLine("- NPC: " + Flatten(entry.Text, 300));
                                    break;
                                case "offers":
                                    md.AppendLine("  - answers: " + string.Join(" / ", entry.Answers));
                                    break;
                                case "chooses":
                                    md.AppendLine("  - player chooses **" + entry.Text + "**");
                                    break;
                                case "trade":
                                    md.AppendLine("  - then opens a trade window with " + entry.Flag + " slots: \"" + entry.Text + "\"");
                                    break;
                                default:
                                    md.AppendLine("  - then " + entry.Kind + " " + entry.Text);
                                    break;
                            }
                        }

                        if (c.Farewell.Count > 0 || c.CloseSeconds.HasValue)
                        {
                            md.AppendLine("- farewell: " + (c.Farewell.Count == 0 ? "(none)" : string.Join(" / ", c.Farewell.Select(f => Flatten(f.Text, 200))))
                                          + (c.CloseSeconds.HasValue ? "; window closes after " + c.CloseSeconds + " s" : string.Empty));
                        }
                    }
                }
            }

            md.AppendLine();
            md.AppendLine("## Fixtures used by players");
            md.AppendLine();
            foreach (FixtureKind kind in model.Fixtures.Values.Where(k => k.Uses > 0).OrderBy(k => k.Template))
            {
                md.AppendLine("- template " + kind.Template + ": " + kind.Positions.Count + " positions (" + string.Join("; ", kind.Positions) + "), used "
                              + kind.Uses + "x" + (kind.UsedWith.Count > 0 ? " with " + string.Join(", ", kind.UsedWith) : string.Empty)
                              + ", gone after use " + kind.DespawnsAfterUse + "x, back at the same position " + kind.Relights + "x"
                              + (kind.FeedbackTexts.Count > 0 ? ", feedback \"" + string.Join("\", \"", kind.FeedbackTexts) + "\"" : string.Empty));
            }

            return md.ToString();
        }

        private static string Describe(Model model, Completion c)
        {
            return c.Trigger
                   + (c.Experience.HasValue ? "; pays " + c.Experience + " xp, " + c.Credits + " credits" : string.Empty)
                   + (c.Items.Count > 0 ? "; items " + string.Join(", ", c.Items) : string.Empty)
                   + (c.NextKeys.Count > 0 ? "; next: " + string.Join(", ", c.NextKeys.Select(model.NameOf)) : string.Empty);
        }

        internal static string Flatten(string text, int max)
        {
            text = (text ?? string.Empty).Replace("\r", " ").Replace("\n", " ");
            return text.Length > max ? text.Substring(0, max) + "..." : text;
        }
    }

    #endregion

    #region SQL

    /// <summary>
    /// Turns the model into the staged quest data one playfield is played from.
    /// </summary>
    /// <remarks>
    /// Where the captures disagree the rule seen most often wins, and every choice made without
    /// capture evidence is written as an "OmniCell-defined" comment and listed in Notes.
    /// </remarks>
    private sealed class SqlWriter
    {
        /// <summary>
        /// How long a used fixture stays away. No capture has timestamps yet.
        /// </summary>
        private const int DefaultRespawnSeconds = 60;

        private readonly Model model;

        private readonly int playfield;

        private readonly StringBuilder sql = new StringBuilder();

        private readonly Dictionary<string, int> ids = new Dictionary<string, int>();

        /// <summary>
        /// Stages with an objective the server can match. No conversation hands out any other: the
        /// player could never finish it.
        /// </summary>
        private readonly HashSet<string> playable = new HashSet<string>(StringComparer.Ordinal);

        public SqlWriter(Model model, int playfield)
        {
            this.model = model;
            this.playfield = playfield;
        }

        public List<string> Notes { get; } = new List<string>();

        public string Write()
        {
            foreach (Stage stage in this.model.Stages.Values)
            {
                this.ids[stage.Key] = stage.CanonicalId;
            }

            List<Stage> stages = this.model.Stages.Values.Where(s => s.Info != null).OrderBy(s => s.FirstSeen).ToList();
            string idList = string.Join(", ", stages.Select(s => this.ids[s.Key]));

            this.Line("-- Staged quests for playfield " + this.playfield + ", generated by Tools/Capture/QuestExtract from retail captures.");
            this.Line("-- See OmniCell/Documentation/Quest-System.md. Replaces every quest, quest progress row, objective, reward,");
            this.Line("-- transition, wire row and conversation of this playfield. Each rule names the captures it came from;");
            this.Line("-- anything not proven by a capture says \"OmniCell-defined\".");
            this.Line();
            this.Line("DELETE FROM questtransitions WHERE FromQuest IN (SELECT Id FROM quests WHERE Playfield = " + this.playfield + ");");
            foreach (string table in new[] { "questobjectives", "questitemrewards", "questwire", "questwireactions", "questwirerewards", "charactersquests" })
            {
                this.Line("DELETE FROM " + table + " WHERE QuestId IN (SELECT Id FROM quests WHERE Playfield = " + this.playfield + ");");
            }

            this.Line("DELETE FROM quests WHERE Playfield = " + this.playfield + ";");
            if (idList.Length > 0)
            {
                this.Line("DELETE FROM questtransitions WHERE FromQuest IN (" + idList + ");");
                foreach (string table in new[] { "questobjectives", "questitemrewards", "questwire", "questwireactions", "questwirerewards", "charactersquests" })
                {
                    this.Line("DELETE FROM " + table + " WHERE QuestId IN (" + idList + ");");
                }

                this.Line("DELETE FROM quests WHERE Id IN (" + idList + ");");
            }

            this.Line("DELETE FROM knubotscript WHERE Playfield = " + this.playfield + ";");
            this.Line("DELETE FROM knubotdialogue WHERE Playfield = " + this.playfield + ";");
            this.Line("DELETE FROM knubotopeners WHERE Playfield = " + this.playfield + ";");
            this.Line();

            foreach (Stage stage in stages)
            {
                this.WriteStage(stage);
            }

            foreach (IGrouping<string, Conversation> npc in this.model.Conversations.GroupBy(c => c.Npc).OrderBy(g => g.Key, StringComparer.Ordinal))
            {
                if (!npc.Key.StartsWith("#", StringComparison.Ordinal))
                {
                    this.WriteConversations(npc.Key, npc.ToList());
                }
            }

            this.WriteFixtures();
            return this.sql.ToString();
        }

        private void Line(string text = "")
        {
            this.sql.AppendLine(text);
        }

        private static string Text(string text)
        {
            return "'" + (text ?? string.Empty).Replace("\\", "\\\\").Replace("'", "\\'").Replace("\r", "\\r").Replace("\n", "\\n") + "'";
        }

        private static string Int(int value)
        {
            return value.ToString(CultureInfo.InvariantCulture);
        }

        private static string Float(float value)
        {
            return value.ToString("R", CultureInfo.InvariantCulture);
        }

        private void WriteStage(Stage stage)
        {
            int id = this.ids[stage.Key];
            QuestInfo info = stage.Info;
            this.Line("-- " + Report.Flatten(stage.Name, 80) + "  [" + string.Join(", ", stage.Ids.Take(4)) + (stage.Ids.Count > 4 ? ", ..." : string.Empty) + "]");
            this.Line(
                "INSERT INTO quests (Id, Name, Description, GiverId, IconId, CashReward, ExperienceReward, Playfield, Requires) VALUES ("
                + Int(id) + ", " + Text(stage.Name) + ", " + Text(stage.Description) + ", " + Int(info.QuestGiver.Instance) + ", "
                + Int(stage.IconId) + ", " + Int(stage.Credits) + ", " + Int(stage.Experience) + ", " + Int(this.playfield) + ", 0);");

            this.WriteObjective(stage, id);

            // Items that come with the stage: the set seen most often.
            List<ItemRef> onGrant = stage.Grants
                .GroupBy(g => string.Join(",", g.ItemsGiven.Select(i => i.LowId + "x" + i.Count)))
                .OrderByDescending(g => g.Count())
                .Select(g => g.First().ItemsGiven)
                .FirstOrDefault() ?? new List<ItemRef>();
            foreach (ItemRef item in onGrant)
            {
                this.Line("INSERT INTO questitemrewards (QuestId, ItemId, Quantity, GrantOnAccept) VALUES (" + Int(id) + ", " + Int(item.LowId) + ", " + Int(Math.Max(1, item.Count)) + ", 1);");
            }

            // The stage's own rewards, as its quest log entry lists them, in the stack size handed over.
            foreach (ItemRef reward in stage.ItemRewards)
            {
                int count = stage.Completions.SelectMany(c => c.Items).Where(i => i.LowId == reward.LowId).Select(i => i.Count).DefaultIfEmpty(1).Max();
                this.Line("INSERT INTO questitemrewards (QuestId, ItemId, Quantity, GrantOnAccept) VALUES (" + Int(id) + ", " + Int(reward.LowId) + ", " + Int(Math.Max(1, count)) + ", 0);");
            }

            int ordinal = 0;
            foreach (string next in stage.Completions.SelectMany(c => c.NextKeys).Distinct())
            {
                int nextId;
                if (this.ids.TryGetValue(next, out nextId) && this.model.Stages[next].Info != null)
                {
                    this.Line("INSERT INTO questtransitions (FromQuest, ToQuest, Ordinal) VALUES (" + Int(id) + ", " + Int(nextId) + ", " + Int(ordinal++) + ");");
                }
            }

            this.WriteWire(stage, id, info);
            this.Line();
        }

        private void WriteObjective(Stage stage, int id)
        {
            // The trigger seen most often; with a tie, the one that knows most (an item, a template).
            IGrouping<string, Completion> best = stage.Completions
                .Where(c => c.Trigger != null && c.Trigger.Kind != "Loot")
                .GroupBy(c => c.Trigger.Kind + "|" + c.Trigger.Name + "|" + c.Trigger.Template + "|" + c.Trigger.Answer)
                .OrderByDescending(g => g.Count())
                .ThenByDescending(g => g.First().Trigger.Template != 0 ? 1 : 0)
                .FirstOrDefault();
            if (best == null)
            {
                this.Notes.Add("no captured finish for \"" + stage.Name + "\": no objective, and no conversation hands it out");
                this.Line("-- No capture shows what finishes this stage, so it has no objective and no conversation hands it out.");
                return;
            }

            // The item: the one identified most often, where any capture identified it.
            Trigger t = best.First().Trigger.Copy();
            Completion identified = best.Where(c => c.Trigger.Item != null)
                .GroupBy(c => c.Trigger.Item.LowId)
                .OrderByDescending(g => g.Count())
                .Select(g => g.First())
                .FirstOrDefault();
            t.Item = identified == null ? null : identified.Trigger.Item;
            t.ItemNote = identified == null ? null : identified.Trigger.ItemNote;

            // What the stage's own text links to is what it wants handed over or used; the item a
            // capture shows in a slot is only inferred.
            List<ItemRef> linked = ItemRefs(stage.Description);
            bool usesItem = t.Kind == "TradeHandIn" || t.Kind == "Buy" || t.Kind.StartsWith("UseItem", StringComparison.Ordinal);
            if (usesItem && linked.Count > 0
                && (t.Item == null || !linked.Any(r => r.LowId == t.Item.LowId || r.HighId == t.Item.HighId || r.LowId == t.Item.HighId || r.HighId == t.Item.LowId)))
            {
                t.Item = linked[0];
                t.ItemNote = " (linked in the stage description)";
            }

            int kind;
            string target;
            string why = null;
            ItemRef item = t.Item;
            int required = 1;
            switch (t.Kind)
            {
                case "Kill":
                    kind = 0;
                    target = this.NamedKill(stage, t.Name, ref why);
                    required = Math.Max(1, stage.KillsRequired);
                    item = null;

                    // A counter naming a group: "label|creature|creature", with every creature seen counting.
                    if (stage.CounterLabel != null && (stage.Counted.Count > 1 || !stage.Counted.Contains(stage.CounterLabel)))
                    {
                        var creatures = new SortedSet<string>(stage.Counted, StringComparer.Ordinal);
                        if (target != null)
                        {
                            creatures.Add(target);
                        }

                        creatures.Remove(stage.CounterLabel);
                        if (creatures.Count > 0)
                        {
                            target = stage.CounterLabel + "|" + string.Join("|", creatures);
                            why = (why ?? string.Empty) + "; the counter says " + stage.CounterLabel + " and counted " + string.Join(", ", creatures);
                        }
                    }

                    break;
                case "UseFixture":
                    kind = 4;
                    target = FixtureTarget(t, ref why);
                    item = null;
                    break;
                case "UseItemOnFixture":
                    kind = 2;
                    target = FixtureTarget(t, ref why);
                    break;
                case "UseItemOnCharacter":
                    kind = 11;
                    target = t.Name;
                    break;
                case "UseItem":
                    kind = 10;
                    target = item == null ? null : Int(item.LowId);
                    item = null;
                    break;
                case "Buy":
                    kind = 8;
                    target = item == null ? null : Int(item.LowId);
                    item = null;
                    break;
                case "Tradeskill":
                    kind = 9;
                    target = item == null ? null : Int(item.LowId);
                    item = null;
                    break;
                case "TalkOnOpen":
                    kind = 3;
                    target = t.Name;
                    item = null;
                    break;
                case "DialogueAnswer":
                    kind = 12;
                    target = t.Answer;
                    item = null;
                    break;
                case "TradeHandIn":
                    kind = 7;
                    target = item == null ? null : Int(item.LowId);
                    break;
                default:
                    kind = -1;
                    target = null;
                    break;
            }

            if (kind < 0 || string.IsNullOrEmpty(target) || target.StartsWith("#", StringComparison.Ordinal) || (kind == 7 && item == null))
            {
                this.Notes.Add("\"" + stage.Name + "\" finished by " + t + ", which the server cannot match");
                this.Line("-- Finished by " + t + " in the captures, which the server cannot match yet.");
                return;
            }

            this.playable.Add(stage.Key);
            this.Line("-- Finished by " + t + (why ?? string.Empty) + "  [" + string.Join(", ", best.Select(c => c.Evidence).Take(4)) + "]");
            this.Line(
                "INSERT INTO questobjectives (QuestId, Ordinal, ObjectiveType, Target, TargetLowId, TargetHighId, TargetQuality, Required) VALUES ("
                + Int(id) + ", 0, " + Int(kind) + ", " + Text(target) + ", " + Int(item == null ? 0 : item.LowId) + ", "
                + Int(item == null ? 0 : item.HighId) + ", " + Int(item == null ? 0 : Math.Max(1, item.Quality)) + ", " + Int(required) + ");");
        }

        /// <summary>
        /// The creature a kill stage wants. A kill a capture shows just before the finish can be a
        /// bystander's (a robot next to the Kneebreaker); a name the stage's own text uses wins.
        /// </summary>
        private string NamedKill(Stage stage, string name, ref string why)
        {
            if (name == null || Mentions(stage.Description, name))
            {
                return name;
            }

            string named = this.model.Stages.Values.SelectMany(s => s.Completions)
                .Where(c => c.Trigger != null && c.Trigger.Kind == "Kill" && c.Trigger.Name != null)
                .Select(c => c.Trigger.Name)
                .Distinct()
                .Where(n => !n.StartsWith("#", StringComparison.Ordinal) && Mentions(stage.Description, n))
                .OrderByDescending(n => n.Length)
                .FirstOrDefault();
            if (named == null)
            {
                return name;
            }

            why = "; the capture's last kill was " + name + ", the stage text names " + named;
            return named;
        }

        /// <summary>
        /// A fixture objective's target: the template, or for a fixture no capture ever spawns - one the
        /// client has from its own playfield data - the instance the client uses.
        /// </summary>
        private static string FixtureTarget(Trigger t, ref string why)
        {
            const string Unknown = "unknown fixture #";
            if (t.Template != 0)
            {
                return Int(t.Template);
            }

            if (t.Name == null || !t.Name.StartsWith(Unknown, StringComparison.Ordinal))
            {
                return null;
            }

            why = "; no capture spawns this fixture (the client has it from its own playfield data), so it is matched by instance";
            return t.Name.Substring(Unknown.Length);
        }

        private void WriteWire(Stage stage, int id, QuestInfo info)
        {
            this.Line(
                "INSERT INTO questwire (QuestId, Source, GiverType, GiverInstance, QuestCode, UnknownHash, Quality, TimeLimit, Unknown20, Unknown21, Unknown22, Unknown23Type, Unknown23Instance, Unknown25, Unknown26) VALUES ("
                + Int(id) + ", 'Captured', " + Int((int)info.QuestGiver.Type) + ", " + Int(info.QuestGiver.Instance) + ", "
                + Int(info.QuestCode ?? 0) + ", " + Int(info.UnknownHash ?? 0) + ", " + Int(info.Quality ?? 0) + ", " + Int(info.TimeLimit) + ", "
                + Int(info.Unknown20 ?? 0) + ", " + Int(info.Unknown21 ?? 0) + ", " + Int(info.Unknown22 ?? 0) + ", "
                + Int(info.Unknown23.HasValue ? (int)info.Unknown23.Value.Type : 0) + ", " + Int(info.Unknown23.HasValue ? info.Unknown23.Value.Instance : 0) + ", "
                + Int(info.Unknown25 ?? 0) + ", " + Int(info.Unknown26 ?? 0) + ");");

            int ordinal = 0;
            foreach (QuestActionList a in info.QuestActions ?? new QuestActionList[0])
            {
                this.Line(
                    "INSERT INTO questwireactions (QuestId, Ordinal, Version, ActionType, ActionInstance, Unknown1Type, Unknown1Instance, Unknown2Type, Unknown2Instance, Unknown3Type, Unknown3Instance, Unknown4Type, Unknown4Instance, Unknown5, Unknown6, Unknown7, Unknown8, Unknown9Type, Unknown9Instance, Unknown10, Unknown11, Unknown12, Unknown13, Unknown14Type, Unknown14Instance, Deadline, Unknown16, TrackingType, PlayfieldType, PlayfieldInstance, Unknown18, Unknown19, X, Y, Z) VALUES ("
                    + Int(id) + ", " + Int(ordinal++) + ", " + Int(a.Version) + ", " + Id(a.Action) + ", " + Id(a.Unknown1) + ", " + Id(a.Unknown2) + ", "
                    + Id(a.Unknown3) + ", " + Id(a.Unknown4) + ", " + Float(a.Unknown5) + ", " + Float(a.Unknown6) + ", " + Float(a.Unknown7) + ", "
                    + Float(a.Unknown8) + ", " + Id(a.Unknown9) + ", " + Float(a.Unknown10) + ", " + Float(a.Unknown11) + ", " + Float(a.Unknown12) + ", "
                    + Float(a.Unknown13) + ", " + Id(a.Unknown14) + ", " + Int(a.Deadline) + ", " + Int(a.Unknown16) + ", " + Int((int)a.Unknown17.Type) + ", "
                    + Id(a.Playfield) + ", " + Int(a.Unknown18) + ", " + Int(a.Unknown19) + ", " + Float(a.X) + ", " + Float(a.Y) + ", " + Float(a.Z) + ");");
            }

            ordinal = 0;
            foreach (QuestItemShort reward in info.ItemRewards ?? new QuestItemShort[0])
            {
                this.Line(
                    "INSERT INTO questwirerewards (QuestId, Ordinal, LowId, HighId, Quality, Unknown1) VALUES (" + Int(id) + ", " + Int(ordinal++) + ", "
                    + Int(reward.LowId) + ", " + Int(reward.HighId) + ", " + Int(reward.Quality) + ", " + Int(reward.Unknown1) + ");");
            }
        }

        private static string Id(Identity identity)
        {
            return Int((int)identity.Type) + ", " + Int(identity.Instance);
        }

        #region Conversations

        private sealed class Node
        {
            public int Id;

            public readonly List<Entry> Says = new List<Entry>();

            public readonly List<string> Offers = new List<string>();

            public readonly List<string> Grants = new List<string>();

            public int TradeSlots;

            public string TradeText;

            public Node TradeNext;

            public readonly Dictionary<string, Node> Edges = new Dictionary<string, Node>(StringComparer.Ordinal);

            public readonly Dictionary<string, Node> Questions = new Dictionary<string, Node>(StringComparer.Ordinal);

            public Node AliasOf;

            public Node Resolve()
            {
                Node node = this;
                while (node.AliasOf != null)
                {
                    node = node.AliasOf;
                }

                return node;
            }

            public string Key()
            {
                return string.Join("\u0001", this.Says.Select(s => s.Text)) + "\u0002" + string.Join("\u0001", this.Offers) + "\u0002"
                       + string.Join("\u0001", this.Grants) + "\u0002" + this.TradeText;
            }
        }

        /// <summary>
        /// Stages a character has to do with: it is named as their giver in the quest log, one of
        /// them is finished by talking to it or handing it something, or its conversation grants one.
        /// </summary>
        private HashSet<string> RelatedStages(string npc)
        {
            var related = new HashSet<string>(StringComparer.Ordinal);
            foreach (Stage stage in this.model.Stages.Values)
            {
                if (stage.LogGiver == npc
                    || stage.Completions.Any(c => c.Trigger != null && c.Trigger.Name == npc
                                                  && (c.Trigger.Kind == "TalkOnOpen" || c.Trigger.Kind == "DialogueAnswer" || c.Trigger.Kind == "TradeHandIn"))
                    || stage.Grants.Any(g => g.Kind == "answer" && g.Npc == npc))
                {
                    related.Add(stage.Key);
                }
            }

            return related;
        }

        private void WriteConversations(string npc, List<Conversation> conversations)
        {
            var nodes = new Dictionary<string, Node>(StringComparer.Ordinal);
            var openers = new List<Tuple<Node, List<string>, List<string>>>();

            // Where a conversation carried on after a hand-in to offer another stage: the node, the stage
            // handed in, the stage offered.
            var resumes = new List<Tuple<Node, string, string>>();
            HashSet<string> related = this.RelatedStages(npc);
            var farewells = new Dictionary<string, List<Entry>>(StringComparer.Ordinal);
            var dialogueGrants = new HashSet<string>(
                this.model.Stages.Values.Where(s => s.Grants.Any(g => g.Kind == "answer" && g.Npc == npc) && this.playable.Contains(s.Key)).Select(s => s.Key),
                StringComparer.Ordinal);

            foreach (Conversation c in conversations)
            {
                if (c.Farewell.Count > 0)
                {
                    string key = string.Join("\u0001", c.Farewell.Select(f => f.Text));
                    farewells[key] = c.Farewell;
                }

                // The state the conversation starts in, after anything opening it finished and granted.
                var active = new List<string>(c.ActiveKeys);
                int index = 0;
                while (index < c.Entries.Count && c.Entries[index].Kind != "says" && c.Entries[index].Kind != "offers")
                {
                    Entry leading = c.Entries[index];
                    if (leading.Kind == "finishes")
                    {
                        active.Remove(leading.StageKey);
                    }
                    else if (leading.Kind == "grants")
                    {
                        active.Add(leading.StageKey);
                    }

                    index++;
                }

                List<Node> walk = this.Walk(c.Entries.Skip(index).ToList(), dialogueGrants, nodes, out List<string> granted, out List<Tuple<string, int, string>> resumed);
                if (walk.Count == 0)
                {
                    continue;
                }

                openers.Add(Tuple.Create(walk[0], active.Where(related.Contains).OrderBy(k => k, StringComparer.Ordinal).ToList(), granted));
                foreach (Tuple<string, int, string> resume in resumed.Where(r => r.Item2 < walk.Count))
                {
                    resumes.Add(Tuple.Create(walk[resume.Item2], resume.Item1, resume.Item3));
                }
            }

            // Questions: an answer whose reply offers the same answers without it.
            foreach (Node node in nodes.Values.ToList())
            {
                foreach (KeyValuePair<string, Node> edge in node.Edges.ToList())
                {
                    Node hub = node.Resolve();
                    Node reply = edge.Value.Resolve();
                    if (reply == hub || reply.Offers.Count == 0 || reply.Grants.Count > 0 || reply.TradeText != null)
                    {
                        continue;
                    }

                    List<string> removed = hub.Offers.Except(reply.Offers).ToList();
                    if (reply.Offers.Any(o => !hub.Offers.Contains(o)) || !removed.Contains(edge.Key)
                        || removed.Any(r => r != edge.Key && !hub.Questions.ContainsKey(r)))
                    {
                        continue;
                    }

                    var replyLines = new Node();
                    replyLines.Says.AddRange(reply.Says);
                    hub.Questions[edge.Key] = replyLines;
                    hub.Edges.Remove(edge.Key);
                    foreach (KeyValuePair<string, Node> further in reply.Edges)
                    {
                        if (!hub.Edges.ContainsKey(further.Key) && !hub.Questions.ContainsKey(further.Key))
                        {
                            hub.Edges[further.Key] = further.Value;
                        }
                    }

                    reply.AliasOf = hub;
                }
            }

            // While a stage this character granted in conversation is still on, it says again what it
            // said handing the stage over, without handing it over again. Retail repeats the text given
            // after accepting (owner's account of Rex Larsson); the node is the captured one.
            var fallbacks = new List<Tuple<Node, List<string>, List<string>>>();
            foreach (Node node in nodes.Values.Where(n => n.AliasOf == null && n.Grants.Count > 0).ToList())
            {
                foreach (string stage in node.Grants)
                {
                    if (openers.Any(o => o.Item2.Count == 1 && o.Item2[0] == stage))
                    {
                        continue;
                    }

                    var repeat = new Node();
                    repeat.Says.AddRange(node.Says);
                    repeat.Offers.AddRange(node.Offers.Where(o => o == "Goodbye"));
                    if (repeat.Offers.Count == 0)
                    {
                        repeat.Offers.Add("Goodbye");
                    }

                    nodes["repeat:" + stage] = repeat;
                    fallbacks.Add(Tuple.Create(repeat, new List<string> { stage }, new List<string>()));
                }
            }

            List<Node> all = nodes.Values.Where(n => n.AliasOf == null).ToList();
            int nextId = 1;
            foreach (Node node in all)
            {
                node.Id = nextId++;
            }

            this.Line("-- Conversations of " + npc + "  [" + string.Join(", ", conversations.Select(c => c.Evidence).Take(6)) + (conversations.Count > 6 ? ", ..." : string.Empty) + "]");

            // A question is answered the same wherever it is asked - Marcus Stone's "Who are you?" at his
            // greeting and again after the hand-in: one reply node, offered by every node that has it.
            var questionIds = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (Node node in all)
            {
                foreach (KeyValuePair<string, Node> question in node.Questions)
                {
                    if (questionIds.ContainsKey(question.Key))
                    {
                        continue;
                    }

                    int questionNode = nextId++;
                    questionIds[question.Key] = questionNode;
                    int replyOrdinal = 0;
                    foreach (Entry say in question.Value.Says)
                    {
                        this.Dialogue(npc, questionNode, replyOrdinal++, 0, say.Text, say.Flag, 0, 0, 0);
                    }
                }
            }
            foreach (Node node in all)
            {
                int ordinal = 0;
                foreach (Entry say in node.Says)
                {
                    this.Dialogue(npc, node.Id, ordinal++, 0, say.Text, say.Flag, 0, 0, 0);
                }

                foreach (string grant in node.Grants)
                {
                    this.Dialogue(npc, node.Id, ordinal++, 2, this.model.NameOf(grant), 0, 0, 0, this.ids[grant]);
                }

                if (node.TradeText != null)
                {
                    Node after = node.TradeNext == null ? null : node.TradeNext.Resolve();
                    this.Dialogue(npc, node.Id, ordinal++, 3, node.TradeText, node.TradeSlots, after == null ? 0 : after.Id, 0, 0);
                }

                foreach (string offer in node.Offers)
                {
                    int questionId;
                    Node target;
                    if (offer == "Goodbye")
                    {
                        this.Dialogue(npc, node.Id, ordinal++, 1, offer, 0, 0, 2, 0);
                    }
                    else if (node.Edges.TryGetValue(offer, out target))
                    {
                        this.Dialogue(npc, node.Id, ordinal++, 1, offer, 0, target.Resolve().Id, 0, 0);
                    }
                    else if (questionIds.TryGetValue(offer, out questionId))
                    {
                        this.Dialogue(npc, node.Id, ordinal++, 1, offer, 0, questionId, 1, 0);
                    }
                    else
                    {
                        this.Dialogue(npc, node.Id, ordinal++, 1, offer, 0, 0, 0, 0);
                    }
                }
            }

            List<Entry> farewell = farewells.Values.OrderByDescending(f => f.Count).FirstOrDefault();
            if (farewell != null)
            {
                int ordinal = 0;
                foreach (Entry say in farewell)
                {
                    this.Dialogue(npc, -1, ordinal++, 0, say.Text, say.Flag, 0, 0, 0);
                }
            }

            var written = new HashSet<string>(StringComparer.Ordinal);
            foreach (Tuple<Node, List<string>, List<string>> opener in openers.Select(o => o).Concat(fallbacks))
            {
                bool fallback = fallbacks.Contains(opener);
                Node start = opener.Item1.Resolve();
                string active = string.Join(",", opener.Item2.Select(k => Int(this.ids[k])));
                string forbid = string.Join(",", opener.Item3.Select(k => Int(this.ids[k])));
                if (!written.Add(active + "|" + forbid + "|" + start.Id))
                {
                    continue;
                }

                int priority = (fallback ? 50 : 100) + (opener.Item2.Count * 10);
                if (fallback)
                {
                    this.Line("-- OmniCell-defined: repeats the captured hand-over text while the stage is on (owner's account).");
                }

                this.Line(
                    "INSERT INTO knubotopeners (Playfield, NpcName, Priority, RequireActive, RequireDone, ForbidStarted, Node) VALUES ("
                    + Int(this.playfield) + ", " + Text(npc) + ", " + Int(priority) + ", " + Text(active) + ", '', " + Text(forbid) + ", " + Int(start.Id) + ");");
            }

            foreach (Tuple<Node, string, string> resume in resumes)
            {
                Node start = resume.Item1.Resolve();
                string done = Int(this.ids[resume.Item2]);
                string offered = Int(this.ids[resume.Item3]);
                if (start.Id == 0 || !written.Add("resume|" + done + "|" + offered + "|" + start.Id))
                {
                    continue;
                }

                this.Line("-- OmniCell-defined: retail offers the next stage in the same window after the hand-in; opened again");
                this.Line("-- before taking it, the conversation resumes past the thanks (Marcus Stone's stim, 20260914-124401).");
                this.Line(
                    "INSERT INTO knubotopeners (Playfield, NpcName, Priority, RequireActive, RequireDone, ForbidStarted, Node) VALUES ("
                    + Int(this.playfield) + ", " + Text(npc) + ", 90, '', " + Text(done) + ", " + Text(offered) + ", " + Int(start.Id) + ");");
            }

            this.Line();
        }

        /// <summary>
        /// One captured conversation as nodes: what the character says, does and offers, and where each
        /// chosen answer led.
        /// </summary>
        private List<Node> Walk(
            List<Entry> entries,
            HashSet<string> dialogueGrants,
            Dictionary<string, Node> nodes,
            out List<string> granted,
            out List<Tuple<string, int, string>> resumed)
        {
            granted = new List<string>();
            resumed = new List<Tuple<string, int, string>>();
            var finishes = new List<Tuple<string, int>>();
            var walk = new List<Node>();
            var current = new Node();
            Node previous = null;
            string chosen = null;
            bool previousWasTrade = false;

            Action close = () =>
                {
                    if (current.Says.Count == 0 && current.Offers.Count == 0 && current.Grants.Count == 0 && current.TradeText == null)
                    {
                        return;
                    }

                    string key = current.Key();
                    Node existing;
                    if (!nodes.TryGetValue(key, out existing))
                    {
                        nodes[key] = current;
                        existing = current;
                    }
                    else
                    {
                        existing.TradeNext = existing.TradeNext ?? current.TradeNext;
                    }

                    if (previous != null)
                    {
                        if (previousWasTrade)
                        {
                            previous.TradeNext = existing;
                        }
                        else if (chosen != null && !previous.Edges.ContainsKey(chosen))
                        {
                            previous.Edges[chosen] = existing;
                        }
                    }

                    walk.Add(existing);
                    previousWasTrade = existing.TradeText != null && existing.Offers.Count == 0;
                    previous = existing;
                    chosen = null;
                    current = new Node();
                };

            foreach (Entry entry in entries)
            {
                switch (entry.Kind)
                {
                    case "says":
                        if (current.Offers.Count > 0 || current.TradeText != null)
                        {
                            close();
                        }

                        current.Says.Add(entry);
                        break;
                    case "offers":
                        current.Offers.AddRange(entry.Answers);
                        break;
                    case "chooses":
                        close();
                        chosen = entry.Text;
                        break;
                    case "finishes":
                        // The node after the one finishing it: the thanks and the reward talk.
                        bool emptyNow = current.Says.Count == 0 && current.Offers.Count == 0 && current.Grants.Count == 0 && current.TradeText == null;
                        finishes.Add(Tuple.Create(entry.StageKey, walk.Count + (emptyNow ? 0 : 1)));
                        break;
                    case "grants":
                        if (dialogueGrants.Contains(entry.StageKey))
                        {
                            current.Grants.Add(entry.StageKey);
                            granted.Add(entry.StageKey);

                            // Offered later in a conversation that finished a stage: coming back before taking
                            // it resumes one node past the thanks ("Was there anything else?").
                            foreach (Tuple<string, int> finish in finishes)
                            {
                                if (finish.Item2 + 1 <= walk.Count)
                                {
                                    resumed.Add(Tuple.Create(finish.Item1, finish.Item2 + 1, entry.StageKey));
                                }
                            }
                        }

                        break;
                    case "trade":
                        current.TradeText = entry.Text;
                        current.TradeSlots = entry.Flag;
                        break;
                }
            }

            close();
            return walk;
        }

        private void Dialogue(string npc, int node, int ordinal, int kind, string text, int flag, int next, int answerKind, int actionValue)
        {
            this.Line(
                "INSERT INTO knubotdialogue (Playfield, NpcName, Node, Ordinal, Kind, Text, Flag, Next, AnswerKind, ActionValue) VALUES ("
                + Int(this.playfield) + ", " + Text(npc) + ", " + Int(node) + ", " + Int(ordinal) + ", " + Int(kind) + ", " + Text(text) + ", "
                + Int(flag) + ", " + Int(next) + ", " + Int(answerKind) + ", " + Int(actionValue) + ");");
        }

        #endregion

        private void WriteFixtures()
        {
            // Every fixture a player used is cleared, so one an earlier run wrote and this run does not is gone.
            List<FixtureKind> touched = this.model.Fixtures.Values.Where(k => k.Uses > 0).OrderBy(k => k.Template).ToList();
            List<FixtureKind> used = touched.Where(k => k.DespawnsAfterUse > 0).ToList();
            if (touched.Count == 0)
            {
                return;
            }

            this.Line("-- Fixtures that go away when used and come back at the same position. The delay is OmniCell-defined:");
            this.Line("-- the captures have no timestamps yet.");
            this.Line("DELETE FROM fixturebehaviours WHERE Template IN (" + string.Join(", ", touched.Select(k => Int(k.Template))) + ");");
            foreach (FixtureKind kind in used)
            {
                string feedback = kind.FeedbackRaw.OrderByDescending(f => f.Value).Select(f => f.Key).FirstOrDefault() ?? string.Empty;
                this.Line(
                    "INSERT INTO fixturebehaviours (Template, DespawnOnUse, RespawnSeconds, Feedback) VALUES (" + Int(kind.Template) + ", 1, "
                    + Int(DefaultRespawnSeconds) + ", " + Text(feedback) + ");");
            }
        }
    }

    #endregion
}
