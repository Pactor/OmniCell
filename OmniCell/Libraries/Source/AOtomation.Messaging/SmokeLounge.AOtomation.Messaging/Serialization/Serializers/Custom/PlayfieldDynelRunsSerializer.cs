// --------------------------------------------------------------------------------------------------------------------
// <copyright file="PlayfieldDynelRunsSerializer.cs" company="OmniCell">
//   Copyright © 2026 OmniCell contributors.
//   Added to SmokeLounge.AOtomation.Messaging, which is distributed under the
//   Do What The Fuck You Want To Public License, Version 2, as published by
//   Sam Hocevar. See http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the PlayfieldDynelRunsSerializer type.
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
    /// The dynel run table that closes a PlayfieldAnarchyF, when there is one.
    /// </summary>
    /// <remarks>
    /// The table is optional and nothing in the message announces it. Of twelve
    /// captured copies, the nine for Arete Landing carry one and the three for
    /// playfield 655 stop dead after the playfield coordinates - so whether it
    /// is there is decided by whether any bytes are left, which the client can
    /// do because the header carried the length.
    ///
    /// That is why this reads the stream position against its length instead of
    /// a count or a flag: there is no count or flag to read. Getting it wrong
    /// costs the whole tail, which is what used to happen - the message was
    /// written eighty six bytes long where the live server sends a hundred and
    /// ninety eight, and the hundred and twelve bytes describing every door and
    /// vending machine in the playfield were dropped on the floor.
    ///
    /// The eight bytes after the last run are two words of 0xFF. They close the
    /// table and are written back as they were read.
    /// </remarks>
    public class PlayfieldDynelRunsSerializer : ISerializer
    {
        #region Constants

        /// <summary>
        /// The bytes that close the table.
        /// </summary>
        private const int TerminatorLength = 8;

        #endregion

        #region Fields

        private readonly Type type;

        #endregion

        #region Constructors and Destructors

        public PlayfieldDynelRunsSerializer()
        {
            this.type = typeof(PlayfieldDynelRun[]);
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
            // No room for a count means there is no table, which is the ordinary
            // case for a playfield that is not instanced.
            if (streamReader.Position + 4 > streamReader.Length)
            {
                return null;
            }

            int count = streamReader.ReadInt32();
            if (count < 0 || streamReader.Position + (count * 20) > streamReader.Length)
            {
                streamReader.Position = streamReader.Position - 4;
                return null;
            }

            var runs = new PlayfieldDynelRun[count];
            for (var i = 0; i < count; i++)
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

            if (streamReader.Position + TerminatorLength <= streamReader.Length)
            {
                streamReader.ReadBytes(TerminatorLength);
            }

            return runs;
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
                    <PlayfieldDynelRunsSerializer, Func<StreamReader, SerializationContext, PropertyMetaData, object>>(
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
            var runs = value as PlayfieldDynelRun[];
            if (runs == null)
            {
                return;
            }

            streamWriter.WriteInt32(runs.Length);
            foreach (PlayfieldDynelRun run in runs)
            {
                streamWriter.WriteInt32((int)run.Type);
                streamWriter.WriteInt32(run.Unknown);
                streamWriter.WriteInt32(run.StartIndex);
                streamWriter.WriteInt32(run.Count);
                streamWriter.WriteInt32(run.FirstInstance);
            }

            for (var i = 0; i < TerminatorLength; i++)
            {
                streamWriter.WriteByte(0xFF);
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
                    .GetMethodInfo<PlayfieldDynelRunsSerializer, Action<StreamWriter, SerializationContext, object, PropertyMetaData>>(
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
