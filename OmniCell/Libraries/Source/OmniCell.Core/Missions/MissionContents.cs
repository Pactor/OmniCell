#region License

// Copyright (c) 2026, OmniCell contributors
//
// This file is part of OmniCell and is distributed under the terms the project
// is licensed under. See the repository root for details.

#endregion

namespace OmniCell.Core.Missions
{
    #region Usings ...

    using System.Collections.Generic;

    using OmniCell.Enums;

    using SmokeLounge.AOtomation.Messaging.GameData;
    using SmokeLounge.AOtomation.Messaging.Messages;
    using SmokeLounge.AOtomation.Messaging.Messages.N3Messages;

    #endregion

    /// <summary>
    /// The doors, chests and objective of a mission, as the client is told
    /// about them.
    /// </summary>
    /// <remarks>
    /// In an ordinary playfield the client already has the doors and the
    /// furniture: they are in its own playfield file and the server only says
    /// which are locked. A mission has no file, so every one of them has to go
    /// out on the wire, and the captured zone-in does exactly that - 209 door
    /// updates and 93 chests in the first minutes of one mission.
    ///
    /// Everything here is drawn from 20260926-135805 stream 10. What is
    /// deliberately not here is behaviour: these are sent so the building
    /// looks like a building, and opening a chest is not wired up.
    /// </remarks>
    public static class MissionContents
    {
        #region What the captures carry

        /// <summary>
        /// The marker every mission dynel carries.
        /// </summary>
        private const int MarkerType = 1000015;

        /// <summary>
        /// A door's state machine is this marker at instance 1, a chest's is
        /// instance 7.
        /// </summary>
        private const int DoorStateMachine = 1;

        /// <summary>
        /// A chest's marker instance.
        /// </summary>
        private const int ChestMarker = 7;

        /// <summary>
        /// 11 in every captured mission dynel.
        /// </summary>
        private const int MsgVersion = 11;

        /// <summary>
        /// 111 in every captured mission dynel - the one used for a thing
        /// standing in the world rather than in a container.
        /// </summary>
        private const byte BodyLocation = 111;

        /// <summary>
        /// The door template, from the door's own StaticInstance stat.
        /// </summary>
        private const int DoorTemplate = 41886;

        /// <summary>
        /// The tail versions a captured door and chest carry.
        /// </summary>
        private const int TailVersion = 2;

        /// <summary>
        /// A door's own version, after the lock and the keyholders.
        /// </summary>
        private const int DoorVersion = 2;

        /// <summary>
        /// The end version a chest carries and a door does not.
        /// </summary>
        private const int ChestTailEndVersion = 3;

        /// <summary>
        /// A door stands a centimetre below the floor its room is on.
        /// </summary>
        /// <remarks>
        /// The captured doors read y 5 where the floor reads 5.01.
        /// </remarks>
        private const float DoorDrop = 0.01f;

        /// <summary>
        /// A chest stands a little above the floor.
        /// </summary>
        /// <remarks>
        /// 5.1 against a floor of 5.01 in the captures.
        /// </remarks>
        private const float ChestLift = 0.09f;

        #endregion

        /// <summary>
        /// Everything standing in a mission, as the messages that describe it.
        /// </summary>
        /// <remarks>
        /// Built rather than sent, so the same list can be put through the
        /// serializer by a tool with no server running. A packet the client
        /// drops is a building with a hole in it and nothing in the log.
        /// </remarks>
        public static List<MessageBody> Messages(MissionOffer mission)
        {
            var messages = new List<MessageBody>();
            if (mission == null || mission.Built == null)
            {
                return messages;
            }

            int playfield = mission.PlayfieldInstance;

            foreach (MissionLayoutDoor door in mission.Built.Layout.Doors)
            {
                messages.Add(Door(door, playfield, door.Instance));
            }

            foreach (MissionFurniture thing in mission.Built.Furniture)
            {
                messages.Add(
                    thing.Kind == MissionFurnitureKind.Chest
                        ? (MessageBody)Chest(thing, playfield, thing.Instance)
                        : Objective(thing, mission, playfield, thing.Instance));
            }

            return messages;
        }

        #region One of each

