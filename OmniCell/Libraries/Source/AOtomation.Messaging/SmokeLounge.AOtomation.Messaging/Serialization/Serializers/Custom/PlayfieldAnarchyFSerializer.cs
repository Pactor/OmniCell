// --------------------------------------------------------------------------------------------------------------------
// <copyright file="PlayfieldAnarchyFSerializer.cs" company="OmniCell">
//   Copyright © 2026 OmniCell contributors.
//   Added to SmokeLounge.AOtomation.Messaging, which is distributed under the
//   Do What The Fuck You Want To Public License, Version 2, as published by
//   Sam Hocevar. See http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the PlayfieldAnarchyFSerializer type.
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
    /// Reads and writes PlayfieldAnarchyF.
    /// </summary>
    /// <remarks>
    /// Written by hand because the middle of this message is an object that
    /// decides its own length, and the attribute serializer has no way to say
    /// that.
    ///
    /// The shape comes from three readers. This message's own, at Gamecode.dll
    /// 0x101258E3, calls its base and then reads two int32s - so PlayfieldX and
    /// PlayfieldZ close the message and everything else belongs to the base.
    /// The base, n3PlayfieldFullUpdateIIR_t::ReadSubClass at N3.dll 0x10029C24,
    /// reads a version and three floats, then a token when the version is above
    /// 1 and a DbObject when it is above 3. The token reader at N3.dll
    /// 0x10038402 wants a 0x61 marker and then an Identity, two int32s and
    /// another Identity - twenty five bytes.
    ///
    /// The DbObject is the part that matters. The base peeks an Identity and
    /// stops there if it is empty, which is every static playfield; otherwise it
    /// rewinds and lets the object read its own body. For identity type 51103
    /// DbObject_t::CreateObject builds an ACGBuildingGeneratorData_t, and that
    /// is a mission - every mission is a generated playfield, so every mission
    /// carries one and no capture of a static playfield ever will.
    /// </remarks>
    public class PlayfieldAnarchyFSerializer : ISerializer
    {
        #region Constants

        /// <summary>
        /// The marker the token reader at N3.dll 0x1003841E insists on.
        /// </summary>
        private const byte TokenMarker = 0x61;

        /// <summary>
        /// ACGBuildingGeneratorData_t - a mission.
        /// </summary>
        private const int BuildingGenerator = 51103;

        /// <summary>
        /// TemplatePlayfieldGeneratorData_t - a playfield like Arete Landing.
        /// </summary>
        private const int TemplateGenerator = 51069;

        #endregion

        #region Fields

        private readonly Type type;

        #endregion

        #region Constructors and Destructors

        public PlayfieldAnarchyFSerializer()
        {
            this.type = typeof(PlayfieldAnarchyFMessage);
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
            var message = new PlayfieldAnarchyFMessage();

            message.N3MessageType = (N3MessageType)streamReader.ReadInt32();
            message.Identity = streamReader.ReadIdentity();
            message.Unknown = streamReader.ReadByte();

            ReadBody(streamReader, message);

            message.PlayfieldX = streamReader.ReadInt32();
            message.PlayfieldZ = streamReader.ReadInt32();

            return message;
        }

        public void Serialize(
            StreamWriter streamWriter,
            SerializationContext serializationContext,
            object value,
            PropertyMetaData propertyMetaData = null)
        {
            var message = (PlayfieldAnarchyFMessage)value;

            streamWriter.WriteInt32((int)message.N3MessageType);
            streamWriter.WriteInt32((int)message.Identity.Type);
            streamWriter.WriteInt32(message.Identity.Instance);
            streamWriter.WriteByte(message.Unknown);

            WriteBody(streamWriter, message);

            streamWriter.WriteInt32(message.PlayfieldX);
            streamWriter.WriteInt32(message.PlayfieldZ);
        }

        #endregion

        #region Methods

        /// <summary>
        /// n3PlayfieldFullUpdateIIR_t::ReadSubClass, N3.dll 0x10029C24.
        /// </summary>
        /// <remarks>
        /// Shared, because two messages carry it: this one with two int32s
        /// after it, and N3PlayfieldFullUpdate with nothing after it.
        /// </remarks>
        internal static void ReadBody(StreamReader streamReader, IPlayfieldFullUpdate message)
        {
            message.Version = streamReader.ReadInt32();
            message.CharacterCoordinates = new Vector3
                                           {
                                               X = streamReader.ReadSingle(),
                                               Y = streamReader.ReadSingle(),
                                               Z = streamReader.ReadSingle()
                                           };

            if (message.Version > 1)
            {
                message.TokenMarker = streamReader.ReadByte();
                message.ModelId = streamReader.ReadIdentity();
                message.Group = streamReader.ReadInt32();
                message.Subgroup = streamReader.ReadInt32();
                message.PlayfieldId = streamReader.ReadIdentity();
            }

            if (message.Version > 3)
            {
                // The base peeks the identity and only builds an object when it
                // is not empty; an empty one is eight bytes and the end of it.
                Identity peeked = streamReader.ReadIdentity();
                if (peeked.Type != 0 || peeked.Instance != 0)
                {
                    int revision = streamReader.ReadInt32();
                    if ((int)peeked.Type == BuildingGenerator)
                    {
                        message.Generator = ReadBuildingGenerator(streamReader, peeked, revision);
                    }
                    else if ((int)peeked.Type == TemplateGenerator)
                    {
                        message.TemplateGenerator = ReadTemplateGenerator(streamReader, peeked, revision);
                    }
                    else
                    {
                        throw new InvalidOperationException(
                            string.Format(
                                "identity type {0} is a playfield generator this reader does not know, "
                                + "so the rest of the message cannot be read",
                                (int)peeked.Type));
                    }
                }
            }
        }

        /// <summary>
        /// The same fields back out, in the same order.
        /// </summary>
        internal static void WriteBody(StreamWriter streamWriter, IPlayfieldFullUpdate message)
        {
            streamWriter.WriteInt32(message.Version);
            streamWriter.WriteSingle(message.CharacterCoordinates.X);
            streamWriter.WriteSingle(message.CharacterCoordinates.Y);
            streamWriter.WriteSingle(message.CharacterCoordinates.Z);

            if (message.Version > 1)
            {
                streamWriter.WriteByte(message.TokenMarker);
                streamWriter.WriteIdentity(message.ModelId);
                streamWriter.WriteInt32(message.Group);
                streamWriter.WriteInt32(message.Subgroup);
                streamWriter.WriteIdentity(message.PlayfieldId);
            }

            if (message.Version > 3)
            {
                if (message.Generator != null)
                {
                    WriteBuildingGenerator(streamWriter, message.Generator);
                }
                else if (message.TemplateGenerator != null)
                {
                    WriteTemplateGenerator(streamWriter, message.TemplateGenerator);
                }
                else
                {
                    // An empty identity, which is where the client stops.
                    streamWriter.WriteInt32(0);
                    streamWriter.WriteInt32(0);
                }
            }
        }

        /// <summary>
        /// ACGBuildingGeneratorData_t::ReadBlob, Gamecode.dll 0x100C773E.
        /// </summary>
        /// <remarks>
        /// The identity has already been read once by the peek. The client
        /// rewinds and reads it again through DbObject_t::ReadBlob, so it is
        /// passed in rather than read twice here.
        /// </remarks>
        private static BuildingGeneratorData ReadBuildingGenerator(
            StreamReader streamReader,
            Identity identity,
            int revision)
        {
            var data = new BuildingGeneratorData();
            data.Identity = identity;
            data.Revision = revision;

            data.Version = streamReader.ReadInt16();
            data.Width = streamReader.ReadInt16();
            data.Height = streamReader.ReadInt16();
            data.WorldHeight = streamReader.ReadInt16();
            data.TemplatePlayfield = streamReader.ReadInt32();

            data.AmbientRed = streamReader.ReadByte();
            data.AmbientGreen = streamReader.ReadByte();
            data.AmbientBlue = streamReader.ReadByte();

            var rooms = new BuildingRoomInfo[streamReader.ReadInt32()];
            for (var i = 0; i < rooms.Length; i++)
            {
                rooms[i] = new BuildingRoomInfo
                           {
                               Room = streamReader.ReadInt16(),
                               Floor = (sbyte)streamReader.ReadByte(),
                               X = streamReader.ReadByte(),
                               Z = streamReader.ReadByte(),
                               Rotation = streamReader.ReadByte()
                           };
            }

            data.Rooms = rooms;
            return data;
        }

        /// <summary>
        /// TemplatePlayfieldGeneratorData_t::ReadBlob, Gamecode.dll 0x10124DC4.
        /// </summary>
        private static PlayfieldTemplateGeneratorData ReadTemplateGenerator(
            StreamReader streamReader,
            Identity identity,
            int revision)
        {
            var data = new PlayfieldTemplateGeneratorData();
            data.Identity = identity;
            data.Revision = revision;
            data.Version = streamReader.ReadInt32();

            var runs = new PlayfieldDynelRun[streamReader.ReadInt32()];
            for (var i = 0; i < runs.Length; i++)
            {
                runs[i] = new PlayfieldDynelRun
                          {
                              Type = (IdentityType)streamReader.ReadInt32(),
                              Unknown = streamReader.ReadInt32(),
                              StartIndex = streamReader.ReadInt32(),
                              Count = streamReader.ReadInt32(),
                              FirstInstance = streamReader.ReadInt32()
                          };
            }

            data.Runs = runs;
            return data;
        }

        private static void WriteTemplateGenerator(StreamWriter streamWriter, PlayfieldTemplateGeneratorData data)
        {
            streamWriter.WriteIdentity(data.Identity);
            streamWriter.WriteInt32(data.Revision);
            streamWriter.WriteInt32(data.Version);

            var runs = data.Runs ?? new PlayfieldDynelRun[0];
            streamWriter.WriteInt32(runs.Length);
            foreach (var run in runs)
            {
                streamWriter.WriteInt32((int)run.Type);
                streamWriter.WriteInt32(run.Unknown);
                streamWriter.WriteInt32(run.StartIndex);
                streamWriter.WriteInt32(run.Count);
                streamWriter.WriteInt32(run.FirstInstance);
            }
        }

        private static void WriteBuildingGenerator(StreamWriter streamWriter, BuildingGeneratorData data)
        {
            streamWriter.WriteIdentity(data.Identity);
            streamWriter.WriteInt32(data.Revision);

            streamWriter.WriteInt16(data.Version);
            streamWriter.WriteInt16(data.Width);
            streamWriter.WriteInt16(data.Height);
            streamWriter.WriteInt16(data.WorldHeight);
            streamWriter.WriteInt32(data.TemplatePlayfield);

            streamWriter.WriteByte(data.AmbientRed);
            streamWriter.WriteByte(data.AmbientGreen);
            streamWriter.WriteByte(data.AmbientBlue);

            var rooms = data.Rooms ?? new BuildingRoomInfo[0];
            streamWriter.WriteInt32(rooms.Length);
            foreach (var room in rooms)
            {
                streamWriter.WriteInt16(room.Room);
                streamWriter.WriteByte((byte)room.Floor);
                streamWriter.WriteByte(room.X);
                streamWriter.WriteByte(room.Z);
                streamWriter.WriteByte(room.Rotation);
            }
        }

        #endregion

        #region Expression plumbing

        public Expression DeserializerExpression(
            ParameterExpression streamReaderExpression,
            ParameterExpression serializationContextExpression,
            Expression assignmentTargetExpression,
            PropertyMetaData propertyMetaData)
        {
            var deserializerMethodInfo =
                ReflectionHelper
                    .GetMethodInfo
                    <PlayfieldAnarchyFSerializer,
                        Func<StreamReader, SerializationContext, PropertyMetaData, object>>(o => o.Deserialize);
            var callExp = Expression.Call(
                Expression.New(this.GetType()),
                deserializerMethodInfo,
                new Expression[]
                    {
                        streamReaderExpression, serializationContextExpression,
                        Expression.Constant(propertyMetaData, typeof(PropertyMetaData))
                    });

            return Expression.Assign(
                assignmentTargetExpression, Expression.TypeAs(callExp, assignmentTargetExpression.Type));
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
                    <PlayfieldAnarchyFSerializer,
                        Action<StreamWriter, SerializationContext, object, PropertyMetaData>>(o => o.Serialize);
            return Expression.Call(
                Expression.New(this.GetType()),
                serializerMethodInfo,
                new[]
                    {
                        streamWriterExpression, serializationContextExpression, valueExpression,
                        Expression.Constant(propertyMetaData, typeof(PropertyMetaData))
                    });
        }

        #endregion
    }
}
