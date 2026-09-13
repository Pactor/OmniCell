using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SmokeLounge.AOtomation.Messaging.Serialization.Serializers.Custom
{
    using System;
    using System.Linq.Expressions;

    using SmokeLounge.AOtomation.Messaging.GameData;
    using SmokeLounge.AOtomation.Messaging.Messages.N3Messages;

    public class FollowInfoSerializer : ISerializer
    {       
        #region Fields

        private readonly Type type;

        #endregion

        #region Constructors and Destructors

        public FollowInfoSerializer()
        {
            this.type = typeof(FollowInfo);
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

        public object Deserialize(
            StreamReader streamReader,
            SerializationContext serializationContext,
            PropertyMetaData propertyMetaData = null)
        {
            byte infoType = streamReader.ReadByte();
            if (infoType == 1)
            {
                var followCoordinateInfo = new FollowCoordinateInfo();
                followCoordinateInfo.FollowInfoType = 1;
                followCoordinateInfo.MoveMode = streamReader.ReadByte();
                followCoordinateInfo.CoordinateCount = streamReader.ReadByte();
                followCoordinateInfo.CurrentCoordinates=new Vector3();
                followCoordinateInfo.CurrentCoordinates.X = streamReader.ReadSingle();
                followCoordinateInfo.CurrentCoordinates.Y = streamReader.ReadSingle();
                followCoordinateInfo.CurrentCoordinates.Z = streamReader.ReadSingle();
                followCoordinateInfo.EndCoordinates=new Vector3();
                followCoordinateInfo.EndCoordinates.X = streamReader.ReadSingle();
                followCoordinateInfo.EndCoordinates.Y = streamReader.ReadSingle();
                followCoordinateInfo.EndCoordinates.Z = streamReader.ReadSingle();
                return followCoordinateInfo;
            }
            if (infoType == 2)
            {
                var followTargetInfo = new FollowTargetInfo();
                followTargetInfo.FollowInfoType = 2;
                followTargetInfo.MoveType = streamReader.ReadByte();
                IdentityType itype = (IdentityType)streamReader.ReadInt32();

                followTargetInfo.Target = new Identity() { Type = itype, Instance = streamReader.ReadInt32() };
                // One float, not a byte and three of padding. The client's
                // reader at Gamecode 0x10073740 takes a single float into the
                // object's + 0x20 here, and its writer at 0x10073623 puts the
                // same float back; neither side has padding in this record.
                //
                // Reading it as a byte plus three discarded bytes and writing
                // the byte plus three zeros round-trips whenever the value is
                // exactly 2.0, which it is in 27,466 of 27,468 captured copies.
                // The two that are not are 2.5, and they came back as 2.0.
                followTargetInfo.Unknown1 = streamReader.ReadSingle();
                followTargetInfo.X = streamReader.ReadSingle();
                followTargetInfo.Y = streamReader.ReadSingle();
                followTargetInfo.Z = streamReader.ReadSingle();
                followTargetInfo.CoordinateCount = streamReader.ReadByte();
                followTargetInfo.Coordinates = new Vector3[followTargetInfo.CoordinateCount];
                for (int i = 0; i < followTargetInfo.CoordinateCount; i++)
                {
                    followTargetInfo.Coordinates[i] = new Vector3
                                                          {
                                                              X = streamReader.ReadSingle(),
                                                              Y = streamReader.ReadSingle(),
                                                              Z = streamReader.ReadSingle()
                                                          };
                }
                return followTargetInfo;
            }

            streamReader.Position = streamReader.Position - 1;
            return null;
        }

        public void Serialize(
            StreamWriter streamWriter,
            SerializationContext serializationContext,
            object value,
            PropertyMetaData propertyMetaData = null)
        {            if (value == null)
            {
                return;
            }

            var ftinfo = value as FollowTargetInfo;
            if (ftinfo != null)
            {
                streamWriter.WriteByte(ftinfo.FollowInfoType);
                streamWriter.WriteByte(ftinfo.MoveType);
                streamWriter.WriteInt32((int)ftinfo.Target.Type);
                streamWriter.WriteInt32(ftinfo.Target.Instance);
                streamWriter.WriteSingle(ftinfo.Unknown1);
                streamWriter.WriteSingle(ftinfo.X);
                streamWriter.WriteSingle(ftinfo.Y);
                streamWriter.WriteSingle(ftinfo.Z);
                Vector3[] coordinates = ftinfo.Coordinates ?? new Vector3[0];
                streamWriter.WriteByte((byte)coordinates.Length);
                foreach (Vector3 coordinate in coordinates)
                {
                    streamWriter.WriteSingle(coordinate.X);
                    streamWriter.WriteSingle(coordinate.Y);
                    streamWriter.WriteSingle(coordinate.Z);
                }
            }
            var fcinfo = value as FollowCoordinateInfo;
            if (fcinfo != null)
            {
                streamWriter.WriteByte(fcinfo.FollowInfoType);
                streamWriter.WriteByte(fcinfo.MoveMode);
                streamWriter.WriteByte(fcinfo.CoordinateCount);
                streamWriter.WriteSingle(fcinfo.CurrentCoordinates.X);
                streamWriter.WriteSingle(fcinfo.CurrentCoordinates.Y);
                streamWriter.WriteSingle(fcinfo.CurrentCoordinates.Z);
                streamWriter.WriteSingle(fcinfo.EndCoordinates.X);
                streamWriter.WriteSingle(fcinfo.EndCoordinates.Y);
                streamWriter.WriteSingle(fcinfo.EndCoordinates.Z);
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
                    <FollowInfoSerializer, Func<StreamReader, SerializationContext, PropertyMetaData, object>>(
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

            var assignmentExp = Expression.Assign(
                assignmentTargetExpression, Expression.TypeAs(callExp, assignmentTargetExpression.Type));
            return assignmentExp;
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
                    <FollowInfoSerializer, Action<StreamWriter, SerializationContext, object, PropertyMetaData>>(o => o.Serialize);
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
    }
}