        /// <summary>
        /// A door between two rooms, or the one onto the outside.
        /// </summary>
        /// <remarks>
        /// Room and AdjoiningRoom are indexes into the placement list the
        /// zone-in packet sent, and -1 is the outside - the captured building's
        /// one door with Room -1 is the way in.
        /// </remarks>
        private static DoorFullUpdateMessage Door(MissionLayoutDoor door, int playfield, int instance)
        {
            return new DoorFullUpdateMessage
                   {
                       Identity = new Identity { Type = IdentityType.Door, Instance = instance },
                       Unknown = 0,
                       Owner = Identity.None,
                       MsgVersion = MsgVersion,
                       Coordinate = new Vector3
                                    {
                                        X = door.X,
                                        Y = MissionBuilding.HeightOf(door.Floor) - DoorDrop,
                                        Z = door.Z
                                    },
                       Heading = new Quaternion { X = 0, Y = 0, Z = 0, W = 1 },
                       Playfield = playfield,
                       StateMachine = new Identity
                                      {
                                          Type = (IdentityType)MarkerType,
                                          Instance = DoorStateMachine
                                      },
                       InventoryId = 0,
                       BodyLocation = BodyLocation,
                       Stats = Wire(DoorTemplate),
                       Name = string.Empty,
                       TailVersion = TailVersion,

                       // 50 is what a door reads with no lock on it.
                       LockDifficulty = 50,
                       Keyholders = new Identity[0],
                       DoorVersion = DoorVersion,
                       Room = (short)door.Room,
                       AdjoiningRoom = (short)door.AdjoiningRoom
                   };
        }

        private static ChestItemFullUpdateMessage Chest(
            MissionFurniture chest, int playfield, int instance)
        {
            return new ChestItemFullUpdateMessage
                   {
                       Identity = new Identity { Type = (IdentityType)51017, Instance = instance },
                       Unknown = 0,
                       Owner = Identity.None,
                       MsgVersion = MsgVersion,
                       Coordinates = new Vector3
                                     {
                                         X = chest.X,
                                         Y = MissionBuilding.HeightOf(chest.Floor) + ChestLift,
                                         Z = chest.Z
                                     },
                       Heading = new Quaternion { X = 0, Y = 0, Z = 0, W = 1 },
                       PlayfieldId = playfield,
                       Marker = new Identity { Type = (IdentityType)MarkerType, Instance = ChestMarker },
                       InventoryId = 0,
                       BodyLocation = BodyLocation,
                       Stats = Wire(0),
                       Name = string.Empty,
                       TailVersion = TailVersion,
                       LockDifficulty = chest.LockDifficulty,
                       Keyholders = new Identity[0],
                       TailEndVersion = ChestTailEndVersion
                   };
        }

        /// <summary>
        /// The thing the mission sends the player after, lying on the floor.
        /// </summary>
        /// <remarks>
        /// Identity type 51022 with QuestInstance set, and ACGItemLevel is the
        /// mission's quality rather than the item's own - which is what the
        /// captured return item objective carries.
        /// </remarks>
        private static SimpleItemFullUpdateMessage Objective(
            MissionFurniture thing, MissionOffer mission, int playfield, int instance)
        {
            var stats = new List<GameTuple<CharacterStat, uint>>
                        {
                            Stat(StatIds.acgitemlevel, (uint)mission.Quality),
                            Stat(StatIds.multiplecount, 1),
                            Stat(StatIds.questinstance, 1)
                        };

            return new SimpleItemFullUpdateMessage
                   {
                       Identity = new Identity { Type = (IdentityType)51022, Instance = instance },
                       Unknown = 0,
                       Owner = Identity.None,
                       MsgVersion = MsgVersion,
                       Coordinate = new Vector3
                                    {
                                        X = thing.X,
                                        Y = MissionBuilding.HeightOf(thing.Floor),
                                        Z = thing.Z
                                    },
                       Heading = new Quaternion { X = 0, Y = 0, Z = 0, W = 1 },
                       Playfield = playfield,
                       Marker = new Identity { Type = (IdentityType)MarkerType, Instance = 0 },
                       InventoryId = 0,
                       BodyLocation = BodyLocation,
                       Stats = stats.ToArray(),
                       Name = string.Empty
                   };
        }

        private static GameTuple<CharacterStat, uint>[] Wire(int template)
        {
            var stats = new List<GameTuple<CharacterStat, uint>>
                        {
                            Stat(StatIds.multiplecount, 1)
                        };
            if (template != 0)
            {
                stats.Insert(0, Stat(StatIds.staticinstance, (uint)template));
            }

            return stats.ToArray();
        }

        private static GameTuple<CharacterStat, uint> Stat(StatIds stat, uint value)
        {
            return new GameTuple<CharacterStat, uint>
                   {
                       Value1 = (CharacterStat)(int)stat,
                       Value2 = value
                   };
        }

        #endregion
    }
}
