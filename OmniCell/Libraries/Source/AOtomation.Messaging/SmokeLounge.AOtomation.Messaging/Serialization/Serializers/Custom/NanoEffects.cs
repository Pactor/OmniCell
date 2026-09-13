// --------------------------------------------------------------------------------------------------------------------
// <copyright file="NanoEffects.cs" company="OmniCell">
//   Copyright © 2026 OmniCell contributors.
//   Added to SmokeLounge.AOtomation.Messaging, which is distributed under the
//   Do What The Fuck You Want To Public License, Version 2, as published by
//   Sam Hocevar. See http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the NanoEffects type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace SmokeLounge.AOtomation.Messaging.Serialization.Serializers.Custom
{
    using System;
    using System.Collections.Generic;

    using SmokeLounge.AOtomation.Messaging.GameData;

    /// <summary>
    /// Reads and writes the effect record three messages share.
    /// </summary>
    /// <remarks>
    /// The client calls it GameData::SpellData_t and reads it with one function,
    /// Gamecode.dll 0x100A71AE, from SpellList, CorpseFullUpdate and ApplySpells.
    /// This is that function's format, in one place, because it was in three.
    ///
    /// The awkward part is the arguments. An effect ends with a run of them
    /// whose number and kinds depend on the game function in the first half of
    /// its identity, and nothing in the record says how long that run is. That
    /// used to be a table of fourteen functions measured off the wire. It is now
    /// the client's own table of 232, read out of GameData.dll
    /// - see <see cref="NanoEffectFormats"/>.
    /// </remarks>
    public static class NanoEffects
    {
        #region Public Methods and Operators

        /// <summary>
        /// An X3F1 counted list of effects.
        /// </summary>
        public static NanoEffect[] ReadList(StreamReader streamReader)
        {
            var effects = new NanoEffect[X3F1Count(streamReader.ReadInt32())];
            for (var i = 0; i < effects.Length; i++)
            {
                effects[i] = Read(streamReader);
            }

            return effects;
        }

        public static void WriteList(StreamWriter streamWriter, NanoEffect[] effects)
        {
            effects = effects ?? new NanoEffect[0];
            streamWriter.WriteInt32(X3F1Value(effects.Length));
            foreach (NanoEffect effect in effects)
            {
                Write(streamWriter, effect);
            }
        }

        public static NanoEffect Read(StreamReader streamReader)
        {
            var effect = new NanoEffect();
            effect.Effect = streamReader.ReadIdentity();
            effect.Version = streamReader.ReadInt32();
            effect.CriterionCount = streamReader.ReadInt32();

            effect.Criteria = new NanoCriterion[Math.Max(0, effect.CriterionCount)];
            for (var c = 0; c < effect.Criteria.Length; c++)
            {
                effect.Criteria[c] = new NanoCriterion
                                     {
                                         Stat = streamReader.ReadInt32(),
                                         Value = streamReader.ReadInt32(),
                                         Operator = (NanoCriterionOperator)streamReader.ReadInt32()
                                     };
            }

            effect.Hits = streamReader.ReadInt32();
            effect.Amount = streamReader.ReadInt32();
            effect.Target = (NanoEffectTarget)streamReader.ReadInt32();
            effect.SpellList = streamReader.ReadInt32();

            effect.Arguments = ReadArguments(streamReader, effect.Effect.Type);
            return effect;
        }

        public static void Write(StreamWriter streamWriter, NanoEffect effect)
        {
            streamWriter.WriteIdentity(effect.Effect);
            streamWriter.WriteInt32(effect.Version);
            streamWriter.WriteInt32(effect.Criteria == null ? 0 : effect.Criteria.Length);

            foreach (NanoCriterion criterion in effect.Criteria ?? new NanoCriterion[0])
            {
                streamWriter.WriteInt32(criterion.Stat);
                streamWriter.WriteInt32(criterion.Value);
                streamWriter.WriteInt32((int)criterion.Operator);
            }

            streamWriter.WriteInt32(effect.Hits);
            streamWriter.WriteInt32(effect.Amount);
            streamWriter.WriteInt32((int)effect.Target);
            streamWriter.WriteInt32(effect.SpellList);

            streamWriter.WriteBytes(effect.Arguments ?? new byte[0]);
        }

        #endregion

        #region Methods

        /// <summary>
        /// The block of arguments an effect ends with.
        /// </summary>
        /// <remarks>
        /// How many there are and what kinds they are comes from the client's
        /// own format table, because the stream does not say. Every argument is
        /// four bytes except a string, which is a four byte length and that many
        /// bytes after it.
        ///
        /// A function the table has no format for throws rather than guessing.
        /// A guess here does not fail where it is made; it fails as a garbled
        /// name or a missing nano some fields later, which is exactly how the
        /// old hand-measured table hid being wrong about game function 53104 -
        /// it recorded a fixed thirty four bytes for a line of speech that is a
        /// string, and thirty four was only ever the length of the speech in the
        /// captures.
        /// </remarks>
        private static byte[] ReadArguments(StreamReader streamReader, IdentityType function)
        {
            int[] arguments = NanoEffectFormats.ArgumentsFor((int)function);
            if (arguments == null)
            {
                throw new InvalidOperationException(
                    string.Format(
                        "the client carries no spell format for game function {0}, so this effect cannot be read",
                        (int)function));
            }

            var block = new List<byte>();
            foreach (int argument in arguments)
            {
                byte[] head = streamReader.ReadBytes(4);
                block.AddRange(head);

                if (NanoEffectFormats.KindOf(argument) != NanoEffectFormats.StringArgument)
                {
                    continue;
                }

                int length = (head[0] << 24) | (head[1] << 16) | (head[2] << 8) | head[3];
                block.AddRange(streamReader.ReadBytes(length));
            }

            return block.ToArray();
        }

        private static int X3F1Count(int value)
        {
            if (value <= 0 || value % 0x3F1 != 0 || (value / 0x3F1) - 1 > 0x7530)
            {
                throw new InvalidOperationException(
                    string.Format("{0} is not an X3F1 count, so this list cannot be read", value));
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
