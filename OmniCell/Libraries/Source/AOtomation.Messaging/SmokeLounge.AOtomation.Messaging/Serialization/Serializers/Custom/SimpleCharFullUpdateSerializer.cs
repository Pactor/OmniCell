// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SimpleCharFullUpdateSerializer.cs" company="SmokeLounge">
//   Copyright © 2013 SmokeLounge.
//   This program is free software. It comes without any warranty, to
//   the extent permitted by applicable law. You can redistribute it
//   and/or modify it under the terms of the Do What The Fuck You Want
//   To Public License, Version 2, as published by Sam Hocevar. See
//   http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the SimpleCharFullUpdateSerializer type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace SmokeLounge.AOtomation.Messaging.Serialization.Serializers.Custom
{
    using System;
    using System.Linq.Expressions;

    using SmokeLounge.AOtomation.Messaging.GameData;
    using SmokeLounge.AOtomation.Messaging.Messages;
    using SmokeLounge.AOtomation.Messaging.Messages.N3Messages;

    /// <summary>
    /// Reads and writes SimpleCharFullUpdate.
    /// </summary>
    /// <remarks>
    /// Both halves are a transcription of the reader in the client, at
    /// Gamecode.dll 0x1007916D - the ReadSubClass slot of
    /// n3SimpleCharFullUpdateIIR_t. To see it again:
    ///
    ///     objdump -d --start-address=0x1007916D --stop-address=0x10079830 Gamecode.dll
    ///
    /// The matching WriteSubClass at 0x10078010 is a bare "ret $0x4": the client
    /// never sends this message, so there is no writer in it to read. That is
    /// why the reader is the only source, and why the order below follows it
    /// exactly rather than following anything that seemed reasonable.
    ///
    /// This was rewritten in one pass against that listing. Chasing it a field
    /// at a time is what failed for days, because a missing field does not show
    /// up as a wrong field - it shows up as everything after it being wrong, and
    /// the standing guess before this (that HasExtendedTextures gated four
    /// bytes) was wrong in kind: it gates a counted array of forty four byte
    /// entries. Eight other fields were missing outright.
    ///
    /// Serialize is the inverse of Deserialize, field for field and flag for
    /// flag, deliberately: WireAudit reads a captured packet and writes it
    /// straight back, and either the bytes come out identical or they do not.
    /// </remarks>
    public class SimpleCharFullUpdateSerializer : ISerializer
    {
        #region Fields

        private readonly Type type;

        #endregion

        #region Constructors and Destructors

        public SimpleCharFullUpdateSerializer()
        {
            this.type = typeof(SimpleCharFullUpdateMessage);
        }

        #endregion

        #region Public Properties

        public Type Type
        {
            get
            {
                return this.type;
            }
        }

        #endregion

        #region Public Methods and Operators

        public object Deserialize(
            StreamReader streamReader,
            SerializationContext serializationContext,
            PropertyMetaData propertyMetaData = null)
        {
            var scfu = new SimpleCharFullUpdateMessage();

            // N3Message
            scfu.N3MessageType = (N3MessageType)streamReader.ReadInt32();
            scfu.Identity = streamReader.ReadIdentity();
            scfu.Unknown = streamReader.ReadByte();

            // The client refuses anything but 0x39 and 0x3A here and abandons
            // the packet, so this is a format version and not a payload byte.
            scfu.Version = streamReader.ReadByte();

            var flags = (SimpleCharFullUpdateFlags)streamReader.ReadInt32();
            scfu.Flags = flags;
            scfu.WireWidths = flags & Widths;

            if (flags.HasFlag(SimpleCharFullUpdateFlags.HasPlayfieldId))
            {
                scfu.PlayfieldId = streamReader.ReadInt32();
            }

            if (flags.HasFlag(SimpleCharFullUpdateFlags.HasParentDynel))
            {
                scfu.ParentDynel = streamReader.ReadIdentity();
            }

            scfu.Coordinates = new Vector3
                               {
                                   X = streamReader.ReadSingle(),
                                   Y = streamReader.ReadSingle(),
                                   Z = streamReader.ReadSingle()
                               };

            if (flags.HasFlag(SimpleCharFullUpdateFlags.HasHeading))
            {
                scfu.Heading = new Quaternion
                               {
                                   X = streamReader.ReadSingle(),
                                   Y = streamReader.ReadSingle(),
                                   Z = streamReader.ReadSingle(),
                                   W = streamReader.ReadSingle()
                               };
            }

            scfu.Appearance = new Appearance { Value = streamReader.ReadUInt32() };

            // The written length counts the NUL terminator, which ReadString trims.
            int nameLength = streamReader.ReadByte();
            scfu.Name = streamReader.ReadString(nameLength);

            scfu.CharacterFlags = (CharacterFlags)streamReader.ReadInt32();
            scfu.AccountFlags = streamReader.ReadInt16();
            scfu.Expansions = streamReader.ReadInt16();

            if (flags.HasFlag(SimpleCharFullUpdateFlags.IsNpc))
            {
                var npc = new SimpleNpcInfo();

                npc.Family = flags.HasFlag(SimpleCharFullUpdateFlags.HasSmallNpcFamily)
                                 ? streamReader.ReadByte()
                                 : streamReader.ReadInt16();

                npc.LosHeight = flags.HasFlag(SimpleCharFullUpdateFlags.HasSmallNpcLosHeight)
                                    ? streamReader.ReadByte()
                                    : streamReader.ReadInt16();

                npc.PetType = flags.HasFlag(SimpleCharFullUpdateFlags.UnknownDataFlag)
                                   ? streamReader.ReadByte()
                                   : streamReader.ReadInt16();

                npc.Unknown2 = streamReader.ReadInt16();

                if (npc.Unknown2 > 0)
                {
                    npc.Unknown3 = streamReader.ReadByte();
                }

                scfu.CharacterInfo = npc;
            }
            else
            {
                var pc = new SimplePcInfo();

                pc.CurrentNano = streamReader.ReadUInt32();
                pc.Team = streamReader.ReadInt32();
                pc.Swim = streamReader.ReadInt16();

                pc.StrengthBase = streamReader.ReadInt16();
                pc.AgilityBase = streamReader.ReadInt16();
                pc.StaminaBase = streamReader.ReadInt16();
                pc.IntelligenceBase = streamReader.ReadInt16();
                pc.SenseBase = streamReader.ReadInt16();
                pc.PsychicBase = streamReader.ReadInt16();

                if (scfu.CharacterFlags.HasFlag(CharacterFlags.HasVisibleName))
                {
                    pc.FirstName = streamReader.ReadString(streamReader.ReadInt16());
                    pc.LastName = streamReader.ReadString(streamReader.ReadInt16());
                }

                if (flags.HasFlag(SimpleCharFullUpdateFlags.HasOrgName))
                {
                    if (scfu.Version > 0x39)
                    {
                        pc.OrgId = streamReader.ReadInt32();
                    }

                    pc.OrgName = streamReader.ReadString(streamReader.ReadInt16());
                }

                scfu.CharacterInfo = pc;
            }

            scfu.Level = flags.HasFlag(SimpleCharFullUpdateFlags.HasExtendedLevel)
                             ? streamReader.ReadInt16()
                             : streamReader.ReadByte();

            // Unsigned. A pet summoned in a capture came back with -22396 health
            // because this read the small form as a signed short, and 43140 does
            // not fit in one. Serialize still writes it back as a short, so the
            // bits are unchanged either way - only the number we think it is was
            // wrong.
            scfu.Health = flags.HasFlag(SimpleCharFullUpdateFlags.HasSmallHealth)
                              ? streamReader.ReadUInt16()
                              : streamReader.ReadInt32();

            // The three arms are not three widths of one number. The byte form
            // is a delta: the client reads it at 0x100794D5 and stores
            // health minus it, at 0x100794E5, while the other two arms store
            // what the wire carries. So a byte of 3 does not mean 3 - it means
            // three less than Health, and a server that wants to say "damage
            // of N" has to send Health minus N when it sets this flag and N
            // when it does not.
            //
            // Reading and writing the raw byte round trips either way, which
            // is why 24,903 copies never said so. What is on the wire here is
            // kept raw on purpose; converting it would make the round trip
            // depend on Health being right.
            if (flags.HasFlag(SimpleCharFullUpdateFlags.HasSmallHealthDamage))
            {
                scfu.HealthDamage = streamReader.ReadByte();
            }
            else if (flags.HasFlag(SimpleCharFullUpdateFlags.HasSmallHealth))
            {
                scfu.HealthDamage = streamReader.ReadUInt16();
            }
            else
            {
                scfu.HealthDamage = streamReader.ReadInt32();
            }

            scfu.MonsterData = streamReader.ReadUInt32();
            scfu.MonsterScale = streamReader.ReadInt16();
            scfu.VisualFlags = streamReader.ReadInt16();
            scfu.VisibleTitle = streamReader.ReadByte();

            // A counted blob the client copies wholesale into a second stream
            // and then skips over, rather than parsing in place.
            scfu.VehicleData = streamReader.ReadBytes(streamReader.ReadInt32());

            if (flags.HasFlag(SimpleCharFullUpdateFlags.HasHeadMesh))
            {
                scfu.HeadMesh = streamReader.ReadUInt32();
            }

            scfu.RunSpeedBase = flags.HasFlag(SimpleCharFullUpdateFlags.HasExtendedRunSpeed)
                                    ? streamReader.ReadInt16()
                                    : streamReader.ReadByte();

            if (flags.HasFlag(SimpleCharFullUpdateFlags.IsUnderAttack))
            {
                scfu.FightingTarget = streamReader.ReadIdentity();
            }

            if (flags.HasFlag(SimpleCharFullUpdateFlags.HasExtendedTextures))
            {
                scfu.ExtendedTextures = new CharacterTexture[X3F1Count(streamReader.ReadInt32())];
                for (var i = 0; i < scfu.ExtendedTextures.Length; i++)
                {
                    scfu.ExtendedTextures[i] = new CharacterTexture
                                               {
                                                   Name = streamReader.ReadBytes(32),
                                                   TextureId = streamReader.ReadInt32(),
                                                   OverlayId = streamReader.ReadInt32(),
                                                   AlphaMode = streamReader.ReadInt32()
                                               };
                }
            }

            if (flags.HasFlag(SimpleCharFullUpdateFlags.IsImmune))
            {
                scfu.ImmuneData = streamReader.ReadByte();
            }

            if (flags.HasFlag(SimpleCharFullUpdateFlags.UnknownFlag3))
            {
                scfu.UnknownData3 = streamReader.ReadByte();
            }

            scfu.ActiveNanos = new ActiveNano[X3F1Count(streamReader.ReadInt32())];
            for (var i = 0; i < scfu.ActiveNanos.Length; i++)
            {
                scfu.ActiveNanos[i] = new ActiveNano
                                      {
                                          NanoId = streamReader.ReadInt32(),
                                          NanoInstance = streamReader.ReadInt32(),
                                          Unknown = streamReader.ReadInt32(),
                                          Time1 = streamReader.ReadInt32(),
                                          Time2 = streamReader.ReadInt32()
                                      };
            }

            if (flags.HasFlag(SimpleCharFullUpdateFlags.HasWaypoints))
            {
                var path = new WaypointPath();
                path.Target = streamReader.ReadIdentity();

                // A plain count, not the X3F1 form. The client caps its own
                // array at thirty; this does not, because a longer list is a
                // packet the client cannot read and should not be quietly
                // rewritten into one it can.
                var points = new Vector3[streamReader.ReadInt32()];
                for (var i = 0; i < points.Length; i++)
                {
                    points[i] = new Vector3
                                {
                                    X = streamReader.ReadSingle(),
                                    Y = streamReader.ReadSingle(),
                                    Z = streamReader.ReadSingle()
                                };
                }

                path.Points = points;
                scfu.Waypoints = path;
            }

            scfu.Textures = new Texture[X3F1Count(streamReader.ReadInt32())];
            for (var i = 0; i < scfu.Textures.Length; i++)
            {
                var texture = new Texture
                              {
                                  Place = streamReader.ReadInt32(),
                                  Id = streamReader.ReadInt32(),
                                  Group = streamReader.ReadInt32()
                              };

                if (texture.HasExtra)
                {
                    texture.OverlayId = streamReader.ReadInt32();
                    texture.AlphaMode = streamReader.ReadInt32();
                }

                scfu.Textures[i] = texture;
            }

            scfu.Meshes = new Mesh[X3F1Count(streamReader.ReadInt32())];
            for (var i = 0; i < scfu.Meshes.Length; i++)
            {
                scfu.Meshes[i] = new Mesh
                                 {
                                     Position = streamReader.ReadByte(),
                                     Id = streamReader.ReadUInt32(),
                                     OverrideTextureId = streamReader.ReadInt32(),
                                     Layer = streamReader.ReadByte()
                                 };
            }

            if (flags.HasFlag(SimpleCharFullUpdateFlags.HasNoWeaponPairs))
            {
                scfu.NoWeaponPairs = new WeaponPair[X3F1Count(streamReader.ReadInt32())];
                for (var i = 0; i < scfu.NoWeaponPairs.Length; i++)
                {
                    scfu.NoWeaponPairs[i] = new WeaponPair
                                            {
                                                ItemLowId = streamReader.ReadInt32(),
                                                ItemHighId = streamReader.ReadInt32(),
                                                WeaponInstanceKey = streamReader.ReadInt32(),
                                                SourceKey = streamReader.ReadInt32()
                                            };
                }
            }

            if (flags.HasFlag(SimpleCharFullUpdateFlags.UnknownFlag4))
            {
                scfu.UnknownData4 = streamReader.ReadByte();
            }

            if (flags.HasFlag(SimpleCharFullUpdateFlags.HasCatTextures))
            {
                scfu.CatTextures = new CatTexture[X3F1Count(streamReader.ReadInt32())];
                for (var i = 0; i < scfu.CatTextures.Length; i++)
                {
                    scfu.CatTextures[i] = new CatTexture
                                              {
                                                  CatId = streamReader.ReadInt32(),
                                                  TextureId = streamReader.ReadInt32()
                                              };
                }
            }

            scfu.Flags2 = streamReader.ReadInt32();

            if ((scfu.Flags2 & 1) != 0)
            {
                var update = new CharacterStatUpdate();

                var stats = new ItemStatPair[streamReader.ReadInt32()];
                for (var i = 0; i < stats.Length; i++)
                {
                    stats[i] = new ItemStatPair
                               {
                                   Stat = streamReader.ReadInt32(),
                                   Value = streamReader.ReadInt32()
                               };
                }

                update.Stats = stats;
                update.MechData = streamReader.ReadInt32();
                update.Source = streamReader.ReadIdentity();
                scfu.StatUpdate = update;
            }

            if ((scfu.Flags2 & 2) != 0)
            {
                scfu.BattlestationSide = streamReader.ReadByte();
            }

            if ((scfu.Flags2 & 4) != 0)
            {
                scfu.PetMaster = streamReader.ReadInt32();
            }

            scfu.Unknown2 = streamReader.ReadByte();

            return scfu;
        }

        /// <summary>
        /// Turns an X3F1 encoded array header into an element count.
        /// </summary>
        /// <remarks>
        /// The wire form is (count + 1) * 0x3F1, so an empty array is 0x3F1 and
        /// the largest the client will accept is 0x7530 entries.
        ///
        /// This used to return zero for anything that was not a multiple of
        /// 0x3F1, and that is what kept the active-nano entry being one int32
        /// short from ever being noticed: the field past the end of the entry
        /// was read as this header, was not a multiple, and produced an empty
        /// array rather than a complaint. Everything after it was then read at
        /// the wrong offset, and the only symptom was a packet that came out a
        /// different length.
        ///
        /// So it throws now. Nothing in the running server reaches this - the
        /// zone builds these messages and never reads one, and the client has no
        /// writer for it - so the only callers are WireAudit and the tests,
        /// which is exactly where a misread should be loud.
        /// </remarks>
        private static int X3F1Count(int value)
        {
            if (value <= 0 || value % 0x3F1 != 0 || (value / 0x3F1) - 1 > 0x7530)
            {
                throw new InvalidOperationException(
                    string.Format(
                        "{0} is not an X3F1 array header, so this read is already at the wrong offset",
                        value));
            }

            return (value / 0x3F1) - 1;
        }

        public Expression DeserializerExpression(
            ParameterExpression streamReaderExpression,
            ParameterExpression serializationContextExpression,
            Expression assignmentTargetExpression,
            PropertyMetaData propertyMetaData)
        {
            var deserializerMethodInfo =
                ReflectionHelper
                    .GetMethodInfo
                    <SimpleCharFullUpdateSerializer, Func<StreamReader, SerializationContext, PropertyMetaData, object>>
                    (o => o.Deserialize);
            var serializerExp = Expression.New(this.GetType());
            var callExp = Expression.Call(
                serializerExp,
                deserializerMethodInfo,
                new Expression[]
                    {
                        streamReaderExpression, serializationContextExpression,
                        Expression.Constant(propertyMetaData, typeof(PropertyMetaData))
                    });

            var assignmentExp = Expression.Assign(
                assignmentTargetExpression, Expression.TypeAs(callExp, assignmentTargetExpression.Type));
            return assignmentExp;
        }

        /// <summary>
        /// Every flag the writer works out for itself from the message body.
        /// </summary>
        /// <remarks>
        /// It has to be the exact set of bits Serialize actually assigns, so
        /// that everything outside it can be carried through untouched. If a
        /// flag is added to Serialize it belongs here too, and the round trip in
        /// WireAudit will say so if it is forgotten.
        ///
        /// Listing a flag here that nothing assigns is worse than not listing
        /// it: it would claim the writer had an opinion, and suppress the
        /// preservation that is the only thing keeping the bit alive. The four
        /// left out - UnknownFlag2, IsPet, UnknownFlag5 and the low
        /// AccompaniesHeadMesh companion aside - gate nothing the reader looks
        /// at, so there is nothing for the writer to decide from.
        /// </remarks>
        /// <summary>
        /// The four flags that say how wide a number was written, rather than
        /// whether it is there at all. See <see cref="SimpleCharFullUpdateMessage.WireWidths"/>.
        /// </summary>
        private const SimpleCharFullUpdateFlags Widths =
            SimpleCharFullUpdateFlags.HasExtendedLevel | SimpleCharFullUpdateFlags.HasSmallHealth
            | SimpleCharFullUpdateFlags.HasSmallHealthDamage
            | SimpleCharFullUpdateFlags.HasExtendedRunSpeed;

        /// <summary>
        /// True when the message came off the wire with this width bit set.
        /// </summary>
        private static bool CameWith(SimpleCharFullUpdateMessage scfu, SimpleCharFullUpdateFlags flag)
        {
            return scfu.WireWidths.HasValue && (scfu.WireWidths.Value & flag) != 0;
        }

        /// <summary>
        /// True when the message came off the wire with this width bit clear.
        /// Not the negation of <see cref="CameWith"/>: a message that was never
        /// read is neither.
        /// </summary>
        private static bool CameWithout(SimpleCharFullUpdateMessage scfu, SimpleCharFullUpdateFlags flag)
        {
            return scfu.WireWidths.HasValue && (scfu.WireWidths.Value & flag) == 0;
        }

        private const SimpleCharFullUpdateFlags Decided =
            SimpleCharFullUpdateFlags.HasPlayfieldId | SimpleCharFullUpdateFlags.HasParentDynel
            | SimpleCharFullUpdateFlags.HasHeading | SimpleCharFullUpdateFlags.IsNpc
            | SimpleCharFullUpdateFlags.HasSmallNpcFamily | SimpleCharFullUpdateFlags.HasSmallNpcLosHeight
            | SimpleCharFullUpdateFlags.UnknownDataFlag | SimpleCharFullUpdateFlags.HasOrgName | SimpleCharFullUpdateFlags.HasExtendedLevel
            | SimpleCharFullUpdateFlags.HasSmallHealth | SimpleCharFullUpdateFlags.HasSmallHealthDamage
            | SimpleCharFullUpdateFlags.HasHeadMesh
            | SimpleCharFullUpdateFlags.HasExtendedRunSpeed | SimpleCharFullUpdateFlags.IsUnderAttack
            | SimpleCharFullUpdateFlags.HasExtendedTextures | SimpleCharFullUpdateFlags.IsImmune
            | SimpleCharFullUpdateFlags.UnknownFlag3 | SimpleCharFullUpdateFlags.HasWaypoints
            | SimpleCharFullUpdateFlags.HasNoWeaponPairs | SimpleCharFullUpdateFlags.UnknownFlag4
            | SimpleCharFullUpdateFlags.HasCatTextures;

        public void Serialize(
            StreamWriter streamWriter,
            SerializationContext serializationContext,
            object value,
            PropertyMetaData propertyMetaData = null)
        {
            var scfu = (SimpleCharFullUpdateMessage)value;

            // N3Message
            streamWriter.WriteInt32((int)scfu.N3MessageType);
            streamWriter.WriteInt32((int)scfu.Identity.Type);
            streamWriter.WriteInt32(scfu.Identity.Instance);
            streamWriter.WriteByte(scfu.Unknown);

            // SCFU
            streamWriter.WriteByte(scfu.Version);
            streamWriter.WriteInt32((int)scfu.Flags); // Will update the flags later

            // Whatever this bit means, the writer cannot work it out. The live
            // server sets it on 3,505 of 3,505 captured NPCs and 414 of 433
            // players, so it is very nearly universal and is not a property of
            // being an NPC - it used to be set inside the NPC branch, which left
            // every player update we sent one bit short of what retail sends.
            // But nineteen captured characters go without it, and forcing it
            // here made those nineteen impossible to reproduce.
            //
            // So it is preserved rather than decided, like the other bits the
            // writer has no rule for, and the zone server sets it on the
            // messages it builds. See SimpleCharFullUpdate in ZoneEngine.
            var flags = SimpleCharFullUpdateFlags.None;

            if (scfu.PlayfieldId.HasValue)
            {
                flags |= SimpleCharFullUpdateFlags.HasPlayfieldId;
                streamWriter.WriteInt32(scfu.PlayfieldId.Value);
            }

            if (scfu.ParentDynel.HasValue)
            {
                flags |= SimpleCharFullUpdateFlags.HasParentDynel;
                streamWriter.WriteInt32((int)scfu.ParentDynel.Value.Type);
                streamWriter.WriteInt32(scfu.ParentDynel.Value.Instance);
            }

            streamWriter.WriteSingle(scfu.Coordinates.X);
            streamWriter.WriteSingle(scfu.Coordinates.Y);
            streamWriter.WriteSingle(scfu.Coordinates.Z);

            if (scfu.Heading != null)
            {
                flags |= SimpleCharFullUpdateFlags.HasHeading;
                streamWriter.WriteSingle(scfu.Heading.X);
                streamWriter.WriteSingle(scfu.Heading.Y);
                streamWriter.WriteSingle(scfu.Heading.Z);
                streamWriter.WriteSingle(scfu.Heading.W);
            }

            streamWriter.WriteUInt32(scfu.Appearance.Value);

            streamWriter.WriteByte((byte)(scfu.Name.Length + 1));
            streamWriter.WriteString(scfu.Name, scfu.Name.Length + 1);

            streamWriter.WriteInt32((int)scfu.CharacterFlags);
            streamWriter.WriteInt16(scfu.AccountFlags);
            streamWriter.WriteInt16(scfu.Expansions);

            var snpc = scfu.CharacterInfo as SimpleNpcInfo;
            if (snpc != null)
            {
                flags |= SimpleCharFullUpdateFlags.IsNpc;
                if (snpc.Family > byte.MaxValue)
                {
                    streamWriter.WriteInt16(snpc.Family);
                }
                else
                {
                    flags |= SimpleCharFullUpdateFlags.HasSmallNpcFamily;
                    streamWriter.WriteByte((byte)snpc.Family);
                }

                if (snpc.LosHeight > byte.MaxValue)
                {
                    streamWriter.WriteInt16(snpc.LosHeight);
                }
                else
                {
                    flags |= SimpleCharFullUpdateFlags.HasSmallNpcLosHeight;
                    streamWriter.WriteByte((byte)snpc.LosHeight);
                }

                // UnknownDataFlag set means the byte form, clear means the short.
                flags |= SimpleCharFullUpdateFlags.UnknownDataFlag;
                streamWriter.WriteByte((byte)snpc.PetType);

                streamWriter.WriteInt16(snpc.Unknown2);

                if (snpc.Unknown2 > 0)
                {
                    streamWriter.WriteByte(snpc.Unknown3.GetValueOrDefault());
                }

                // UnknownFlag2 is not asserted here, and used to be.
                //
                // The live server sets it on 49% of NPCs and 38% of players and
                // it lines up with nothing: not extended textures, not
                // waypoints, not either of them together, not any combination
                // of the other flags. 6,158 captured character updates and no
                // rule. Setting it for every NPC was therefore wrong about half
                // the time, and it is the single largest cause of this message
                // failing to round trip.
                //
                // So it is left to whatever the message was given, which is
                // preserved with the rest of the undecided bits.
            }

            var spc = scfu.CharacterInfo as SimplePcInfo;
            if (spc != null)
            {
                streamWriter.WriteUInt32(spc.CurrentNano);
                streamWriter.WriteInt32(spc.Team);
                streamWriter.WriteInt16(spc.Swim);

                streamWriter.WriteInt16(spc.StrengthBase);
                streamWriter.WriteInt16(spc.AgilityBase);
                streamWriter.WriteInt16(spc.StaminaBase);
                streamWriter.WriteInt16(spc.IntelligenceBase);
                streamWriter.WriteInt16(spc.SenseBase);
                streamWriter.WriteInt16(spc.PsychicBase);

                if (scfu.CharacterFlags.HasFlag(CharacterFlags.HasVisibleName))
                {
                    streamWriter.WriteInt16((short)spc.FirstName.Length);
                    streamWriter.WriteString(spc.FirstName);
                    streamWriter.WriteInt16((short)spc.LastName.Length);
                    streamWriter.WriteString(spc.LastName);
                }

                // Null and empty are different here. Deserialize leaves OrgName
                // null when the flag was clear and sets it when the flag was
                // set, so null is the only honest test - an org name that is
                // genuinely empty still carries the flag on the wire, and
                // deciding by emptiness dropped the bit.
                if (spc.OrgName != null)
                {
                    flags |= SimpleCharFullUpdateFlags.HasOrgName;

                    if (scfu.Version > 0x39)
                    {
                        streamWriter.WriteInt32(spc.OrgId.GetValueOrDefault());
                    }

                    streamWriter.WriteInt16((short)spc.OrgName.Length);
                    streamWriter.WriteString(spc.OrgName);
                }
            }

            // byte.MaxValue and not sbyte.MaxValue. The small form is read
            // with ReadByte, so anything up to 255 fits in it, and testing
            // against 127 sent a short for every level from 128 up that retail
            // sends as one byte.
            if (scfu.Level > byte.MaxValue || scfu.Level < 0
                || CameWith(scfu, SimpleCharFullUpdateFlags.HasExtendedLevel))
            {
                flags |= SimpleCharFullUpdateFlags.HasExtendedLevel;
                streamWriter.WriteInt16(scfu.Level);
            }
            else
            {
                streamWriter.WriteByte((byte)scfu.Level);
            }

            // ushort.MaxValue, because the small form is read with ReadUInt16.
            // Testing against short.MaxValue sent a full int32 for every health
            // between 32,768 and 65,535 that retail sends as two bytes - a pet
            // in one capture has 43,140 of them.
            if (scfu.Health >= 0 && scfu.Health <= ushort.MaxValue
                && !CameWithout(scfu, SimpleCharFullUpdateFlags.HasSmallHealth))
            {
                flags |= SimpleCharFullUpdateFlags.HasSmallHealth;
                streamWriter.WriteUInt16((ushort)scfu.Health);
            }
            else
            {
                streamWriter.WriteInt32(scfu.Health);
            }

            if (scfu.HealthDamage <= byte.MaxValue
                && !CameWithout(scfu, SimpleCharFullUpdateFlags.HasSmallHealthDamage))
            {
                flags |= SimpleCharFullUpdateFlags.HasSmallHealthDamage;
                streamWriter.WriteByte((byte)scfu.HealthDamage);
            }
            else
            {
                if (flags.HasFlag(SimpleCharFullUpdateFlags.HasSmallHealth))
                {
                    streamWriter.WriteUInt16((ushort)scfu.HealthDamage);
                }
                else
                {
                    streamWriter.WriteInt32(scfu.HealthDamage);
                }
            }

            streamWriter.WriteUInt32(scfu.MonsterData);
            streamWriter.WriteInt16(scfu.MonsterScale);
            streamWriter.WriteInt16(scfu.VisualFlags);
            streamWriter.WriteByte(scfu.VisibleTitle);

            streamWriter.WriteInt32(scfu.VehicleData.Length);
            streamWriter.WriteBytes(scfu.VehicleData);

            if (scfu.HeadMesh.HasValue)
            {
                // HasHeadMesh only. The low bit next to it is not a companion
                // to this one - see AccompaniesHeadMesh - and forcing it here
                // overwrote what the wire said in 944 captured updates.
                flags |= SimpleCharFullUpdateFlags.HasHeadMesh;
                streamWriter.WriteUInt32(scfu.HeadMesh.Value);
            }

            // byte.MaxValue, for the same reason as Level above.
            if (scfu.RunSpeedBase > byte.MaxValue || scfu.RunSpeedBase < 0
                || CameWith(scfu, SimpleCharFullUpdateFlags.HasExtendedRunSpeed))
            {
                flags |= SimpleCharFullUpdateFlags.HasExtendedRunSpeed;
                streamWriter.WriteInt16(scfu.RunSpeedBase);
            }
            else
            {
                streamWriter.WriteByte((byte)scfu.RunSpeedBase);
            }

            if (scfu.FightingTarget.HasValue)
            {
                flags |= SimpleCharFullUpdateFlags.IsUnderAttack;
                streamWriter.WriteInt32((int)scfu.FightingTarget.Value.Type);
                streamWriter.WriteInt32(scfu.FightingTarget.Value.Instance);
            }

            if (scfu.ExtendedTextures != null)
            {
                flags |= SimpleCharFullUpdateFlags.HasExtendedTextures;
                streamWriter.WriteInt32((scfu.ExtendedTextures.Length + 1) * 0x3F1);
                foreach (var texture in scfu.ExtendedTextures)
                {
                    streamWriter.WriteBytes(texture.Name);
                    streamWriter.WriteInt32(texture.TextureId);
                    streamWriter.WriteInt32(texture.OverlayId);
                    streamWriter.WriteInt32(texture.AlphaMode);
                }
            }

            if (scfu.ImmuneData.HasValue)
            {
                flags |= SimpleCharFullUpdateFlags.IsImmune;
                streamWriter.WriteByte(scfu.ImmuneData.Value);
            }

            if (scfu.UnknownData3.HasValue)
            {
                flags |= SimpleCharFullUpdateFlags.UnknownFlag3;
                streamWriter.WriteByte(scfu.UnknownData3.Value);
            }

            streamWriter.WriteInt32((scfu.ActiveNanos.Length + 1) * 0x3F1);
            foreach (var activeNano in scfu.ActiveNanos)
            {
                streamWriter.WriteInt32(activeNano.NanoId);
                streamWriter.WriteInt32(activeNano.NanoInstance);
                streamWriter.WriteInt32(activeNano.Unknown);
                streamWriter.WriteInt32(activeNano.Time1);
                streamWriter.WriteInt32(activeNano.Time2);
            }

            if (scfu.Waypoints != null)
            {
                flags |= SimpleCharFullUpdateFlags.HasWaypoints;
                streamWriter.WriteInt32((int)scfu.Waypoints.Target.Type);
                streamWriter.WriteInt32(scfu.Waypoints.Target.Instance);
                streamWriter.WriteInt32(scfu.Waypoints.Points.Length);
                foreach (var point in scfu.Waypoints.Points)
                {
                    streamWriter.WriteSingle(point.X);
                    streamWriter.WriteSingle(point.Y);
                    streamWriter.WriteSingle(point.Z);
                }
            }

            streamWriter.WriteInt32((scfu.Textures.Length + 1) * 0x3F1);
            foreach (var texture in scfu.Textures)
            {
                streamWriter.WriteInt32(texture.Place);
                streamWriter.WriteInt32(texture.Id);
                streamWriter.WriteInt32(texture.Group);

                if (texture.HasExtra)
                {
                    streamWriter.WriteInt32(texture.OverlayId.GetValueOrDefault());
                    streamWriter.WriteInt32(texture.AlphaMode.GetValueOrDefault());
                }
            }

            streamWriter.WriteInt32((scfu.Meshes.Length + 1) * 0x3F1);
            foreach (var mesh in scfu.Meshes)
            {
                streamWriter.WriteByte(mesh.Position);
                streamWriter.WriteUInt32(mesh.Id);
                streamWriter.WriteInt32(mesh.OverrideTextureId);
                streamWriter.WriteByte(mesh.Layer);
            }

            if (scfu.NoWeaponPairs != null)
            {
                flags |= SimpleCharFullUpdateFlags.HasNoWeaponPairs;
                streamWriter.WriteInt32((scfu.NoWeaponPairs.Length + 1) * 0x3F1);
                foreach (var pair in scfu.NoWeaponPairs)
                {
                    streamWriter.WriteInt32(pair.ItemLowId);
                    streamWriter.WriteInt32(pair.ItemHighId);
                    streamWriter.WriteInt32(pair.WeaponInstanceKey);
                    streamWriter.WriteInt32(pair.SourceKey);
                }
            }

            if (scfu.UnknownData4.HasValue)
            {
                flags |= SimpleCharFullUpdateFlags.UnknownFlag4;
                streamWriter.WriteByte(scfu.UnknownData4.Value);
            }

            if (scfu.CatTextures != null)
            {
                flags |= SimpleCharFullUpdateFlags.HasCatTextures;
                streamWriter.WriteInt32((scfu.CatTextures.Length + 1) * 0x3F1);
                foreach (var texture in scfu.CatTextures)
                {
                    streamWriter.WriteInt32(texture.CatId);
                    streamWriter.WriteInt32(texture.TextureId);
                }
            }

            streamWriter.WriteInt32(scfu.Flags2);

            if ((scfu.Flags2 & 1) != 0)
            {
                var stats = scfu.StatUpdate != null && scfu.StatUpdate.Stats != null
                                ? scfu.StatUpdate.Stats
                                : new ItemStatPair[0];

                streamWriter.WriteInt32(stats.Length);
                foreach (var stat in stats)
                {
                    streamWriter.WriteInt32(stat.Stat);
                    streamWriter.WriteInt32(stat.Value);
                }

                var source = scfu.StatUpdate != null ? scfu.StatUpdate.Source : default(Identity);
                streamWriter.WriteInt32(scfu.StatUpdate != null ? scfu.StatUpdate.MechData : 0);
                streamWriter.WriteInt32((int)source.Type);
                streamWriter.WriteInt32(source.Instance);
            }

            if ((scfu.Flags2 & 2) != 0)
            {
                streamWriter.WriteByte(scfu.BattlestationSide.GetValueOrDefault());
            }

            if ((scfu.Flags2 & 4) != 0)
            {
                streamWriter.WriteInt32(scfu.PetMaster.GetValueOrDefault());
            }

            streamWriter.WriteByte(scfu.Unknown2);

            // Anything this method did not decide is carried through rather
            // than dropped.
            //
            // The flag word is rebuilt from scratch here, which is right for
            // every bit whose condition is written a few lines above it - a bit
            // that contradicts the bytes beside it is worse than no bit at all.
            // It is wrong for the rest. The live server sets bits this
            // serializer has no opinion on, and rebuilding the word silently
            // discarded them: a captured character update could be read in and
            // written back out with two bits missing, which is how this was
            // found.
            //
            // So the bits above are authoritative and the remainder is
            // preserved. A message built from nothing has no remainder to
            // preserve and is unaffected.
            flags |= scfu.Flags & ~Decided;

            var pos = streamWriter.Position;
            streamWriter.Position = 30;
            streamWriter.WriteInt32((int)flags);
            streamWriter.Position = pos;
        }

        public Expression SerializerExpression(
            ParameterExpression streamWriterExpression,
            ParameterExpression serializationContextExpression,
            Expression valueExpression,
            PropertyMetaData propertyMetaData)
        {
            var serializerMethodInfo =
                ReflectionHelper
                    .GetMethodInfo
                    <SimpleCharFullUpdateSerializer,
                        Action<StreamWriter, SerializationContext, object, PropertyMetaData>>(o => o.Serialize);
            var serializerExp = Expression.New(this.GetType());
            var callExp = Expression.Call(
                serializerExp,
                serializerMethodInfo,
                new[]
                    {
                        streamWriterExpression, serializationContextExpression, valueExpression,
                        Expression.Constant(propertyMetaData, typeof(PropertyMetaData))
                    });
            return callExp;
        }

        #endregion
    }
}
