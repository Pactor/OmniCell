#region License

// Copyright (c) 2005-2013, CellAO Team
// 
// All rights reserved.
// 
// Redistribution and use in source and binary forms, with or without modification, are permitted provided that the following conditions are met:
// 
//     * Redistributions of source code must retain the above copyright notice, this list of conditions and the following disclaimer.
//     * Redistributions in binary form must reproduce the above copyright notice, this list of conditions and the following disclaimer in the documentation and/or other materials provided with the distribution.
//     * Neither the name of the CellAO Team nor the names of its contributors may be used to endorse or promote products derived from this software without specific prior written permission.
// 
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
// "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT
// LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR
// A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR
// CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL,
// EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO,
// PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR
// PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF
// LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING
// NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
// SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

#endregion

namespace Extractor_Serializer
{
    #region Usings ...

    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;

    using OmniCell.Core.Actions;
    using OmniCell.Core.Events;
    using OmniCell.Core.Functions;
    using OmniCell.Core.Items;
    using OmniCell.Core.Nanos;
    using OmniCell.Core.Requirements;
    using OmniCell.Enums;

    using MsgPack;

    using OmniCell.Core.Content;

    using Utility;

    #endregion

    /// <summary>
    /// Parser for serialized items of the RDB
    /// </summary>
    public class NewParser
    {

        #region Fields

        /// <summary>
        /// The br.
        /// </summary>
        public BufferedReader br;

        /// <summary>
        /// The function sets.
        /// </summary>
        private Dictionary<string, string> FunctionSets;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="NewParser"/> class.
        /// </summary>
        public NewParser()
        {
            this.LoadFunctionSets();
        }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// The parse anim sound set.
        /// </summary>
        /// <param name="typeN">
        /// The type n.
        /// </param>
        /// <param name="ITEM">
        /// The item.
        /// </param>
        private void ParseAnimSoundSet(int blockKey, RecordData record)
        {
            var set = new AnimSoundSet
            {
                BlockKey = blockKey,
                Value = this.br.ReadInt32()
            };
            int count = this.Read3F1Count("animation/sound entries");
            for (int i = 0; i < count; i++)
            {
                var entry = new AnimSoundEntry { Key = this.br.ReadInt32() };
                int valueCount = this.Read3F1Count("animation/sound values");
                for (int j = 0; j < valueCount; j++)
                {
                    entry.Values.Add(this.br.ReadInt32());
                }

                set.Entries.Add(entry);
            }

            record.AnimSoundSets.Add(set);
        }

        /// <summary>
        /// The parse item.
        /// </summary>
        /// <param name="rectype">
        /// The rectype.
        /// </param>
        /// <param name="recnum">
        /// The recnum.
        /// </param>
        /// <param name="data">
        /// The data.
        /// </param>
        /// <param name="itemNamesSqlList">
        /// </param>
        /// <returns>
        /// The <see cref="AOItem"/>.
        /// </returns>
        /// <summary>
        /// The name of the item ParseItem read last. Kept here rather than on the template or its
        /// RecordData, which are serialized into items.ocp; item relations are matched by name.
        /// </summary>
        public string LastItemName { get; private set; }

        public ItemTemplate ParseItem(Extractor.RecordType recordType, int recnum, byte[] data, List<string> itemNamesSqlList)
        {
            int rectype = (int)recordType;
            this.br = new BufferedReader(rectype, recnum, data);
            var record = new RecordData();
            var aoi = new ItemTemplate { ID = recnum, Record = record };

            this.ReadHeader(record);
            int attributeCount = this.Read3F1Count("item attributes");
            for (int i = 0; i < attributeCount; i++)
            {
                int attrkey = this.br.ReadInt32();
                int attrval = this.br.ReadInt32();
                if (attrkey == 54)
                {
                    aoi.Quality = attrval;
                }
                else
                {
                    aoi.Stats.Add(attrkey, attrval);
                }
            }

            this.ExpectInt32(0x15, "name block key");
            this.ExpectInt32(0x21, "name block value");

            int nameLength = this.ReadLength16("item name");
            int descriptionLength = this.ReadLength16("item description");
            string itemname = this.br.ReadString(nameLength);
            record.Description = this.br.ReadString(descriptionLength);
            this.LastItemName = itemname;

            if (itemNamesSqlList != null)
            {
                itemNamesSqlList.Add(string.Format("( {0} , '{1}' , '{2}', '{3}' ) ",
                    recnum,
                    SqlText(itemname),
                    Enum.GetName(typeof(Extractor.RecordType), recordType),
                    aoi.getItemAttribute(79)));
            }

            this.ParseBody(record, aoi.Attack, aoi.Defend, aoi.Actions, aoi.Events);

            return aoi;
        }

