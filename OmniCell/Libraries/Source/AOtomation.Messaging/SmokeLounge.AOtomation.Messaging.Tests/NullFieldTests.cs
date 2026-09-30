#region License

// Copyright (c) 2026, OmniCell contributors
//
// This file is part of OmniCell and is distributed under the terms the project
// is licensed under. See the repository root for details.

#endregion

namespace SmokeLounge.AOtomation.Messaging.Tests
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using SmokeLounge.AOtomation.Messaging.Messages;
    using SmokeLounge.AOtomation.Messaging.Serialization;

    using StreamWriter = SmokeLounge.AOtomation.Messaging.Serialization.StreamWriter;

    /// <summary>
    /// A string or an array nobody filled in must not take a message down.
    /// </summary>
    /// <remarks>
    /// The teleport bug in Pactor/OmniCell#1 was one of these: Trailer was
    /// null, the array serializer took its length without looking, and every
    /// teleport threw part way through writing. Sweeping the whole message set
    /// afterwards found it was not alone - 46 of the 155 message bodies threw
    /// when serialized straight after construction, 19 of them purely because
    /// a string was null.
    ///
    /// Both serializers now read a null as empty, which is what a null means
    /// on the wire, and this is the net that keeps it that way.
    ///
    /// **A null object is deliberately still an exception.** A message with no
    /// Vector3 has no position, and writing 0,0,0 rather than throwing would
    /// put a player at the corner of the map with nothing said. Those are the
    /// caller's job, and the sweep below fills them in before it tests.
    /// </remarks>
    [TestClass]
    public class NullFieldTests
    {
        /// <summary>
        /// The six whose structural fields this test cannot build itself.
        /// </summary>
        /// <remarks>
        /// Not exceptions to the rule - they carry a property whose type has
        /// no parameterless constructor (IPAddress, and the info and
        /// appearance records), so the sweep cannot put a message together to
        /// test. Their strings and arrays go through the same two serializers
        /// as everything else.
        /// </remarks>
        private static readonly HashSet<string> CannotBuild = new HashSet<string>
                                                                  {
                                                                      "CorpseFullUpdateMessage",
                                                                      "GenericCmdMessage",
                                                                      "InfoPacketMessage",
                                                                      "SimpleCharFullUpdateMessage",
                                                                      "ZoneInfoMessage",
                                                                      "ZoneRedirectionMessage",
                                                                  };

        /// <summary>
        /// Every message serializes with its strings and arrays left null.
        /// </summary>
        [TestMethod]
        public void NullStringsAndArraysNeverBreakAMessage()
        {
            var resolver = new SerializerResolverBuilder<MessageBody>().Build();
            var broken = new List<string>();
            int tested = 0;

            foreach (Type type in typeof(MessageBody).Assembly.GetTypes()
                .Where(t => typeof(MessageBody).IsAssignableFrom(t) && !t.IsAbstract
                            && t.GetConstructor(Type.EmptyTypes) != null)
                .OrderBy(t => t.Name))
            {
                if (CannotBuild.Contains(type.Name))
                {
                    continue;
                }

                object body = Activator.CreateInstance(type);
                Fill(body, 0);

                try
                {
                    ISerializer serializer = resolver.GetSerializer(type);
                    using (var stream = new MemoryStream())
                    using (var writer = new StreamWriter(stream))
                    {
                        serializer.Serialize(writer, new SerializationContext(resolver), body);
                    }

                    tested++;
                }
                catch (Exception exception)
                {
                    while (exception.InnerException != null)
                    {
                        exception = exception.InnerException;
                    }

                    broken.Add(type.Name + ": " + exception.GetType().Name);
                }
            }

            Assert.IsTrue(tested > 100, "the sweep found almost nothing to test - " + tested);
            Assert.AreEqual(
                0,
                broken.Count,
                "these threw with only strings and arrays left null: " + string.Join(", ", broken));
        }

        /// <summary>
        /// Gives every null reference property a value, except the two this is
        /// about.
        /// </summary>
        private static void Fill(object instance, int depth)
        {
            if (instance == null || depth > 4)
            {
                return;
            }

            foreach (PropertyInfo property in instance.GetType().GetProperties())
            {
                if (!property.CanRead || !property.CanWrite)
                {
                    continue;
                }

                Type type = property.PropertyType;
                if (type == typeof(string) || type.IsArray || type.IsValueType)
                {
                    continue;
                }

                object value;
                try
                {
                    value = property.GetValue(instance);
                }
                catch (Exception)
                {
                    continue;
                }

                if (value != null)
                {
                    Fill(value, depth + 1);
                    continue;
                }

                if (type.GetConstructor(Type.EmptyTypes) == null)
                {
                    continue;
                }

                try
                {
                    object made = Activator.CreateInstance(type);
                    property.SetValue(instance, made);
                    Fill(made, depth + 1);
                }
                catch (Exception)
                {
                }
            }
        }
    }
}
