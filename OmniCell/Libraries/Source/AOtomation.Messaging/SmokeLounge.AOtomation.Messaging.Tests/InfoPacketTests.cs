// --------------------------------------------------------------------------------------------------------------------
// <copyright file="InfoPacketTests.cs" company="SmokeLounge">
//   Copyright © 2013 SmokeLounge.
//   This program is free software. It comes without any warranty, to
//   the extent permitted by applicable law. You can redistribute it
//   and/or modify it under the terms of the Do What The Fuck You Want
//   To Public License, Version 2, as published by Sam Hocevar. See
//   http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the InfoPacketTests type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace SmokeLounge.AOtomation.Messaging.Tests
{
    using System.IO;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using SmokeLounge.AOtomation.Messaging.GameData;
    using SmokeLounge.AOtomation.Messaging.Messages;
    using SmokeLounge.AOtomation.Messaging.Messages.N3Messages;
    using SmokeLounge.AOtomation.Messaging.Serialization;

    [TestClass]
    public class InfoPacketTests
    {
        #region Public Methods and Operators

        [TestMethod]
        [DeploymentItem(".\\TestData\\InfoPacket #1 - 0x40")]
        public void Deserialize0x40Test()
        {
            var packet = this.ReadTestPacket("InfoPacket #1 - 0x40");

            var messageSerialzer = new MessageSerializer();
            var actual = this.Deserialize(messageSerialzer, packet);
            var infoPacketMessage = (InfoPacketMessage)actual.Body;
            var infoPacket = infoPacketMessage.Info;

            Assert.AreEqual(InfoPacketFlags.Versioned, infoPacketMessage.Flags);
            Assert.AreEqual(0, infoPacket.OrganizationId);
            Assert.IsNull(infoPacket.OrganizationRank);
            Assert.IsNull(infoPacket.GridDestinations);
            Assert.IsNull(infoPacket.CityPlayfieldId);
            Assert.IsNull(infoPacket.AcgItems);
            Assert.IsNotNull(infoPacket.PvpDuelKills);
        }

        [TestMethod]
        [DeploymentItem(".\\TestData\\InfoPacket #2 - 0x41")]
        public void Deserialize0x41Test()
        {
            var packet = this.ReadTestPacket("InfoPacket #2 - 0x41");

            var messageSerialzer = new MessageSerializer();
            var actual = this.Deserialize(messageSerialzer, packet);
            var infoPacketMessage = (InfoPacketMessage)actual.Body;
            var infoPacket = infoPacketMessage.Info;

            Assert.AreEqual(
                InfoPacketFlags.Versioned | InfoPacketFlags.Organization, infoPacketMessage.Flags);
            Assert.AreNotEqual(0, infoPacket.OrganizationId);
            Assert.IsNotNull(infoPacket.OrganizationRank);
            Assert.IsNull(infoPacket.GridDestinations);
            Assert.IsNotNull(infoPacket.CityPlayfieldId);
            Assert.IsNull(infoPacket.AcgItems);
        }

        [TestMethod]
        [DeploymentItem(".\\TestData\\InfoPacket #3 - 0x43")]
        public void Deserialize0x43Test()
        {
            var packet = this.ReadTestPacket("InfoPacket #3 - 0x43");

            var messageSerialzer = new MessageSerializer();
            var actual = this.Deserialize(messageSerialzer, packet);
            var infoPacketMessage = (InfoPacketMessage)actual.Body;
            var infoPacket = infoPacketMessage.Info;

            Assert.AreEqual(
                InfoPacketFlags.Versioned | InfoPacketFlags.Organization
                | InfoPacketFlags.OrganizationCities,
                infoPacketMessage.Flags);
            Assert.AreNotEqual(0, infoPacket.OrganizationId);
            Assert.IsNotNull(infoPacket.OrganizationRank);
            Assert.IsNotNull(infoPacket.GridDestinations);
            Assert.IsNotNull(infoPacket.CityPlayfieldId);
            Assert.IsNull(infoPacket.AcgItems);
        }

        [TestMethod]
        [DeploymentItem(".\\TestData\\InfoPacket #4 - 0x47")]
        public void Deserialize0x47Test()
        {
            var packet = this.ReadTestPacket("InfoPacket #4 - 0x47");

            var messageSerialzer = new MessageSerializer();
            var actual = this.Deserialize(messageSerialzer, packet);
            var infoPacketMessage = (InfoPacketMessage)actual.Body;
            var infoPacket = infoPacketMessage.Info;

            Assert.AreNotEqual(0, infoPacket.OrganizationId);
            Assert.IsNotNull(infoPacket.OrganizationRank);
            Assert.IsNotNull(infoPacket.GridDestinations);
            Assert.IsNotNull(infoPacket.CityPlayfieldId);
            Assert.IsNotNull(infoPacket.AcgItems);
            Assert.IsNotNull(infoPacket.PvpDuelScore);
        }

        [TestMethod]
        [DeploymentItem(".\\TestData\\InfoPacket #5 - 0x54")]
        public void Deserialize0x54Test()
        {
            var packet = this.ReadTestPacket("InfoPacket #5 - 0x54");

            var messageSerialzer = new MessageSerializer();
            var actual = this.Deserialize(messageSerialzer, packet);
            var infoPacketMessage = (InfoPacketMessage)actual.Body;
            var infoPacket = infoPacketMessage.Info;

            Assert.IsNotNull(infoPacket.AcgItems);
            Assert.IsNull(infoPacket.SuppressionTimer);
            Assert.IsNull(infoPacket.SuppressionLevel);
            Assert.IsNull(infoPacket.PvpDuelKills);
        }

        [TestMethod]
        [DeploymentItem(".\\TestData\\InfoPacket #6 - 0x57")]
        public void Deserialize0x57Test()
        {
            var packet = this.ReadTestPacket("InfoPacket #6 - 0x57");

            var messageSerialzer = new MessageSerializer();
            var actual = this.Deserialize(messageSerialzer, packet);
            var infoPacketMessage = (InfoPacketMessage)actual.Body;
            var infoPacket = infoPacketMessage.Info;

            Assert.AreEqual(
                InfoPacketFlags.Versioned | InfoPacketFlags.NotAPlayer | InfoPacketFlags.HasAcgItems
                | InfoPacketFlags.Suppression,
                infoPacketMessage.Flags);
            Assert.IsNotNull(infoPacket.SuppressionTimer);
            Assert.IsNotNull(infoPacket.SuppressionLevel);
            Assert.IsNull(infoPacket.PvpDuelKills);
        }

        [TestMethod]
        [DeploymentItem(".\\TestData\\InfoPacket #1 - 0x40")]
        public void Roundtrip0x40Test()
        {
            var expected = this.ReadTestPacket("InfoPacket #1 - 0x40");

            var messageSerializer = new MessageSerializer();
            var deserialized = this.Deserialize(messageSerializer, expected);
            var actual = this.Serialize(messageSerializer, deserialized);

            CollectionAssert.AreEqual(expected, actual);
        }

        [TestMethod]
        [DeploymentItem(".\\TestData\\InfoPacket #2 - 0x41")]
        public void Roundtrip0x41Test()
        {
            var expected = this.ReadTestPacket("InfoPacket #2 - 0x41");

            var messageSerializer = new MessageSerializer();
            var deserialized = this.Deserialize(messageSerializer, expected);
            var actual = this.Serialize(messageSerializer, deserialized);

            CollectionAssert.AreEqual(expected, actual);
        }

        [TestMethod]
        [DeploymentItem(".\\TestData\\InfoPacket #3 - 0x43")]
        public void Roundtrip0x43Test()
        {
            var expected = this.ReadTestPacket("InfoPacket #3 - 0x43");

            var messageSerializer = new MessageSerializer();
            var deserialized = this.Deserialize(messageSerializer, expected);
            var actual = this.Serialize(messageSerializer, deserialized);

            CollectionAssert.AreEqual(expected, actual);
        }

        [TestMethod]
        [DeploymentItem(".\\TestData\\InfoPacket #4 - 0x47")]
        public void Roundtrip0x47Test()
        {
            var expected = this.ReadTestPacket("InfoPacket #4 - 0x47");

            var messageSerializer = new MessageSerializer();
            var deserialized = this.Deserialize(messageSerializer, expected);
            var actual = this.Serialize(messageSerializer, deserialized);

            CollectionAssert.AreEqual(expected, actual);
        }

        [TestMethod]
        [DeploymentItem(".\\TestData\\InfoPacket #5 - 0x54")]
        public void Roundtrip0x54Test()
        {
            var expected = this.ReadTestPacket("InfoPacket #5 - 0x54");

            var messageSerializer = new MessageSerializer();
            var deserialized = this.Deserialize(messageSerializer, expected);
            var actual = this.Serialize(messageSerializer, deserialized);

            CollectionAssert.AreEqual(expected, actual);
        }

        [TestMethod]
        [DeploymentItem(".\\TestData\\InfoPacket #6 - 0x57")]
        public void Roundtrip0x57Test()
        {
            var expected = this.ReadTestPacket("InfoPacket #6 - 0x57");

            var messageSerializer = new MessageSerializer();
            var deserialized = this.Deserialize(messageSerializer, expected);
            var actual = this.Serialize(messageSerializer, deserialized);

            CollectionAssert.AreEqual(expected, actual);
        }

        #endregion

        [TestMethod]
        [DeploymentItem(".\\TestData\\InfoPacket #1 - 0x40")]
        public void FactionStandingsAndSuppressionRoundtripTest()
        {
            // Neither block has ever been captured: nothing on the wire has
            // carried flag 0x08 or flag 0x20, so nothing else in this suite
            // exercises them and a mistake in either would sit unnoticed.
            //
            // They are named from the reader rather than from a capture. It
            // files the twelve int32s into stats 561 to 572 - the faction
            // standings - and formats the suppression pair into the localised
            // text keyed TimeUntilGasChanges, "Time until supppression field
            // changes to %u%%: %02d:%02d:%02d", which is what makes the byte a
            // gas percentage selector and the int32 a count of seconds.
            //
            // So this takes a captured record, adds both blocks, and checks the
            // bytes they cost and that they come back. Forty eight for the
            // twelve int32s, five for an int32 and a byte.
            var messageSerializer = new MessageSerializer();
            var captured = this.ReadTestPacket("InfoPacket #1 - 0x40");
            var message = this.Deserialize(messageSerializer, captured);
            var body = (InfoPacketMessage)message.Body;

            Assert.IsNull(body.Info.FactionStandings, "the capture has neither block");
            Assert.IsNull(body.Info.SuppressionTimer);

            body.Flags |= InfoPacketFlags.HasFactionStandings | InfoPacketFlags.Suppression;
            body.Info.SuppressionTimer = 3661;
            body.Info.SuppressionLevel = 2;
            body.Info.FactionStandings = new FactionStandings
                                             {
                                                 ClanSentinels = 1,
                                                 OtMed = 2,
                                                 ClanGaia = 3,
                                                 OtTrans = 4,
                                                 ClanVanguards = 5,
                                                 Gos = 6,
                                                 OtFollowers = 7,
                                                 OtOperator = 8,
                                                 OtUnredeemed = 9,
                                                 ClanDevoted = 10,
                                                 ClanConserver = 11,
                                                 ClanRedeemed = 12
                                             };

            var written = this.Serialize(messageSerializer, message);
            Assert.AreEqual(captured.Length + 48 + 5, written.Length,
                            "twelve int32s and an int32 with a byte");

            var read = (InfoPacketMessage)this.Deserialize(messageSerializer, written).Body;
            Assert.AreEqual(3661, read.Info.SuppressionTimer);
            Assert.AreEqual((byte)2, read.Info.SuppressionLevel);
            Assert.IsNotNull(read.Info.FactionStandings);
            Assert.AreEqual(1, read.Info.FactionStandings.ClanSentinels);
            Assert.AreEqual(6, read.Info.FactionStandings.Gos);
            Assert.AreEqual(12, read.Info.FactionStandings.ClanRedeemed,
                            "the twelve are in stat order, 561 to 572");
        }

        #region Methods

        private Message Deserialize(MessageSerializer messageSerializer, byte[] packet)
        {
            using (var memoryStream = new MemoryStream(packet))
            {
                var actual = messageSerializer.Deserialize(memoryStream);
                return actual;
            }
        }

        private byte[] ReadTestPacket(string path)
        {
            BinaryReader binaryReader = null;

            try
            {
                using (var fileStream = new FileStream(path, FileMode.Open, FileAccess.Read))
                {
                    binaryReader = new BinaryReader(fileStream);
                    var packet = binaryReader.ReadBytes((int)fileStream.Length);
                    binaryReader = null;
                    return packet;
                }
            }
            finally
            {
                if (binaryReader != null)
                {
                    binaryReader.Dispose();
                }
            }
        }

        private byte[] Serialize(MessageSerializer messageSerializer, Message message)
        {
            using (var memoryStream = new MemoryStream())
            {
                messageSerializer.Serialize(memoryStream, message);
                return memoryStream.ToArray();
            }
        }

        #endregion
    }
}