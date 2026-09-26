namespace OmniCell.Core.Missions
{
    using System.Collections.Generic;

    using SmokeLounge.AOtomation.Messaging.GameData;

    /// <summary>
    /// A door in a built mission, between two placed rooms or onto the outside.
    /// </summary>
    public class MissionLayoutDoor
    {
        /// <summary>
        /// Where it stands, in world metres.
        /// </summary>
        public int X { get; set; }

        /// <summary>
        /// Where it stands, in world metres.
        /// </summary>
        public int Z { get; set; }

        /// <summary>
        /// The floor it is on.
        /// </summary>
        public int Floor { get; set; }

        /// <summary>
        /// One side, as an index into the placement list.
        /// </summary>
        /// <remarks>
        /// -1 for the outside, which is what the entrance carries. That is the
        /// client's own convention, read off the doors a retail server sends.
        /// </remarks>
        public int Room { get; set; }

        /// <summary>
        /// The other side, likewise.
        /// </summary>
        public int AdjoiningRoom { get; set; }
    }

    /// <summary>
    /// A built mission: where every room went, and every door between them.
    /// </summary>
    /// <remarks>
    /// <see cref="Rooms"/> is exactly what BuildingGeneratorData carries, so a
    /// layout goes onto the wire without translation.
    /// </remarks>
    public class MissionLayout
    {
        public MissionLayout()
        {
            this.Rooms = new List<BuildingRoomInfo>();
            this.Doors = new List<MissionLayoutDoor>();
        }

        /// <summary>
        /// The pool the rooms are indexes into.
        /// </summary>
        public int Playfield { get; set; }

        /// <summary>
        /// The grid, in slots. Thirty by thirty in all 276 captured buildings.
        /// </summary>
        public int GridWidth { get; set; }

        /// <summary>
        /// The grid, in slots.
        /// </summary>
        public int GridHeight { get; set; }

        /// <summary>
        /// How tall a floor is. 64 in all 276.
        /// </summary>
        public int WorldHeight { get; set; }

        /// <summary>
        /// The placements, in the order the zone-in packet sends them, which is
        /// also what a door's Room and AdjoiningRoom index.
        /// </summary>
        public List<BuildingRoomInfo> Rooms { get; set; }

        /// <summary>
        /// One per socket position. Every socket is used; the single one that
        /// only one room reaches is the way in.
        /// </summary>
        public List<MissionLayoutDoor> Doors { get; set; }
    }
}
