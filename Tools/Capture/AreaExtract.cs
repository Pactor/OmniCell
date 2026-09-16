using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Text;

using ICSharpCode.SharpZipLib.Zip.Compression;

using OmniCell.Core.Content;
using OmniCell.Core.Playfields;
using OmniCell.Core.Statels;
using OmniCell.Database.Dao;
using OmniCell.Database.Entities;

using SmokeLounge.AOtomation.Messaging.GameData;

using Utility;

// Pulls a playfield out of one or more captures and writes it out as data.
//
//     AreaExtract <out-dir> <streams.csv> [<streams.csv> ...]
//                 [--playfield N] [--as N]
//
//   --playfield  only keep things the server placed in this playfield id, as it
//                appears on the wire. Arete Landing is instanced, so the id in
//                a capture is the instance (2150461) rather than 6553.
//   --as         write that playfield id into the SQL instead. Defaults to
//                --playfield.
//
// Writes into <out-dir>:
//   npcs.csv         every distinct character the server spawned
//   statics.csv      vending machines, terminals and the like
//   vendors.csv      what each vending machine had on its shelves
//   corpses.sql      what each kind of creature leaves behind
//   quests.md        quest text, objectives and rewards
//   quests.sql       the same as INSERTs, with objectives where they parse
//   dialogue.md      NPC conversations in the order they were said
//   mobspawns.sql    npcs.csv as INSERTs, with the stat rows and weapons a
//                    spawn needs
//
// Captured fields and emulator decisions must remain distinguishable. World,
// item, dialogue and quest fields come from decoded messages. Quest objective
// targets may be resolved against captured marker positions, and Requires may
// contain a clearly documented reachability ordering when separate capture
// runs do not overlap. Those are representations made by OmniCell, not values
// claimed to have appeared on the wire. Unresolved content is reported.
//
// A capture records whatever was standing there, and that includes other
// players' pets, which are not part of the playfield and leave when their owner
// does. The live server marks them: a pet's SimpleCharFullUpdate has a non-zero
// PetType, and those characters are left out (see TakeNpc). Arete Landing had
// thirteen - Bureaucrat Workers and Engineer Automatons - and an Anger
// Manifestation that took the client down when anyone clicked it.
//
// mobspawns.sql still ends with the older, weaker test - a spawn whose name
// appears inside the brackets of an item name - run against the database,
// since this tool does not know the item names.
internal static class AreaExtract
{
    private const int HeaderLength = 16;

    private const int SizeOffset = 6;

    private static short BigEndianInt16(byte[] data, int offset)
    {
        return (short)((data[offset] << 8) | data[offset + 1]);
    }

    private static bool IsZlibHeader(byte[] d, int i)
    {
        return i + 1 < d.Length && (d[i] & 0x0F) == 8 && (((d[i] << 8) | d[i + 1]) % 31) == 0;
    }

    private static byte[] InflateAll(byte[] input, int startOffset)
    {
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

                pos += result.Length > before ? Math.Max((int)inflater.TotalIn, 1) : 1;
            }

