using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

// Turns text typed in game into capture marks, so a session can be annotated
// without alt-tabbing out of a fullscreen client.
//
//   ChatMarks <chat.csv> <out.marks.txt> [prefix]
//
// chat.csv is "epoch,payloadhex" for client to chat-server traffic:
//
//   tshark -r <pcapng> -Y "tcp.dstport==7105 and tcp.len>0" -T fields
//          -e frame.time_epoch -e tcp.payload -E separator=,
//
// Any printable run in a packet the client sent is treated as something the
// player typed, and becomes a mark at that packet's timestamp.
//
// With a prefix given, only text starting with it is kept, and the prefix is
// stripped. That keeps ordinary conversation out of the marks.
internal static class ChatMarks
{
    private const int MinimumRun = 3;

    private static void Main(string[] args)
    {
        if (args.Length < 2)
        {
            Console.WriteLine("usage: ChatMarks <chat.csv> <out.marks.txt> [prefix, default MARK]");
            return;
        }

        // Defaulted, not optional. The chat login packet carries the account
        // name and an auth hash as plain text, and an unfiltered scan would
        // write both into the marks file. Requiring a marker keeps credentials
        // out of it.
        string prefix = args.Length > 2 && args[2].Length > 0 ? args[2] : "MARK";
        var marks = new List<string>();
        int packets = 0;
        int candidates = 0;

        foreach (string line in File.ReadAllLines(args[0]))
        {
            int comma = line.IndexOf(',');
            if (comma <= 0)
            {
                continue;
            }

            double epoch;
            if (!double.TryParse(
                line.Substring(0, comma), NumberStyles.Float, CultureInfo.InvariantCulture, out epoch))
            {
                continue;
            }

            string hex = new string(line.Substring(comma + 1).Where(Uri.IsHexDigit).ToArray());
            if (hex.Length < 2)
            {
                continue;
            }

            packets++;
            var bytes = new byte[hex.Length / 2];
            for (int i = 0; i < bytes.Length; i++)
            {
                bytes[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
            }

            foreach (string text in PrintableRuns(bytes))
            {
                candidates++;
                string label = text.Trim();

                int at = label.IndexOf(prefix, StringComparison.OrdinalIgnoreCase);
                if (at < 0)
                {
                    continue;
                }

                label = label.Substring(at + prefix.Length).Trim();

                if (label.Length == 0)
                {
                    continue;
                }

                DateTime local = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                    .AddSeconds(epoch).ToLocalTime();

                marks.Add(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "{0}|{1}|{2}",
                        (long)(epoch * 1000),
                        local.ToString("HH:mm:ss"),
                        label));
            }
        }

        File.WriteAllLines(args[1], marks.ToArray());

        Console.WriteLine("   " + packets + " packets, " + candidates + " text runs, "
                          + marks.Count + " marks written");
        if (marks.Count == 0 && candidates > 0)
        {
            Console.WriteLine("   (text was found but none of it contained \"" + prefix + "\")");
        }

        foreach (string m in marks.Take(40))
        {
            Console.WriteLine("     " + m);
        }
    }

    /// <summary>
    /// Pulls runs of printable ASCII out of a packet.
    /// </summary>
    /// <remarks>
    /// The chat protocol is not decoded here on purpose. What the player typed
    /// is length prefixed plain text inside the packet, and finding it by
    /// scanning is enough to timestamp a label - which is all a mark needs.
    /// </remarks>
    private static IEnumerable<string> PrintableRuns(byte[] data)
    {
        var sb = new StringBuilder();
        foreach (byte b in data)
        {
            if (b >= 0x20 && b < 0x7f)
            {
                sb.Append((char)b);
                continue;
            }

            if (sb.Length >= MinimumRun)
            {
                yield return sb.ToString();
            }

            sb.Clear();
        }

        if (sb.Length >= MinimumRun)
        {
            yield return sb.ToString();
        }
    }
}