        /// <summary>
        /// The parse nano.
        /// </summary>
        /// <param name="rectype">
        /// The rectype.
        /// </param>
        /// <param name="recnum">
        /// The recnum.
        /// </param>
        /// <param name="data">
        /// The data.
        /// </param>
        /// <param name="sqlFile">
        /// The sql file.
        /// </param>
        /// <returns>
        /// The <see cref="AONanos"/>.
        /// </returns>
        /// <summary>
        /// A name as the inside of a MySQL string literal. MySQL reads a backslash as an escape, so a
        /// name ending in one swallowed its closing quote and broke every row after it in the file.
        /// </summary>
        private static string SqlText(string text)
        {
            return text.Replace("\\", "\\\\").Replace("'", "''");
        }

        public NanoFormula ParseNano(int recnum, byte[] data, List<string> itemNamesSqlList)
        {
            this.br = new BufferedReader((int)Extractor.RecordType.Nano, recnum, data);
            var record = new RecordData();
            var aon = new NanoFormula { ID = recnum, Record = record };

            this.ReadHeader(record);
            int attributeCount = this.Read3F1Count("nano attributes");
            for (int i = 0; i < attributeCount; i++)
            {
                int attrkey = this.br.ReadInt32();
                int attrval = this.br.ReadInt32();
                aon.Stats.Add(attrkey, attrval);
            }

            this.ExpectInt32(0x15, "name block key");
            this.ExpectInt32(0x21, "name block value");

            int nameLength = this.ReadLength16("nano name");
            int descriptionLength = this.ReadLength16("nano description");
            string nanoName = this.br.ReadString(nameLength);
            record.Description = this.br.ReadString(descriptionLength);

            // The name was previously read and dropped on the floor, so nanos never
            // reached the itemnames table at all. ParseItem records the same four
            // columns at the same offset; mirror it so nano programs are named too.
            if (itemNamesSqlList != null)
            {
                itemNamesSqlList.Add(
                    string.Format(
                        "( {0} , '{1}' , '{2}', '{3}' ) ",
                        recnum,
                        SqlText(nanoName),
                        Enum.GetName(typeof(Extractor.RecordType), Extractor.RecordType.Nano),
                        aon.getItemAttribute(79)));
            }

            this.ParseBody(record, aon.Attack, aon.Defend, aon.Actions, aon.Events);

            return aon;
        }

        private void ReadHeader(RecordData record)
        {
            record.HeaderA = this.br.ReadInt32();
            record.HeaderB = this.br.ReadInt32();
            record.HeaderC = this.br.ReadInt32();
            this.ExpectInt32(0x17, "attribute block key");
        }

        private void ParseBody(
            RecordData record,
            Dictionary<int, int> attack,
            Dictionary<int, int> defend,
            List<AOAction> actions,
            List<Event> events)
        {
            while (this.br.Ptr < this.br.Buffer.Length)
            {
                int blockOffset = this.br.Ptr;
                int blockKey = this.br.ReadInt32();
                record.BlockOrder.Add(blockKey);
                switch (blockKey)
                {
                    case 0:
                        break;
                    case 2:
                        this.ParseFunctionSet(events);
                        break;
                    case 4:
                        this.ParseAtkDefSet(attack, defend, record);
                        break;
                    case 6:
                        this.ParseBlock6(record);
                        break;
                    case 14:
                    case 20:
                        this.ParseAnimSoundSet(blockKey, record);
                        break;
                    case 22:
                        this.ParseActionSet(actions, record);
                        break;
                    case 23:
                        this.ParseShopHash(events, record);
                        break;
                    default:
                        if (this.FunctionSets.ContainsKey(blockKey.ToString()))
                        {
                            record.BareFunctions.Add(this.ParseFunction(blockKey, 0));
                            break;
                        }

                        throw this.ParseError("unknown body block " + blockKey, blockOffset);
                }
            }
        }

