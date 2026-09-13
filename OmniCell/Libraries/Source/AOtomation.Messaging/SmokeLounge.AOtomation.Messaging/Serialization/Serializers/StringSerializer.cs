// --------------------------------------------------------------------------------------------------------------------
// <copyright file="StringSerializer.cs" company="SmokeLounge">
//   Copyright © 2013 SmokeLounge.
//   This program is free software. It comes without any warranty, to
//   the extent permitted by applicable law. You can redistribute it
//   and/or modify it under the terms of the Do What The Fuck You Want
//   To Public License, Version 2, as published by Sam Hocevar. See
//   http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the StringSerializer type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace SmokeLounge.AOtomation.Messaging.Serialization.Serializers
{
    using System;
    using System.Collections.Generic;
    using System.Linq.Expressions;

    public class StringSerializer : ISerializer
    {
        #region Fields

        private readonly Type type;

        #endregion

        #region Constructors and Destructors

        public StringSerializer()
        {
            this.type = typeof(string);
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
            // Length is not written anywhere ahead of these; the terminator is
            // the only thing marking the end, so the size serializer has nothing
            // to read and must not be asked.
            if (propertyMetaData.Options.SerializeSize == ArraySizeType.NullTerminated)
            {
                return streamReader.ReadStringNullTerminated();
            }

            // A length that counts a terminator. Read the lot and hand back the
            // string without it; see ArraySizeType.Int32Terminated.
            if (propertyMetaData.Options.SerializeSize == ArraySizeType.Int32Terminated)
            {
                int terminated = streamReader.ReadInt32();
                return terminated == 0 ? string.Empty : streamReader.ReadString(terminated);
            }

            int length;
            if (propertyMetaData.Options.SerializeSize == ArraySizeType.NoSerialization)
            {
                length = propertyMetaData.Options.FixedSizeLength;
            }
            else
            {
                var arraySizeSerializer = new ArraySizeSerializer(propertyMetaData.Options.SerializeSize);
                length =
                    (int)
                    arraySizeSerializer.Deserialize(
                        streamReader, serializationContext, propertyMetaData: propertyMetaData);
            }

            return streamReader.ReadString(length);
        }

        public Expression DeserializerExpression(
            ParameterExpression streamReaderExpression, 
            ParameterExpression serializationContextExpression, 
            Expression assignmentTargetExpression, 
            PropertyMetaData propertyMetaData)
        {
            // The compiled path builds its own length handling and would never
            // reach the branch in Deserialize, so it calls it instead. See
            // ArraySizeType.Int32Terminated.
            if (propertyMetaData.Options.SerializeSize == ArraySizeType.Int32Terminated)
            {
                var terminatedRead =
                    ReflectionHelper
                        .GetMethodInfo<StringSerializer, Func<StreamReader, SerializationContext, PropertyMetaData, object>>(
                            o => o.Deserialize);
                Expression callTerminated = Expression.Call(
                    Expression.New(this.GetType()),
                    terminatedRead,
                    new Expression[]
                        {
                            streamReaderExpression, serializationContextExpression,
                            Expression.Constant(propertyMetaData, typeof(PropertyMetaData))
                        });
                return Expression.Assign(
                    assignmentTargetExpression,
                    Expression.Convert(callTerminated, assignmentTargetExpression.Type));
            }

            if (propertyMetaData.Options.SerializeSize == ArraySizeType.NullTerminated)
            {
                var readNulMethodInfo =
                    ReflectionHelper.GetMethodInfo<StreamReader, Func<string>>(o => o.ReadStringNullTerminated);
                Expression callReadNulExp = Expression.Call(streamReaderExpression, readNulMethodInfo);
                return assignmentTargetExpression.Type.IsAssignableFrom(this.type)
                           ? Expression.Assign(assignmentTargetExpression, callReadNulExp)
                           : Expression.Assign(
                               assignmentTargetExpression,
                               Expression.Convert(callReadNulExp, assignmentTargetExpression.Type));
            }

            var expressions = new List<Expression>();

            var lengthExpression = Expression.Variable(typeof(int), "length");

            Expression assignLengthExpression;

            if (propertyMetaData.Options.SerializeSize == ArraySizeType.NoSerialization)
            {
                assignLengthExpression = Expression.Assign(
                    lengthExpression, Expression.Constant(propertyMetaData.Options.FixedSizeLength, typeof(int)));
            }
            else
            {
                assignLengthExpression =
                    new ArraySizeSerializer(propertyMetaData.Options.SerializeSize).DeserializerExpression(
                        streamReaderExpression, serializationContextExpression, lengthExpression, propertyMetaData);
            }

            expressions.Add(assignLengthExpression);

            var readMethodInfo = ReflectionHelper.GetMethodInfo<StreamReader, Func<int, string>>(o => o.ReadString);
            var callReadExp = Expression.Call(
                streamReaderExpression, readMethodInfo, new Expression[] { lengthExpression });

            Expression setString = assignmentTargetExpression.Type.IsAssignableFrom(this.type)
                                       ? Expression.Assign(assignmentTargetExpression, callReadExp)
                                       : Expression.Assign(
                                           assignmentTargetExpression, 
                                           Expression.Convert(callReadExp, assignmentTargetExpression.Type));

            expressions.Add(setString);

            var block = Expression.Block(new[] { lengthExpression }, expressions);
            return block;
        }

        public void Serialize(
            StreamWriter streamWriter, 
            SerializationContext serializationContext, 
            object value, 
            PropertyMetaData propertyMetaData = null)
        {
            if (propertyMetaData.Options.SerializeSize == ArraySizeType.NullTerminated)
            {
                streamWriter.WriteStringNullTerminated((string)value);
                return;
            }

            if (propertyMetaData.Options.SerializeSize == ArraySizeType.Int32Terminated)
            {
                var terminated = (string)value ?? string.Empty;
                if (terminated.Length == 0)
                {
                    streamWriter.WriteInt32(0);
                    return;
                }

                streamWriter.WriteInt32(terminated.Length + 1);
                streamWriter.WriteString(terminated, terminated.Length + 1);
                return;
            }

            if (propertyMetaData.Options.SerializeSize != ArraySizeType.NoSerialization)
            {
                var arraySizeSerializer = new ArraySizeSerializer(propertyMetaData.Options.SerializeSize);
                arraySizeSerializer.Serialize(streamWriter, serializationContext, value, propertyMetaData);
            }

            var writeStringParam = propertyMetaData.Options.IsFixedSize
                                       ? (int?)propertyMetaData.Options.FixedSizeLength
                                       : null;
            streamWriter.WriteString((string)value, writeStringParam);
        }

        public Expression SerializerExpression(
            ParameterExpression streamWriterExpression, 
            ParameterExpression serializationContextExpression, 
            Expression valueExpression, 
            PropertyMetaData propertyMetaData)
        {
            if (propertyMetaData.Options.SerializeSize == ArraySizeType.Int32Terminated)
            {
                var terminatedWrite =
                    ReflectionHelper
                        .GetMethodInfo<StringSerializer, Action<StreamWriter, SerializationContext, object, PropertyMetaData>>(
                            o => o.Serialize);
                return Expression.Call(
                    Expression.New(this.GetType()),
                    terminatedWrite,
                    new Expression[]
                        {
                            streamWriterExpression, serializationContextExpression,
                            Expression.Convert(valueExpression, typeof(object)),
                            Expression.Constant(propertyMetaData, typeof(PropertyMetaData))
                        });
            }

            if (valueExpression.Type.IsAssignableFrom(this.type) == false)
            {
                valueExpression = Expression.Convert(valueExpression, this.type);
            }

            if (propertyMetaData.Options.SerializeSize == ArraySizeType.NullTerminated)
            {
                var writeNulMethodInfo =
                    ReflectionHelper.GetMethodInfo<StreamWriter, Action<string>>(o => o.WriteStringNullTerminated);
                return Expression.Call(
                    streamWriterExpression,
                    writeNulMethodInfo,
                    new[] { Expression.Convert(valueExpression, this.type) });
            }

            var expressions = new List<Expression>();
            if (propertyMetaData.Options.SerializeSize != ArraySizeType.NoSerialization)
            {
                var serializeSizeExp =
                    new ArraySizeSerializer(propertyMetaData.Options.SerializeSize).SerializerExpression(
                        streamWriterExpression, serializationContextExpression, valueExpression, propertyMetaData);
                expressions.Add(serializeSizeExp);
            }

            var writeMethodInfo = ReflectionHelper.GetMethodInfo<StreamWriter, Action<string, int?>>(o => o.WriteString);

            Expression writeStringParam = propertyMetaData.Options.IsFixedSize
                                              ? Expression.Constant(
                                                  propertyMetaData.Options.FixedSizeLength, typeof(int?))
                                              : Expression.Constant(null, typeof(int?));

            var callWriteExp = Expression.Call(
                streamWriterExpression, 
                writeMethodInfo, 
                new[] { Expression.Convert(valueExpression, this.type), writeStringParam });
            expressions.Add(callWriteExp);
            var block = Expression.Block(expressions);
            return block;
        }

        #endregion
    }
}