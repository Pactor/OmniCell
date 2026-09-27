// --------------------------------------------------------------------------------------------------------------------
// <copyright file="PlayfieldDump.cs" company="OmniCell">
//   Copyright © 2026 OmniCell contributors.
// </copyright>
// <summary>
//   What is standing in a playfield, without starting a server to find out.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace OmniCell.Tools.Capture
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;

    using OmniCell.Core.Content;
    using OmniCell.Core.Playfields;
    using OmniCell.Core.Statels;
    using OmniCell.Enums;

    using SmokeLounge.AOtomation.Messaging.GameData;

    /// <summary>
    /// The statels of a playfield, and what each one does.
    /// </summary>
    /// <remarks>
    /// The zone engine has a `pf` console command that answers this, and it
    /// needs a running server with a database behind it. This reads the same
    /// thing out of the content pack and a copy of itemnames.sql, which is
    /// what you want when the question is "why can nobody get out of here".
    ///
    ///     PlayfieldDump &lt;playfields.ocp&gt; &lt;playfield&gt; [--names itemnames.sql] [--find text]
    ///
    /// With --find only the statels whose name matches are listed, in full.
    /// Without it the playfield is summarised by template.
    /// </remarks>
    internal static class PlayfieldDump
    {
        private static int Main(string[] args)
        {
            if (args.Length < 2)
            {
                Console.Error.WriteLine(
                    "usage: PlayfieldDump <playfields.ocp> <playfield> [--names itemnames.sql] [--find text]");
                return 1;
            }

            string pack = args[0];
            int wanted = int.Parse(args[1], CultureInfo.InvariantCulture);
            string names = null;
            string find = null;
            int function = 0;
            bool missing = false;
            float nearX = 0, nearZ = 0, nearR = 0;
            for (int i = 2; i < args.Length; i++)
            {
                if (args[i] == "--names" && i + 1 < args.Length) names = args[++i];
                else if (args[i] == "--find" && i + 1 < args.Length) find = args[++i];
                else if (args[i] == "--near" && i + 3 < args.Length)
                {
                    nearX = float.Parse(args[++i], CultureInfo.InvariantCulture);
                    nearZ = float.Parse(args[++i], CultureInfo.InvariantCulture);
                    nearR = float.Parse(args[++i], CultureInfo.InvariantCulture);
                }
                else if (args[i] == "--missing") missing = true;
                else if (args[i] == "--function" && i + 1 < args.Length)
                {
                    function = int.Parse(args[++i], CultureInfo.InvariantCulture);
                }
            }

            List<PlayfieldData> all = OmniCellContentPack.ReadPlayfields(pack);

            if (missing)
            {
                return Missing(all, names == null ? null : ItemNames(names));
            }

            if (function != 0)
            {
                return Everywhere(all, function, names == null ? null : ItemNames(names));
            }

            PlayfieldData playfield = all.FirstOrDefault(p => p.PlayfieldId == wanted);
            if (playfield == null)
            {
                Console.Error.WriteLine("No playfield {0} in {1}.", wanted, pack);
                return 1;
            }

            Dictionary<int, string> named = names == null
                                                ? new Dictionary<int, string>()
                                                : ItemNames(names);

            Console.WriteLine(
                "playfield {0} \"{1}\": {2} statels, {3} destinations",
                playfield.PlayfieldId,
                (playfield.Name ?? string.Empty).Trim(),
                playfield.Statels == null ? 0 : playfield.Statels.Count,
                playfield.Destinations == null ? 0 : playfield.Destinations.Count);
            Console.WriteLine();

            List<StatelData> statels = playfield.Statels ?? new List<StatelData>();

            if (nearR > 0)
            {
                Console.WriteLine(
                    "  within {0} metres of {1}, {2}:", nearR, nearX, nearZ);
                foreach (StatelData statel in statels
                    .Where(s => Math.Sqrt(
                        ((s.X - nearX) * (s.X - nearX)) + ((s.Z - nearZ) * (s.Z - nearZ))) <= nearR)
                    .OrderBy(s => Math.Sqrt(
                        ((s.X - nearX) * (s.X - nearX)) + ((s.Z - nearZ) * (s.Z - nearZ)))))
                {
                    Console.WriteLine(
                        "    {0,-26} template {1,-8} \"{2}\"  at {3:F0} {4:F0} {5:F0}  {6}",
                        statel.Identity.ToString(true),
                        statel.TemplateId,
                        Name(named, statel.TemplateId),
                        statel.X, statel.Y, statel.Z,
                        string.Join(
                            "; ",
                            statel.Events.Where(e => e.Functions.Count > 0).Select(
                                e => e.EventType + ": " + string.Join(
                                    ", ",
                                    e.Functions.Select(
                                        f => Describe((FunctionType)f.FunctionType, f))))));
                }

                return 0;
            }

            if (find != null)
            {
                foreach (StatelData statel in statels)
                {
                    string name = Name(named, statel.TemplateId);
                    if (name.IndexOf(find, StringComparison.OrdinalIgnoreCase) < 0) continue;

                    Console.WriteLine(
                        "{0}  template {1} \"{2}\"  at {3:F1} {4:F1} {5:F1}",
                        statel.Identity.ToString(true), statel.TemplateId, name,
                        statel.X, statel.Y, statel.Z);

                    foreach (Core.Events.Event ev in statel.Events)
                    {
                        Console.WriteLine(
                            "    {0}: {1}",
                            ev.EventType,
                            ev.Functions.Count == 0
                                ? "nothing"
                                : string.Join(
                                    ", ",
                                    ev.Functions.Select(
                                        f => Describe((FunctionType)f.FunctionType, f))));
                    }

                    Console.WriteLine();
                }

                return 0;
            }

            Console.WriteLine("  by identity type:");
            foreach (IGrouping<int, StatelData> kind in statels
                .GroupBy(s => (int)s.Identity.Type)
                .OrderByDescending(g => g.Count()))
            {
                Console.WriteLine("    {0,-8} {1,4}", kind.Key, kind.Count());
            }

            Console.WriteLine();
            Console.WriteLine("  the ones that do something:");
            foreach (IGrouping<int, StatelData> template in statels
                .Where(s => s.Events.Any(e => e.Functions.Count > 0))
                .GroupBy(s => s.TemplateId)
                .OrderByDescending(g => g.Count()))
            {
                StatelData one = template.First();
                Console.WriteLine(
                    "    template {0,-8} x{1,-4} \"{2}\"",
                    template.Key, template.Count(), Name(named, template.Key));
                foreach (Core.Events.Event ev in one.Events.Where(e => e.Functions.Count > 0))
                {
                    Console.WriteLine(
                        "        {0}: {1}",
                        ev.EventType,
                        string.Join(
                            ", ",
                            ev.Functions.Select(f => Describe((FunctionType)f.FunctionType, f))));
                }
            }

            return 0;
        }

        /// <summary>
        /// What the playfields ask for that the server has no name for.
        /// </summary>
        /// <remarks>
        /// A statel's behaviour is a list of functions with a list of
        /// requirements, and either can name something this server does not
        /// know. A function with no name in the enum does nothing at all - the
        /// door it belongs to is inert - and a requirement operator with no
        /// implementation throws, which takes the whole behaviour with it.
        /// Both are silent from the outside: a door that does nothing looks
        /// the same either way.
        ///
        /// So this counts them. It is the list of what the world asks for and
        /// does not get.
        /// </remarks>
        private static int Missing(List<PlayfieldData> all, Dictionary<int, string> named)
        {
            var functions = new Dictionary<int, int>();
            var operators = new Dictionary<int, int>();
            var wherePlayfield = new Dictionary<int, HashSet<int>>();

            foreach (PlayfieldData playfield in all)
            {
                foreach (StatelData statel in playfield.Statels ?? new List<StatelData>())
                {
                    foreach (Core.Events.Event ev in statel.Events)
                    {
                        foreach (Core.Functions.Function f in ev.Functions)
                        {
                            if (!Enum.IsDefined(typeof(FunctionType), f.FunctionType))
                            {
                                Count(functions, f.FunctionType);
                                Where(wherePlayfield, f.FunctionType, playfield.PlayfieldId);
                            }

                            foreach (Core.Requirements.Requirement r in
                                f.Requirements ?? new List<Core.Requirements.Requirement>())
                            {
                                if (!Implemented((int)r.Operator)) Count(operators, (int)r.Operator);
                            }
                        }
                    }
                }
            }

            Console.WriteLine("functions the enum has no name for:");
            foreach (KeyValuePair<int, int> one in functions.OrderByDescending(x => x.Value))
            {
                HashSet<int> pfs;
                wherePlayfield.TryGetValue(one.Key, out pfs);
                Console.WriteLine(
                    "  {0,-8} {1,4} uses in {2} playfields{3}",
                    one.Key, one.Value, pfs == null ? 0 : pfs.Count,
                    pfs == null || pfs.Count > 6
                        ? string.Empty
                        : " (" + string.Join(", ", pfs.OrderBy(x => x)) + ")");
            }

            Console.WriteLine();
            Console.WriteLine("requirement operators with no implementation:");
            foreach (KeyValuePair<int, int> one in operators.OrderByDescending(x => x.Value))
            {
                Console.WriteLine(
                    "  {0,-8} {1,4} uses  {2}",
                    one.Key, one.Value,
                    Enum.IsDefined(typeof(Operator), one.Key)
                        ? ((Operator)one.Key).ToString()
                        : "unnamed");
            }

            return 0;
        }

        /// <summary>
        /// The operators RequirementLambdaCreator can build an expression for.
        /// Anything else throws when the requirement is checked.
        /// </summary>
        private static bool Implemented(int op)
        {
            switch ((Operator)op)
            {
                case Operator.GreaterThan:
                case Operator.LessThan:
                case Operator.EqualTo:
                case Operator.TestNumPets:
                case Operator.BitAnd:
                case Operator.NotBitAnd:
                case Operator.BitOr:
                case Operator.Unequal:
                case Operator.Not:
                case Operator.HasNotFormula:
                case Operator.HasRunningNano:
                case Operator.FlyingAllowed:
                    return true;
                default:
                    return false;
            }
        }

        private static void Count(Dictionary<int, int> counts, int key)
        {
            int n;
            counts.TryGetValue(key, out n);
            counts[key] = n + 1;
        }

        private static void Where(Dictionary<int, HashSet<int>> where, int key, int playfield)
        {
            HashSet<int> set;
            if (!where.TryGetValue(key, out set))
            {
                set = new HashSet<int>();
                where[key] = set;
            }

            set.Add(playfield);
        }

        /// <summary>
        /// Every use of one function, across every playfield in the pack.
        /// </summary>
        /// <remarks>
        /// What a function without a name does has to be read off what it is
        /// called with, and one playfield's worth of that is a guess.
        /// </remarks>
        private static int Everywhere(
            List<PlayfieldData> all, int function, Dictionary<int, string> named)
        {
            var known = new HashSet<int>(all.Select(p => p.PlayfieldId));
            int uses = 0;
            int lastArgumentIsPlayfield = 0;
            var shapes = new Dictionary<int, int>();

            foreach (PlayfieldData playfield in all)
            {
                foreach (StatelData statel in playfield.Statels ?? new List<StatelData>())
                {
                    foreach (Core.Events.Event ev in statel.Events)
                    {
                        foreach (Core.Functions.Function f in ev.Functions)
                        {
                            if (f.FunctionType != function) continue;

                            List<string> values = f.Arguments == null || f.Arguments.Values == null
                                                      ? new List<string>()
                                                      : f.Arguments.Values
                                                          .Select(v => Convert.ToString(v))
                                                          .ToList();
                            uses++;

                            int count;
                            shapes.TryGetValue(values.Count, out count);
                            shapes[values.Count] = count + 1;

                            int last;
                            if (values.Count > 0
                                && int.TryParse(values[values.Count - 1], out last)
                                && known.Contains(last))
                            {
                                lastArgumentIsPlayfield++;
                            }

                            if (uses <= 20)
                            {
                                Console.WriteLine(
                                    "  pf {0,-6} {1,-22} template {2,-8} \"{3}\"  ({4})",
                                    playfield.PlayfieldId,
                                    ev.EventType,
                                    statel.TemplateId,
                                    named == null ? "?" : Name(named, statel.TemplateId),
                                    string.Join(" ", values));
                            }
                        }
                    }
                }
            }

            Console.WriteLine();
            Console.WriteLine("function {0}: {1} uses", function, uses);
            Console.WriteLine(
                "  argument counts: {0}",
                string.Join(", ", shapes.OrderBy(s => s.Key).Select(s => s.Key + " args x" + s.Value)));
            Console.WriteLine(
                "  last argument is a playfield in the pack: {0} of {1}", lastArgumentIsPlayfield, uses);
            return 0;
        }

        private static string Describe(FunctionType type, Core.Functions.Function function)
        {
            string arguments = function.Arguments == null || function.Arguments.Values == null
                                   ? string.Empty
                                   : string.Join(
                                       " ",
                                       function.Arguments.Values.Select(v => Convert.ToString(v)));
            string requires = function.Requirements == null || function.Requirements.Count == 0
                                  ? string.Empty
                                  : " [" + string.Join(
                                        " ",
                                        function.Requirements.Select(
                                            r => r.ChildOperator + ":" + r.Target + " "
                                                 + (Enum.IsDefined(typeof(StatIds), r.Statnumber)
                                                        ? ((StatIds)r.Statnumber).ToString()
                                                        : "stat " + r.Statnumber)
                                                 + " " + r.Operator + " " + r.Value))
                                    + "]";
            return (arguments.Length == 0 ? type.ToString() : type + "(" + arguments + ")") + requires;
        }

        private static string Name(Dictionary<int, string> named, int template)
        {
            string name;
            return named.TryGetValue(template, out name) ? name : "?";
        }

        /// <summary>
        /// The item names, straight out of the sql the extractor writes.
        /// </summary>
        /// <remarks>
        /// Read rather than queried, because the point of this tool is not
        /// needing a database up.
        /// </remarks>
        private static Dictionary<int, string> ItemNames(string path)
        {
            var names = new Dictionary<int, string>();
            if (!File.Exists(path))
            {
                Console.Error.WriteLine("no item names at " + path);
                return names;
            }

            var row = new Regex(@"\(\s*(\d+)\s*,\s*'((?:[^']|'')*)'", RegexOptions.Compiled);
            foreach (Match match in row.Matches(File.ReadAllText(path)))
            {
                int id = int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
                names[id] = match.Groups[2].Value.Replace("''", "'");
            }

            return names;
        }
    }
}
