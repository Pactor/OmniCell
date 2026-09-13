using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

// Decides which parts of a recorded session may be shared, and says why in
// terms the person sharing it can check.
//
//     Scrub <out dir> <manifest.txt> <secrets file, or -> <stream.csv> <decoded.txt> ...
//
// Pairs of csv and decoded transcript, as many as the recording had
// connections. The ones that pass are copied into the output directory; the
// ones that do not are named, with the reason, and left where they are.
//
// Nothing is edited. That is deliberate, and it is the whole design: half of a
// zone session is one continuous deflate stream, so a name cannot be lifted out
// of the middle of it without rebuilding every byte that follows. A tool that
// offered to scrub a capture would be promising something it cannot do. This
// promises the other thing - that the parts carrying credentials are not
// copied - which it can prove.
//
// Exit code is 0 only when something was admitted and nothing private survived
// into it.
internal static class Scrub
{
    /// <summary>
    /// What a connection turned out to be.
    /// </summary>
    private enum Kind
    {
        /// <summary>
        /// A zone server. The half coming back is compressed, which is the
        /// thing that identifies it - see <see cref="Classify"/>.
        /// </summary>
        Zone,

        /// <summary>
        /// The login server. Carries the account name in the clear, the
        /// encrypted password, and the name of every character on the account.
        /// </summary>
        Login,

        /// <summary>
        /// The chat server: tells, org chat, private groups.
        /// </summary>
        Chat,

        /// <summary>
        /// Something else, or too little of it to tell. Not shared.
        /// </summary>
        Unknown
    }

    /// <summary>
    /// Messages that only ever occur on the login server.
    /// </summary>
    /// <remarks>
    /// The port cannot be used for this. On the live servers 7505 is a zone
    /// server on one host and the login server on another, and a recording of
    /// one session held fourteen connections on 7505 of which one was the
    /// login. Deciding by port would have shared an account name, an encrypted
    /// password and a list of every character on the account.
    /// </remarks>
    private static readonly string[] LoginMessages =
        {
            "UserCredentialsMessage", "UserLoginMessage", "ServerSaltMessage", "CharacterListMessage",
            "LoginErrorMessage"
        };

    private static int Main(string[] args)
    {
        if (args.Length < 5 || (args.Length - 3) % 2 != 0)
        {
            Console.WriteLine("usage: Scrub <outdir> <manifest> <secrets|-> <stream.csv> <decoded.txt> ...");
            return 2;
        }

        string outDir = args[0];
        string manifestPath = args[1];
        List<string> secrets = ReadSecrets(args[2]);

        Directory.CreateDirectory(outDir);

        var report = new StringBuilder();
        var problems = new List<string>();
        var admitted = new List<string>();
        var refused = new List<string>();
        int removedRows = 0;

        for (int i = 3; i < args.Length; i += 2)
        {
            string csv = args[i];
            string decoded = args[i + 1];

            if (!File.Exists(csv) || !File.Exists(decoded))
            {
                continue;
            }

            Kind kind = Classify(decoded);
            string name = Path.GetFileName(csv);

            if (kind != Kind.Zone)
            {
                refused.Add(Describe(name, kind));
                continue;
            }

            Session session = Read(csv);

            // The game's own half is plain text on the wire, so it can be
            // searched directly and a message that should not be there can be
            // dropped: each one stands alone and nothing after it depends on
            // it.
            List<int> hits = FindInPayload(session, secrets);
            foreach (int row in hits)
            {
                session.Rows[row] = null;
            }

            removedRows += hits.Count;

            // The server's half cannot be searched that way - the same word
            // compresses differently depending on what came before it - so the
            // transcript is searched instead, which is that half after it has
            // been expanded and named. Nor can it be edited, so a hit here
            // stops the whole thing rather than being quietly dropped.
            List<string> inReplies = FindInText(decoded, secrets);
            if (inReplies.Count > 0)
            {
                problems.Add(
                    name + ": something you asked to keep out appears " + inReplies.Count
                    + " time(s) in the server's replies, and that half cannot be edited");
                continue;
            }

            Write(Path.Combine(outDir, name), session);
            File.Copy(decoded, Path.Combine(outDir, Path.GetFileName(decoded)), true);

            admitted.Add(
                name + "  -  " + session.ClientChunks + " sent, " + session.ServerChunks + " received, "
                + Census(decoded) + (hits.Count > 0 ? "  [" + hits.Count + " message(s) removed at your request]" : string.Empty));
        }

        report.AppendLine("What is in this session, and what is not");
        report.AppendLine("========================================");
        report.AppendLine();
        report.AppendLine("Prepared " + DateTime.Now.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture));
        report.AppendLine();

