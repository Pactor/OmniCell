#region License

// Copyright (c) 2026, OmniCell contributors
//
// This file is part of OmniCell and is distributed under the terms the project
// is licensed under. See the repository root for details.

#endregion

namespace SmokeLounge.AOtomation.Messaging.Tests
{
    using System.IO;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using SmokeLounge.AOtomation.Messaging.GameData;
    using SmokeLounge.AOtomation.Messaging.Messages;
    using SmokeLounge.AOtomation.Messaging.Messages.N3Messages;
    using SmokeLounge.AOtomation.Messaging.Serialization;

    using StreamWriter = SmokeLounge.AOtomation.Messaging.Serialization.StreamWriter;

    /// <summary>
    /// An array property nobody filled in must not take the message down.
    /// </summary>
    /// <remarks>
    /// Found through teleporting. <see cref="N3TeleportMessage.Trailer"/> is a
    /// byte array behind an int32 count, and neither of the two places the
    /// server builds a teleport ever set it. The array serializer took the
    /// length of it without looking, so every teleport threw a
    /// NullReferenceException part way through writing - with the size
    /// already on the wire - and the destination playfield's mobs were never
    /// sent. Reported as Pactor/OmniCell#1.
    ///
    /// An empty array is the right reading of a null one, and not a guess:
    /// six of the thirty three copies of this field across the captures carry
    /// a count of zero, and they are precisely the six that always survived
    /// the round trip.
    /// </remarks>
    [TestClass]
    public class NullArrayTests
    {
        /// <summary>
        /// A teleport with no trailer set writes, and writes a zero count.
        /// </summary>
        [TestMethod]
        public void TeleportWithNoTrailerSerializes()
        {
            var message = new N3TeleportMessage
                              {
                                  Identity = new Identity { Type = IdentityType.CanbeAffected, Instance = 1 },
                                  Destination = new Vector3 { X = 1f, Y = 2f, Z = 3f },
                                  Heading = new Quaternion { X = 0f, Y = 0f, Z = 0f, W = 1f },
                                  Playfield = new Identity { Type = IdentityType.Playfield1, Instance = 152 },
                                  Playfield2 = Identity.None,
                                  ChangePlayfield = Identity.None,
                              };

            Assert.IsNull(message.Trailer, "the field this is about has to start null");

            byte[] written = Write(message);

            // The count is the last four bytes, and nothing follows it.
            Assert.IsTrue(written.Length >= 4, "nothing was written");
            int count = (written[written.Length - 4] << 24) | (written[written.Length - 3] << 16)
                        | (written[written.Length - 2] << 8) | written[written.Length - 1];
            Assert.AreEqual(0, count, "a null trailer should write a count of zero");
        }

        /// <summary>
        /// And an empty one writes the same bytes as a null one.
        /// </summary>
        /// <remarks>
        /// Which is the point: the fix in the message builders and the guard
        /// in the serializer cannot disagree.
        /// </remarks>
        [TestMethod]
        public void NullTrailerWritesTheSameAsAnEmptyOne()
        {
            var nothing = new N3TeleportMessage
                              {
                                  Identity = new Identity { Type = IdentityType.CanbeAffected, Instance = 1 },
                                  Destination = new Vector3 { X = 1f, Y = 2f, Z = 3f },
                                  Heading = new Quaternion { X = 0f, Y = 0f, Z = 0f, W = 1f },
                                  Playfield = new Identity { Type = IdentityType.Playfield1, Instance = 152 },
                                  Playfield2 = Identity.None,
                                  ChangePlayfield = Identity.None,
                              };

            var empty = new N3TeleportMessage
                            {
                                Identity = new Identity { Type = IdentityType.CanbeAffected, Instance = 1 },
                                Destination = new Vector3 { X = 1f, Y = 2f, Z = 3f },
                                Heading = new Quaternion { X = 0f, Y = 0f, Z = 0f, W = 1f },
                                Playfield = new Identity { Type = IdentityType.Playfield1, Instance = 152 },
                                Playfield2 = Identity.None,
                                ChangePlayfield = Identity.None,
                                Trailer = new byte[0],
                            };

            CollectionAssert.AreEqual(Write(nothing), Write(empty));
        }

        private static byte[] Write(MessageBody message)
        {
            var resolver = new SerializerResolverBuilder<MessageBody>().Build();
            ISerializer serializer = resolver.GetSerializer(message.GetType());

            using (var stream = new MemoryStream())
            {
                using (var writer = new StreamWriter(stream))
                {
                    serializer.Serialize(writer, new SerializationContext(resolver), message);
                }

                return stream.ToArray();
            }
        }
    }
}
