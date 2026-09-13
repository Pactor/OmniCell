// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CorpseFullUpdateSerializer.cs" company="OmniCell">
//   Copyright © 2026 OmniCell contributors.
//   Added to SmokeLounge.AOtomation.Messaging, which is distributed under the
//   Do What The Fuck You Want To Public License, Version 2, as published by
//   Sam Hocevar. See http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the CorpseFullUpdateSerializer type.
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
    /// Reads and writes a corpse.
    /// </summary>
    /// <remarks>
    /// Written by hand rather than driven from the member attributes because of
    /// the conditional at the end: the mesh array is present only when the int
    /// before it is non-zero, and where it is zero there is no array header
    /// either. That is not something the attribute serializer can express.
    ///
    /// Verified against every corpse in the captures - 154 of them, ten kinds of
    /// creature, ten different lengths - by deserializing and serializing
    /// straight back and comparing bytes.
    /// </remarks>
    public class CorpseFullUpdateSerializer : ISerializer
    {
        #region Fields

        private readonly Type type;

        #endregion

        #region Constructors and Destructors

        public CorpseFullUpdateSerializer()
        {
            this.type = typeof(CorpseFullUpdateMessage);
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
            var corpse = new CorpseFullUpdateMessage();

            corpse.N3MessageType = (N3MessageType)streamReader.ReadInt32();
            corpse.Identity = streamReader.ReadIdentity();
            corpse.Unknown = streamReader.ReadByte();

            corpse.MsgVersion = streamReader.ReadInt32();
            corpse.ItemVersion = streamReader.ReadInt32();
            corpse.HolderType = streamReader.ReadInt32();
            corpse.HolderInstance = streamReader.ReadInt32();

            corpse.Coordinates = new Vector3
                                 {
                                     X = streamReader.ReadSingle(),
                                     Y = streamReader.ReadSingle(),
                                     Z = streamReader.ReadSingle()
                                 };

            corpse.Heading = new Quaternion
                             {
                                 X = streamReader.ReadSingle(),
                                 Y = streamReader.ReadSingle(),
                                 Z = streamReader.ReadSingle(),
                                 W = streamReader.ReadSingle()
                             };

            corpse.PlayfieldId = streamReader.ReadInt32();
            corpse.StateMachineType = streamReader.ReadInt32();
            corpse.StateMachineInstance = streamReader.ReadInt32();
            corpse.InventoryIdAndBodyLocation = streamReader.ReadInt16();

            corpse.Stats = new GameTuple<int, int>[X3F1Count(streamReader.ReadInt32())];
            for (var i = 0; i < corpse.Stats.Length; i++)
            {
                corpse.Stats[i] = new GameTuple<int, int>
                                  {
                                      Value1 = streamReader.ReadInt32(),
                                      Value2 = streamReader.ReadInt32()
                                  };
            }

            corpse.Name = streamReader.ReadString(streamReader.ReadInt32());

            corpse.LockableVersion = streamReader.ReadInt32();
            corpse.LockDifficulty = streamReader.ReadInt32();

            corpse.Keyholders = new Identity[X3F1Count(streamReader.ReadInt32())];
            for (var i = 0; i < corpse.Keyholders.Length; i++)
            {
                corpse.Keyholders[i] = streamReader.ReadIdentity();
            }

            corpse.ChestVersion = streamReader.ReadInt32();

            corpse.NanoEffects = NanoEffects.ReadList(streamReader);

            corpse.Owner = streamReader.ReadIdentity();

            corpse.Textures = new Texture[X3F1Count(streamReader.ReadInt32())];
            for (var i = 0; i < corpse.Textures.Length; i++)
            {
                corpse.Textures[i] = new Texture
                                     {
                                         Place = streamReader.ReadInt32(),
                                         Id = streamReader.ReadInt32(),
                                         Group = streamReader.ReadInt32()
                                     };
            }

            corpse.HasMeshes = streamReader.ReadInt32();

            if (corpse.HasMeshes == 0)
            {
                // No array header follows, the message just ends.
                corpse.Meshes = new CorpseMesh[0];
                return corpse;
            }

            corpse.Meshes = new CorpseMesh[X3F1Count(streamReader.ReadInt32())];
            for (var i = 0; i < corpse.Meshes.Length; i++)
            {
                corpse.Meshes[i] = new CorpseMesh
                                   {
                                       Name = streamReader.ReadString(CorpseMesh.NameLength),
                                       Id = streamReader.ReadInt32(),
                                       OverlayId = streamReader.ReadInt32(),
                                       AlphaMode = streamReader.ReadInt32()
                                   };
            }

            return corpse;
        }

        public void Serialize(
            StreamWriter streamWriter,
            SerializationContext serializationContext,
            object value,
            PropertyMetaData propertyMetaData = null)
        {
            var corpse = (CorpseFullUpdateMessage)value;

            streamWriter.WriteInt32((int)corpse.N3MessageType);
            streamWriter.WriteInt32((int)corpse.Identity.Type);
            streamWriter.WriteInt32(corpse.Identity.Instance);
            streamWriter.WriteByte(corpse.Unknown);

            streamWriter.WriteInt32(corpse.MsgVersion);
            streamWriter.WriteInt32(corpse.ItemVersion);
            streamWriter.WriteInt32(corpse.HolderType);
            streamWriter.WriteInt32(corpse.HolderInstance);

            streamWriter.WriteSingle(corpse.Coordinates.X);
            streamWriter.WriteSingle(corpse.Coordinates.Y);
            streamWriter.WriteSingle(corpse.Coordinates.Z);

            streamWriter.WriteSingle(corpse.Heading.X);
            streamWriter.WriteSingle(corpse.Heading.Y);
            streamWriter.WriteSingle(corpse.Heading.Z);
            streamWriter.WriteSingle(corpse.Heading.W);

            streamWriter.WriteInt32(corpse.PlayfieldId);
            streamWriter.WriteInt32(corpse.StateMachineType);
            streamWriter.WriteInt32(corpse.StateMachineInstance);
            streamWriter.WriteInt16(corpse.InventoryIdAndBodyLocation);

            streamWriter.WriteInt32(X3F1Value(corpse.Stats.Length));
            foreach (GameTuple<int, int> stat in corpse.Stats)
            {
                streamWriter.WriteInt32(stat.Value1);
                streamWriter.WriteInt32(stat.Value2);
            }

            string name = corpse.Name ?? string.Empty;
            streamWriter.WriteInt32(name.Length + 1);
            streamWriter.WriteString(name, name.Length + 1);

            streamWriter.WriteInt32(corpse.LockableVersion);
            streamWriter.WriteInt32(corpse.LockDifficulty);

            streamWriter.WriteInt32(X3F1Value(corpse.Keyholders.Length));
            foreach (Identity entry in corpse.Keyholders)
            {
                streamWriter.WriteInt32((int)entry.Type);
                streamWriter.WriteInt32(entry.Instance);
            }

            streamWriter.WriteInt32(corpse.ChestVersion);

            NanoEffects.WriteList(streamWriter, corpse.NanoEffects);

            streamWriter.WriteInt32((int)corpse.Owner.Type);
            streamWriter.WriteInt32(corpse.Owner.Instance);

            streamWriter.WriteInt32(X3F1Value(corpse.Textures.Length));
            foreach (Texture texture in corpse.Textures)
            {
                streamWriter.WriteInt32(texture.Place);
                streamWriter.WriteInt32(texture.Id);
                streamWriter.WriteInt32(texture.Group);
            }

            streamWriter.WriteInt32(corpse.HasMeshes);

            if (corpse.HasMeshes == 0)
            {
                return;
            }

            streamWriter.WriteInt32(X3F1Value(corpse.Meshes.Length));
            foreach (CorpseMesh mesh in corpse.Meshes)
            {
                streamWriter.WriteString(mesh.Name ?? string.Empty, CorpseMesh.NameLength);
                streamWriter.WriteInt32(mesh.Id);
                streamWriter.WriteInt32(mesh.OverlayId);
                streamWriter.WriteInt32(mesh.AlphaMode);
            }
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
                    <CorpseFullUpdateSerializer, Func<StreamReader, SerializationContext, PropertyMetaData, object>>
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

            return Expression.Assign(
                assignmentTargetExpression,
                Expression.TypeAs(callExp, assignmentTargetExpression.Type));
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
                    <CorpseFullUpdateSerializer,
                        Action<StreamWriter, SerializationContext, object, PropertyMetaData>>(o => o.Serialize);
            var serializerExp = Expression.New(this.GetType());
            return Expression.Call(
                serializerExp,
                serializerMethodInfo,
                new[]
                    {
                        streamWriterExpression, serializationContextExpression, valueExpression,
                        Expression.Constant(propertyMetaData, typeof(PropertyMetaData))
                    });
        }

        #endregion

        #region Methods

        private static int X3F1Count(int value)
        {
            if (value <= 0 || value % 0x3F1 != 0)
            {
                return 0;
            }

            return (value / 0x3F1) - 1;
        }

        private static int X3F1Value(int count)
        {
            return (count + 1) * 0x3F1;
        }

        #endregion
    }
}