        private void ParseBlock6(RecordData record)
        {
            this.ExpectInt32(0x1B, "block 6 value");
            int count = this.Read3F1Count("block 6 pairs");
            for (int i = 0; i < count; i++)
            {
                record.Block6Pairs.Add(this.br.ReadInt32());
                record.Block6Pairs.Add(this.br.ReadInt32());
            }
        }

        private int ReadLength16(string description)
        {
            int offset = this.br.Ptr;
            short value = this.br.ReadInt16();
            if (value < 0)
            {
                throw this.ParseError("negative " + description + " length " + value, offset);
            }

            return value;
        }

        private int Read3F1Count(string description)
        {
            int offset = this.br.Ptr;
            int encoded = this.br.ReadInt32();
            if (encoded < 1009 || (encoded % 1009) != 0)
            {
                throw this.ParseError("invalid " + description + " counter " + encoded, offset);
            }

            int count = (encoded / 1009) - 1;
            if (count < 0 || count > 10000000)
            {
                throw this.ParseError("out-of-range " + description + " count " + count, offset);
            }

            return count;
        }

        private void ExpectInt32(int expected, string description)
        {
            int offset = this.br.Ptr;
            int actual = this.br.ReadInt32();
            if (actual != expected)
            {
                throw this.ParseError(
                    description + " is " + actual + ", expected " + expected,
                    offset);
            }
        }

        private InvalidDataException ParseError(string message, int offset)
        {
            return new InvalidDataException(
                "RDB record type " + this.br.RecordType + " id " + this.br.RecordNum
                + " at offset 0x" + offset.ToString("X") + ": " + message
                + ". Nearby bytes: " + this.br.DescribeBytes(offset, 16, 32));
        }

        /// <summary>
        /// The parse reqs.
        /// </summary>
        /// <param name="rreqs">
        /// The rreqs.
        /// </param>
        /// <returns>
        /// The <see cref="List"/>.
        /// </returns>
        public List<Requirement> ParseReqs(List<rawreqs> rreqs)
        {
            int numreqs = rreqs.Count;

            List<Requirement> output = new List<Requirement>();
            Requirement aor = null;
            for (int i = 0; i < numreqs; i++)
            {
                rawreqs rr = rreqs[i];
                if (aor == null)
                {
                    aor = new Requirement();
                }

                aor.Target = ItemTarget.Self; // 0x13
                aor.Statnumber = rr.stat;
                aor.Operator = (Operator)Enum.ToObject(typeof(Operator), rr.ops);
                aor.Value = rr.val;
                aor.ChildOperator = Operator.Unknown;

                if ((i < numreqs - 1)
                    && (
                    (aor.Operator == Operator.OnTarget)
                    || (aor.Operator == Operator.OnSelf)
                    || (aor.Operator == Operator.OnUser)
                        || (aor.Operator == Operator.OnValidTarget)
                        || (aor.Operator == Operator.OnInvalidTarget)
                        || (aor.Operator == Operator.OnValidUser)
                        || (aor.Operator == Operator.OnInvalidUser)
                        || (aor.Operator == Operator.OnGeneralBeholder)
                        || (aor.Operator == Operator.OnCaster)
                        || (aor.Operator == Operator.Unknown2)))
                {
                    aor.Target = (ItemTarget)(int)aor.Operator;
                    i++;
                    rr = rreqs[i];
                    aor.Statnumber = rr.stat;
                    aor.Value = rr.val;
                    aor.Operator = (Operator)Enum.ToObject(typeof(Operator), rr.ops);
                }

                if (!((i >= numreqs - 1) || (numreqs == 2)))
                {
                    int anum = rreqs[i + 1].stat;
                    int aval = rreqs[i + 1].val;
                    int aop = rreqs[i + 1].ops;

                    if ((((aop == (int)Operator.Or) || (aop == (int)Operator.And)) || (aop == (int)Operator.Not)) || (anum == (int)Operator.EqualTo))
                    {
                        aor.ChildOperator = (Operator)Enum.ToObject(typeof(Operator), aop);
                        i++;
                    }
                }
                output.Add(aor);
                aor = null;
            }

            if (output.Count > 1)
            {
                output[0].ChildOperator = output[1].ChildOperator;
            }
            else
            {
                output[0].ChildOperator = Operator.Or;
            }
            /*output[0].ChildOperator = Operator.Or;
            for (int i = 0; i < output.Count - 2; i++)
            {
                output[i].ChildOperator = output[i + 1].ChildOperator;
            }*/

            return output;
        }

