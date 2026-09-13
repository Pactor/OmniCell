using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

using ICSharpCode.SharpZipLib.Zip.Compression;

// Decodes an Anarchy Online TCP stream into named protocol messages.
//
// Input is a text file produced by extract-stream.bat, one line per TCP segment:
//     <stream>,<direction>,<hex payload>
//
// The zone stream is zlib compressed after SendInitiateCompressionMessage
// negotiates it, so each direction is tried as plaintext first and then as a
// zlib stream from the point framing breaks down. The compression uses flush
// framing rather than a terminated stream, which is why inflation is done
// incrementally and a truncated tail is treated as the end rather than an error.
//
// Framing: every packet starts with a 16 byte big-endian header and Size at
// offset 6 gives the total packet length.
internal static class PcapDecode
{
    private const int HeaderLength = 16;
    private const int SizeOffset = 6;

    private static short BigEndianInt16(byte[] data, int offset)
    {
        return (short)((data[offset] << 8) | data[offset + 1]);
    }

    /// <summary>
    /// Reads the N3 message id, which sits just past the header.
    /// </summary>
    private static int BigEndianInt32(byte[] data, int offset)
    {
        if (offset + 4 > data.Length)
        {
            return 0;
        }

        return (data[offset] << 24) | (data[offset + 1] << 16) | (data[offset + 2] << 8) | data[offset + 3];
    }

    internal static string LastInflateStop = "";

    private static string DescribeMessage(object body, byte[] packet)
    {
        var sb = new StringBuilder(body == null ? "<null body>" : body.GetType().Name);
        if (body != null)
        {
            foreach (PropertyInfo property in body.GetType().GetProperties())
            {
                if (property.Name == "PacketType" || property.Name == "N3MessageType"
                    || property.Name == "SystemMessageType")
                {
                    continue;
                }

                object value;
                try
                {
                    value = property.GetValue(body, null);
                }
                catch
                {
                    continue;
                }

                var array = value as Array;
                sb.Append(' ').Append(property.Name).Append('=');
                sb.Append(array == null ? value : "[" + array.Length + "]");
            }
        }

        sb.Append(" raw=").Append(BitConverter.ToString(packet));
        return sb.ToString();
    }

    private static string DescribeSimpleItem(object body, byte[] packet)
    {
        var sb = new StringBuilder("SimpleItemFullUpdateMessage");
        foreach (PropertyInfo property in body.GetType().GetProperties())
        {
            if (property.Name == "PacketType" || property.Name == "N3MessageType"
                || property.Name == "SystemMessageType")
            {
                continue;
            }

            object value;
            try
            {
                value = property.GetValue(body, null);
            }
            catch
            {
                continue;
            }

            sb.Append(' ').Append(property.Name).Append('=');
            var array = value as Array;
            if (property.Name != "Stats" || array == null)
            {
                if (value != null && (property.Name == "Coordinate" || property.Name == "Heading"))
                {
                    sb.Append('{');
                    bool firstComponent = true;
                    foreach (PropertyInfo component in value.GetType().GetProperties())
                    {
                        if (!component.CanRead || component.GetIndexParameters().Length != 0)
                        {
                            continue;
                        }

                        if (!firstComponent)
                        {
                            sb.Append(", ");
                        }

                        sb.Append(component.Name).Append('=').Append(component.GetValue(value, null));
                        firstComponent = false;
                    }

                    sb.Append('}');
                }
                else
                {
                    sb.Append(array == null ? value : "[" + array.Length + "]");
                }
                continue;
            }

            sb.Append('[');
            bool first = true;
            foreach (object stat in array)
            {
                PropertyInfo idProperty = stat.GetType().GetProperty("Value1");
                PropertyInfo valueProperty = stat.GetType().GetProperty("Value2");
                if (idProperty == null || valueProperty == null)
                {
                    continue;
                }

                object statId = idProperty.GetValue(stat, null);
                if (!first)
                {
                    sb.Append(", ");
                }

                sb.Append(statId).Append('(').Append(Convert.ToInt32(statId)).Append(")=")
                    .Append(valueProperty.GetValue(stat, null));
                first = false;
            }

            sb.Append(']');
        }

        sb.Append(" raw=").Append(BitConverter.ToString(packet));
        return sb.ToString();
    }

