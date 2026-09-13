// --------------------------------------------------------------------------------------------------------------------
// <copyright file="PlayfieldAllCitiesSerializer.cs" company="OmniCell">
//   Copyright © 2026 OmniCell contributors.
//   Added to SmokeLounge.AOtomation.Messaging, which is distributed under the
//   Do What The Fuck You Want To Public License, Version 2, as published by
//   Sam Hocevar. See http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the PlayfieldAllCitiesSerializer type.
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
    /// Reads and writes PlayfieldAllCities.
    /// </summary>
    /// <remarks>
    /// The message is a length and a blob. The reader at Gamecode.dll
    /// 0x101304F8 takes an int16, copies that many bytes into a second stream,
    /// and seeks past them without looking inside; the shape of what it copied
    /// is decided by
    /// PlayfieldCityHolderClient_c::UpdateNewHouses(BinaryStream&amp;, bool) at
    /// city.dll 0x10016317, which reads a uint32 count and that many twenty
    /// five byte house records.
    ///
    /// Written by hand because the length is a byte count rather than an entry
    /// count, and because zero means the whole payload is absent - not a list
    /// of no houses, but nothing at all, count included. Every captured copy is
    /// that case.
    /// </remarks>
    public class PlayfieldAllCitiesSerializer : ISerializer
    {
        #region Constants

        /// <summary>
        /// Position, template, the demolition byte, and an Identity.
        /// </summary>
        private const int HouseLength = 12 + 4 + 1 + 8;

        #endregion

        #region Fields

        private readonly Type type;

        #endregion

        #region Constructors and Destructors

        public PlayfieldAllCitiesSerializer()
        {
            this.type = typeof(PlayfieldAllCitiesMessage);
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
            var message = new PlayfieldAllCitiesMessage();

            message.N3MessageType = (N3MessageType)streamReader.ReadInt32();
            message.Identity = streamReader.ReadIdentity();
            message.Unknown = streamReader.ReadByte();

            int payloadLength = streamReader.ReadInt16();
            if (payloadLength <= 0)
            {
                // Nothing follows at all. Null rather than an empty array,
                // because an empty array would have to write a count.
                return message;
            }

            var houses = new CityHouse[streamReader.ReadInt32()];
            for (var i = 0; i < houses.Length; i++)
            {
                houses[i] = new CityHouse
                            {
                                Position =
                                    new Vector3
                                    {
                                        X = streamReader.ReadSingle(),
                                        Y = streamReader.ReadSingle(),
                                        Z = streamReader.ReadSingle()
                                    },
                                Template = streamReader.ReadInt32(),
                                DemolitionStarted = streamReader.ReadByte() == 1,
                                Identity = streamReader.ReadIdentity()
                            };
            }

            message.Houses = houses;
            return message;
        }

        public void Serialize(
            StreamWriter streamWriter,
            SerializationContext serializationContext,
            object value,
            PropertyMetaData propertyMetaData = null)
        {
            var message = (PlayfieldAllCitiesMessage)value;

            streamWriter.WriteInt32((int)message.N3MessageType);
            streamWriter.WriteInt32((int)message.Identity.Type);
            streamWriter.WriteInt32(message.Identity.Instance);
            streamWriter.WriteByte(message.Unknown);

            if (message.Houses == null)
            {
                streamWriter.WriteInt16(0);
                return;
            }

            streamWriter.WriteInt16((short)(4 + (message.Houses.Length * HouseLength)));
            streamWriter.WriteInt32(message.Houses.Length);

            foreach (var house in message.Houses)
            {
                streamWriter.WriteSingle(house.Position.X);
                streamWriter.WriteSingle(house.Position.Y);
                streamWriter.WriteSingle(house.Position.Z);
                streamWriter.WriteInt32(house.Template);
                streamWriter.WriteByte(house.DemolitionStarted ? (byte)1 : (byte)0);
                streamWriter.WriteInt32((int)house.Identity.Type);
                streamWriter.WriteInt32(house.Identity.Instance);
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
                    <PlayfieldAllCitiesSerializer,
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
                    <PlayfieldAllCitiesSerializer,
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
