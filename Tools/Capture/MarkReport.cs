using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

// Lines up the labels typed during an annotated capture against the messages the
// client actually sent, so an unnamed message can be identified by doing the
// thing on purpose and seeing what goes out.
//
//   MarkReport <client.csv> <marks.txt> [AOtomation.dll] [preSeconds] [postSeconds]
//
// client.csv is "epoch,srcport,payloadhex" per TCP segment from the client to
// the zone servers:
//
//   tshark -r <pcapng> -Y "tcp.len>0 and tcp.dstport>=7500 and tcp.dstport<=7520
//          and tcp.dstport!=7505" -T fields -e frame.time_epoch -e tcp.srcport
//          -e tcp.payload -E separator=,
//
// The source port is what keeps two accounts apart. Client to
// server is a plain byte stream with no framing of its own beyond each message's
// own length, so segments have to be concatenated in order to be parsed at all -
// and concatenating two connections into one buffer does not give a mixed
// report, it gives a wrong one, because the first message of the second client
// lands wherever the first client's stream happened to stop. So each connection
// is parsed on its own and the results merged by time afterwards.
//
// A two column csv without the port still works, and reads as one connection.
internal static class MarkReport
{
    private const int HeaderLength = 16;

    private const int SizeOffset = 6;

    private const int TypeOffset = 16;