        report.AppendLine("Included");
        report.AppendLine("--------");
        if (admitted.Count == 0)
        {
            report.AppendLine("  nothing");
        }

        foreach (string line in admitted)
        {
            report.AppendLine("  " + line);
        }

        report.AppendLine();
        report.AppendLine("Left behind");
        report.AppendLine("-----------");
        if (refused.Count == 0)
        {
            report.AppendLine("  nothing else was in the recording");
        }

        foreach (string line in refused)
        {
            report.AppendLine("  " + line);
        }

        report.AppendLine();
        report.AppendLine("How that was decided");
        report.AppendLine("--------------------");
        report.AppendLine("  Not by port number, which cannot tell them apart: on the live servers");
        report.AppendLine("  7505 is a zone server on one machine and the login server on another.");
        report.AppendLine("  A connection is included only if it proves what it is - a zone server's");
        report.AppendLine("  replies are compressed, and nothing else's are. Anything that does not");
        report.AppendLine("  prove itself is left out, including anything unrecognised.");
        report.AppendLine();
        report.AppendLine("Never present in this format");
        report.AppendLine("----------------------------");
        report.AppendLine("  your IP address, the server's, and every other address on your network");
        report.AppendLine("  the hardware address of your network card");
        report.AppendLine("  port numbers, timings and routing - these files hold payload and nothing else");
        report.AppendLine("  anything from any other program on your machine");
        report.AppendLine();
        report.AppendLine("Still in it, on purpose");
        report.AppendLine("-----------------------");
        report.AppendLine("  your character's name, which is what anyone standing next to you sees");
        report.AppendLine("  where you walked, what you looked at, fought, bought and talked to");
        report.AppendLine("  public and area chat you were close enough to hear");
        report.AppendLine();

        if (removedRows > 0)
        {
            report.AppendLine(
                removedRows + " message(s) were taken out because they contained something you named.");
            report.AppendLine();
        }

        if (admitted.Count == 0)
        {
            problems.Add("no zone traffic in this recording - was the game running?");
        }

        if (problems.Count > 0)
        {
            report.AppendLine("NOT SAFE TO SHARE");
            report.AppendLine("-----------------");
            foreach (string problem in problems)
            {
                report.AppendLine("  " + problem);
            }

            report.AppendLine();
        }

        File.WriteAllText(manifestPath, report.ToString());
        Console.Write(report.ToString());

