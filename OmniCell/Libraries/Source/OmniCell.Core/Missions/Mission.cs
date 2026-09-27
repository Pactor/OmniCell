namespace OmniCell.Core.Missions
{
    using System.Collections.Generic;

    using SmokeLounge.AOtomation.Messaging.GameData;

    /// <summary>
    /// Something a built mission contains, in world coordinates.
    /// </summary>
    public class MissionSpawn
    {
        /// <summary>
        /// Which placed room it stands in, as an index into the layout.
        /// </summary>
        public int Room { get; set; }

        /// <summary>
        /// World metres.
        /// </summary>
        public float X { get; set; }

        /// <summary>
        /// World metres.
        /// </summary>
        public float Z { get; set; }

        /// <summary>
        /// The floor, counting up from the lowest.
        /// </summary>
        public int Floor { get; set; }
    }

    /// <summary>
    /// A chest or a floor object.
    /// </summary>
    public class MissionFurniture : MissionSpawn
    {
        public MissionFurnitureKind Kind { get; set; }

        /// <summary>
        /// How hard it is to pick. 50 is what a lock reads when there is none,
        /// which is 220 of the 255 captured chests.
        /// </summary>
        public int LockDifficulty { get; set; }

        /// <summary>
        /// True when nobody has recorded where this room puts its furniture and
        /// the position is only somewhere on its floor.
        /// </summary>
        /// <remarks>
        /// 230 of the 639 rooms have a spot on record. Without a fallback the
        /// other 409 stay empty and a building comes out with two chests where
        /// retail gives it nine, so the fallback earns its place - but it is a
        /// guess and says so, and it stops being needed as more runs are
        /// recorded.
        /// </remarks>
        public bool Approximate { get; set; }
    }

    /// <summary>
    /// One monster, placed and levelled.
    /// </summary>
    public class MissionMonster : MissionSpawn
    {
        /// <summary>
        /// SimpleCharFullUpdate's MonsterData.
        /// </summary>
        public int Monster { get; set; }

        public string Name { get; set; }

        public int Level { get; set; }

        /// <summary>
        /// How much health it has.
        /// </summary>
        public int Health { get; set; }
    }

    /// <summary>
    /// A whole mission: the building, and everything standing in it.
    /// </summary>
    /// <remarks>
    /// Everything here is drawn from what missions were measured to contain -
    /// see Documentation/Missions.md. Nothing in it is on the wire yet; this is
    /// the thing a zone server would build and then send as a
    /// BuildingGeneratorData with a stream of full updates behind it.
    /// </remarks>
    public class Mission
    {
        public Mission()
        {
            this.Furniture = new List<MissionFurniture>();
            this.Monsters = new List<MissionMonster>();
        }

        /// <summary>
        /// The building.
        /// </summary>
        public MissionLayout Layout { get; set; }

        /// <summary>
        /// The quality this mission was rolled at, which is what the monsters
        /// are levelled against.
        /// </summary>
        public int Quality { get; set; }

        /// <summary>
        /// What the player is here to do.
        /// </summary>
        public MissionType Type { get; set; }

        /// <summary>
        /// Chests and floor objects.
        /// </summary>
        public List<MissionFurniture> Furniture { get; set; }

        /// <summary>
        /// The monsters.
        /// </summary>
        public List<MissionMonster> Monsters { get; set; }

        /// <summary>
        /// Where the objective stands, for the types that have one in the
        /// world. Null for a mission whose objective is a monster.
        /// </summary>
        public MissionFurniture Objective { get; set; }
    }
}
