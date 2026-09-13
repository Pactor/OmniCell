// --------------------------------------------------------------------------------------------------------------------
// <copyright file="AoTransportSignalSerializer.cs" company="OmniCell">
//   Copyright © 2026 OmniCell contributors.
//   Added to SmokeLounge.AOtomation.Messaging, which is distributed under the
//   Do What The Fuck You Want To Public License, Version 2, as published by
//   Sam Hocevar. See http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the AoTransportSignalSerializer type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace SmokeLounge.AOtomation.Messaging.Serialization.Serializers.Custom
{
    using System;
    using System.Linq.Expressions;

    using SmokeLounge.AOtomation.Messaging.Messages;
    using SmokeLounge.AOtomation.Messaging.Messages.N3Messages;

    /// <summary>
    /// Reads and writes AoTransportSignal.
    /// </summary>
    /// <remarks>
    /// Written by hand because the body has no length in front of it. It is
    /// whatever is left in the packet, which is not something an AoMember can
    /// say - every array the attribute serializer knows how to read is counted,
    /// and this one is not.
    ///
    /// Transcribed from Gamecode.dll 0x1012FAAB and checked against the writer
    /// at 0x1012FA7E, which emits the same two fields the same way.
    /// </remarks>
    public class AoTransportSignalSerializer : ISerializer
    {
        #region Fields

        private readonly Type type;

        #endregion

        #region Constructors and Destructors

        public AoTransportSignalSerializer()
        {
            this.type = typeof(AoTransportSignalMessage);
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
            var message = new AoTransportSignalMessage();

            message.N3MessageType = (N3MessageType)streamReader.ReadInt32();
            message.Identity = streamReader.ReadIdentity();
            message.Unknown = streamReader.ReadByte();

            message.Signal = streamReader.ReadInt32();

            // The client asks the stream for its size, takes the difference
            // from where it has got to, and copies that. An empty body is a bad
            // packet there, so it is one here.
            var remaining = (int)(streamReader.Length - streamReader.Position);
            if (remaining <= 0)
            {
                throw new InvalidOperationException(
                    "an AoTransportSignal with no body is not a packet the client will accept");
            }

            message.Payload = streamReader.ReadBytes(remaining);

            return message;
        }

        public void Serialize(
            StreamWriter streamWriter,
            SerializationContext serializationContext,
            object value,
            PropertyMetaData propertyMetaData = null)
        {
            var message = (AoTransportSignalMessage)value;

            streamWriter.WriteInt32((int)message.N3MessageType);
            streamWriter.WriteInt32((int)message.Identity.Type);
            streamWriter.WriteInt32(message.Identity.Instance);
            streamWriter.WriteByte(message.Unknown);

            streamWriter.WriteInt32(message.Signal);

            var payload = message.Payload ?? new byte[0];
            foreach (var b in payload)
            {
                streamWriter.WriteByte(b);
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
                    <AoTransportSignalSerializer, Func<StreamReader, SerializationContext, PropertyMetaData, object>>(
                        o => o.Deserialize);
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
                    <AoTransportSignalSerializer, Action<StreamWriter, SerializationContext, object, PropertyMetaData>>(
                        o => o.Serialize);
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