        /// <summary>
        /// The read reqs.
        /// </summary>
        /// <param name="numreqs">
        /// The numreqs.
        /// </param>
        /// <returns>
        /// The <see cref="List"/>.
        /// </returns>
        public List<Requirement> ReadReqs(int numreqs)
        {
            if (numreqs > 0)
            {
                return this.ParseReqs(this.ReadRawReqs(numreqs, null));
            }

            return new List<Requirement>();
        }

        private List<rawreqs> ReadRawReqs(int count, List<int> flattened)
        {
            if (count < 0 || count > 1000000)
            {
                throw this.ParseError("invalid requirement count " + count, this.br.Ptr - 4);
            }

            var result = new List<rawreqs>(count);
            for (int i = 0; i < count; i++)
            {
                int stat = this.br.ReadInt32();
                int value = this.br.ReadInt32();
                int operation = this.br.ReadInt32();
                result.Add(new rawreqs { stat = stat, val = value, ops = operation });
                if (flattened != null)
                {
                    flattened.Add(stat);
                    flattened.Add(value);
                    flattened.Add(operation);
                }
            }

            return result;
        }

        #endregion

        #region Methods

        /// <summary>
        /// The load function sets.
        /// </summary>
        private void LoadFunctionSets()
        {
            this.FunctionSets = new Dictionary<string, string>();
            string filename = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "FunctionSets.cfg");
            TextReader tr = new StreamReader(filename, Encoding.GetEncoding("windows-1252"));
            string line;
            while ((line = tr.ReadLine()) != null)
            {
                if (line != string.Empty)
                {
                    string[] parts = line.Split('=');
                    this.FunctionSets.Add(parts[0], parts[1]);
                }
            }

            tr.Close();
        }

        /// <summary>
        /// The parse action set.
        /// </summary>
        /// <param name="actions">
        /// The actions.
        /// </param>
        /// <exception cref="Exception">
        /// </exception>
        private void ParseActionSet(List<AOAction> actions, RecordData record)
        {
            this.ExpectInt32(0x24, "action block value");
            int count = this.Read3F1Count("actions");
            for (int i = 0; i < count; i++)
            {
                int actionNumber = this.br.ReadInt32();
                int requirementCount = this.Read3F1Count("action requirements");
                var raw = new RequirementSet { Hook = actionNumber };
                List<rawreqs> requirements = this.ReadRawReqs(requirementCount, raw.Triples);
                var action = new AOAction
                {
                    ActionType = (ActionType)Enum.ToObject(typeof(ActionType), actionNumber),
                    Requirements = requirementCount == 0
                        ? new List<Requirement>()
                        : this.ParseReqs(requirements)
                };
                actions.Add(action);
                record.ActionRequirements.Add(raw);
            }
        }

        /// <summary>
        /// The parse args.
        /// </summary>
        /// <param name="funcNum">
        /// The func num.
        /// </param>
        /// <param name="R">
        /// The r.
        /// </param>
        /// <returns>
        /// The <see cref="object[]"/>.
        /// </returns>
        /// <exception cref="IndexOutOfRangeException">
        /// </exception>
        private object[] ParseArgs(int funcNum)
        {
            string definition;
            if (!this.FunctionSets.TryGetValue(funcNum.ToString(), out definition))
            {
                throw this.ParseError("unknown function " + funcNum, this.br.Ptr);
            }

