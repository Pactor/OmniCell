// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ItemStatPairsSerializer.cs" company="OmniCell">
//   Copyright © 2026 OmniCell contributors.
//   Added to SmokeLounge.AOtomation.Messaging, which is distributed under the
//   Do What The Fuck You Want To Public License, Version 2, as published by
//   Sam Hocevar. See http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the ItemStatPairsSerializer type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace SmokeLounge.AOtomation.Messaging.Serialization.Serializers.Custom
{
    using System;
    using System.Collections.Generic;
    using System.Linq.Expressions;

    using SmokeLounge.AOtomation.Messaging.GameData;
    using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

    /// <summary>
    /// Stat id and value pairs that run to the end of a WeaponItemFullUpdate.
    /// </summary>
    /// <remarks>
    /// The message ends with a run of stat pairs. Six of them are always there
    /// and the model names them one by one - staticinstance, acgitemlevel,
    /// acgitemtemplateid, acgitemtemplateid2, multiplecount and energy, each
    /// with its value. Some weapons carry two more: itemdelay (294) and
    /// rechargedelay (210), which are how long the weapon takes to swing and to
    /// come back.
    ///
    /// Whether they are there is not announced. Of 3,906 captured copies, 2,914
    /// carry them and 992 do not, and the two populations line up exactly with
    /// the X3F1 count in front of the pairs (once called Unknown6) - 10090
    /// against 8072, the counts for nine stats and for seven (see
    /// WeaponItemFullUpdateMessage). Reading to the end needs no gating on it:
    /// pairs are taken while more than the closing word remains.
    ///
    /// The last four bytes are zero in all 3,906 and are read separately.
    /// </remarks>
    public class ItemStatPairsSerializer : ISerializer
    {
        #region Constants

        /// <summary>
        /// The closing word that follows the last pair.
        /// </summary>
        private const int TerminatorLength = 4;

        /// <summary>
        /// A stat id and its value.
        /// </summary>
        private const int PairLength = 8;

        #endregion

        #region Fields

        private readonly Type type;

        #endregion

        #region Constructors and Destructors

        public ItemStatPairsSerializer()
        {
            this.type = typeof(ItemStatPair[]);
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
            var pairs = new List<ItemStatPair>();
            while (streamReader.Position + PairLength + TerminatorLength <= streamReader.Length)
            {
                pairs.Add(
                    new ItemStatPair { Stat = streamReader.ReadInt32(), Value = streamReader.ReadInt32() });
            }

            return pairs.ToArray();
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
                    <ItemStatPairsSerializer, Func<StreamReader, SerializationContext, PropertyMetaData, object>>(
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
            var castExp = Expression.Convert(callExp, this.type);
            var assignmentExp = Expression.Assign(assignmentTargetExpression, castExp);
            return assignmentExp;
        }

        public void Serialize(
            StreamWriter streamWriter,
            SerializationContext serializationContext,
            object value,
            PropertyMetaData propertyMetaData = null)
        {
            var pairs = value as ItemStatPair[];
            if (pairs == null)
            {
                return;
            }

            foreach (ItemStatPair pair in pairs)
            {
                streamWriter.WriteInt32(pair.Stat);
                streamWriter.WriteInt32(pair.Value);
            }
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
                    <ItemStatPairsSerializer, Action<StreamWriter, SerializationContext, object, PropertyMetaData>>(
                        o => o.Serialize);
            var serializerExp = Expression.New(this.GetType());
            var callExp = Expression.Call(
                serializerExp,
                serializerMethodInfo,
                new Expression[]
                    {
                        streamWriterExpression, serializationContextExpression,
                        Expression.Convert(valueExpression, typeof(object)),
                        Expression.Constant(propertyMetaData, typeof(PropertyMetaData))
                    });
            return callExp;
        }

        #endregion
    }
}