    private static byte[] Inflate(byte[] input, int startOffset)
    {
        var inflater = new Inflater(false);
        inflater.SetInput(input, startOffset, input.Length - startOffset);
        var outBuffer = new byte[65536];
        LastInflateStop = "ran to end of input";
        using (var result = new MemoryStream())
        {
            while (true)
            {
                if (inflater.IsFinished)
                {
                    LastInflateStop = "stream finished";
                    break;
                }

                if (inflater.IsNeedingInput)
                {
                    LastInflateStop = "needs more input (stream truncated or flush framed)";
                    break;
                }

                if (inflater.IsNeedingDictionary)
                {
                    LastInflateStop = "needs a preset dictionary";
                    break;
                }

                int produced;
                try
                {
                    produced = inflater.Inflate(outBuffer);
                }
                catch (Exception ex)
                {
                    LastInflateStop = "error after " + result.Length + " bytes: " + ex.Message;
                    break;
                }

                if (produced <= 0)
                {
                    // No progress and not finished: nothing more can be extracted.
                    LastInflateStop = "no progress after " + result.Length + " bytes";
                    break;
                }

                result.Write(outBuffer, 0, produced);
            }

            return result.ToArray();
        }
    }

    private static bool IsZlibHeader(byte[] d, int i)
    {
        return i + 1 < d.Length
               && (d[i] & 0x0F) == 8
               && (((d[i] << 8) | d[i + 1]) % 31) == 0;
    }

    /// <summary>
    /// The server resets its zlib stream periodically, so the capture holds a run of
    /// independent streams rather than one. Inflate each in turn, resuming at the next
    /// zlib header after the bytes the previous stream consumed.
    /// </summary>
    private static byte[] InflateAll(byte[] input, int startOffset, out int streamCount)
    {
        streamCount = 0;
        using (var result = new MemoryStream())
        {
            int pos = startOffset;
            while (pos + 2 < input.Length)
            {
                if (!IsZlibHeader(input, pos))
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

                int consumed = (int)inflater.TotalIn;
                if (result.Length > before)
                {
                    streamCount++;
                    pos += Math.Max(consumed, 1);
                }
                else
                {
                    pos++;
                }
            }

            return result.ToArray();
        }
    }

    /// <summary>
    /// Counts how many well formed packets can be framed from <paramref name="data"/>.
    /// Used to decide whether a buffer is plaintext or still compressed.
    /// </summary>
    private static int CountFramable(byte[] data, int limit)
    {
        return CountFramable(data, limit, 1);
    }

    /// <summary>
    /// As above, but stepping to a boundary of <paramref name="alignment"/> bytes
    /// after each packet.
    /// </summary>
    private static int CountFramable(byte[] data, int limit, int alignment)
    {
        int pos = 0, found = 0;
        while (pos + HeaderLength <= data.Length && found < limit)
        {
            short size = BigEndianInt16(data, pos + SizeOffset);
            if (size < HeaderLength || pos + size > data.Length)
            {
                break;
            }

            found++;
            pos += size + Padding(size, alignment);
        }

        return found;
    }

    /// <summary>
    /// Bytes of padding after a packet of <paramref name="size"/> bytes.
    /// </summary>
    private static int Padding(int size, int alignment)
    {
        return alignment <= 1 ? 0 : (alignment - (size % alignment)) % alignment;
    }

    /// <summary>
    /// Works out whether a direction pads its packets to a boundary.
    /// </summary>
    /// <remarks>
    /// The client pads what it sends to a 4 byte boundary; the server does not.
    /// Framing the client stream without allowing for that derails after the
    /// second packet, which is why this direction used to decode almost nothing.
    ///
    /// Detected rather than assumed, because it differs by direction and may
    /// well differ by client build too.
    /// </remarks>
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

    private static void Main(string[] args)
    {
        try
        {
            Run(args);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("PcapDecode failed: " + ex.Message);
            Environment.ExitCode = 1;
        }
    }

