namespace OmniCell.Core.Content
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.Compression;
    using System.Linq;

    using MsgPack;

    using OmniCell.Core.Actions;
    using OmniCell.Core.Events;
    using OmniCell.Core.Functions;
    using OmniCell.Core.Items;
    using OmniCell.Core.Nanos;
    using OmniCell.Core.Playfields;
    using OmniCell.Core.Requirements;
    using OmniCell.Core.Statels;
    using OmniCell.Enums;

    using SmokeLounge.AOtomation.Messaging.GameData;

    /// <summary>
    /// Reads and writes OmniCell's canonical, versioned content representation.
    /// The format is deliberately independent of the AO resource database and of
    /// the legacy extractor's MessagePack object cache.
    /// </summary>
    /// <remarks>
    /// Version 2 adds, after each item and nano, the rest of its client record
    /// (<see cref="RecordData"/>), and after each function its header int32s, raw
    /// requirement triples and raw argument bytes (<see cref="FunctionRecordData"/>).
    /// Version 3 adds functions stored directly in the record body. Older packs
    /// still load; version 1 Record fields are null and version 2 bare-function
    /// lists are empty.
    /// </remarks>
    public static class OmniCellContentPack
    {
        private const string Magic = "OMNICELL-CONTENT";
        private const int FormatVersion = 3;
        private const int OldestReadableVersion = 1;

        private enum ContentKind : byte
        {
            Items = 1,
            Nanos = 2,
            Playfields = 3
        }

        public static void WriteItems(string filename, IEnumerable<ItemTemplate> source)
        {
            List<ItemTemplate> records = source.OrderBy(x => x.ID).ToList();
            WritePack(filename, ContentKind.Items, records.Count, writer =>
            {
                foreach (ItemTemplate item in records)
                {
                    writer.Write(item.ID);
                    writer.Write(item.Flags);
                    writer.Write(item.ItemType);
                    writer.Write(item.MultipleCount);
                    writer.Write(item.Nothing);
                    writer.Write(item.Quality);
                    WriteDictionary(writer, item.Attack);
                    WriteDictionary(writer, item.Defend);
                    WriteDictionary(writer, item.Stats);
                    WriteIntList(writer, item.Relations);
                    WriteActions(writer, item.Actions);
                    WriteEvents(writer, item.Events);
                    WriteRecordData(writer, item.Record);
                }
            });
        }

        public static List<ItemTemplate> ReadItems(string filename)
        {
            return ReadPack(filename, ContentKind.Items, (reader, count, version) =>
            {
                var result = new List<ItemTemplate>(count);
                for (int i = 0; i < count; i++)
                {
                    var item = new ItemTemplate
                    {
                        ID = reader.ReadInt32(),
                        Flags = reader.ReadInt32(),
                        ItemType = reader.ReadInt32(),
                        MultipleCount = reader.ReadInt32(),
                        Nothing = reader.ReadInt32(),
                        Quality = reader.ReadInt32(),
                        Attack = ReadDictionary(reader),
                        Defend = ReadDictionary(reader),
                        Stats = ReadDictionary(reader),
                        Relations = ReadIntList(reader),
                        Actions = ReadActions(reader),
                        Events = ReadEvents(reader, version)
                    };
                    if (version >= 2) item.Record = ReadRecordData(reader, version);
                    result.Add(item);
                }
                return result;
            });
        }

        public static void WriteNanos(string filename, IEnumerable<NanoFormula> source)
        {
            List<NanoFormula> records = source.OrderBy(x => x.ID).ToList();
            WritePack(filename, ContentKind.Nanos, records.Count, writer =>
            {
                foreach (NanoFormula nano in records)
                {
                    writer.Write(nano.ID);
                    writer.Write(nano.Instance);
                    writer.Write(nano.ItemType);
                    writer.Write(nano.Type);
                    writer.Write(nano.flags);
                    WriteDictionary(writer, nano.Attack);
                    WriteDictionary(writer, nano.Defend);
                    WriteDictionary(writer, nano.Stats);
                    WriteActions(writer, nano.Actions);
                    WriteEvents(writer, nano.Events);
                    WriteRecordData(writer, nano.Record);
                }
            });
        }

        public static List<NanoFormula> ReadNanos(string filename)
        {
            return ReadPack(filename, ContentKind.Nanos, (reader, count, version) =>
            {
                var result = new List<NanoFormula>(count);
                for (int i = 0; i < count; i++)
                {
                    var nano = new NanoFormula
                    {
                        ID = reader.ReadInt32(),
                        Instance = reader.ReadInt32(),
                        ItemType = reader.ReadInt32(),
                        Type = reader.ReadInt32(),
                        flags = reader.ReadInt32(),
                        Attack = ReadDictionary(reader),
                        Defend = ReadDictionary(reader),
                        Stats = ReadDictionary(reader),
                        Actions = ReadActions(reader),
                        Events = ReadEvents(reader, version)
                    };
                    if (version >= 2) nano.Record = ReadRecordData(reader, version);
                    result.Add(nano);
                }
                return result;
            });
        }

        public static void WritePlayfields(string filename, IEnumerable<PlayfieldData> source)
        {
            List<PlayfieldData> records = source.OrderBy(x => x.PlayfieldId).ToList();
            WritePack(filename, ContentKind.Playfields, records.Count, writer =>
            {
                foreach (PlayfieldData playfield in records)
                {
                    writer.Write(playfield.PlayfieldId);
                    writer.Write(playfield.Name ?? string.Empty);

                    writer.Write(playfield.Destinations.Count);
                    foreach (KeyValuePair<byte, PlayfieldDestination> pair in playfield.Destinations.OrderBy(x => x.Key))
                    {
                        writer.Write(pair.Key);
                        PlayfieldDestination destination = pair.Value;
                        writer.Write(destination.DestinationId);
                        writer.Write(destination.StartX);
                        writer.Write(destination.StartY);
                        writer.Write(destination.StartZ);
                        writer.Write(destination.EndX);
                        writer.Write(destination.EndY);
                        writer.Write(destination.EndZ);
                    }

                    writer.Write(playfield.Doors1.Count);
                    foreach (Door door in playfield.Doors1)
                    {
                        writer.Write(door.Flags);
                        writer.Write(door.Id);
                        writer.Write(door.Index);
                        writer.Write(door.Index2);
                        writer.Write(door.PlayfieldDesignator);
                        writer.Write(door.PlayfieldId);
                        writer.Write(door.X);
                        writer.Write(door.Y);
                        writer.Write(door.Z);
                        writer.Write(door.unknown1);
                        writer.Write(door.unknown2);
                        writer.Write(door.unknown3);
                        writer.Write(door.unknown4);
                        writer.Write(door.unknown5);
                    }

                    writer.Write(playfield.Walls.Count);
                    foreach (PlayfieldWalls wallSet in playfield.Walls)
                    {
                        writer.Write(wallSet.Walls.Count);
                        foreach (PlayfieldWall wall in wallSet.Walls)
                        {
                            writer.Write(wall.DestinationIndex);
                            writer.Write(wall.DestinationPlayfield);
                            writer.Write(wall.Flags);
                            writer.Write(wall.X);
                            writer.Write(wall.Y);
                            writer.Write(wall.Z);
                        }
                    }

                    writer.Write(playfield.Statels.Count);
                    foreach (StatelData statel in playfield.Statels)
                    {
                        WriteIdentity(writer, statel.Identity);
                        WriteIdentity(writer, statel.Parent);
                        writer.Write(statel.PlayfieldId);
                        writer.Write(statel.TemplateId);
                        writer.Write(statel.X);
                        writer.Write(statel.Y);
                        writer.Write(statel.Z);
                        writer.Write(statel.HeadingW);
                        writer.Write(statel.HeadingX);
                        writer.Write(statel.HeadingY);
                        writer.Write(statel.HeadingZ);
                        WriteEvents(writer, statel.Events);
                    }
                }
            });
        }

        public static List<PlayfieldData> ReadPlayfields(string filename)
        {
            return ReadPack(filename, ContentKind.Playfields, (reader, count, version) =>
            {
                var result = new List<PlayfieldData>(count);
                for (int i = 0; i < count; i++)
                {
                    var playfield = new PlayfieldData { PlayfieldId = reader.ReadInt32(), Name = reader.ReadString() };

                    int destinationCount = ReadCount(reader, "destinations");
                    for (int j = 0; j < destinationCount; j++)
                    {
                        byte key = reader.ReadByte();
                        playfield.Destinations.Add(key, new PlayfieldDestination
                        {
                            DestinationId = reader.ReadInt32(),
                            StartX = reader.ReadSingle(), StartY = reader.ReadSingle(), StartZ = reader.ReadSingle(),
                            EndX = reader.ReadSingle(), EndY = reader.ReadSingle(), EndZ = reader.ReadSingle()
                        });
                    }

                    int doorCount = ReadCount(reader, "doors");
                    for (int j = 0; j < doorCount; j++)
                    {
                        playfield.Doors1.Add(new Door
                        {
                            Flags = reader.ReadInt32(), Id = reader.ReadInt32(), Index = reader.ReadInt16(),
                            Index2 = reader.ReadInt32(), PlayfieldDesignator = reader.ReadInt16(),
                            PlayfieldId = reader.ReadInt32(), X = reader.ReadSingle(), Y = reader.ReadSingle(),
                            Z = reader.ReadSingle(), unknown1 = reader.ReadInt32(), unknown2 = reader.ReadByte(),
                            unknown3 = reader.ReadInt32(), unknown4 = reader.ReadInt16(), unknown5 = reader.ReadInt32()
                        });
                    }

                    int wallSetCount = ReadCount(reader, "wall sets");
                    for (int j = 0; j < wallSetCount; j++)
                    {
                        var wallSet = new PlayfieldWalls();
                        int wallCount = ReadCount(reader, "walls");
                        for (int k = 0; k < wallCount; k++)
                        {
                            wallSet.Walls.Add(new PlayfieldWall
                            {
                                DestinationIndex = reader.ReadByte(), DestinationPlayfield = reader.ReadInt16(),
                                Flags = reader.ReadByte(), X = reader.ReadSingle(), Y = reader.ReadSingle(), Z = reader.ReadSingle()
                            });
                        }
                        playfield.Walls.Add(wallSet);
                    }

                    int statelCount = ReadCount(reader, "statels");
                    for (int j = 0; j < statelCount; j++)
                    {
                        var statel = new StatelData
                        {
                            Identity = ReadIdentity(reader), Parent = ReadIdentity(reader),
                            PlayfieldId = reader.ReadInt32(), TemplateId = reader.ReadInt32(),
                            X = reader.ReadSingle(), Y = reader.ReadSingle(), Z = reader.ReadSingle(),
                            HeadingW = reader.ReadSingle(), HeadingX = reader.ReadSingle(),
                            HeadingY = reader.ReadSingle(), HeadingZ = reader.ReadSingle(),
                            Events = ReadEvents(reader, version)
                        };
                        playfield.Statels.Add(statel);
                    }
                    result.Add(playfield);
                }
                return result;
            });
        }

        private static void WritePack(string filename, ContentKind kind, int count, Action<BinaryWriter> writeRecords)
        {
            string fullPath = Path.GetFullPath(filename);
            string directory = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
            using (var file = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None))
            using (var gzip = new GZipStream(file, CompressionLevel.Optimal))
            using (var writer = new BinaryWriter(gzip))
            {
                writer.Write(Magic);
                writer.Write(FormatVersion);
                writer.Write((byte)kind);
                writer.Write(count);
                writeRecords(writer);
            }
        }

        private static T ReadPack<T>(string filename, ContentKind expectedKind, Func<BinaryReader, int, int, T> readRecords)
        {
            using (var file = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (var gzip = new GZipStream(file, CompressionMode.Decompress))
            using (var reader = new BinaryReader(gzip))
            {
                if (reader.ReadString() != Magic) throw new InvalidDataException("Not an OmniCell content pack: " + filename);
                int version = reader.ReadInt32();
                if (version < OldestReadableVersion || version > FormatVersion) throw new InvalidDataException("Unsupported OmniCell content format version " + version);
                ContentKind actualKind = (ContentKind)reader.ReadByte();
                if (actualKind != expectedKind) throw new InvalidDataException("Unexpected content kind in " + filename);
                return readRecords(reader, ReadCount(reader, "records"), version);
            }
        }

        private static void WriteDictionary(BinaryWriter writer, IDictionary<int, int> values)
        {
            writer.Write(values == null ? 0 : values.Count);
            if (values == null) return;
            foreach (KeyValuePair<int, int> pair in values.OrderBy(x => x.Key)) { writer.Write(pair.Key); writer.Write(pair.Value); }
        }

        private static Dictionary<int, int> ReadDictionary(BinaryReader reader)
        {
            int count = ReadCount(reader, "dictionary values");
            var result = new Dictionary<int, int>(count);
            for (int i = 0; i < count; i++) result.Add(reader.ReadInt32(), reader.ReadInt32());
            return result;
        }

        private static void WriteIntList(BinaryWriter writer, IList<int> values)
        {
            writer.Write(values == null ? 0 : values.Count);
            if (values != null) foreach (int value in values) writer.Write(value);
        }

        private static List<int> ReadIntList(BinaryReader reader)
        {
            int count = ReadCount(reader, "integer values");
            var result = new List<int>(count);
            for (int i = 0; i < count; i++) result.Add(reader.ReadInt32());
            return result;
        }

        private static void WriteBytes(BinaryWriter writer, byte[] values)
        {
            writer.Write(values == null ? 0 : values.Length);
            if (values != null) writer.Write(values);
        }

        private static byte[] ReadBytes(BinaryReader reader)
        {
            int count = ReadCount(reader, "bytes");
            byte[] result = reader.ReadBytes(count);
            if (result.Length != count) throw new EndOfStreamException("Content pack ended inside a byte array.");
            return result;
        }

        private static void WriteActions(BinaryWriter writer, IList<AOAction> actions)
        {
            writer.Write(actions == null ? 0 : actions.Count);
            if (actions == null) return;
            foreach (AOAction action in actions) { writer.Write((int)action.ActionType); WriteRequirements(writer, action.Requirements); }
        }

        private static List<AOAction> ReadActions(BinaryReader reader)
        {
            int count = ReadCount(reader, "actions");
            var result = new List<AOAction>(count);
            for (int i = 0; i < count; i++) result.Add(new AOAction { ActionType = (ActionType)reader.ReadInt32(), Requirements = ReadRequirements(reader) });
            return result;
        }

        private static void WriteEvents(BinaryWriter writer, IList<Event> events)
        {
            writer.Write(events == null ? 0 : events.Count);
            if (events == null) return;
            foreach (Event value in events)
            {
                writer.Write((int)value.EventType);
                writer.Write(value.Functions == null ? 0 : value.Functions.Count);
                if (value.Functions != null) foreach (Function function in value.Functions) WriteFunction(writer, function);
            }
        }

        private static List<Event> ReadEvents(BinaryReader reader, int version)
        {
            int count = ReadCount(reader, "events");
            var result = new List<Event>(count);
            for (int i = 0; i < count; i++)
            {
                var value = new Event { EventType = (EventType)reader.ReadInt32() };
                int functions = ReadCount(reader, "functions");
                for (int j = 0; j < functions; j++) value.Functions.Add(ReadFunction(reader, version));
                result.Add(value);
            }
            return result;
        }

        private static void WriteFunction(BinaryWriter writer, Function function)
        {
            writer.Write(function.FunctionType);
            writer.Write(function.Target);
            writer.Write(function.TickCount);
            writer.Write(function.TickInterval);
            writer.Write(function.dolocalstats);
            WriteRequirements(writer, function.Requirements);
            IList<MessagePackObject> values = function.Arguments == null ? null : function.Arguments.Values;
            writer.Write(values == null ? 0 : values.Count);
            if (values != null)
            {
                foreach (MessagePackObject value in values)
                {
                    if (value.IsTypeOf(typeof(int)) == true) { writer.Write((byte)1); writer.Write(value.AsInt32()); }
                    else if (value.IsTypeOf(typeof(float)) == true) { writer.Write((byte)2); writer.Write(value.AsSingle()); }
                    else if (value.IsTypeOf(typeof(string)) == true) { writer.Write((byte)3); writer.Write(value.AsStringUtf8() ?? string.Empty); }
                    else throw new InvalidDataException("Unsupported function argument type: " + value.UnderlyingType);
                }
            }

            FunctionRecordData record = function.Record;
            writer.Write(record != null);
            if (record == null) return;
            writer.Write(record.LeadingZeroWords);
            writer.Write(record.Header1);
            writer.Write(record.Header2);
            writer.Write(record.Header3);
            WriteIntList(writer, record.RequirementTriples);
            WriteBytes(writer, record.Arguments);
        }

        private static Function ReadFunction(BinaryReader reader, int version)
        {
            var result = new Function
            {
                FunctionType = reader.ReadInt32(), Target = reader.ReadInt32(), TickCount = reader.ReadInt32(),
                TickInterval = reader.ReadUInt32(), dolocalstats = reader.ReadBoolean(), Requirements = ReadRequirements(reader)
            };
            int count = ReadCount(reader, "function arguments");
            for (int i = 0; i < count; i++)
            {
                byte kind = reader.ReadByte();
                if (kind == 1) result.Arguments.Values.Add(reader.ReadInt32());
                else if (kind == 2) result.Arguments.Values.Add(reader.ReadSingle());
                else if (kind == 3) result.Arguments.Values.Add(reader.ReadString());
                else throw new InvalidDataException("Unsupported function argument kind " + kind);
            }

            if (version >= 2 && reader.ReadBoolean())
            {
                result.Record = new FunctionRecordData
                {
                    LeadingZeroWords = reader.ReadInt32(), Header1 = reader.ReadInt32(), Header2 = reader.ReadInt32(),
                    Header3 = reader.ReadInt32(), RequirementTriples = ReadIntList(reader), Arguments = ReadBytes(reader)
                };
            }

            return result;
        }

        private static void WriteRecordData(BinaryWriter writer, RecordData record)
        {
            writer.Write(record != null);
            if (record == null) return;
            writer.Write(record.HeaderA);
            writer.Write(record.HeaderB);
            writer.Write(record.HeaderC);
            writer.Write(record.Description ?? string.Empty);
            WriteIntList(writer, record.BlockOrder);

            writer.Write(record.AttributeGroups == null ? 0 : record.AttributeGroups.Count);
            if (record.AttributeGroups != null)
            {
                foreach (AttributeGroup group in record.AttributeGroups) { writer.Write(group.Key); WriteIntList(writer, group.Pairs); }
            }

            WriteIntList(writer, record.Block6Pairs);

            writer.Write(record.AnimSoundSets == null ? 0 : record.AnimSoundSets.Count);
            if (record.AnimSoundSets != null)
            {
                foreach (AnimSoundSet set in record.AnimSoundSets)
                {
                    writer.Write(set.BlockKey);
                    writer.Write(set.Value);
                    writer.Write(set.Entries == null ? 0 : set.Entries.Count);
                    if (set.Entries != null) foreach (AnimSoundEntry entry in set.Entries) { writer.Write(entry.Key); WriteIntList(writer, entry.Values); }
                }
            }

            writer.Write(record.ActionRequirements == null ? 0 : record.ActionRequirements.Count);
            if (record.ActionRequirements != null)
            {
                foreach (RequirementSet set in record.ActionRequirements) { writer.Write(set.Hook); WriteIntList(writer, set.Triples); }
            }

            writer.Write(record.ShopBlocks == null ? 0 : record.ShopBlocks.Count);
            if (record.ShopBlocks != null)
            {
                foreach (ShopBlock block in record.ShopBlocks)
                {
                    writer.Write(block.EventType);
                    writer.Write(block.Entries == null ? 0 : block.Entries.Count);
                    if (block.Entries != null) foreach (byte[] entry in block.Entries) WriteBytes(writer, entry);
                }
            }

            writer.Write(record.BareFunctions == null ? 0 : record.BareFunctions.Count);
            if (record.BareFunctions != null)
            {
                foreach (Function function in record.BareFunctions) WriteFunction(writer, function);
            }
        }

        private static RecordData ReadRecordData(BinaryReader reader, int version)
        {
            if (!reader.ReadBoolean()) return null;
            var record = new RecordData
            {
                HeaderA = reader.ReadInt32(), HeaderB = reader.ReadInt32(), HeaderC = reader.ReadInt32(),
                Description = reader.ReadString(), BlockOrder = ReadIntList(reader)
            };

            int groups = ReadCount(reader, "attribute groups");
            for (int i = 0; i < groups; i++) record.AttributeGroups.Add(new AttributeGroup { Key = reader.ReadInt32(), Pairs = ReadIntList(reader) });

            record.Block6Pairs = ReadIntList(reader);

            int sets = ReadCount(reader, "animation/sound sets");
            for (int i = 0; i < sets; i++)
            {
                var set = new AnimSoundSet { BlockKey = reader.ReadInt32(), Value = reader.ReadInt32() };
                int entries = ReadCount(reader, "animation/sound entries");
                for (int j = 0; j < entries; j++) set.Entries.Add(new AnimSoundEntry { Key = reader.ReadInt32(), Values = ReadIntList(reader) });
                record.AnimSoundSets.Add(set);
            }

            int actions = ReadCount(reader, "action requirement sets");
            for (int i = 0; i < actions; i++) record.ActionRequirements.Add(new RequirementSet { Hook = reader.ReadInt32(), Triples = ReadIntList(reader) });

            int shops = ReadCount(reader, "shop blocks");
            for (int i = 0; i < shops; i++)
            {
                var block = new ShopBlock { EventType = reader.ReadInt32() };
                int entries = ReadCount(reader, "shop entries");
                for (int j = 0; j < entries; j++) block.Entries.Add(ReadBytes(reader));
                record.ShopBlocks.Add(block);
            }

            if (version >= 3)
            {
                int functions = ReadCount(reader, "bare functions");
                for (int i = 0; i < functions; i++) record.BareFunctions.Add(ReadFunction(reader, version));
            }

            return record;
        }

        private static void WriteRequirements(BinaryWriter writer, IList<Requirement> requirements)
        {
            writer.Write(requirements == null ? 0 : requirements.Count);
            if (requirements == null) return;
            foreach (Requirement value in requirements)
            {
                writer.Write((int)value.ChildOperator); writer.Write((int)value.Operator);
                writer.Write(value.Statnumber); writer.Write((int)value.Target); writer.Write(value.Value);
            }
        }

        private static List<Requirement> ReadRequirements(BinaryReader reader)
        {
            int count = ReadCount(reader, "requirements");
            var result = new List<Requirement>(count);
            for (int i = 0; i < count; i++) result.Add(new Requirement
            {
                ChildOperator = (Operator)reader.ReadInt32(), Operator = (Operator)reader.ReadInt32(),
                Statnumber = reader.ReadInt32(), Target = (ItemTarget)reader.ReadInt32(), Value = reader.ReadInt32()
            });
            return result;
        }

        private static void WriteIdentity(BinaryWriter writer, Identity identity)
        {
            writer.Write((int)identity.Type); writer.Write(identity.Instance);
        }

        private static Identity ReadIdentity(BinaryReader reader)
        {
            return new Identity { Type = (IdentityType)reader.ReadInt32(), Instance = reader.ReadInt32() };
        }

        private static int ReadCount(BinaryReader reader, string name)
        {
            int count = reader.ReadInt32();
            if (count < 0 || count > 10000000) throw new InvalidDataException("Invalid " + name + " count: " + count);
            return count;
        }
    }
}
