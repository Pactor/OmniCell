// WireAudit - which of our message models actually match the wire.
//
//   WireAudit <streams.csv> [<streams.csv> ...] [--type Name] [--show N]
//
// The protocol index calls a packet green when every byte of it is identified.
// This is how a packet earns that, in bulk: take a packet the live server
// really sent, read it into our model, write the model back out, and compare
// the bytes. If they differ, our model is wrong - it has invented a field,
// missed one, got a width wrong, or silently dropped a tail. There is no
// arguing with it, because retail wrote the input.
//
// It is not proof of understanding. A model can round-trip a packet whose
// fields are all called Unknown7, and several here do; that earns amber, not
// green. What it does prove is the shape: the field count, the widths, the
// order, and that nothing was thrown away. A packet that fails this cannot be
// green no matter how well its fields are named, and a packet that passes it
// can never be silently corrupting the stream.
//
// Both directions are audited. The client is as much an authority on the
// layout as the server, and some messages only ever travel one way.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

using ICSharpCode.SharpZipLib.Zip.Compression;

internal static class WireAudit
{
    private const int HeaderLength = 16;

    private const int SizeOffset = 6;

    private static MethodInfo deserialize;

    private static MethodInfo serialize;

    private static object serializer;

    private sealed class Result
    {
        public string Name = string.Empty;

        public int Seen;

        public int Matched;

        public readonly List<string[]> Failures = new List<string[]>();

        /// <summary>
        /// How often each byte offset came out wrong, and how many packets had
        /// a length our writer disagreed with.
        /// </summary>
        public readonly Dictionary<int, int> WrongAt = new Dictionary<int, int>();

        public int WrongLength;

        /// <summary>
        /// Every distinct value each field took, so a field that never varies
        /// can be documented as the constant it is.
        /// </summary>
        public readonly Dictionary<string, Dictionary<string, int>> Values =
            new Dictionary<string, Dictionary<string, int>>();

        /// <summary>
        /// Which named fields came back different, and how often.
        /// </summary>
        public readonly Dictionary<string, int> WrongField = new Dictionary<string, int>();
    }

    private static short BigEndianInt16(byte[] data, int offset)
    {
        return (short)((data[offset] << 8) | data[offset + 1]);
    }

    private static object Get(object o, string name)
    {
        if (o == null)
        {
            return null;
        }

        PropertyInfo p = o.GetType().GetProperty(name);
        return p == null ? null : p.GetValue(o, null);
    }

    /// <summary>
    /// Where two byte strings first differ, and what they look like there.
    /// </summary>
    private static string Difference(byte[] expected, byte[] actual)
    {
        int at = 0;
        while (at < expected.Length && at < actual.Length && expected[at] == actual[at])
        {
            at++;
        }

        if (at >= expected.Length && at >= actual.Length)
        {
            return "identical";
        }

        int from = Math.Max(0, at - 4);
        return "first differs at byte " + at + " of " + expected.Length + "/" + actual.Length
               + "  retail " + Window(expected, from) + "  ours " + Window(actual, from);
    }

    private static string Window(byte[] data, int from)
    {
        var bytes = new List<string>();
        for (int i = from; i < Math.Min(data.Length, from + 12); i++)
        {
            bytes.Add(data[i].ToString("X2"));
        }

        return string.Join("-", bytes.ToArray()) + (from + 12 < data.Length ? ".." : string.Empty);
    }

    /// <summary>
    /// Records an unreadable packet under the message id it claims to be.
    /// </summary>
    /// <remarks>
    /// The id is a big-endian int32 at offset 16, straight after the sixteen
    /// byte envelope. A packet too short to have one is filed under zero.
    /// </remarks>
    private static void Unreadable(
        byte[] packet,
        Dictionary<uint, int> counts,
        Dictionary<uint, List<byte[]>> samples,
        Dictionary<uint, Dictionary<int, int>> lengths)
    {
        uint id = 0;
        if (packet.Length >= 20)
        {
            id = ((uint)packet[16] << 24) | ((uint)packet[17] << 16) | ((uint)packet[18] << 8) | packet[19];
        }

        if (counts.ContainsKey(id))
        {
            counts[id]++;
        }
        else
        {
            counts[id] = 1;
            samples[id] = new List<byte[]>();
            lengths[id] = new Dictionary<int, int>();
        }

        // A few of each, so a shape that only appears once is still reachable
        if (samples[id].Count < 4 && !samples[id].Any(s => s.Length == packet.Length))
        {
            samples[id].Add(packet);
        }

        Dictionary<int, int> byLength = lengths[id];
        byLength[packet.Length] = byLength.ContainsKey(packet.Length) ? byLength[packet.Length] + 1 : 1;
    }