    private static bool TryAppendHex(string value, List<byte> destination)
    {
        string hex = value.Replace(":", string.Empty).Trim();
        if (hex.Length < 2 || (hex.Length & 1) != 0 || hex.Any(c => !Uri.IsHexDigit(c)))
        {
            return false;
        }

        for (int i = 0; i < hex.Length; i += 2)
        {
            destination.Add(Convert.ToByte(hex.Substring(i, 2), 16));
        }

        return true;
    }

    private static void Run(string[] args)
    {
        if (args.Length < 1)
        {
            Console.WriteLine("usage: PcapDecode <streams.txt> [messagesDll]");
            return;
        }

        // group segments by stream and direction, preserving capture order
        var streams = new Dictionary<string, List<byte>>();
        int skippedInputRows = 0;
        foreach (string line in File.ReadAllLines(args[0]))
        {
            string[] parts = line.Split(',');
            if (parts.Length < 3 || parts[2].Length < 2)
            {
                skippedInputRows++;
                continue;
            }

            string key = parts[0] + "/" + parts[1];
            List<byte> segment = new List<byte>();
            if (!TryAppendHex(parts[2], segment))
            {
                skippedInputRows++;
                continue;
            }

            if (!streams.ContainsKey(key))
            {
                streams[key] = new List<byte>();
            }

            streams[key].AddRange(segment);
        }

        Console.WriteLine("directions found: " + streams.Count);
        if (skippedInputRows != 0)
        {
            Console.Error.WriteLine("skipped non-stream input rows: " + skippedInputRows);
        }
        Console.WriteLine();

        var serializerAssembly = Assembly.LoadFrom(
            args.Length > 1
                ? args[1]
                : Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "SmokeLounge.AOtomation.Messaging.dll"));
        var serializerType = serializerAssembly.GetType(
            "SmokeLounge.AOtomation.Messaging.Serialization.MessageSerializer");
        object serializer = Activator.CreateInstance(serializerType);
        var deserialize = serializerType.GetMethod("Deserialize", new[] { typeof(Stream) });

        var totals = new Dictionary<string, int>();
        var unknownTypes = new Dictionary<int, int>();
        var perkPackets = new List<string>();
        var followPackets = new List<string>();
        var weaponPackets = new List<string>();
        var simpleItemPackets = new List<string>();
        var simpleItemIdentities = new HashSet<string>();
        var messageTails = new Dictionary<string, Queue<string>>();
        var fullCharacters = new List<string>();
        var orderedPackets = new List<string>();
        bool printOrdered = args.Length > 2 && args[2] == "--ordered";

