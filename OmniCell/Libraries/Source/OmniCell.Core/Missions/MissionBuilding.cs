namespace OmniCell.Core.Missions
{
    using System;
    using System.Linq;

    using SmokeLounge.AOtomation.Messaging.GameData;

    /// <summary>
    /// The building a mission is run in, and the shape the client is told to
    /// build it from.
    /// </summary>
    /// <remarks>
    /// The client owns the geometry. A server sends it the pool and a list of
    /// placements, and the client puts the rooms together - which is what
    /// <see cref="BuildingGeneratorData"/> is, and it rides on the zone-in
    /// packet as a DbObject of identity type 51103.
    ///
    /// So the whole of a mission's world is: this list, and then the ordinary
    /// dynels for what stands in the rooms. <see cref="MissionFactory"/> makes
    /// both from the pack.
    /// </remarks>
    public static class MissionBuilding
    {
        /// <summary>
        /// The grid every captured mission is laid out on.
        /// </summary>
        /// <remarks>
        /// 30 by 30 slots and a world height of 64 in all 276 captured
        /// buildings, whatever the pool and whatever the size.
        /// </remarks>
        public const int Grid = 30;

        /// <summary>
        /// The world height every captured mission carries.
        /// </summary>
        public const int WorldHeight = 64;

        /// <summary>
        /// 150 in every captured mission, on all three channels.
        /// </summary>
        private const byte Ambient = 150;

        /// <summary>
        /// The revision and version every captured generator carries.
        /// </summary>
        private const int Revision = 2;

        /// <summary>
        /// The revision and version every captured generator carries.
        /// </summary>
        private const short Version = 3;

        /// <summary>
        /// The identity type a mission building goes by.
        /// </summary>
        public const IdentityType BuildingType = (IdentityType)51103;

        /// <summary>
        /// How far apart the floors are, in metres.
        /// </summary>
        /// <remarks>
        /// 64, which is the generator's own WorldHeight. A four floor mission
        /// recorded on 2026-09-25 put every monster on floor 0 at y 5.01 and
        /// every monster on floor 1 at 69.01, and the difference is exactly
        /// the WorldHeight every captured building carries. So that field is
        /// the gap between floors rather than a ceiling height.
        /// </remarks>
        public const float FloorHeight = 64f;

        /// <summary>
        /// The height the floor of a room stands at, on floor zero.
        /// </summary>
        /// <remarks>
        /// 5.01 in the captures, and a monster or a chest stands on it. Rooms
        /// with a ramp or a step in them run from about 3.4 to 10, so this is
        /// the usual height rather than the only one.
        /// </remarks>
        public const float GroundHeight = 5.01f;

        /// <summary>
        /// Where a character walking in through the door ends up.
        /// </summary>
        /// <remarks>
        /// Just inside the way in. The captured mission's entrance door stood
        /// at 300, 145 and the character landed at 299.9, 5.01, 145.4 - half a
        /// metre inside it, on the floor.
        /// </remarks>
        public static void Landing(Mission mission, out float x, out float y, out float z)
        {
            x = mission.Layout.EntranceX;
            z = mission.Layout.EntranceZ;
            y = GroundHeight;

            // A step inwards, away from the side of the room the door is on.
            // Which way that is: world z runs the other way from the grid's,
            // because a room's world origin is (grid height - z - depth) * 10,
            // so the north side of a room is its low z edge and stepping in
            // from it raises z. East and west are not turned over: the
            // captured entrance stood on its room's high x edge and the
            // character landed a tenth of a metre below it.
            const float Step = 0.5f;
            switch (mission.Layout.EntranceSide)
            {
                case MissionDoorSide.North: z += Step; break;
                case MissionDoorSide.South: z -= Step; break;
                case MissionDoorSide.East: x -= Step; break;
                case MissionDoorSide.West: x += Step; break;
            }
        }

        /// <summary>
        /// How high a floor stands.
        /// </summary>
        public static float HeightOf(int floor)
        {
            return GroundHeight + (floor * FloorHeight);
        }

        /// <summary>
        /// Build the mission this offer describes.
        /// </summary>
        /// <remarks>
        /// Everything about a mission follows from the offer, so the building
        /// is not made when the terminal is asked - it is made when somebody
        /// takes the mission, and from a seed the offer's own identity gives,
        /// which means the same mission always builds the same building.
        /// </remarks>
        /// <param name="instances">
        /// Where the instances the client will know each door, chest and
        /// monster by come from. The server allocates them out of its object
        /// pool; a tool can count.
        /// </param>
        public static Mission Build(MissionOffer offer, Func<int> instances)
        {
            if (offer == null) throw new ArgumentNullException("offer");
            if (instances == null) throw new ArgumentNullException("instances");

            MissionPool pool;
            if (!MissionPoolLoader.Pools.TryGetValue(offer.Pool, out pool))
            {
                return null;
            }

            var factory = new MissionFactory(pool, offer.Instance, offer.Quality);
            Mission mission = factory.Build(offer.Quality, offer.Type, Math.Max(1, offer.Floors));
            if (mission == null)
            {
                return null;
            }

            foreach (MissionLayoutDoor door in mission.Layout.Doors) door.Instance = instances();
            foreach (MissionFurniture thing in mission.Furniture) thing.Instance = instances();
            foreach (MissionMonster monster in mission.Monsters) monster.Instance = instances();

            Target(mission, offer);
            return mission;
        }

        /// <summary>
        /// The one the mission is about, for the types that name a creature.
        /// </summary>
        /// <remarks>
        /// A kill person mission names something and the player has to kill
        /// that, not any of them - the assignment says "the big boss-man of
        /// the hole is Quinn Buhlig". So one of the monsters is given the name
        /// the offer used, and it is the one furthest into the building, which
        /// is where a boss stands.
        ///
        /// A find person mission is the same creature with a different job:
        /// the assignment says to track somebody down and observe them, and
        /// finding them is targeting them. Both name a person, so both get
        /// one.
        ///
        /// Which monster retail picks is not known; that it is one of them is,
        /// because the name in the assignment is a creature's.
        /// </remarks>
        private static void Target(Mission mission, MissionOffer offer)
        {
            bool names = offer.Type == MissionType.KillPerson || offer.Type == MissionType.FindPerson;
            if (!names || string.IsNullOrEmpty(offer.Objective))
            {
                return;
            }

            MissionMonster boss = mission.Monsters
                .OrderByDescending(m => Math.Abs(m.Floor))
                .ThenByDescending(m => m.Room)
                .FirstOrDefault();
            if (boss == null)
            {
                return;
            }

            boss.Name = offer.Objective;
            boss.IsObjective = true;
        }

        /// <summary>
        /// The placements, as the zone-in packet carries them.
        /// </summary>
        /// <param name="mission">The built mission.</param>
        /// <param name="instance">
        /// The building's own instance, which the zone-in packet names twice -
        /// once as the generator's identity and once as the message's ModelId.
        /// </param>
        public static BuildingGeneratorData Generator(Mission mission, int instance)
        {
            if (mission == null) throw new ArgumentNullException("mission");

            return new BuildingGeneratorData
                   {
                       Identity = new Identity { Type = BuildingType, Instance = instance },
                       Revision = Revision,
                       Version = Version,
                       Width = (short)mission.Layout.GridWidth,
                       Height = (short)mission.Layout.GridHeight,
                       WorldHeight = (short)mission.Layout.WorldHeight,
                       TemplatePlayfield = mission.Layout.Playfield,
                       AmbientRed = Ambient,
                       AmbientGreen = Ambient,
                       AmbientBlue = Ambient,
                       Rooms = mission.Layout.Rooms.ToArray()
                   };
        }
    }
}