            var values = new List<object>();
            foreach (string rawPart in definition.Split(','))
            {
                string part = rawPart.Trim().ToLowerInvariant();
                if (part.Length < 2)
                {
                    throw this.ParseError("invalid FunctionSets entry '" + rawPart + "' for function " + funcNum, this.br.Ptr);
                }

                int count;
                if (!int.TryParse(part.Substring(0, part.Length - 1), out count) || count < 0)
                {
                    throw this.ParseError("invalid FunctionSets count '" + rawPart + "' for function " + funcNum, this.br.Ptr);
                }

                char kind = part[part.Length - 1];
                for (int i = 0; i < count; i++)
                {
                    if (kind == 'n')
                    {
                        values.Add(this.br.ReadInt32());
                    }
                    else if (kind == 'h')
                    {
                        values.Add(this.br.ReadHash());
                    }
                    else if (kind == 's')
                    {
                        int lengthOffset = this.br.Ptr;
                        int storedLength = this.br.ReadInt32();
                        if (storedLength < 0 || storedLength > this.br.Buffer.Length - this.br.Ptr)
                        {
                            throw this.ParseError(
                                "invalid string argument length " + storedLength + " for function " + funcNum,
                                lengthOffset);
                        }

                        string value = string.Empty;
                        if (storedLength > 0)
                        {
                            value = this.br.ReadString(storedLength - 1);
                            int terminatorOffset = this.br.Ptr;
                            if (this.br.ReadByte() != 0)
                            {
                                throw this.ParseError("string argument has no null terminator", terminatorOffset);
                            }
                        }

                        values.Add(value);
                    }
                    else if (kind == 'x')
                    {
                        this.br.ReadBytes(1);
                    }
                    else
                    {
                        throw this.ParseError("unknown FunctionSets type '" + kind + "' for function " + funcNum, this.br.Ptr);
                    }
                }
            }

            return values.ToArray();
        }

        /// <summary>
        /// The parse atk def set.
        /// </summary>
        /// <param name="attackstat">
        /// The attackstat.
        /// </param>
        /// <param name="defstat">
        /// The defstat.
        /// </param>
        private void ParseAtkDefSet(Dictionary<int, int> attackstat, Dictionary<int, int> defstat, RecordData record)
        {
            this.ExpectInt32(4, "attack/defense block value");
            int groupCount = this.Read3F1Count("attack/defense groups");
            for (int i = 0; i < groupCount; i++)
            {
                var group = new AttributeGroup { Key = this.br.ReadInt32() };
                int memberCount = this.Read3F1Count("attack/defense group members");
                for (int j = 0; j < memberCount; j++)
                {
                    int key = this.br.ReadInt32();
                    int value = this.br.ReadInt32();
                    group.Pairs.Add(key);
                    group.Pairs.Add(value);
                    if (group.Key == 12) attackstat[key] = value;
                    else if (group.Key == 13) defstat[key] = value;
                }

                record.AttributeGroups.Add(group);
            }
        }

        /// <summary>
        /// The parse function set.
        /// </summary>
        /// <param name="retlist">
        /// The retlist.
        /// </param>
        private void ParseFunctionSet(List<Event> retlist)
        {
            int eventTypeValue = this.br.ReadInt32();
            int count = this.Read3F1Count("event functions");
            var aoe = new Event
            {
                EventType = (EventType)Enum.ToObject(typeof(EventType), eventTypeValue)
            };

            for (int i = 0; i < count; i++)
            {
                int leadingZeroWords = 0;
                while (this.br.PeekInt32() == 0)
                {
                    this.br.ReadInt32();
                    leadingZeroWords++;
                }

                int functionId = this.br.ReadInt32();
                aoe.Functions.Add(this.ParseFunction(functionId, leadingZeroWords));
            }

            retlist.Add(aoe);
        }

        private Function ParseFunction(int functionId, int leadingZeroWords)
        {
            if (!this.FunctionSets.ContainsKey(functionId.ToString()))
            {
                throw this.ParseError("unknown function " + functionId, this.br.Ptr - 4);
            }

            var raw = new FunctionRecordData { LeadingZeroWords = leadingZeroWords };
            var function = new Function { FunctionType = functionId, Record = raw };
            raw.Header1 = this.br.ReadInt32();
            raw.Header2 = this.br.ReadInt32();

            int requirementCountOffset = this.br.Ptr;
            int requirementCount = this.br.ReadInt32();
            if (requirementCount < 0 || requirementCount > 1000000)
            {
                throw this.ParseError("invalid function requirement count " + requirementCount, requirementCountOffset);
            }

            List<rawreqs> requirements = this.ReadRawReqs(requirementCount, raw.RequirementTriples);
            if (requirements.Count > 0) function.Requirements.AddRange(this.ParseReqs(requirements));

            function.TickCount = this.br.ReadInt32();
            function.TickInterval = unchecked((uint)this.br.ReadInt32());
            function.Target = this.br.ReadInt32();
            raw.Header3 = this.br.ReadInt32();

            int argumentOffset = this.br.Ptr;
            foreach (object value in this.ParseArgs(functionId))
            {
                function.Arguments.Values.Add(MessagePackObject.FromObject(value));
            }

            raw.Arguments = this.br.CopyBytes(argumentOffset, this.br.Ptr - argumentOffset);
            return function;
        }