        foreach (var entry in streams.OrderByDescending(s => s.Value.Count))
        {
            messageTails[entry.Key] = new Queue<string>();
            byte[] raw = entry.Value.ToArray();
            byte[] data = raw;
            string note = "plaintext";

            // Detect a zlib stream by its RFC1950 header rather than by guessing from
            // how many frames happen to parse: compressed bytes can pass a framing
            // check by chance, which is exactly what happened the first time.
            // CM must be 8, and (CMF<<8 | FLG) must be a multiple of 31.
            for (int start = 0; start + 1 < Math.Min(raw.Length, 4096); start++)
            {
                bool looksZlib = (raw[start] & 0x0F) == 8
                                 && (((raw[start] << 8) | raw[start + 1]) % 31) == 0;
                if (!looksZlib)
                {
                    continue;
                }

                int streamCount;
                byte[] candidate = InflateAll(raw, start, out streamCount);
                if (candidate.Length > 0 && CountFramable(candidate, 3) >= 1)
                {
                    data = candidate;
                    note = "inflated from offset " + start + " across " + streamCount + " zlib streams";
                    break;
                }
            }

            // Plaintext only if inflation found nothing usable and the raw bytes frame.
            if (note == "plaintext" && CountFramable(raw, 3) < 1)
            {
                note = "unrecognised (neither framed plaintext nor zlib)";
            }

            Console.WriteLine("=== " + entry.Key + "  " + raw.Length + " bytes captured, " + note
                              + (note == "plaintext" ? string.Empty : ", " + data.Length + " bytes inflated"));

            int alignment = DetectAlignment(data);
            if (alignment > 1)
            {
                Console.WriteLine("    packets padded to a " + alignment + " byte boundary");
            }

            int pos = 0, ok = 0, bad = 0;
            while (pos + HeaderLength <= data.Length)
            {
                short size = BigEndianInt16(data, pos + SizeOffset);
                if (size < HeaderLength || pos + size > data.Length)
                {
                    bad++;
                    pos++; // resynchronise a byte at a time
                    continue;
                }

                var packet = new byte[size];
                Array.Copy(data, pos, packet, 0, size);
                int typeId = size >= 20 ? BigEndianInt32(data, pos + 16) : 0;
                pos += size + Padding(size, alignment);

                try
                {
                    object message = deserialize.Invoke(serializer, new object[] { new MemoryStream(packet) });
                    if (message == null)
                    {
                        bad++;
                        unknownTypes[typeId] = unknownTypes.ContainsKey(typeId) ? unknownTypes[typeId] + 1 : 1;
                        continue;
                    }

                    object body = message.GetType().GetProperty("Body").GetValue(message, null);
                    string name = body == null ? "<null body>" : body.GetType().Name;
                    totals[name] = totals.ContainsKey(name) ? totals[name] + 1 : 1;
                    ok++;

                    if (printOrdered)
                    {
                        orderedPackets.Add(
                            entry.Key + " sequence=" + (ushort)BigEndianInt16(packet, 0)
                            + " " + DescribeMessage(body, packet));
                    }

                    Queue<string> tail = messageTails[entry.Key];
                    tail.Enqueue(DescribeMessage(body, packet));
                    while (tail.Count > 20)
                    {
                        tail.Dequeue();
                    }

                    if (name == "FullCharacterMessage")
                    {
                        var sb = new StringBuilder(
                            entry.Key + "  FullCharacterMessage sequence="
                            + (ushort)BigEndianInt16(packet, 0));
                        foreach (PropertyInfo property in body.GetType().GetProperties())
                        {
                            if (property.Name == "PacketType" || property.Name == "N3MessageType")
                            {
                                continue;
                            }

                            object value = property.GetValue(body, null);
                            var array = value as Array;
                            sb.AppendLine().Append("    ").Append(property.Name).Append('=');
                            if (array == null)
                            {
                                sb.Append(value);
                                continue;
                            }

                            sb.Append('[').Append(array.Length).Append(']');
                            if (property.Name == "InventorySlots")
                            {
                                foreach (object slot in array)
                                {
                                    sb.AppendLine().Append("      slot {");
                                    foreach (PropertyInfo slotProperty in slot.GetType().GetProperties())
                                    {
                                        sb.Append(' ').Append(slotProperty.Name).Append('=')
                                            .Append(slotProperty.GetValue(slot, null));
                                    }

                                    sb.Append(" }");
                                }
                            }
                        }

                        fullCharacters.Add(sb.ToString());
                    }

                    if (name == "WeaponItemFullUpdateMessage")
                    {
                        weaponPackets.Add(entry.Key + "  " + DescribeMessage(body, packet));
                    }

                    if (name == "SimpleItemFullUpdateMessage")
                    {
                        PropertyInfo identityProperty = body.GetType().GetProperty("Identity");
                        string identity = identityProperty == null
                                              ? "<missing identity>"
                                              : Convert.ToString(identityProperty.GetValue(body, null));
                        if (simpleItemIdentities.Add(identity))
                        {
                            simpleItemPackets.Add(entry.Key + "  " + DescribeSimpleItem(body, packet));
                        }
                    }

                    if (name == "FollowTargetMessage")
                    {
                        object info = body.GetType().GetProperty("Info").GetValue(body, null);
                        bool nonCoordinate = info != null && info.GetType().Name != "FollowCoordinateInfo";
                        if (followPackets.Count >= 20 && (!nonCoordinate || followPackets.Count >= 40))
                        {
                            continue;
                        }

                        var sb = new StringBuilder();
                        sb.Append(entry.Key).Append("  ").Append(name);
                        object identity = body.GetType().GetProperty("Identity").GetValue(body, null);
                        sb.Append(" identity=").Append(identity);
                        sb.Append(" info=").Append(info == null ? "<null>" : info.GetType().Name).Append(" {");
                        if (info != null)
                        {
                            foreach (PropertyInfo property in info.GetType().GetProperties())
                            {
                                sb.Append(' ').Append(property.Name).Append('=').Append(property.GetValue(info, null));
                            }
                        }

                        sb.Append(" } raw=").Append(BitConverter.ToString(packet));
                        followPackets.Add(sb.ToString());
                    }

                    if (name.IndexOf("Perk", StringComparison.OrdinalIgnoreCase) >= 0
                        || name.IndexOf("Research", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        var sb = new StringBuilder();
                        sb.Append(entry.Key).Append("  ").Append(name).Append(" { ");
                        foreach (var p in body.GetType().GetProperties())
                        {
                            object v;
                            try
                            {
                                v = p.GetValue(body, null);
                            }
                            catch (Exception)
                            {
                                continue;
                            }

                            sb.Append(p.Name).Append('=').Append(v).Append(' ');
                        }

                        sb.Append("}  raw: ").Append(BitConverter.ToString(packet));
                        perkPackets.Add(sb.ToString());
                    }
                }
                catch (Exception)
                {
                    bad++;
                    unknownTypes[typeId] = unknownTypes.ContainsKey(typeId) ? unknownTypes[typeId] + 1 : 1;
                }
            }

            Console.WriteLine("    parsed " + ok + " messages, " + bad + " unparsed");
        }

        Console.WriteLine();
        Console.WriteLine("=== message types seen ===");
        foreach (var kv in totals.OrderByDescending(k => k.Value))
        {
            Console.WriteLine(string.Format("  {0,6}  {1}", kv.Value, kv.Key));
        }

        if (unknownTypes.Count > 0)
        {
            Console.WriteLine();
            Console.WriteLine("=== message ids that did not deserialise ===");
            Console.WriteLine("    (an id AOtomation does not know, or a body it could not read)");
            foreach (var kv in unknownTypes.OrderByDescending(k => k.Value))
            {
                Console.WriteLine(string.Format("  {0,6}  0x{1:x8}", kv.Value, kv.Key));
            }
        }

        Console.WriteLine();
        Console.WriteLine("=== perk and research messages, in full ===");
        if (perkPackets.Count == 0)
        {
            Console.WriteLine("  none found");
        }

        foreach (string p in perkPackets)
        {
            Console.WriteLine("  " + p);
        }

        Console.WriteLine();
        Console.WriteLine("=== first follow messages, in full ===");
        if (followPackets.Count == 0)
        {
            Console.WriteLine("  none found");
        }

        foreach (string p in followPackets)
        {
            Console.WriteLine("  " + p);
        }

        Console.WriteLine();
        Console.WriteLine("=== weapon item full updates, in full ===");
        foreach (string p in weaponPackets)
        {
            Console.WriteLine("  " + p);
        }

        Console.WriteLine();
        Console.WriteLine("=== unique simple item full updates, with expanded stats ===");
        foreach (string p in simpleItemPackets)
        {
            Console.WriteLine("  " + p);
        }

        Console.WriteLine();
        Console.WriteLine("=== final messages in each direction ===");
        foreach (KeyValuePair<string, Queue<string>> tail in messageTails)
        {
            Console.WriteLine("  -- " + tail.Key + " --");
            foreach (string message in tail.Value)
            {
                Console.WriteLine("  " + message);
            }
        }

        Console.WriteLine();
        if (printOrdered)
        {
            Console.WriteLine("=== ordered messages in each direction ===");
            foreach (string orderedPacket in orderedPackets)
            {
                Console.WriteLine(orderedPacket);
            }

            Console.WriteLine();
        }

        Console.WriteLine("=== full character messages ===");
        foreach (string fullCharacter in fullCharacters)
        {
            Console.WriteLine(fullCharacter);
        }
    }
}
