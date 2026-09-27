// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MobExtract.cs" company="OmniCell">
//   Copyright © 2026 OmniCell contributors.
// </copyright>
// <summary>
//   Builds MissionMobs.tsv - the creatures a mission spawns - out of the bot's run recordings.
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

    using SmokeLounge.AOtomation.Messaging.GameData;
    using SmokeLounge.AOtomation.Messaging.Messages;
    using SmokeLounge.AOtomation.Messaging.Messages.N3Messages;
    using SmokeLounge.AOtomation.Messaging.Serialization;

    /// <summary>
    /// What a mission server puts inside the building it generates.
    /// </summary>
    /// <remarks>
    /// The AOBuddy10 bot records every mission it runs as a .pkt beside a
    /// .json summary. A .pkt is frames of
    /// <c>[1 byte direction][8 bytes ms][4 bytes length][packet]</c>,
    /// big-endian, each packet as it was on the wire after decompression, so
    /// the messaging library reads them straight. This walks them and writes
    /// the table <see cref="PoolExtract"/> folds into the mission pool pack.
    ///
    /// Three things have to be got right for the table to mean anything.
    ///
    /// **Which playfield a creature is in.** A recording begins at the zone-in
    /// before the mission, so the creatures the bot walked past outdoors are in
    /// the stream too, and they carry the levels of wherever it was rather than
    /// of the mission. Every <see cref="SimpleCharFullUpdateMessage"/> says
    /// which playfield it is in, and the mission's own instance is on the
    /// <see cref="PlayfieldAnarchyFMessage"/> that opened it, so the cut is the
    /// server's own answer rather than a guess at the building's extent.
    ///
    /// **What the level is measured against.** Not the player's - the bot ran
    /// one character through all of these. The mission's QL, which the
    /// <see cref="QuestFullUpdateMessage"/> in the same stream carries as
    /// <see cref="QuestInfo.Quality"/>. Levels are written as offsets from it.
    ///
    /// **That one body is not one creature.** <c>MonsterData</c> is the body,
    /// and a creature's rank variants share it: 17649 arrives as both
    /// "34 - Automatic" and "34-V worker". So a row is a pool, a body and a
    /// name together.
    ///
    ///     MobExtract &lt;output.tsv&gt; &lt;records directory&gt; [more...]
    ///
    /// The recording the bot is writing right now is opened shared and read as
    /// far as it goes; a half-written frame at the end just ends the walk.
    /// </remarks>
    internal static class MobExtract
    {
        /// <summary>
        /// One creature, in one pool, as the recordings have it.
        /// </summary>
        private sealed class Creature
        {
            public int Pool;

            public int Monster;

            public string Name;

            public int Seen;

            public int MinOffset = int.MaxValue;

            public int MaxOffset = int.MinValue;

            public double HealthPerLevel;

            public int HealthSeen;
        }

        /// <summary>
        /// What one recording was: the mission it is of, and its QL.
        /// </summary>
        private sealed class Run
        {
            public string File;

            public int Playfield;

            public int Pool;

            public int Quality;

            public int Kept;

            public readonly HashSet<int> Counted = new HashSet<int>();

            public int Cut;

            public int Pets;

            public int Missions;

            public int Quests;

            public AcgItem Reward;

            public string MissionInfo;
        }

        private static int Main(string[] args)
        {
            if (args.Length < 2)
            {
                Console.Error.WriteLine("usage: MobExtract <output.tsv> <records directory> [more...]");
                Console.Error.WriteLine();
                Console.Error.WriteLine("  a records directory is one holding the bot's rec-*.pkt files");
                return 1;
            }

            string output = args[0];
            var serializer = new MessageSerializer();
            var table = new Dictionary<string, Creature>();
            var runs = new List<Run>();
            int files = 0, unreadable = 0, noMission = 0, noQuality = 0;

            foreach (string directory in args.Skip(1))
            {
                if (!Directory.Exists(directory))
                {
                    Console.Error.WriteLine("missing: " + directory);
                    return 1;
                }

                foreach (string path in Directory.GetFiles(directory, "rec-*.pkt").OrderBy(p => p))
                {
                    files++;
                    Run run = Walk(serializer, path, table, ref unreadable);
                    if (run == null)
                    {
                        noMission++;
                        continue;
                    }

                    if (run.Quality <= 0)
                    {
                        noQuality++;
                        continue;
                    }

                    runs.Add(run);
                }
            }

            if (runs.Count == 0)
            {
                Console.Error.WriteLine("no usable recordings found");
                return 1;
            }

            Write(output, table.Values);

            Console.WriteLine(
                "{0} recordings, {1} usable, {2} with no mission zone-in, {3} with no QL, {4} unreadable packets",
                files,
                runs.Count,
                noMission,
                noQuality,
                unreadable);
            Console.WriteLine(
                "{0} creatures kept, {1} cut as outside the mission playfield, {2} as pets",
                runs.Sum(r => r.Kept),
                runs.Sum(r => r.Cut),
                runs.Sum(r => r.Pets));
            int many = runs.Count(r => r.Quests > 1);
            if (many > 0)
            {
                Console.WriteLine("note: {0} recordings carry more than one quest", many);
            }

            int crowded = runs.Count(r => r.Missions > 0);
            if (crowded > 0)
            {
                Console.WriteLine(
                    "WARNING: {0} recordings hold more than one mission zone-in; "
                    + "the QL and playfield are taken from the first",
                    crowded);
            }

            Console.WriteLine();

            Console.WriteLine("=== pools");
            foreach (var g in table.Values.GroupBy(c => c.Pool).OrderBy(g => g.Key))
            {
                Run[] mine = runs.Where(r => r.Pool == g.Key).ToArray();
                Console.WriteLine(
                    "  {0,-5} {1,3} creatures over {2,3} bodies   {3,2} runs   QL {4}-{5}",
                    g.Key,
                    g.Count(),
                    g.Select(c => c.Monster).Distinct().Count(),
                    mine.Length,
                    mine.Length == 0 ? 0 : mine.Min(r => r.Quality),
                    mine.Length == 0 ? 0 : mine.Max(r => r.Quality));
            }

            Console.WriteLine();
            Console.WriteLine("=== level offsets from the mission QL, over every row");
            foreach (var g in table.Values
                .GroupBy(c => c.MinOffset)
                .OrderBy(g => g.Key))
            {
                Console.WriteLine("  {0,3}  {1} rows have it as their low end", g.Key, g.Count());
            }

            // The reward item is the same parse and is worth saying out loud:
            // nothing else we hold records what a mission server picked to pay
            // with, and the offers OmniCell generates carry no item at all.
            Console.WriteLine();
            Console.WriteLine("=== reward items the server picked");
            Run[] paid = runs.Where(r => r.Reward != null && r.Reward.LowId != 0).ToArray();
            Console.WriteLine("  {0} of {1} runs carried one", paid.Length, runs.Count);
            foreach (var g in paid.GroupBy(r => r.Reward.LowId).OrderByDescending(g => g.Count()).Take(20))
            {
                Run one = g.First();
                Console.WriteLine(
                    "  {0,6}x  low {1,-8} high {2,-8} ql {3,-4} at mission QL {4}",
                    g.Count(),
                    g.Key,
                    one.Reward.HighId,
                    one.Reward.Quality,
                    string.Join(",", g.Select(r => r.Quality).Distinct().OrderBy(q => q)));
            }

            Console.WriteLine();
            Console.WriteLine("written to " + Path.GetFullPath(output));
            return 0;
        }

        /// <summary>
        /// Reads one recording, adding what it spawned to the table.
        /// </summary>
        /// <remarks>
        /// Two passes over the frames, because the zone-in and the quest can
        /// arrive in either order and a creature is only keepable once both are
        /// known. The frames are cheap to walk twice; deserializing is not, so
        /// the first pass stops at the two messages it wants.
        /// </remarks>
        private static Run Walk(
            MessageSerializer serializer,
            string path,
            Dictionary<string, Creature> table,
            ref int unreadable)
        {
            byte[] all;
            try
            {
                // The bot may be writing this one right now.
                using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    all = new byte[stream.Length];
                    stream.ReadExactly(all, 0, all.Length);
                }
            }
            catch (IOException)
            {
                return null;
            }

            var run = new Run { File = Path.GetFileNameWithoutExtension(path), Playfield = int.MinValue };

            foreach (MessageBody body in Bodies(serializer, all, null))
            {
                var anarchy = body as PlayfieldAnarchyFMessage;
                if (anarchy != null && anarchy.Generator != null)
                {
                    if (run.Playfield == int.MinValue)
                    {
                        run.Playfield = anarchy.PlayfieldId.Instance;
                        run.Pool = anarchy.Generator.TemplatePlayfield;
                    }
                    else
                    {
                        run.Missions++;
                    }

                    continue;
                }

                var quest = body as QuestFullUpdateMessage;
                if (quest == null || quest.QuestInfos == null)
                {
                    continue;
                }

                run.Quests += quest.QuestInfos.Length;

                // The first, to go with the first zone-in. The bot starts
                // recording as it accepts, so a run holds one quest carrying
                // one mission; a recording that ran on into a second holds
                // that one too and it belongs to creatures this run has
                // already cut on playfield.
                if (run.Quality > 0)
                {
                    continue;
                }

                foreach (QuestInfo info in quest.QuestInfos)
                {
                    if (info == null || !info.Quality.HasValue || info.Quality.Value <= 0)
                    {
                        continue;
                    }

                    run.Quality = info.Quality.Value;
                    run.Reward = info.RewardItem;
                    run.MissionInfo = info.ShortInfo;
                    break;
                }
            }

            if (run.Playfield == int.MinValue || run.Quality <= 0)
            {
                return run.Playfield == int.MinValue ? null : run;
            }

            foreach (MessageBody body in Bodies(serializer, all, typeof(SimpleCharFullUpdateMessage)))
            {
                var character = (SimpleCharFullUpdateMessage)body;
                if (character.Level <= 0 || string.IsNullOrEmpty(character.Name))
                {
                    continue;
                }

                // A player is not a creature - a person wears MonsterData of
                // zero - and neither is a pet, which does have a body. The bot
                // is a Meta-Physicist, so its three pets stand in every mission
                // it runs: they arrive as "Rage Materialization", "Salvinous"
                // and "Deranged Mindreaver", at a level that follows the bot
                // rather than the mission, and each turned up in every pool.
                //
                // The test is PetMaster, which carries the owner's dynel, and
                // not the IsPet flag: that bit reads set on ordinary mission
                // creatures too - a Rhinoman Smasher with no master has it -
                // so filtering on it throws away most of the table.
                if (character.MonsterData == 0 || character.PetMaster != null)
                {
                    run.Pets++;
                    continue;
                }

                if (character.PlayfieldId != run.Playfield)
                {
                    run.Cut++;
                    continue;
                }

                // The server resends a character as the bot walks back into
                // view; Seen is meant to be how many of it a mission had, so
                // each dynel counts once in the run it was seen in.
                if (!run.Counted.Add(character.Identity.Instance))
                {
                    continue;
                }

                run.Kept++;
                string key = run.Pool + "|" + character.MonsterData + "|" + character.Name;
                Creature creature;
                if (!table.TryGetValue(key, out creature))
                {
                    creature = new Creature
                    {
                        Pool = run.Pool,
                        Monster = (int)character.MonsterData,
                        Name = character.Name,
                    };
                    table.Add(key, creature);
                }

                creature.Seen++;
                int offset = character.Level - run.Quality;
                if (offset < creature.MinOffset)
                {
                    creature.MinOffset = offset;
                }

                if (offset > creature.MaxOffset)
                {
                    creature.MaxOffset = offset;
                }

                if (character.Health > 0)
                {
                    creature.HealthPerLevel += character.Health / (double)character.Level;
                    creature.HealthSeen++;
                }
            }

            unreadable += Unreadable;
            Unreadable = 0;
            return run;
        }

        /// <summary>
        /// How many packets in the last walk would not deserialize.
        /// </summary>
        private static int Unreadable;

        /// <summary>
        /// Every server message in a recording, optionally only one kind.
        /// </summary>
        private static IEnumerable<MessageBody> Bodies(
            MessageSerializer serializer,
            byte[] all,
            Type only)
        {
            int offset = 0;
            while (offset + 13 <= all.Length)
            {
                bool fromServer = all[offset] == 0;
                int length = (all[offset + 9] << 24) | (all[offset + 10] << 16) | (all[offset + 11] << 8)
                             | all[offset + 12];
                offset += 13;
                if (length < 0 || offset + length > all.Length)
                {
                    // A half-written frame at the end of a live recording.
                    yield break;
                }

                if (!fromServer || length < 20)
                {
                    offset += length;
                    continue;
                }

                Message message = null;
                try
                {
                    using (var stream = new MemoryStream(all, offset, length, false))
                    {
                        message = serializer.Deserialize(stream);
                    }
                }
                catch (Exception)
                {
                    Unreadable++;
                }

                offset += length;
                if (message == null || message.Body == null)
                {
                    continue;
                }

                if (only != null && message.Body.GetType() != only)
                {
                    continue;
                }

                yield return message.Body;
            }
        }

        /// <summary>
        /// Writes the table in the shape <see cref="PoolExtract"/> reads.
        /// </summary>
        private static void Write(string output, IEnumerable<Creature> creatures)
        {
            var text = new StringBuilder();
            text.AppendLine("# The creatures a mission server spawns, from the bot's packet recordings.");
            text.AppendLine("# Written by Tools/Capture/MobExtract - do not edit by hand, re-run it.");
            text.AppendLine("#");
            text.AppendLine("# Only creatures inside the generated building: a recording starts at the");
            text.AppendLine("# previous zone-in, so the outdoor ones on the way there are in the stream too");
            text.AppendLine("# and are cut by the playfield each one says it is in.");
            text.AppendLine("# monster is SimpleCharFullUpdate.MonsterData - the body, shared by the rank");
            text.AppendLine("# variants of one creature: 17649 is both \"34 - Automatic\" and \"34-V worker\".");
            text.AppendLine("# Level is given against the mission QL, because that is what it tracks, not the");
            text.AppendLine("# player's. Only the pools the bot has run appear.");
            // Commented, because that is how PoolExtract skips it.
            text.AppendLine("# pool\tmonster\tname\tseen\tminLevelOffset\tmaxLevelOffset\thealthPerLevel");

            foreach (Creature creature in creatures
                .OrderBy(c => c.Pool)
                .ThenBy(c => c.Monster)
                .ThenBy(c => c.Name, StringComparer.Ordinal))
            {
                text.AppendLine(
                    string.Join(
                        "\t",
                        creature.Pool.ToString(CultureInfo.InvariantCulture),
                        creature.Monster.ToString(CultureInfo.InvariantCulture),
                        creature.Name.Replace('\t', ' '),
                        creature.Seen.ToString(CultureInfo.InvariantCulture),
                        creature.MinOffset.ToString(CultureInfo.InvariantCulture),
                        creature.MaxOffset.ToString(CultureInfo.InvariantCulture),
                        (creature.HealthSeen == 0
                             ? 0.0
                             : creature.HealthPerLevel / creature.HealthSeen).ToString(
                                 "0.0",
                                 CultureInfo.InvariantCulture)));
            }

            File.WriteAllText(output, text.ToString());
        }
    }
}
