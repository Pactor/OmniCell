// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ProtocolByteAuditTests.cs" company="OmniCell">
//   Copyright © 2026 OmniCell contributors.
//   Added to SmokeLounge.AOtomation.Messaging, which is distributed under the
//   Do What The Fuck You Want To Public License, Version 2, as published by
//   Sam Hocevar. See http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the ProtocolByteAuditTests type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace SmokeLounge.AOtomation.Messaging.Tests
{
    using System;
    using System.IO;
    using System.Text;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using SmokeLounge.AOtomation.Messaging.GameData;
    using SmokeLounge.AOtomation.Messaging.Messages;
    using SmokeLounge.AOtomation.Messaging.Messages.N3Messages;
    using SmokeLounge.AOtomation.Messaging.Messages.SystemMessages;
    using SmokeLounge.AOtomation.Messaging.Serialization;

    [TestClass]
    public class ProtocolByteAuditTests
    {
        [TestMethod]
        public void ClientGetItemMatchesTheLayoutTakenFromTheClient()
        {
            // Never captured - it is a message the client sends and this one has
            // not been sent on any recorded session. The layout is the client's:
            // reader 0x10015285 takes one Identity and the writer at 0x100152A5
            // puts back the same one. What the Identity is comes from
            // N3Msg_GetItem, the only place the message is built, which resolves
            // its argument to an item before handing it to the constructor.
            var body = new ClientGetItemMessage
            {
                Identity = Id(IdentityType.CanbeAffected, unchecked((int)0x0A0B0C01)),
                Unknown = 0,
                Item = Id(IdentityType.WeaponInstance, 0x25C1E0A5)
            };

            AssertRetailPacket(
                "00 12 00 0A 00 01 00 25 0A 0B 0C 01 00 00 00 02 37 13 6C 6B " +
                "00 00 C3 50 0A 0B 0C 01 00 00 00 C7 4A 25 C1 E0 A5",
                body,
                0x0012,
                unchecked((int)0x0A0B0C01),
                2);
        }

        [TestMethod]
        public void ClientContainerAddItemMatchesTheLayoutTakenFromTheClient()
        {
            // Container first, item second. N3Msg_ContainerAddItem hands its
            // first argument to N3Msg_GetContainerInventoryList and its second
            // to N3Msg_IsItemPossibleToUnWear, and the constructor writes them
            // out in that order.
            var body = new ClientContainerAddItemMessage
            {
                Identity = Id(IdentityType.CanbeAffected, unchecked((int)0x0A0B0C01)),
                Unknown = 0,
                Container = Id(IdentityType.WeaponInstance, 0x25C1E0A5),
                Item = Id(IdentityType.WeaponInstance, 0x25C1E0B7)
            };

            AssertRetailPacket(
                "00 12 00 0A 00 01 00 2D 0A 0B 0C 01 00 00 00 02 1F 4D 5F 7E " +
                "00 00 C3 50 0A 0B 0C 01 00 00 00 C7 4A 25 C1 E0 A5 00 00 C7 " +
                "4A 25 C1 E0 B7",
                body,
                0x0012,
                unchecked((int)0x0A0B0C01),
                2);
        }

        [TestMethod]
        public void ClientRequestBuyMatchesTheLayoutTakenFromTheClient()
        {
            // One int32, and the three city requests that carry nothing else -
            // buy, close the window, toggle cloaking - are the same packet with
            // different ids. The value is the instance half of the city's
            // CityID_t, pushed by the sender straight from CityAI_c::GetID.
            var body = new ClientRequestBuyMessage
            {
                Identity = Id(IdentityType.CanbeAffected, unchecked((int)0x0A0B0C01)),
                Unknown = 0,
                CityInstance = 1234
            };

            AssertRetailPacket(
                "00 12 00 0A 00 01 00 21 0A 0B 0C 01 00 00 00 02 55 79 66 02 " +
                "00 00 C3 50 0A 0B 0C 01 00 00 00 04 D2",
                body,
                0x0012,
                unchecked((int)0x0A0B0C01),
                2);
        }

        [TestMethod]
        public void ClientRequestCloseGuiMatchesTheLayoutTakenFromTheClient()
        {
            var body = new ClientRequestCloseGuiMessage
            {
                Identity = Id(IdentityType.CanbeAffected, unchecked((int)0x0A0B0C01)),
                Unknown = 0,
                CityInstance = 1234
            };

            AssertRetailPacket(
                "00 12 00 0A 00 01 00 21 0A 0B 0C 01 00 00 00 02 1B 3C 61 4D " +
                "00 00 C3 50 0A 0B 0C 01 00 00 00 04 D2",
                body,
                0x0012,
                unchecked((int)0x0A0B0C01),
                2);
        }

        [TestMethod]
        public void ClientRqToggleCloakingMatchesTheLayoutTakenFromTheClient()
        {
            var body = new ClientRqToggleCloakingMessage
            {
                Identity = Id(IdentityType.CanbeAffected, unchecked((int)0x0A0B0C01)),
                Unknown = 0,
                CityInstance = 1234
            };

            AssertRetailPacket(
                "00 12 00 0A 00 01 00 21 0A 0B 0C 01 00 00 00 02 3F 5E 4B 46 " +
                "00 00 C3 50 0A 0B 0C 01 00 00 00 04 D2",
                body,
                0x0012,
                unchecked((int)0x0A0B0C01),
                2);
        }

        [TestMethod]
        public void ClientRequestDemolishMatchesTheLayoutTakenFromTheClient()
        {
            // The city id, then a TilePos_c - two int32s, which is what
            // city.dll's own reader for that type takes.
            var body = new ClientRequestDemolishMessage
            {
                Identity = Id(IdentityType.CanbeAffected, unchecked((int)0x0A0B0C01)),
                Unknown = 0,
                CityInstance = 1234,
                Tile = new TilePos { X = 7, Z = 11 }
            };

            AssertRetailPacket(
                "00 12 00 0A 00 01 00 29 0A 0B 0C 01 00 00 00 02 3F 1B 6F 70 " +
                "00 00 C3 50 0A 0B 0C 01 00 00 00 04 D2 00 00 00 07 00 00 00 " +
                "0B",
                body,
                0x0012,
                unchecked((int)0x0A0B0C01),
                2);
        }

        [TestMethod]
        public void ClientRequestBuildMatchesTheLayoutTakenFromTheClient()
        {
            // Five fields, and the last four line up one for one with the four
            // arguments CityAI_c::ClientRequestBuild takes after the requester.
            // The house template and the rotation are the pair the client looks
            // a CityHouseTemplate_c up by; one field is still unnamed.
            //
            // That last one is filled here even though the client itself only
            // ever sends it empty - both call sites into the sender are guarded
            // on it being zero. A layout test wants a value in every field, and
            // a zero there would not show that it is serialized at all.
            var body = new ClientRequestBuildMessage
            {
                Identity = Id(IdentityType.CanbeAffected, unchecked((int)0x0A0B0C01)),
                Unknown = 0,
                CityInstance = 1234,
                HouseTemplate = Id(IdentityType.CityController, 0x00030D41),
                Tile = new TilePos { X = 7, Z = 11 },
                Rotation = 3,
                Unknown3 = Id(IdentityType.CanbeAffected, unchecked((int)0x0A0B0C01))
            };

            AssertRetailPacket(
                "00 12 00 0A 00 01 00 3D 0A 0B 0C 01 00 00 00 02 53 01 14 16 " +
                "00 00 C3 50 0A 0B 0C 01 00 00 00 04 D2 00 00 C4 18 00 03 0D " +
                "41 00 00 00 07 00 00 00 0B 00 00 00 03 00 00 C3 50 0A 0B 0C " +
                "01",
                body,
                0x0012,
                unchecked((int)0x0A0B0C01),
                2);
        }

        [TestMethod]
        public void RelocateDynelsMatchesTheLayoutTakenFromTheClient()
        {
            // Never captured. The layout is the client's and its reader and
            // writer agree: an Identity, then an X3F1 count, then one Identity
            // per entry. Two entries here, so the count on the wire is
            // (2 + 1) * 0x3F1 = 3027.
            var body = new RelocateDynelsMessage
            {
                Identity = Id(IdentityType.CanbeAffected, unchecked((int)0x0A0B0C01)),
                Unknown = 0,
                Destination = Id(IdentityType.CanbeAffected, unchecked((int)0x0A0B0C01)),
                Items = new[]
                {
                    Id(IdentityType.WeaponInstance, 0x25C1E0A5),
                    Id(IdentityType.WeaponInstance, 0x25C1E0B7)
                }
            };

            AssertRetailPacket(
                "00 12 00 0A 00 01 00 39 00 00 0D B8 0A 0B 0C 01 26 4B 51 4B " +
                "00 00 C3 50 0A 0B 0C 01 00 00 00 C3 50 0A 0B 0C 01 00 00 0B " +
                "D3 00 00 C7 4A 25 C1 E0 A5 00 00 C7 4A 25 C1 E0 B7",
                body,
                0x0012,
                0x00000DB8,
                unchecked((int)0x0A0B0C01));
        }

        [TestMethod]
        public void ItemReplacedMatchesTheLayoutTakenFromTheClient()
        {
            // Never captured. A placement and two ACGItem_ts, read through
            // GameData's own stream operator - four int32s each, the last of
            // which the writer emits as a literal zero.
            var body = new ItemReplacedMessage
            {
                Identity = Id(IdentityType.CanbeAffected, unchecked((int)0x0A0B0C01)),
                Unknown = 0,
                Placement = 0x12,
                Old = new AcgItem { LowId = 292235, HighId = 292235, Quality = 1, Unused = 0 },
                New = new AcgItem { LowId = 291082, HighId = 291082, Quality = 5, Unused = 0 }
            };

            AssertRetailPacket(
                "00 12 00 0A 00 01 00 41 00 00 0D B8 0A 0B 0C 01 3A 22 3B 50 " +
                "00 00 C3 50 0A 0B 0C 01 00 00 00 00 12 00 04 75 8B 00 04 75 " +
                "8B 00 00 00 01 00 00 00 00 00 04 71 0A 00 04 71 0A 00 00 00 " +
                "05 00 00 00 00",
                body,
                0x0012,
                0x00000DB8,
                unchecked((int)0x0A0B0C01));
        }

        [TestMethod]
        public void BankCorpseMatchesTheLayoutTakenFromTheClient()
        {
            // Never captured. A container through the shared container reader,
            // then the instance half of the container's identity - the client
            // supplies the 0xDEAE that goes beside it. One entry here, so the
            // count is (1 + 1) * 0x3F1 = 2018.
            var body = new BankCorpseMessage
            {
                Identity = Id(IdentityType.CanbeAffected, unchecked((int)0x0A0B0C01)),
                Unknown = 0,
                Contents = new[]
                {
                    new InventorySlot
                    {
                        Placement = 1,
                        Flags = 1,
                        Count = 1,
                        Identity = Id(IdentityType.WeaponInstance, 0x25C1E0A5),
                        ItemLowId = 292235,
                        ItemHighId = 292235,
                        Quality = 1,
                        Unused = 0
                    }
                },
                Instance = 0x0020D03D
            };

            AssertRetailPacket(
                "00 12 00 0A 00 01 00 45 00 00 0D B8 0A 0B 0C 01 52 21 34 20 " +
                "00 00 C3 50 0A 0B 0C 01 00 00 00 07 E2 00 00 00 01 00 01 00 " +
                "01 00 00 C7 4A 25 C1 E0 A5 00 04 75 8B 00 04 75 8B 00 00 00 " +
                "01 00 00 00 00 00 20 D0 3D",
                body,
                0x0012,
                0x00000DB8,
                unchecked((int)0x0A0B0C01));
        }

        [TestMethod]
        public void BankMatchesTheLayoutTakenFromTheClient()
        {
            // Never captured. The client's reader pushes the container and the
            // stream, calls the shared container reader, and stops - so this is
            // the whole message. The int32 and Identity that used to stand
            // after the list were twelve bytes of our own invention.
            var body = new BankMessage
            {
                Identity = Id(IdentityType.CanbeAffected, unchecked((int)0x0A0B0C01)),
                Unknown = 0,
                Contents = new[]
                {
                    new InventorySlot
                    {
                        Placement = 1,
                        Flags = 3,
                        Count = 1,
                        Identity = Id(IdentityType.WeaponInstance, 0x25C1E0A5),
                        ItemLowId = 292235,
                        ItemHighId = 292235,
                        Quality = 1,
                        Unused = 0
                    }
                }
            };

            AssertRetailPacket(
                "00 12 00 0A 00 01 00 41 00 00 0D B8 0A 0B 0C 01 34 3C 28 7F " +
                "00 00 C3 50 0A 0B 0C 01 00 00 00 07 E2 00 00 00 01 00 03 00 " +
                "01 00 00 C7 4A 25 C1 E0 A5 00 04 75 8B 00 04 75 8B 00 00 00 " +
                "01 00 00 00 00",
                body,
                0x0012,
                0x00000DB8,
                unchecked((int)0x0A0B0C01));
        }

        [TestMethod]
        public void InspectMatchesTheLayoutTakenFromTheClient()
        {
            // Never captured. An Identity and a container, and the client's
            // reader and writer agree on both. The container's page id and the
            // identity it is built with are the client's own doing and are not
            // on the wire.
            var body = new InspectMessage
            {
                Identity = Id(IdentityType.CanbeAffected, unchecked((int)0x0A0B0C01)),
                Unknown = 0,
                Target = Id(IdentityType.CanbeAffected, unchecked((int)0x0A0B0C02)),
                Contents = new[]
                {
                    new InventorySlot
                    {
                        Placement = 6,
                        Flags = 2,
                        Count = 1,
                        Identity = Id(IdentityType.WeaponInstance, 0x25C1E0A5),
                        ItemLowId = 121571,
                        ItemHighId = 121571,
                        Quality = 1,
                        Unused = 0
                    }
                }
            };

            AssertRetailPacket(
                "00 12 00 0A 00 01 00 49 00 00 0D B8 0A 0B 0C 01 5A 58 5F 65 " +
                "00 00 C3 50 0A 0B 0C 01 00 00 00 C3 50 0A 0B 0C 02 00 00 07 " +
                "E2 00 00 00 06 00 02 00 01 00 00 C7 4A 25 C1 E0 A5 00 01 DA " +
                "E3 00 01 DA E3 00 00 00 01 00 00 00 00",
                body,
                0x0012,
                0x00000DB8,
                unchecked((int)0x0A0B0C01));
        }

        [TestMethod]
        public void N3PlayfieldFullUpdateMatchesTheLayoutTakenFromTheClient()
        {
            // Never captured, and it does not need to be: this is the body
            // PlayfieldAnarchyF carries, which has seventeen captures behind
            // it, with the two world-map int32s left off. The bytes below are a
            // Rubi-Ka playfield - version 4, the 0x61 proxy marker, the model
            // and the running instance, and an empty generator identity, which
            // is where the client stops.
            var body = new N3PlayfieldFullUpdateMessage
            {
                Identity = Id(IdentityType.Playfield2, 655),
                Unknown = 0,
                Version = 4,
                CharacterCoordinates = new Vector3 { X = 3609.084f, Y = 35.11f, Z = 785.7675f },
                TokenMarker = 0x61,
                ModelId = Id(IdentityType.Playfield1, 655),
                Group = 0,
                Subgroup = 0,
                PlayfieldId = Id(IdentityType.Playfield2, 655)
            };

            AssertRetailPacket(
                "00 02 00 0A 00 01 00 4E 00 00 0D B8 0A 0B 0C 02 30 16 13 55 " +
                "00 00 9C 50 00 00 02 8F 00 00 00 00 04 45 61 91 58 42 0C 70 " +
                "A4 44 44 71 1F 61 00 00 C7 9C 00 00 02 8F 00 00 00 00 00 00 " +
                "00 00 00 00 9C 50 00 00 02 8F 00 00 00 00 00 00 00 00",
                body,
                0x0002,
                0x00000DB8,
                unchecked((int)0x0A0B0C02));
        }

        [TestMethod]
        public void WaypointPathMatchesTheLayoutTakenFromTheClient()
        {
            // Never captured. An X3F1 list of Vector3s and a float, and the
            // client's reader and writer agree on both. Two waypoints here, so
            // the count is (2 + 1) * 0x3F1 = 3027.
            var body = new WaypointPathMessage
            {
                Identity = Id(IdentityType.CanbeAffected, unchecked((int)0x0A0B0C01)),
                Unknown = 0,
                Waypoints = new[]
                {
                    new Vector3 { X = 3609f, Y = 35f, Z = 785f },
                    new Vector3 { X = 3700f, Y = 40f, Z = 800f }
                },
                Speed = 12.5f
            };

            AssertRetailPacket(
                "00 12 00 0A 00 01 00 3D 00 00 0D B8 0A 0B 0C 01 33 31 20 42 " +
                "00 00 C3 50 0A 0B 0C 01 00 00 00 0B D3 45 61 90 00 42 0C 00 " +
                "00 44 44 40 00 45 67 40 00 42 20 00 00 44 48 00 00 41 48 00 " +
                "00",
                body,
                0x0012,
                0x00000DB8,
                unchecked((int)0x0A0B0C01));
        }

        [TestMethod]
        public void ReloadMatchesTheLayoutTakenFromTheClient()
        {
            // Never captured. Two identities and three int32s, and the client's
            // reader and writer agree on all five. The ammunition identity is
            // an inventory location - page 0x68, slot 0x43 - not an item.
            var body = new ReloadMessage
            {
                Identity = Id(IdentityType.CanbeAffected, unchecked((int)0x0A0B0C01)),
                Unknown = 0,
                Ammo = Id(IdentityType.Inventory, 0x43),
                Weapon = Id(IdentityType.WeaponInstance, 0x25C1E0A5),
                AmmoCount = 0x0C,
                Energy = 100,
                AmmoUsedUp = 0
            };

            AssertRetailPacket(
                "00 12 00 0A 00 01 00 39 00 00 0D B8 0A 0B 0C 01 26 51 5E 61 " +
                "00 00 C3 50 0A 0B 0C 01 00 00 00 00 68 00 00 00 43 00 00 C7 " +
                "4A 25 C1 E0 A5 00 00 00 0C 00 00 00 64 00 00 00 00",
                body,
                0x0012,
                0x00000DB8,
                unchecked((int)0x0A0B0C01));
        }

        [TestMethod]
        public void GfxTriggerDynelToDynelMatchesTheLayoutTakenFromTheClient()
        {
            // Never captured. Shape 6 is the one the client hands to
            // CreateEffect2(int, n3Dynel_t const&, n3Dynel_t const&, int): two
            // identities and the attachment point on the first of them. 1017
            // is "Bip01 L Hand_ac".
            var body = new GfxTriggerMessage
            {
                Identity = Id(IdentityType.CanbeAffected, unchecked((int)0x0A0B0C01)),
                Unknown = 0,
                Selector = 6,
                EffectId = 0x1B5A,
                Target = Id(IdentityType.CanbeAffected, unchecked((int)0x0A0B0C01)),
                SecondTarget = Id(IdentityType.CanbeAffected, unchecked((int)0x0A0B0C02)),
                Attachment = GfxAttachment.LeftHand
            };

            AssertRetailPacket(
                "00 12 00 0A 00 01 00 39 00 00 0D B8 0A 0B 0C 01 7A 22 22 02 " +
                "00 00 C3 50 0A 0B 0C 01 00 00 00 00 06 00 00 1B 5A 00 00 C3 " +
                "50 0A 0B 0C 01 00 00 C3 50 0A 0B 0C 02 00 00 03 F9",
                body,
                0x0012,
                0x00000DB8,
                unchecked((int)0x0A0B0C01));
        }

        [TestMethod]
        public void GfxTriggerPointToPointMatchesTheLayoutTakenFromTheClient()
        {
            // Shape 3 goes to CreateEffect2(int, Vector3 const&, Vector3
            // const&) - two positions and no dynel, so no attachment point.
            var body = new GfxTriggerMessage
            {
                Identity = Id(IdentityType.CanbeAffected, unchecked((int)0x0A0B0C01)),
                Unknown = 0,
                Selector = 3,
                EffectId = 0x1B5A,
                Position = new Vector3 { X = 3609f, Y = 35f, Z = 785f },
                SecondPosition = new Vector3 { X = 3700f, Y = 40f, Z = 800f }
            };

            AssertRetailPacket(
                "00 13 00 0A 00 01 00 3D 00 00 0D B8 0A 0B 0C 01 7A 22 22 02 " +
                "00 00 C3 50 0A 0B 0C 01 00 00 00 00 03 00 00 1B 5A 45 61 90 " +
                "00 42 0C 00 00 44 44 40 00 45 67 40 00 42 20 00 00 44 48 00 " +
                "00",
                body,
                0x0013,
                0x00000DB8,
                unchecked((int)0x0A0B0C01));
        }

        [TestMethod]
        public void PlayfieldTowerUpdateClientMatchesTheLayoutTakenFromTheClient()
        {
            // Never captured, so both halves are the client's: the reader at
            // 0x1012B38A takes an Identity and an int32 and reads a tower
            // record after them only when that int32 is exactly 2, and the
            // dispatcher at 0x1012B350 erases the matching tower on 1 and
            // inserts on anything else.
            //
            // The tower itself is a real one - the values are lifted from a
            // captured PlayfieldAllTowers, so the record half is checked
            // against retail bytes even though the message never was.
            var removed = new PlayfieldTowerUpdateClientMessage
            {
                Identity = Id(IdentityType.Playfield2, 6553),
                Unknown = 0,
                Tower = new Identity { Type = (IdentityType)50004, Instance = 2842832 },
                Action = TowerUpdateAction.Remove
            };

            AssertRetailPacket(
                "00 14 00 0A 00 01 00 29 00 00 0D B8 0A 0B 0C 01 5B 1E 05 2C " +
                "00 00 9C 50 00 00 19 99 00 00 00 C3 54 00 2B 60 D0 00 00 00 " +
                "01",
                removed,
                0x0014,
                0x00000DB8,
                unchecked((int)0x0A0B0C01));

            var added = new PlayfieldTowerUpdateClientMessage
            {
                Identity = Id(IdentityType.Playfield2, 6553),
                Unknown = 0,
                Tower = new Identity { Type = (IdentityType)50004, Instance = 2842833 },
                Action = TowerUpdateAction.Add,
                TowerProxy = new TowerProxyBase
                {
                    TowerFieldIdentity = new Identity { Type = (IdentityType)50004, Instance = 2842833 },
                    OwnerIdentity = Id(IdentityType.CanbeAffected, 2053361656),
                    Coordinates = new Vector3 { X = 1998.913f, Y = 35.11f, Z = 383.0376f },
                    MeshId = 201330,
                    Side = Side.Clan,
                    AnimationId = 201634,
                    Scale = 1.04f,
                    MarkerFlags = 2
                }
            };

            AssertRetailPacket(
                "00 15 00 0A 00 01 00 59 00 00 0D B8 0A 0B 0C 01 5B 1E 05 2C " +
                "00 00 9C 50 00 00 19 99 00 00 00 C3 54 00 2B 60 D1 00 00 00 " +
                "02 00 00 C3 54 00 2B 60 D1 00 00 C3 50 7A 63 CF F8 44 F9 DD " +
                "37 42 0C 70 A4 43 BF 84 D0 00 03 12 72 00 00 00 01 00 03 13 " +
                "A2 3F 85 1E B8 00 00 00 02",
                added,
                0x0015,
                0x00000DB8,
                unchecked((int)0x0A0B0C01));
        }

        [TestMethod]
        public void TrapItemFullUpdateMatchesTheLayoutTakenFromTheClient()
        {
            // Never captured. The reader at 0x100A28CA calls
            // SimpleItemFullUpdate's own at 0x100A1661 for everything the item
            // family shares and then takes five fields of its own: a version
            // against the static at 0x101C21C4, stat 289 trapdifficulty, two
            // Identities and one byte holding two bits.
            //
            // One byte, not two. Bit 1 is Armed and bit 0 is Revealed, so an
            // armed trap the character has not spotted closes the message with
            // 0x02.
            var body = new TrapItemFullUpdateMessage
            {
                Identity = Id(IdentityType.CanbeAffected, unchecked((int)0x0A0B0C01)),
                Unknown = 0,
                MsgVersion = 11,
                Identitytype = 0,
                Instance = 0,
                Coordinate = new Vector3 { X = 3609f, Y = 35f, Z = 785f },
                Heading = new Quaternion { X = 0f, Y = 0f, Z = 0f, W = 1f },
                Playfield = 655,
                Marker = new Identity { Type = (IdentityType)1000015, Instance = 0 },
                InventoryId = 0,
                BodyLocation = 0,
                Stats = new GameTuple<CharacterStat, uint>[0],
                Name = string.Empty,
                TrapVersion = 1,
                TrapDifficulty = 50,
                Unknown1 = Id(IdentityType.CanbeAffected, unchecked((int)0x0A0B0C01)),
                Unknown2 = Identity.None,
                Armed = true,
                Revealed = false
            };

            Assert.AreEqual(2, body.Flags, "armed is bit 1");

            AssertRetailPacket(
                "00 16 00 0A 00 01 00 74 00 00 0D B8 0A 0B 0C 01 59 31 39 28 " +
                "00 00 C3 50 0A 0B 0C 01 00 00 00 00 0B 00 00 00 00 00 00 00 " +
                "00 45 61 90 00 42 0C 00 00 44 44 40 00 00 00 00 00 00 00 00 " +
                "00 00 00 00 00 3F 80 00 00 00 00 02 8F 00 0F 42 4F 00 00 00 " +
                "00 00 00 00 00 03 F1 00 00 00 00 00 00 00 01 00 00 00 32 00 " +
                "00 C3 50 0A 0B 0C 01 00 00 00 00 00 00 00 00 02",
                body,
                0x0016,
                0x00000DB8,
                unchecked((int)0x0A0B0C01));
        }

        [TestMethod]
        public void MineFullUpdateIsATrapWithTwoMoreFields()
        {
            // Never captured either. The reader at 0x100A0DA4 calls
            // TrapItemFullUpdate's whole reader and then takes a version
            // against the static at 0x101C2088 and one unnamed int32.
            //
            // This copy is armed and already spotted, so the shared byte is
            // 0x03 - which is the check that the two bits are one byte and not
            // two.
            var body = new MineFullUpdateMessage
            {
                Identity = Id(IdentityType.CanbeAffected, unchecked((int)0x0A0B0C01)),
                Unknown = 0,
                MsgVersion = 11,
                Identitytype = 0,
                Instance = 0,
                Coordinate = new Vector3 { X = 3609f, Y = 35f, Z = 785f },
                Heading = new Quaternion { X = 0f, Y = 0f, Z = 0f, W = 1f },
                Playfield = 655,
                Marker = new Identity { Type = (IdentityType)1000015, Instance = 0 },
                InventoryId = 0,
                BodyLocation = 0,
                Stats = new GameTuple<CharacterStat, uint>[0],
                Name = string.Empty,
                TrapVersion = 1,
                TrapDifficulty = 50,
                Unknown1 = Id(IdentityType.CanbeAffected, unchecked((int)0x0A0B0C01)),
                Unknown2 = Identity.None,
                Armed = true,
                Revealed = true,
                MineVersion = 1,
                Unknown3 = 0
            };

            Assert.AreEqual(3, body.Flags, "armed and revealed share one byte");

            AssertRetailPacket(
                "00 17 00 0A 00 01 00 7C 00 00 0D B8 0A 0B 0C 01 21 5B 56 78 " +
                "00 00 C3 50 0A 0B 0C 01 00 00 00 00 0B 00 00 00 00 00 00 00 " +
                "00 45 61 90 00 42 0C 00 00 44 44 40 00 00 00 00 00 00 00 00 " +
                "00 00 00 00 00 3F 80 00 00 00 00 02 8F 00 0F 42 4F 00 00 00 " +
                "00 00 00 00 00 03 F1 00 00 00 00 00 00 00 01 00 00 00 32 00 " +
                "00 C3 50 0A 0B 0C 01 00 00 00 00 00 00 00 00 03 00 00 00 01 " +
                "00 00 00 00",
                body,
                0x0017,
                0x00000DB8,
                unchecked((int)0x0A0B0C01));
        }

        [TestMethod]
        public void MarketSendCarriesCreditsWithOrWithoutItems()
        {
            // Never captured, so both halves are the client's. The reader at
            // 0x1002D83D takes an Identity, an int32 and an X3F1 list of
            // Identities that it then trims to eight; the writer emits the
            // same three.
            //
            // The int32 is credits, and GUI.dll 0x10009C40 is what says so: it
            // atois the window's text field, refuses a negative with
            // "Feedback_MarketNoNegative", and clamps what is left to
            // N3Msg_GetSkill(0x3D, 2) - stat 61, cash.
            var withItems = new MarketSendMessage
            {
                Identity = Id(IdentityType.CanbeAffected, 0x0A0B0C06),
                Unknown = 0,
                Sender = Id(IdentityType.CanbeAffected, 0x0A0B0C06),
                Credits = 2500,
                Items = new[]
                {
                    Id(IdentityType.CanbeAffected, 77680959),
                    Id(IdentityType.CanbeAffected, 77680960)
                }
            };

            AssertRetailPacket(
                "00 18 00 0A 00 01 00 3D 0A 0B 0C 06 00 00 0E 13 47 0B 2E 14 " +
                "00 00 C3 50 0A 0B 0C 06 00 00 00 C3 50 0A 0B 0C 06 00 00 09 " +
                "C4 00 00 0B D3 00 00 C3 50 04 A1 51 3F 00 00 C3 50 04 A1 51 " +
                "40",
                withItems,
                0x0018,
                0x0A0B0C06,
                0x00000E13);

            // The guard at GUI.dll 0x10009D8E sends when there is either an
            // item or an amount, so a deposit of pure credits goes out with an
            // empty list - which is what says this int32 is not an item price.
            var creditsOnly = new MarketSendMessage
            {
                Identity = Id(IdentityType.CanbeAffected, 0x0A0B0C06),
                Unknown = 0,
                Sender = Id(IdentityType.CanbeAffected, 0x0A0B0C06),
                Credits = 2500,
                Items = new Identity[0]
            };

            AssertRetailPacket(
                "00 19 00 0A 00 01 00 2D 0A 0B 0C 06 00 00 0E 13 47 0B 2E 14 " +
                "00 00 C3 50 0A 0B 0C 06 00 00 00 C3 50 0A 0B 0C 06 00 00 09 " +
                "C4 00 00 03 F1",
                creditsOnly,
                0x0019,
                0x0A0B0C06,
                0x00000E13);
        }

        [TestMethod]
        public void MailReadsTheThreeShapesRetailActuallySends()
        {
            // The first mail packets ever captured, from the session on
            // 2026-09-11 that bought something from the market. Until that day
            // this message had no C# at all and its id sat in the audit as
            // unreadable, so all three of these are new ground.
            //
            // The same mail three times: the inbox listing that announces it,
            // the body that arrives when it is opened, and an update.
            var listing = new MailMessage
            {
                Identity = Id(IdentityType.CanbeAffected, unchecked((int)0x0A0B0C04)),
                Unknown = 0,
                Kind = MailKind.InboxList,
                Inbox = new[]
                {
                    new MailRecord
                    {
                        MailId = 22176626,
                        Unknown1 = 0,
                        From = "Shop",
                        Subject = "Your Item Purchase!",
                        Sent = 1789164293,
                        Expires = 1820786693,
                        Flags = 0x5C,
                        HasNoAttachment = 1
                    }
                }
            };

            AssertRetailPacket(
                "05 FF 00 0A 00 01 00 57 00 00 0D AD 0A 0B 0C 04 33 3B 28 67 " +
                "00 00 C3 50 0A 0B 0C 04 00 00 00 00 00 07 E2 00 00 00 00 01 " +
                "52 63 72 00 00 00 00 00 04 53 68 6F 70 00 13 59 6F 75 72 20 " +
                "49 74 65 6D 20 50 75 72 63 68 61 73 65 21 6A A4 7B 05 6C 87 " +
                "00 05 00 00 00 5C 01",
                listing,
                0x05FF,
                0x00000DAD,
                unchecked((int)0x0A0B0C04));

            // The same record with the attachment block, which the listing
            // leaves out. The byte that switches it is 0 here and 1 above.
            var body = new MailMessage
            {
                Identity = Id(IdentityType.CanbeAffected, unchecked((int)0x0A0B0C04)),
                Unknown = 0,
                Kind = MailKind.MessageBody,
                Message = new MailRecord
                {
                    MailId = 22176626,
                    Unknown1 = 0,
                    From = "Shop",
                    Subject = "Your Item Purchase!",
                    Sent = 1789164293,
                    Expires = 1820786693,
                    Flags = 0x5C,
                    HasNoAttachment = 0,
                    Amount = 0,
                    Attachment = new AcgItem { LowId = 303475, HighId = 303475, Quality = 1, Unused = 0 },
                    AttachmentCount = 1,
                    Body = "Your purchased item is attached. Enjoy!"
                }
            };

            AssertRetailPacket(
                "06 00 00 0A 00 01 00 94 00 00 0D AD 0A 0B 0C 04 33 3B 28 67 " +
                "00 00 C3 50 0A 0B 0C 04 00 00 02 00 00 00 00 01 52 63 72 00 " +
                "00 00 00 00 04 53 68 6F 70 00 13 59 6F 75 72 20 49 74 65 6D " +
                "20 50 75 72 63 68 61 73 65 21 6A A4 7B 05 6C 87 00 05 00 00 " +
                "00 5C 00 00 00 00 00 00 04 A1 73 00 04 A1 73 00 00 00 01 00 " +
                "00 00 00 00 00 00 01 00 27 59 6F 75 72 20 70 75 72 63 68 61 " +
                "73 65 64 20 69 74 65 6D 20 69 73 20 61 74 74 61 63 68 65 64 " +
                "2E 20 45 6E 6A 6F 79 21",
                body,
                0x0600,
                0x00000DAD,
                unchecked((int)0x0A0B0C04));

            // An update: the same mail id, and 0x5D against the 0x5C the record
            // carried - the flags word with one more bit set.
            var update = new MailMessage
            {
                Identity = Id(IdentityType.CanbeAffected, unchecked((int)0x0A0B0C04)),
                Unknown = 0,
                Kind = MailKind.MailUpdate,
                MailId = 22176626,
                Value = 0x5D
            };

            AssertRetailPacket(
                "06 02 00 0A 00 01 00 2B 00 00 0D AD 0A 0B 0C 04 33 3B 28 67 " +
                "00 00 C3 50 0A 0B 0C 04 00 00 04 00 00 00 00 01 52 63 72 00 " +
                "00 00 5D",
                update,
                0x0602,
                0x00000DAD,
                unchecked((int)0x0A0B0C04));
        }

        [TestMethod]
        public void ServerPosDebugInfoMatchesTheLayoutTakenFromTheClient()
        {
            // Never captured. Three Vector3s and nothing else; the client's
            // reader takes twelve floats and its writer puts back the same
            // twelve.
            var body = new ServerPosDebugInfoMessage
            {
                Identity = Id(IdentityType.CanbeAffected, unchecked((int)0x0A0B0C01)),
                Unknown = 0,
                ServerPosition = new Vector3 { X = 3609f, Y = 35f, Z = 785f },
                SecondPosition = new Vector3 { X = 3610.5f, Y = 35f, Z = 786.25f },
                Unknown1 = new Vector3()
            };

            AssertRetailPacket(
                "00 12 00 0A 00 01 00 41 00 00 0D B8 0A 0B 0C 01 5C 24 04 04 " +
                "00 00 C3 50 0A 0B 0C 01 00 45 61 90 00 42 0C 00 00 44 44 40 " +
                "00 45 61 A8 00 42 0C 00 00 44 44 90 00 00 00 00 00 00 00 00 " +
                "00 00 00 00 00",
                body,
                0x0012,
                0x00000DB8,
                unchecked((int)0x0A0B0C01));
        }

        [TestMethod]
        public void RaidCreatedCarriesNothingAfterTheKind()
        {
            // Never captured. The client reads an int16 and stops unless it is
            // 1; kind 0 raises "Feedback_RaidCreated" and emits the signal the
            // raid window greys its create button on.
            var body = new RaidMessage
            {
                Identity = Id(IdentityType.CanbeAffected, unchecked((int)0x0A0B0C01)),
                Unknown = 0,
                Kind = RaidUpdateType.Created
            };

            AssertRetailPacket(
                "00 13 00 0A 00 01 00 1F 00 00 0D B8 0A 0B 0C 01 3B 3B 28 78 " +
                "00 00 C3 50 0A 0B 0C 01 00 00 00",
                body,
                0x0013,
                0x00000DB8,
                unchecked((int)0x0A0B0C01));
        }

        [TestMethod]
        public void RaidLocksMatchesTheLayoutTakenFromTheClient()
        {
            // Never captured. Kind 1 brings a version that must be 1, a plain
            // int32 count rather than the X3F1 form, and twenty bytes an
            // entry.
            var body = new RaidMessage
            {
                Identity = Id(IdentityType.CanbeAffected, unchecked((int)0x0A0B0C01)),
                Unknown = 0,
                Kind = RaidUpdateType.Locks,
                Version = 1,
                Locks =
                    new[]
                        {
                            new RaidLock
                                {
                                    Playfield = Id(IdentityType.Playfield, 0x0000208C),
                                    Instance = Id(IdentityType.Playfield, 1),
                                    ExpiresAt = 0x4D2FA080
                                },
                            new RaidLock
                                {
                                    Playfield = Id(IdentityType.Playfield, 0x0000208D),
                                    Instance = Id(IdentityType.Playfield, 2),
                                    ExpiresAt = 0x4D2FB4C0
                                }
                        }
            };

            AssertRetailPacket(
                "00 14 00 0A 00 01 00 4F 00 00 0D B8 0A 0B 0C 01 3B 3B 28 78 " +
                "00 00 C3 50 0A 0B 0C 01 00 00 01 00 00 00 01 00 00 00 02 " +
                "00 00 C7 9D 00 00 20 8C 00 00 C7 9D 00 00 00 01 4D 2F A0 80 " +
                "00 00 C7 9D 00 00 20 8D 00 00 C7 9D 00 00 00 02 4D 2F B4 C0",
                body,
                0x0014,
                0x00000DB8,
                unchecked((int)0x0A0B0C01));
        }

        [TestMethod]
        public void SetNameMatchesTheLayoutTakenFromTheClient()
        {
            // Never captured. Slot 1 is the first name, named by the export
            // N3Msg_GetFirstName reading the very member the dispatcher
            // writes. The reader refuses a length outside 1 to 31.
            var body = new SetNameMessage
            {
                Identity = Id(IdentityType.CanbeAffected, unchecked((int)0x0A0B0C01)),
                Unknown = 0,
                NameSlot = 1,
                Unknown1 = 1,
                Name = "YoMomma",
                Unknown2 = Id(IdentityType.CanbeAffected, unchecked((int)0x0A0B0C01)),
                Unknown3 = 0
            };

            AssertRetailPacket(
                "00 15 00 0A 00 01 00 39 00 00 0D B8 0A 0B 0C 01 73 4E 5A 7B " +
                "00 00 C3 50 0A 0B 0C 01 00 00 00 00 01 01 00 00 00 07 59 6F " +
                "4D 6F 6D 6D 61 00 00 C3 50 0A 0B 0C 01 00 00 00 00",
                body,
                0x0015,
                0x00000DB8,
                unchecked((int)0x0A0B0C01));
        }

        [TestMethod]
        public void ScriptMatchesTheLayoutTakenFromTheClient()
        {
            // Never captured. Ten fields, and both strings carry an int16
            // length through the shared helper at Gamecode 0x10038AF8 rather
            // than the int32 most of this protocol uses.
            var body = new ScriptMessage
            {
                Identity = Id(IdentityType.CanbeAffected, unchecked((int)0x0A0B0C01)),
                Unknown = 0,
                ScriptId = 0x1234,
                SupercedeScriptId = 0x1235,
                Command = ScriptCommand.RunIfNear,
                ScriptText = "effect.fx",
                StartTime = 0x12345678,
                Duration = 10000,
                ServerTime = 0x12345600,
                Name = "Nova",
                PositionX = 3609,
                PositionZ = 785
            };

            AssertRetailPacket(
                "00 16 00 0A 00 01 00 4E 00 00 0D B8 0A 0B 0C 01 20 4F 48 71 " +
                "00 00 C3 50 0A 0B 0C 01 00 00 00 12 34 00 00 12 35 00 00 00 " +
                "04 00 09 65 66 66 65 63 74 2E 66 78 12 34 56 78 00 00 27 10 " +
                "12 34 56 00 00 04 4E 6F 76 61 00 00 0E 19 00 00 03 11",
                body,
                0x0016,
                0x00000DB8,
                unchecked((int)0x0A0B0C01));
        }

        [TestMethod]
        public void PlaySoundCountsItsTerminatorLikeRetailDoes()
        {
            // Retail, from the 2026-09-11 capture. The name is 28 characters
            // and the count reads 29, because the client's writer sends
            // strlen + 1 bytes and the last of them is the NUL.
            var body = new PlaySoundMessage
            {
                Identity = Id(IdentityType.CanbeAffected, 0x0A0B0C06),
                Unknown = 1,
                SoundResource = "SM_Sandy_AH_Helpful_Colonist",
                Source = Id(IdentityType.CanbeAffected, 0x0E930C7B)
            };

            AssertRetailPacket(
                "00 77 00 0A 00 01 00 46 00 00 0E 19 0A 0B 0C 06 45 5D 29 38 " +
                "00 00 C3 50 0A 0B 0C 06 01 00 00 00 1D 53 4D 5F 53 61 6E 64 " +
                "79 5F 41 48 5F 48 65 6C 70 66 75 6C 5F 43 6F 6C 6F 6E 69 73 " +
                "74 00 00 00 C3 50 0E 93 0C 7B",
                body,
                0x0077,
                0x00000E19,
                0x0A0B0C06);
        }

        [TestMethod]
        public void MoveItemGlassesMatchesRetailCaptureByteForByte()
        {
            var body = new MoveItemMessage
            {
                Identity = Id(IdentityType.CanbeAffected, unchecked((int)0x0A0B0C01)),
                Unknown = 0,
                Source = Id(IdentityType.Inventory, 0x43),
                Destination = 0x12
            };

            AssertRetailPacket(
                "00 12 00 0A 00 01 00 29 0A 0B 0C 01 00 00 00 02 54 69 37 3F " +
                "00 00 C3 50 0A 0B 0C 01 00 00 00 00 68 00 00 00 43 00 00 00 12",
                body,
                0x0012,
                unchecked((int)0x0A0B0C01),
                2);
        }

        [TestMethod]
        public void MoveItemOtherCapturedVariantsMatchRetailByteForByte()
        {
            AssertMoveItem(
                "00 0F 00 0A 00 01 00 29 0A 0B 0C 01 00 00 00 02 54 69 37 3F " +
                "00 00 C3 50 0A 0B 0C 01 00 00 00 00 68 00 00 00 46 00 00 00 06",
                0x000F,
                unchecked((int)0x0A0B0C01),
                IdentityType.Inventory,
                0x46,
                0x06);

            AssertMoveItem(
                "00 3A 00 0A 00 01 00 29 0A 0B 0C 01 00 00 00 02 54 69 37 3F " +
                "00 00 C3 50 0A 0B 0C 01 00 00 00 00 6B 00 70 00 00 00 00 00 6F",
                0x003A,
                unchecked((int)0x0A0B0C01),
                IdentityType.Backpack,
                0x00700000,
                0x6F);

            AssertMoveItem(
                "02 9A 00 0A 00 01 00 29 0A 0B 0C 01 00 00 00 02 54 69 37 3F " +
                "00 00 C3 50 0A 0B 0C 01 00 00 00 00 68 00 00 00 4F 00 00 00 2B",
                0x029A,
                unchecked((int)0x0A0B0C01),
                IdentityType.Inventory,
                0x4F,
                0x2B);
        }

        [TestMethod]
        public void ContainerAddItemGlassesMatchesRetailCaptureByteForByte()
        {
            var body = new ContainerAddItemMessage
            {
                Identity = Id(IdentityType.CanbeAffected, unchecked((int)0x0A0B0C01)),
                Unknown = 0,
                SourceContainer = Id(IdentityType.Inventory, 0x43),
                Target = Id(IdentityType.CanbeAffected, unchecked((int)0x0A0B0C01)),
                TargetPlacement = 0x12
            };

            AssertRetailPacket(
                "08 22 00 0A 00 01 00 31 00 00 0D B8 0A 0B 0C 01 47 53 7A 24 " +
                "00 00 C3 50 0A 0B 0C 01 00 00 00 00 68 00 00 00 43 00 00 C3 50 " +
                "0A 0B 0C 01 00 00 00 12",
                body,
                0x0822,
                0x0DB8,
                unchecked((int)0x0A0B0C01));
        }

        [TestMethod]
        public void ContainerAddItemCapturedVariantsMatchRetailByteForByte()
        {
            AssertContainerAddItem(
                "00 65 00 0A 00 01 00 31 00 00 0D B8 0A 0B 0C 02 47 53 7A 24 " +
                "00 00 C3 50 0A 0B 0C 02 00 00 00 00 6E 00 00 00 00 00 00 00 6E " +
                "0A 0B 0C 02 00 00 00 6F",
                0x0065,
                0x0DB8,
                unchecked((int)0x0A0B0C02),
                unchecked((int)0x0A0B0C02),
                IdentityType.OverflowWindow,
                0,
                IdentityType.OverflowWindow,
                unchecked((int)0x0A0B0C02),
                0x6F);

            AssertContainerAddItem(
                "4E 6B 00 0A 00 01 00 31 00 00 0D AF 0A 0B 0C 01 47 53 7A 24 " +
                "00 00 C3 50 0A 0B 0C 01 00 00 00 00 6B 00 84 00 00 00 00 C3 50 " +
                "0A 0B 0C 01 00 00 00 6F",
                0x4E6B,
                0x0DAF,
                unchecked((int)0x0A0B0C01),
                unchecked((int)0x0A0B0C01),
                IdentityType.Backpack,
                0x00840000,
                IdentityType.CanbeAffected,
                unchecked((int)0x0A0B0C01),
                0x6F);

            AssertContainerAddItem(
                "55 EB 00 0A 00 01 00 31 00 00 0D AF 0A 0B 0C 01 47 53 7A 24 " +
                "00 00 C3 50 0A 0B 0C 01 00 00 00 00 68 00 00 00 4B 00 00 C3 50 " +
                "0A 0B 0C 01 00 00 00 13",
                0x55EB,
                0x0DAF,
                unchecked((int)0x0A0B0C01),
                unchecked((int)0x0A0B0C01),
                IdentityType.Inventory,
                0x4B,
                IdentityType.CanbeAffected,
                unchecked((int)0x0A0B0C01),
                0x13);
        }

        [TestMethod]
        public void TemplateActionEquipMatchesRetailCaptureByteForByte()
        {
            var body = new TemplateActionMessage
            {
                Identity = Id(IdentityType.CanbeAffected, unchecked((int)0x0A0B0C01)),
                Unknown = 0,
                ItemLowId = 292235,
                ItemHighId = 292235,
                Quality = 1,
                Unknown1 = 1,
                Unknown2 = 6,
                Placement = Id(IdentityType.ArmorPage, 18),
                Unknown3 = 0,
                Unknown4 = 0
            };

            AssertRetailPacket(
                "08 23 00 0A 00 01 00 41 00 00 0D B8 0A 0B 0C 01 35 50 56 44 " +
                "00 00 C3 50 0A 0B 0C 01 00 00 04 75 8B 00 04 75 8B 00 00 00 01 " +
                "00 00 00 01 00 00 00 06 00 00 00 66 00 00 00 12 00 00 00 00 " +
                "00 00 00 00",
                body,
                0x0823,
                0x0DB8,
                unchecked((int)0x0A0B0C01));
        }

        [TestMethod]
        public void TemplateActionTargetedUseMatchesRetailCaptureByteForByte()
        {
            var body = new TemplateActionMessage
            {
                Identity = Id(IdentityType.CanbeAffected, unchecked((int)0x0A0B0C01)),
                Unknown = 0,
                ItemLowId = 291082,
                ItemHighId = 291082,
                Quality = 1,
                Unknown1 = 1,
                Unknown2 = 3,
                Placement = Id(IdentityType.Inventory, 64),
                Unknown3 = (int)IdentityType.CanbeAffected,
                Unknown4 = unchecked((int)0x0A0B0C01)
            };

            AssertRetailPacket(
                "0F 2B 00 0A 00 01 00 41 00 00 0D B8 0A 0B 0C 01 35 50 56 44 " +
                "00 00 C3 50 0A 0B 0C 01 00 00 04 71 0A 00 04 71 0A 00 00 00 01 " +
                "00 00 00 01 00 00 00 03 00 00 00 68 00 00 00 40 00 00 C3 50 " +
                "0A 0B 0C 01",
                body,
                0x0F2B,
                0x0DB8,
                unchecked((int)0x0A0B0C01));
        }

        [TestMethod]
        public void GenericCmdUseRequestAndAcknowledgementMatchRetailCaptureByteForByte()
        {
            int characterId = unchecked((int)0x0A0B0C01);
            var request = GenericCmd(characterId, 1, 0, 1, GenericCmdAction.Use, 0,
                Id(IdentityType.Inventory, 0x41));

            AssertRetailPacket(
                "00 0E 00 0A 00 01 00 3D 0A 0B 0C 01 00 00 00 02 52 52 68 58 " +
                "00 00 C3 50 0A 0B 0C 01 01 00 00 00 00 00 00 00 01 00 00 00 03 " +
                "00 00 00 00 00 00 C3 50 0A 0B 0C 01 00 00 00 68 00 00 00 41",
                request, 0x000E, characterId, 2);

            var acknowledgement = GenericCmd(characterId, 0, 1, 1, GenericCmdAction.Use, 0,
                Id(IdentityType.Inventory, 0x41));

            AssertRetailPacket(
                "06 22 00 0A 00 01 00 3D 00 00 0D B8 0A 0B 0C 01 52 52 68 58 " +
                "00 00 C3 50 0A 0B 0C 01 00 00 00 00 01 00 00 00 01 00 00 00 03 " +
                "00 00 00 00 00 00 C3 50 0A 0B 0C 01 00 00 00 68 00 00 00 41",
                acknowledgement, 0x0622, 0x0DB8, characterId);
        }

        [TestMethod]
        public void GenericCmdTwoIdentityActionsMatchRetailCapturesByteForByte()
        {
            int characterId = unchecked((int)0x0A0B0C01);
            var useItemOnItem = GenericCmd(
                characterId, 1, 0, 8, GenericCmdAction.UseItemOnItem, 0,
                Id(IdentityType.Inventory, 0x45),
                Id(unchecked((IdentityType)0x0000C73D), unchecked((int)0x580D8D14)));

            AssertRetailPacket(
                "00 BB 00 0A 00 01 00 45 0A 0B 0C 01 00 00 00 02 52 52 68 58 " +
                "00 00 C3 50 0A 0B 0C 01 01 00 00 00 00 00 00 00 08 00 00 00 05 " +
                "00 00 00 00 00 00 C3 50 0A 0B 0C 01 00 00 00 68 00 00 00 45 " +
                "00 00 C7 3D 58 0D 8D 14",
                useItemOnItem, 0x00BB, characterId, 2);

            int secondCharacterId = unchecked((int)0x0A0B0C02);
            var useItemOnCharacter = GenericCmd(
                secondCharacterId, 1, 0, 84, GenericCmdAction.UseItemOnCharacter, 0,
                Id(IdentityType.Inventory, 0x52),
                Id(IdentityType.CanbeAffected, unchecked((int)0x7A57370C)));

            AssertRetailPacket(
                "0D 40 00 0A 00 01 00 45 0A 0B 0C 02 00 00 00 02 52 52 68 58 " +
                "00 00 C3 50 0A 0B 0C 02 01 00 00 00 00 00 00 00 54 00 00 00 20 " +
                "00 00 00 00 00 00 C3 50 0A 0B 0C 02 00 00 00 68 00 00 00 52 " +
                "00 00 C3 50 7A 57 37 0C",
                useItemOnCharacter, 0x0D40, secondCharacterId, 2);

            GenericCmdMessage decoded = DeserializeGenericCmd(
                "0D 40 00 0A 00 01 00 45 0A 0B 0C 02 00 00 00 02 52 52 68 58 " +
                "00 00 C3 50 0A 0B 0C 02 01 00 00 00 00 00 00 00 54 00 00 00 20 " +
                "00 00 00 00 00 00 C3 50 0A 0B 0C 02 00 00 00 68 00 00 00 52 " +
                "00 00 C3 50 7A 57 37 0C");
            Assert.AreEqual(GenericCmdAction.UseItemOnCharacter, decoded.Action);
            Assert.AreEqual(2, decoded.Target.Length);
            Assert.AreEqual(IdentityType.Inventory, decoded.Target[0].Type);
            Assert.AreEqual(0x52, decoded.Target[0].Instance);
            Assert.AreEqual(IdentityType.CanbeAffected, decoded.Target[1].Type);
            Assert.AreEqual(unchecked((int)0x7A57370C), decoded.Target[1].Instance);
        }

        [TestMethod]
        public void KnuBotFinishTradeMatchesRetailCaptureByteForByte()
        {
            var body = new KnuBotFinishTradeMessage
            {
                Identity = Id(IdentityType.CanbeAffected, unchecked((int)0x0A0B0C02)),
                Unknown = 0,
                ProtocolVersion = 2,
                Target = Id(IdentityType.CanbeAffected, unchecked((int)0x7A573701)),
                Declined = 0,
                Credits = 0
            };

            AssertRetailPacket(
                "06 5F 00 0A 00 01 00 2F 0A 0B 0C 02 00 00 00 02 55 68 2B 24 " +
                "00 00 C3 50 0A 0B 0C 02 00 00 02 00 00 C3 50 7A 57 37 01 00 " +
                "00 00 00 00 00 00 00",
                body,
                0x065F,
                unchecked((int)0x0A0B0C02),
                2);
        }

        [TestMethod]
        public void ActionSearchCorpseMatchesRetailCaptureByteForByte()
        {
            var body = new ActionMessage
            {
                Identity = Id(unchecked((IdentityType)0x0000C76A), 0x0101180C),
                Unknown = 1,
                FieldMask = 1,
                Action = 102,
                Instigator = Id(IdentityType.None, 0x000B0C02)
            };

            AssertRetailPacket(
                "84 11 00 0A 00 01 00 2D 00 00 0D B8 00 0B 0C 02 20 49 52 7C " +
                "00 00 C7 6A 01 01 18 0C 01 00 00 00 01 00 00 00 66 00 00 00 " +
                "00 00 0B 0C 02",
                body,
                0x8411,
                0x0DB8,
                0x000B0C02);
        }

        [TestMethod]
        public void CharSecSpecAttackBrawlMatchesRetailCaptureByteForByte()
        {
            var body = new CharSecSpecAttackMessage
            {
                Identity = Id(IdentityType.None, 0x005559CD),
                Unknown = 0,
                Target = Id(IdentityType.None, 0x006F4381),
                SpecialAttackSkill = 142
            };

            AssertRetailPacket(
                "42 BF 00 0A 00 01 00 29 00 00 00 00 00 0B 0C 02 51 49 21 20 " +
                "00 00 00 00 00 55 59 CD 00 00 00 00 00 00 6F 43 81 00 00 00 8E",
                body,
                0x42BF,
                0,
                0x000B0C02);
        }

        [TestMethod]
        public void PlaySoundMatchesExtractedClientLayoutByteForByte()
        {
            // The count was 9 for a nine character name until 2026-09-11, and
            // it was wrong: it has to count the terminator. This test was
            // written from our own model rather than from a capture, so it
            // pinned the mistake instead of catching it - see
            // PlaySoundCountsItsTerminatorLikeRetailDoes, which uses retail
            // bytes.
            var body = new PlaySoundMessage
            {
                Identity = Id(IdentityType.CanbeAffected, unchecked((int)0x0A0B0C02)),
                Unknown = 0,
                SoundResource = "sound.wav",
                Source = Id(IdentityType.CanbeAffected, unchecked((int)0x7A573701))
            };

            AssertRetailPacket(
                "00 01 00 0A 00 01 00 33 00 00 0D B8 0A 0B 0C 02 45 5D 29 38 " +
                "00 00 C3 50 0A 0B 0C 02 00 00 00 00 0A 73 6F 75 6E 64 2E 77 " +
                "61 76 00 00 00 C3 50 7A 57 37 01",
                body,
                0x0001,
                0x0DB8,
                unchecked((int)0x0A0B0C02));
        }

        [TestMethod]
        public void UpdateClientVisualMatchesExtractedClientLayoutByteForByte()
        {
            // The opcode in the hex below was 0x45072A0B until 2026-09-11, and
            // it was wrong. No capture carries this message, so the bytes were
            // written from our own enum and the enum's id had never been
            // checked against anything. The client registers the class as
            // UpdateClientVisualIIR_t, the id on the wire is the hash of that
            // name, and the hash is 0x45072A2D - so anything we sent with the
            // old id would not have resolved to a class in the client at all.
            var body = new UpdateClientVisualMessage
            {
                Identity = Id(IdentityType.CanbeAffected, unchecked((int)0x0A0B0C02)),
                Unknown = 0,
                HeadMesh = 123456,
                Race = 1,
                Breed = 2,
                Sex = 3
            };

            AssertRetailPacket(
                "00 02 00 0A 00 01 00 24 00 00 0D B8 0A 0B 0C 02 45 07 2A 2D " +
                "00 00 C3 50 0A 0B 0C 02 00 00 01 E2 40 01 02 03",
                body,
                0x0002,
                0x0DB8,
                unchecked((int)0x0A0B0C02));
        }

        [TestMethod]
        public void CastNanoSpellTargetPresentMatchesRetailCaptureByteForByte()
        {
            var character = Id(IdentityType.CanbeAffected, unchecked((int)0x0A0B0C01));
            var body = new CastNanoSpellMessage
            {
                Identity = character,
                Unknown = 0,
                NanoId = 291081,
                Target = character,
                TargetPresent = 1,
                Caster = character
            };

            AssertRetailPacket(
                "0F 28 00 0A 00 01 00 35 00 00 0D B8 0A 0B 0C 01 25 31 4D 6D " +
                "00 00 C3 50 0A 0B 0C 01 00 00 04 71 09 00 00 C3 50 0A 0B 0C 01 " +
                "00 00 00 01 00 00 C3 50 0A 0B 0C 01",
                body,
                0x0F28,
                0x0DB8,
                unchecked((int)0x0A0B0C01));
        }

        [TestMethod]
        public void OrgInfoPacketMatchesRetailCaptureByteForByte()
        {
            var body = new OrgInfoPacketMessage
            {
                Identity = Id(IdentityType.CanbeAffected, 990727),
                Unknown = 0,
                OrganizationId = 4736,
                Name = "Athen Paladins"
            };

            AssertRetailPacket(
                "07 0E 00 0A 00 01 00 31 00 00 0D AD 0A 0B 0C 02 2E 2A 4A 6B " +
                "00 00 C3 50 00 0F 1E 07 00 00 00 12 80 00 0E 41 74 68 65 6E " +
                "20 50 61 6C 61 64 69 6E 73",
                body,
                0x070E,
                0x0DAD,
                unchecked((int)0x0A0B0C02));
        }

        [TestMethod]
        public void MissedAttackInfoMatchesRetailCaptureByteForByte()
        {
            var body = new MissedAttackInfoMessage
            {
                Identity = Id(IdentityType.None, 723970),
                Unknown = 1,
                WeaponEnergy = -1,
                WeaponSlot = 6,
                Attacker = Id(IdentityType.None, 7291989),
                Target = Id(IdentityType.None, 723970),
                AttackSkillStat = 0
            };

            AssertRetailPacket(
                "83 85 00 0A 00 01 00 39 00 00 0D B8 00 0B 0C 02 5C 65 4B 28 " +
                "00 00 00 00 00 0B 0C 02 01 FF FF FF FF 00 00 00 06 00 00 00 00 " +
                "00 6F 44 55 00 00 00 00 00 0B 0C 02 00 00 00 00",
                body,
                0x8385,
                0x0DB8,
                723970);
        }

        [TestMethod]
        public void AttackInfoMatchesRetailCaptureByteForByte()
        {
            var body = new AttackInfoMessage
            {
                Identity = Id(IdentityType.CanbeAffected, unchecked((int)0x7A6ECE58)),
                Unknown = 0,
                Damage = 11,
                WeaponEnergy = -1,
                WeaponSlot = 0,
                Target = Id(IdentityType.CanbeAffected, unchecked((int)0x0A0B0C02)),
                VisualEffectId = 0,
                DamageType = 3,
                WeaponInstance = unchecked((int)0x4C455731)
            };

            AssertRetailPacket(
                "A4 50 00 0A 00 01 00 3D 00 00 0D B8 00 0B 0C 02 46 00 2F 16 " +
                "00 00 C3 50 7A 6E CE 58 00 00 00 00 0B FF FF FF FF 00 00 00 00 " +
                "00 00 C3 50 0A 0B 0C 02 00 00 00 00 00 00 00 03 4C 45 57 31",
                body,
                0xA450,
                0x0DB8,
                723970);
        }

        [TestMethod]
        public void SpecialAttackInfoMatchesRetailCaptureByteForByte()
        {
            var body = new SpecialAttackInfo
            {
                Identity = Id(IdentityType.None, 5593549),
                Unknown = 0,
                WeaponSlot = 0,
                Damage = 2,
                WeaponEnergy = -1,
                Target = Id(IdentityType.None, 7291777),
                SpecialAttackSkill = 142,
                VisualEffectId = 0
            };

            AssertRetailPacket(
                "42 C0 00 0A 00 01 00 39 00 00 00 00 00 0B 0C 02 75 4F 11 15 " +
                "00 00 00 00 00 55 59 CD 00 00 00 00 00 00 00 00 02 FF FF FF FF " +
                "00 00 00 00 00 6F 43 81 00 00 00 8E 00 00 00 00",
                body,
                0x42C0,
                0,
                723970);
        }

        [TestMethod]
        public void WeatherControlMatchesExtractedClientWireLayoutByteForByte()
        {
            var body = new WeatherControlMessage
            {
                Identity = Id(IdentityType.None, 0x11223344),
                Unknown = 0,
                FadeIn = 0x0102,
                Duration = 0x03040506,
                FadeOut = 0x0708,
                Range = 1.0f,
                WeatherType = 0x09,
                WeatherIntensity = 0x0A,
                Wind = 0x0B,
                Clouds = 0x0C,
                Thunderstrikes = 0x0D,
                Tremors = 0x0E,
                TremorPercentage = 0x0F,
                ThunderstrikePercentage = 0x10,
                CloudColorRed = 0x11,
                CloudColorGreen = 0x12,
                CloudColorBlue = 0x13,
                FogColorRed = 0x14,
                FogColorGreen = 0x15,
                FogColorBlue = 0x16,
                ZBufferVisibility = 0x17,
                Position = new Vector3 { X = 2.0f, Y = 3.0f, Z = 4.0f },
                ElapsedTime = 5.0f
            };

            AssertRetailPacket(
                "12 34 00 0A 00 01 00 48 55 66 77 88 11 22 33 44 0C 5A 5D 6D " +
                "00 00 00 00 11 22 33 44 00 01 02 03 04 05 06 07 08 3F 80 00 00 " +
                "09 0A 0B 0C 0D 0E 0F 10 11 12 13 14 15 16 17 40 00 00 00 40 " +
                "40 00 00 40 80 00 00 40 A0 00 00",
                body,
                0x1234,
                0x55667788,
                0x11223344);
        }

        [TestMethod]
        public void SpecialAttackWeaponMatchesRetailZeroDescriptorPacketByteForByte()
        {
            var body = new SpecialAttackWeaponMessage
            {
                Identity = Id(IdentityType.CanbeAffected, unchecked((int)0x7A6D52C4)),
                Unknown = 0,
                Specials = new SpecialAttack[0],
                CloseCombatInitiative = 266,
                DistanceWeaponInitiative = 266,
                PhysicalProwessInitiative = 266,
                NanoProwessInitiative = 231,
                AggDef = 100
            };

            AssertRetailPacket(
                "05 77 00 0A 00 01 00 35 00 00 0D B8 0A 0B 0C 02 1D 3C 0F 1C " +
                "00 00 C3 50 7A 6D 52 C4 00 00 00 03 F1 00 00 01 0A 00 00 01 0A " +
                "00 00 01 0A 00 00 00 E7 00 00 00 64",
                body,
                0x0577,
                0x0DB8,
                unchecked((int)0x0A0B0C02));
        }

        [TestMethod]
        public void PetCommandAttackMatchesRetailCaptureByteForByte()
        {
            var body = new PetCommandMessage
            {
                Identity = Id(IdentityType.CanbeAffected, unchecked((int)0x0A0B0C02)),
                Unknown = 0,
                Scope = 0,
                Command = 7,
                CommandParameter = 0,
                Pets = new[] { Id(IdentityType.CanbeAffected, unchecked((int)0x7A6FB1F1)) },
                IsPetType15 = 0,
                CommandText = string.Empty
            };

            AssertRetailPacket(
                "00 3C 00 0A 00 01 00 3D 0A 0B 0C 02 00 00 00 02 6B 33 33 03 " +
                "00 00 C3 50 0A 0B 0C 02 00 00 00 00 00 00 00 00 07 00 00 00 00 " +
                "00 00 07 E2 00 00 C3 50 7A 6F B1 F1 00 00 00 00 00 00 00 00",
                body,
                0x003C,
                unchecked((int)0x0A0B0C02),
                2);
        }

        [TestMethod]
        public void FollowTargetPathMatchesRetailCaptureByteForByte()
        {
            // A character walking: the move mode, then a count and that many
            // coordinates. Two here - where it is and where it is going.
            var body = new FollowTargetMessage
            {
                Identity = Id(IdentityType.CanbeAffected, 2052717552),
                Unknown = 0,
                Info = new FollowCoordinateInfo
                {
                    MoveMode = 0x19,
                    CoordinateCount = 2,
                    CurrentCoordinates = new Vector3
                    {
                        X = 3596.879150390625f,
                        Y = 51.744998931884766f,
                        Z = 784.1422119140625f
                    },
                    EndCoordinates = new Vector3
                    {
                        X = 3596.04150390625f,
                        Y = 52.244998931884766f,
                        Z = 783.8493041992188f
                    }
                }
            };

            AssertRetailPacket(
                "00 66 00 0A 00 01 00 38 00 00 0D B8 0A 0B 0C 03 26 0F 36 71 " +
                "00 00 C3 50 7A 59 FB F0 00 01 19 02 45 60 CE 11 42 4E FA E1 " +
                "44 44 09 1A 45 60 C0 AA 42 50 FA E1 44 43 F6 5B",
                body,
                0x0066,
                0x00000DB8,
                unchecked((int)0x0A0B0C03));
        }

        [TestMethod]
        public void FollowTargetFullStopMatchesRetailCaptureByteForByte()
        {
            // A character stopping.
            //
            // The bytes here have now caught two different mistakes. Until
            // 2026-09-10 the model read an Int32 "Dummy1" that was three bytes
            // of this field plus the first byte of X, which shifted every float
            // after it. The fix split it into a byte and three of "alignment",
            // and that was wrong too - it is one float, and 40 00 00 00 in the
            // hex below is 2.0.
            //
            // Reading it as a byte and three zeros reproduces these exact bytes,
            // which is why the second mistake survived 27,466 captured copies.
            // It took a copy carrying 2.5 to show it.
            var body = new FollowTargetMessage
            {
                Identity = Id(IdentityType.CanbeAffected, 2054140730),
                Unknown = 0,
                Info = new FollowTargetInfo
                {
                    MoveType = 0x19,
                    Target = Id((IdentityType)0, 0),
                    Unknown1 = 2f,
                    X = 3622.1884765625f,
                    Y = 51.744998931884766f,
                    Z = 798.478271484375f,
                    CoordinateCount = 0,
                    Coordinates = new Vector3[0]
                }
            };

            AssertRetailPacket(
                "00 80 00 0A 00 01 00 38 00 00 0D B8 0A 0B 0C 03 26 0F 36 71 " +
                "00 00 C3 50 7A 6F B3 3A 00 02 19 00 00 00 00 00 00 00 00 40 " +
                "00 00 00 45 62 63 04 42 4E FA E1 44 47 9E 9C 00",
                body,
                0x0080,
                0x00000DB8,
                unchecked((int)0x0A0B0C03));
        }

        [TestMethod]
        public void PlayfieldAllTowersEmptyMatchesRetailCaptureByteForByte()
        {
            // Arete Landing has no towers, and the live server says so rather
            // than saying nothing: the list is present and empty. The client
            // waits for it either way.
            var body = new PlayfieldAllTowersMessage
            {
                Identity = Id(IdentityType.Playfield2, 2150461),
                Unknown = 1,
                Towers = new TowerProxyBase[0]
            };

            AssertRetailPacket(
                "00 61 00 0A 00 01 00 21 00 00 0D B8 0A 0B 0C 02 55 22 07 26 " +
                "00 00 9C 50 00 20 D0 3D 01 00 00 03 F1",
                body,
                0x0061,
                0x00000DB8,
                unchecked((int)0x0A0B0C02));
        }

        [TestMethod]
        public void PlayfieldAllCitiesEmptyMatchesRetailCaptureByteForByte()
        {
            // The companion to the tower list, and a different shape: the two
            // bytes are a count of payload BYTES, not an X3F1 array header and
            // not a count of houses. Zero means nothing follows at all - the
            // house count lives inside the payload, so it is absent too.
            var body = new PlayfieldAllCitiesMessage
            {
                Identity = Id(IdentityType.Playfield2, 2150461),
                Unknown = 1
            };

            AssertRetailPacket(
                "00 62 00 0A 00 01 00 1F 00 00 0D B8 0A 0B 0C 02 59 21 01 26 " +
                "00 00 9C 50 00 20 D0 3D 01 00 00",
                body,
                0x0062,
                0x00000DB8,
                unchecked((int)0x0A0B0C02));
        }

        [TestMethod]
        public void CharDCMoveMouseTurnMatchesRetailCaptureByteForByte()
        {
            // The most-sent message in the game: 15,142 of them across the
            // captures. Move type 10 is a mouse turn to the right; the heading
            // is a quaternion and only Y and W are non-zero, because a character
            // standing on the ground turns about one axis.
            var body = new CharDCMoveMessage
            {
                Identity = Id(IdentityType.CanbeAffected, 168496130),
                Unknown = 0,
                MoveType = 10,
                Heading = new Quaternion
                {
                    X = 0f,
                    Y = 0.6988572478294373f,
                    Z = 0f,
                    W = 0.7152611613273621f
                },
                Coordinates = new Vector3
                {
                    X = 3609.084228515625f,
                    Y = 52.26499557495117f,
                    Z = 785.7674560546875f
                },
                MillisecondsSincePreviousMove = 0,
                TiltWorldZ = 0,
                TiltLocalX = 0
            };

            AssertRetailPacket(
                "01 5C 00 0A 00 01 00 46 00 00 0D B8 0A 0B 0C 02 54 11 11 23 " +
                "00 00 C3 50 0A 0B 0C 02 00 0A 00 00 00 00 3F 32 E8 4F 00 00 " +
                "00 00 3F 37 1B 5B 45 61 91 59 42 51 0F 5B 44 44 71 1E 00 00 " +
                "00 00 00 00 00 00 00 00 00 00",
                body,
                0x015C,
                0x00000DB8,
                unchecked((int)0x0A0B0C02));
        }

        [TestMethod]
        public void CharDCMoveTiltFieldsAreFloatsNotIntegers()
        {
            // The same mouse turn, with the two trailing fields carrying
            // something. No capture does - the retail server sends zero in all
            // 3,000 - and zero is the one value that cannot tell a float from
            // an int32, which is how they sat in the model as reserved integers
            // until 2026-09-11.
            //
            // The client will not take them as integers: the reader at
            // 0x1006BEB8 reads both with the BinaryStream float operator and
            // puts each through _finite, and the dispatcher hands them to
            // n3Dynel_t::VehicleForwardUpdate as two angles. So the bytes below
            // are 0.5 and -0.25 as IEEE floats, and this test exists to fail if
            // anyone makes them integers again.
            var body = new CharDCMoveMessage
            {
                Identity = Id(IdentityType.CanbeAffected, 168496130),
                Unknown = 0,
                MoveType = 10,
                Heading = new Quaternion
                {
                    X = 0f,
                    Y = 0.6988572478294373f,
                    Z = 0f,
                    W = 0.7152611613273621f
                },
                Coordinates = new Vector3
                {
                    X = 3609.084228515625f,
                    Y = 52.26499557495117f,
                    Z = 785.7674560546875f
                },
                MillisecondsSincePreviousMove = 0,
                TiltWorldZ = 0.5f,
                TiltLocalX = -0.25f
            };

            AssertRetailPacket(
                "01 5C 00 0A 00 01 00 46 00 00 0D B8 0A 0B 0C 02 54 11 11 23 " +
                "00 00 C3 50 0A 0B 0C 02 00 0A 00 00 00 00 3F 32 E8 4F 00 00 " +
                "00 00 3F 37 1B 5B 45 61 91 59 42 51 0F 5B 44 44 71 1E 00 00 " +
                "00 00 3F 00 00 00 BE 80 00 00",
                body,
                0x015C,
                0x00000DB8,
                unchecked((int)0x0A0B0C02));
        }

        [TestMethod]
        public void FormatFeedbackLiteralTextMatchesRetailCaptureByteForByte()
        {
            // Server feedback text. The payload is the client's formatted-string
            // blob, not plain text - the leading run encodes the template - and
            // the trailing integer says which kind of blob it is: 0 for a single
            // literal string like this one, 1 for a template carrying arguments.
            var body = new FormatFeedbackMessage
            {
                Identity = Id(IdentityType.None, 723970),
                Unknown = 1,
                ChatCategory = 0,
                FormattedMessage = "~&!!!\":!!!)<s%HC-12 SecTec: Camera feed activated.",
                PayloadKind = 0
            };

            AssertRetailPacket(
                "77 2F 00 0A 00 01 00 59 00 00 0D B8 00 0B 0C 02 20 6B 4B 73 " +
                "00 00 00 00 00 0B 0C 02 01 00 00 00 00 00 32 7E 26 21 21 21 " +
                "22 3A 21 21 21 29 3C 73 25 48 43 2D 31 32 20 53 65 63 54 65 " +
                "63 3A 20 43 61 6D 65 72 61 20 66 65 65 64 20 61 63 74 69 76 " +
                "61 74 65 64 2E 00 00 00 00",
                body,
                0x772F,
                0x00000DB8,
                0x000B_0C02);
        }

        [TestMethod]
        public void PingRequestAndReplyMatchRetailCapturesByteForByte()
        {
            // A request: originator stamp set, both later stamps zero because
            // nothing has answered it yet.
            AssertRetailSystemPacket(
                "0B 0B 00 0B 00 01 00 28 00 00 0D B8 0A 0B 0C 03 " +
                "00 00 00 01 00 00 00 00 03 E2 92 D5 00 00 00 00 " +
                "00 00 00 00 00 06 87 E9",
                new PingMessage
                {
                    PingObjType = PingMessage.Request,
                    HopCount = 0,
                    OriginatorStamp = 0x03E292D5,
                    ReceiveStamp = 0,
                    TransmitStamp = 0,
                    Sequence = 0x000687E9
                },
                0x0B0B,
                0x00000DB8,
                unchecked((int)0x0A0B0C03));
        }

        /// <summary>
        /// The same assertion for a system message, which carries no identity.
        /// </summary>
        private static void AssertRetailSystemPacket(
            string expectedHex,
            MessageBody body,
            ushort sequence,
            int sender,
            int receiver)
        {
            var message = new Message
            {
                Header = new Header
                {
                    MessageId = sequence,
                    PacketType = body.PacketType,
                    Unknown = 1,
                    Sender = sender,
                    Receiver = receiver
                },
                Body = body
            };

            byte[] actual;
            using (var stream = new MemoryStream())
            {
                new MessageSerializer().Serialize(stream, message);
                actual = stream.ToArray();
            }

            byte[] expected = ParseHex(expectedHex);
            CollectionAssert.AreEqual(
                expected,
                actual,
                "Expected ({0}): {1}{2}Actual ({3}): {4}",
                expected.Length,
                BitConverter.ToString(expected),
                Environment.NewLine,
                actual.Length,
                BitConverter.ToString(actual));
        }

        [TestMethod]
        public void PlayfieldAnarchyFInstancedMatchesRetailCaptureByteForByte()
        {
            // Entering Arete Landing. The message ends with a DbObject that
            // reads itself: identity type 51069 is a
            // TemplatePlayfieldGeneratorData_t, and its body is a run-length
            // table of everything in the playfield that is not a character.
            // Until 2026-09-10 nothing read or wrote it, so this went out 86
            // bytes long where the live server sends 198; until the object was
            // named, its first two fields were being read as PlayfieldX and
            // PlayfieldZ, which are really the closing 0xFFFFFFFF pair.
            //
            // The five runs are arithmetic proof of the shape. Counts 1, 1, 8,
            // 16 and 1 with start indices 0, 1, 2, 10 and 26 - each start is the
            // previous start plus the previous count. The run of eight is
            // VendingMachine and the live server sends exactly eight
            // VendingMachineFullUpdate messages here; the two runs of one are
            // Door and it sends exactly two DoorStatusUpdate messages.
            var body = new PlayfieldAnarchyFMessage
            {
                Identity = Id(IdentityType.Playfield2, 2150461),
                Unknown = 0,
                Version = 4,
                CharacterCoordinates = new Vector3
                {
                    X = 3609.084228515625f,
                    Y = 52.26499557495117f,
                    Z = 785.7674560546875f
                },
                TokenMarker = 0x61,
                ModelId = Id((IdentityType)0x0000C79E, 6553),
                Group = 1,
                Subgroup = 0,
                PlayfieldId = Id(IdentityType.Playfield2, 2150461),
                PlayfieldX = -1,
                PlayfieldZ = -1,
                TemplateGenerator = new PlayfieldTemplateGeneratorData
                {
                    Identity = Id((IdentityType)51069, 1),
                    Revision = 1,
                    Version = 1,
                    Runs = new[]
                    {
                        new PlayfieldDynelRun
                        {
                            Type = IdentityType.Door, Unknown = 1, StartIndex = 0, Count = 1,
                            FirstInstance = 280037385
                        },
                        new PlayfieldDynelRun
                        {
                            Type = (IdentityType)0x0000C73D, Unknown = 1, StartIndex = 1, Count = 1,
                            FirstInstance = 1477021805
                        },
                        new PlayfieldDynelRun
                        {
                            Type = IdentityType.VendingMachine, Unknown = 1, StartIndex = 2, Count = 8,
                            FirstInstance = 320051221
                        },
                        new PlayfieldDynelRun
                        {
                            Type = (IdentityType)0x0000C73D, Unknown = 1, StartIndex = 10, Count = 16,
                            FirstInstance = 1477021806
                        },
                        new PlayfieldDynelRun
                        {
                            Type = IdentityType.Door, Unknown = 1, StartIndex = 26, Count = 1,
                            FirstInstance = 280037386
                        }
                    }
                }
            };

            AssertRetailPacket(
                "00 02 00 0A 00 01 00 C6 00 00 0D B8 0A 0B 0C 02 5F 4B 1A 39 " +
                "00 00 9C 50 00 20 D0 3D 00 00 00 00 04 45 61 91 59 42 51 0F " +
                "5B 44 44 71 1E 61 00 00 C7 9E 00 00 19 99 00 00 00 01 00 00 " +
                "00 00 00 00 9C 50 00 20 D0 3D 00 00 C7 7D 00 00 00 01 00 00 " +
                "00 01 00 00 00 01 00 00 00 05 00 00 C7 48 00 00 00 01 00 00 " +
                "00 00 00 00 00 01 10 B1 08 09 00 00 C7 3D 00 00 00 01 00 00 " +
                "00 01 00 00 00 01 58 09 90 6D 00 00 C7 5B 00 00 00 01 00 00 " +
                "00 02 00 00 00 08 13 13 98 15 00 00 C7 3D 00 00 00 01 00 00 " +
                "00 0A 00 00 00 10 58 09 90 6E 00 00 C7 48 00 00 00 01 00 00 " +
                "00 1A 00 00 00 01 10 B1 08 0A FF FF FF FF FF FF FF FF",
                body,
                0x0002,
                0x00000DB8,
                unchecked((int)0x0A0B0C02));
        }

        [TestMethod]
        public void PlayfieldAnarchyFMissionMatchesRetailCaptureByteForByte()
        {
            // Zoning into a mission. Every mission is a generated playfield, so
            // the DbObject this message ends with is an
            // ACGBuildingGeneratorData_t - identity type 51103 - rather than the
            // TemplatePlayfieldGeneratorData_t an ordinary playfield carries.
            // It is the recipe: thirty by thirty, seed 341, and thirty two
            // pieces the client assembles the mission out of.
            //
            // Until 2026-09-10 this was the one captured copy that could not be
            // read. 215 of its 301 bytes went unread, because the six fields
            // that stood where the object begins only happen to add up for an
            // ordinary playfield.
            //
            // PlayfieldX and PlayfieldZ are both -1: a mission is not anywhere
            // on the world map, which is why the client draws its marker from
            // the mission rather than from here.
            var body = new PlayfieldAnarchyFMessage
            {
                Identity = Id(IdentityType.Playfield2, 2150799),
                Unknown = 0,
                Version = 4,
                CharacterCoordinates = new Vector3
                {
                    X = 1.801025390625f,
                    Y = 5.010000228881836f,
                    Z = 135.010009765625f
                },
                TokenMarker = 0x61,
                ModelId = Id((IdentityType)51103, 14598183),
                Group = 0,
                Subgroup = 0,
                PlayfieldId = Id(IdentityType.Playfield2, 2150799),
                PlayfieldX = -1,
                PlayfieldZ = -1,
                Generator = new BuildingGeneratorData
                {
                    Identity = Id((IdentityType)51103, 14598183),
                    Revision = 2,
                    Version = 3,
                    Width = 30,
                    Height = 30,
                    WorldHeight = 64,
                    TemplatePlayfield = 341,
                    AmbientRed = 30,
                    AmbientGreen = 30,
                    AmbientBlue = 30,
                    Rooms = new[]
                    {
                            new BuildingRoomInfo { Room = 17, Floor = 0, X = 0, Z = 16, Rotation = 3 },
                            new BuildingRoomInfo { Room = 102, Floor = 0, X = 1, Z = 14, Rotation = 1 },
                            new BuildingRoomInfo { Room = 31, Floor = 0, X = 5, Z = 15, Rotation = 1 },
                            new BuildingRoomInfo { Room = 34, Floor = 0, X = 3, Z = 20, Rotation = 0 },
                            new BuildingRoomInfo { Room = 0, Floor = 0, X = 6, Z = 12, Rotation = 3 },
                            new BuildingRoomInfo { Room = 38, Floor = 0, X = 1, Z = 14, Rotation = 3 },
                            new BuildingRoomInfo { Room = 31, Floor = 0, X = 4, Z = 16, Rotation = 2 },
                            new BuildingRoomInfo { Room = 33, Floor = 0, X = 7, Z = 17, Rotation = 3 },
                            new BuildingRoomInfo { Room = 100, Floor = 0, X = 2, Z = 23, Rotation = 3 },
                            new BuildingRoomInfo { Room = 96, Floor = 0, X = 6, Z = 9, Rotation = 2 },
                            new BuildingRoomInfo { Room = 33, Floor = 0, X = 9, Z = 14, Rotation = 2 },
                            new BuildingRoomInfo { Room = 39, Floor = 0, X = 0, Z = 14, Rotation = 0 },
                            new BuildingRoomInfo { Room = 0, Floor = 0, X = 7, Z = 18, Rotation = 0 },
                            new BuildingRoomInfo { Room = 11, Floor = 0, X = 5, Z = 18, Rotation = 0 },
                            new BuildingRoomInfo { Room = 54, Floor = 0, X = 5, Z = 17, Rotation = 3 },
                            new BuildingRoomInfo { Room = 9, Floor = 0, X = 5, Z = 16, Rotation = 1 },
                            new BuildingRoomInfo { Room = 56, Floor = 0, X = 5, Z = 23, Rotation = 0 },
                            new BuildingRoomInfo { Room = 12, Floor = 0, X = 3, Z = 26, Rotation = 3 },
                            new BuildingRoomInfo { Room = 14, Floor = 0, X = 5, Z = 26, Rotation = 1 },
                            new BuildingRoomInfo { Room = 54, Floor = 0, X = 2, Z = 24, Rotation = 3 },
                            new BuildingRoomInfo { Room = 59, Floor = 0, X = 7, Z = 24, Rotation = 1 },
                            new BuildingRoomInfo { Room = 14, Floor = 0, X = 1, Z = 23, Rotation = 3 },
                            new BuildingRoomInfo { Room = 12, Floor = 0, X = 4, Z = 23, Rotation = 1 },
                            new BuildingRoomInfo { Room = 13, Floor = 0, X = 6, Z = 11, Rotation = 3 },
                            new BuildingRoomInfo { Room = 59, Floor = 0, X = 9, Z = 10, Rotation = 1 },
                            new BuildingRoomInfo { Room = 13, Floor = 0, X = 8, Z = 11, Rotation = 2 },
                            new BuildingRoomInfo { Room = 59, Floor = 0, X = 7, Z = 8, Rotation = 0 },
                            new BuildingRoomInfo { Room = 11, Floor = 0, X = 6, Z = 10, Rotation = 2 },
                            new BuildingRoomInfo { Room = 51, Floor = 0, X = 10, Z = 16, Rotation = 1 },
                            new BuildingRoomInfo { Room = 12, Floor = 0, X = 1, Z = 15, Rotation = 1 },
                            new BuildingRoomInfo { Room = 11, Floor = 0, X = 10, Z = 19, Rotation = 1 },
                            new BuildingRoomInfo { Room = 14, Floor = 0, X = 7, Z = 21, Rotation = 2 }
                    }
                }
            };

            AssertRetailPacket(
                "00 02 00 0A 00 01 01 2D 00 00 0D B1 0A 0B 0C 08 5F 4B 1A 39 "  +
                "00 00 9C 50 00 20 D1 8F 00 00 00 00 04 3F E6 88 00 40 A0 51 "  +
                "EC 43 07 02 90 61 00 00 C7 9F 00 DE C0 27 00 00 00 00 00 00 "  +
                "00 00 00 00 9C 50 00 20 D1 8F 00 00 C7 9F 00 DE C0 27 00 00 "  +
                "00 02 00 03 00 1E 00 1E 00 40 00 00 01 55 1E 1E 1E 00 00 00 "  +
                "20 00 11 00 00 10 03 00 66 00 01 0E 01 00 1F 00 05 0F 01 00 "  +
                "22 00 03 14 00 00 00 00 06 0C 03 00 26 00 01 0E 03 00 1F 00 "  +
                "04 10 02 00 21 00 07 11 03 00 64 00 02 17 03 00 60 00 06 09 "  +
                "02 00 21 00 09 0E 02 00 27 00 00 0E 00 00 00 00 07 12 00 00 "  +
                "0B 00 05 12 00 00 36 00 05 11 03 00 09 00 05 10 01 00 38 00 "  +
                "05 17 00 00 0C 00 03 1A 03 00 0E 00 05 1A 01 00 36 00 02 18 "  +
                "03 00 3B 00 07 18 01 00 0E 00 01 17 03 00 0C 00 04 17 01 00 "  +
                "0D 00 06 0B 03 00 3B 00 09 0A 01 00 0D 00 08 0B 02 00 3B 00 "  +
                "07 08 00 00 0B 00 06 0A 02 00 33 00 0A 10 01 00 0C 00 01 0F "  +
                "01 00 0B 00 0A 13 01 00 0E 00 07 15 02 FF FF FF FF FF FF FF "  +
                "FF",
                body,
                0x0002,
                0x00000DB1,
                0x0A0B0C08);
        }

        [TestMethod]
        public void HealthDamageEnergyHitMatchesRetailCaptureByteForByte()
        {
            // A character taking 24 energy damage from itself - a reflect or a
            // damage shield, which is the only way the source and the message
            // identity are the same on a hit.
            //
            // The last three fields are what this test is for. 92 is Energy
            // in the client's own damage type table, DeathCause is None because
            // nobody died, and the trailing int32 is zero because nothing here
            // dealt its damage through an item the client had to name.
            var body = new HealthDamageMessage
            {
                Identity = Id(IdentityType.CanbeAffected, 0x0A0B0C07),
                Unknown = 0,
                Health = 620,
                Delta = -24,
                DamageType = DamageType.Energy,
                DeathCause = DeathCause.None,
                Source = Id(IdentityType.CanbeAffected, 0x0A0B0C07),
                SourceItem = 0
            };

            AssertRetailPacket(
                "03 CA 00 0A 00 01 00 39 00 00 0D AD 0A 0B 0C 07 37 10 25 6C " +
                "00 00 C3 50 0A 0B 0C 07 00 00 00 02 6C FF FF FF E8 00 00 00 " +
                "5C 00 00 00 00 00 00 C3 50 0A 0B 0C 07 00 00 00 00",
                body,
                0x03CA,
                0x00000DAD,
                0x0A0B0C07);
        }

        [TestMethod]
        public void AoTransportSignalMatchesExtractedClientLayoutByteForByte()
        {
            // No capture holds one of these, so the layout is the client's word
            // rather than the wire's - but there is very little of it to get
            // wrong. AOTransportSignalIIR_c's reader at Gamecode.dll 0x1012FAAB
            // takes an int32 and then the rest of the stream, by asking the
            // stream for its size and taking the difference from where it has
            // got to; the writer at 0x1012FA7E emits the int32 and then the
            // body's bytes with no length in front of them.
            //
            // So the body is length-implicit and this test is really about
            // that: eight bytes of body and nothing saying eight.
            var body = new AoTransportSignalMessage
            {
                Identity = Id(IdentityType.CanbeAffected, 0x0A0B0C07),
                Unknown = 0,
                Signal = 7,
                Payload = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08 }
            };

            AssertRetailPacket(
                "01 00 00 0A 00 01 00 29 00 00 0D AD 0A 0B 0C 07 62 74 1E 15 " +
                "00 00 C3 50 0A 0B 0C 07 00 00 00 00 07 01 02 03 04 05 06 07 " +
                "08",
                body,
                0x0100,
                0x00000DAD,
                0x0A0B0C07);
        }

        [TestMethod]
        public void WeaponItemFullUpdateWithDelayStatsMatchesRetailCaptureByteForByte()
        {
            // A weapon is a SimpleItemFullUpdate under another id: its reader
            // calls that one and returns what it says, with no tail of its own.
            // This is the nine stat variant - the two extra are 294 itemdelay
            // and 210 rechargedelay, how long the weapon takes to swing and how
            // long before it can swing again.
            //
            // The nine are what this test is really for. They used to be a run
            // of fixed named fields, so what reads here as stat 701 with the
            // value 4 was a field called AcgItemLevel holding 701 and a field
            // called QualityLevel holding 4, and the X3F1 count in front of
            // them was Unknown6 holding 10090.
            var body = new WeaponItemFullUpdateMessage
            {
                Identity = Id(IdentityType.WeaponInstance, 633164592),
                Unknown = 0,
                MsgVersion = 11,
                Identitytype = (int)IdentityType.CanbeAffected,
                Instance = 2052536159,
                Playfield = 2150461,
                StateMachine = Id((IdentityType)1000015, 0),
                InventoryId = 1,
                BodyLocation = 6,
                Stats = new[]
                {
                    Stat(CharacterStat.Flags, 1027),
                    Stat((CharacterStat)23, 123362),
                    Stat((CharacterStat)701, 4),
                    Stat((CharacterStat)702, 123362),
                    Stat((CharacterStat)703, 123363),
                    Stat((CharacterStat)412, 1),
                    Stat((CharacterStat)26, unchecked((uint)-1)),
                    Stat((CharacterStat)294, 235),
                    Stat((CharacterStat)210, 235)
                },
                Name = string.Empty
            };

            AssertRetailPacket(
                "00 07 00 0A 00 01 00 87 00 00 0D B8 0A 0B 0C 02 3B 1D 22 68 " +
                "00 00 C7 4A 25 BD 53 30 00 00 00 00 0B 00 00 C3 50 7A 57 37 " +
                "5F 00 20 D0 3D 00 0F 42 4F 00 00 00 00 01 06 00 00 27 6A 00 " +
                "00 00 00 00 00 04 03 00 00 00 17 00 01 E1 E2 00 00 02 BD 00 " +
                "00 00 04 00 00 02 BE 00 01 E1 E2 00 00 02 BF 00 01 E1 E3 00 " +
                "00 01 9C 00 00 00 01 00 00 00 1A FF FF FF FF 00 00 01 26 00 " +
                "00 00 EB 00 00 00 D2 00 00 00 EB 00 00 00 00",
                body,
                0x0007,
                0x00000DB8,
                unchecked((int)0x0A0B0C02));
        }

        [TestMethod]
        public void ZoneLoginPresentsTheLoginTicketByteForByte()
        {
            // The client's first word to the zone server. It carries the ticket
            // the login server issued: in one capture the login server on port
            // 7505 sends ZoneInfo with Cookie1 0x0A0B0D01 and Cookie2
            // 0x0A0B0D02 at t=32.4s, and the client presents exactly those
            // eight bytes to the zone on 7512 at t=36.6s. Nothing read them
            // until 2026-09-10, so this was modelled as 24 bytes where the
            // client sends 32.
            AssertRetailSystemPacket(
                "00 01 00 01 00 01 00 20 0A 0B 0C 02 00 00 00 02 " +
                "00 00 00 1B 0A 0B 0C 02 0A 0B 0D 01 0A 0B 0D 02",
                new ZoneLoginMessage
                {
                    CharacterId = unchecked((int)0x0A0B0C02),
                    Cookie1 = 0x0A0B0D01,
                    Cookie2 = 0x0A0B0D02
                },
                0x0001,
                unchecked((int)0x0A0B0C02),
                2);
        }

        [TestMethod]
        public void ChestItemFullUpdateMatchesRetailCaptureByteForByte()
        {
            // The lootable container a corpse leaves behind. Sixteen bytes at
            // the end went unread until 2026-09-10, so every one of these was
            // written short.
            var body = new ChestItemFullUpdateMessage
            {
                Identity = Id((IdentityType)0x0000C749, 0x0BB2BA87),
                Unknown = 0,
                Owner = Id(IdentityType.CanbeAffected, unchecked((int)0x7A5559C1)),
                MsgVersion = 11,
                Identitytype = 50000,
                Instance = unchecked((int)0x7A5559C1),
                Coordinates = null,
                Heading = null,
                PlayfieldId = 2150461,
                Marker = Id((IdentityType)ItemMessageConstants.ItemMessageMarker, 0),
                InventoryId = 3,
                BodyLocation = 19,
                Stats = new[]
                {
                    new GameTuple<CharacterStat, uint> { Value1 = CharacterStat.Flags, Value2 = 67108865 },
                    new GameTuple<CharacterStat, uint>
                        { Value1 = CharacterStat.StaticInstance, Value2 = 296977 },
                    new GameTuple<CharacterStat, uint> { Value1 = CharacterStat.ACGItemLevel, Value2 = 1 },
                    new GameTuple<CharacterStat, uint>
                        { Value1 = CharacterStat.ACGItemTemplateID, Value2 = 296977 },
                    new GameTuple<CharacterStat, uint>
                        { Value1 = CharacterStat.ACGItemTemplateID2, Value2 = 296977 },
                    new GameTuple<CharacterStat, uint> { Value1 = CharacterStat.MultipleCount, Value2 = 1 }
                },
                Name = string.Empty,
                TailVersion = 2,
                LockDifficulty = 50,
                Keyholders = new Identity[0],
                TailEndVersion = 3
            };

            AssertRetailPacket(
                "00 18 00 0A 00 01 00 7F 00 00 0D B8 0A 0B 0C 02 46 5A 5D 73 " +
                "00 00 C7 49 0B B2 BA 87 00 00 00 00 0B 00 00 C3 50 7A 55 59 " +
                "C1 00 20 D0 3D 00 0F 42 4F 00 00 00 00 03 13 00 00 1B 97 00 " +
                "00 00 00 04 00 00 01 00 00 00 17 00 04 88 11 00 00 02 BD 00 " +
                "00 00 01 00 00 02 BE 00 04 88 11 00 00 02 BF 00 04 88 11 00 " +
                "00 01 9C 00 00 00 01 00 00 00 00 00 00 00 02 00 00 00 32 00 " +
                "00 03 F1 00 00 00 03",
                body,
                0x0018,
                0x00000DB8,
                unchecked((int)0x0A0B0C02));
        }

        [TestMethod]
        public void SimpleItemFullUpdateNameKeepsItsTerminatorByteForByte()
        {
            // An item name on the wire is length-prefixed and null-terminated,
            // and the length counts the terminator: "Mission key to Alien
            // Mothership" is thirty one characters and the length is thirty two.
            //
            // The reader used to trim the terminator, so the writer put back a
            // length of thirty one and dropped the byte - the client was handed
            // a name with nothing marking its end. Two of 747 captured copies
            // caught it, because every other captured item name is empty.
            var body = new SimpleItemFullUpdateMessage
            {
                Identity = Id((IdentityType)0x0000C76D, 0x00FDE5D1),
                Unknown = 0,
                Owner = Id(IdentityType.CanbeAffected, unchecked((int)0x7A555990)),
                MsgVersion = 11,
                Identitytype = 50000,
                Instance = unchecked((int)0x7A555990),
                Coordinate = null,
                Heading = null,
                Playfield = 655,
                Marker = Id((IdentityType)ItemMessageConstants.ItemMessageMarker, 0),
                InventoryId = 1,
                BodyLocation = 72,
                Stats = new[]
                {
                    new GameTuple<CharacterStat, uint> { Value1 = CharacterStat.Flags, Value2 = 0x80000205 },
                    new GameTuple<CharacterStat, uint>
                        { Value1 = CharacterStat.StaticInstance, Value2 = 28577 },
                    new GameTuple<CharacterStat, uint> { Value1 = CharacterStat.ACGItemLevel, Value2 = 1 },
                    new GameTuple<CharacterStat, uint>
                        { Value1 = CharacterStat.ACGItemTemplateID, Value2 = 28577 },
                    new GameTuple<CharacterStat, uint>
                        { Value1 = CharacterStat.ACGItemTemplateID2, Value2 = 28577 },
                    new GameTuple<CharacterStat, uint> { Value1 = CharacterStat.MultipleCount, Value2 = 1 }
                },
                Name = "Mission key to Alien Mothership"
            };

            AssertRetailPacket(
                "00 E2 00 0A 00 01 00 8F 00 00 0D AD 0A 0B 0C 01 3B 11 25 6F " +
                "00 00 C7 6D 00 FD E5 D1 00 00 00 00 0B 00 00 C3 50 7A 55 59 " +
                "90 00 00 02 8F 00 0F 42 4F 00 00 00 00 01 48 00 00 1B 97 00 " +
                "00 00 00 80 00 02 05 00 00 00 17 00 00 6F A1 00 00 02 BD 00 " +
                "00 00 01 00 00 02 BE 00 00 6F A1 00 00 02 BF 00 00 6F A1 00 " +
                "00 01 9C 00 00 00 01 00 00 00 20 4D 69 73 73 69 6F 6E 20 6B " +
                "65 79 20 74 6F 20 41 6C 69 65 6E 20 4D 6F 74 68 65 72 73 68 " +
                "69 70 00",
                body,
                0x00E2,
                0x00000DAD,
                unchecked((int)0x0A0B0C01));
        }

        [TestMethod]
        public void QuestFullUpdateSurvivesARetailCaptureUnchanged()
        {
            // A round trip rather than a construction: this message is five
            // hundred bytes of quest text and six integers nobody can explain,
            // so building one from scratch would prove transcription and not
            // understanding. What is locked here is the string convention - the
            // quest text is length-prefixed and null-terminated with the length
            // counting the terminator, which is why every captured copy used to
            // come out a byte short.
            AssertRetailRoundTrip(
                "0D E6 00 0A 00 01 02 01 00 00 0D B8 0A 0B 0C 02 46 5A 40 61 " +
                "00 00 C3 50 0A 0B 0C 02 01 00 00 07 E2 00 00 DA C3 55 CF 18 " +
                "32 00 00 00 0F 00 00 00 00 00 00 00 00 00 00 00 02 54 61 6C " +
                "6B 20 74 6F 20 56 61 75 67 68 6E 20 48 61 6D 6D 6F 6E 64 00 " +
                "00 00 00 74 54 61 6C 6B 20 74 6F 20 56 61 75 67 68 6E 20 48 " +
                "61 6D 6D 6F 6E 64 3C 42 52 3E 3C 42 52 3E 59 6F 75 72 20 49 " +
                "44 20 63 61 72 64 20 69 73 20 66 69 6E 61 6C 6C 79 20 63 6F " +
                "6D 70 6C 65 74 65 21 20 54 61 6C 6B 20 74 6F 20 56 61 75 67 " +
                "68 6E 20 48 61 6D 6D 6F 6E 64 20 61 62 6F 75 74 20 6C 65 61 " +
                "76 69 6E 67 20 41 72 65 74 65 20 4C 61 6E 64 69 6E 67 2E 00 " +
                "00 00 C3 50 7A 57 37 08 00 00 00 06 00 00 04 10 00 00 00 00 " +
                "00 00 0A 24 00 00 03 F1 00 00 03 F1 00 00 03 F1 33 59 36 4B " +
                "00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 " +
                "00 00 00 00 00 00 00 00 00 00 00 00 00 00 C3 50 0A 0B 0C 02 " +
                "00 03 BC 52 00 00 00 00 00 00 00 00 00 00 07 E2 00 00 00 18 " +
                "00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 01 11 D3 " +
                "00 01 9A 58 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 " +
                "00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 " +
                "00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 " +
                "00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 D2 F1 " +
                "4D A5 8F 82 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 " +
                "00 00 00 00 00 00 00 00 00 00 00 00 00 00 07 E2 00 00 C3 50 " +
                "0A 0B 0C 02 00 00 00 01 05 A5 8F 82 00 00 00 00 00 00 00 00 " +
                "00 00 00 06 00 00 07 E2 00 00 C3 50 0A 0B 0C 02 00 00 00 00 " +
                "00 01 9A 58 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 " +
                "00 00 00 00 00 00 00 07 00 00 03 F1 01 ");
        }

        /// <summary>
        /// Read a captured packet and write it back; the bytes must not move.
        /// </summary>
        /// <remarks>
        /// Weaker than building a message from scratch, and worth saying so: it
        /// proves the model loses nothing, not that we could produce the packet
        /// unaided. For a message whose fields are still unnamed that is the
        /// honest limit of what a test can claim.
        /// </remarks>
        [TestMethod]
        public void SimpleCharFullUpdateCatTextureListCostsEightBytesAnEntry()
        {
            // Flag 0x40000000 is empty in every session on disk, so nothing
            // else here exercises the list behind it and a mistake in it would
            // sit unnoticed. It is named from the client: the dispatcher walks
            // the list at 0x10078A8F and hands each entry to
            // VisualCATMesh_t::SetCATTexture, whose first argument selects the
            // texture being replaced and whose second is the replacement.
            //
            // The entries are two int32s read with the shared Identity reader,
            // which is why they were modelled as Identities. This adds two of
            // them to a captured update and checks what they cost - the X3F1
            // count and eight bytes an entry - and that they come back.
            byte[] captured = ParseHex(
"00 44 00 0A 00 01 01 0E 00 00 03 56 00 00 00 20 " +
                "27 1B 3A 6B 00 00 C3 50 7A 57 37 24 00 3A 00 00 " +
                "5A C0 00 00 19 99 45 58 F6 8F 41 10 28 F6 44 57 " +
                "49 79 00 00 00 00 3F 7E 48 E9 00 00 00 00 BD EC " +
                "8B 44 00 00 07 28 07 54 61 69 6C 6F 72 00 10 28 " +
                "12 01 00 00 00 00 00 00 00 0A 00 00 00 00 00 05 " +
                "00 00 00 00 00 00 00 00 00 00 00 00 00 8E 2F 90 " +
                "00 00 00 65 DC 00 74 00 1F 00 00 00 00 2A 80 00 " +
                "00 00 00 00 00 00 80 00 00 00 03 01 00 01 00 01 " +
                "00 01 00 01 00 00 00 03 00 00 00 00 00 00 00 00 " +
                "00 00 00 00 00 00 00 00 00 00 9E BB 05 00 00 03 " +
                "F1 00 00 17 A6 00 00 00 00 00 00 00 00 00 00 00 " +
                "00 00 00 00 01 00 00 78 8E 00 00 00 00 00 00 00 " +
                "02 00 00 9F C7 00 00 00 00 00 00 00 03 00 00 78 " +
                "77 00 00 00 00 00 00 00 04 00 00 78 A6 00 00 00 " +
                "00 00 00 0B D3 00 00 00 9E BB 00 00 00 00 04 01 " +
                "00 00 1E 61 00 00 00 00 02 00 00 00 00 00");

            Message message;
            using (var stream = new MemoryStream(captured))
            {
                message = new MessageSerializer().Deserialize(stream);
            }

            var body = (SimpleCharFullUpdateMessage)message.Body;
            Assert.IsNull(body.CatTextures, "the capture carries none");

            body.CatTextures = new[]
                                   {
                                       new CatTexture { CatId = 0x48283, TextureId = 0x48281 },
                                       new CatTexture { CatId = -1, TextureId = 0x48282 }
                                   };

            byte[] written;
            using (var stream = new MemoryStream())
            {
                new MessageSerializer().Serialize(stream, message);
                written = stream.ToArray();
            }

            Assert.AreEqual(captured.Length + 4 + 16, written.Length,
                            "an X3F1 count and eight bytes an entry");

            Message again;
            using (var stream = new MemoryStream(written))
            {
                again = new MessageSerializer().Deserialize(stream);
            }

            var read = (SimpleCharFullUpdateMessage)again.Body;
            Assert.IsNotNull(read.CatTextures);
            Assert.AreEqual(2, read.CatTextures.Length);
            Assert.AreEqual(0x48283, read.CatTextures[0].CatId);
            Assert.AreEqual(0x48281, read.CatTextures[0].TextureId);
            Assert.AreEqual(-1, read.CatTextures[1].CatId,
                            "the first argument is signed - a negative id sends "
                            + "SetCATTexture down its by-name path");
        }

        [TestMethod]
        public void AppearanceSplitsIntoTheSixFieldsTheClientReads()
        {
            // The packed word is one int32 on the wire, so a round trip cannot
            // tell a wrong split from a right one - which is how Race stayed
            // five bits too wide across 230 captured copies, sweeping the field
            // above it into itself.
            //
            // The client splits it at 0x1007921B onward: bits 0-2 side, 3-4
            // fatness, 5-7 breed, 8-9 gender, 10-11 race, 12-16 the field it
            // pushes as stat 423, currentstate. Every boundary below is one of
            // those, and the two halves have to agree in both directions.
            var appearance = new Appearance
                                 {
                                     Side = (Side)5,
                                     Fatness = (Fatness)2,
                                     Breed = (Breed)6,
                                     Gender = (Gender)3,
                                     Race = 2,
                                     CurrentState = 0x1B
                                 };

            const uint Packed = 5 | (2u << 3) | (6u << 5) | (3u << 8) | (2u << 10) | (0x1Bu << 12);
            Assert.AreEqual(Packed, appearance.Value, "the six fields pack into one word");

            var reread = new Appearance { Value = Packed };
            Assert.AreEqual((Side)5, reread.Side);
            Assert.AreEqual((Fatness)2, reread.Fatness);
            Assert.AreEqual((Breed)6, reread.Breed);
            Assert.AreEqual((Gender)3, reread.Gender);
            Assert.AreEqual(2u, reread.Race, "race is two bits, not everything above bit 9");
            Assert.AreEqual(0x1Bu, reread.CurrentState, "bits 12 to 16 are stat 423");

            // The captured shapes: an appearance with the state clear is the
            // common case, and 7, 10 and 11 are the three values seen in it.
            foreach (var state in new uint[] { 0, 7, 10, 11 })
            {
                var one = new Appearance { Race = 1, CurrentState = state };
                Assert.AreEqual(state, new Appearance { Value = one.Value }.CurrentState);
                Assert.AreEqual(1u, new Appearance { Value = one.Value }.Race);
            }
        }

        [TestMethod]
        public void SimpleCharFullUpdateWeaponPairAndParentDynelKeepTheirWidths()
        {
            // Two more fields no session on disk carries. The weapon pair list
            // is flag 0x100 and the parent dynel is flag 0x20, and both were
            // renamed on 2026-09-11 from what the model called them - a
            // fighting target that turned out to be the dynel the character is
            // attached to, and a third int32 that turned out to be the key the
            // weapon is filed under in the character's WeaponHolder_t, the same
            // key AttackInfo's weapon-instance override looks up.
            //
            // A pair is four int32s and the reader stores the third and fourth
            // swapped, which is a detail the writer has to get right and no
            // capture would catch.
            byte[] captured = ParseHex(
"00 44 00 0A 00 01 01 0E 00 00 03 56 00 00 00 20 " +
                "27 1B 3A 6B 00 00 C3 50 7A 57 37 24 00 3A 00 00 " +
                "5A C0 00 00 19 99 45 58 F6 8F 41 10 28 F6 44 57 " +
                "49 79 00 00 00 00 3F 7E 48 E9 00 00 00 00 BD EC " +
                "8B 44 00 00 07 28 07 54 61 69 6C 6F 72 00 10 28 " +
                "12 01 00 00 00 00 00 00 00 0A 00 00 00 00 00 05 " +
                "00 00 00 00 00 00 00 00 00 00 00 00 00 8E 2F 90 " +
                "00 00 00 65 DC 00 74 00 1F 00 00 00 00 2A 80 00 " +
                "00 00 00 00 00 00 80 00 00 00 03 01 00 01 00 01 " +
                "00 01 00 01 00 00 00 03 00 00 00 00 00 00 00 00 " +
                "00 00 00 00 00 00 00 00 00 00 9E BB 05 00 00 03 " +
                "F1 00 00 17 A6 00 00 00 00 00 00 00 00 00 00 00 " +
                "00 00 00 00 01 00 00 78 8E 00 00 00 00 00 00 00 " +
                "02 00 00 9F C7 00 00 00 00 00 00 00 03 00 00 78 " +
                "77 00 00 00 00 00 00 00 04 00 00 78 A6 00 00 00 " +
                "00 00 00 0B D3 00 00 00 9E BB 00 00 00 00 04 01 " +
                "00 00 1E 61 00 00 00 00 02 00 00 00 00 00");

            Message message;
            using (var stream = new MemoryStream(captured))
            {
                message = new MessageSerializer().Deserialize(stream);
            }

            var body = (SimpleCharFullUpdateMessage)message.Body;
            Assert.IsNull(body.ParentDynel, "the capture carries neither");
            Assert.IsNull(body.NoWeaponPairs);

            body.ParentDynel = Id(IdentityType.CanbeAffected, unchecked((int)0x0A0B0C02));
            body.NoWeaponPairs = new[]
                                     {
                                         new WeaponPair
                                             {
                                                 ItemLowId = 300523,
                                                 ItemHighId = 300524,
                                                 WeaponInstanceKey = 0x4C455731,
                                                 SourceKey = 0x4C455731
                                             }
                                     };

            byte[] written;
            using (var stream = new MemoryStream())
            {
                new MessageSerializer().Serialize(stream, message);
                written = stream.ToArray();
            }

            Assert.AreEqual(captured.Length + 8 + 4 + 16, written.Length,
                            "an Identity, an X3F1 count and sixteen bytes for the pair");

            Message again;
            using (var stream = new MemoryStream(written))
            {
                again = new MessageSerializer().Deserialize(stream);
            }

            var read = (SimpleCharFullUpdateMessage)again.Body;
            Assert.AreEqual(unchecked((int)0x0A0B0C02), read.ParentDynel.Value.Instance);
            Assert.AreEqual(1, read.NoWeaponPairs.Length);
            Assert.AreEqual(300523, read.NoWeaponPairs[0].ItemLowId);
            Assert.AreEqual(300524, read.NoWeaponPairs[0].ItemHighId);
            Assert.AreEqual(0x4C455731, read.NoWeaponPairs[0].WeaponInstanceKey,
                            "LEW1 - the equipper key Equip_LeetBite1 carries");
            Assert.AreEqual(0x4C455731, read.NoWeaponPairs[0].SourceKey);
        }

        [TestMethod]
        public void WeaponPairSourceKeyIsFourCharactersAndTheKeyIsASkillForInnateAttacks()
        {
            // The two int32s behind the templates were Unknown3 and Unknown4
            // for as long as the model existed, and they are equal in nine of
            // the twelve captured entries, so nothing in a round trip can tell
            // them apart. Three entries separate them, and this pins the shape
            // those three have.
            //
            // A character's innate attacks arrive as weapon pairs like any
            // other: the templates name the Martial Arts, Dimach and Brawl
            // items in the client's resource database, the source key is the
            // four-character code the server spells for each - MAAT, DIIT,
            // BRAW - and the instance key is the skill's stat id rather than a
            // code, because there is no equipper to name. 100 is MartialArts,
            // 144 Dimach, 142 Brawl, and 100 is the value the client singles
            // out at 0x1006B2A8.
            var innate = new[]
                             {
                                 new WeaponPair
                                     {
                                         ItemLowId = 43712,
                                         ItemHighId = 144745,
                                         WeaponInstanceKey = 100,
                                         SourceKey = 0x4D414154
                                     },
                                 new WeaponPair
                                     {
                                         ItemLowId = 42033,
                                         ItemHighId = 42032,
                                         WeaponInstanceKey = 144,
                                         SourceKey = 0x44494954
                                     },
                                 new WeaponPair
                                     {
                                         ItemLowId = 70292,
                                         ItemHighId = 70293,
                                         WeaponInstanceKey = 142,
                                         SourceKey = 0x42524157
                                     }
                             };

            var expected = new[] { "MAAT", "DIIT", "BRAW" };
            for (var i = 0; i < innate.Length; i++)
            {
                var code = BitConverter.GetBytes(innate[i].SourceKey);
                Array.Reverse(code);
                Assert.AreEqual(expected[i], Encoding.ASCII.GetString(code),
                                "the source key is four printable capitals, big endian");
                Assert.IsTrue(innate[i].WeaponInstanceKey < 1000,
                              "the key of an innate attack is a stat id, not a code");
                Assert.AreNotEqual(innate[i].SourceKey, innate[i].WeaponInstanceKey);
            }

            // And the ordinary case, where the two agree because the equipper
            // record is both what supplied the weapon and what it is filed
            // under.
            var granted = new WeaponPair
                              {
                                  ItemLowId = 120910,
                                  ItemHighId = 120911,
                                  WeaponInstanceKey = 0x4C455731,
                                  SourceKey = 0x4C455731
                              };
            Assert.AreEqual(granted.SourceKey, granted.WeaponInstanceKey,
                            "LeetBite1_001 and _400, granted by Equip_LeetBite1");
        }

        [TestMethod]
        public void QuestAlternativeHeaderIsTwentyTwoBytesInTheClientsOrder()
        {
            // The mission terminal, and the only packet in the set that had no
            // byte table at all until 2026-09-12. Four captures exist and they
            // round trip, but every one of them carries five missions with the
            // same shape, so a round trip cannot tell a wrong width from a
            // right one on the fields in front of them.
            //
            // The reader is explicit about those: a version byte, a difficulty
            // byte, six dimension bytes read one at a time by GameData's own
            // operator, an int32 seed, an originator byte, the terminal's
            // Identity, and the mission count. Twenty two bytes, in that
            // order. This builds one with no missions and checks both the
            // total and that each byte lands where the client looks for it.
            var body = new QuestAlternativeMessage
            {
                Identity = Id(IdentityType.CanbeAffected, 0x0A0B0C09),
                Unknown = 1,
                VersionId = 4,
                Difficulty = 11,
                GoodBad = 11,
                ControlledLackingControl = 52,
                OpenHidden = 43,
                PhysicalMystical = 229,
                ExplosivePatient = 92,
                MoneyExperience = 254,
                Seed = 0x74309A1D,
                Originator = QuestOriginator.NeutralBooth,
                MissionTerminalIdentity = Id((IdentityType)56001, unchecked((int)0xC000024F)),
                QuestInfos = new QuestAlternativeEntry[0]
            };

            var message = new Message
            {
                Header = new Header
                {
                    MessageId = 0x1234,
                    PacketType = PacketType.N3Message,
                    Unknown = 1,
                    Sender = 0x0DB8,
                    Receiver = 0x0A0B0C09
                },
                Body = body
            };

            byte[] written;
            using (var stream = new MemoryStream())
            {
                new MessageSerializer().Serialize(stream, message);
                written = stream.ToArray();
            }

            // 29 bytes of header and pass-on flag, then the twenty two.
            Assert.AreEqual(29 + 22, written.Length,
                            "a version, a difficulty, six dimensions, a seed, "
                            + "an originator, an Identity and a count");

            Assert.AreEqual(4, written[29], "version");
            Assert.AreEqual(11, written[30], "difficulty");

            var dimensions = new byte[] { 11, 52, 43, 229, 92, 254 };
            for (var i = 0; i < dimensions.Length; i++)
            {
                Assert.AreEqual(dimensions[i], written[31 + i],
                                "dimension " + i + " is one byte, in GetDimensionName order");
            }

            // The seed is big-endian like everything else on this wire.
            Assert.AreEqual(0x74, written[37]);
            Assert.AreEqual(0x30, written[38]);
            Assert.AreEqual(0x9A, written[39]);
            Assert.AreEqual(0x1D, written[40]);

            Assert.AreEqual(1, written[41], "NeutralBooth");
            Assert.AreEqual(0, written[50], "no missions");

            Message again;
            using (var stream = new MemoryStream(written))
            {
                again = new MessageSerializer().Deserialize(stream);
            }

            var read = (QuestAlternativeMessage)again.Body;
            Assert.AreEqual(0x74309A1D, read.Seed);
            Assert.AreEqual(QuestOriginator.NeutralBooth, read.Originator);
            Assert.AreEqual(229, read.PhysicalMystical,
                            "the dimensions are signed on the wire - 229 is minus 27 - "
                            + "and are carried raw because the serializer has no sbyte");
            Assert.AreEqual(unchecked((int)0xC000024F), read.MissionTerminalIdentity.Instance);
        }

        [TestMethod]
        public void QuestOriginatorPairsEachBoothWithItsTeamVersion()
        {
            // GameData's own enum. GetQuestOriginatorName at GameData.dll
            // 0x10002D0A is a table of nine strings indexed by it, and
            // IsTeamOriginator at 0x10002D23 answers yes to the even values,
            // which is what makes each odd value a solo booth and the even one
            // above it the team version of the same booth.
            //
            // Zero is in the enum and never on the wire: the reader refuses
            // anything outside 1 to 8 and prints those bounds, which are the
            // constants pushed at 0x100CA062.
            Assert.AreEqual(0, (byte)QuestOriginator.Unknown);

            var pairs = new[]
                            {
                                new[] { QuestOriginator.NeutralBooth, QuestOriginator.NeutralBoothTeam },
                                new[] { QuestOriginator.OmniBooth, QuestOriginator.OmniBoothTeam },
                                new[] { QuestOriginator.ClanBooth, QuestOriginator.ClanBoothTeam }
                            };

            foreach (var pair in pairs)
            {
                Assert.AreEqual(1, (byte)pair[1] - (byte)pair[0], "the team value follows its booth");
                Assert.AreEqual(1, (byte)pair[0] % 2, "a solo originator is odd");
                Assert.AreEqual(0, (byte)pair[1] % 2, "a team originator is even");
            }

            foreach (QuestOriginator value in Enum.GetValues(typeof(QuestOriginator)))
            {
                Assert.IsTrue((byte)value <= 8, "the reader refuses anything above eight");
            }
        }

        [TestMethod]
        public void GameTimeCarriesOneFloatAndThreeIntegers()
        {
            // The fourth field was typed float in this model and is an int32
            // on the wire. Both are four bytes, so every capture round trips
            // through the wrong type without complaint and no test in this
            // suite could have caught it - which is how it survived a note on
            // the packet page saying it was wrong.
            //
            // The reader settles it. Gamecode 0x1003977A takes one float
            // through the stream's float operator at 0x101540CC and then three
            // int32s through 0x101540BC, and the dispatcher at 0x10039800
            // loads only the first with fld before calling
            // GameTime_t::Update(float, DayPeriod_e, int, int).
            //
            // So this writes a value whose float and integer readings differ,
            // and checks the bytes. 1201445800 is the bit pattern of
            // 80183.3125f, which is what our own server was putting in the
            // field while it was a float.
            var body = new GameTimeMessage
            {
                Identity = Id(IdentityType.None, 0),
                Unknown = 1,
                CurrentGameTime = 30024.0f,
                DayPeriod = DayPeriod.Dusk,
                CurrentGameDay = 185408,
                SystemTimeReference = 1201445800
            };

            var message = new Message
            {
                Header = new Header
                {
                    MessageId = 0x1234,
                    PacketType = PacketType.N3Message,
                    Unknown = 1,
                    Sender = 0x0DB8,
                    Receiver = 0x0A0B0C09
                },
                Body = body
            };

            byte[] written;
            using (var stream = new MemoryStream())
            {
                new MessageSerializer().Serialize(stream, message);
                written = stream.ToArray();
            }

            Assert.AreEqual(29 + 16, written.Length, "a float and three int32s");

            // 30024.0f is 0x46EA9000 big-endian, and the day period, the day
            // and the reference follow it as plain integers.
            var expected = new byte[]
                               {
                                   0x46, 0xEA, 0x90, 0x00,
                                   0x00, 0x00, 0x00, 0x02,
                                   0x00, 0x02, 0xD4, 0x40,
                                   0x47, 0x9C, 0x9B, 0xA8
                               };
            for (var i = 0; i < expected.Length; i++)
            {
                Assert.AreEqual(expected[i], written[29 + i],
                                "byte " + i + " of the body");
            }

            Message again;
            using (var stream = new MemoryStream(written))
            {
                again = new MessageSerializer().Deserialize(stream);
            }

            var read = (GameTimeMessage)again.Body;
            Assert.AreEqual(30024.0f, read.CurrentGameTime);
            Assert.AreEqual(DayPeriod.Dusk, read.DayPeriod);
            Assert.AreEqual(185408, read.CurrentGameDay);
            Assert.AreEqual(1201445800, read.SystemTimeReference,
                            "an int32, not the float whose bits these are");
        }

        private static void AssertRetailRoundTrip(string capturedHex)
        {
            byte[] expected = ParseHex(capturedHex);

            Message message;
            using (var stream = new MemoryStream(expected))
            {
                message = new MessageSerializer().Deserialize(stream);
            }

            Assert.IsNotNull(message, "the captured packet did not deserialize at all");

            byte[] actual;
            using (var stream = new MemoryStream())
            {
                new MessageSerializer().Serialize(stream, message);
                actual = stream.ToArray();
            }

            CollectionAssert.AreEqual(
                expected,
                actual,
                "Expected ({0}): {1}{2}Actual ({3}): {4}",
                expected.Length,
                BitConverter.ToString(expected),
                Environment.NewLine,
                actual.Length,
                BitConverter.ToString(actual));
        }

        [TestMethod]
        public void KnuBotCloseChatWindowCarriesItsReasonByteForByte()
        {
            // The server closing a conversation and saying why. The reason was
            // modelled as a bare integer, and it was the length of a string
            // nothing read - so the twenty two captured copies that say
            // something were written as a length with nothing after it.
            var body = new KnuBotCloseChatWindowMessage
            {
                Identity = Id((IdentityType)0, 0x000B0C02),
                Unknown = 0,
                Version = 2,
                Target = Id((IdentityType)0, 0x00573706),
                Seconds = 5,
                Reason =
                    "You are too far away from ICC Immigration Officer Bill to continue this conversation."
            };

            AssertRetailPacket(
                "80 84 00 0A 00 01 00 84 00 00 0D B8 00 0B 0C 02 27 0A 4C 62 " +
                "00 00 00 00 00 0B 0C 02 00 00 02 00 00 00 00 00 57 37 06 00 " +
                "00 00 05 00 00 00 55 59 6F 75 20 61 72 65 20 74 6F 6F 20 66 " +
                "61 72 20 61 77 61 79 20 66 72 6F 6D 20 49 43 43 20 49 6D 6D " +
                "69 67 72 61 74 69 6F 6E 20 4F 66 66 69 63 65 72 20 42 69 6C " +
                "6C 20 74 6F 20 63 6F 6E 74 69 6E 75 65 20 74 68 69 73 20 63 " +
                "6F 6E 76 65 72 73 61 74 69 6F 6E 2E",
                body,
                0x8084,
                0x00000DB8,
                0x000B0C02);
        }

        [TestMethod]
        public void KnuBotRejectedItemsEmptyListMatchesRetailCaptureByteForByte()
        {
            // The list of items the bot would not take is counted by a plain
            // Int32 that nothing was reading, so every captured copy came out
            // four bytes short. All thirty two carry an empty list, which means
            // the entries themselves are still untested by anything.
            var body = new KnuBotRejectedItemsMessage
            {
                Identity = Id((IdentityType)0, 0x000B0C02),
                Unknown = 0,
                Version = 2,
                Target = Id((IdentityType)0, 0x00573706),
                Items = new KnuBotRejectedItem[0],
                CashReturned = 0
            };

            AssertRetailPacket(
                "7F C4 00 0A 00 01 00 2F 00 00 0D B8 00 0B 0C 02 2D 21 24 07 " +
                "00 00 00 00 00 0B 0C 02 00 00 02 00 00 00 00 00 57 37 06 00 " +
                "00 00 00 00 00 00 00",
                body,
                0x7FC4,
                0x00000DB8,
                0x000B0C02);
        }

        [TestMethod]
        public void SimpleCharFullUpdateNpcMatchesRetailCaptureByteForByte()
        {
            // Rex Larsson, a quest NPC in Arete Landing, built from nothing and
            // compared against the 244 bytes the live server sent for him.
            //
            // The whole message is a transcription of the client reader at
            // Gamecode.dll 0x1007916D; this is the check that the transcription
            // is right in the direction that matters, since the server only ever
            // sends this message and the client only ever receives it.
            //
            // Every conditional branch this packet takes is a flag below:
            // HasPlayfieldId, HasHeading, IsNpc with a byte family and a short
            // line-of-sight height, HasSmallHealth, HasSmallHealthDamage and
            // HasHeadMesh. UnknownFlag, AccompaniesHeadMesh, UnknownFlag2 and
            // IsPet gate nothing the reader looks at and are carried through
            // untouched.
            var body = new SimpleCharFullUpdateMessage
            {
                Identity = Id(IdentityType.CanbeAffected, unchecked((int)0x7A573703)),
                Unknown = 0,
                Version = 0x3A,
                Flags = SimpleCharFullUpdateFlags.IsNpc | SimpleCharFullUpdateFlags.UnknownFlag
                        | SimpleCharFullUpdateFlags.AccompaniesHeadMesh
                        | SimpleCharFullUpdateFlags.UnknownFlag2 | SimpleCharFullUpdateFlags.IsPet,
                PlayfieldId = 0x0020D03D,
                Coordinates = new Vector3
                {
                    X = 3624.14990234375f,
                    Y = 51.744998931884766f,
                    Z = 787.0333862304688f
                },
                Heading = new Quaternion
                {
                    X = 0f,
                    Y = -0.7073596119880676f,
                    Z = 0f,
                    W = 0.7068538665771484f
                },
                Appearance = new Appearance { Value = 0x628 },
                Name = "Rex Larsson",
                CharacterFlags = (CharacterFlags)0x108C1201,
                AccountFlags = 0,
                Expansions = 0,
                CharacterInfo = new SimpleNpcInfo
                {
                    Family = 0x89,

                    // 3000 does not fit in a byte, so this one goes out as a
                    // short and HasSmallNpcLosHeight stays clear. The family
                    // beside it does fit, and its flag is set.
                    LosHeight = 3000,
                    PetType = 0,
                    Unknown2 = 0
                },
                Level = 15,
                Health = 511,
                HealthDamage = 0,
                MonsterData = 0x65DA,
                MonsterScale = 97,
                VisualFlags = 31,
                VisibleTitle = 0,
                VehicleData = new byte[]
                {
                    0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                    0x80, 0x00, 0x00, 0x00, 0x01, 0x01, 0x00, 0x01,
                    0x00, 0x01, 0x00, 0x01, 0x00, 0x01, 0x00, 0x00,
                    0x00, 0x02, 0x00, 0x00
                },
                HeadMesh = 0x9EF3,
                RunSpeedBase = 0x34,
                ActiveNanos = new ActiveNano[0],
                Textures = new[]
                {
                    new Texture { Place = 0, Id = 0x48283, Group = 0 },
                    new Texture { Place = 1, Id = 0x48281, Group = 0 },
                    new Texture { Place = 2, Id = 0x48282, Group = 0 },
                    new Texture { Place = 3, Id = 0x48280, Group = 0 },
                    new Texture { Place = 4, Id = 0x48284, Group = 0 }
                },
                Meshes = new[]
                {
                    new Mesh { Position = 0, Id = 0x32140, OverrideTextureId = 0, Layer = 2 },
                    new Mesh { Position = 0, Id = 0x9EF3, OverrideTextureId = 0, Layer = 4 }
                },
                Flags2 = 0,
                Unknown2 = 0
            };

            AssertRetailPacket(
                "00 05 00 0A 00 01 00 F4 00 00 0D B8 0A 0B 0C 02 27 1B 3A 6B " +
                "00 00 C3 50 7A 57 37 03 00 3A 0A 22 4A CB 00 20 D0 3D 45 62 " +
                "82 66 42 4E FA E1 44 44 C2 23 00 00 00 00 BF 35 15 85 00 00 " +
                "00 00 3F 34 F4 60 00 00 06 28 0C 52 65 78 20 4C 61 72 73 73 " +
                "6F 6E 00 10 8C 12 01 00 00 00 00 89 0B B8 00 00 00 0F 01 FF " +
                "00 00 00 65 DA 00 61 00 1F 00 00 00 00 1C 80 00 00 00 00 00 " +
                "00 00 80 00 00 00 01 01 00 01 00 01 00 01 00 01 00 00 00 02 " +
                "00 00 00 00 9E F3 34 00 00 03 F1 00 00 17 A6 00 00 00 00 00 " +
                "04 82 83 00 00 00 00 00 00 00 01 00 04 82 81 00 00 00 00 00 " +
                "00 00 02 00 04 82 82 00 00 00 00 00 00 00 03 00 04 82 80 00 " +
                "00 00 00 00 00 00 04 00 04 82 84 00 00 00 00 00 00 0B D3 00 " +
                "00 03 21 40 00 00 00 00 02 00 00 00 9E F3 00 00 00 00 04 00 " +
                "00 00 00 00",
                body,
                0x0005,
                0x00000DB8,
                unchecked((int)0x0A0B0C02));
        }

        [TestMethod]
        public void SimpleCharFullUpdateKeepsTheWideFormsTheSenderChose()
        {
            // A tailor in Old Athen, level ten, health 116 - and the sender put
            // the level in a short and the health in a full int anyway. Both fit
            // the narrow form, so a writer that picks the width from the value
            // makes this packet one byte short and every byte after the level
            // wrong. Fifteen updates in the captures do this, ten of them with
            // the health as well.
            //
            // The width is the sender's to choose and nothing in the value
            // predicts it, which is why SimpleCharFullUpdateMessage.WireWidths
            // exists. This is the packet that proved it.
            AssertRetailRoundTrip(
                "00 44 00 0A 00 01 01 0E 00 00 03 56 00 00 00 20 " +
                "27 1B 3A 6B 00 00 C3 50 7A 57 37 24 00 3A 00 00 " +
                "5A C0 00 00 19 99 45 58 F6 8F 41 10 28 F6 44 57 " +
                "49 79 00 00 00 00 3F 7E 48 E9 00 00 00 00 BD EC " +
                "8B 44 00 00 07 28 07 54 61 69 6C 6F 72 00 10 28 " +
                "12 01 00 00 00 00 00 00 00 0A 00 00 00 00 00 05 " +
                "00 00 00 00 00 00 00 00 00 00 00 00 00 8E 2F 90 " +
                "00 00 00 65 DC 00 74 00 1F 00 00 00 00 2A 80 00 " +
                "00 00 00 00 00 00 80 00 00 00 03 01 00 01 00 01 " +
                "00 01 00 01 00 00 00 03 00 00 00 00 00 00 00 00 " +
                "00 00 00 00 00 00 00 00 00 00 9E BB 05 00 00 03 " +
                "F1 00 00 17 A6 00 00 00 00 00 00 00 00 00 00 00 " +
                "00 00 00 00 01 00 00 78 8E 00 00 00 00 00 00 00 " +
                "02 00 00 9F C7 00 00 00 00 00 00 00 03 00 00 78 " +
                "77 00 00 00 00 00 00 00 04 00 00 78 A6 00 00 00 " +
                "00 00 00 0B D3 00 00 00 9E BB 00 00 00 00 04 01 " +
                "00 00 1E 61 00 00 00 00 02 00 00 00 00 00");
        }

        [TestMethod]
        public void SimpleCharFullUpdateWithRunningNanoSurvivesTheRoundTrip()
        {
            // A Dockworker with a nano running on him, which is the case that
            // kept this message from ever round-tripping: an ActiveNano entry is
            // five int32s and every reader we had took it for four. The fifth
            // was then read as the array header that follows, came out not a
            // multiple of 0x3F1, and was silently treated as an empty array -
            // so the rest of the packet was lost without a single error.
            //
            // This copy also carries the second flag word set, which nothing
            // read before either.
            AssertRetailRoundTrip(
                "00 06 00 0A 00 01 00 FC 00 00 0D B8 0A 0B 0C 02 27 1B 3A 6B " +
                "00 00 C3 50 7A 57 37 5F 00 3A 02 0A 4A CB 00 20 D0 3D 45 61 " +
                "76 93 40 DD 1E B9 44 41 D1 D2 00 00 00 00 BF 41 BC AD 00 00 " +
                "00 00 3F 27 56 80 00 00 06 28 0B 44 6F 63 6B 77 6F 72 6B 65 " +
                "72 00 10 08 12 01 00 00 00 00 67 00 00 00 00 05 00 73 00 00 " +
                "00 65 DA 00 5D 00 1F 00 00 00 00 1C 80 00 00 00 00 00 00 00 " +
                "80 00 00 00 03 01 00 01 00 01 00 01 00 01 00 00 00 02 00 00 " +
                "00 00 9E F3 13 00 00 03 F1 00 00 17 A6 00 00 00 00 00 04 82 " +
                "83 00 00 00 00 00 00 00 01 00 04 82 81 00 00 00 00 00 00 00 " +
                "02 00 04 82 82 00 00 00 00 00 00 00 03 00 04 82 80 00 00 00 " +
                "00 00 00 00 04 00 04 82 84 00 00 00 00 00 00 0F C4 00 00 03 " +
                "21 40 00 00 00 00 02 00 00 00 9E F3 00 00 00 00 04 01 00 01 " +
                "3F 88 00 00 00 00 02 00 00 00 00 00");
        }

        [TestMethod]
        public void CloneMatchesTheLayoutTakenFromTheClient()
        {
            // Clone has never appeared in a capture, so there is no retail copy
            // to check against and this is not that kind of test. The bytes
            // below were worked out from the client - the reader at
            // Gamecode.dll 0x10072E3B and the writer at 0x10072DDC, which agree
            // - and written down here so the layout cannot drift without
            // something failing.
            //
            // What it carries is not in doubt. The client writes the four
            // values straight into the character's stats at 0x10058800: head
            // mesh into 64, race into 89, breed into 4 and sex into 59.
            var body = new CloneMessage
            {
                Identity = Id(IdentityType.CanbeAffected, unchecked((int)0x7A573703)),
                Unknown = 0,
                Clothes = new[]
                {
                    new Texture { Place = 0, Id = 0x48283, Group = 0 },
                    new Texture { Place = 1, Id = 0x48281, Group = 0 }
                },
                HeadMesh = 0x9EF3,
                Race = 1,
                Breed = 3,
                Sex = 2,
                Name = "Vixen"
            };

            AssertRetailPacket(
                "00 07 00 0A 00 01 00 46 00 00 0D B8 0A 0B 0C 02 3C 26 51 79 " +
                "00 00 C3 50 7A 57 37 03 00 00 00 0B D3 00 00 00 00 00 04 82 " +
                "83 00 00 00 00 00 00 00 01 00 04 82 81 00 00 00 00 00 00 9E " +
                "F3 01 03 02 56 69 78 65 6E 00",
                body,
                0x0007,
                0x00000DB8,
                unchecked((int)0x0A0B0C02));
        }

        [TestMethod]
        public void TradeMatchesRetailCaptureByteForByte()
        {
            // Every one of the 124 captured Trades opens with 2, and the client
            // is where that stops being a coincidence: the reader compares this
            // int32 against a static holding 2 and gives up on the whole message
            // if it differs, and the writer emits the static rather than a field.
            //
            // Action is one signed byte, not an int - the reader uses movsbl -
            // and the two identities follow it.
            var body = new TradeMessage
            {
                Identity = Id(IdentityType.None, 0x000B0C02),
                Unknown = 0,
                Version = 2,
                Action = TradeAction.None,
                Target = Id(IdentityType.None, 0x006ECC54),
                Container = Id(IdentityType.None, 0)
            };

            AssertRetailPacket(
                "6C 72 00 0A 00 01 00 32 00 00 0D B8 00 0B 0C 02 36 28 4F 6E " +
                "00 00 00 00 00 0B 0C 02 00 00 00 00 02 00 00 00 00 00 00 6E " +
                "CC 54 00 00 00 00 00 00 00 00",
                body,
                0x6C72,
                0x00000DB8,
                0x000B0C02);
        }

        [TestMethod]
        public void PlayfieldAllCitiesWithOneHouseMatchesTheLayoutTakenFromTheClient()
        {
            // No captured copy carries a house, so these bytes were computed
            // from the client rather than taken off the wire:
            // PlayfieldCityHolderClient_c::UpdateNewHouses at city.dll
            // 0x10016317 reads a uint32 count and then, per house, three floats
            // of position, an int32 template, one byte kept as a boolean, and an
            // Identity. Twenty five bytes each, and the message length counts
            // bytes rather than houses - 4 + 25 here.
            var body = new PlayfieldAllCitiesMessage
            {
                Identity = Id(IdentityType.Playfield2, 0x0020D03D),
                Unknown = 1,
                Houses = new[]
                {
                    new CityHouse
                    {
                        Position = new Vector3 { X = 3624.5f, Y = 51.75f, Z = 787f },
                        Template = 0x1B58,
                        DemolitionStarted = true,
                        Identity = Id(IdentityType.CanbeAffected, 0x0020D100)
                    }
                }
            };

            AssertRetailPacket(
                "00 63 00 0A 00 01 00 3C 00 00 0D B8 0A 0B 0C 02 59 21 01 26 " +
                "00 00 9C 50 00 20 D0 3D 01 00 1D 00 00 00 01 45 62 88 00 42 " +
                "4F 00 00 44 44 C0 00 00 00 1B 58 01 00 00 C3 50 00 20 D1 00",
                body,
                0x0063,
                0x00000DB8,
                unchecked((int)0x0A0B0C02));
        }

        [TestMethod]
        public void SimpleItemFullUpdateOnTheGroundMatchesRetailCaptureByteForByte()
        {
            // An item lying in the world rather than held by anyone. That is
            // what makes the coordinates and heading present at all: the reader
            // at Gamecode.dll 0x100A1696 tests the owner identity's type and
            // only reads them when it is zero. 647 of the 747 captured copies
            // are this shape.
            //
            // The two bytes after the marker go straight into stats - the
            // inventory id into 55 and the body location into 220 - which is
            // what names them.
            var body = new SimpleItemFullUpdateMessage
            {
                Identity = Id((IdentityType)51005, 0x58099085),
                Unknown = 0,
                MsgVersion = 11,
                Identitytype = 0,
                Instance = 0,
                Coordinate = new Vector3
                {
                    X = 3602.389404296875f,
                    Y = 51.69404220581055f,
                    Z = 797.7532958984375f
                },
                Heading = new Quaternion
                {
                    X = 0f,
                    Y = 0.003961213398724794f,
                    Z = 0f,
                    W = 0.9999921321868896f
                },
                Playfield = 0x0020D03D,
                Marker = Id((IdentityType)ItemMessageConstants.ItemMessageMarker, 0),
                InventoryId = 0,
                BodyLocation = 111,
                Stats = new[]
                {
                    new GameTuple<CharacterStat, uint> { Value1 = CharacterStat.Flags, Value2 = 0x80080201 },
                    new GameTuple<CharacterStat, uint> { Value1 = CharacterStat.StaticInstance, Value2 = 0x00048944 },
                    new GameTuple<CharacterStat, uint> { Value1 = CharacterStat.ACGItemLevel, Value2 = 1 },
                    new GameTuple<CharacterStat, uint> { Value1 = (CharacterStat)702, Value2 = 0x00048944 },
                    new GameTuple<CharacterStat, uint> { Value1 = (CharacterStat)703, Value2 = 0x00048944 },
                    new GameTuple<CharacterStat, uint> { Value1 = (CharacterStat)412, Value2 = 1 }
                },
                Name = string.Empty
            };

            AssertRetailPacket(
                "00 03 00 0A 00 01 00 8B 00 00 0D B8 0A 0B 0C 02 3B 11 25 6F " +
                "00 00 C7 3D 58 09 90 85 00 00 00 00 0B 00 00 00 00 00 00 00 " +
                "00 45 61 26 3B 42 4E C6 B3 44 47 70 36 00 00 00 00 3B 81 CD " +
                "11 00 00 00 00 3F 7F FF 7C 00 20 D0 3D 00 0F 42 4F 00 00 00 " +
                "00 00 6F 00 00 1B 97 00 00 00 00 80 08 02 01 00 00 00 17 00 " +
                "04 89 44 00 00 02 BD 00 00 00 01 00 00 02 BE 00 04 89 44 00 " +
                "00 02 BF 00 04 89 44 00 00 01 9C 00 00 00 01 00 00 00 00",
                body,
                0x0003,
                0x00000DB8,
                unchecked((int)0x0A0B0C02));
        }

        [TestMethod]
        public void ReflectAttackMatchesRetailCaptureByteForByte()
        {
            // Ninety two of these in one captured mission and not one in any
            // earlier capture, because nothing before it had a pet taking hits.
            // They were unreadable until 2026-09-10 for a reason that had
            // nothing to do with the protocol: ReflectAttackMessage.cs was
            // never listed in the project file, so the class did not exist in
            // the build and the serializer had nothing to resolve the id to.
            //
            // Damage is proven by what the client does with it - the dispatcher
            // at Gamecode.dll 0x100A1222 reads stat 27, health, off the target
            // and subtracts this from it.
            var body = new ReflectAttackMessage
            {
                Identity = Id(IdentityType.CanbeAffected, unchecked((int)0x7A721936)),
                Unknown = 0,
                Damage = 46,
                Reflector = Id(IdentityType.CanbeAffected, unchecked((int)0x7A73D3F0)),
                VisualEffectId = 0
            };

            AssertRetailPacket(
                "15 36 00 0A 00 01 00 2D 00 00 0D B1 0A 0B 0C 08 1C 3A 4F 77 " +
                "00 00 C3 50 7A 72 19 36 00 00 00 00 2E 00 00 C3 50 7A 73 D3 " +
                "F0 00 00 00 00",
                body,
                0x1536,
                0x00000DB1,
                0x0A0B0C08);
        }

        [TestMethod]
        public void FovMatchesRetailCaptureByteForByte()
        {
            // An NPC told to start looking around: thirty three bytes, always,
            // with nothing conditional in it. 391 of these appear in one
            // captured mission run and none in any earlier capture.
            //
            // The angles are radians and they land on whole degrees - 1.134464
            // is a 65 degree half-arc, 0.4363323 is 25 degrees a second, and
            // the turn speed is that negated because the head has just reached
            // the top of its arc and AtLimit is Bounce. Across all 391 copies
            // there are only two sets of these, one per NPC, with only
            // CurrentAngle moving.
            var body = new FovMessage
            {
                Identity = Id(IdentityType.CanbeAffected, unchecked((int)0x7A73D3D2)),
                Unknown = 0,
                Unknown1 = 0.2617993950843811f,
                SweepHalfAngle = 1.1344640254974365f,
                AtLimit = FovLimitBehaviour.Bounce,
                TurnDownSpeed = 0.4363323152065277f,
                TurnUpSpeed = 0.4363323152065277f,
                SweepCentre = 4.084070205688477f,
                CurrentAngle = 5.029825687408447f,
                TurnSpeed = -0.4363323152065277f,
                Unknown9 = 13
            };

            AssertRetailPacket(
                "00 A3 00 0A 00 01 00 3E 00 00 0D B1 0A 0B 0C 08 2A 29 3D 0F " +
                "00 00 C3 50 7A 73 D3 D2 00 3E 86 0A 92 3F 91 36 1E 00 3E DF " +
                "66 F3 3E DF 66 F3 40 82 B0 B4 40 A0 F4 55 BE DF 66 F3 00 00 " +
                "00 0D",
                body,
                0x00A3,
                0x00000DB1,
                0x0A0B0C08);
        }

        [TestMethod]
        public void FightModeUpdateSetsNineDistrictsToSeventyFivePercent()
        {
            // Every district of a Notum Wars playfield, and every field of it
            // was an Unknown until the client was asked.
            //
            // The identity is the playfield: the dispatcher hands its instance
            // half to the playfield lookup and resolves each district name
            // against that playfield. The names are names - they go to
            // GameData's PlayfieldDistrictInfo_t::GetDistrictData, the overload
            // that takes a string - and the int in front of each is the
            // change's own id, which is how a later message takes it out again.
            //
            // The byte after the name is three bits: set rather than add,
            // override, and remove-by-id rather than add-by-name. The byte
            // after that is the suppression level, and the client names the
            // five values itself - 0x1003F4A3 switches on it and loads
            // SuppressionField100, 75, 25, 5 or 0. Every district here carries
            // 1, which is seventy five percent.
            var body = new FightModeUpdateMessage
            {
                Identity = Id((IdentityType)0, 0),
                Unknown = 0,
                Playfield = Id((IdentityType)0xC79C, 655),
                Entries = new[]
                {
                    District(88, "Plago"),
                    District(89, "Harstad"),
                    District(86, "Mune"),
                    District(91, "Klor"),
                    District(90, "Ubleo"),
                    District(84, "Skop Notum Mine"),
                    District(92, "Flubu Notum Mine"),
                    District(85, "jucha"),
                    District(87, "Mocnuf Notum Mine")
                }
            };

            AssertRetailPacket(
                "00 40 00 0A 00 01 00 BF 00 00 0D AD 0A 0B 0C 02 37 1D 05 42 " +
                "00 00 00 00 00 00 00 00 00 00 00 C7 9C 00 00 02 8F 00 00 27 " +
                "6A 00 00 00 58 00 05 50 6C 61 67 6F 01 01 00 00 00 59 00 07 " +
                "48 61 72 73 74 61 64 01 01 00 00 00 56 00 04 4D 75 6E 65 01 " +
                "01 00 00 00 5B 00 04 4B 6C 6F 72 01 01 00 00 00 5A 00 05 55 " +
                "62 6C 65 6F 01 01 00 00 00 54 00 0F 53 6B 6F 70 20 4E 6F 74 " +
                "75 6D 20 4D 69 6E 65 01 01 00 00 00 5C 00 10 46 6C 75 62 75 " +
                "20 4E 6F 74 75 6D 20 4D 69 6E 65 01 01 00 00 00 55 00 05 6A " +
                "75 63 68 61 01 01 00 00 00 57 00 11 4D 6F 63 6E 75 66 20 4E " +
                "6F 74 75 6D 20 4D 69 6E 65 01 01",
                body,
                0x0040,
                0x00000DAD,
                unchecked((int)0x0A0B0C02));
        }

        /// <summary>
        /// One district set to seventy five percent suppression, which is what
        /// all nine entries of the captured copy are.
        /// </summary>
        private static FightModeUpdateEntry District(int id, string name)
        {
            return new FightModeUpdateEntry
            {
                Id = id,
                District = name,
                Flags = FightModeChangeFlags.Set,
                Level = SuppressionLevel.SeventyFive
            };
        }

        [TestMethod]
        public void InventoryUpdateCarriesACorpseYouMayEmptyButNotFill()
        {
            // A corpse, and every field of this message except the entry list
            // was an Unknown. The client names all but one bit of it.
            //
            // 21 is the capacity. 2 is what may be done with the container, and
            // the client tests it a bit at a time: 0x1004C0E5 tests bit 1 and
            // prints Inv_DstCantAdd when it is missing, 0x1004BFCE tests bit 2
            // and prints Inv_SrcCantRemove. A corpse is 2, which is take only.
            // Everything the client builds for itself is 3 - except the
            // overflow window at 0x1004B5F1, which is also 2.
            //
            // 112 is the slot the bag takes in the looter's own inventory, and
            // the 1 at the end opens the window: the dispatcher clears bit 0x40
            // of the chest's +0x4C and calls its vtable +0xAC, which reads stat
            // 435, ReadOnly.
            //
            // The entry's 0x82 is two bits. 2 says the slot holds a live dynel
            // rather than a template item, which is why this is the only
            // captured entry with a real identity in it - 0x1002A961 answers 1,
            // 2 or 4 for what a slot holds and only one is ever set. 0x80 is
            // the bit nothing in this client reads: it has a getter and a
            // setter and neither is called anywhere, and the server sets it on
            // nine of the eleven captured entries.
            //
            // The 1 after it is the stack count, clamped to 0xFFFF by the
            // setter at 0x1002AF06. The three numbers after the identity are
            // GameData's ACGItem_t - two templates and a level - and the fourth
            // is the int ACGItem_t serializes and never uses, which its own
            // writer emits as a literal zero.
            var body = new InventoryUpdateMessage
            {
                Identity = Id((IdentityType)0xC350, 0x0A0B0C08),
                Unknown = 1,
                NumberOfSlots = 21,
                Access = InventoryAccess.CanRemove,
                Entries = new[]
                {
                    new InventoryEntry
                    {
                        Slotnumber = 0,
                        Flags = unchecked((short)0x0082),
                        Count = 1,
                        Identity = Id((IdentityType)0xC749, 0x0BB54424),
                        LowId = 206731,
                        HighId = 206731,
                        Quality = 150,
                        Unused = 0
                    }
                },
                BagIdentity = Id(IdentityType.Corpse, 0x0102580C),
                SlotnumberInMainInventory = 112,
                Open = 1
            };

            AssertRetailPacket(
                "0A CD 00 0A 00 01 00 59 00 00 0D B1 0A 0B 0C 08 4E 53 69 76 " +
                "00 00 C3 50 0A 0B 0C 08 01 00 00 00 15 00 00 00 02 00 00 07 " +
                "E2 00 00 00 00 00 82 00 01 00 00 C7 49 0B B5 44 24 00 03 27 " +
                "8B 00 03 27 8B 00 00 00 96 00 00 00 00 00 00 C7 6A 01 02 58 " +
                "0C 00 00 00 70 00 00 00 01",
                body,
                0x0ACD,
                0x00000DB1,
                0x0A0B0C08);
        }

        [TestMethod]
        public void InventoryUpdateEntryCanHoldATemplateItemInstead()
        {
            // The same corpse a slot later. 0xA1 is the other shape an entry
            // takes: bit 1 for a template item rather than bit 2 for a dynel,
            // so the identity is empty and only the templates carry anything.
            // 0x20 is set, which is what puts the entry in the corpse window -
            // CorpseEntry_t's list builder at 0x10125D9D lists an occupied slot
            // only when 0x1002AEA1 agrees, and that agrees on this bit or on
            // the item's own stat 30 carrying 0x40.
            var body = new InventoryUpdateMessage
            {
                Identity = Id((IdentityType)0xC350, 0x0A0B0C08),
                Unknown = 1,
                NumberOfSlots = 21,
                Access = InventoryAccess.CanRemove,
                Entries = new[]
                {
                    new InventoryEntry
                    {
                        Slotnumber = 0,
                        Flags = unchecked((short)0x00A1),
                        Count = 1,
                        Identity = Id((IdentityType)0, 0),
                        LowId = 121911,
                        HighId = 121911,
                        Quality = 200,
                        Unused = 0
                    }
                },
                BagIdentity = Id(IdentityType.Corpse, 0x0102580B),
                SlotnumberInMainInventory = 113,
                Open = 1
            };

            AssertRetailPacket(
                "13 D8 00 0A 00 01 00 59 00 00 0D B1 0A 0B 0C 08 4E 53 69 76 " +
                "00 00 C3 50 0A 0B 0C 08 01 00 00 00 15 00 00 00 02 00 00 07 " +
                "E2 00 00 00 00 00 A1 00 01 00 00 00 00 00 00 00 00 00 01 DC " +
                "37 00 01 DC 37 00 00 00 C8 00 00 00 00 00 00 C7 6A 01 02 58 " +
                "0B 00 00 00 71 00 00 00 01",
                body,
                0x13D8,
                0x00000DB1,
                0x0A0B0C08);
        }

        [TestMethod]
        public void DoorFullUpdateCarriesTheTwoRoomsTheDoorJoins()
        {
            // The one door in the captured mission that leads outside, which is
            // why the first of its two rooms is -1.
            //
            // That int32 at the end was the last Unknown here, and N3.dll names
            // it without any inference. The client keeps the pair at Door_t +
            // 0x1D0; 0x1007F51F splits it into its two 16 bit halves and hands
            // each to n3Playfield_t::GetRoom, skipping either half that reads
            // 0xFFFF, and only bothers at all when n3Playfield_t::IsDungeon
            // agrees. The pair also goes out whole to n3RoomMonitor_t's
            // DoorOpened and DoorClosed. Building one of these from a door
            // copies Door_t + 0x1D0 into the message at + 0x78, at 0x100A0101,
            // which closes the loop.
            //
            // Which half is which had already been worked out from the geometry
            // - put a door's world position back on the room grid the same
            // playfield's PlayfieldAnarchyF carries and it lands on the cell
            // the first half names - and the client agrees. What the geometry
            // could not show is that the other half is a room too: a door
            // between two rooms is only ever standing in one of them.
            var body = new DoorFullUpdateMessage
            {
                Identity = Id((IdentityType)51016, 0x10B3D63C),
                Unknown = 0,
                MsgVersion = 11,
                Identitytype = 0,
                Instance = 0,
                Coordinate = new Vector3
                {
                    X = 0.0010014285799115896f,
                    Y = 5f,
                    Z = 135f
                },
                Heading = new Quaternion
                {
                    X = 0f,
                    Y = 0.7071066498756409f,
                    Z = 0f,
                    W = 0.70710688829422f
                },
                Playfield = 0x0020D18F,
                StateMachine = Id((IdentityType)ItemMessageConstants.ItemMessageMarker, 1),
                InventoryId = 0,
                BodyLocation = 111,
                Stats = new[]
                {
                    new GameTuple<CharacterStat, uint> { Value1 = CharacterStat.Flags, Value2 = 0xC0081083 },
                    new GameTuple<CharacterStat, uint> { Value1 = CharacterStat.StaticInstance, Value2 = 0x0000A396 },
                    new GameTuple<CharacterStat, uint> { Value1 = (CharacterStat)701, Value2 = 0 },
                    new GameTuple<CharacterStat, uint> { Value1 = (CharacterStat)702, Value2 = 0 },
                    new GameTuple<CharacterStat, uint> { Value1 = (CharacterStat)703, Value2 = 0 },
                    new GameTuple<CharacterStat, uint> { Value1 = (CharacterStat)412, Value2 = 1 },
                    new GameTuple<CharacterStat, uint> { Value1 = (CharacterStat)252, Value2 = 0 },
                    new GameTuple<CharacterStat, uint> { Value1 = (CharacterStat)192, Value2 = 0 },
                    new GameTuple<CharacterStat, uint> { Value1 = (CharacterStat)193, Value2 = 0 },
                    new GameTuple<CharacterStat, uint> { Value1 = (CharacterStat)195, Value2 = 0 },
                    new GameTuple<CharacterStat, uint> { Value1 = (CharacterStat)259, Value2 = 0 }
                },
                Name = string.Empty,
                TailVersion = 2,
                LockDifficulty = 50,
                Keyholders = new Identity[0],
                DoorVersion = 2,
                Room = -1,
                AdjoiningRoom = 0
            };

            AssertRetailPacket(
                "00 03 00 0A 00 01 00 C7 00 00 0D B1 0A 0B 0C 08 36 5A 50 71 " +
                "00 00 C7 48 10 B3 D6 3C 00 00 00 00 0B 00 00 00 00 00 00 00 " +
                "00 3A 83 42 5E 40 A0 00 00 43 07 00 00 00 00 00 00 3F 35 04 " +
                "F1 00 00 00 00 3F 35 04 F5 00 20 D1 8F 00 0F 42 4F 00 00 00 " +
                "01 00 6F 00 00 2F 4C 00 00 00 00 C0 08 10 83 00 00 00 17 00 " +
                "00 A3 96 00 00 02 BD 00 00 00 00 00 00 02 BE 00 00 00 00 00 " +
                "00 02 BF 00 00 00 00 00 00 01 9C 00 00 00 01 00 00 00 FC 00 " +
                "00 00 00 00 00 00 C0 00 00 00 00 00 00 00 C1 00 00 00 00 00 " +
                "00 00 C3 00 00 00 00 00 00 01 03 00 00 00 00 00 00 00 00 00 " +
                "00 00 02 00 00 00 32 00 00 03 F1 00 00 00 02 FF FF 00 00",
                body,
                0x0003,
                0x00000DB1,
                0x0A0B0C08);
        }

        [TestMethod]
        public void TeamMemberInfoIsFourStatsAndNothingElse()
        {
            // Nothing had ever captured this one either, and the model had nine
            // fields where the client's reader has five. It opened with a byte
            // and an int16 and closed with an int16, and 24 bytes of body is all
            // the client reads: an identity and four int32s.
            //
            // The four are stats, and the dispatcher at 0x1007A7DD names them by
            // setting them - 221, 214, 1 and 27, which are maxnanoenergy,
            // currentnano, life and health. Each maximum goes in before the
            // current value that belongs with it, which is why it applies them
            // in a different order than it reads them.
            //
            // The member here is at full: 713 of 713 nano and 1631 of 1631
            // health.
            var body = new TeamMemberInfoMessage
            {
                Identity = Id((IdentityType)0xC350, unchecked((int)0x0A0B0C01)),
                Unknown = 0,
                Character = Id((IdentityType)0xC350, 0x0A0B0C05),
                CurrentNano = 713,
                MaxNano = 713,
                MaxHealth = 1631,
                CurrentHealth = 1631
            };

            AssertRetailPacket(
                "16 94 00 0A 00 01 00 35 00 00 0D AD 0A 0B 0C 01 28 78 42 48 " +
                "00 00 C3 50 0A 0B 0C 01 00 00 00 C3 50 0A 0B 0C 05 00 00 02 " +
                "C9 00 00 02 C9 00 00 06 5F 00 00 06 5F",
                body,
                0x1694,
                0x00000DAD,
                unchecked((int)0x0A0B0C01));
        }

        [TestMethod]
        public void TeamMemberCarriesLevelProfessionAndRaidGroup()
        {
            // The first capture of this message there has ever been here. Until
            // a second account was logged in there was nothing to check the
            // model against, and the model had nine fields where the client's
            // reader has six - three invented bytes at the front and two at the
            // back, which nothing had ever contradicted.
            //
            // This is one of the two members the moment the team forms. The
            // identity is the member, the TeamWindow identity is the team, and
            // the three numbers that used to be Unknown are named here for the
            // first time:
            //
            //   -1 is the raid group. The client keeps it on the team at +0x38
            //   when the member is the local player, and it indexes the array
            //   of six group pointers at the team's +0x20 - six, because a raid
            //   is six teams. The accessors fall back to +0x38 when asked for a
            //   negative group and clamp to the first when that is negative
            //   too, and 0x10065D2F answers "am I in a group" with +0x38 >= 0.
            //   An ordinary team is the one that is not in a raid, and sends -1.
            //
            //   10 is the level and 4 is the profession, and the same capture
            //   proves both: this account's own FullCharacter carries 10 for
            //   stat 54 and 4 for stat 60. The other member of the same team
            //   carries 46 and 14 in both places - a level 46 Keeper teaming
            //   with a level 10 Fixer.
            //
            // The profession is sixteen bits here where the Profession enum is
            // thirty two, which is why the field is a short.
            var body = new TeamMemberMessage
            {
                Identity = Id((IdentityType)0xC350, unchecked((int)0x0A0B0C01)),
                Unknown = 0,
                Character = Id((IdentityType)0xC350, unchecked((int)0x0A0B0C01)),
                Team = Id((IdentityType)0xDEA9, 0x029811C6),
                RaidGroup = -1,
                Level = 10,
                Profession = 4,
                Name = "Alpha"
            };

            AssertRetailPacket(
                "16 93 00 0A 00 01 00 40 00 00 0D AD 0A 0B 0C 01 46 31 2D 2E " +
                "00 00 C3 50 0A 0B 0C 01 00 00 00 C3 50 0A 0B 0C 01 00 00 DE " +
                "A9 02 98 11 C6 FF FF FF FF 00 00 00 0A 00 04 00 00 00 05 41 " +
                "6C 70 68 61",
                body,
                0x1693,
                0x00000DAD,
                unchecked((int)0x0A0B0C01));
        }

        [TestMethod]
        public void TeamMemberCarriesTheOtherMemberOfTheSameTeam()
        {
            // The same team, the other member, seen on the other account's own
            // stream. Same team identity, and the level and profession that the
            // other account's FullCharacter carries for stats 54 and 60.
            var body = new TeamMemberMessage
            {
                Identity = Id((IdentityType)0xC350, 0x0A0B0C05),
                Unknown = 0,
                Character = Id((IdentityType)0xC350, 0x0A0B0C05),
                Team = Id((IdentityType)0xDEA9, 0x029811C6),
                RaidGroup = -1,
                Level = 46,
                Profession = 14,
                Name = "Testplayer"
            };

            AssertRetailPacket(
                "15 FD 00 0A 00 01 00 45 00 00 0D AD 0A 0B 0C 05 46 31 2D 2E " +
                "00 00 C3 50 0A 0B 0C 05 00 00 00 C3 50 0A 0B 0C 05 00 00 DE " +
                "A9 02 98 11 C6 FF FF FF FF 00 00 00 2E 00 0E 00 00 00 0A 54 " +
                "65 73 74 70 6C 61 79 65 72",
                body,
                0x15FD,
                0x00000DAD,
                0x0A0B0C05);
        }

        [TestMethod]
        public void ChatTextCarriesItsColourAndItsWindowByteForByte()
        {
            // A system announcement, and the only ChatText in the captures. Its
            // three trailing fields were Unknown1, Unknown2 and Unknown3 for as
            // long as this message has existed here, and all three are named
            // from the client - two of them from a different module.
            //
            // The 16 is the colour. The dispatcher hands it to the engine's
            // chat text signal as the third argument; GUI.dll subscribes to
            // that signal at 0x10083E7B, finds the window the first argument
            // names, and calls AddLine with the text and this. AddLine looks
            // the value up in the colour table at 0x10268D38 and wraps the line
            // in a font tag - and entry 16 of that table is CCYellow, which is
            // what a system announcement is printed in.
            //
            // The byte after it is where the line goes, and the reader refuses
            // anything above 1: zero to a chat window, one to the floating
            // on-screen message, which has no window and no colour. The int32
            // is the window, the same routing value OrgClient carries in the
            // other direction.
            AssertRetailPacket(
                "2E 64 00 0A 00 01 00 44 00 00 0D B1 0A 0B 0C 08 5F 4B 44 2A " +
                "00 00 00 00 00 00 00 00 00 00 1F 54 61 72 61 73 71 75 65 20 " +
                "69 73 20 6E 6F 20 6C 6F 6E 67 65 72 20 69 6D 6D 6F 72 74 61 " +
                "6C 2E 10 00 00 00 00 00",
                new ChatTextMessage
                {
                    Identity = Id((IdentityType)0, 0),
                    Unknown = 0,
                    Text = "Tarasque is no longer immortal.",
                    Colour = ChatTextColour.Yellow,
                    OnScreen = 0,
                    Window = 0
                },
                0x2E64,
                0x00000DB1,
                0x0A0B0C08);
        }

        [TestMethod]
        public void MechInfoHasTwoShapesAndTheFirstIntPicksBetweenThem()
        {
            // Nothing in the captures carries this message, so both shapes here
            // are the client's reader and writer and nothing else. They agree
            // with each other, which is what makes them worth trusting:
            // Gamecode.dll 0x10075557 takes the first int32 and branches on it,
            // and 0x100755EA writes a literal 0 or 1 depending on whether the
            // message is carrying a stat table at all.
            //
            // The model had the first int32 as a bare Unknown and read the
            // stats, the identity and a four byte "hash" unconditionally, so
            // the short form below - which is what the client sends when there
            // is no character to report - came out as rubbish and ran off the
            // end of the packet.
            AssertRetailPacket(
                "00 31 00 0A 00 01 00 29 00 00 0D B8 0A 0B 0C 02 58 57 42 39 " +
                "00 00 C3 50 0A 0B 0C 02 00 00 00 00 00 00 00 C7 4A 25 BD 52 " +
                "FF",
                new MechInfoMessage
                {
                    Identity = Id(IdentityType.CanbeAffected, unchecked((int)0x0A0B0C02)),
                    Unknown = 0,
                    HasStats = 0,
                    MechIdentity = Id((IdentityType)0xC74A, 0x25BD52FF)
                },
                0x0031,
                0x00000DB8,
                unchecked((int)0x0A0B0C02));

            // And the long form. The stat list is a plain int32 count and then
            // that many pairs - not an X3F1 count, unlike most lists on this
            // wire - because GameData's shared stat table reader at 0x10009DAD
            // is what reads it.
            AssertRetailPacket(
                "00 32 00 0A 00 01 00 41 00 00 0D B8 0A 0B 0C 02 58 57 42 39 " +
                "00 00 C3 50 0A 0B 0C 02 00 00 00 00 01 00 00 00 02 00 00 00 " +
                "10 00 00 00 64 00 00 00 11 00 00 00 C8 00 00 C7 4A 25 BD 52 " +
                "FF 00 00 00 07",
                new MechInfoMessage
                {
                    Identity = Id(IdentityType.CanbeAffected, unchecked((int)0x0A0B0C02)),
                    Unknown = 0,
                    HasStats = 1,
                    Stats = new[]
                    {
                        Stat((CharacterStat)16, 100),
                        Stat((CharacterStat)17, 200)
                    },
                    MechIdentity = Id((IdentityType)0xC74A, 0x25BD52FF),
                    MechData = 7
                },
                0x0032,
                0x00000DB8,
                unchecked((int)0x0A0B0C02));
        }

        [TestMethod]
        public void OrgClientPromoteCarriesAByteAndBankPaymembersCarriesNothing()
        {
            // Promote is the only org command that carries a byte, and until
            // now the model had it carrying nothing. The client's writer at
            // Gamecode.dll 0x1012653F picks one of three arms out of the table
            // at 0x101265B4: a string for thirteen commands, nothing for
            // fourteen, and this byte for Promote alone.
            //
            // The byte says which of the two promote messages this is. Typing
            // "/org promote" builds it at 0x10041093 with zero; accepting the
            // promotion dialog goes through the client's own
            // N3Msg_OrgPromotionConfirmed export, which builds it at 0x1001DD8C
            // with one.
            AssertRetailPacket(
                "00 C1 00 0A 00 01 00 2B 0A 0B 0C 02 00 00 00 02 7F 4B 31 08 " +
                "00 00 C3 50 0A 0B 0C 02 00 0A 00 00 C3 50 7A 6F B1 F1 00 00 " +
                "00 00 01",
                new OrgClientMessage
                {
                    Identity = Id(IdentityType.CanbeAffected, unchecked((int)0x0A0B0C02)),
                    Unknown = 0,
                    Command = OrgClientCommand.Promote,
                    Target = Id(IdentityType.CanbeAffected, 0x7A6FB1F1),
                    Window = 0,
                    Confirmed = 1
                },
                0x00C1,
                unchecked((int)0x0A0B0C02),
                2);

            // And BankPaymembers, which the model had writing a string. The
            // table gives it the empty arm, so a string here would have put
            // two bytes on the wire the client does not read - the same two
            // that would then be the front of whatever came next.
            AssertRetailPacket(
                "00 C2 00 0A 00 01 00 2A 0A 0B 0C 02 00 00 00 02 7F 4B 31 08 " +
                "00 00 C3 50 0A 0B 0C 02 00 15 00 00 C3 50 7A 6F B1 F1 00 00 " +
                "00 00",
                new OrgClientMessage
                {
                    Identity = Id(IdentityType.CanbeAffected, unchecked((int)0x0A0B0C02)),
                    Unknown = 0,
                    Command = OrgClientCommand.BankPaymembers,
                    Target = Id(IdentityType.CanbeAffected, 0x7A6FB1F1),
                    Window = 0,
                    CommandArgs = "this is not sent"
                },
                0x00C2,
                unchecked((int)0x0A0B0C02),
                2);
        }

        [TestMethod]
        public void VendingMachineWithANameMatchesRetailCaptureByteForByte()
        {
            // ICC Pharmacy. Every vending machine in the mission captures has
            // an empty name, which is why this went unnoticed: the name length
            // counts the terminator, the writer wrote it without, and a shop
            // with no name is zero either way. 176 of the 1,354 captured shops
            // came out a byte short, and every byte after the name moved.
            //
            // It is the same Int32Terminated count the rest of the item family
            // uses - the corpse two tests down carries "Remains of Bureaucrat
            // Worker" as 29 for 28 characters.
            var body = new VendingMachineFullUpdateMessage
            {
                Identity = Id(IdentityType.VendingMachine, 0x70000000),
                Unknown = 0,
                TypeIdentifier = 11,
                NpcIdentity = Id((IdentityType)0, 0),
                Coordinates = new Vector3 { X = 3421f, Y = 9.300202f, Z = 797.5f },
                Heading = new Quaternion { X = 0f, Y = 0.7071081f, Z = 0f, W = 0.70710546f },
                PlayfieldId = 0x1999,
                MarkerType = 1000015,
                MarkerInstance = 0,
                InventoryIdAndBodyLocation = 0x006F,
                Stats = new[]
                {
                    Stat(CharacterStat.Flags, 0x80023601),
                    Stat((CharacterStat)12, 0x0004845D),
                    Stat((CharacterStat)23, 0x0004896D),
                    Stat((CharacterStat)30, 8),
                    Stat((CharacterStat)76, 0),
                    Stat((CharacterStat)88, 0),
                    Stat((CharacterStat)298, 0),
                    Stat((CharacterStat)336, 0x0004896C),
                    Stat((CharacterStat)360, 65),
                    Stat((CharacterStat)426, 4),
                    Stat((CharacterStat)427, 105),
                    Stat((CharacterStat)501, 2),
                    Stat((CharacterStat)688, 2)
                },
                Name = "ICC Pharmacy",
                TailVersion = 2,
                LockDifficulty = 50,
                Keyholders = new Identity[0],
                TailEndVersion = 3
            };

            AssertRetailPacket(
                "00 03 00 0A 00 01 00 E0 00 00 03 56 00 00 00 20 7F 54 49 05 " +
                "00 00 C7 5B 70 00 00 00 00 00 00 00 0B 00 00 00 00 00 00 00 " +
                "00 45 55 D0 00 41 14 CD A1 44 47 60 00 00 00 00 00 3F 35 05 " +
                "09 00 00 00 00 3F 35 04 DD 00 00 19 99 00 0F 42 4F 00 00 00 " +
                "00 00 6F 00 00 37 2E 00 00 00 00 80 02 36 01 00 00 00 0C 00 " +
                "04 84 5D 00 00 00 17 00 04 89 6D 00 00 00 1E 00 00 00 08 00 " +
                "00 00 4C 00 00 00 00 00 00 00 58 00 00 00 00 00 00 01 2A 00 " +
                "00 00 00 00 00 01 50 00 04 89 6C 00 00 01 68 00 00 00 41 00 " +
                "00 01 AA 00 00 00 04 00 00 01 AB 00 00 00 69 00 00 01 F5 00 " +
                "00 00 02 00 00 02 B0 00 00 00 02 00 00 00 0D 49 43 43 20 50 " +
                "68 61 72 6D 61 63 79 00 00 00 00 02 00 00 00 32 00 00 03 F1 " +
                "00 00 00 03",
                body,
                0x0003,
                0x00000356,
                0x00000020);
        }

        [TestMethod]
        public void EffectArgumentsCanBeAStringAndTheTableSaysWhichOnesAre()
        {
            // Game function 53044, which puts a line of text on the screen. Its
            // first argument is a string, and that is the shape the old
            // hand-measured table could not express: it had one entry marked as
            // a string and a constant twenty bytes of padding, which happened to
            // be right for this function and wrong for 53104, whose speech line
            // is also a string and which the table recorded as a flat thirty
            // four bytes - the length of the speech in the captures and nothing
            // more general than that.
            //
            // The client's own table has both. 53044 is a string and five
            // integers; 53104 is a string and one. Nothing here is measured.
            var body = new ApplySpellsMessage
            {
                Identity = Id(IdentityType.CanbeAffected, unchecked((int)0x0A0B0C02)),
                Unknown = 0,
                NanoEffects = new[]
                {
                    new NanoEffect
                    {
                        Effect = Id((IdentityType)53044, 162319),
                        Version = 4,
                        CriterionCount = 0,
                        Criteria = new NanoCriterion[0],
                        Hits = 1,
                        Amount = 0,
                        Target = NanoEffectTarget.User,
                        SpellList = 9,
                        Arguments = new byte[]
                        {
                            0x00, 0x00, 0x00, 0x05,
                            0x48, 0x65, 0x6C, 0x6C, 0x6F,
                            0x00, 0x00, 0x00, 0x01,
                            0x00, 0x00, 0x00, 0x02,
                            0x00, 0x00, 0x00, 0x03,
                            0x00, 0x00, 0x00, 0x04,
                            0x00, 0x00, 0x00, 0x05
                        }
                    }
                },
                Target = Id(IdentityType.CanbeAffected, unchecked((int)0x0A0B0C02)),
                Apply = true
            };

            AssertRetailPacket(
                "01 24 00 0A 00 01 00 67 00 00 0D B8 0A 0B 0C 02 34 2C 1D 1D " +
                "00 00 C3 50 0A 0B 0C 02 00 00 00 07 E2 00 00 CF 34 00 02 7A " +
                "0F 00 00 00 04 00 00 00 00 00 00 00 01 00 00 00 00 00 00 00 " +
                "02 00 00 00 09 00 00 00 05 48 65 6C 6C 6F 00 00 00 01 00 00 " +
                "00 02 00 00 00 03 00 00 00 04 00 00 00 05 00 00 C3 50 0A 0B " +
                "0C 02 01",
                body,
                0x0124,
                0x00000DB8,
                unchecked((int)0x0A0B0C02));
        }

        [TestMethod]
        public void ApplySpellsRemovalWritesTheLayoutTheClientReaderDemands()
        {
            // No capture carries this message, so the bytes below are not a
            // recording - they are the layout the client's own reader and
            // writer require, and the test exists to hold the three fields in
            // that order. The reader is Gamecode.dll 0x10128EA9 and the writer
            // 0x10128EEF: the effect list, then an identity, then one byte.
            //
            // The list is the same GameData SpellData_t record SpellList and
            // CorpseFullUpdate carry, read here by the same shared code, so
            // this also exercises an effect that carries a criterion - one of
            // the two things that had SpellList wrong for 1,685 captured
            // copies.
            //
            // Apply is false here, which is the direction worth testing. The
            // dispatcher at 0x10128E45 inverts this byte, and the inverted
            // value becomes bit 0 of each effect's flag word; an effect with
            // that bit set subtracts its amount from the target's stat instead
            // of adding it, at 0x100A4F6C. So a zero byte means these effects
            // are coming off, not going on.
            var body = new ApplySpellsMessage
            {
                Identity = Id(IdentityType.CanbeAffected, unchecked((int)0x0A0B0C02)),
                Unknown = 0,
                NanoEffects = new[]
                {
                    new NanoEffect
                    {
                        Effect = Id((IdentityType)53045, 162319),
                        Version = 4,
                        CriterionCount = 1,
                        Criteria = new[]
                        {
                            new NanoCriterion { Stat = 128, Value = 1639, Operator = NanoCriterionOperator.Larger }
                        },
                        Hits = 1,
                        Amount = 0,
                        Target = NanoEffectTarget.None,
                        SpellList = 0,
                        Arguments = new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x2A }
                    }
                },
                Target = Id(IdentityType.CanbeAffected, unchecked((int)0x0A0B0C02)),
                Apply = false
            };

            AssertRetailPacket(
                "01 23 00 0A 00 01 00 5E 00 00 0D B8 0A 0B 0C 02 34 2C 1D 1D " +
                "00 00 C3 50 0A 0B 0C 02 00 00 00 07 E2 00 00 CF 35 00 02 7A " +
                "0F 00 00 00 04 00 00 00 01 00 00 00 80 00 00 06 67 00 00 00 " +
                "02 00 00 00 01 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 " +
                "00 00 00 00 2A 00 00 C3 50 0A 0B 0C 02 00",
                body,
                0x0123,
                0x00000DB8,
                unchecked((int)0x0A0B0C02));
        }

        [TestMethod]
        public void CorpseCarriesOneNanoEffectOfFunction53031()
        {
            // A Bureaucrat Worker's corpse, and the smallest of the 236 in the
            // captures. What it is here to pin down is the tail: a counted list
            // of GameData SpellData_t, the same record SpellList carries, read
            // by the same function in the client and now by the same code here.
            //
            // It used to be eight numbered fields on the message - a list of
            // paired int32s that caught the effect identity, then the version,
            // then seven more int32s, then five singles. That is one effect
            // written out longhand, and it held together only because every
            // captured corpse carries exactly one of them and none carries a
            // criterion. The bytes below are why it held: read as a record they
            // come out as a record, and read flat they came out as the same
            // bytes in the wrong shape.
            //
            // Reading it properly needed one addition to the tail-length table.
            // Game function 53031 carries twenty eight bytes, and this packet
            // gives that directly: the effect starts at its own count and ends
            // where the owner identity begins, and with the record's fixed
            // fields taken off the remainder is exactly twenty eight.
            const string Captured =
                "02 70 00 0A 00 01 01 A0 00 00 0D B8 0A 0B 0C 02 4F 47 4E 05 " +
                "00 00 C7 6A 01 01 18 02 00 00 00 00 08 00 00 00 0B 00 00 00 " +
                "00 00 00 00 00 45 54 23 0F 40 7A 3D 71 44 3E 74 4D 00 00 00 " +
                "00 3E D9 31 AA 00 00 00 00 3F 67 D3 20 00 20 D0 3D 00 00 00 " +
                "00 00 00 00 00 00 6F 00 00 46 F2 00 00 00 00 00 18 18 05 00 " +
                "00 00 17 00 00 00 00 00 00 02 BD 00 00 00 00 00 00 02 BE 00 " +
                "00 00 00 00 00 02 BF 00 00 00 00 00 00 01 9C 00 00 00 01 00 " +
                "00 01 68 00 00 00 5C 00 00 00 DF 00 00 00 00 00 00 00 3B 00 " +
                "00 00 01 00 00 00 04 00 00 00 07 00 00 00 59 00 00 00 01 00 " +
                "00 01 9F 00 00 C3 50 00 00 01 A0 7A 6F B1 F1 00 00 00 2A 00 " +
                "01 77 18 00 00 00 3D 00 00 00 00 00 00 00 08 00 00 46 50 00 " +
                "00 00 22 00 00 00 3C 00 00 00 1D 52 65 6D 61 69 6E 73 20 6F " +
                "66 20 42 75 72 65 61 75 63 72 61 74 20 57 6F 72 6B 65 72 00 " +
                "00 00 00 02 00 00 00 32 00 00 03 F1 00 00 00 03 00 00 07 E2 " +
                "00 00 CF 27 39 F9 16 48 00 00 00 04 00 00 00 00 00 00 00 01 " +
                "00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 " +
                "00 00 01 F4 00 00 00 01 00 00 00 04 00 01 77 38 00 00 00 00 " +
                "00 00 C3 50 7A 6F B1 F1 00 00 17 A6 00 00 00 00 00 00 00 00 " +
                "00 00 00 00 00 00 00 01 00 00 00 00 00 00 00 00 00 00 00 02 " +
                "00 00 00 00 00 00 00 00 00 00 00 03 00 00 00 00 00 00 00 00 " +
                "00 00 00 04 00 00 00 00 00 00 00 00 00 00 00 00";

            AssertRetailRoundTrip(Captured);

            CorpseFullUpdateMessage corpse;
            using (var stream = new MemoryStream(ParseHex(Captured)))
            {
                Message message = new MessageSerializer().Deserialize(stream);
                Assert.IsInstanceOfType(message.Body, typeof(CorpseFullUpdateMessage));
                corpse = (CorpseFullUpdateMessage)message.Body;
            }

            Assert.AreEqual(1, corpse.NanoEffects.Length, "a corpse carries one effect");

            NanoEffect effect = corpse.NanoEffects[0];
            Assert.AreEqual(53031, (int)effect.Effect.Type, "the game function");
            Assert.AreEqual(4, effect.Version);
            Assert.AreEqual(0, effect.CriterionCount, "no captured corpse carries a criterion");
            Assert.AreEqual(0, effect.Criteria.Length);
            Assert.AreEqual(1, effect.Hits);
            Assert.AreEqual(0, effect.Amount);
            Assert.AreEqual(0, effect.SpellList);
            Assert.AreEqual(
                28,
                effect.Arguments.Length,
                "game function 53031 takes seven arguments, and the client's format is what says seven");

            // Two of the seven vary per corpse; the handler takes those two out
            // of the database and fixes the 1 and the 4, which is what this copy
            // has.
            CollectionAssert.AreEqual(
                new byte[]
                {
                    0x00, 0x00, 0x00, 0x00,
                    0x00, 0x00, 0x00, 0x00,
                    0x00, 0x00, 0x01, 0xF4,
                    0x00, 0x00, 0x00, 0x01,
                    0x00, 0x00, 0x00, 0x04,
                    0x00, 0x01, 0x77, 0x38,
                    0x00, 0x00, 0x00, 0x00
                },
                effect.Arguments);

            // The character the corpse used to be, which is not the holder
            // identity thirty bytes into the packet - that one is zero, and its
            // being zero is what puts the position on the wire.
            Assert.AreEqual(IdentityType.CanbeAffected, corpse.Owner.Type);
            Assert.AreEqual(0, corpse.HolderType);
        }

        [TestMethod]
        public void SpellListStunHasNoTailAndSurvivesTheRoundTrip()
        {
            // Mesmeric Gaze - a mezz, captured against a pet's target. Game
            // function 53122 is Stun, and the thing that matters about it is
            // that its effect carries no arguments at all - nothing after
            // the four integers every effect has.
            //
            // 112 copies of this exact shape were unreadable until 2026-09-10,
            // because an effect whose function the table did not know still had
            // four bytes taken off the stream for a graphics value it does not
            // carry.
            // The reader then ran four bytes ahead for the rest of the message
            // and the name came out as rubbish, which is a failure four fields
            // away from its cause.
            AssertRetailRoundTrip(
                "03 94 00 0A 00 01 00 7B 00 00 0D B1 0A 0B 0C 08 4D 45 01 14 " +
                "00 00 C3 50 7A 72 19 37 00 00 00 07 E2 00 00 CF 82 00 03 91 " +
                "62 00 00 00 04 00 00 00 01 00 00 01 C7 00 00 00 00 00 00 00 " +
                "02 00 00 00 01 00 00 00 00 00 00 00 03 00 00 00 09 00 00 C3 " +
                "50 7A 73 D3 EF 00 00 C3 50 7A 72 19 37 00 00 0D 4D 65 73 6D " +
                "65 72 69 63 20 47 61 7A 65 01 00 00 CF 1B 00 03 91 62 00 00 " +
                "00 00 00");
        }

        [TestMethod]
        public void CentralControllerFullUpdateMatchesRetailCaptureByteForByte()
        {
            // A sentry controller standing in a mission, and the first time any
            // of the five message classes that had no C# at all has been seen on
            // the wire.
            //
            // It is a SimpleItemFullUpdate with two bytes on the end, and those
            // two bytes check themselves against the message: the client builds
            // the display name from them with "%s %s controller", and the Name
            // field twenty five bytes earlier reads "Active sentry controller".
            var body = new CentralControllerFullUpdateMessage
            {
                Identity = Id((IdentityType)51010, 0x009DF036),
                Unknown = 0,
                MsgVersion = 11,
                Identitytype = 0,
                Instance = 0,
                Coordinate = new Vector3
                {
                    X = 55.883392333984375f,
                    Y = 5.010002613067627f,
                    Z = 37.427391052246094f
                },
                Heading = new Quaternion { X = 0f, Y = 0f, Z = 0f, W = 1f },
                Playfield = 0x0020D18F,
                Marker = Id((IdentityType)ItemMessageConstants.ItemMessageMarker, 0),
                InventoryId = 0,
                BodyLocation = 111,
                Stats = new[]
                {
                    new GameTuple<CharacterStat, uint> { Value1 = CharacterStat.Flags, Value2 = 0x0A120D00 },
                    new GameTuple<CharacterStat, uint> { Value1 = CharacterStat.StaticInstance, Value2 = 0x00033851 },
                    new GameTuple<CharacterStat, uint> { Value1 = CharacterStat.ACGItemLevel, Value2 = 250 },
                    new GameTuple<CharacterStat, uint> { Value1 = (CharacterStat)702, Value2 = 0x00033851 },
                    new GameTuple<CharacterStat, uint> { Value1 = (CharacterStat)703, Value2 = 0x000338D4 },
                    new GameTuple<CharacterStat, uint> { Value1 = (CharacterStat)412, Value2 = 1 },
                    new GameTuple<CharacterStat, uint> { Value1 = (CharacterStat)501, Value2 = 0 },
                    new GameTuple<CharacterStat, uint> { Value1 = (CharacterStat)500, Value2 = 0 }
                },
                Name = "Active sentry controller",
                Kind = CentralControllerKind.Sentry,
                Status = CentralControllerStatus.Active
            };

            AssertRetailPacket(
                "29 CC 00 0A 00 01 00 B6 00 00 0D B1 0A 0B 0C 08 15 25 33 07 " +
                "00 00 C7 42 00 9D F0 36 00 00 00 00 0B 00 00 00 00 00 00 00 " +
                "00 42 5F 88 98 40 A0 51 F1 42 15 B5 A6 00 00 00 00 00 00 00 " +
                "00 00 00 00 00 3F 80 00 00 00 20 D1 8F 00 0F 42 4F 00 00 00 " +
                "00 00 6F 00 00 23 79 00 00 00 00 0A 12 0D 00 00 00 00 17 00 " +
                "03 38 51 00 00 02 BD 00 00 00 FA 00 00 02 BE 00 03 38 51 00 " +
                "00 02 BF 00 03 38 D4 00 00 01 9C 00 00 00 01 00 00 01 F5 00 " +
                "00 00 00 00 00 01 F4 00 00 00 00 00 00 00 19 41 63 74 69 76 " +
                "65 20 73 65 6E 74 72 79 20 63 6F 6E 74 72 6F 6C 6C 65 72 00 " +
                "02 00",
                body,
                0x29CC,
                0x00000DB1,
                0x0A0B0C08);
        }

        private static void AssertRetailPacket(
            string expectedHex,
            N3Message body,
            ushort sequence,
            int sender,
            int receiver)
        {
            var message = new Message
            {
                Header = new Header
                {
                    MessageId = sequence,
                    PacketType = PacketType.N3Message,
                    Unknown = 1,
                    Sender = sender,
                    Receiver = receiver
                },
                Body = body
            };

            byte[] actual;
            using (var stream = new MemoryStream())
            {
                new MessageSerializer().Serialize(stream, message);
                actual = stream.ToArray();
            }

            byte[] expected = ParseHex(expectedHex);
            CollectionAssert.AreEqual(
                expected,
                actual,
                "Expected ({0}): {1}{2}Actual ({3}): {4}",
                expected.Length,
                BitConverter.ToString(expected),
                Environment.NewLine,
                actual.Length,
                BitConverter.ToString(actual));
        }

        private static void AssertMoveItem(
            string expectedHex,
            ushort sequence,
            int characterId,
            IdentityType sourceType,
            int sourceInstance,
            int destination)
        {
            var body = new MoveItemMessage
            {
                Identity = Id(IdentityType.CanbeAffected, characterId),
                Unknown = 0,
                Source = Id(sourceType, sourceInstance),
                Destination = destination
            };

            AssertRetailPacket(expectedHex, body, sequence, characterId, 2);
        }

        private static void AssertContainerAddItem(
            string expectedHex,
            ushort sequence,
            int sender,
            int receiver,
            int affectedIdentity,
            IdentityType sourceType,
            int sourceInstance,
            IdentityType targetType,
            int targetInstance,
            int targetPlacement)
        {
            var body = new ContainerAddItemMessage
            {
                Identity = Id(IdentityType.CanbeAffected, affectedIdentity),
                Unknown = 0,
                SourceContainer = Id(sourceType, sourceInstance),
                Target = Id(targetType, targetInstance),
                TargetPlacement = targetPlacement
            };

            AssertRetailPacket(expectedHex, body, sequence, sender, receiver);
        }

        private static GenericCmdMessage GenericCmd(
            int characterId,
            byte passOn,
            int verification,
            int serial,
            GenericCmdAction action,
            int flag,
            params Identity[] targets)
        {
            return new GenericCmdMessage
            {
                Identity = Id(IdentityType.CanbeAffected, characterId),
                Unknown = passOn,
                Verification = verification,
                Serial = serial,
                Action = action,
                Flag = flag,
                User = Id(IdentityType.CanbeAffected, characterId),
                Target = targets
            };
        }

        private static GameTuple<CharacterStat, uint> Stat(CharacterStat id, uint value)
        {
            return new GameTuple<CharacterStat, uint> { Value1 = id, Value2 = value };
        }

        private static Identity Id(IdentityType type, int instance)
        {
            return new Identity { Type = type, Instance = instance };
        }

        private static byte[] ParseHex(string value)
        {
            string[] parts = value.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            var result = new byte[parts.Length];
            for (int index = 0; index < parts.Length; index++)
            {
                result[index] = Convert.ToByte(parts[index], 16);
            }

            return result;
        }

        private static GenericCmdMessage DeserializeGenericCmd(string packetHex)
        {
            using (var stream = new MemoryStream(ParseHex(packetHex)))
            {
                var message = new MessageSerializer().Deserialize(stream);
                Assert.IsNotNull(message);
                Assert.IsInstanceOfType(message.Body, typeof(GenericCmdMessage));
                return (GenericCmdMessage)message.Body;
            }
        }
    }
}