    private static void Main(string[] args)
    {
        if (args.Length < 2)
        {
            Console.WriteLine("usage: MarkReport <client.csv> <marks.txt> [dll] [pre] [post]");
            return;
        }

        double pre = args.Length > 3 ? double.Parse(args[3], CultureInfo.InvariantCulture) : 3.0;
        double post = args.Length > 4 ? double.Parse(args[4], CultureInfo.InvariantCulture) : 20.0;

        Dictionary<int, string> names = LoadNames(args.Length > 2 ? args[2] : null);
        List<Message> messages = ReadMessages(args[0], names);
        List<Mark> marks = ReadMarks(args[1]);

        // One tag per connection, in the order each was first heard from, so a
        // two account capture reads as A and B rather than as port numbers.
        var tags = new Dictionary<int, string>();
        foreach (Message m in messages)
        {
            if (!tags.ContainsKey(m.Source))
            {
                tags[m.Source] = ((char)('A' + tags.Count)).ToString();
            }
        }

        bool twoClients = tags.Count > 1;

        Console.WriteLine("  " + messages.Count + " client messages, " + marks.Count + " marks");
        if (twoClients)
        {
            var legend = new List<string>();
            foreach (KeyValuePair<int, string> tag in tags)
            {
                legend.Add(tag.Value + " = port " + tag.Key);
            }

            Console.WriteLine("  " + tags.Count + " clients: " + string.Join(", ", legend));
        }

        Console.WriteLine();

        // How common is each type overall? A type that is everywhere (movement)
        // tells us nothing; a rare one next to a mark is the interesting case.
        var overall = new Dictionary<string, int>();
        foreach (Message m in messages)
        {
            overall[m.Name] = overall.ContainsKey(m.Name) ? overall[m.Name] + 1 : 1;
        }

        foreach (Mark mark in marks)
        {
            Console.WriteLine("======================================================");
            Console.WriteLine("  [" + mark.Clock + "]  " + mark.Label);
            Console.WriteLine("======================================================");

            List<Message> near =
                messages.Where(m => m.Time >= mark.Time - pre && m.Time <= mark.Time + post).ToList();

            if (near.Count == 0)
            {
                Console.WriteLine("    nothing sent in this window");
                Console.WriteLine();
                continue;
            }

            foreach (Message m in near)
            {
                double offset = m.Time - mark.Time;
                string flag = overall[m.Name] <= 20 ? "  <-- rare" : string.Empty;
                string who = twoClients ? tags[m.Source] + "  " : string.Empty;
                Console.WriteLine(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "    {0,7:+0.00;-0.00}s  {1}{2,-26} {3}{4}",
                        offset,
                        who,
                        m.Name,
                        m.Hex,
                        flag));
            }

            Console.WriteLine();
        }
    }

    /// <summary>
    /// Reads the id to name map straight out of AOtomation's own enum, so the
    /// report cannot drift from the library.
    /// </summary>
    private static Dictionary<int, string> LoadNames(string dll)
    {
        var names = new Dictionary<int, string>();
        string path = dll ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "SmokeLounge.AOtomation.Messaging.dll");
        if (!File.Exists(path))
        {
            return names;
        }

        try
        {
            Assembly asm = Assembly.LoadFrom(path);
            Type t = asm.GetType("SmokeLounge.AOtomation.Messaging.Messages.N3MessageType");
            if (t == null || !t.IsEnum)
            {
                return names;
            }

            foreach (object v in Enum.GetValues(t))
            {
                names[Convert.ToInt32(v)] = Enum.GetName(t, v);
            }
        }
        catch (Exception)
        {
            // Names are a convenience. Ids alone still identify a message.
        }

        return names;
    }

    private static List<Message> ReadMessages(string csv, Dictionary<int, string> names)
    {
        var streams = new Dictionary<int, List<byte>>();
        var stampsBySource = new Dictionary<int, List<KeyValuePair<int, double>>>();

        foreach (string line in File.ReadAllLines(csv))
        {
            string[] parts = line.Split(',');
            if (parts.Length < 2)
            {
                continue;
            }

            double epoch;
            if (!double.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out epoch))
            {
                continue;
            }

            // Three columns means the port is there to separate the clients with.
            // Two is the old shape, and reads as a single connection.
            int source = 0;
            int payloadAt = 1;
            if (parts.Length > 2)
            {
                int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out source);
                payloadAt = 2;
            }

            string hex = new string(
                string.Join(",", parts.Skip(payloadAt)).Where(Uri.IsHexDigit).ToArray());
            if (hex.Length < 2)
            {
                continue;
            }

            if (!streams.ContainsKey(source))
            {
                streams[source] = new List<byte>();
                stampsBySource[source] = new List<KeyValuePair<int, double>>();
            }

            stampsBySource[source].Add(new KeyValuePair<int, double>(streams[source].Count, epoch));
            for (int i = 0; i + 1 < hex.Length; i += 2)
            {
                streams[source].Add(Convert.ToByte(hex.Substring(i, 2), 16));
            }
        }

        var result = new List<Message>();
        foreach (KeyValuePair<int, List<byte>> connection in streams)
        {
            ReadConnection(
                connection.Value.ToArray(),
                connection.Key,
                stampsBySource[connection.Key],
                names,
                result);
        }

        result.Sort((a, b) => a.Time.CompareTo(b.Time));
        return result;
    }

    /// <summary>
    /// Cuts one connection's byte stream into messages. Each message carries its
    /// own length and the client pads what it sends to a four byte boundary, so
    /// the stream can be walked - but only within one connection.
    /// </summary>
    private static void ReadConnection(
        byte[] data,
        int source,
        List<KeyValuePair<int, double>> stamps,
        Dictionary<int, string> names,
        List<Message> result)
    {
        int pos = 0;
        while (pos + HeaderLength <= data.Length)
        {
            int size = (data[pos + SizeOffset] << 8) | data[pos + SizeOffset + 1];
            if (size < HeaderLength || pos + size > data.Length)
            {
                pos++;
                continue;
            }

            int id = 0;
            if (size >= TypeOffset + 4)
            {
                id = (data[pos + TypeOffset] << 24) | (data[pos + TypeOffset + 1] << 16)
                     | (data[pos + TypeOffset + 2] << 8) | data[pos + TypeOffset + 3];
            }

            string name = names.ContainsKey(id) ? names[id] : string.Format("UNKNOWN 0x{0:x8}", id);

            // Everything past the header and id, which is where the payload that
            // actually varies between one use and the next lives.
            var tail = new StringBuilder();
            for (int i = pos + TypeOffset + 4; i < pos + size && i < data.Length; i++)
            {
                tail.Append(data[i].ToString("x2"));
            }

            result.Add(new Message(TimeAt(stamps, pos), source, name, tail.ToString()));

            // The client pads what it sends up to a 4 byte boundary.
            pos += size + ((4 - (size % 4)) % 4);
        }
    }

    private static double TimeAt(List<KeyValuePair<int, double>> stamps, int offset)
    {
        double time = stamps.Count > 0 ? stamps[0].Value : 0;
        foreach (KeyValuePair<int, double> s in stamps)
        {
            if (s.Key > offset)
            {
                break;
            }

            time = s.Value;
        }

        return time;
    }

    private static List<Mark> ReadMarks(string path)
    {
        var marks = new List<Mark>();
        foreach (string line in File.ReadAllLines(path))
        {
            if (line.StartsWith("#") || line.Trim().Length == 0)
            {
                continue;
            }

            string[] parts = line.Split(new[] { '|' }, 3);
            if (parts.Length < 3)
            {
                continue;
            }

            double ms;
            if (!double.TryParse(parts[0].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out ms))
            {
                continue;
            }

            marks.Add(new Mark(ms / 1000.0, parts[1].Trim(), parts[2].Trim()));
        }

        return marks;
    }

    private class Message
    {
        public Message(double time, int source, string name, string hex)
        {
            this.Time = time;
            this.Source = source;
            this.Name = name;
            this.Hex = hex;
        }

        public double Time { get; private set; }

        /// <summary>The client's TCP source port - which account sent this.</summary>
        public int Source { get; private set; }

        public string Name { get; private set; }

        public string Hex { get; private set; }
    }

    private class Mark
    {
        public Mark(double time, string clock, string label)
        {
            this.Time = time;
            this.Clock = clock;
            this.Label = label;
        }

        public double Time { get; private set; }

        public string Clock { get; private set; }

        public string Label { get; private set; }
    }
}
