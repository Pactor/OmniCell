using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

// Converts "tshark -z follow,tcp,raw,N" output into the CSV that PcapDecode reads.
// tshark indents one direction with a tab, which is how the two directions are told
// apart. Using tshark's reassembly rather than raw payload concatenation matters:
// it removes duplicates and fixes ordering, and a stateful zlib stream cannot
// survive either problem.
//
//     FollowToCsv <follow.txt> <streamIndex> <out.csv>
//     FollowToCsv <follow.txt> --split <out directory> [prefix]
//
// The second form is for a dump made with several -z options at once, which is
// how a whole recording is read in one go. tshark takes a fresh pass over the
// capture file for every -z follow it is given separately, so asking for
// sixty-nine of them one at a time means reading the file sixty-nine times: on
// an eighteen megabyte recording that was forty minutes. Asked for all at once
// it is one pass and a few seconds, and this splits the result back apart.
internal static class FollowToCsv
{
    private static readonly Regex HexOnly = new Regex("^[0-9a-fA-F]+$", RegexOptions.Compiled);

    /// <summary>
    /// The line tshark writes before each block, naming the connection.
    /// </summary>
    private static readonly Regex Header =
        new Regex(@"^Filter:\s*tcp\.stream\s+eq\s+(\d+)", RegexOptions.Compiled);

    private static int Main(string[] args)
    {
        if (args.Length < 3)
        {
            Console.WriteLine("usage: FollowToCsv <follow.txt> <streamIndex> <out.csv>");
            Console.WriteLine("       FollowToCsv <follow.txt> --split <out directory> [prefix]");
            return 2;
        }

        if (args[1] == "--split")
        {
            return Split(args[0], args[2], args.Length > 3 ? args[3] : "stream");
        }

        var rows = new List<string>();
        int server = 0;
        int client = 0;

        foreach (string line in File.ReadAllLines(args[0]))
        {
            string row = Row(line, args[1], ref server, ref client);
            if (row != null)
            {
                rows.Add(row);
            }
        }

        File.WriteAllLines(args[2], rows);
        Console.WriteLine(
            "   " + rows.Count + " chunks (" + server + " server->client, " + client + " client->server)");
        return 0;
    }

    /// <summary>
    /// Cuts a dump of several connections into one file per connection.
    /// </summary>
    /// <remarks>
    /// The stream number comes from tshark's own Filter line rather than from
    /// the order the blocks appear in, because the order is tshark's and not
    /// the order the numbers were asked for.
    /// </remarks>
    private static int Split(string dump, string outDir, string prefix)
    {
        Directory.CreateDirectory(outDir);

        string stream = null;
        var rows = new List<string>();
        int server = 0;
        int client = 0;
        int written = 0;

        foreach (string line in File.ReadAllLines(dump))
        {
            Match header = Header.Match(line);
            if (header.Success)
            {
                written += Flush(outDir, prefix, stream, rows);
                stream = header.Groups[1].Value;
                rows.Clear();
                server = 0;
                client = 0;
                continue;
            }

            if (stream == null)
            {
                continue;
            }

            string row = Row(line, stream, ref server, ref client);
            if (row != null)
            {
                rows.Add(row);
            }
        }

        written += Flush(outDir, prefix, stream, rows);
        Console.WriteLine("   " + written + " connection(s) written to " + outDir);
        return 0;
    }

    private static int Flush(string outDir, string prefix, string stream, List<string> rows)
    {
        if (stream == null || rows.Count == 0)
        {
            return 0;
        }

        File.WriteAllLines(Path.Combine(outDir, prefix + "_s" + stream + ".csv"), rows);
        return 1;
    }

    /// <summary>
    /// One line of a follow dump as a csv row, or null if it is not payload.
    /// </summary>
    private static string Row(string line, string stream, ref int server, ref int client)
    {
        if (line.StartsWith("=====", StringComparison.Ordinal)
            || line.StartsWith("Follow:", StringComparison.Ordinal)
            || line.StartsWith("Filter:", StringComparison.Ordinal)
            || line.StartsWith("Node ", StringComparison.Ordinal))
        {
            return null;
        }

        string trimmed = line.Trim();
        if (trimmed.Length == 0 || !HexOnly.IsMatch(trimmed))
        {
            return null;
        }

        bool fromServer = line.StartsWith("\t", StringComparison.Ordinal);
        if (fromServer)
        {
            server++;
        }
        else
        {
            client++;
        }

        return stream + "," + (fromServer ? "server" : "client") + "," + trimmed;
    }
}
