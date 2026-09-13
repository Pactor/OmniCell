// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ApplySpellsSerializer.cs" company="OmniCell">
//   Copyright © 2026 OmniCell contributors.
//   Added to SmokeLounge.AOtomation.Messaging, which is distributed under the
//   Do What The Fuck You Want To Public License, Version 2, as published by
//   Sam Hocevar. See http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the ApplySpellsSerializer type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace SmokeLounge.AOtomation.Messaging.Serialization.Serializers.Custom
{
    using System;
    using System.Linq.Expressions;

    using SmokeLounge.AOtomation.Messaging.Messages;
    using SmokeLounge.AOtomation.Messaging.Messages.N3Messages;

    /// <summary>
    /// Reads and writes ApplySpells.
    /// </summary>
    /// <remarks>
    /// By hand for the same reason SpellList is: the effect list it opens with
    /// has no byte count in front of it, and how far each record runs depends
    /// on which game function the record names. That is a table lookup, and the
    /// attribute serializer has nowhere to put one.
    ///
    /// The order here is the client's, from the writer at Gamecode.dll
    /// 0x10128EEF: the list, then the identity, then the byte.
    /// </remarks>
    public class ApplySpellsSerializer : ISerializer
    {
        #region Fields

        private readonly Type type;

        #endregion

        #region Constructors and Destructors

        public ApplySpellsSerializer()
        {
            this.type = typeof(ApplySpellsMessage);
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
            var applySpells = new ApplySpellsMessage();

            applySpells.N3MessageType = (N3MessageType)streamReader.ReadInt32();
            applySpells.Identity = streamReader.ReadIdentity();
            applySpells.Unknown = streamReader.ReadByte();

            applySpells.NanoEffects = NanoEffects.ReadList(streamReader);
            applySpells.Target = streamReader.ReadIdentity();

            // The client normalizes this the moment it reads it - setne at
            // 0x10128EDB - so any non-zero byte on the wire is the same true to
            // the client, and writing 1 back is what the client itself would do.
            applySpells.Apply = streamReader.ReadByte() != 0;

            return applySpells;
        }

        public void Serialize(
            StreamWriter streamWriter,
            SerializationContext serializationContext,
            object value,
            PropertyMetaData propertyMetaData = null)
        {
            var applySpells = (ApplySpellsMessage)value;

            streamWriter.WriteInt32((int)applySpells.N3MessageType);
            streamWriter.WriteIdentity(applySpells.Identity);
            streamWriter.WriteByte(applySpells.Unknown);

            NanoEffects.WriteList(streamWriter, applySpells.NanoEffects);
            streamWriter.WriteIdentity(applySpells.Target);
            streamWriter.WriteByte(applySpells.Apply ? (byte)1 : (byte)0);
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
                    <ApplySpellsSerializer, Func<StreamReader, SerializationContext, PropertyMetaData, object>>
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
                    <ApplySpellsSerializer, Action<StreamWriter, SerializationContext, object, PropertyMetaData>>
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