            return result.ToArray();
        }
    }

    private static int Padding(int size, int alignment)
    {
        return alignment <= 1 ? 0 : (alignment - (size % alignment)) % alignment;
    }

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

    /// <summary>
    /// A named member of a deserialised message body.
    /// </summary>
    /// <remarks>
    /// This throws when the member is not there, and that is the whole point.
    /// It used to return null, and every quest this tool ever wrote had no
    /// giver because it asked for "Unknown5" - a name the message class had
    /// since changed to QuestGiver. Nothing failed; a null read as zero and
    /// 114 quests went into the database given by nobody.
    ///
    /// The message classes are still being renamed as fields get proven, so a
    /// name in here going stale is the normal case, not the exceptional one.
    /// It has to be loud.
    /// </remarks>
    private static object Get(object o, string name)
    {
        if (o == null)
        {
            return null;
        }

        PropertyInfo p = o.GetType().GetProperty(name);
        if (p == null)
        {
            throw new ArgumentException(o.GetType().Name + " has no member called " + name);
        }

        return p.GetValue(o, null);
    }

    /// <summary>
    /// A named member that a body of this shape may legitimately not have.
    /// </summary>
    /// <remarks>
    /// Unlike <see cref="Get"/> this returns null instead of throwing, and it
    /// is for the one case where that is right: a record that is polymorphic.
    /// The Info on a movement message is one of several shapes, and asking one
    /// of the others for its X is a question rather than a mistake.
    ///
    /// Everywhere else, use Get. A member that is missing because the class was
    /// renamed has to stop the tool, not read as zero.
    /// </remarks>
    private static object Maybe(object o, string name)
    {
        if (o == null)
        {
            return null;
        }

        PropertyInfo p = o.GetType().GetProperty(name);
        return p == null ? null : p.GetValue(o, null);
    }

    private static string Str(object o)
    {
        if (o == null)
        {
            return string.Empty;
        }

        if (o is float)
        {
            return ((float)o).ToString("0.####", CultureInfo.InvariantCulture);
        }

        return Convert.ToString(o, CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Reads a wire value as an int.
    /// </summary>
    /// <remarks>
    /// MonsterData, HeadMesh and Appearance are unsigned on the wire and do use
    /// the top bit, so a straight Convert.ToInt32 overflows on them. The stat
    /// columns they end up in are signed 32 bit, and the client reads the same
    /// bits back either way, so the value is reinterpreted rather than clamped.
    /// </remarks>
    private static int Int(object o)
    {
        if (o == null)
        {
            return 0;
        }

        if (o is uint)
        {
            return unchecked((int)(uint)o);
        }

        if (o is ulong)
        {
            return unchecked((int)(ulong)o);
        }

        return Convert.ToInt32(o, CultureInfo.InvariantCulture);
    }

    private static string Csv(string s)
    {
        s = s ?? string.Empty;
        return s.IndexOfAny(new[] { ',', '"', '\n', '\r' }) < 0 ? s : "\"" + s.Replace("\"", "\"\"") + "\"";
    }

    private static string Sql(string s)
    {
        return (s ?? string.Empty).Replace("\\", "\\\\").Replace("'", "\\'");
    }

    /// <summary>
    /// One piece a character is drawn from.
    /// </summary>
    /// <remarks>
    /// A humanoid is a list of these - head, body, arms, legs - each on a body
    /// position and a draw layer, optionally with a texture that replaces the
    /// mesh's own. A monster has none: its shape comes from its monster data
    /// instead, and its list is genuinely empty.
    /// </remarks>
    /// <summary>
    /// One thing a character says, and what you can say back.
    /// </summary>
    /// <remarks>
    /// A KnuBot conversation arrives as runs of text followed by a list of
    /// answers. The text is what the character says; the list is what the
    /// window offers you. One of each is a step, and a conversation is a
    /// sequence of them.
    /// </remarks>
    private sealed class Step
    {
        public readonly List<string> Says = new List<string>();

        public readonly List<string> Answers = new List<string>();

        /// <summary>
        /// The answer index the client actually sent for this step, or -1 when
        /// the client half was not captured or the step was left unanswered.
        /// </summary>
        public int SelectedAnswer = -1;

        /// <summary>
        /// The quest the player was handed at this step, or zero.
        /// </summary>
        public int Grants;
    }

    private sealed class AoMesh
    {
        public int Position, Id, OverrideTextureId, Layer;
    }

    private sealed class Npc
    {
        public int Instance;

        public string Name = string.Empty;

        public float X, Y, Z, HX, HY, HZ, HW;

        public int Level, Health, Playfield, RunSpeed;

        // Health is the most the character can have; HealthDamage is how far below that it stood.
        // The Wounded Dockworkers at Arete Landing are all 32 with 20 of damage, lying on the
        // ground (20260914-124401 s4).
        public int HealthDamage;

        // The movement mode, the first byte of the movement state in VehicleData (offset 12).
        // Walking and running creatures say 1 to 3; the wounded lying about Arete Landing say 8,
        // which is sitting on the ground.
        public int MoveMode;

        public bool IsNpc;

        public uint Appearance;

        public int Side, Fatness, Breed, Gender, Race;

        public int Family, HeadMesh, MonsterData, MonsterScale, VisualFlags, CharacterFlags;

        public readonly List<int> TextureIds = new List<int>();

        public readonly List<AoMesh> Meshes = new List<AoMesh>();
    }

    /// <summary>
    /// What a character was seen hitting for.
    /// </summary>
    private sealed class Damage
    {
        public int Min = int.MaxValue;

        public int Max;

        public int Swings;
    }

    private sealed class Corpse
    {
        public string MobName = string.Empty;

        public readonly Dictionary<int, int> Stats = new Dictionary<int, int>();

        public int Unknown20, Unknown23;
    }

    private sealed class Weapon
    {
        public int Owner, Type, Instance, InventoryId, BodyLocation, ItemFlags, LowId, HighId, Quality, Unknown6, Unknown7;

        public int? ItemDelay, RechargeDelay, Energy;
    }

    private sealed class Quest
    {
        public int Id;

        public string Name = string.Empty;

        public string Description = string.Empty;

        public int GiverId, GiverType, IconId, CashReward, ExperienceReward;

        /// <summary>
        /// The one action the quest carries, which is its objective.
        /// </summary>
        /// <remarks>
        /// Every captured quest has exactly one. Kind is the action record's
        /// own version and says what has to be done; Target names a fixture
        /// when the objective is to use one; and the marker is where the thing
        /// to be done is, which is how the objective is resolved to a character
        /// or a fixture standing there.
        /// </remarks>
        public int Kind, TargetType, TargetInstance, MarkerPlayfield, Requires;

        /// <summary>
        /// How many of whatever the objective is about, or zero when it is not
        /// a thing you do a number of times.
        /// </summary>
        public int Needs;

        /// <summary>
        /// The character whose conversation was open when this quest first
        /// appeared in the log, which is the character who gave it.
        /// </summary>
        public int TalkedTo;

        /// <summary>
        /// The last character talked to when this quest was first seen, whether
        /// or not the log gained it at that moment.
        /// </summary>
        /// <remarks>
        /// Weaker than <see cref="TalkedTo"/> and used only when that is empty.
        /// A quest already in the log the first time a session shows it was
        /// handed over before the capture began, so nothing saw it happen; the
        /// character the player was last standing in front of is a better guess
        /// than the chain owner, and is often right because a capture opens
        /// where the last one left off.
        /// </remarks>
        public int NearTalkedTo;

        /// <summary>
        /// Where in the run this quest first appeared. The order the player was
        /// given them, which is the order the chain goes in.
        /// </summary>
        public int Seen;

        public float MarkerX, MarkerY, MarkerZ;

        public bool HasAction;

        // QuestInfo fields whose values vary by quest.
        public int WireGiverType, WireGiverInstance, QuestCode, UnknownHash, Quality;
        public int TimeLimit, Unknown20, Unknown21, Unknown22, Unknown23Type, Unknown23Instance;
        public int Unknown25, Unknown26;

        // The captured quest action. Its tracking instance is allocated at
        // runtime, but its type and every authored field come from this row.
        public int ActionVersion, ActionType, ActionInstance;
        public int ActionUnknown1Type, ActionUnknown1Instance, ActionUnknown2Type, ActionUnknown2Instance;
        public int ActionUnknown3Type, ActionUnknown3Instance, ActionUnknown4Type, ActionUnknown4Instance;
        public float ActionUnknown5, ActionUnknown6, ActionUnknown7, ActionUnknown8;
        public int ActionUnknown9Type, ActionUnknown9Instance;
        public float ActionUnknown10, ActionUnknown11, ActionUnknown12, ActionUnknown13;
        public int ActionUnknown14Type, ActionUnknown14Instance, ActionDeadline, ActionUnknown16;
        public int ActionTrackingType, ActionPlayfieldType, ActionPlayfieldInstance;
        public int ActionUnknown18, ActionUnknown19;
        public readonly List<int[]> WireRewards = new List<int[]>();
    }

    private sealed class Static
    {
        public int Type, Instance, Playfield;

        public float X, Y, Z, HX, HY, HZ, HW;

        public string Kind = string.Empty;

        public readonly List<string> Stats = new List<string>();

        public readonly List<int[]> WireStats = new List<int[]>();

        /// <summary>
        /// The character whose shop this is, or zero when it stands on its own.
        /// </summary>
        /// <remarks>
        /// A vending machine is a fixture with a position. A shopkeeper's stock
        /// is the same kind of object with no position at all, because where it
        /// is is wherever its character is - and the character is named right
        /// here, in the short three-byte form the quest givers use.
        /// </remarks>
        public int Npc;
    }

    private static void Main(string[] args)
    {
        if (args.Length < 2)
        {
            Console.WriteLine("usage: AreaExtract <out-dir> <streams.csv> [...] [--playfield N] [--as N] [--statels playfields.ocp]");
            return;
        }

        string outDir = args[0];
        int wantPlayfield = 0;
        int writeAs = 0;
        string statelFile = null;
        var inputs = new List<string>();

        for (int i = 1; i < args.Length; i++)
        {
            if (args[i] == "--playfield" && i + 1 < args.Length)
            {
                int.TryParse(args[++i], out wantPlayfield);
            }
            else if (args[i] == "--as" && i + 1 < args.Length)
            {
                int.TryParse(args[++i], out writeAs);
            }
            else if (args[i] == "--statels" && i + 1 < args.Length)
            {
                statelFile = args[++i];
            }
            else
            {
                inputs.Add(args[i]);
            }
        }

        if (writeAs == 0)
        {
            writeAs = wantPlayfield;
        }

        Directory.CreateDirectory(outDir);

        // Beside the exe, not wherever it happens to be run from.
        Assembly messaging = Assembly.LoadFrom(
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "SmokeLounge.AOtomation.Messaging.dll"));
        Type serializerType = messaging.GetType("SmokeLounge.AOtomation.Messaging.Serialization.MessageSerializer");
        object serializer = Activator.CreateInstance(serializerType);
        MethodInfo deserialize = serializerType.GetMethod("Deserialize", new[] { typeof(Stream) });

        var npcs = new Dictionary<int, Npc>();
        var damage = new Dictionary<int, Damage>();
        var paths = new Dictionary<string, List<List<float[]>>>();
        var statics = new Dictionary<string, Static>();
        var quests = new Dictionary<int, string>();
        var shops = new Dictionary<int, List<string>>();
        var questRows = new Dictionary<int, Quest>();
        var weapons = new Dictionary<int, Weapon>();
        var corpses = new Dictionary<string, Corpse>();
        var timeline = new List<int[]>();
        var alive = new HashSet<int>();
        var dialogue = new List<string>();
        var questProtocol = new List<string>();
        var questFields = new List<string>();
        var speakerNames = new Dictionary<int, string>();

        // Characters the live server described as somebody's pet. See TakeNpc.
        var pets = new HashSet<int>();

        // Conversations, by the session that heard them and the character who
        // was talking. Per session because a conversation is a walk through a
        // tree and two sessions take different turnings; the longest walk of
        // each character is the one kept.
        var scripts = new Dictionary<string, Dictionary<int, List<Step>>>();

        // The order each session watched the quest log grow.
        var runs = new Dictionary<string, List<int>>();
        int skipped = 0;

        foreach (string input in inputs)
        {
            var streams = new Dictionary<string, List<byte>>();
            var clientStreams = new Dictionary<string, List<byte>>();
            foreach (string line in File.ReadAllLines(input))
            {
                string[] parts = line.Split(',');
                if (parts.Length < 3 || parts[2].Length < 2
                    || (parts[1] != "server" && parts[1] != "client"))
                {
                    continue;
                }

                Dictionary<string, List<byte>> direction = parts[1] == "server" ? streams : clientStreams;
                if (!direction.ContainsKey(parts[0]))
                {
                    direction[parts[0]] = new List<byte>();
                }

                string hex = parts[2].Replace(":", string.Empty).Trim();
                for (int i = 0; i + 1 < hex.Length; i += 2)
                {
                    direction[parts[0]].Add(Convert.ToByte(hex.Substring(i, 2), 16));
                }
            }

            foreach (var entry in streams.OrderByDescending(s => s.Value.Count))
            {
                string session = input + "|" + entry.Key;
                int talkingTo = 0;

                // Nobody is standing anywhere until this session says so.
                //
                // How many of a thing are alive at once is the whole basis for
                // how many spawn points it gets, and it was being counted
                // across every capture at once: a character seen in one session
                // was still counted as standing there while a second session
                // was read, so the count only ever went up. Thirty four
                // Malfunctioning Cleaning Robots came out of an area that holds
                // about ten.
                alive.Clear();
                timeline.Add(new[] { SessionBreak, 0 });

                // What the player's quest log held last time the server sent
                // it, so the next one can be read as a difference.
                var held = new List<int>();
                byte[] raw = entry.Value.ToArray();
                byte[] data = raw;

                for (int start = 0; start + 1 < Math.Min(raw.Length, 4096); start++)
                {
                    if (!IsZlibHeader(raw, start))
                    {
                        continue;
                    }

                    byte[] candidate = InflateAll(raw, start);
                    if (candidate.Length > 0 && CountFramable(candidate, 3, 1) >= 1)
                    {
                        data = candidate;
                        break;
                    }
                }

                int alignment = DetectAlignment(data);
                int pos = 0;

                while (pos + HeaderLength <= data.Length)
                {
                    short size = BigEndianInt16(data, pos + SizeOffset);
                    if (size < HeaderLength || pos + size > data.Length)
                    {
                        pos++;
                        continue;
                    }

                    var packet = new byte[size];
                    Array.Copy(data, pos, packet, 0, size);
                    pos += size + Padding(size, alignment);

                    object body;
                    try
                    {
                        using (var ms = new MemoryStream(packet))
                        {
                            object msg = deserialize.Invoke(serializer, new object[] { ms });
                            body = msg == null ? null : Get(msg, "Body");
                        }
                    }
                    catch (Exception)
                    {
                        continue;
                    }

                    if (body == null)
                    {
                        continue;
                    }

                    switch (body.GetType().Name)
                    {
                        case "AttackInfoMessage":
                            TakeDamage(body, damage);
                            break;

                        case "FollowTargetMessage":
                            TakePath(body, paths, session);
                            break;

                        case "SimpleCharFullUpdateMessage":
                        {
                            // A character can be introduced more than once - the
                            // live server takes them away as you walk off and
                            // gives them back when you return - so the timeline
                            // records an arrival only when it is really one.
                            int who = Int(Get(Get(body, "Identity"), "Instance"));
                            if (alive.Add(who))
                            {
                                timeline.Add(new[] { who, 1 });
                            }

                            TakeNpc(body, npcs, speakerNames, pets, wantPlayfield, ref skipped);
                            break;
                        }

                        case "DespawnMessage":
                        {
                            int who = Int(Get(Get(body, "Identity"), "Instance"));
                            if (alive.Remove(who))
                            {
                                timeline.Add(new[] { who, -1 });
                            }

                            // The route stops here. Everything after it is the
                            // character being seen again somewhere else, and the
                            // step between the two is not a walk - it is where
                            // we stopped looking.
                            Break(paths, session, who);
                            break;
                        }

                        case "VendingMachineFullUpdateMessage":
                            TakeStatic(body, statics, "VendingMachine", wantPlayfield);
                            break;

                        // Everything else the playfield is furnished with -
                        // terminals, doors, the cargo box a quest asks you to
                        // open. They arrive on the same message as an item in a
                        // bag and are told apart by having a position.
                        case "SimpleItemFullUpdateMessage":
                            TakeStatic(
                                body,
                                statics,
                                Str(Get(Get(body, "Identity"), "Type")),
                                wantPlayfield);
                            break;

                        case "CorpseFullUpdateMessage":
                        {
                            TakeCorpse(body, corpses);

                            break;
                        }

                        case "WeaponItemFullUpdateMessage":
                            TakeWeapon(body, weapons, wantPlayfield);
                            break;

                        case "ShopUpdateMessage":
                            TakeShop(body, shops);
                            break;

                        case "QuestFullUpdateMessage":
                            TakeQuestFields(body, session, questFields);
                            TakeQuests(body, quests, questRows, talkingTo, held, Run(runs, session), scripts, session);
                            break;

                        case "QuestMessage":
                            questProtocol.Add(
                                string.Join(
                                    "\t",
                                    "QuestMessage",
                                    Int(Get(body, "Unknown")),
                                    Int(Get(body, "Version")),
                                    Int(Get(body, "Unknown2")),
                                    Str(Get(Get(body, "QuestIdentity"), "Type")),
                                    Int(Get(Get(body, "QuestIdentity"), "Instance")),
                                    Str(Get(Get(body, "Unknown3"), "Type")),
                                    Int(Get(Get(body, "Unknown3"), "Instance"))));
                            break;

                        case "CharacterActionMessage":
                            if (string.Equals(Str(Get(body, "Action")), "MissionChanged", StringComparison.Ordinal))
                            {
                                questProtocol.Add(
                                    string.Join(
                                        "\t",
                                        "MissionChanged",
                                        Int(Get(body, "Unknown")),
                                        Int(Get(body, "Unknown1")),
                                        Str(Get(Get(body, "Target"), "Type")),
                                        Int(Get(Get(body, "Target"), "Instance")),
                                        Int(Get(body, "Parameter1")),
                                        Int(Get(body, "Parameter2")),
                                        Int(Get(body, "Unknown2"))));
                            }

                            break;

                        // Who handed a quest over is not on the quest. What is
                        // on the wire is the conversation that was open when it
                        // turned up in the log, and that is the same thing.
                        case "KnuBotOpenChatWindowMessage":
                            talkingTo = Int(Get(Get(body, "Target"), "Instance"));
                            break;

                        // Not cleared when the window closes, and that is a
                        // decision rather than an oversight.
                        //
                        // KnuBotCloseChatWindow is in the captures and decodes
                        // fine, and leaving the conversation set for ever is
                        // plainly wrong on its face: a quest granted with no
                        // window open - by handing another one in, or by using
                        // something - lands on whoever was last spoken to.
                        //
                        // Both ways of ending it were measured against the one
                        // thing here that is known independently, which is that
                        // Rex Larsson gives you his robots and then his cargo
                        // box:
                        //
                        //   clear on close        30 of 38 quests attributed;
                        //                         Rex loses the robots quest
                        //                         entirely, because taking a
                        //                         quest closes the window and
                        //                         the log arrives after it
                        //   clear one log later   34 attributed but Rex heads
                        //                         with Deliver Antonio's, and
                        //                         picks up Return to Marcus
                        //   clear on the next
                        //   message of any kind   30 strong, and the seven that
                        //                         fall through to QuestGiver
                        //                         put him back to nine
                        //   never clear           28 strong, 8 near, 2 from
                        //                         QuestGiver, and every chain
                        //                         head is right
                        //
                        // The last one wins on the evidence available. What
                        // keeps it honest is not the clearing but the
                        // difference: only what the log gains is credited to a
                        // conversation at all, and the three grades are written
                        // into the file per quest so a wrong one is visible.
                        //
                        // Worth revisiting with a capture taken specifically of
                        // quest hand-ins, where the close and the grant can be
                        // seen next to each other rather than inferred.

                        case "KnuBotAppendTextMessage":
                            dialogue.Add(
                                Speaker(Get(body, "Target"), speakerNames) + ": " + Str(Get(body, "Text")));
                            Saying(scripts, session, Int(Get(Get(body, "Target"), "Instance")))
                                .Says.Add(Str(Get(body, "Text")));
                            break;

                        case "KnuBotAnswerListMessage":
                        {
                            int who = Int(Get(Get(body, "Target"), "Instance"));
                            Step step = Saying(scripts, session, who);
                            foreach (object option in (System.Collections.IEnumerable)Get(body, "DialogOptions")
                                     ?? new object[0])
                            {
                                dialogue.Add(
                                    "    > " + Str(Get(option, "Text"))
                                    + "   [to " + Speaker(Get(body, "Target"), speakerNames) + "]");
                                step.Answers.Add(Str(Get(option, "Text")));
                            }

                            // The answers close the step. Whatever is said next
                            // belongs to the one after it.
                            scripts[session][who].Add(new Step());
                            break;
                        }
                    }
                }

                List<byte> clientBytes;
                if (clientStreams.TryGetValue(entry.Key, out clientBytes))
                {
                    Dictionary<int, List<int>> selected =
                        ReadSelectedAnswers(clientBytes.ToArray(), deserialize, serializer);
                    int decoded = selected.Values.Sum(x => x.Count);
                    int attached = AttachSelectedAnswers(scripts, session, selected);
                    if (decoded > 0)
                    {
                        Console.WriteLine(
                            "answers   " + decoded + " client selections decoded, " + attached
                            + " paired with a captured answer list in " + Path.GetFileName(input)
                            + " stream " + entry.Key);
                    }
                }
            }
        }

        Thin(npcs, timeline);

        // Other players' pets were standing there too. They belong to whoever summoned them, not to
        // the playfield, so they are not spawned.
        int petsDropped = pets.Count(npcs.Remove);

        WriteNpcs(outDir, npcs, weapons, damage, paths, writeAs);
        WriteStatics(outDir, statics, writeAs);
        WriteStaticDynels(outDir, statics, writeAs);
        WriteQuests(outDir, quests);
        float arriveX;
        float arriveZ;
        Arrival(statelFile, writeAs, out arriveX, out arriveZ);
        WriteQuestSql(outDir, questRows, npcs, statics, runs, arriveX, arriveZ, writeAs);
        WriteQuestWireSql(outDir, questRows);
        WriteScripts(outDir, scripts, npcs, questRows, writeAs);
        WriteCorpses(outDir, corpses, npcs);
        WriteShops(outDir, shops);
        WriteVendors(outDir, statics, shops, npcs, writeAs, statelFile);
        File.WriteAllLines(Path.Combine(outDir, "dialogue.md"), dialogue);
        File.WriteAllLines(
            Path.Combine(outDir, "quest-protocol.tsv"),
            new[] { "Kind\tUnknown\tVersionOrUnknown1\tUnknown2OrTargetType\tQuestTypeOrTargetInstance\tQuestInstanceOrParameter1\tUnknown3TypeOrParameter2\tUnknown3InstanceOrUnknown2" }
                .Concat(questProtocol));
        File.WriteAllLines(
            Path.Combine(outDir, "quest-fields.tsv"),
            new[] { "Session\tQuestId\tField\tValue" }.Concat(questFields));

        Console.WriteLine("npcs      " + npcs.Count + " distinct (" + skipped + " skipped, wrong playfield)");
        Console.WriteLine("pets      " + petsDropped + " player pets left out");
        if (legLengths.Count > 0)
        {
            legLengths.Sort();
            Console.WriteLine(
                "routes    " + routesWritten + " patrols of " + legLengths.Count + " legs; median "
                + legLengths[legLengths.Count / 2].ToString("0.0", CultureInfo.InvariantCulture)
                + " units, shortest " + legLengths[0].ToString("0.0", CultureInfo.InvariantCulture)
                + ", longest " + legLengths[legLengths.Count - 1].ToString("0.0", CultureInfo.InvariantCulture)
                + "; " + (legsRunning * 100 / legLengths.Count) + "% at a run");
        }

        Console.WriteLine("weapons   " + weapons.Count + " characters armed");
        Console.WriteLine(
            "meshes    " + npcs.Values.Count(n => n.Meshes.Count > 0) + " characters are built from "
            + npcs.Values.Sum(n => n.Meshes.Count) + " meshes; the rest are shaped by their monster data");
        Console.WriteLine("corpses   " + corpses.Count + " kinds of creature");
        Console.WriteLine("statics   " + statics.Count);
        Console.WriteLine("quests.md " + quests.Count + " written up");
        Console.WriteLine("shops     " + shops.Count + " with stock, across every playfield captured");
        Console.WriteLine("dialogue  " + dialogue.Count + " lines");
        Console.WriteLine("written to " + Path.GetFullPath(outDir));
    }

    private static string Speaker(object identity, Dictionary<int, string> names)
    {
        int instance = Int(Get(identity, "Instance"));
        return names.ContainsKey(instance) ? names[instance] : "npc " + instance;
    }

    private static void TakeNpc(
        object body,
        Dictionary<int, Npc> npcs,
        Dictionary<int, string> names,
        HashSet<int> pets,
        int wantPlayfield,
        ref int skipped)
    {
        object playfieldId = Get(body, "PlayfieldId");
        if (playfieldId == null)
        {
            return;
        }

        int playfield = Int(playfieldId);
        int instance = Int(Get(Get(body, "Identity"), "Instance"));
        string name = Str(Get(body, "Name"));

        if (name.Length > 0)
        {
            names[instance] = name;
        }

        if (wantPlayfield != 0 && playfield != wantPlayfield)
        {
            skipped++;
            return;
        }

        // A player's pet says so. The NPC block's PetType is non-zero on every pet in the retail
        // captures (Bureaucrat Workers, Engineer Automatons, Anger Manifestations, metaphysicist
        // demons) and zero on every ordinary creature. Checked on every update, not only the first:
        // a pet is often introduced before it has been given to its master. PetMaster is not the
        // test - a charmed creature has a master for a while and is still the playfield's own.
        object npcInfo = Get(body, "CharacterInfo");
        if (npcInfo != null && npcInfo.GetType().Name == "SimpleNpcInfo" && Int(Get(npcInfo, "PetType")) != 0)
        {
            pets.Add(instance);
        }

        if (npcs.ContainsKey(instance) || name.Length == 0)
        {
            return;
        }

        object coords = Get(body, "Coordinates");
        object heading = Get(body, "Heading");

        var npc = new Npc
                  {
                      Instance = instance,
                      Name = name,
                      Playfield = playfield,
                      X = (float)(Get(coords, "X") ?? 0f),
                      Y = (float)(Get(coords, "Y") ?? 0f),
                      Z = (float)(Get(coords, "Z") ?? 0f),
                      HX = (float)(Get(heading, "X") ?? 0f),
                      HY = (float)(Get(heading, "Y") ?? 0f),
                      HZ = (float)(Get(heading, "Z") ?? 0f),
                      HW = (float)(Get(heading, "W") ?? 0f),
                      Level = Int(Get(body, "Level")),
                      Health = Int(Get(body, "Health")),
                      HealthDamage = Int(Get(body, "HealthDamage")),
                      IsNpc = Get(body, "CharacterInfo") != null
                              && Get(body, "CharacterInfo").GetType().Name == "SimpleNpcInfo",
                      Appearance = Convert.ToUInt32(Get(Get(body, "Appearance"), "Value") ?? 0u),
                      HeadMesh = Int(Get(body, "HeadMesh")),
                      MonsterData = Int(Get(body, "MonsterData")),
                      MonsterScale = Int(Get(body, "MonsterScale")),
                      VisualFlags = Int(Get(body, "VisualFlags")),
                      CharacterFlags = Int(Get(body, "CharacterFlags"))
                  };

        object appearance = Get(body, "Appearance");
        npc.Side = Int(Get(appearance, "Side"));
        npc.Fatness = Int(Get(appearance, "Fatness"));
        npc.Breed = Int(Get(appearance, "Breed"));
        npc.Gender = Int(Get(appearance, "Gender"));
        npc.Race = Int(Get(appearance, "Race"));

        npc.RunSpeed = Int(Get(body, "RunSpeedBase"));

        byte[] vehicle = Get(body, "VehicleData") as byte[];
        npc.MoveMode = vehicle != null && vehicle.Length > 12 ? vehicle[12] : 0;

        object info = Get(body, "CharacterInfo");
        if (info != null && info.GetType().Name == "SimpleNpcInfo")
        {
            npc.Family = Int(Get(info, "Family"));
        }

        foreach (object mesh in (System.Collections.IEnumerable)Get(body, "Meshes") ?? new object[0])
        {
            npc.Meshes.Add(
                new AoMesh
                {
                    Position = Int(Get(mesh, "Position")),
                    Id = Int(Get(mesh, "Id")),
                    OverrideTextureId = Int(Get(mesh, "OverrideTextureId")),
                    Layer = Int(Get(mesh, "Layer"))
                });
        }

        foreach (object texture in (System.Collections.IEnumerable)Get(body, "Textures") ?? new object[0])
        {
            npc.TextureIds.Add(Int(Get(texture, "Id")));
        }

        npcs[instance] = npc;
    }

    private static void TakeStatic(
        object body,
        Dictionary<string, Static> statics,
        string kind,
        int wantPlayfield)
    {
        // VendingMachineFullUpdate calls them PlayfieldId and Coordinates;
        // SimpleItemFullUpdate calls the same two things Playfield and
        // Coordinate. Same record, two spellings.
        int playfield = Int(Maybe(body, "PlayfieldId") ?? Maybe(body, "Playfield"));
        if (wantPlayfield != 0 && playfield != wantPlayfield)
        {
            return;
        }

        object identity = Get(body, "Identity");
        int type = Int(Get(identity, "Type"));
        int instance = Int(Get(identity, "Instance"));
        string key = type + "/" + instance;
        if (statics.ContainsKey(key))
        {
            return;
        }

        object coords = Maybe(body, "Coordinates") ?? Maybe(body, "Coordinate");
        object heading = Get(body, "Heading");
        int npc = Int(Get(Maybe(body, "NpcIdentity"), "Instance"));
        if (coords == null && npc == 0)
        {
            return;
        }

        var st = new Static
                 {
                     Type = type,
                     Instance = instance,
                     Playfield = playfield,
                     Kind = kind,
                     Npc = npc,
                     X = (float)(Maybe(coords, "X") ?? 0f),
                     Y = (float)(Maybe(coords, "Y") ?? 0f),
                     Z = (float)(Maybe(coords, "Z") ?? 0f),
                     HX = (float)(Maybe(heading, "X") ?? 0f),
                     HY = (float)(Maybe(heading, "Y") ?? 0f),
                     HZ = (float)(Maybe(heading, "Z") ?? 0f),
                     HW = (float)(Maybe(heading, "W") ?? 1f)
                 };

        foreach (object stat in (System.Collections.IEnumerable)Get(body, "Stats") ?? new object[0])
        {
            st.Stats.Add(Str(Get(stat, "Value1")) + "=" + Str(Get(stat, "Value2")));
            st.WireStats.Add(
                new[] { Int(Get(stat, "Value1")), unchecked((int)Convert.ToInt64(Get(stat, "Value2"))) });
        }

        statics[key] = st;
    }

    /// <summary>
    /// Records what a vending machine was selling.
    /// </summary>
    /// <remarks>
    /// ShopUpdate carries item ids and quality, not names or prices - the
    /// client looks those up in its own resource database. Keyed by the vending
    /// machine's identity so it lines up with a row in statics.csv.
    /// </remarks>
    /// <summary>
    /// Records the weapon a character was holding.
    /// </summary>
    /// <remarks>
    /// One WeaponItemFullUpdate follows the SimpleCharFullUpdate of every armed
    /// character. Keyed by the weapon's own identity so a character seen twice
    /// does not produce the weapon twice.
    /// </remarks>
    private static void TakeWeapon(object body, Dictionary<int, Weapon> weapons, int wantPlayfield)
    {
        if (wantPlayfield != 0 && Int(Get(body, "Playfield")) != wantPlayfield)
        {
            return;
        }

        object identity = Get(body, "Identity");
        int instance = Int(Get(identity, "Instance"));
        if (weapons.ContainsKey(instance))
        {
            return;
        }

        // The item is described by a stat list rather than by fields of its own.
        // Every captured shape has the common seven; some add itemdelay and
        // rechargedelay. Preserve both shapes and the separate placement bytes.
        var stats = new Dictionary<int, long>();
        foreach (object stat in (System.Collections.IEnumerable)Get(body, "Stats") ?? new object[0])
        {
            stats[Int(Get(stat, "Value1"))] = Convert.ToInt64(Get(stat, "Value2"));
        }

        weapons[instance] = new Weapon
                            {
                                Owner = Int(Get(Get(body, "Owner"), "Instance")),
                                Type = Int(Get(identity, "Type")),
                                Instance = instance,
                                InventoryId = Int(Get(body, "InventoryId")),
                                BodyLocation = Int(Get(body, "BodyLocation")),
                                ItemFlags = unchecked((int)Held(stats, StatFlags)),
                                LowId = unchecked((int)Held(stats, StatAcgItemTemplateId)),
                                HighId = unchecked((int)Held(stats, StatAcgItemTemplateId2)),
                                Quality = unchecked((int)Held(stats, StatAcgItemLevel)),
                                Unknown6 = unchecked((int)Held(stats, StatStaticInstance)),
                                Unknown7 = unchecked((int)Held(stats, StatMultipleCount)),
                                ItemDelay = Optional(stats, StatItemDelay),
                                RechargeDelay = Optional(stats, StatRechargeDelay),
                                Energy = Optional(stats, StatEnergy)
                            };
    }

    /// <summary>
    /// The stat ids WeaponItemFullUpdate describes an item with.
    /// </summary>
    private const int StatFlags = 0;

    private const int StatStaticInstance = 23;

    private const int StatMultipleCount = 412;

    private const int StatAcgItemLevel = 701;

    private const int StatAcgItemTemplateId = 702;

    private const int StatAcgItemTemplateId2 = 703;

    private const int StatItemDelay = 294;

    private const int StatRechargeDelay = 210;

    private const int StatEnergy = 26;

    private static int? Optional(Dictionary<int, long> stats, int stat)
    {
        long value;
        return stats.TryGetValue(stat, out value) ? unchecked((int)value) : (int?)null;
    }

    private static string SqlNullable(int? value)
    {
        return value.HasValue ? value.Value.ToString(CultureInfo.InvariantCulture) : "NULL";
    }

    private static long Held(Dictionary<int, long> stats, int stat)
    {
        long value;
        return stats.TryGetValue(stat, out value) ? value : 0L;
    }

    /// <summary>
    /// Records what one kind of creature leaves behind.
    /// </summary>
    /// <remarks>
    /// The corpse's name is "Remains of X", where X is the creature. Keyed on
    /// the creature so that a hundred and fifty corpses of ten kinds produce ten
    /// rows.
    /// </remarks>
    /// <summary>
    /// What a character hit for, from the swings the capture saw.
    /// </summary>
    /// <remarks>
    /// AttackInfo names the attacker in its Identity and carries the damage in
    /// the first field. Both sides of a fight send it, so a player's swings end
    /// up in here too - harmless, because nothing reads a player's entry.
    ///
    /// A miss is a MissedAttackInfo and is not counted, so the range is the
    /// range of hits.
    /// </remarks>
    private static void TakeDamage(object body, Dictionary<int, Damage> damage)
    {
        int attacker = Int(Get(Get(body, "Identity"), "Instance"));
        int hit = Int(Get(body, "Damage"));

        // The stat list's marker for "nobody has set this" turns up in captured
        // fields too, and it is not a number of hit points.
        if (attacker == 0 || hit <= 0 || hit == 1234567890)
        {
            return;
        }

        Damage seen;
        if (!damage.TryGetValue(attacker, out seen))
        {
            seen = new Damage();
            damage[attacker] = seen;
        }

        seen.Min = Math.Min(seen.Min, hit);
        seen.Max = Math.Max(seen.Max, hit);
        seen.Swings++;
    }

    /// <summary>
    /// How far apart two points are on the ground, ignoring height.
    /// </summary>
    private static double Apart(float[] a, float[] b)
    {
        double dx = a[0] - b[0];
        double dz = a[2] - b[2];
        return Math.Sqrt((dx * dx) + (dz * dz));
    }

    /// <summary>
    /// The piece of a character's route that is still being written.
    /// </summary>
    /// <remarks>
    /// A route is kept in pieces rather than as one line because we only see a
    /// character while it is near enough for the server to describe it. It
    /// walks out of range, the server despawns it, and when it comes back it is
    /// somewhere else - not because it went there in one step, but because
    /// nobody was watching.
    ///
    /// Keyed by session as well as by character: an identity is handed out per
    /// session and the numbers are reused, so instance 2052536067 is Rex
    /// Larsson in one capture and something else in the next.
    /// </remarks>
    private static List<float[]> Segment(
        Dictionary<string, List<List<float[]>>> paths,
        string session,
        int who)
    {
        string key = session + "\u0000" + who;
        List<List<float[]>> segments;
        if (!paths.TryGetValue(key, out segments))
        {
            segments = new List<List<float[]>>();
            paths[key] = segments;
        }

        if (segments.Count == 0)
        {
            segments.Add(new List<float[]>());
        }

        return segments[segments.Count - 1];
    }

    private static void Break(Dictionary<string, List<List<float[]>>> paths, string session, int who)
    {
        List<List<float[]>> segments;
        if (paths.TryGetValue(session + "\u0000" + who, out segments) && (segments.Count > 0)
            && (segments[segments.Count - 1].Count > 0))
        {
            segments.Add(new List<float[]>());
        }
    }

    /// <summary>
    /// Where a character was told to walk.
    /// </summary>
    /// <remarks>
    /// FollowTarget is how the live server moves an NPC: it names where the
    /// character is and where it is going, and the client walks it between the
    /// two. Collecting the destinations in order gives the patrol route the
    /// server was walking it around, which is a real route rather than a guess
    /// at one.
    ///
    /// MoveMode says how: 25 is running, anything else walking.
    ///
    /// Only distinct places are kept. A mob told to go somewhere it already is,
    /// or nudged a few centimetres, is not describing a route.
    /// </remarks>
    private static void TakePath(
        object body,
        Dictionary<string, List<List<float[]>>> paths,
        string session)
    {
        int who = Int(Get(Get(body, "Identity"), "Instance"));
        object info = Get(body, "Info");

        // Two records share this message and only one of them is a route.
        //
        // FollowCoordinateInfo is the server saying "this character is walking
        // from here to there": a start, an end, and the mode it is moving in.
        // 5135 of the 5370 in one capture of Arete Landing are these, and they
        // are the patrol legs.
        //
        // FollowTargetInfo is "this character is chasing that one". Its X, Y, Z
        // are wherever the quarry happened to be, which is a chase and not a
        // route, and writing those down as waypoints gives a spawn a patrol
        // made of somebody else's movements.
        object end = Maybe(info, "EndCoordinates");
        if (who == 0 || end == null)
        {
            return;
        }

        object from = Get(info, "CurrentCoordinates");

        // MoveMode is 24 for a walk and 25 for a run - the CharDCMove numbers,
        // not the CurrentMovementMode ones the name suggests. Both appear: 3316
        // walks against 1839 runs in one capture of the playfield.
        var point = new[]
                        {
                            Convert.ToSingle(Get(end, "X")), Convert.ToSingle(Get(end, "Y")),
                            Convert.ToSingle(Get(end, "Z")), Int(Get(info, "MoveMode")) == 25 ? 1f : 0f
                        };

        // Keyed by the session the character was seen in as well as by the
        // character. A live server hands out an identity per session and reuses
        // the numbers, so instance 2052536067 is Rex Larsson in one capture and
        // something else in the next. Chaining those into one route is what
        // makes a Cleaning Robot cross the playfield in a single step.
        // Every leg says where it starts as well as where it ends, so a route
        // can be checked rather than assumed: leg n + 1 has to begin where leg
        // n finished. When it does not, the character got from one to the other
        // by something that is not walking - it was killed and put back, or it
        // spent the gap out of sight - and the two do not belong on one route.
        var startsAt = new[]
                           {
                               Convert.ToSingle(Get(from, "X")), Convert.ToSingle(Get(from, "Y")),
                               Convert.ToSingle(Get(from, "Z"))
                           };

        List<float[]> route = Segment(paths, session, who);
        if (route.Count > 0 && Apart(route[route.Count - 1], startsAt) > 1.0)
        {
            Break(paths, session, who);
            route = Segment(paths, session, who);
        }

        if (route.Count == 0)
        {
            route.Add(new[] { startsAt[0], startsAt[1], startsAt[2], point[3] });
        }

        if (Apart(route[route.Count - 1], point) < 1.0)
        {
            return;
        }

        route.Add(point);
    }

    private static void TakeCorpse(object body, Dictionary<string, Corpse> corpses)
    {
        string name = Str(Get(body, "Name"));
        const string Prefix = "Remains of ";
        if (!name.StartsWith(Prefix, StringComparison.Ordinal))
        {
            return;
        }

        string mob = name.Substring(Prefix.Length);
        if (corpses.ContainsKey(mob))
        {
            return;
        }

        // The two per-creature values are arguments of the one effect a corpse
        // carries, not fields of the message - game function 53031's third and
        // sixth. CorpseFullUpdateMessageHandler writes them back to the same
        // two offsets.
        var corpse = new Corpse { MobName = mob };
        foreach (object effect in (System.Collections.IEnumerable)Get(body, "NanoEffects") ?? new object[0])
        {
            var arguments = (byte[])Get(effect, "Arguments");
            if (arguments != null && arguments.Length >= 24)
            {
                corpse.Unknown20 = BitConverter.ToInt32(arguments, 8);
                corpse.Unknown23 = BitConverter.ToInt32(arguments, 20);
            }

            break;
        }

        foreach (object stat in (System.Collections.IEnumerable)Get(body, "Stats") ?? new object[0])
        {
            corpse.Stats[Int(Get(stat, "Value1"))] = Int(Get(stat, "Value2"));
        }

        corpses[mob] = corpse;
    }

    private static void WriteCorpses(string outDir, Dictionary<string, Corpse> corpses, Dictionary<int, Npc> npcs)
    {
        // "Remains of X" is named the same way for a player as for a creature,
        // and a player's corpse is not something a creature leaves behind. Any
        // corpse named after a player character the captures saw is left out.
        // One whose player was never seen standing cannot be told apart here.
        HashSet<string> players = PlayerNames(npcs);

        var sql = new List<string>
                  {
                      "-- What each kind of creature leaves behind, read out of the corpses the",
                      "-- live server sent. Everything that differs between one creature and",
                      "-- another; the values that are the same for all of them are constants in",
                      "-- CorpseFullUpdateMessageHandler.",
                      "--",
                      "-- CatMesh is the corpse model, DeadTimer how long it lies there, Cash",
                      "-- what is on it.",
                      string.Empty
                  };

        foreach (Corpse c in corpses.Values.Where(c => !players.Contains(c.MobName)).OrderBy(c => c.MobName))
        {
            sql.Add(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "REPLACE INTO mobcorpses (MobName, CatMesh, TimeExist, Cash, CanChangeClothes,"
                    + " MonsterScale, Breed, Sex, Race, HeadMesh, DeadTimer, Unknown20, Unknown23)"
                    + " VALUES ('{0}', {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}, {9}, {10}, {11}, {12});",
                    Sql(c.MobName),
                    StatOr(c, 42, 0),
                    StatOr(c, 8, 0),
                    StatOr(c, 61, 0),
                    StatOr(c, 223, 0),
                    StatOr(c, 360, 100),
                    StatOr(c, 4, 0),
                    StatOr(c, 59, 0),
                    StatOr(c, 89, 1),
                    StatOr(c, 64, 0),
                    StatOr(c, 34, 60),
                    c.Unknown20,
                    c.Unknown23));
        }

        File.WriteAllLines(Path.Combine(outDir, "corpses.sql"), sql);
    }

    private static int StatOr(Corpse corpse, int stat, int fallback)
    {
        int value;
        return corpse.Stats.TryGetValue(stat, out value) ? value : fallback;
    }

    private static void TakeShop(object body, Dictionary<int, List<string>> shops)
    {
        int instance = Int(Get(Get(body, "Identity"), "Instance"));
        if (shops.ContainsKey(instance))
        {
            return;
        }

        var slots = new List<string>();
        foreach (object slot in (System.Collections.IEnumerable)Get(body, "VendingMachineSlots") ?? new object[0])
        {
            slots.Add(
                Str(Get(slot, "ItemLowId")) + ":" + Str(Get(slot, "ItemHighId")) + ":"
                + Str(Get(slot, "Quality")));
        }

        shops[instance] = slots;
    }

    /// <summary>
    /// The vending machines of a playfield, with what they were selling.
    /// </summary>
    /// <remarks>
    /// A vendor is not a spawn. It is a statel - a fixture the playfield file
    /// already places - and the server builds one for every VendingMachine
    /// statel it finds there whether the database knows it or not. What the
    /// database supplies is the stock, through two hops: a vendors row names a
    /// vendortemplate by hash, and that names a set of shopinventorytemplates
    /// rows by another hash.
    ///
    /// So the join that matters is captured machine to statel, and it is made
    /// on position because nothing else survives: the identity the live server
    /// gave a machine is per session, and the statel's own identity is not on
    /// the wire at all. Eight of Arete Landing's captured machines sit exactly
    /// on a statel - to the last decimal, not near it - which is what makes the
    /// match safe to do this way.
    ///
    /// Two do not, and they are reported rather than placed. The playfield file
    /// is 18.8.50 and the captures are 18.8.62; the area gained machines in
    /// between, and a machine with no statel has nowhere to stand until the
    /// playfield file is rebuilt.
    /// </remarks>
    private static void WriteVendors(
        string outDir,
        Dictionary<string, Static> statics,
        Dictionary<int, List<string>> shops,
        Dictionary<int, Npc> npcs,
        int playfield,
        string statelFile)
    {
        if (statelFile == null)
        {
            return;
        }

        List<StatelData> statels = VendingMachineStatels(statelFile, playfield);
        if (statels == null)
        {
            Console.WriteLine("vendors   no playfield " + playfield + " in " + statelFile);
            return;
        }

        var sql = new List<string>
                  {
                      "-- The vending machines of playfield " + playfield + " and what they sell,",
                      "-- read out of the ShopUpdates the live server sent.",
                      "--",
                      "-- A machine is placed by the playfield file, not by this. What is here is",
                      "-- the link from each one to its stock: a vendors row, the vendortemplate",
                      "-- it names, and the shopinventorytemplates rows that template names.",
                      "--",
                      "-- The hash is the playfield and the statel's index, so it says which",
                      "-- machine it belongs to and cannot collide with another playfield's. The",
                      "-- three columns that hold one were seven and four characters wide, which",
                      "-- is not enough for that, so they are widened here too.",
                      string.Empty,
                      "ALTER TABLE vendors MODIFY `Hash` varchar(32) NOT NULL;",
                      "ALTER TABLE vendortemplate MODIFY `Hash` varchar(32) NOT NULL;",
                      "ALTER TABLE vendortemplate MODIFY `ShopInvHash` varchar(32) NOT NULL;",
                      string.Empty,
                      "DELETE FROM vendors WHERE Playfield = " + playfield + ";",
                      "DELETE FROM vendortemplate WHERE Hash LIKE '" + playfield + "-%';",
                      "DELETE FROM shopinventorytemplates WHERE Hash LIKE '" + playfield + "-%';",
                      string.Empty
                  };

        int placed = 0;
        var homeless = new List<Static>();
        var bare = new List<StatelData>(statels);

        foreach (Static machine in statics.Values
            .Where(s => s.Kind == "VendingMachine" && shops.ContainsKey(s.Instance))
            .OrderBy(s => s.Instance))
        {
            StatelData statel = Standing(machine, statels);
            if (statel == null)
            {
                homeless.Add(machine);
                continue;
            }

            bare.Remove(statel);
            placed++;

            int index = (statel.Identity.Instance >> 16) & 0xff;
            string hash = playfield + "-" + index;
            int template = Template(machine);

            sql.Add(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "INSERT INTO vendors (Id, Playfield, X, Y, Z, HeadingX, HeadingY, HeadingZ, HeadingW,"
                    + " Name, TemplateId, Hash)"
                    + " VALUES ({0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}, '', {9}, '{10}');",
                    (playfield << 16) | index,
                    playfield,
                    Str(statel.X),
                    Str(statel.Y),
                    Str(statel.Z),
                    Str(statel.HeadingX),
                    Str(statel.HeadingY),
                    Str(statel.HeadingZ),
                    Str(statel.HeadingW),
                    template,
                    hash));

            // The machine's name is the name of the item it is built from, and
            // the server already has a table of those. Taking it from there as
            // the row goes in beats copying it into this file and letting the
            // two drift.
            sql.Add(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "INSERT INTO vendortemplate (Hash, Lvl, Name, ItemTemplate, ShopInvHash, MinQl, MaxQl,"
                    + " Buy, Sell, Skill) SELECT '{0}', 1,"
                    + " COALESCE((SELECT Name FROM itemnames WHERE Id = {1}), ''), {1}, '{0}', 1, 500,"
                    + " 0.05, 1.00, 161;",
                    hash,
                    template));

            foreach (string slot in shops[machine.Instance])
            {
                string[] parts = slot.Split(':');
                sql.Add(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "INSERT INTO shopinventorytemplates (Hash, LowId, HighId, MinQl, MaxQl,"
                        + " MultipleCount, AdminDescription, Active) VALUES ('{0}', {1}, {2}, {3}, {3}, 1,"
                        + " '', 1);",
                        hash,
                        parts[0],
                        parts[1],
                        parts[2]));
            }

            sql.Add(string.Empty);
        }

        // Shopkeepers. A machine stands on a statel; a shopkeeper's stock has
        // no position because it goes wherever its character goes, and the
        // character is named on the record.
        int carried = 0;
        var noCharacter = new List<Static>();
        foreach (Static shop in statics.Values
            .Where(s => s.Npc != 0 && shops.ContainsKey(s.Instance))
            .OrderBy(s => s.Instance))
        {
            Npc keeper = Shopkeeper(shop.Npc, npcs);
            if (keeper == null)
            {
                noCharacter.Add(shop);
                continue;
            }

            string hash = playfield + "-s" + shop.Instance;
            int template = Template(shop);
            carried++;

            sql.Add(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "INSERT INTO vendors (Id, Playfield, X, Y, Z, HeadingX, HeadingY, HeadingZ, HeadingW,"
                    + " Name, TemplateId, Hash, Npc)"
                    + " VALUES ({0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}, '', {9}, '{10}', {11});",
                    shop.Instance,
                    playfield,
                    Str(keeper.X),
                    Str(keeper.Y),
                    Str(keeper.Z),
                    Str(keeper.HX),
                    Str(keeper.HY),
                    Str(keeper.HZ),
                    Str(keeper.HW),
                    template,
                    hash,
                    keeper.Instance));

            sql.Add(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "INSERT INTO vendortemplate (Hash, Lvl, Name, ItemTemplate, ShopInvHash, MinQl, MaxQl,"
                    + " Buy, Sell, Skill) SELECT '{0}', 1,"
                    + " COALESCE((SELECT Name FROM itemnames WHERE Id = {1}), ''), {1}, '{0}', 1, 500,"
                    + " 0.05, 1.00, 162;",
                    hash,
                    template));

            foreach (string slot in shops[shop.Instance])
            {
                string[] parts = slot.Split(':');
                sql.Add(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "INSERT INTO shopinventorytemplates (Hash, LowId, HighId, MinQl, MaxQl,"
                        + " MultipleCount, AdminDescription, Active) VALUES ('{0}', {1}, {2}, {3}, {3}, 1,"
                        + " '', 1);",
                        hash,
                        parts[0],
                        parts[1],
                        parts[2]));
            }

            sql.Add("-- " + keeper.Name + " sells " + shops[shop.Instance].Count + " things.");
            sql.Add(string.Empty);
        }

        foreach (Static shop in noCharacter)
        {
            sql.Add(
                "-- NEEDS REVIEW: a shop of " + shops[shop.Instance].Count + " things belongs to character "
                + shop.Npc + ", who is not spawned here.");
        }

        foreach (Static machine in homeless)
        {
            sql.Add(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "-- NEEDS REVIEW: a machine at {0:0.##},{1:0.##},{2:0.##} selling {3} things stands on"
                    + " no statel. The playfield file predates it.",
                    machine.X,
                    machine.Y,
                    machine.Z,
                    shops[machine.Instance].Count));
        }

        File.WriteAllLines(Path.Combine(outDir, "vendors.sql"), sql);
        Console.WriteLine(
            "vendors   " + placed + " machines stocked and " + carried + " shopkeepers; "
            + homeless.Count + " machines with no statel to stand on, " + bare.Count
            + " statels nobody opened, " + noCharacter.Count + " shops whose character is not here");
    }

    /// <summary>
    /// The spawned character a shop record names as its owner.
    /// </summary>
    /// <remarks>
    /// The identity on the record is the same three-byte short form the quest
    /// givers carry - 0x573709 against a spawn's 0x7A573709 - so it is matched
    /// the same way, and only when exactly one spawned character matches it.
    /// </remarks>
    private static Npc Shopkeeper(int npc, Dictionary<int, Npc> npcs)
    {
        Npc exact;
        if (npcs.TryGetValue(npc, out exact) && exact.IsNpc)
        {
            return exact;
        }

        Npc found = null;
        foreach (Npc candidate in npcs.Values)
        {
            if (!candidate.IsNpc || (candidate.Instance & 0xFFFFFF) != (npc & 0xFFFFFF))
            {
                continue;
            }

            if (found != null)
            {
                return null;
            }

            found = candidate;
        }

        return found;
    }

    /// <summary>
    /// The statel a captured machine is standing on, or null.
    /// </summary>
    /// <remarks>
    /// Half a unit. The eight that match are exact and the nearest miss is 5.8
    /// units away, so there is no judgement in the number - anything in between
    /// would do the same thing.
    /// </remarks>
    private static StatelData Standing(Static machine, List<StatelData> statels)
    {
        foreach (StatelData statel in statels)
        {
            float dx = statel.X - machine.X;
            float dy = statel.Y - machine.Y;
            float dz = statel.Z - machine.Z;
            if (Math.Sqrt((dx * dx) + (dy * dy) + (dz * dz)) < 0.5)
            {
                return statel;
            }
        }

        return null;
    }

    /// <summary>
    /// The item template a machine is built from - its StaticInstance stat.
    /// </summary>
    private static int Template(Static machine)
    {
        const string Named = "StaticInstance=";
        foreach (string stat in machine.Stats)
        {
            if (stat.StartsWith(Named, StringComparison.Ordinal))
            {
                int value;
                int.TryParse(stat.Substring(Named.Length), out value);
                return value;
            }
        }

        return 0;
    }

    private static List<StatelData> VendingMachineStatels(string statelFile, int playfield)
    {
        foreach (PlayfieldData data in OmniCellContentPack.ReadPlayfields(statelFile))
        {
            if (data.PlayfieldId != playfield)
            {
                continue;
            }

            return data.Statels.Where(s => s.Identity.Type == IdentityType.VendingMachine)
                .OrderBy(s => s.Identity.Instance)
                .ToList();
        }

        return null;
    }

    private static void WriteShops(string outDir, Dictionary<int, List<string>> shops)
    {
        var csv = new List<string> { "VendingMachineInstance,Slot,ItemLowId,ItemHighId,Quality" };
        foreach (var shop in shops.OrderBy(s => s.Key))
        {
            for (var i = 0; i < shop.Value.Count; i++)
            {
                string[] parts = shop.Value[i].Split(':');
                csv.Add(shop.Key + "," + i + "," + parts[0] + "," + parts[1] + "," + parts[2]);
            }
        }

        File.WriteAllLines(Path.Combine(outDir, "vendors.csv"), csv);
    }

    /// <summary>
    /// How many quests have been seen, so each can remember when it arrived.
    /// </summary>
    private static int questsSeen;

    /// <summary>
    /// Writes every decoded QuestInfo field exactly as it appeared. This is a
    /// tall audit table, not distributable content: it exists so production
    /// packet values are never selected from a majority or filled by guess.
    /// </summary>
    private static void TakeQuestFields(object body, string session, List<string> rows)
    {
        foreach (object info in (System.Collections.IEnumerable)Get(body, "QuestInfos") ?? new object[0])
        {
            int id = Int(Get(Get(info, "QuestIdentity"), "Instance"));
            foreach (PropertyInfo property in info.GetType().GetProperties().OrderBy(p => p.Name))
            {
                rows.Add(
                    CleanTsv(session) + "\t" + id.ToString(CultureInfo.InvariantCulture) + "\t"
                    + property.Name + "\t" + CleanTsv(AuditValue(property.GetValue(info, null), 0)));
            }
        }
    }

    private static string AuditValue(object value, int depth)
    {
        if (value == null)
        {
            return "<null>";
        }

        Type type = value.GetType();
        if (value is string || type.IsPrimitive || type.IsEnum || value is decimal)
        {
            return Convert.ToString(value, CultureInfo.InvariantCulture);
        }

        if (depth >= 3)
        {
            return "<" + type.Name + ">";
        }

        var enumerable = value as System.Collections.IEnumerable;
        if (enumerable != null)
        {
            var values = new List<string>();
            foreach (object item in enumerable)
            {
                values.Add(AuditValue(item, depth + 1));
            }

            return "[" + string.Join("|", values) + "]";
        }

        return "{"
               + string.Join(
                   ",",
                   type.GetProperties()
                       .Where(p => p.GetIndexParameters().Length == 0)
                       .OrderBy(p => p.Name)
                       .Select(p => p.Name + "=" + AuditValue(p.GetValue(value, null), depth + 1)))
               + "}";
    }

    private static string CleanTsv(string value)
    {
        return (value ?? string.Empty).Replace("\t", "\\t").Replace("\r", "\\r").Replace("\n", "\\n");
    }

    /// <summary>
    /// A quest log, and who gave whatever is new in it.
    /// </summary>
    /// <remarks>
    /// The message is the whole log, not the one quest that just arrived, so
    /// the character whose conversation is open did not give all of it. They
    /// gave the difference.
    ///
    /// Without that reading, the first log the server sends while a player is
    /// standing in front of Rex Larsson credits him with every quest that
    /// player is already carrying. That is how he came to hold nine of them,
    /// then three. He holds one.
    ///
    /// The first log of a session is all difference and none of it is his -
    /// nobody was talking to anybody when the player logged in - so it only
    /// records what is there.
    /// </remarks>
    private static void TakeQuests(
        object body,
        Dictionary<int, string> quests,
        Dictionary<int, Quest> questRows,
        int talkingTo,
        List<int> held,
        List<int> run,
        Dictionary<string, Dictionary<int, List<Step>>> scripts,
        string session)
    {
        bool firstLog = held.Count == 0;
        var now = new List<int>();
        foreach (object info in (System.Collections.IEnumerable)Get(body, "QuestInfos") ?? new object[0])
        {
            int id = Int(Get(Get(info, "QuestIdentity"), "Instance"));
            now.Add(id);

            // Everything the log gains, in the order it gained it, whether or
            // not anybody happened to be talking at the time.
            if (!firstLog && !held.Contains(id) && !run.Contains(id))
            {
                run.Add(id);
            }

            bool granted = !firstLog && !held.Contains(id) && talkingTo != 0;
            if (questRows.ContainsKey(id))
            {
                if (granted && questRows[id].TalkedTo == 0)
                {
                    questRows[id].TalkedTo = talkingTo;
                }

                if (granted)
                {
                    Handed(scripts, session, talkingTo, id);
                }

                if (talkingTo != 0 && questRows[id].NearTalkedTo == 0)
                {
                    questRows[id].NearTalkedTo = talkingTo;
                }
            }

            if (quests.ContainsKey(id))
            {
                continue;
            }

            questRows[id] = new Quest
                            {
                                Id = id,
                                Name = Str(Get(info, "ShortInfo")),
                                Description = Str(Get(info, "Info")),
                                TalkedTo = granted ? talkingTo : 0,
                                NearTalkedTo = talkingTo,
                                Seen = ++questsSeen,
                                GiverId = Int(Get(Get(info, "QuestGiver"), "Instance")),
                                GiverType = Int(Get(Get(info, "QuestGiver"), "Type")),
                                Needs = Int(Get(info, "Unknown21")),
                                IconId = Int(Get(info, "MissionIconId")),
                                CashReward = Int(Get(info, "CashReward")),
                                ExperienceReward = Int(Get(info, "ExperienceReward")),
                                WireGiverType = Int(Get(Get(info, "QuestGiver"), "Type")),
                                WireGiverInstance = Int(Get(Get(info, "QuestGiver"), "Instance")),
                                QuestCode = Int(Get(info, "QuestCode")),
                                UnknownHash = Int(Get(info, "UnknownHash")),
                                Quality = Int(Get(info, "Quality")),
                                TimeLimit = Int(Get(info, "TimeLimit")),
                                Unknown20 = Int(Get(info, "Unknown20")),
                                Unknown21 = Int(Get(info, "Unknown21")),
                                Unknown22 = Int(Get(info, "Unknown22")),
                                Unknown23Type = Int(Get(Get(info, "Unknown23"), "Type")),
                                Unknown23Instance = Int(Get(Get(info, "Unknown23"), "Instance")),
                                Unknown25 = Int(Get(info, "Unknown25")),
                                Unknown26 = Int(Get(info, "Unknown26"))
                            };

            foreach (object reward in (System.Collections.IEnumerable)Get(info, "ItemRewards") ?? new object[0])
            {
                questRows[id].WireRewards.Add(
                    new[]
                        {
                            Int(Get(reward, "LowId")), Int(Get(reward, "HighId")),
                            Int(Get(reward, "Quality")), Int(Get(reward, "Unknown1"))
                        });
            }

            foreach (object action in (System.Collections.IEnumerable)Get(info, "QuestActions") ?? new object[0])
            {
                Quest row = questRows[id];
                row.HasAction = true;
                row.Kind = Int(Get(action, "Version"));
                row.TargetType = Int(Get(Get(action, "Action"), "Type"));
                row.TargetInstance = Int(Get(Get(action, "Action"), "Instance"));
                row.MarkerPlayfield = Int(Get(Get(action, "Playfield"), "Instance"));
                row.MarkerX = Convert.ToSingle(Get(action, "X"));
                row.MarkerY = Convert.ToSingle(Get(action, "Y"));
                row.MarkerZ = Convert.ToSingle(Get(action, "Z"));
                row.ActionVersion = Int(Get(action, "Version"));
                row.ActionType = Int(Get(Get(action, "Action"), "Type"));
                row.ActionInstance = Int(Get(Get(action, "Action"), "Instance"));
                row.ActionUnknown1Type = Int(Get(Get(action, "Unknown1"), "Type"));
                row.ActionUnknown1Instance = Int(Get(Get(action, "Unknown1"), "Instance"));
                row.ActionUnknown2Type = Int(Get(Get(action, "Unknown2"), "Type"));
                row.ActionUnknown2Instance = Int(Get(Get(action, "Unknown2"), "Instance"));
                row.ActionUnknown3Type = Int(Get(Get(action, "Unknown3"), "Type"));
                row.ActionUnknown3Instance = Int(Get(Get(action, "Unknown3"), "Instance"));
                row.ActionUnknown4Type = Int(Get(Get(action, "Unknown4"), "Type"));
                row.ActionUnknown4Instance = Int(Get(Get(action, "Unknown4"), "Instance"));
                row.ActionUnknown5 = Convert.ToSingle(Get(action, "Unknown5"));
                row.ActionUnknown6 = Convert.ToSingle(Get(action, "Unknown6"));
                row.ActionUnknown7 = Convert.ToSingle(Get(action, "Unknown7"));
                row.ActionUnknown8 = Convert.ToSingle(Get(action, "Unknown8"));
                row.ActionUnknown9Type = Int(Get(Get(action, "Unknown9"), "Type"));
                row.ActionUnknown9Instance = Int(Get(Get(action, "Unknown9"), "Instance"));
                row.ActionUnknown10 = Convert.ToSingle(Get(action, "Unknown10"));
                row.ActionUnknown11 = Convert.ToSingle(Get(action, "Unknown11"));
                row.ActionUnknown12 = Convert.ToSingle(Get(action, "Unknown12"));
                row.ActionUnknown13 = Convert.ToSingle(Get(action, "Unknown13"));
                row.ActionUnknown14Type = Int(Get(Get(action, "Unknown14"), "Type"));
                row.ActionUnknown14Instance = Int(Get(Get(action, "Unknown14"), "Instance"));
                row.ActionDeadline = Int(Get(action, "Deadline"));
                row.ActionUnknown16 = Int(Get(action, "Unknown16"));
                row.ActionTrackingType = Int(Get(Get(action, "Unknown17"), "Type"));
                row.ActionPlayfieldType = Int(Get(Get(action, "Playfield"), "Type"));
                row.ActionPlayfieldInstance = Int(Get(Get(action, "Playfield"), "Instance"));
                row.ActionUnknown18 = Int(Get(action, "Unknown18"));
                row.ActionUnknown19 = Int(Get(action, "Unknown19"));
                break;
            }

            var sb = new StringBuilder();
            sb.AppendLine("## " + Str(Get(info, "ShortInfo")));
            sb.AppendLine();
            sb.AppendLine("- quest id: " + id + " (0x" + id.ToString("x") + ")");
            sb.AppendLine("- given by: " + Int(Get(Get(info, "QuestGiver"), "Type")) + ":"
                          + Int(Get(Get(info, "QuestGiver"), "Instance")));
            sb.AppendLine("- mission icon: " + Int(Get(info, "MissionIconId")));
            sb.AppendLine("- cash reward: " + Int(Get(info, "CashReward")));
            sb.AppendLine("- experience reward: " + Int(Get(info, "ExperienceReward")));
            sb.AppendLine();
            sb.AppendLine(Str(Get(info, "Info")));
            sb.AppendLine();
            quests[id] = sb.ToString();
        }

        held.Clear();
        held.AddRange(now);
    }

    /// <summary>
    /// Converts the captured QuestInfo records into normalized SQL. Only quest
    /// ids actually selected by WriteQuestSql are written; rejected or
    /// out-of-playfield capture records never leak into the content patch.
    /// </summary>
    private static void WriteQuestWireSql(string outDir, Dictionary<int, Quest> questRows)
    {
        string questsPath = Path.Combine(outDir, "quests.sql");
        var selected = new HashSet<int>();
        foreach (string line in File.ReadAllLines(questsPath))
        {
            Match match = Regex.Match(line, @"^REPLACE INTO quests .*VALUES \(([-0-9]+),");
            int id;
            if (match.Success
                && int.TryParse(match.Groups[1].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out id))
            {
                selected.Add(id);
            }
        }

        var sql = new List<string>
                      {
                          "-- Normalized emulator-owned representation of captured QuestInfo fields.",
                          "-- Generated by Tools/Capture/AreaExtract; no raw packet bytes are stored.",
                          "DELETE FROM questwirerewards WHERE QuestId IN (SELECT Id FROM quests WHERE Playfield = 6553);",
                          "DELETE FROM questwireactions WHERE QuestId IN (SELECT Id FROM quests WHERE Playfield = 6553);",
                          "DELETE FROM questwire WHERE QuestId IN (SELECT Id FROM quests WHERE Playfield = 6553);",
                          string.Empty
                      };

        foreach (Quest q in questRows.Values.Where(q => selected.Contains(q.Id)).OrderBy(q => q.Id))
        {
            sql.Add(
                "REPLACE INTO questwire (QuestId, Source, GiverType, GiverInstance, QuestCode, UnknownHash, Quality, TimeLimit, Unknown20, Unknown21, Unknown22, Unknown23Type, Unknown23Instance, Unknown25, Unknown26) VALUES ("
                + JoinSql(
                    q.Id, "'Captured'", q.WireGiverType, q.WireGiverInstance, q.QuestCode, q.UnknownHash,
                    q.Quality, q.TimeLimit, q.Unknown20, q.Unknown21, q.Unknown22, q.Unknown23Type,
                    q.Unknown23Instance, q.Unknown25, q.Unknown26)
                + ");");

            sql.Add(
                "REPLACE INTO questwireactions (QuestId, Ordinal, Version, ActionType, ActionInstance, Unknown1Type, Unknown1Instance, Unknown2Type, Unknown2Instance, Unknown3Type, Unknown3Instance, Unknown4Type, Unknown4Instance, Unknown5, Unknown6, Unknown7, Unknown8, Unknown9Type, Unknown9Instance, Unknown10, Unknown11, Unknown12, Unknown13, Unknown14Type, Unknown14Instance, Deadline, Unknown16, TrackingType, PlayfieldType, PlayfieldInstance, Unknown18, Unknown19, X, Y, Z) VALUES ("
                + JoinSql(
                    q.Id, 0, q.ActionVersion, q.ActionType, q.ActionInstance,
                    q.ActionUnknown1Type, q.ActionUnknown1Instance, q.ActionUnknown2Type, q.ActionUnknown2Instance,
                    q.ActionUnknown3Type, q.ActionUnknown3Instance, q.ActionUnknown4Type, q.ActionUnknown4Instance,
                    FloatSql(q.ActionUnknown5), FloatSql(q.ActionUnknown6), FloatSql(q.ActionUnknown7),
                    FloatSql(q.ActionUnknown8), q.ActionUnknown9Type, q.ActionUnknown9Instance,
                    FloatSql(q.ActionUnknown10), FloatSql(q.ActionUnknown11), FloatSql(q.ActionUnknown12),
                    FloatSql(q.ActionUnknown13), q.ActionUnknown14Type, q.ActionUnknown14Instance,
                    q.ActionDeadline, q.ActionUnknown16, q.ActionTrackingType, q.ActionPlayfieldType,
                    q.ActionPlayfieldInstance, q.ActionUnknown18, q.ActionUnknown19,
                    FloatSql(q.MarkerX), FloatSql(q.MarkerY), FloatSql(q.MarkerZ))
                + ");");

            for (int ordinal = 0; ordinal < q.WireRewards.Count; ordinal++)
            {
                int[] reward = q.WireRewards[ordinal];
                sql.Add(
                    "REPLACE INTO questwirerewards (QuestId, Ordinal, LowId, HighId, Quality, Unknown1) VALUES ("
                    + JoinSql(q.Id, ordinal, reward[0], reward[1], reward[2], reward[3]) + ");");
            }

            sql.Add(string.Empty);
        }

        File.WriteAllLines(Path.Combine(outDir, "questwire.sql"), sql);
    }

    private static string JoinSql(params object[] values)
    {
        return string.Join(
            ", ",
            values.Select(
                value => value == null
                             ? "NULL"
                             : Convert.ToString(value, CultureInfo.InvariantCulture)));
    }

    private static string FloatSql(float value)
    {
        return value.ToString("R", CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Marks the start of a session in the timeline. No character has instance
    /// zero, so it cannot be mistaken for one.
    /// </summary>
    private const int SessionBreak = 0;

    /// <summary>
    /// How far apart two sightings of the same kind of character have to be
    /// before they count as two different places.
    /// </summary>
    private const float ClusterRadius = 12f;

    /// <summary>
    /// Turn every character the capture ever saw into the spawn points that
    /// produced them.
    /// </summary>
    /// <remarks>
    /// This is the difference between a list of sightings and a world.
    ///
    /// A capture of Arete Landing holds four hundred and eighty three distinct
    /// characters, and writing one spawn per character puts four hundred and
    /// eighty three of them in the playfield at once. That is not what the
    /// playfield holds. One hundred and eleven of them are called Malfunctioning
    /// Cleaning Robot and forty six are called Burning Cleaning Robot, and the
    /// Burning ones stand at three coordinates between them - the same robot,
    /// killed and killed again over a fifteen minute session, arriving each time
    /// with a new identity. The client never held more than sixty eight
    /// characters of any kind at one time.
    ///
    /// So the question is not how many were seen but how many were there, and
    /// the capture answers it. Sightings of one kind are grouped by where they
    /// happened - anything within ClusterRadius of another sighting is the same
    /// place - and then the timeline is replayed to find the most that were ever
    /// alive in that place at one moment. That many spawn points, no more.
    ///
    /// It is a floor rather than an exact count, because a client is only told
    /// about what is near it, and two of a kind at opposite ends of the same
    /// place may never have been visible together. It is a floor built from
    /// measurement, which is the right kind of wrong: a playfield slightly too
    /// quiet is a playfield, and four hundred and eighty three characters
    /// standing in a heap is not.
    ///
    /// Which ones survive is chosen for spread. Taking the first few would pile
    /// the survivors wherever the player happened to be looking first, so each
    /// pick is the sighting furthest from everything already picked.
    /// </remarks>
    private static void Thin(Dictionary<int, Npc> npcs, List<int[]> timeline)
    {
        int before = npcs.Count;
        int nowhere = Nowhere(npcs);
        int same = SameSpot(npcs);

        // Sightings by kind. A character with no name is not a kind.
        var byName = new Dictionary<string, List<Npc>>();
        foreach (Npc npc in npcs.Values)
        {
            if (npc.Name.Length == 0)
            {
                continue;
            }

            if (!byName.ContainsKey(npc.Name))
            {
                byName[npc.Name] = new List<Npc>();
            }

            byName[npc.Name].Add(npc);
        }

        // Which place each sighting happened in.
        var place = new Dictionary<int, int>();
        var places = new List<List<Npc>>();
        foreach (var kind in byName)
        {
            foreach (List<Npc> cluster in Cluster(kind.Value))
            {
                foreach (Npc npc in cluster)
                {
                    place[npc.Instance] = places.Count;
                }

                places.Add(cluster);
            }
        }

        // The most that were ever alive in each place at one moment.
        // How many stood in each place at once, every time it changed.
        var counts = new List<int>[places.Count];
        for (int i = 0; i < places.Count; i++)
        {
            counts[i] = new List<int>();
        }

        var live = new int[places.Count];
        foreach (int[] step in timeline)
        {
            // A new session. Whatever was standing about belonged to the last
            // one and is not standing there now, so the running count starts
            // again - but the high-water mark does not, because the answer
            // wanted is the most any single session ever saw at once.
            if (step[0] == SessionBreak)
            {
                for (int i = 0; i < live.Length; i++)
                {
                    live[i] = 0;
                }

                continue;
            }

            int which;
            if (!place.TryGetValue(step[0], out which))
            {
                continue;
            }

            live[which] += step[1];
            counts[which].Add(live[which]);
        }

        var keep = new HashSet<int>();
        for (int i = 0; i < places.Count; i++)
        {
            foreach (Npc npc in Spread(places[i], Math.Max(1, Holds(counts[i]))))
            {
                keep.Add(npc.Instance);
            }
        }

        foreach (int instance in npcs.Keys.ToList())
        {
            Npc npc = npcs[instance];
            if (npc.Name.Length > 0 && !keep.Contains(instance))
            {
                npcs.Remove(instance);
            }
        }

        Console.WriteLine(
            "spawn points\t" + npcs.Count + "\t(from " + before + " sightings in " + places.Count + " places, " + same + " of them one character seen twice, " + nowhere + " with no position)");
    }

    /// <summary>
    /// Group sightings that happened in the same place.
    /// </summary>
    /// <remarks>
    /// Single linkage: a sighting joins a group if it is within ClusterRadius of
    /// anything already in it. A wandering character seen in a dozen spots along
    /// its patrol is one place, which is what we want - it has one spawn point.
    /// </remarks>
    private static List<List<Npc>> Cluster(List<Npc> sightings)
    {
        var groups = new List<List<Npc>>();
        var taken = new bool[sightings.Count];

        for (int i = 0; i < sightings.Count; i++)
        {
            if (taken[i])
            {
                continue;
            }

            var group = new List<Npc> { sightings[i] };
            taken[i] = true;

            for (int scan = 0; scan < group.Count; scan++)
            {
                for (int j = 0; j < sightings.Count; j++)
                {
                    if (taken[j] || Apart(group[scan], sightings[j]) > ClusterRadius)
                    {
                        continue;
                    }

                    taken[j] = true;
                    group.Add(sightings[j]);
                }
            }

            groups.Add(group);
        }

        return groups;
    }

    /// <summary>
    /// Pick a number of sightings from a place, as far from each other as they
    /// can be got.
    /// </summary>
    private static List<Npc> Spread(List<Npc> group, int wanted)
    {
        if (wanted >= group.Count)
        {
            return group;
        }

        var picked = new List<Npc> { group[0] };
        while (picked.Count < wanted)
        {
            Npc best = null;
            float bestDistance = -1f;
            foreach (Npc candidate in group)
            {
                if (picked.Contains(candidate))
                {
                    continue;
                }

                float nearest = float.MaxValue;
                foreach (Npc already in picked)
                {
                    nearest = Math.Min(nearest, Apart(candidate, already));
                }

                if (nearest > bestDistance)
                {
                    bestDistance = nearest;
                    best = candidate;
                }
            }

            if (best == null)
            {
                break;
            }

            picked.Add(best);
        }

        return picked;
    }

    private static float Apart(Npc a, Npc b)
    {
        float dx = a.X - b.X;
        float dz = a.Z - b.Z;
        return (float)Math.Sqrt((dx * dx) + (dz * dz));
    }

    private static void WriteNpcs(
        string outDir,
        Dictionary<int, Npc> npcs,
        Dictionary<int, Weapon> weapons,
        Dictionary<int, Damage> damage,
        Dictionary<string, List<List<float[]>>> paths,
        int writeAs)
    {
        // Damage was seen per character; it belongs to the kind of character.
        // One Cleaning Robot swinging tells us what every Cleaning Robot hits
        // for, and most of them were never in a fight while anyone was watching.
        var damageByName = new Dictionary<string, Damage>();
        foreach (var seen in damage)
        {
            Npc who;
            if (!npcs.TryGetValue(seen.Key, out who) || who.Name.Length == 0)
            {
                continue;
            }

            Damage forName;
            if (!damageByName.TryGetValue(who.Name, out forName))
            {
                forName = new Damage();
                damageByName[who.Name] = forName;
            }

            forName.Min = Math.Min(forName.Min, seen.Value.Min);
            forName.Max = Math.Max(forName.Max, seen.Value.Max);
            forName.Swings += seen.Value.Swings;
        }

        var csv = new List<string>
                  {
                      "Instance,Name,IsNpc,Level,Health,X,Y,Z,HeadingX,HeadingY,HeadingZ,HeadingW,Appearance,Textures"
                  };

        var sql = new List<string>
                  {
                      "-- Arete Landing, extracted from live captures by Tools/Capture/AreaExtract.",
                      "--",
                      "-- Positions are where each character stood the first time the server",
                      "-- described it. For a stationary NPC that is its post; for a wandering",
                      "-- mob it is a sighting, so the spawn spreads out along its patrol",
                      "-- rather than sitting on one point.",
                      "--",
                      "-- Waypoints are the destinations the live server actually sent each",
                      "-- character to, in the order it sent them, read off FollowTarget. A",
                      "-- spawn with two or fewer is left with none, because that is a character",
                      "-- walking on the spot rather than a route.",
                      "--",
                      "-- mindamage and maxdamage are what that kind of character was seen",
                      "-- hitting for, where a capture caught it in a fight. Everything else",
                      "-- gets the table those thirteen fit - see TableMaxDamage - so that no",
                      "-- creature in the playfield is left punching for nothing.",
                      "--",
                      "-- Every value below came off the wire. Nothing is inferred.",
                      string.Empty,
                      "DELETE FROM mobspawnsmeshs WHERE Playfield = " + writeAs + ";",
                      "DELETE FROM mobspawnsweapons WHERE Playfield = " + writeAs + ";",
                      "DELETE FROM mobspawns_stats WHERE Playfield = " + writeAs + ";",
                      "DELETE FROM mobspawns WHERE Playfield = " + writeAs + ";",
                      string.Empty
                  };

        foreach (Npc n in npcs.Values.OrderBy(n => n.Name).ThenBy(n => n.Instance))
        {
            csv.Add(
                string.Join(
                    ",",
                    n.Instance.ToString(),
                    Csv(n.Name),
                    n.IsNpc ? "npc" : "pc",
                    n.Level.ToString(),
                    n.Health.ToString(),
                    Str(n.X),
                    Str(n.Y),
                    Str(n.Z),
                    Str(n.HX),
                    Str(n.HY),
                    Str(n.HZ),
                    Str(n.HW),
                    n.Appearance.ToString(),
                    Csv(string.Join(" ", n.TextureIds))));

            if (!n.IsNpc)
            {
                continue;
            }

            sql.Add(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "INSERT INTO mobspawns (Id, Playfield, X, Y, Z, HeadingX, HeadingY, HeadingZ, HeadingW, Name,"
                    + " Textures0, Textures1, Textures2, Textures3, Textures4, Waypoints)"
                    + " VALUES ({0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}, '{9}', {10}, {11}, {12}, {13}, {14},"
                    + " {15});",
                    n.Instance,
                    writeAs,
                    Str(n.X),
                    Str(n.Y),
                    Str(n.Z),
                    Str(n.HX),
                    Str(n.HY),
                    Str(n.HZ),
                    Str(n.HW),
                    Sql(n.Name),
                    Texture(n, 0),
                    Texture(n, 1),
                    Texture(n, 2),
                    Texture(n, 3),
                    Texture(n, 4),
                    WaypointBlob(paths, n)));

            // The stat ids a spawn needs to look like itself and be fought.
            // Names are from OmniCell.Enums.StatIds.
            AddStat(sql, n, writeAs, 0, n.CharacterFlags);      // flags
            AddStat(sql, n, writeAs, 1, n.Health);              // life
            AddStat(sql, n, writeAs, 4, n.Breed);               // breed
            AddStat(sql, n, writeAs, 27, Math.Max(1, n.Health - n.HealthDamage)); // health, as it was
            AddStat(sql, n, writeAs, 33, n.Side);               // side
            AddStat(sql, n, writeAs, 47, n.Fatness);            // fatness
            AddStat(sql, n, writeAs, 54, n.Level);              // level
            AddStat(sql, n, writeAs, 156, n.RunSpeed);          // runspeed
            AddStat(sql, n, writeAs, 59, n.Gender);             // sex
            AddStat(sql, n, writeAs, 64, n.HeadMesh);           // headmesh
            AddStat(sql, n, writeAs, 89, n.Race);               // race
            AddStat(sql, n, writeAs, 359, n.MonsterData);       // monsterdata
            AddStat(sql, n, writeAs, 360, n.MonsterScale);      // monsterscale
            AddStat(sql, n, writeAs, 455, n.Family);            // npcfamily
            AddStat(sql, n, writeAs, 673, n.VisualFlags);       // visualflags

            // Only a resting pose is kept: sitting (8), sleeping (11), lounging (12). Anything else is
            // how the creature was getting about at the moment it was seen, and the server decides
            // that for itself.
            if (n.MoveMode == 8 || n.MoveMode == 11 || n.MoveMode == 12)
            {
                AddStat(sql, n, writeAs, 173, n.MoveMode);      // currentmovementmode
            }

            // What this kind of character hits for: what it was seen hitting
            // for if it was ever in a fight, and the table otherwise.
            Damage hits;
            if (damageByName.TryGetValue(n.Name, out hits) && hits.Swings > 0)
            {
                AddStat(sql, n, writeAs, 286, hits.Min);        // mindamage
                AddStat(sql, n, writeAs, 285, hits.Max);        // maxdamage
            }
            else
            {
                AddStat(sql, n, writeAs, 286, TableMinDamage(n.Level));
                AddStat(sql, n, writeAs, 285, TableMaxDamage(n.Level));
            }

            // What the character is built out of.
            //
            // Without these a spawned character is drawn from its equipment,
            // and a mob has none, so every one of them went out as a single
            // mesh with an id of zero. The list is on the wire and this is it.
            foreach (AoMesh mesh in n.Meshes)
            {
                sql.Add(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "INSERT INTO mobspawnsmeshs (Id, Playfield, Position, OverrideTextureId, MeshId,"
                        + " Layer) VALUES ({0}, {1}, {2}, {3}, {4}, {5});",
                        n.Instance,
                        writeAs,
                        mesh.Position,
                        mesh.OverrideTextureId,
                        mesh.Id,
                        mesh.Layer));
            }

            foreach (Weapon w in weapons.Values.Where(w => w.Owner == n.Instance))
            {
                sql.Add(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "INSERT INTO mobspawnsweapons (SpawnId, Playfield, WeaponType, WeaponInstance,"
                        + " InventoryId, BodyLocation, ItemFlags, ItemLowId, ItemHighId, QualityLevel,"
                        + " Unknown6, Unknown7, ItemDelay, RechargeDelay, Energy)"
                        + " VALUES ({0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}, {9}, {10}, {11}, {12}, {13}, {14});",
                        n.Instance,
                        writeAs,
                        w.Type,
                        w.Instance,
                        w.InventoryId,
                        w.BodyLocation,
                        w.ItemFlags,
                        w.LowId,
                        w.HighId,
                        w.Quality,
                        w.Unknown6,
                        w.Unknown7,
                        SqlNullable(w.ItemDelay),
                        SqlNullable(w.RechargeDelay),
                        SqlNullable(w.Energy)));
            }

            sql.Add(string.Empty);
        }

        File.WriteAllLines(Path.Combine(outDir, "npcs.csv"), csv);
        // A capture records whatever was standing there, and that includes other
        // players' pets. A pet is not part of the playfield: it belongs to
        // whoever cast it and leaves when they do, and one of them - an Anger
        // Manifestation - took the client down every time anybody clicked it.
        //
        // Told apart by evidence rather than by knowing the game: a summoned
        // creature has a nano crystal named after it, so a spawn whose name
        // appears inside the brackets of an item name was made by a player. The
        // matching is left to the database, which has the item names and this
        // does not.
        sql.Add(string.Empty);
        sql.Add("-- Anything a player summoned is not part of this playfield.");
        sql.Add(
            "CREATE TEMPORARY TABLE captured_player_pets (Id INT NOT NULL, Playfield INT NOT NULL,"
            + " PRIMARY KEY (Id, Playfield));");
        sql.Add(
            "INSERT INTO captured_player_pets (Id, Playfield) SELECT m.Id, m.Playfield FROM mobspawns m"
            + " WHERE m.Playfield = " + writeAs
            + " AND EXISTS (SELECT 1 FROM itemnames n WHERE n.name LIKE CONCAT('%(', m.Name, ')%'));");
        sql.Add(
            "DELETE s FROM mobspawns_stats s JOIN captured_player_pets p"
            + " ON p.Id = s.Id AND p.Playfield = s.Playfield;");
        sql.Add(
            "DELETE w FROM mobspawnsweapons w JOIN captured_player_pets p"
            + " ON p.Id = w.SpawnId AND p.Playfield = w.Playfield;");
        // Aliased, and every column qualified. itemnames has a column called
        // name as well, so an unqualified Name inside the subquery resolves to
        // that one and the test compares a row to itself - which is never true,
        // and deletes nothing, quietly.
        sql.Add(
            "DELETE m FROM mobspawns m JOIN captured_player_pets p"
            + " ON p.Id = m.Id AND p.Playfield = m.Playfield;");
        sql.Add("DROP TEMPORARY TABLE captured_player_pets;");

        File.WriteAllLines(Path.Combine(outDir, "mobspawns.sql"), sql);
    }

    private static int Texture(Npc n, int slot)
    {
        return slot < n.TextureIds.Count ? n.TextureIds[slot] : 0;
    }

    /// <summary>
    /// A spawn's patrol route, as the blob the mobspawns table keeps them in.
    /// </summary>
    /// <remarks>
    /// The zone reads this column with MessagePackZip and starts a character
    /// patrolling once it has more than two of them, so a route of one or two
    /// points is not worth writing - it would be a character walking on the
    /// spot. Those get NULL, which is what a stationary NPC has always had.
    ///
    /// Written as a hex literal because that is how a blob goes into a text file
    /// of SQL and comes back out the same.
    /// </remarks>
    /// <summary>
    /// How long a patrol leg is, so a route that came out wrong says so.
    /// </summary>
    /// <remarks>
    /// A leg is one FollowTarget: the distance the server told a client the
    /// character was about to walk. Legs a metre long mean the wrong record is
    /// being read - a chase, or a position update - rather than a route.
    /// </remarks>
    private static readonly List<double> legLengths = new List<double>();

    private static int routesWritten;

    private static int legsRunning;

    private static string WaypointBlob(Dictionary<string, List<List<float[]>>> paths, Npc n)
    {
        // The longest unbroken piece, from whichever session saw the most of the
        // patrol. Joining two pieces would put a leg across the playfield
        // between the end of one and the start of the next.
        List<float[]> route = null;
        string tail = "\u0000" + n.Instance;
        foreach (var entry in paths)
        {
            if (!entry.Key.EndsWith(tail, StringComparison.Ordinal))
            {
                continue;
            }

            foreach (List<float[]> piece in entry.Value)
            {
                if (route == null || piece.Count > route.Count)
                {
                    route = piece;
                }
            }
        }

        if (route == null || route.Count <= 2)
        {
            return "NULL";
        }


        routesWritten++;
        for (int i = 1; i < route.Count; i++)
        {
            float legX = route[i][0] - route[i - 1][0];
            float legZ = route[i][2] - route[i - 1][2];
            legLengths.Add(Math.Sqrt((legX * legX) + (legZ * legZ)));
            if (route[i][3] != 0f)
            {
                legsRunning++;
            }
        }

        var waypoints = new List<MobSpawnWaypoint>();
        foreach (float[] point in route)
        {
            waypoints.Add(
                new MobSpawnWaypoint
                    {
                        Identity = n.Instance,
                        Playfield = n.Playfield,
                        X = point[0],
                        Y = point[1],
                        Z = point[2],
                        WalkMode = (int)point[3]
                    });
        }

        byte[] blob = MessagePackZip.SerializeData<MobSpawnWaypoint>(waypoints);
        var hex = new StringBuilder("0x");
        foreach (byte b in blob)
        {
            hex.Append(b.ToString("x2"));
        }

        return hex.ToString();
    }

    /// <summary>
    /// Damage by level, for creatures no capture ever saw in a fight.
    /// </summary>
    /// <remarks>
    /// Thirteen kinds of creature were seen swinging across the captures, from
    /// a level 1 Cleaning Robot to a level 40 ICC Peacekeeper. Their damage
    /// against their level is close to a straight line through the origin, which
    /// is what you would expect of a game where a level 20 monster hits about
    /// twice as hard as a level 10 one:
    ///
    ///     level  min  max          level  min  max
    ///         1    4    5              6    5   19
    ///         1    6   17              6    5   18
    ///         2    7    9              7   17   18
    ///         3   17   31             15    3   23
    ///         4   10   13             40   83  172
    ///         5    2 .. 6   8 .. 19
    ///
    /// A least squares fit through the origin over those thirteen gives 3.873
    /// per level for the maximum and 1.808 for the minimum. It sits inside the
    /// observed spread at level 5 (9 to 19, against 2 to 11, 2 to 8 and 6 to 19),
    /// at level 6 (11 to 23, against 5 to 19 and 5 to 18) and at level 40 (72 to
    /// 155, against 83 to 172). It reads low at level 1, which a line through
    /// the origin has to; both level 1 creatures were seen fighting, so the
    /// table is not what they use.
    ///
    /// Kept as thousandths so the arithmetic stays in integers and the numbers
    /// above are readable as themselves.
    /// </remarks>
    private const int MaxDamagePerLevel = 3873;

    // The merged Arete captures saw the same transient Gas Fire fixtures many
    // times under replacement identities. These are the four captured fixture
    // identities retained by the emulator representation; emitting every
    // snapshot produces twenty-two overlapping fires in one playfield.
    private static readonly HashSet<int> AreteGasFireInstances = new HashSet<int>
        { 1477021836, 1477283083, 1477283084, 1477283092 };

    /// <summary>
    /// The fixtures of a playfield as staticdynels rows.
    /// </summary>
    /// <remarks>
    /// A statel that anything happens to - a terminal, a door, a cargo box -
    /// is a static dynel to the server, and the playfield builds one for every
    /// row it finds. The stats column is the SimpleItemFullUpdate stat list
    /// exactly as it came off the wire, packed the way LoadStaticDynels reads
    /// it back, because that list is what the client is sent again.
    ///
    /// A fixture whose acgitemtemplateid is missing or zero is skipped, not
    /// because it is uninteresting but because the loader has nothing to build
    /// it from: it looks the item up by that id and there is no fallback.
    /// </remarks>
    private static void WriteStaticDynels(string outDir, Dictionary<string, Static> statics, int playfield)
    {
        var sql = new List<string>
                  {
                      "-- The fixtures of playfield " + playfield + ", read out of the",
                      "-- SimpleItemFullUpdates the live server sent.",
                      "--",
                      "-- Position, heading and the whole stat list, as sent. The stats are",
                      "-- packed the way the playfield loader reads them back.",
                      string.Empty,
                      "DELETE FROM staticdynels WHERE Playfield = " + playfield + ";",
                      string.Empty
                  };

        int written = 0;
        int noTemplate = 0;
        int repeatedAreteFires = 0;
        foreach (Static s in statics.Values.OrderBy(s => s.Type).ThenBy(s => s.Instance))
        {
            if (s.Npc != 0)
            {
                // Somebody's stock, not a thing standing in the world. It is
                // placed by WriteVendors, against the character who carries it.
                continue;
            }

            if (!s.WireStats.Any(p => p[0] == StatAcgItemTemplateId && p[1] != 0))
            {
                noTemplate++;
                continue;
            }

            int templateId = s.WireStats.First(p => p[0] == StatAcgItemTemplateId)[1];
            if (playfield == 6553 && templateId == 295883 && !AreteGasFireInstances.Contains(s.Instance))
            {
                repeatedAreteFires++;
                continue;
            }

            var packed = new List<GameTuple<CharacterStat, uint>>();
            foreach (int[] pair in s.WireStats)
            {
                packed.Add(
                    new GameTuple<CharacterStat, uint>
                        {
                            Value1 = (CharacterStat)pair[0],
                            Value2 = unchecked((uint)pair[1])
                        });
            }

            var hex = new StringBuilder("0x");
            foreach (byte b in MessagePackZip.SerializeData<GameTuple<CharacterStat, uint>>(packed))
            {
                hex.Append(b.ToString("x2"));
            }

            sql.Add(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "INSERT INTO staticdynels (Type, Instance, Playfield, X, Y, Z, HeadingX, HeadingY,"
                    + " HeadingZ, HeadingW, stats, customevents)"
                    + " VALUES ({0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}, {9}, {10}, NULL);",
                    s.Type,
                    s.Instance,
                    playfield,
                    Str(s.X),
                    Str(s.Y),
                    Str(s.Z),
                    Str(s.HX),
                    Str(s.HY),
                    Str(s.HZ),
                    Str(s.HW),
                    hex));
            written++;
        }

        File.WriteAllLines(Path.Combine(outDir, "staticdynels.sql"), sql);
        Console.WriteLine(
            "fixtures  " + written + " placed" + (noTemplate == 0 ? string.Empty
                : ", " + noTemplate + " with no item template to build from")
            + (repeatedAreteFires == 0 ? string.Empty
                : ", " + repeatedAreteFires + " repeated Arete Gas Fire snapshots removed"));
    }

    /// <summary>
    /// See MaxDamagePerLevel.
    /// </summary>
    private const int MinDamagePerLevel = 1808;

    private static int TableMaxDamage(int level)
    {
        return Math.Max(3, ((Math.Max(1, level) * MaxDamagePerLevel) + 500) / 1000);
    }

    private static int TableMinDamage(int level)
    {
        return Math.Max(1, ((Math.Max(1, level) * MinDamagePerLevel) + 500) / 1000);
    }

    private static void AddStat(List<string> sql, Npc n, int playfield, int stat, int value)
    {
        sql.Add(
            string.Format(
                CultureInfo.InvariantCulture,
                "INSERT INTO mobspawns_stats (Id, Playfield, Stat, Value) VALUES ({0}, {1}, {2}, {3});",
                n.Instance,
                playfield,
                stat,
                value));
    }

    private static void WriteStatics(string outDir, Dictionary<string, Static> statics, int writeAs)
    {
        var csv = new List<string> { "Type,Instance,Kind,X,Y,Z,HeadingX,HeadingY,HeadingZ,HeadingW,Stats" };

        foreach (Static s in statics.Values.OrderBy(s => s.Type).ThenBy(s => s.Instance))
        {
            csv.Add(
                string.Join(
                    ",",
                    s.Type.ToString(),
                    s.Instance.ToString(),
                    s.Kind,
                    Str(s.X),
                    Str(s.Y),
                    Str(s.Z),
                    Str(s.HX),
                    Str(s.HY),
                    Str(s.HZ),
                    Str(s.HW),
                    Csv(string.Join(" ", s.Stats))));
        }

        File.WriteAllLines(Path.Combine(outDir, "statics.csv"), csv);
    }

    /// <summary>
    /// The spawned character a quest is given by, or null when it is given by
    /// something that is not one.
    /// </summary>
    /// <remarks>
    /// Two forms turn up in the captures. Most quests carry a CanbeAffected
    /// identity - 50000, the same type a spawned mob is given - whose instance
    /// is the character. Fourteen carry a type of zero and an instance three
    /// bytes wide: 0x573703 against Rex Larsson's 0x7A573703, 0x573701 against
    /// Alex Gibbs's 0x7A573701, 0x573705 against Vernon Godfray's. The top byte
    /// is missing and nothing else about them differs.
    ///
    /// A short form is only accepted when exactly one spawned character matches
    /// it. Nineteen pairs of characters in Arete Landing share their low three
    /// bytes, so this has to be checked rather than assumed - and where it is
    /// ambiguous the quest is left with no giver and flagged, which is
    /// recoverable, instead of being hung on the wrong NPC, which is not.
    /// </remarks>
    private static Npc Giver(Quest quest, Dictionary<int, Npc> npcs)
    {
        // Who the player was talking to when the quest arrived, first.
        //
        // QuestGiver, below, is the same value for a whole chain: five distinct
        // values across sixty two captured quests, which put all twenty four of
        // Rex Larsson's chain in his own conversation window. It reads like the
        // chain's owner rather than who handed each errand over. The
        // conversation that was open when a quest turned up is per quest and is
        // the character the player was actually standing in front of.
        // Who the player was talking to when the quest arrived. Nothing else.
        //
        // QuestGiver was the fallback and it is not a giver: it is one value
        // for a whole chain, so every quest that arrived with no conversation
        // open landed on Rex Larsson and he ended up with nine. An unknown
        // giver is a quest left out, which is visible, rather than a quest on
        // the wrong character, which is not.
        if (quest.TalkedTo != 0)
        {
            return Shopkeeper(quest.TalkedTo, npcs);
        }

        // No capture caught this one being handed over - it was already in the
        // log the first time a session showed it. Then whoever the player was
        // last standing in front of, which is often right because a session
        // opens where the last one left off.
        Npc nearby = quest.NearTalkedTo == 0 ? null : Shopkeeper(quest.NearTalkedTo, npcs);
        if (nearby != null)
        {
            return nearby;
        }

        // And last, QuestGiver, which is the weakest of the three: it holds the
        // chain's owner rather than the giver, so a whole branch of the newbie
        // chain reads as one character's. Never used over a conversation.
        return Shopkeeper(quest.GiverId, npcs);
    }

    /// <summary>
    /// Writes the quests as SQL, with objectives where they can be read.
    /// </summary>
    /// <remarks>
    /// The quest itself - id, name, full text, giver, icon and rewards - is
    /// structured data off the wire and is written as it stands.
    ///
    /// Objectives are not. QuestFullUpdate carries them inside QuestActions,
    /// whose fields are still mostly unread, so the only machine readable
    /// statement of what a quest wants is the "Mission Objective" line inside
    /// its description, written for a person to read. Two forms of it are
    /// unambiguous enough to take:
    ///
    ///   Kill N Somethings.
    ///   Talk to Someone.
    ///
    /// A kill objective is only written when the name it parses to matches a
    /// character the same captures actually spawned. That check earns its keep
    /// immediately: the first quest in Arete Landing asks for five
    /// "Malfunctining Cleaning Robots", which is a typo in Funcom's own quest
    /// text - the mob is a "Malfunctioning Cleaning Robot" - and a parser that
    /// trusted the sentence would write an objective that could never be
    /// completed. Those come out commented, with the nearest real name beside
    /// them, for a person to decide.
    /// </remarks>
    private static void WriteQuestSql(
        string outDir,
        Dictionary<int, Quest> quests,
        Dictionary<int, Npc> npcs,
        Dictionary<string, Static> statics,
        Dictionary<string, List<int>> runs,
        float arriveX,
        float arriveZ,
        int playfield)
    {
        var names = new HashSet<string>(
            npcs.Values.Where(n => n.IsNpc).Select(n => n.Name),
            StringComparer.OrdinalIgnoreCase);

        int resolved = 0;
        int unresolved = 0;

        var sql = new List<string>
                  {
                      "-- Quests, extracted from live captures by Tools/Capture/AreaExtract.",
                      "--",
                      "-- Quest text/rewards and QuestAction marker fields are structured data",
                      "-- off the wire. Objective targets are resolved against captured actors,",
                      "-- fixtures and item references; see Objectives. Requires is OmniCell's",
                      "-- documented reachability representation, not a captured quest field.",
                      "--",
                      "-- Anything commented out below did not parse into something that could",
                      "-- be completed, and wants a person to look at it.",
                      string.Empty,
                      "DELETE FROM questobjectives WHERE QuestId IN"
                      + " (SELECT Id FROM quests WHERE Playfield = " + playfield + ");",
                      "DELETE FROM quests WHERE Playfield = " + playfield + ";",
                      string.Empty
                  };

        // One quest per quest, not one per session it was seen in.
        //
        // A quest identity is handed out per player: three characters walking
        // the same newbie chain produce three "Buy a Lockpick" rows with three
        // different ids, and loading all of them lets a player take the same
        // quest three times. The same name and the same description is the same
        // quest, and the earliest id is kept for it.
        var families = quests.Values.GroupBy(q => q.Name + "\u0000" + q.Description).ToList();
        List<Quest> distinct = families
            .Select(g => g.OrderBy(q => q.Id).First())
            .OrderBy(q => q.Id)
            .ToList();

        // The same merge has to be applied to what the sessions watched, or
        // most of the order is thrown away with the duplicates.
        //
        // A run is the list of ids one session saw arrive. Four sessions out of
        // five followed a different character's copy of the chain, so every id
        // in their runs points at a quest that was merged out - and every
        // ordering they recorded goes out with it. One session's run survived,
        // which is why two stretches of the chain had nothing linking them and
        // the middle ran before the opening.
        var sameQuest = new Dictionary<int, int>();
        foreach (var family in families)
        {
            int keep = family.OrderBy(q => q.Id).First().Id;
            foreach (Quest member in family)
            {
                sameQuest[member.Id] = keep;
            }
        }

        foreach (List<int> run in runs.Values)
        {
            for (int at = 0; at < run.Count; at++)
            {
                int keep;
                if (sameQuest.TryGetValue(run[at], out keep))
                {
                    run[at] = keep;
                }
            }
        }

        sql.Add(
            "-- " + distinct.Count + " quests, from " + quests.Count
            + " seen; the rest were the same quest handed to another character.");
        sql.Add(string.Empty);

        // A quest whose objective is somewhere else is not this playfield's.
        // The captures are of a player's whole quest log, so a capture taken in
        // Arete Landing holds the Leet-killing errands of the area after it, and
        // writing those in as Arete's makes them look like Arete quests that do
        // not work.
        int elsewhere = distinct.RemoveAll(
            q => q.HasAction && q.MarkerPlayfield != 0 && q.MarkerPlayfield != playfield);
        if (elsewhere > 0)
        {
            sql.Add("-- " + elsewhere + " more belong to other playfields and are left out.");
            sql.Add(string.Empty);
        }

        // Chain each giver's quests in the order the player was given them.
        //
        // A giver holds a chain, not a menu. Which order is not on any quest,
        // but the captures went through the chain once and the order the
        // quests turned up in the log is the order they were handed over.
        //
        // Inferred, not proven, and worth being clear which. Nothing on the
        // wire says a giver cannot offer two errands at once; what the captures
        // show is one player walking Arete Landing once and taking them one
        // after another. A giver who really does offer two would be forced into
        // a false order by this, and the symptom would be a quest that never
        // appears because the one it was given after was never taken.
        Quest previous = null;
        foreach (int id in Stitch(runs, distinct, npcs, arriveX, arriveZ))
        {
            Quest q = distinct.FirstOrDefault(x => x.Id == id);
            if (q == null || Giver(q, npcs) == null)
            {
                continue;
            }

            if (previous != null)
            {
                q.Requires = previous.Id;
            }

            previous = q;
        }

        foreach (Quest q in distinct)
        {
            // A quest nobody here gives out is not this playfield's quest.
            //
            // The captures are of a player's whole quest log, so a session in
            // Arete Landing carries the errands of the areas after it: killing
            // Leets, reporting to the Alien Agency. Their givers are characters
            // who stand somewhere else. Written in as Arete's they are quests
            // that can never be started, which is indistinguishable from a
            // broken one.
            Npc giver = Giver(q, npcs);
            if (giver == null)
            {
                unresolved++;
                sql.Add(
                    "-- " + q.Id + " \"" + q.Name + "\" is given by " + q.GiverType + ":" + q.GiverId
                    + ", who is not here, so it is left out.");
                sql.Add(string.Empty);
                continue;
            }

            resolved++;
            sql.Add(
                "-- " + q.Id + " is associated with " + giver.Name + " ("
                + (q.TalkedTo != 0
                       ? "capture-backed: the log gained it while talking to them"
                       : q.NearTalkedTo != 0
                           ? "OmniCell fallback: last talked to when it was first seen; not a proven giver"
                           : "QuestGiver chain association; not a proven giver") + ").");

            sql.Add(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "REPLACE INTO quests (Id, Name, Description, GiverId, IconId, CashReward,"
                    + " ExperienceReward, Playfield, Requires)"
                    + " VALUES ({0}, '{1}', '{2}', {3}, {4}, {5}, {6}, {7}, {8});",
                    q.Id,
                    Sql(q.Name),
                    Sql(q.Description),
                    giver.Instance,
                    q.IconId,
                    q.CashReward,
                    q.ExperienceReward,
                    playfield,
                    q.Requires));

            foreach (string line in Objectives(q, npcs, statics, playfield))
            {
                sql.Add(line);
            }

            sql.Add(string.Empty);
        }

        File.WriteAllLines(Path.Combine(outDir, "quests.sql"), sql);
        Console.WriteLine(
            "quests    " + resolved + " written, given by somebody standing here; " + unresolved
            + " left out for having no giver here, " + elsewhere + " for belonging to another playfield"
            + " (of " + quests.Count + " seen)");
    }

    /// <summary>
    /// A quest's objective, read off the action the quest carries.
    /// </summary>
    /// <remarks>
    /// This used to be read out of the English in the description, which got
    /// ten of forty six. It is on the wire: every captured quest carries one
    /// QuestAction, and the action holds the objective's world marker, its
    /// kind, and - where the thing to be done is a fixture - that fixture's
    /// identity.
    ///
    /// The marker is what makes it resolvable. "Talk to Stan Goodman" carries
    /// 3462, 880, and Stanley Goodman stands at 3463, 880; "Open the Cargo Box"
    /// carries 3621, 782 and the Cargo Box is at 3622, 780. So the objective's
    /// target is whatever is standing on its marker, which is a fact about the
    /// world rather than a reading of a sentence.
    ///
    /// The action's own version is the kind. The values across the captures:
    ///
    ///    6   deliver something to somebody. Action holds what.
    ///    8   use a fixture. Action is its identity, type 51005.
    ///   18   a tradeskill step. Action holds a four character code.
    ///   20   kill a number of something.
    ///   24   go to somebody or something and deal with them.
    ///
    /// Anything the marker cannot be resolved against is written commented out
    /// and flagged, never guessed at.
    /// </remarks>
    private static IEnumerable<string> Objectives(
        Quest quest,
        Dictionary<int, Npc> npcs,
        Dictionary<string, Static> statics,
        int playfield)
    {
        var lines = new List<string>();
        if (!quest.HasAction)
        {
            lines.Add("-- NEEDS REVIEW " + quest.Id + ": carries no action, so there is nothing to finish.");
            lines.Add(string.Empty);
            return lines;
        }

        // A fixture the action names outright beats anything found by position.
        Static fixture = null;
        if (quest.TargetInstance != 0)
        {
            foreach (Static candidate in statics.Values)
            {
                if (candidate.Instance == quest.TargetInstance)
                {
                    fixture = candidate;
                    break;
                }
            }
        }

        // Otherwise, whatever is standing on the marker - character or fixture,
        // whichever is nearer. Nearest of one kind and then the other is not the
        // same question: the Cargo Box's marker has a Cleaning Robot 2.8 units
        // from it and the box itself 1.4, and asking about characters first
        // hangs the quest on the robot.
        // A marker of 0,0 is not a place. It is what a quest carries when its
        // object is somewhere this playfield cannot say, and matching on it
        // finds whatever else has no position - the shopkeepers' stock, which
        // sits at the origin for the same reason.
        Npc who = null;
        if (fixture == null && (quest.MarkerX != 0f || quest.MarkerZ != 0f))
        {
            double best = OnTheMarker;
            foreach (Npc npc in npcs.Values)
            {
                if (!npc.IsNpc || npc.Name.Length == 0)
                {
                    continue;
                }

                double away = Away(npc.X, npc.Z, quest);
                if (away < best)
                {
                    best = away;
                    who = npc;
                }
            }

            foreach (Static candidate in statics.Values)
            {
                if (candidate.Npc != 0)
                {
                    continue;
                }

                double away = Away(candidate.X, candidate.Z, quest);
                if (away < best)
                {
                    best = away;
                    fixture = candidate;
                    who = null;
                }
            }
        }

        // A marker is its target's own position with the fractions dropped, so
        // a hit is within a unit or so of it. When nothing is that close the
        // marker is stale - it points at where a character was standing when
        // the capture was taken, and that character patrols - and the name is
        // the better source. Every quest in the chain names who it is about.
        string named = null;
        if (who == null && fixture == null)
        {
            who = Named(quest, npcs, out named);
        }

        // Before falling back to a place: some quests do not want you to go
        // anywhere, they want you to end up holding something or wearing it,
        // and they say which in as many words.
        if (who == null && fixture == null)
        {
            string built = Match(quest.Description, @"to create an? (.+?)\s*\.");
            string worn = Match(quest.Description, @"Equip the (.+?) after ");

            if (worn.Length > 0)
            {
                lines.Add("-- " + quest.Id + ": wear " + worn + ".");
                lines.Add(Objective(quest.Id, 0, ObjectiveEquip, worn, 1));
                lines.Add(string.Empty);
                return lines;
            }

            if (built.Length > 0)
            {
                lines.Add("-- " + quest.Id + ": build " + built + ".");
                lines.Add(Objective(quest.Id, 0, ObjectiveCollect, built, 1));
                lines.Add(string.Empty);
                return lines;
            }
        }

        if (who == null && fixture == null)
        {
            // Nothing to talk to, nothing to use, and no name that means
            // anybody. What is left is the marker itself, which is a real fact
            // off the wire: the quest is about something at this spot.
            //
            // Some quest targets can be absent from the captured world state.
            // A captured target must be resolved earlier and must never fall
            // through to this marker-only representation. Going to the exact
            // marker carried by the quest is the most that can be represented
            // here without inventing a world object.
            if (quest.MarkerPlayfield == playfield && (quest.MarkerX != 0f || quest.MarkerZ != 0f))
            {
                string spot = quest.MarkerX.ToString("0.##", CultureInfo.InvariantCulture) + ","
                              + quest.MarkerZ.ToString("0.##", CultureInfo.InvariantCulture);
                lines.Add(
                    "-- " + quest.Id + ": nothing captured stands at " + spot
                    + ", so the objective is to be there.");
                lines.Add(Objective(quest.Id, 0, ObjectiveReach, spot, 1));
                lines.Add(string.Empty);
                return lines;
            }

            lines.Add(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "-- NEEDS REVIEW {0}: nothing stands on its marker at {1:0},{2:0}, and its name",
                    quest.Id,
                    quest.MarkerX,
                    quest.MarkerZ));
            lines.Add("--   \"" + quest.Name + "\" names nobody who is spawned here either.");
            lines.Add(string.Empty);
            return lines;
        }

        int required = quest.Needs > 0 ? quest.Needs : Required(quest.Description);
        if (who != null)
        {
            lines.Add(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "-- {0}: {1}, {2}.",
                    quest.Id,
                    who.Name,
                    named != null
                        ? "by " + named + " - its marker at "
                          + quest.MarkerX.ToString("0", CultureInfo.InvariantCulture) + ","
                          + quest.MarkerZ.ToString("0", CultureInfo.InvariantCulture) + " has nobody on it"
                        : "standing " + Away(who.X, who.Z, quest).ToString("0.0", CultureInfo.InvariantCulture)
                          + " units from its marker"));
            lines.Add(
                Objective(
                    quest.Id,
                    0,
                    quest.Kind == KillAction ? ObjectiveKill : ObjectiveTalkTo,
                    who.Name,
                    quest.Kind == KillAction ? required : 1));
        }
        else
        {
            lines.Add(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "-- {0}: fixture {1}, template {2}, standing {3:0.0} units from its marker.",
                    quest.Id,
                    fixture.Instance,
                    Template(fixture),
                    Away(fixture.X, fixture.Z, quest)));
            lines.Add(
                Objective(
                    quest.Id,
                    0,
                    ObjectiveUse,
                    fixture.Instance.ToString(CultureInfo.InvariantCulture),
                    1));
        }

        lines.Add(string.Empty);
        return lines;
    }

    /// <summary>
    /// The character a quest is about, taken from what the quest says.
    /// </summary>
    /// <remarks>
    /// Second choice, used when the marker points at nothing. A marker is a
    /// position recorded once, and a character who patrols has walked away from
    /// it - Marcus Stone's marker misses him by seven units.
    ///
    /// The label and the objective line are searched, because the label the
    /// server sends is cut at thirty characters - "Take the contents of the
    /// Str..." loses the rest of its own sentence - and the objective line
    /// carries it in full. The rest of the description is not searched: it is
    /// story, and it names everybody in the chain.
    ///
    /// Full names first. Failing that a given name, and only when exactly one
    /// spawned character has it - "Report to Alex" is Alex Gibbs because he is
    /// the only Alex in the playfield, and if there were two it would be a
    /// guess and is left alone.
    /// </remarks>
    private static Npc Named(Quest quest, Dictionary<int, Npc> npcs, out string how)
    {
        how = null;
        // The label and the objective line, not the whole description. A
        // description is a paragraph of story and names everybody in the chain:
        // "Extinguish the Gas Fire" mentions Marcus Stone in its second
        // sentence and matching on that hangs the quest on him instead of on
        // the fire.
        // The label, the untruncated title, and the objective line.
        //
        // All three because each is missing something. The label is cut at
        // thirty characters. The objective line is written by hand and is
        // sometimes wrong - "Kill 5 Malfunctining Cleaning Robots" is Funcom's
        // typo, and matching only on that gives the quest the wrong robot. The
        // description's first line is the title again, in full and spelled
        // right.
        string says = quest.Name + " " + FirstLine(quest.Description) + " "
                      + MissionObjective(quest.Description);

        // A delivery is always to somebody, and the label is usually cut before
        // it says who - "Deliver DNA-Locked Armor to ..." - so for those the
        // whole description is fair game. For anything else it is not: a
        // description names everybody in the chain, and "Extinguish the Gas
        // Fire" mentions Marcus Stone in its second sentence.
        if (quest.Kind == DeliverAction)
        {
            says += " " + quest.Description;
        }

        Npc best = null;
        foreach (Npc npc in npcs.Values)
        {
            if (!npc.IsNpc || npc.Name.Length < 4
                || says.IndexOf(npc.Name, StringComparison.OrdinalIgnoreCase) < 0)
            {
                continue;
            }

            if (best == null || npc.Name.Length > best.Name.Length)
            {
                best = npc;
            }
        }

        if (best != null)
        {
            how = "its full name";
            return best;
        }

        var given = new Dictionary<string, List<Npc>>(StringComparer.OrdinalIgnoreCase);
        foreach (Npc npc in npcs.Values)
        {
            if (!npc.IsNpc)
            {
                continue;
            }

            // The first word only. Any word would make "Merchant" a name and
            // hang "Take the contents of the Strongbox" on the Furniture
            // Merchant, who is the only one of those in the playfield.
            string part = npc.Name.Split(' ')[0];
            if (part.Length >= 4)
            {
                if (!given.ContainsKey(part))
                {
                    given[part] = new List<Npc>();
                }

                if (!given[part].Contains(npc))
                {
                    given[part].Add(npc);
                }
            }
        }

        foreach (var entry in given)
        {
            if (entry.Value.Count != 1
                || !Regex.IsMatch(says, "\\b" + Regex.Escape(entry.Key) + "\\b", RegexOptions.IgnoreCase))
            {
                continue;
            }

            if (best == null || entry.Key.Length > best.Name.Length)
            {
                best = entry.Value[0];
                how = "the only " + entry.Key + " in the playfield";
            }
        }

        return best;
    }

    /// <summary>
    /// The first group of a pattern, or empty.
    /// </summary>
    /// <remarks>
    /// The tradeskill quests name what they want made and what they want worn,
    /// in a fixed form written by whoever wrote the quest: "to create a Leg
    /// Implant: Agility, Shiny." and "Equip the Leg Implant: Agility, Shiny
    /// after activating the Stationary Automated Surgery Clinic." Those two
    /// sentences are the only place the item is named - the quest's action
    /// carries a four-character code for it and nothing that resolves to a
    /// name - so this is where the objective has to come from.
    /// </remarks>
    private static string Match(string text, string pattern)
    {
        Match found = Regex.Match(text ?? string.Empty, pattern, RegexOptions.IgnoreCase);
        return found.Success ? Regex.Replace(found.Groups[1].Value, "<[^>]*>", string.Empty).Trim() : string.Empty;
    }

    /// <summary>
    /// A quest's title, from the top of its description.
    /// </summary>
    /// <remarks>
    /// The same words as the label the server sends beside it, except that the
    /// label is cut at thirty characters and this is not.
    /// </remarks>
    private static string FirstLine(string description)
    {
        string text = description ?? string.Empty;
        int end = text.IndexOf("<BR>", StringComparison.OrdinalIgnoreCase);
        return end < 0 ? text : text.Substring(0, end);
    }

    private static double Away(float x, float z, Quest quest)
    {
        double dx = x - quest.MarkerX;
        double dz = z - quest.MarkerZ;
        return Math.Sqrt((dx * dx) + (dz * dz));
    }

    /// <summary>
    /// The spawned character standing on a quest's marker, or null.
    /// </summary>
    /// <remarks>
    /// Three units. The markers that hit a character are within one of it - a
    /// marker is the character's own position rounded to whole units - and the
    /// nearest thing to any marker that misses is much further off than three.
    /// </remarks>
    private static Npc Standing(Quest quest, Dictionary<int, Npc> npcs)
    {
        Npc best = null;
        double nearest = 3.0;
        foreach (Npc npc in npcs.Values)
        {
            if (!npc.IsNpc || npc.Name.Length == 0)
            {
                continue;
            }

            double dx = npc.X - quest.MarkerX;
            double dz = npc.Z - quest.MarkerZ;
            double away = Math.Sqrt((dx * dx) + (dz * dz));
            if (away < nearest)
            {
                nearest = away;
                best = npc;
            }
        }

        return best;
    }

    private static Static StandingFixture(Quest quest, Dictionary<string, Static> statics)
    {
        Static best = null;
        double nearest = 3.0;
        foreach (Static candidate in statics.Values)
        {
            if (candidate.Npc != 0)
            {
                // Carried stock, not a thing standing anywhere.
                continue;
            }

            double dx = candidate.X - quest.MarkerX;
            double dz = candidate.Z - quest.MarkerZ;
            double away = Math.Sqrt((dx * dx) + (dz * dz));
            if (away < nearest)
            {
                nearest = away;
                best = candidate;
            }
        }

        return best;
    }

    /// <summary>
    /// How many, read out of the sentence.
    /// </summary>
    /// <remarks>
    /// Second choice. The count is on the wire - QuestInfo carries it in the
    /// field this still calls Unknown21, which is 5 on "Terminate 5
    /// Malfunctioning Cleaning Robots" and 5 on "Alien Invasion" and zero on
    /// every quest that is not a number of anything - so the sentence is only
    /// read when the record does not say.
    ///
    /// Which is just as well, because the sentence is not reliable: the
    /// objective line of that same quest reads "Kill 5 Malfunctining Cleaning
    /// Robots", and a quest that mentions any other number first would be
    /// counted wrong.
    /// </remarks>
    private static int Required(string description)
    {
        Match digits = Regex.Match(description, @"\b(\d+)\s+\w", RegexOptions.None);
        if (digits.Success)
        {
            int count;
            if (int.TryParse(digits.Groups[1].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out count)
                && count > 0 && count < 100)
            {
                return count;
            }
        }

        string[] words = { "one", "two", "three", "four", "five", "six", "seven", "eight", "nine", "ten" };
        for (int i = 0; i < words.Length; i++)
        {
            if (Regex.IsMatch(description, @"\b" + words[i] + @"\b", RegexOptions.IgnoreCase))
            {
                return i + 1;
            }
        }

        return 1;
    }

    /// <summary>
    /// The action versions the captures carry, by what they ask for.
    /// </summary>
    /// <summary>
    /// How far from its marker the thing an objective is about may stand.
    /// </summary>
    /// <remarks>
    /// A marker is the target's own position with the fractions dropped, so a
    /// hit is normally within a unit. It is wider than that because a marker
    /// put on a character who has since walked - the position we recorded is
    /// one sighting of a patrol - lands a few units out. Every resolution and
    /// its distance is written into the file, so a wrong one is visible rather
    /// than silent.
    /// </remarks>
    private const double OnTheMarker = 2.0;

    private const int KillAction = 20;

    private const int DeliverAction = 6;

    private const int ObjectiveKill = 0;

    private const int ObjectiveTalkTo = 3;

    private const int ObjectiveUse = 4;

    private const int ObjectiveCollect = 1;

    private const int ObjectiveReach = 5;

    private const int ObjectiveEquip = 6;

    private static string Objective(int questId, int ordinal, int type, string target, int required)
    {
        return string.Format(
            CultureInfo.InvariantCulture,
            "REPLACE INTO questobjectives (QuestId, Ordinal, ObjectiveType, Target, Required)"
            + " VALUES ({0}, {1}, {2}, '{3}', {4});",
            questId,
            ordinal,
            type,
            Sql(target),
            required);
    }

    /// <summary>
    /// Pulls the Mission Objective line out of a quest description.
    /// </summary>
    private static string MissionObjective(string description)
    {
        Match m = Regex.Match(
            description ?? string.Empty,
            @"Mission Objective:\s*(?:<BR>)?\s*(.*?)(?:</font>|<BR>|<br>|$)",
            RegexOptions.IgnoreCase | RegexOptions.Singleline);

        return m.Success ? Regex.Replace(m.Groups[1].Value, "<[^>]+>", string.Empty).Trim() : string.Empty;
    }

    /// <summary>
    /// Turns "Cleaning Robots" into "Cleaning Robot".
    /// </summary>
    /// <remarks>
    /// Quest text counts things and so names them in the plural; spawns are
    /// named in the singular. Only a trailing s is removed, because anything
    /// cleverer would start inventing names, and a name that does not match is
    /// caught and reported rather than used.
    /// </remarks>
    private static string Singular(string name)
    {
        name = name.Trim().Trim('"');
        return name.EndsWith("s", StringComparison.OrdinalIgnoreCase) ? name.Substring(0, name.Length - 1) : name;
    }

    /// <summary>
    /// Names the closest spawned character, to save the next person a search.
    /// </summary>
    private static string Nearest(string target, HashSet<string> names)
    {
        string best = null;
        int bestScore = int.MaxValue;

        foreach (string name in names)
        {
            int score = Distance(target.ToLowerInvariant(), name.ToLowerInvariant());
            if (score < bestScore)
            {
                bestScore = score;
                best = name;
            }
        }

        return best == null || bestScore > target.Length / 2
                   ? string.Empty
                   : " Nearest spawned name: \"" + best + "\".";
    }

    private static int Distance(string a, string b)
    {
        var d = new int[a.Length + 1, b.Length + 1];
        for (int i = 0; i <= a.Length; i++)
        {
            d[i, 0] = i;
        }

        for (int j = 0; j <= b.Length; j++)
        {
            d[0, j] = j;
        }

        for (int i = 1; i <= a.Length; i++)
        {
            for (int j = 1; j <= b.Length; j++)
            {
                int cost = a[i - 1] == b[j - 1] ? 0 : 1;
                d[i, j] = Math.Min(Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1), d[i - 1, j - 1] + cost);
            }
        }

        return d[a.Length, b.Length];
    }

    private static void WriteQuests(string outDir, Dictionary<int, string> quests)
    {
        var md = new List<string> { "# Arete Landing quests", string.Empty };
        md.AddRange(quests.Values);
        File.WriteAllLines(Path.Combine(outDir, "quests.md"), md);
    }

    /// <summary>
    /// Throw away a sighting that never said where it was.
    /// </summary>
    /// <remarks>
    /// One Malfunctioning Cleaning Robot came through with x set and y and z
    /// holding 0 and 6.59E-39 - a denormal, which is a bit pattern rather than
    /// a coordinate. Nothing sent that; the position was never filled in.
    ///
    /// A playfield is laid out hundreds of units from the origin - Arete
    /// Landing runs x 3400-3650, z 550-950 - so a y and z both inside one unit
    /// of zero is an absent position and not a place. Kept as a spawn point it
    /// would have put a robot under the world.
    /// </remarks>
    private static int Nowhere(Dictionary<int, Npc> npcs)
    {
        var drop = new List<int>();
        foreach (var entry in npcs)
        {
            Npc npc = entry.Value;
            if (npc.Name.Length > 0 && Math.Abs(npc.Y) < 1f && Math.Abs(npc.Z) < 1f)
            {
                drop.Add(entry.Key);
            }
        }

        foreach (int key in drop)
        {
            npcs.Remove(key);
        }

        return drop.Count;
    }

    /// <summary>
    /// Drop sightings that stood in exactly the same place as another of the
    /// same name, and say how many went.
    /// </summary>
    /// <remarks>
    /// Thin asks how many of a kind were alive at once and keeps that many,
    /// which is the right question and gets the wandering ones right - six
    /// Waste Collectors within a couple of units of each other really are six,
    /// at six different coordinates.
    ///
    /// It gets the still ones wrong, because the timeline is built across every
    /// capture at once, so a vendor who was there in two sessions looks like two
    /// vendors alive at the same moment. The give-away is that they are not near
    /// each other, they are on top of each other: Barry the Food Vendor twice at
    /// 3516.731, 826.9835, to four decimal places, with two runtime identities
    /// from two different days. Nothing places two characters at one point.
    ///
    /// Ninety nine rows of Arete Landing were that - two Barrys, two Clan
    /// Bartenders, three Omni-AF Privates - and each would have spawned inside
    /// itself. An exact match is the whole test; anything looser starts eating
    /// the Waste Collectors.
    /// </remarks>
    private static int SameSpot(Dictionary<int, Npc> npcs)
    {
        var seen = new HashSet<string>();
        var drop = new List<int>();
        foreach (var entry in npcs)
        {
            Npc npc = entry.Value;
            if (npc.Name.Length == 0)
            {
                continue;
            }

            string where = npc.Name
                + "|" + npc.X.ToString("R", CultureInfo.InvariantCulture)
                + "|" + npc.Y.ToString("R", CultureInfo.InvariantCulture)
                + "|" + npc.Z.ToString("R", CultureInfo.InvariantCulture);
            if (!seen.Add(where))
            {
                drop.Add(entry.Key);
            }
        }

        foreach (int key in drop)
        {
            npcs.Remove(key);
        }

        return drop.Count;
    }

    /// <summary>
    /// The list a session records the quests it watched arrive in.
    /// </summary>
    private static List<int> Run(Dictionary<string, List<int>> runs, string session)
    {
        List<int> run;
        if (!runs.TryGetValue(session, out run))
        {
            run = new List<int>();
            runs[session] = run;
        }

        return run;
    }

    /// <summary>
    /// One order for the whole area's quests, stitched out of what each session
    /// watched happen.
    /// </summary>
    /// <remarks>
    /// The chain runs across characters, not within them. Rex Larsson has you
    /// kill his robots and open his cargo box and then sends you to Marcus
    /// Stone, who sends you to Flint Novak, who sends you to Alex Gibbs.
    /// Chaining each giver's own quests separately made every one of them the
    /// start of something, which is not how any of it is played.
    ///
    /// No single capture holds the whole run - one starts at Rex and reaches
    /// the surveillance drone, another picks up at planting the bug - so the
    /// order is stitched out of several. Inside a session the order the log
    /// grew is the order the quests were handed over, and that is trusted
    /// completely. Between sessions nothing is assumed beyond what their
    /// overlap forces. That makes it a topological sort over "a came before b
    /// in some session", and where no session ever ordered two quests the
    /// earlier sighting goes first - a tie-break, not a claim.
    /// </remarks>
    private static List<int> Stitch(
        Dictionary<string, List<int>> runs,
        List<Quest> quests,
        Dictionary<int, Npc> npcs,
        float arriveX,
        float arriveZ)
    {
        var after = new Dictionary<int, List<int>>();
        var waiting = new Dictionary<int, int>();
        foreach (Quest q in quests)
        {
            after[q.Id] = new List<int>();
            waiting[q.Id] = 0;
        }

        foreach (List<int> run in runs.Values)
        {
            for (int i = 1; i < run.Count; i++)
            {
                int before = run[i - 1];
                int then = run[i];
                if (!after.ContainsKey(before) || !after.ContainsKey(then)
                    || after[before].Contains(then))
                {
                    continue;
                }

                after[before].Add(then);
            }
        }

        // Two quests offered at the same time get taken in whichever order the
        // player felt like, and different sessions then disagree about which
        // came first. That is not a contradiction about the chain - it is the
        // chain not having an opinion - so both edges come out and the pair is
        // left unordered.
        //
        // One pair in Arete Landing: Stanley Goodman hands over "Talk to Sarah
        // Greene" and "Buy some Nano Programs" together, and two captures took
        // them each way round. Left in, that single loop stops the sort dead
        // and the twenty quests behind it come out in no order at all.
        foreach (int before in after.Keys.ToList())
        {
            foreach (int then in after[before].ToList())
            {
                if (!after.ContainsKey(then) || !after[then].Contains(before))
                {
                    continue;
                }

                after[before].Remove(then);
                after[then].Remove(before);
                Console.WriteLine(
                    "          the captures put \"" + quests.First(q => q.Id == before).Name
                    + "\" and \"" + quests.First(q => q.Id == then).Name
                    + "\" both ways round; left unordered");
            }
        }

        foreach (int before in after.Keys)
        {
            foreach (int then in after[before])
            {
                waiting[then]++;
            }
        }

        var order = new List<int>();
        var ready = quests.Where(q => waiting[q.Id] == 0).Select(q => q.Id).ToList();
        while (ready.Count > 0)
        {
            // Of the quests now available, the one whose giver stands
            // nearest where a player arrives in the playfield.
            //
            // This only decides between quests that no session ever put in
            // order - stretches of the chain that were captured separately and
            // never overlap. Which of those comes first is not on the wire, and
            // capture dates do not say either: this area's opening was sniffed
            // two days after its middle. What does say is the ground. A chain
            // starts where the player is put down, and Arete Landing puts them
            // on Rex Larsson's platform.
            int next = ready
                .OrderBy(id => Distance(quests.First(q => q.Id == id), npcs, arriveX, arriveZ))
                .ThenBy(id => quests.First(q => q.Id == id).Seen)
                .First();
            ready.Remove(next);
            order.Add(next);

            foreach (int then in after[next])
            {
                if (--waiting[then] == 0)
                {
                    ready.Add(then);
                }
            }
        }

        // Left over means a loop longer than a pair, which no capture of this
        // area produces. Nothing is dropped for it - it goes on the end and
        // says so, loudly, because a quest ordered by nothing is a quest whose
        // place in the chain is a guess.
        foreach (Quest q in quests.OrderBy(q => q.Seen))
        {
            if (!order.Contains(q.Id))
            {
                Console.WriteLine("          \"" + q.Name + "\" is ordered by nothing; put last");
                order.Add(q.Id);
            }
        }

        return order;
    }

    /// <summary>
    /// How far a quest's giver stands from where players arrive.
    /// </summary>
    private static double Distance(Quest quest, Dictionary<int, Npc> npcs, float x, float z)
    {
        Npc giver = Giver(quest, npcs);
        if (giver == null)
        {
            return double.MaxValue;
        }

        double dx = giver.X - x;
        double dz = giver.Z - z;
        return Math.Sqrt((dx * dx) + (dz * dz));
    }

    /// <summary>
    /// Where a player arriving in the playfield is put down.
    /// </summary>
    /// <remarks>
    /// The middle of the playfield's first destination line, four units back
    /// from it, which is what the game itself does when somebody walks through
    /// a grid exit. Zero when the playfield file has no destination, and then
    /// nothing is ordered by it.
    /// </remarks>
    private static void Arrival(string statelFile, int playfield, out float x, out float z)
    {
        x = 0f;
        z = 0f;
        if (statelFile == null)
        {
            return;
        }

        foreach (PlayfieldData data in OmniCellContentPack.ReadPlayfields(statelFile))
        {
            if (data.PlayfieldId != playfield)
            {
                continue;
            }

            foreach (PlayfieldDestination line in data.Destinations.Values)
            {
                double spread = Math.Sqrt(
                    ((line.EndX - line.StartX) * (line.EndX - line.StartX))
                    + ((line.EndZ - line.StartZ) * (line.EndZ - line.StartZ)));
                if (spread <= 0)
                {
                    continue;
                }

                x = (float)((((line.EndX - line.StartX) * 0.5) + line.StartX)
                            - (((line.EndZ - line.StartZ) / spread) * 4.0));
                z = (float)((((line.EndZ - line.StartZ) * 0.5) + line.StartZ)
                            + (((line.EndX - line.StartX) / spread) * 4.0));
                return;
            }

            return;
        }
    }

    /// <summary>
    /// Reads the answer indices from the client half of one zone connection.
    /// </summary>
    private static Dictionary<int, List<int>> ReadSelectedAnswers(
        byte[] raw,
        MethodInfo deserialize,
        object serializer)
    {
        var selected = new Dictionary<int, List<int>>();
        byte[] data = raw;

        // Client traffic is normally framed plaintext. Keep the same guarded
        // compression detection as the server reader so a capture from a build
        // that compresses both directions fails into decoded evidence rather
        // than silently producing no selections.
        int rawAlignment = DetectAlignment(raw);
        bool rawFrames = CountFramable(raw, 3, rawAlignment) >= 1;
        for (int start = 0; !rawFrames && start + 1 < Math.Min(raw.Length, 4096); start++)
        {
            if (!IsZlibHeader(raw, start))
            {
                continue;
            }

            byte[] candidate = InflateAll(raw, start);
            if (candidate.Length > 0 && CountFramable(candidate, 3, 1) >= 1)
            {
                data = candidate;
                break;
            }
        }

        int alignment = DetectAlignment(data);
        int pos = 0;
        while (pos + HeaderLength <= data.Length)
        {
            short size = BigEndianInt16(data, pos + SizeOffset);
            if (size < HeaderLength || pos + size > data.Length)
            {
                pos++;
                continue;
            }

            var packet = new byte[size];
            Array.Copy(data, pos, packet, 0, size);
            pos += size + Padding(size, alignment);

            object body;
            try
            {
                using (var stream = new MemoryStream(packet))
                {
                    object message = deserialize.Invoke(serializer, new object[] { stream });
                    body = message == null ? null : Get(message, "Body");
                }
            }
            catch (Exception)
            {
                continue;
            }

            if (body == null || body.GetType().Name != "KnuBotAnswerMessage")
            {
                continue;
            }

            int who = Int(Get(Get(body, "Target"), "Instance"));
            int answer = Int(Get(body, "Answer"));
            List<int> answers;
            if (!selected.TryGetValue(who, out answers))
            {
                answers = new List<int>();
                selected[who] = answers;
            }

            answers.Add(answer);
        }

        return selected;
    }

    /// <summary>
    /// Pairs client selections with the server's answer lists in their captured
    /// order. No selection is manufactured for an unanswered or partial step.
    /// </summary>
    private static int AttachSelectedAnswers(
        Dictionary<string, Dictionary<int, List<Step>>> scripts,
        string session,
        Dictionary<int, List<int>> selected)
    {
        int attached = 0;
        Dictionary<int, List<Step>> byTalker;
        if (!scripts.TryGetValue(session, out byTalker))
        {
            return 0;
        }

        foreach (KeyValuePair<int, List<int>> talker in selected)
        {
            List<Step> steps;
            if (!byTalker.TryGetValue(talker.Key, out steps))
            {
                continue;
            }

            List<Step> answerSteps = steps.Where(s => s.Answers.Count > 0).ToList();
            int count = Math.Min(answerSteps.Count, talker.Value.Count);
            for (int i = 0; i < count; i++)
            {
                int answer = talker.Value[i];
                if (answer >= 0 && answer < answerSteps[i].Answers.Count)
                {
                    answerSteps[i].SelectedAnswer = answer;
                    attached++;
                }
            }
        }

        return attached;
    }

    /// <summary>
    /// The step a character is part way through saying.
    /// </summary>
    private static Step Saying(
        Dictionary<string, Dictionary<int, List<Step>>> scripts,
        string session,
        int who)
    {
        Dictionary<int, List<Step>> here;
        if (!scripts.TryGetValue(session, out here))
        {
            here = new Dictionary<int, List<Step>>();
            scripts[session] = here;
        }

        List<Step> steps;
        if (!here.TryGetValue(who, out steps))
        {
            steps = new List<Step> { new Step() };
            here[who] = steps;
        }

        return steps[steps.Count - 1];
    }

    /// <summary>
    /// Records that a quest was handed over at the step just finished.
    /// </summary>
    /// <remarks>
    /// The step just finished, not the one being built: a quest arrives after
    /// the answer that asked for it, so by the time the log shows it the next
    /// step is already open.
    /// </remarks>
    private static void Handed(
        Dictionary<string, Dictionary<int, List<Step>>> scripts,
        string session,
        int who,
        int quest)
    {
        Dictionary<int, List<Step>> here;
        List<Step> steps;
        if (who == 0 || !scripts.TryGetValue(session, out here) || !here.TryGetValue(who, out steps))
        {
            return;
        }

        for (int i = steps.Count - 1; i >= 0; i--)
        {
            if (steps[i].Answers.Count > 0)
            {
                steps[i].Grants = quest;
                return;
            }
        }
    }

    /// <summary>
    /// What each character actually says, as the captures heard it.
    /// </summary>
    /// <remarks>
    /// Funcom's words. Every line here was sent by the live server to a client
    /// standing in front of that character - none of it is written by us, which
    /// is the whole point: the four lines of our own that stood in for Rex
    /// Larsson's conversation were the first thing anybody noticed.
    ///
    /// One walk per character, the longest one heard. A conversation is a tree
    /// and a session only ever walks one path through it, so the longest walk
    /// is the most of it anybody saw. Options nobody clicked are not in any
    /// capture and cannot be here.
    /// </remarks>
    private static void WriteScripts(
        string outDir,
        Dictionary<string, Dictionary<int, List<Step>>> scripts,
        Dictionary<int, Npc> npcs,
        Dictionary<int, Quest> quests,
        int playfield)
    {
        // The longest walk heard of each character.
        var best = new Dictionary<int, List<Step>>();
        foreach (var session in scripts)
        {
            foreach (var talker in session.Value)
            {
                // Keyed by the character, not by the identity the session saw:
                // a live server hands out a fresh one each time, so the same
                // person turns up under several numbers and ends up with
                // several conversations.
                Npc talking = Shopkeeper(talker.Key, npcs);
                if (talking == null)
                {
                    continue;
                }

                List<Step> steps = talker.Value.Where(s => s.Says.Count > 0 || s.Answers.Count > 0).ToList();

                // A step with nothing to answer cannot be played: the window
                // would show the text and offer no way out of it. It is not a
                // step anyway - it is whatever the character was still saying
                // when the capture's player closed the window, so the answers
                // that would have followed were never sent.
                while (steps.Count > 0 && steps[steps.Count - 1].Answers.Count == 0)
                {
                    steps.RemoveAt(steps.Count - 1);
                }
                List<Step> have;
                if (!best.TryGetValue(talking.Instance, out have) || Better(steps, have))
                {
                    best[talking.Instance] = steps;
                }
            }
        }

        // A quest id is per player and the quest table keeps the earliest of
        // each, so the ids the conversations recorded have to be moved the same
        // way or they name rows that are not there.
        var sameQuest = new Dictionary<int, int>();
        foreach (var family in quests.Values.GroupBy(q => q.Name + "\u0000" + q.Description))
        {
            int keep = family.OrderBy(q => q.Id).First().Id;
            foreach (Quest member in family)
            {
                sameQuest[member.Id] = keep;
            }
        }

        HashSet<string> players = PlayerNames(npcs);

        var sql = new List<string>
                  {
                      "-- What the characters of playfield " + playfield + " say, as the live",
                      "-- server said it. Funcom's words; nothing here is written by us.",
                      "-- {name} stands where the live server put the recording player's own",
                      "-- name; the server fills it in with whoever is talking.",
                      "--",
                      "-- One walk per character - the longest one any capture heard. A",
                      "-- conversation is a tree and a session walks one path through it, so an",
                      "-- option nobody clicked is in no capture and cannot be here.",
                      string.Empty,
                      "DELETE FROM knubotscript WHERE Playfield = " + playfield + ";",
                      string.Empty
                  };

        int written = 0;
        int lines = 0;
        int handovers = 0;
        foreach (var talker in best.OrderBy(x => x.Key))
        {
            Npc who = Shopkeeper(talker.Key, npcs);
            if (who == null || talker.Value.Count == 0)
            {
                continue;
            }

            written++;
            sql.Add("-- " + who.Name);
            for (int step = 0; step < talker.Value.Count; step++)
            {
                Step s = talker.Value[step];
                int grants;
                if (s.Grants == 0 || !sameQuest.TryGetValue(s.Grants, out grants))
                {
                    grants = 0;
                }

                foreach (string says in s.Says)
                {
                    sql.Add(Line(who.Instance, playfield, step, lines++, 0, Personal(says, players), grants));
                }

                bool capturedSelection = grants != 0
                                         && s.SelectedAnswer >= 0
                                         && s.SelectedAnswer < s.Answers.Count;
                for (int answerIndex = 0; answerIndex < s.Answers.Count; answerIndex++)
                {
                    sql.Add(Line(who.Instance, playfield, step, lines++, 1, Personal(s.Answers[answerIndex], players), grants));
                }

                if (grants != 0)
                {
                    handovers++;
                }

                if (capturedSelection)
                {
                    sql.Add(
                        "-- CAPTURED CLIENT SELECTION: answer " + s.SelectedAnswer + " (\""
                        + Personal(s.Answers[s.SelectedAnswer], players).Replace("\"", "\\\"") + "\") was selected before quest "
                        + grants + " appeared. This records sequence only; it does not infer whether the"
                        + " answer accepted that quest, handed in the preceding quest, or caused another transition.");
                }
            }

            sql.Add(string.Empty);
        }

        File.WriteAllLines(Path.Combine(outDir, "knubotscript.sql"), sql);
        Console.WriteLine(
            "dialogue  " + written + " characters have a conversation, " + lines + " lines, "
            + handovers + " of them hand a quest over");
    }

    /// <summary>
    /// The names of every player character the captures saw - anything that
    /// is not an NPC.
    /// </summary>
    private static HashSet<string> PlayerNames(Dictionary<int, Npc> npcs)
    {
        return new HashSet<string>(
            npcs.Values.Where(n => !n.IsNpc && !string.IsNullOrEmpty(n.Name)).Select(n => n.Name),
            StringComparer.Ordinal);
    }

    /// <summary>
    /// A line with any player's name in it replaced by {name}. The live server
    /// writes the listening player's name into some lines, and a script that
    /// kept it would greet every player on OmniCell by the name of whoever
    /// made the recording.
    /// </summary>
    private static string Personal(string text, HashSet<string> players)
    {
        foreach (string player in players)
        {
            text = System.Text.RegularExpressions.Regex.Replace(
                text,
                @"\b" + System.Text.RegularExpressions.Regex.Escape(player) + @"\b",
                "{name}");
        }

        return text;
    }

    private static string Line(int npc, int playfield, int step, int ordinal, int kind, string text, int grants)
    {
        return string.Format(
            CultureInfo.InvariantCulture,
            "INSERT INTO knubotscript (Npc, Playfield, Step, Ordinal, Kind, Text, Grants)"
            + " VALUES ({0}, {1}, {2}, {3}, {4}, '{5}', {6});",
            npc,
            playfield,
            step,
            ordinal,
            kind,
            Sql(text),
            grants);
    }

    /// <summary>
    /// Whether one walk through a conversation is worth more than another.
    /// </summary>
    /// <remarks>
    /// A walk that saw a quest change hands beats one that did not, however
    /// long it is: the step a quest is handed over at is the only thing in here
    /// that cannot be recovered from the text, and a longer walk that never saw
    /// it is a conversation that leads nowhere.
    ///
    /// Between two of the same kind, the longer one - it is more of the tree.
    /// </remarks>
    private static bool Better(List<Step> candidate, List<Step> against)
    {
        bool hands = candidate.Any(s => s.Grants != 0);
        bool held = against.Any(s => s.Grants != 0);
        return hands != held ? hands : candidate.Count > against.Count;
    }

    /// <summary>
    /// How many characters a place holds, from how many stood in it at once.
    /// </summary>
    /// <remarks>
    /// Not the largest number ever seen there, which is what this used to take
    /// and which is the least trustworthy figure available.
    ///
    /// The count only knows a character has gone when the server says so, and
    /// a missed despawn is permanent for the rest of the session while a
    /// correct one costs nothing - so every error the count can make pushes it
    /// up and none pushes it down, and all of them collect in the top of the
    /// range. The platform of Malfunctioning Cleaning Robots outside Rex
    /// Larsson's office is the clearest case: across 1558 moments the number
    /// standing there was between one and nine for most of them, reached
    /// eleven in a tenth, and touched twenty four exactly once.
    ///
    /// So the ninetieth percentile: what the place holds in all but the
    /// busiest tenth of the time it was watched. Eleven robots, against a
    /// maximum of twenty four and a remembered figure of about ten.
    /// </remarks>
    private static int Holds(List<int> counts)
    {
        if (counts.Count == 0)
        {
            return 0;
        }

        List<int> sorted = counts.OrderBy(n => n).ToList();
        return sorted[(int)(sorted.Count * 0.9)];
    }
}
