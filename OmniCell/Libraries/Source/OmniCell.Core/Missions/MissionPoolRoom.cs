namespace OmniCell.Core.Missions
{
    using System;

    /// <summary>
    /// One room a mission can be built out of.
    /// </summary>
    /// <remarks>
    /// A mission's zone-in packet places rooms by index into its pool's room
    /// list - see BuildingGeneratorData - so <see cref="Index"/> is the number
    /// that goes on the wire and everything else here is what a generator needs
    /// to decide where the room may go.
    ///
    /// The footprint is in slots. A slot is ten metres and a cell is two, and a
    /// pool room is always five cells per slot plus one: the extra row and
    /// column are the cells it shares with the rooms beyond it. That holds for
    /// every one of the 639 rooms, which is how the rule was checked rather than
    /// assumed.
    /// </remarks>
    public class MissionPoolRoom
    {
        /// <summary>
        /// The room's place in its pool's list, which is what the zone-in packet
        /// sends.
        /// </summary>
        public int Index { get; set; }

        /// <summary>
        /// The client's own name for the room, kept for reading logs by.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// How many ten metre slots the room spans east to west.
        /// </summary>
        public int SlotsWidth { get; set; }

        /// <summary>
        /// How many ten metre slots the room spans north to south.
        /// </summary>
        public int SlotsHeight { get; set; }

        /// <summary>
        /// What the room's name says it is for.
        /// </summary>
        public MissionRoomRole Role { get; set; }

        /// <summary>
        /// One bit per cell, row major, set where the room has a floor tile.
        /// </summary>
        /// <remarks>
        /// <see cref="CellsWidth"/> by <see cref="CellsHeight"/> bits, packed
        /// eight to a byte with the lowest bit of a byte holding the first cell
        /// of its run.
        ///
        /// A cell is floor when the room has any tile id there at all. The last
        /// row and the last column are the shared cells and they carry only two
        /// values in all 639 rooms: zero, or 0x80 in the 110 cells where this
        /// room claims the shared cell rather than leaving it to its neighbour.
        /// Both are kept here as they are - a set bit is a floor either way -
        /// because the composition rule is that a shared cell is floor when
        /// <em>any</em> room covering it says so, and the neighbour's own first
        /// row or column is what usually says it.
        /// </remarks>
        public byte[] Floor { get; set; }

        /// <summary>
        /// The room's width in two metre cells.
        /// </summary>
        public int CellsWidth
        {
            get { return (this.SlotsWidth * 5) + 1; }
        }

        /// <summary>
        /// The room's depth in two metre cells.
        /// </summary>
        public int CellsHeight
        {
            get { return (this.SlotsHeight * 5) + 1; }
        }

        /// <summary>
        /// Whether the room has a floor at a cell, in the room's own
        /// orientation.
        /// </summary>
        /// <param name="x">The cell column, from zero.</param>
        /// <param name="z">The cell row, from zero.</param>
        /// <returns>True when the room has a tile there.</returns>
        public bool HasFloor(int x, int z)
        {
            if (x < 0 || z < 0 || x >= this.CellsWidth || z >= this.CellsHeight) return false;
            if (this.Floor == null) return false;

            int bit = (z * this.CellsWidth) + x;
            int index = bit >> 3;
            if (index >= this.Floor.Length) return false;

            return (this.Floor[index] & (1 << (bit & 7))) != 0;
        }

        /// <summary>
        /// How many bytes a floor mask for this footprint takes.
        /// </summary>
        public int FloorBytes
        {
            get { return (int)Math.Ceiling(this.CellsWidth * this.CellsHeight / 8.0); }
        }
    }
}