        /// <summary>
        /// The parse shop hash.
        /// </summary>
        /// <param name="events">
        /// The events.
        /// </param>
        private void ParseShopHash(List<Event> events, RecordData record)
        {
            int eventNum = this.br.ReadInt32();
            int count = this.Read3F1Count("shop entries");
            var aoe = new Event { EventType = (EventType)Enum.ToObject(typeof(EventType), eventNum) };
            var block = new ShopBlock { EventType = eventNum };
            for (int i = 0; i < count; i++)
            {
                int entryOffset = this.br.Ptr;
                string hash = this.br.ReadString(4);
                int first = this.br.ReadByte();
                int second = this.br.ReadByte();
                if (first == 0 && second == 0)
                {
                    first = this.br.ReadInt16();
                    second = this.br.ReadInt16();
                }

                this.br.ReadBytes(11);
                block.Entries.Add(this.br.CopyBytes(entryOffset, this.br.Ptr - entryOffset));

                var function = new Function
                {
                    Target = 255,
                    TickCount = 1,
                    TickInterval = 0,
                    FunctionType = (int)FunctionType.Shophash
                };
                function.Arguments.Values.Add(hash);
                function.Arguments.Values.Add(first);
                function.Arguments.Values.Add(second);
                aoe.Functions.Add(function);
            }

            events.Add(aoe);
            record.ShopBlocks.Add(block);
        }

        #endregion

        /// <summary>
        /// The buffered reader.
        /// </summary>
        public class BufferedReader
        {
            #region Fields

            /// <summary>
            /// The buffer.
            /// </summary>
            public byte[] Buffer;

            /// <summary>
            /// The ptr.
            /// </summary>
            public int Ptr;

            /// <summary>
            /// The record num.
            /// </summary>
            public int RecordNum;

            /// <summary>
            /// The record type.
            /// </summary>
            public int RecordType;

            #endregion

            #region Constructors and Destructors

            /// <summary>
            /// Initializes a new instance of the <see cref="BufferedReader"/> class.
            /// </summary>
            /// <param name="rectype">
            /// The rectype.
            /// </param>
            /// <param name="recnum">
            /// The recnum.
            /// </param>
            /// <param name="data">
            /// The data.
            /// </param>
            public BufferedReader(int rectype, int recnum, byte[] data)
            {
                this.RecordType = rectype;
                this.RecordNum = recnum;
                this.Buffer = data;
            }

            #endregion

            #region Public Methods and Operators

            /// <summary>
            /// The read 3 f 1.
            /// </summary>
            /// <returns>
            /// The <see cref="int"/>.
            /// </returns>
            public int Read3F1()
            {
                int num = this.ReadInt32();
                num = (int)((long)Math.Round(Math.Round(unchecked(num / 1009.0 - 1.0))));
                return num;
            }

            /// <summary>
            /// The read byte.
            /// </summary>
            /// <returns>
            /// The <see cref="byte"/>.
            /// </returns>
            public byte ReadByte()
            {
                this.EnsureAvailable(1, "byte");
                byte b = this.Buffer[this.Ptr];
                this.Ptr++;
                return b;
            }

            public byte[] ReadBytes(int count)
            {
                this.EnsureAvailable(count, "byte sequence");
                byte[] result = this.CopyBytes(this.Ptr, count);
                this.Ptr += count;
                return result;
            }

