// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SpellListSerializer.cs" company="OmniCell">
//   Copyright © 2026 OmniCell contributors.
//   Added to SmokeLounge.AOtomation.Messaging, which is distributed under the
//   Do What The Fuck You Want To Public License, Version 2, as published by
//   Sam Hocevar. See http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the SpellListSerializer type.
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
    /// Reads and writes SpellList.
    /// </summary>
    /// <remarks>
    /// Written by hand because an effect's argument block has no length in front
    /// of it: how many bytes to read depends on which game function the effect
    /// is, and that is a lookup the attribute serializer cannot express. The
    /// lookup itself is in NanoEffects, with the rest of the record - this
    /// message is not the only one that carries it.
    ///
    /// Verified across all 1685 captured SpellLists, single and multiple effect,
    /// by deserializing and serializing back and comparing bytes.
    /// </remarks>
    public class SpellListSerializer : ISerializer
    {
        #region Fields

        private readonly Type type;

        #endregion

        #region Constructors and Destructors

        public SpellListSerializer()
        {
            this.type = typeof(SpellListMessage);
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
            var spellList = new SpellListMessage();

            spellList.N3MessageType = (N3MessageType)streamReader.ReadInt32();
            spellList.Identity = streamReader.ReadIdentity();
            spellList.Unknown = streamReader.ReadByte();

            spellList.NanoEffects = NanoEffects.ReadList(streamReader);

            spellList.Source = streamReader.ReadIdentity();
            spellList.Character = streamReader.ReadIdentity();

            // A byte, and then a two byte string length - not a short followed
            // by a one byte length, which is what this read for years. The two
            // are the same three bytes on the wire as long as no name reaches
            // 256 characters, so it round-tripped perfectly while being wrong
            // about all three of them. Gamecode.dll 0x100AAC28 reads the byte;
            // 0x100AAC43 is the short-counted string reader.
            spellList.SetSpellFlag = streamReader.ReadByte();
            spellList.Name = streamReader.ReadString(streamReader.ReadInt16());

            spellList.HasNano = streamReader.ReadByte();
            spellList.Nano = spellList.HasNano == 0 ? Identity.None : streamReader.ReadIdentity();

            // Byte first, then the int. The other way round for years, for the
            // same reason: writing them back in the wrong order reproduces the
            // bytes exactly and puts the wrong values in both fields.
            spellList.UnreadFlag = streamReader.ReadByte();
            spellList.ApplyScope = streamReader.ReadInt32();

            return spellList;
        }

        public void Serialize(
            StreamWriter streamWriter,
            SerializationContext serializationContext,
            object value,
            PropertyMetaData propertyMetaData = null)
        {
            var spellList = (SpellListMessage)value;

            streamWriter.WriteInt32((int)spellList.N3MessageType);
            streamWriter.WriteInt32((int)spellList.Identity.Type);
            streamWriter.WriteInt32(spellList.Identity.Instance);
            streamWriter.WriteByte(spellList.Unknown);

            NanoEffects.WriteList(streamWriter, spellList.NanoEffects);

            streamWriter.WriteIdentity(spellList.Source);
            streamWriter.WriteIdentity(spellList.Character);

            streamWriter.WriteByte(spellList.SetSpellFlag);

            string name = spellList.Name ?? string.Empty;
            streamWriter.WriteInt16((short)name.Length);
            streamWriter.WriteString(name, name.Length);

            streamWriter.WriteByte(spellList.HasNano);
            if (spellList.HasNano != 0)
            {
                streamWriter.WriteIdentity(spellList.Nano);
            }

            streamWriter.WriteByte(spellList.UnreadFlag);
            streamWriter.WriteInt32(spellList.ApplyScope);
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
                    <SpellListSerializer, Func<StreamReader, SerializationContext, PropertyMetaData, object>>
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
                    <SpellListSerializer, Action<StreamWriter, SerializationContext, object, PropertyMetaData>>
                    (o => o.Serialize);
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

    }
}