        return problems.Count == 0 ? 0 : 1;
    }

    /// <summary>
    /// What a connection is, from what the decoder made of it.
    /// </summary>
    /// <remarks>
    /// Login is checked first and by message name, because a login connection
    /// is plaintext in both directions and would otherwise fall through to
    /// Unknown - which is safe, but says the wrong thing in the report. What
    /// admits a connection is the compression: PcapDecode says "inflated from
    /// offset" only when the replies were a zlib stream, and only a zone server
    /// sends those.
    /// </remarks>
    private static Kind Classify(string decoded)
    {
        string text = File.ReadAllText(decoded);

        foreach (string message in LoginMessages)
        {
            if (text.IndexOf(message, StringComparison.Ordinal) >= 0)
            {
                return Kind.Login;
            }
        }

        if (text.IndexOf("inflated from offset", StringComparison.Ordinal) >= 0)
        {
            return Kind.Zone;
        }

        if (text.IndexOf("PrivateMessage", StringComparison.Ordinal) >= 0
            || text.IndexOf("ChatServer", StringComparison.Ordinal) >= 0)
        {
            return Kind.Chat;
        }

        return Kind.Unknown;
    }

    private static string Describe(string name, Kind kind)
    {
        switch (kind)
        {
            case Kind.Login:
                return name + "  -  the login server. Your account name, your encrypted password "
                       + "and the name of every character on the account are in it.";
            case Kind.Chat:
                return name + "  -  the chat server. Tells, org chat and private groups are in it.";
            default:
                return name + "  -  not recognised as a zone server, so not shared.";
        }
    }

    /// <summary>
    /// One reassembled connection: the rows of the csv, split by who sent them.
    /// </summary>
    private sealed class Session
    {
        public readonly List<string> Rows = new List<string>();

        public int ClientChunks;

        public int ServerChunks;
    }

    private static Session Read(string path)
    {
        var session = new Session();

        foreach (string line in File.ReadAllLines(path))
        {
            session.Rows.Add(line);

            string[] parts = line.Split(new[] { ',' }, 3);
            if (parts.Length < 3)
            {
                continue;
            }

            if (parts[1] == "server")
            {
                session.ServerChunks++;
            }
            else
            {
                session.ClientChunks++;
            }
        }

        return session;
    }

    private static void Write(string path, Session session)
    {
        var kept = new List<string>();
        foreach (string row in session.Rows)
        {
            if (row != null)
            {
                kept.Add(row);
            }
        }

        File.WriteAllLines(path, kept);
    }

    /// <summary>
    /// Which rows of the game's own half contain one of the words to keep out.
    /// </summary>
    /// <remarks>
    /// Searched as bytes rather than as text, and in both of the ways the
    /// client writes a string: one byte per character and two. A name typed
    /// into a box and the same name inside a message are not encoded alike, and
    /// looking for only one of them finds only one of them.
    /// </remarks>
    private static List<int> FindInPayload(Session session, List<string> secrets)
    {
        var hits = new List<int>();
        if (secrets.Count == 0)
        {
            return hits;
        }

        for (int i = 0; i < session.Rows.Count; i++)
        {
            string[] parts = session.Rows[i].Split(new[] { ',' }, 3);
            if (parts.Length < 3 || parts[1] == "server")
            {
                continue;
            }

            byte[] payload = FromHex(parts[2]);
            if (payload == null)
            {
                continue;
            }

            foreach (string secret in secrets)
            {
                if (Contains(payload, Encoding.ASCII.GetBytes(secret))
                    || Contains(payload, Encoding.ASCII.GetBytes(secret.ToLowerInvariant()))
                    || Contains(payload, Encoding.Unicode.GetBytes(secret)))
                {
                    hits.Add(i);
                    break;
                }
            }
        }

        return hits;
    }

    private static List<string> FindInText(string path, List<string> secrets)
    {
        var hits = new List<string>();
        if (secrets.Count == 0)
        {
            return hits;
        }

        foreach (string line in File.ReadAllLines(path))
        {
            foreach (string secret in secrets)
            {
                if (line.IndexOf(secret, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    hits.Add(line);
                    break;
                }
            }
        }

        return hits;
    }

    /// <summary>
    /// What kinds of message the connection turned out to hold.
    /// </summary>
    /// <remarks>
    /// Read out of PcapDecode's own tally rather than counted again here, which
    /// would disagree with it the first time either of them changed. Printed so
    /// that the person sharing the file can see for themselves that it is game
    /// traffic, rather than taking a tool's word for it.
    /// </remarks>
    private static string Census(string decoded)
    {
        var counts = new List<KeyValuePair<string, int>>();
        bool inTally = false;

        foreach (string line in File.ReadAllLines(decoded))
        {
            if (line.StartsWith("=== message types seen", StringComparison.Ordinal))
            {
                inTally = true;
                continue;
            }

            if (!inTally)
            {
                continue;
            }

            if (line.StartsWith("===", StringComparison.Ordinal))
            {
                break;
            }

            string[] parts = line.Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            int count;
            if (parts.Length == 2 && int.TryParse(parts[0], out count))
            {
                counts.Add(new KeyValuePair<string, int>(parts[1], count));
            }
        }

        if (counts.Count == 0)
        {
            return "no messages could be named";
        }

        counts.Sort((a, b) => b.Value.CompareTo(a.Value));

        var text = new StringBuilder(counts.Count + " kinds of message (");
        for (int i = 0; i < counts.Count && i < 4; i++)
        {
            if (i > 0)
            {
                text.Append(", ");
            }

            text.Append(counts[i].Key).Append(" x").Append(counts[i].Value);
        }

        return text.Append(")").ToString();
    }

    private static List<string> ReadSecrets(string path)
    {
        var secrets = new List<string>();
        if (path == "-" || !File.Exists(path))
        {
            return secrets;
        }

        foreach (string line in File.ReadAllLines(path))
        {
            string trimmed = line.Trim();

            // Two characters is not a secret, it is a substring of everything.
            if (trimmed.Length >= 3)
            {
                secrets.Add(trimmed);
            }
        }

        return secrets;
    }

    private static byte[] FromHex(string hex)
    {
        if (hex.Length % 2 != 0)
        {
            return null;
        }

        var bytes = new byte[hex.Length / 2];
        for (int i = 0; i < bytes.Length; i++)
        {
            if (!byte.TryParse(
                    hex.Substring(i * 2, 2),
                    NumberStyles.HexNumber,
                    CultureInfo.InvariantCulture,
                    out bytes[i]))
            {
                return null;
            }
        }

        return bytes;
    }

    private static bool Contains(byte[] haystack, byte[] needle)
    {
        if (needle.Length == 0 || needle.Length > haystack.Length)
        {
            return false;
        }

        for (int i = 0; i <= haystack.Length - needle.Length; i++)
        {
            int j = 0;
            while (j < needle.Length && haystack[i + j] == needle[j])
            {
                j++;
            }

            if (j == needle.Length)
            {
                return true;
            }
        }

        return false;
    }
}