            public byte[] CopyBytes(int offset, int count)
            {
                if (offset < 0 || count < 0 || offset > this.Buffer.Length - count)
                {
                    throw this.CreateReadError("invalid byte range offset " + offset + " length " + count, offset);
                }

                var result = new byte[count];
                Array.Copy(this.Buffer, offset, result, 0, count);
                return result;
            }

            public string DescribeBytes(int offset, int before, int after)
            {
                int start = Math.Max(0, offset - before);
                int end = Math.Min(this.Buffer.Length, offset + after);
                var result = new StringBuilder();
                for (int i = start; i < end; i++)
                {
                    if (result.Length > 0) result.Append(' ');
                    if (i == offset) result.Append('[');
                    result.Append(this.Buffer[i].ToString("X2"));
                    if (i == offset) result.Append(']');
                }

                return result.ToString();
            }

            public int PeekInt32()
            {
                this.EnsureAvailable(4, "int32");
                return BitConverter.ToInt32(this.Buffer, this.Ptr);
            }

            /// <summary>
            /// The read hash.
            /// </summary>
            /// <returns>
            /// The <see cref="string"/>.
            /// </returns>
            public string ReadHash()
            {
                byte[] array = this.ReadBytes(4);
                Array.Reverse(array);
                return Encoding.ASCII.GetString(array);
            }

            /// <summary>
            /// The read int 16.
            /// </summary>
            /// <returns>
            /// The <see cref="short"/>.
            /// </returns>
            public short ReadInt16()
            {
                this.EnsureAvailable(2, "int16");
                short num = BitConverter.ToInt16(this.Buffer, this.Ptr);
                this.Ptr += 2;
                return num;
            }

            /// <summary>
            /// The read int 32.
            /// </summary>
            /// <returns>
            /// The <see cref="int"/>.
            /// </returns>
            public int ReadInt32()
            {
                this.EnsureAvailable(4, "int32");
                int num = BitConverter.ToInt32(this.Buffer, this.Ptr);
                this.Ptr += 4;
                return num;
            }

            /// <summary>
            /// The read string.
            /// </summary>
            /// <returns>
            /// The <see cref="string"/>.
            /// </returns>
            public string ReadString()
            {
                int start = this.Ptr;
                while (this.Ptr < this.Buffer.Length && this.Buffer[this.Ptr] != 0)
                {
                    this.Ptr++;
                }

                if (this.Ptr == this.Buffer.Length)
                {
                    throw this.CreateReadError("unterminated string", start);
                }

                return Encoding.UTF8.GetString(this.Buffer, start, this.Ptr - start);
            }

            /// <summary>
            /// The read string.
            /// </summary>
            /// <param name="Length">
            /// The length.
            /// </param>
            /// <returns>
            /// The <see cref="string"/>.
            /// </returns>
            public string ReadString(int Length)
            {
                this.EnsureAvailable(Length, "string");
                string result = Encoding.UTF8.GetString(this.Buffer, this.Ptr, Length);
                this.Ptr += Length;
                return result;
            }

            /// <summary>
            /// The skip.
            /// </summary>
            /// <param name="Count">
            /// The count.
            /// </param>
            public void Skip(int Count)
            {
                this.EnsureAvailable(Count, "skip");
                this.Ptr += Count;
            }

            private void EnsureAvailable(int count, string valueType)
            {
                if (count < 0 || this.Ptr > this.Buffer.Length - count)
                {
                    throw this.CreateReadError(
                        "cannot read " + valueType + " (need " + count + " bytes, have " + (this.Buffer.Length - this.Ptr) + ")",
                        this.Ptr);
                }
            }

            private InvalidDataException CreateReadError(string message, int offset)
            {
                return new InvalidDataException(
                    "RDB record type " + this.RecordType + " id " + this.RecordNum
                    + " at offset 0x" + offset.ToString("X") + ": " + message
                    + ". Nearby bytes: " + this.DescribeBytes(offset, 16, 32));
            }

            #endregion
        }

        /// <summary>
        /// The rawreqs.
        /// </summary>
        public class rawreqs
        {
            #region Fields

            /// <summary>
            /// The ops.
            /// </summary>
            public int ops = 0;

            /// <summary>
            /// The stat.
            /// </summary>
            public int stat = 0;

            /// <summary>
            /// The val.
            /// </summary>
            public int val = 0;

            #endregion
        }
    }
}
