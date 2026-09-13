// --------------------------------------------------------------------------------------------------------------------
// <copyright file="NanoEffectSerializer.cs" company="OmniCell">
//   Copyright (c) 2026 OmniCell contributors.
//   Added to SmokeLounge.AOtomation.Messaging, which is distributed under the
//   Do What The Fuck You Want To Public License, Version 2, as published by
//   Sam Hocevar. See http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the NanoEffectSerializer type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace SmokeLounge.AOtomation.Messaging.Serialization.Serializers.Custom
{
    using System;
    using System.Linq.Expressions;

    using SmokeLounge.AOtomation.Messaging.GameData;

    /// <summary>
    /// One GameData::SpellData_t, for the attribute-driven path.
    /// </summary>
    /// <remarks>
    /// A spell effect cannot be described with attributes: how many arguments
    /// it carries is decided by its own game function, through the format table
    /// GameData builds in code. <see cref="NanoEffects"/> already reads and
    /// writes one, and three messages already use it - ApplySpells, SpellList
    /// and CorpseFullUpdate - but each of those does it from a hand-written
    /// serializer for the whole message, so the type was unreachable from a
    /// message that is otherwise described by attributes.
    ///
    /// Registering this against <see cref="NanoEffect"/> makes it reachable.
    /// The array serializer resolves its element type through the same
    /// registry, so an X3F1 counted NanoEffect[] member now works anywhere -
    /// which is what FullCharacter's list of running effects needed, and it
    /// needed a whole custom serializer for a forty member message otherwise.
    ///
    /// The three messages that call NanoEffects directly are untouched: they
    /// never ask the resolver for this type.
    /// </remarks>
    public class NanoEffectSerializer : ISerializer
    {
        #region Fields

        private readonly Type type;

        #endregion

        #region Constructors and Destructors

        public NanoEffectSerializer()
        {
            this.type = typeof(NanoEffect);
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
            return NanoEffects.Read(streamReader);
        }

        public void Serialize(
            StreamWriter streamWriter,
            SerializationContext serializationContext,
            object value,
            PropertyMetaData propertyMetaData = null)
        {
            var effect = value as NanoEffect;
            if (effect == null)
            {
                return;
            }

            NanoEffects.Write(streamWriter, effect);
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
                    <NanoEffectSerializer, Func<StreamReader, SerializationContext, PropertyMetaData, object>>(
                        o => o.Deserialize);
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
                    <NanoEffectSerializer, Action<StreamWriter, SerializationContext, object, PropertyMetaData>>(
                        o => o.Serialize);
            var serializerExp = Expression.New(this.GetType());
            return Expression.Call(
                serializerExp,
                serializerMethodInfo,
                new Expression[]
                    {
                        streamWriterExpression, serializationContextExpression,
                        Expression.Convert(valueExpression, typeof(object)),
                        Expression.Constant(propertyMetaData, typeof(PropertyMetaData))
                    });
        }

        #endregion
    }
}