    private static void Main(string[] args)
    {
        if (args.Length < 1)
        {
            Console.WriteLine("usage: WireAudit <streams.csv> [...] [--type Name] [--show N] [--values [--top N] [--elements N]] [--where Field=Value]");
            return;
        }

        string only = null;
        uint unknownId = 0xFFFFFFFF;
        int show = 1;
        int dump = 0;
        int sample = 0;
        bool values = false;
        int top = 4;
        string whereField = null;
        string whereValue = null;
        var inputs = new List<string>();
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "--type" && i + 1 < args.Length)
            {
                only = args[++i];
            }
            else if (args[i] == "--unknown" && i + 1 < args.Length)
            {
                unknownId = Convert.ToUInt32(args[++i], 16);
            }
            else if (args[i] == "--values")
            {
                values = true;
            }
            else if (args[i] == "--where" && i + 1 < args.Length)
            {
                // --where Action=35 prints every packet whose Action reads 35,
                // with the direction it travelled. A field's value is one
                // question; which way it was going is usually the next one.
                string[] halves = args[++i].Split('=');
                whereField = halves[0];
                whereValue = halves.Length > 1 ? halves[1] : string.Empty;
            }
            else if (args[i] == "--elements" && i + 1 < args.Length)
            {
                // How far into a list to walk. Two is enough to see an entry's
                // shape; a character's stat block needs all of them.
                arrayLimit = int.Parse(args[++i]);
            }
            else if (args[i] == "--top" && i + 1 < args.Length)
            {
                // How many of a field's values to print. Four is enough to see
                // whether a field varies; a stat list needs all of them.
                top = int.Parse(args[++i]);
            }
            else if (args[i] == "--sample" && i + 1 < args.Length)
            {
                sample = int.Parse(args[++i]);
            }
            else if (args[i] == "--dump" && i + 1 < args.Length)
            {
                dump = int.Parse(args[++i]);
            }
            else if (args[i] == "--show" && i + 1 < args.Length)
            {
                show = int.Parse(args[++i]);
            }
            else
            {
                inputs.Add(args[i]);
            }
        }

        // Beside the exe, not wherever it happens to be run from.
        Assembly messaging = Assembly.LoadFrom(
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "SmokeLounge.AOtomation.Messaging.dll"));
        Type serializerType = messaging.GetType("SmokeLounge.AOtomation.Messaging.Serialization.MessageSerializer");
        serializer = Activator.CreateInstance(serializerType);
        deserialize = serializerType.GetMethod("Deserialize", new[] { typeof(Stream) });
        serialize = serializerType.GetMethod(
            "Serialize",
            new[] { typeof(Stream), messaging.GetType("SmokeLounge.AOtomation.Messaging.Messages.Message") });

        var results = new Dictionary<string, Result>();

        // What the unreadable ones are, keyed by the N3 message id at offset
        // 16. A packet nothing can parse is either a message we have no class
        // for at all or one whose reader gives up part way, and those are very
        // different problems - the count on its own cannot tell them apart.
        var unknown = new Dictionary<uint, int>();
        var unknownSample = new Dictionary<uint, List<byte[]>>();
        var unknownLengths = new Dictionary<uint, Dictionary<int, int>>();
        int unreadable = 0;
        int total = 0;

        foreach (string input in inputs)
        {
            foreach (var carried in Packets(input))
            {
                byte[] packet = carried.Value;
                total++;
                object message;
                try
                {
                    using (var ms = new MemoryStream(packet))
                    {
                        message = deserialize.Invoke(serializer, new object[] { ms });
                    }
                }
                catch (Exception)
                {
                    Unreadable(packet, unknown, unknownSample, unknownLengths);
                    unreadable++;
                    continue;
                }

                object body = message == null ? null : Get(message, "Body");
                if (body == null)
                {
                    Unreadable(packet, unknown, unknownSample, unknownLengths);
                    unreadable++;
                    continue;
                }

                string name = body.GetType().Name;
                if (only != null && name != only)
                {
                    continue;
                }

                Result result;
                if (!results.TryGetValue(name, out result))
                {
                    result = new Result { Name = name };
                    results[name] = result;
                }

                result.Seen++;

                if (whereField != null)
                {
                    object held = Get(body, whereField);
                    if (held != null && held.ToString() == whereValue)
                    {
                        Console.WriteLine("### " + carried.Key + ", " + name + ", "
                                          + whereField + " = " + whereValue + ", "
                                          + packet.Length + " bytes");
                        Console.WriteLine(Spaced(packet));
                        Console.WriteLine();
                    }
                }

                if (values)
                {
                    Collect(body, string.Empty, result.Values, 0);
                }

                // Retail hex for a construction test, whether or not our model
                // round-trips it. This is the raw material for
                // ProtocolByteAuditTests: paste the hex, build the body, assert.
                if (sample > 0)
                {
                    sample--;
                    Console.WriteLine("### " + name + ", " + packet.Length + " bytes");
                    Console.WriteLine(Spaced(packet));
                    Console.WriteLine();
                }

                byte[] rewritten;
                try
                {
                    using (var ms = new MemoryStream())
                    {
                        serialize.Invoke(serializer, new[] { ms, message });
                        rewritten = ms.ToArray();
                    }
                }
                catch (Exception e)
                {
                    if (result.Failures.Count < show)
                    {
                        result.Failures.Add(new[] { "threw on write: " + Innermost(e) });
                    }

                    continue;
                }

                if (Same(packet, rewritten))
                {
                    result.Matched++;
                }
                else
                {
                    if (packet.Length != rewritten.Length)
                    {
                        result.WrongLength++;
                    }

                    int common = Math.Min(packet.Length, rewritten.Length);
                    for (int at = 0; at < common; at++)
                    {
                        if (packet[at] != rewritten[at])
                        {
                            result.WrongAt[at] = result.WrongAt.ContainsKey(at) ? result.WrongAt[at] + 1 : 1;
                        }
                    }

                    // Read our own bytes back and see which fields moved. A
                    // byte offset says something is wrong; a field name says
                    // what.
                    try
                    {
                        using (var ms = new MemoryStream(rewritten))
                        {
                            object echo = deserialize.Invoke(serializer, new object[] { ms });
                            Compare(Get(echo, "Body"), body, string.Empty, result.WrongField, 0);
                        }
                    }
                    catch (Exception)
                    {
                        Note(result.WrongField, "<our bytes will not read back>");
                    }

                    if (dump > 0)
                    {
                        dump--;
                        Console.WriteLine("### retail packet, " + packet.Length + " bytes");
                        Console.WriteLine(Hex(packet));
                        Console.WriteLine("### our rewrite, " + rewritten.Length + " bytes");
                        Console.WriteLine(Hex(rewritten));
                        Console.WriteLine();
                    }

                    if (result.Failures.Count < show)
                    {
                        result.Failures.Add(new[] { Difference(packet, rewritten) });
                    }
                }
            }
        }

        Console.WriteLine(
            total + " packets read, " + unreadable + " unreadable, " + results.Count + " message types");
        Console.WriteLine();

        if (unknown.Count > 0)
        {
            Console.WriteLine("--- could not be read at all, by message id (" + unknown.Count + " distinct)");
            foreach (var pair in unknown.OrderByDescending(p => p.Value))
            {
                List<byte[]> some = unknownSample[pair.Key];
                Console.WriteLine(
                    string.Format(
                        "0x{0:X8} {1,6} packets   {2}",
                        pair.Key,
                        pair.Value,
                        Sizes(unknownLengths[pair.Key])));

                if (unknownId == pair.Key)
                {
                    foreach (byte[] one in some)
                    {
                        Console.WriteLine();
                        Console.WriteLine("  " + one.Length + " bytes");
                        Console.WriteLine(Hex(one));
                    }

                    Console.WriteLine();
                }
            }

            Console.WriteLine();
        }

        var bad = results.Values.Where(r => r.Matched != r.Seen).OrderByDescending(r => r.Seen - r.Matched).ToList();
        var good = results.Values.Where(r => r.Matched == r.Seen).OrderByDescending(r => r.Seen).ToList();

        Console.WriteLine("--- does not survive a round trip (" + bad.Count + ")");
        foreach (Result r in bad)
        {
            Console.WriteLine(
                string.Format("{0,-34} {1,5} seen  {2,5} match  {3,5} differ", r.Name, r.Seen, r.Matched, r.Seen - r.Matched));
            foreach (string[] failure in r.Failures)
            {
                Console.WriteLine("      " + failure[0]);
            }

            if (r.WrongLength > 0)
            {
                Console.WriteLine("      wrong length in " + r.WrongLength + " of them");
            }

            if (r.WrongField.Count > 0)
            {
                foreach (var field in r.WrongField.OrderByDescending(kv => kv.Value).Take(10))
                {
                    Console.WriteLine("      field " + field.Key + " (x" + field.Value + ")");
                }
            }

            if (r.WrongAt.Count > 0)
            {
                var offsets = r.WrongAt.OrderBy(kv => kv.Key)
                    .Select(kv => kv.Key + "(x" + kv.Value + ")")
                    .Take(14)
                    .ToArray();
                Console.WriteLine(
                    "      wrong at byte " + string.Join(", ", offsets)
                    + (r.WrongAt.Count > 14 ? " and " + (r.WrongAt.Count - 14) + " more" : string.Empty));
            }
        }

        if (values)
        {
            foreach (Result r in results.Values)
            {
                Console.WriteLine();
                Console.WriteLine("--- field values for " + r.Name + " (" + r.Seen + " copies)");
                foreach (var field in r.Values)
                {
                    var shown = field.Value.OrderByDescending(kv => kv.Value).Take(top)
                        .Select(kv => kv.Key + " x" + kv.Value).ToArray();
                    Console.WriteLine(
                        string.Format("{0,-46} {1,4} distinct   {2}", field.Key, field.Value.Count,
                            string.Join(", ", shown)));
                }
            }

            return;
        }

        Console.WriteLine();
        Console.WriteLine("--- byte-exact round trip (" + good.Count + ")");
        foreach (Result r in good)
        {
            Console.WriteLine(string.Format("{0,-34} {1,5} seen", r.Name, r.Seen));
        }
    }

    /// <summary>
    /// Retail bytes as the hex string a construction test pastes in.
    /// </summary>
    private static string Spaced(byte[] data)
    {
        var parts = new List<string>();
        foreach (byte b in data)
        {
            parts.Add(b.ToString("X2"));
        }

        var lines = new List<string>();
        for (int i = 0; i < parts.Count; i += 20)
        {
            lines.Add("\"" + string.Join(" ", parts.Skip(i).Take(20).ToArray()) + " \" +");
        }

        return string.Join(Environment.NewLine, lines.ToArray());
    }

    /// <summary>
    /// A length histogram, shortest first, so a fixed size packet is obvious.
    /// </summary>
    private static string Sizes(Dictionary<int, int> lengths)
    {
        var parts = new List<string>();
        foreach (var pair in lengths.OrderByDescending(p => p.Value).Take(4))
        {
            parts.Add(pair.Key + "b x" + pair.Value);
        }

        if (lengths.Count > 4)
        {
            parts.Add("+" + (lengths.Count - 4) + " more");
        }

        return string.Join(", ", parts.ToArray());
    }

    /// <summary>
    /// The first few bytes on one line, for a table rather than a dump.
    /// </summary>
    private static string Flat(byte[] data, int count)
    {
        var row = new List<string>();
        for (int i = 0; i < count && i < data.Length; i++)
        {
            row.Add(data[i].ToString("X2"));
        }

        return string.Join(" ", row.ToArray());
    }

    private static string Hex(byte[] data)
    {
        var lines = new List<string>();
        for (int i = 0; i < data.Length; i += 16)
        {
            var row = new List<string>();
            for (int j = i; j < Math.Min(data.Length, i + 16); j++)
            {
                row.Add(data[j].ToString("X2"));
            }

            lines.Add(i.ToString("0000") + "  " + string.Join(" ", row.ToArray()));
        }

        return string.Join(Environment.NewLine, lines.ToArray());
    }

    /// <summary>
    /// Record what every leaf field of a message held.
    /// </summary>
    /// <remarks>
    /// A field that is the same in every captured copy can be written down as
    /// that constant and honestly called identified. A field that varies needs
    /// a reason, and this says which is which without reading a thousand
    /// packets by hand.
    /// </remarks>
    private static int arrayLimit = 2;

    private static void Collect(object body, string path, Dictionary<string, Dictionary<string, int>> into, int depth)
    {
        // Five rather than three because a record inside a record inside a
        // list is now an ordinary shape here: a SpellList holds effects, an
        // effect holds criteria, and a criterion's own fields are five steps
        // down. Arrays stop after two elements unless --elements says
        // otherwise, so the walk stays small by default.
        if (body == null || depth > 5)
        {
            return;
        }

        Type type = body.GetType();
        if (type.IsPrimitive || type.IsEnum || body is string || body is decimal)
        {
            if (!into.ContainsKey(path))
            {
                into[path] = new Dictionary<string, int>();
            }

            string key = body.ToString();
            if (into[path].Count < 400)
            {
                into[path][key] = into[path].ContainsKey(key) ? into[path][key] + 1 : 1;
            }

            return;
        }

        var array = body as Array;
        if (array != null)
        {
            Collect(array.Length, path + ".Length", into, depth);
            for (int i = 0; i < Math.Min(array.Length, arrayLimit); i++)
            {
                // Elements share one path by default, which is what makes the
                // summary readable. Once --elements asks for the whole list the
                // question has usually become "which value went with which", so
                // index them and keep the pairing.
                string element = arrayLimit > 2 ? path + "[" + i + "]" : path + "[]";
                Collect(array.GetValue(i), element, into, depth + 1);
            }

            return;
        }

        foreach (PropertyInfo property in type.GetProperties())
        {
            if (property.GetIndexParameters().Length > 0 || property.Name == "PacketType")
            {
                continue;
            }

            object value;
            try
            {
                value = property.GetValue(body, null);
            }
            catch (Exception)
            {
                continue;
            }

            Collect(value, path.Length == 0 ? property.Name : path + "." + property.Name, into, depth + 1);
        }
    }

    private static void Note(Dictionary<string, int> into, string what)
    {
        into[what] = into.ContainsKey(what) ? into[what] + 1 : 1;
    }

    /// <summary>
    /// Walk two message bodies side by side and record every field that does
    /// not survive the round trip.
    /// </summary>
    /// <remarks>
    /// Ours first, retail second. Depth limited, because a few of these nest
    /// several arrays deep and the point is to name the field, not to print the
    /// packet.
    /// </remarks>
    private static void Compare(object ours, object retail, string path, Dictionary<string, int> into, int depth)
    {
        if (depth > 4 || (ours == null && retail == null))
        {
            return;
        }

        if (ours == null || retail == null)
        {
            Note(into, path + (ours == null ? " (we dropped it)" : " (we invented it)"));
            return;
        }

        Type type = retail.GetType();
        if (type.IsPrimitive || type.IsEnum || retail is string || retail is decimal)
        {
            if (!retail.Equals(ours))
            {
                Note(into, path + "  retail=" + retail + " ours=" + ours);
            }

            return;
        }

        var retailArray = retail as Array;
        var ourArray = ours as Array;
        if (retailArray != null || ourArray != null)
        {
            int retailLength = retailArray == null ? -1 : retailArray.Length;
            int ourLength = ourArray == null ? -1 : ourArray.Length;
            if (retailLength != ourLength)
            {
                Note(into, path + "[]  retail has " + retailLength + " ours has " + ourLength);
                return;
            }

            for (int i = 0; i < retailLength && i < 4; i++)
            {
                Compare(ourArray.GetValue(i), retailArray.GetValue(i), path + "[" + i + "]", into, depth + 1);
            }

            return;
        }

        foreach (PropertyInfo property in type.GetProperties())
        {
            if (property.GetIndexParameters().Length > 0)
            {
                continue;
            }

            object retailValue;
            object ourValue;
            try
            {
                retailValue = property.GetValue(retail, null);
                ourValue = property.GetValue(ours, null);
            }
            catch (Exception)
            {
                continue;
            }

            Compare(ourValue, retailValue, path.Length == 0 ? property.Name : path + "." + property.Name, into, depth + 1);
        }
    }

    private static string Innermost(Exception e)
    {
        while (e.InnerException != null)
        {
            e = e.InnerException;
        }

        return e.GetType().Name + ": " + e.Message;
    }

    private static bool Same(byte[] a, byte[] b)
    {
        if (a.Length != b.Length)
        {
            return false;
        }

        for (int i = 0; i < a.Length; i++)
        {
            if (a[i] != b[i])
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Every framed packet in a capture, both directions.
    /// </summary>
    /// <remarks>
    /// The server side is one zlib stream and the client side is plaintext, so
    /// they are gathered separately and framed separately. Client to server is
    /// padded to a four byte boundary and server to client is not, which is why
    /// the alignment is worked out per direction rather than assumed.
    /// </remarks>
    private static IEnumerable<KeyValuePair<string, byte[]>> Packets(string input)
    {
        var byStream = new Dictionary<string, List<byte>>();
        foreach (string line in File.ReadAllLines(input))
        {
            string[] parts = line.Split(',');
            if (parts.Length < 3 || parts[2].Length < 2)
            {
                continue;
            }

            string key = parts[0] + "/" + parts[1];
            if (!byStream.ContainsKey(key))
            {
                byStream[key] = new List<byte>();
            }

            string hex = parts[2].Replace(":", string.Empty).Trim();
            for (int i = 0; i + 1 < hex.Length; i += 2)
            {
                byStream[key].Add(Convert.ToByte(hex.Substring(i, 2), 16));
            }
        }

        foreach (var entry in byStream)
        {
            // "stream/direction" - the direction half is what --where reports.
            string direction = entry.Key.Substring(entry.Key.IndexOf('/') + 1);
            byte[] raw = entry.Value.ToArray();
            byte[] data = raw;

            for (int start = 0; start + 1 < Math.Min(raw.Length, 4096); start++)
            {
                if ((raw[start] & 0x0F) != 8 || (((raw[start] << 8) | raw[start + 1]) % 31) != 0)
                {
                    continue;
                }

                byte[] candidate = InflateAll(raw, start);
                if (candidate.Length > raw.Length / 2)
                {
                    data = candidate;
                    break;
                }
            }

            foreach (int alignment in new[] { 1, 4 })
            {
                var found = new List<byte[]>();
                int pos = 0;
                while (pos + HeaderLength <= data.Length)
                {
                    short size = BigEndianInt16(data, pos + SizeOffset);
                    if (size < HeaderLength || pos + size > data.Length)
                    {
                        pos++;
                        found.Clear();
                        continue;
                    }

                    var packet = new byte[size];
                    Array.Copy(data, pos, packet, 0, size);
                    found.Add(packet);
                    pos += size;
                    if (alignment > 1 && size % alignment != 0)
                    {
                        pos += alignment - (size % alignment);
                    }
                }

                if (found.Count > 0)
                {
                    foreach (byte[] packet in found)
                    {
                        yield return new KeyValuePair<string, byte[]>(direction, packet);
                    }

                    break;
                }
            }
        }
    }

    private static byte[] InflateAll(byte[] input, int startOffset)
    {
        using (var result = new MemoryStream())
        {
            int pos = startOffset;
            while (pos + 2 < input.Length)
            {
                if ((input[pos] & 0x0F) != 8 || (((input[pos] << 8) | input[pos + 1]) % 31) != 0)
                {
                    pos++;
                    continue;
                }

                var inflater = new Inflater(false);
                inflater.SetInput(input, pos, input.Length - pos);
                var buffer = new byte[65536];
                long before = result.Length;

                while (!inflater.IsFinished && !inflater.IsNeedingInput && !inflater.IsNeedingDictionary)
                {
                    int produced;
                    try
                    {
                        produced = inflater.Inflate(buffer);
                    }
                    catch (Exception)
                    {
                        break;
                    }

                    if (produced <= 0)
                    {
                        break;
                    }

                    result.Write(buffer, 0, produced);
                }

                if (result.Length == before)
                {
                    pos++;
                    continue;
                }

                break;
            }

            return result.ToArray();
        }
    }
}
