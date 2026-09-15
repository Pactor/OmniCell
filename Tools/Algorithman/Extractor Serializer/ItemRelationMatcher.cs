namespace Extractor_Serializer
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;

    /// <summary>
    /// Works out which item records are the same item at different quality levels, from the
    /// client's own data. Replaces the relations aoitems.com used to supply.
    /// </summary>
    /// <remarks>
    /// A port of Tyrbot's items_extractor id_matcher (github.com/Pactor/Tyrbot, items_extractor/src),
    /// driven by the same three rule files, kept in RelationRules:
    ///   static_list.txt          lowid,highid,lowql,highql[,icon,name] - families scattered through
    ///                            the database, which name matching cannot find (NCU memories,
    ///                            belt components and similar).
    ///   nameseparation_list.txt  pattern1,pattern2,itemtype - two name prefixes that are one item
    ///                            at different quality levels ("Senpai %" and "Hanshi %").
    ///   delete_list.txt          SQL WHERE clauses for records that are not real items (NPC
    ///                            weapons, test items, implants) and must not be paired.
    /// Everything else is paired by identical name and item type, in quality order.
    ///
    /// Tyrbot writes low/high pairs. OmniCell wants each item's whole family - every record of the
    /// item from its lowest to its highest quality - so each group Tyrbot pairs within becomes one
    /// family, static pairs chain through the IDs they share, and families that share a record are
    /// merged.
    /// </remarks>
    public class ItemRelationMatcher
    {
        private static readonly string[] ItemTypeNames = { "Misc", "Weapon", "Armor", "Implant", "Template", "Spirit" };

        private readonly List<Entry> entries = new List<Entry>();

        private readonly List<List<int>> groups = new List<List<int>>();

        private readonly HashSet<int> staticIds = new HashSet<int>();

        public ItemRelationMatcher(string rulesDirectory)
        {
            this.RulesDirectory = rulesDirectory;
        }

        public string RulesDirectory { get; private set; }

        /// <summary>Records named by static_list.txt: curated, so they outrank any other source.</summary>
        public ISet<int> StaticIds
        {
            get { return this.staticIds; }
        }

        public void Add(int id, int quality, string name, int icon, int itemClass)
        {
            string itemType = itemClass >= 0 && itemClass < ItemTypeNames.Length ? ItemTypeNames[itemClass] : "Armor";
            this.entries.Add(new Entry(id, quality, (name ?? string.Empty).Trim(), icon, itemType));
        }

        /// <summary>
        /// Runs the rules and returns each record's family, lowest quality first. Records the rules
        /// delete are absent; callers relate those to themselves.
        /// </summary>
        /// <param name="applyDeleteList">
        /// Tyrbot's delete list keeps its player item search free of NPC attacks, test records and
        /// implants. Those records still exist server-side - they stay in items.ocp either way - the
        /// list only stops them being paired by name. False pairs them like any other record.
        /// </param>
        public Dictionary<int, List<int>> Match(bool applyDeleteList = true)
        {
            this.groups.Clear();
            this.staticIds.Clear();
            Dictionary<int, int> quality = this.entries.GroupBy(e => e.Id).ToDictionary(g => g.Key, g => g.First().Ql);
            List<Entry> working = new List<Entry>(this.entries);

            this.ApplyStaticList(working);
            if (applyDeleteList)
            {
                this.ApplyDeleteList(working);
            }

            this.ApplyNameSeparations(working);
            this.PairRemaining(working);

            return BuildFamilies(this.groups, quality);
        }

        private void ApplyStaticList(List<Entry> working)
        {
            List<int[]> rules = new List<int[]>();
            foreach (string line in ReadRules("static_list.txt"))
            {
                string[] parts = line.Split(',').Select(p => p.Trim()).ToArray();
                int lowId, highId, lowQl, highQl;
                if (parts.Length < 4 || !int.TryParse(parts[0], out lowId) || !int.TryParse(parts[1], out highId)
                    || !int.TryParse(parts[2], out lowQl) || !int.TryParse(parts[3], out highQl))
                {
                    Console.WriteLine("static_list.txt: skipped '" + line + "' (expected lowid,highid,lowql,highql)");
                    continue;
                }

                rules.Add(new[] { lowId, highId });
            }

            HashSet<int> present = new HashSet<int>(working.Select(e => e.Id));
            foreach (int[] rule in rules)
            {
                if (present.Contains(rule[0]))
                {
                    this.groups.Add(new List<int> { rule[0], rule[1] });
                    this.staticIds.Add(rule[0]);
                    this.staticIds.Add(rule[1]);
                }
            }

            HashSet<int> ruled = new HashSet<int>(rules.SelectMany(r => r));
            working.RemoveAll(e => ruled.Contains(e.Id));
        }

        private void ApplyDeleteList(List<Entry> working)
        {
            foreach (string line in ReadRules("delete_list.txt"))
            {
                Func<Entry, bool> clause = ParseDeleteClause(line);
                if (clause == null)
                {
                    Console.WriteLine("delete_list.txt: skipped '" + line + "' (not a clause this port understands)");
                    continue;
                }

                working.RemoveAll(e => clause(e));
            }
        }

        private void ApplyNameSeparations(List<Entry> working)
        {
            List<string[]> rules = new List<string[]>();
            foreach (string line in ReadRules("nameseparation_list.txt"))
            {
                string[] parts = line.Split(',').Select(p => p.Trim()).ToArray();
                if (parts.Length < 3)
                {
                    Console.WriteLine("nameseparation_list.txt: skipped '" + line + "' (expected pattern1,pattern2,itemtype)");
                    continue;
                }

                rules.Add(parts);
            }

            // Every rule pairs against the same staging set; matched records are removed only after
            // all rules ran, as Tyrbot does, so a middle tier ("Quality Omni Life") pairs both ways.
            foreach (string[] rule in rules)
            {
                Regex first = LikeToRegex(rule[0]);
                Regex second = LikeToRegex(rule[1]);
                string firstText = rule[0].Replace("%", string.Empty);
                string secondText = rule[1].Replace("%", string.Empty);
                string itemType = rule[2];

                foreach (var family in working
                             .Where(e => e.ItemType == itemType && (first.IsMatch(e.Name) || second.IsMatch(e.Name)))
                             .GroupBy(e => e.Name.Replace(secondText, string.Empty).Replace(firstText, string.Empty))
                             .OrderBy(g => g.Key, StringComparer.Ordinal))
                {
                    this.PairEntries(family.OrderBy(e => e.Ql).ThenBy(e => e.Id).ToList());
                }
            }

            foreach (string[] rule in rules)
            {
                Regex first = LikeToRegex(rule[0]);
                Regex second = LikeToRegex(rule[1]);
                working.RemoveAll(e => e.ItemType == rule[2] && (first.IsMatch(e.Name) || second.IsMatch(e.Name)));
            }
        }

        private void PairRemaining(List<Entry> working)
        {
            foreach (var family in working.GroupBy(e => new { e.Name, e.ItemType }))
            {
                List<Entry> byQuality = family.OrderBy(e => e.Ql).ThenBy(e => e.Id).ToList();
                bool sequential = true;
                for (int i = 0; i < byQuality.Count - 1; i++)
                {
                    if (byQuality[i].Ql >= byQuality[i + 1].Ql)
                    {
                        sequential = false;
                        break;
                    }
                }

                if (sequential)
                {
                    this.PairEntries(byQuality);
                    continue;
                }

                // Several copies of the item share this name: split them where, walking by record
                // ID, the quality stops rising.
                int currentQl = 0;
                List<Entry> run = new List<Entry>();
                foreach (Entry entry in family.OrderBy(e => e.Id))
                {
                    if (currentQl >= entry.Ql)
                    {
                        this.PairEntries(run);
                        run = new List<Entry>();
                    }

                    run.Add(entry);
                    currentQl = entry.Ql;
                }

                this.PairEntries(run);
            }
        }

        // Tyrbot's pair_entries and add_entry_items only decide how a group splits into low/high
        // pairs; for families every record of the group belongs together, so the group is kept whole.
        private void PairEntries(List<Entry> group)
        {
            if (group.Count > 0)
            {
                this.groups.Add(group.Select(e => e.Id).ToList());
            }
        }

        private static Dictionary<int, List<int>> BuildFamilies(List<List<int>> groups, Dictionary<int, int> quality)
        {
            Dictionary<int, int> parent = new Dictionary<int, int>();
            Func<int, int> find = null;
            find = id =>
            {
                int p;
                if (!parent.TryGetValue(id, out p))
                {
                    parent[id] = id;
                    return id;
                }

                if (p == id)
                {
                    return id;
                }

                int root = find(p);
                parent[id] = root;
                return root;
            };

            foreach (List<int> group in groups)
            {
                int root = find(group[0]);
                foreach (int id in group.Skip(1))
                {
                    int other = find(id);
                    if (other != root)
                    {
                        parent[other] = root;
                    }
                }
            }

            Dictionary<int, List<int>> families = new Dictionary<int, List<int>>();
            foreach (var component in parent.Keys.ToList().GroupBy(find))
            {
                // Only records the client has; a rule may name one that is gone.
                List<int> members = component.Where(quality.ContainsKey)
                                             .OrderBy(id => quality[id]).ThenBy(id => id)
                                             .ToList();
                foreach (int id in members)
                {
                    families[id] = members;
                }
            }

            return families;
        }

        private IEnumerable<string> ReadRules(string fileName)
        {
            string path = Path.Combine(this.RulesDirectory, fileName);
            if (!File.Exists(path))
            {
                Console.WriteLine("Item relation rules not found: " + path);
                yield break;
            }

            foreach (string raw in File.ReadAllLines(path, Encoding.UTF8))
            {
                string line = raw.Trim();
                if (line.Length > 0 && !line.StartsWith("#", StringComparison.Ordinal))
                {
                    yield return line;
                }
            }
        }

        /// <summary>
        /// The WHERE clauses delete_list.txt uses: icon = N, aoid = N, name = '...',
        /// UPPER(name) LIKE [UPPER]('...'), joined by AND. A trailing -- comment is ignored.
        /// </summary>
        private static Func<Entry, bool> ParseDeleteClause(string line)
        {
            int comment = line.IndexOf("--", StringComparison.Ordinal);
            if (comment >= 0)
            {
                line = line.Substring(0, comment).Trim();
            }

            List<Func<Entry, bool>> parts = new List<Func<Entry, bool>>();
            // Split on AND outside quotes: "Blood Stained and Corroded Crystal %" is one pattern.
            foreach (string term in Regex.Split(line, @"\s+AND\s+(?=(?:[^']*'[^']*')*[^']*$)", RegexOptions.IgnoreCase))
            {
                Match m;
                if ((m = Regex.Match(term, @"^(icon|aoid)\s*=\s*(\d+)$", RegexOptions.IgnoreCase)).Success)
                {
                    int value = int.Parse(m.Groups[2].Value);
                    bool icon = m.Groups[1].Value.Equals("icon", StringComparison.OrdinalIgnoreCase);
                    parts.Add(e => (icon ? e.Icon : e.Id) == value);
                }
                else if ((m = Regex.Match(term, @"^name\s*=\s*'((?:[^']|'')*)'$", RegexOptions.IgnoreCase)).Success)
                {
                    string name = m.Groups[1].Value.Replace("''", "'");
                    parts.Add(e => e.Name == name);
                }
                else if ((m = Regex.Match(term, @"^UPPER\(name\)\s+LIKE\s+(?:UPPER\()?'((?:[^']|'')*)'\)?$", RegexOptions.IgnoreCase)).Success)
                {
                    Regex like = LikeToRegex(m.Groups[1].Value.Replace("''", "'"));
                    parts.Add(e => like.IsMatch(e.Name));
                }
                else
                {
                    return null;
                }
            }

            return e => parts.All(p => p(e));
        }

        /// <summary>SQL LIKE (% any run, _ any one character), case-insensitive like SQLite's.</summary>
        private static Regex LikeToRegex(string pattern)
        {
            StringBuilder regex = new StringBuilder("^");
            foreach (char c in pattern)
            {
                regex.Append(c == '%' ? ".*" : c == '_' ? "." : Regex.Escape(c.ToString()));
            }

            return new Regex(regex.Append('$').ToString(), RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.CultureInvariant);
        }

        private sealed class Entry
        {
            public Entry(int id, int ql, string name, int icon, string itemType)
            {
                this.Id = id;
                this.Ql = ql;
                this.Name = name;
                this.Icon = icon;
                this.ItemType = itemType;
            }

            public int Id { get; private set; }

            public int Ql { get; private set; }

            public string Name { get; private set; }

            public int Icon { get; private set; }

            public string ItemType { get; private set; }
        }
    }
}
