namespace OmniCell.Core.Missions
{
    /// <summary>
    /// Which wall of a cell a door stands in.
    /// </summary>
    /// <remarks>
    /// The two low bits of the room record's door value, and the numbering is
    /// the client's rather than ours - it is what makes the value decode to a
    /// cell inside the room in all 1,889 records.
    /// </remarks>
    public enum MissionDoorSide : byte
    {
        /// <summary>
        /// The wall at increasing z.
        /// </summary>
        South = 0,

        /// <summary>
        /// The wall at increasing x.
        /// </summary>
        East = 1,

        /// <summary>
        /// The wall at decreasing z.
        /// </summary>
        North = 2,

        /// <summary>
        /// The wall at decreasing x.
        /// </summary>
        West = 3
    }

    /// <summary>
    /// A place in a room template where a door can stand.
    /// </summary>
    /// <remarks>
    /// The room record in the client's tilemap ends with a count and that many
    /// pairs of int16, and this is one pair decoded. The encoding was settled on
    /// 2026-09-25:
    ///
    ///     value = 4 * (z * 5W + x) + side
    ///
    /// where W is the room's width in slots, (x, z) is a cell on the room's
    /// 5W by 5H interior grid - the tile grid less the shared last row and
    /// column - and side is <see cref="MissionDoorSide"/>.
    ///
    /// Two things say it is right. Every one of the 1,889 records in the ten
    /// pools decodes to a cell inside its own room, none out of range. And
    /// placing these sockets through the room lists of twenty six recorded
    /// missions - six pools, 451 doors, from the AOBuddy10 bot's own run
    /// recordings - reproduces every one of those 451 door positions, to the
    /// metre.
    ///
    /// Getting there corrected the placement rotation. The first version of
    /// this, checked against a single Grey Caves mission, turned a rotated room
    /// clockwise and reproduced all seventeen of that mission's doors; over the
    /// 451 it managed 362. Grey Caves could not tell the difference because its
    /// rooms are nearly all square. Anticlockwise gives 451 of 451, and the
    /// extractor now checks a HiTech building as well, which scores 21 of 21
    /// one way and 8 of 21 the other.
    ///
    /// A socket is not a door. The count of sockets in a template is the number
    /// of doors that room gets - checked room by room against the same capture,
    /// where the count agreed exactly everywhere the capture was complete - but
    /// which neighbour each one opens onto is the generator's business, and 229
    /// of the 1,889 sit on an interior cell rather than the boundary, which is a
    /// door between parts of one room.
    /// </remarks>
    public class MissionDoorSocket
    {
        /// <summary>
        /// The cell's column on the room's 5W by 5H interior grid.
        /// </summary>
        public int X { get; set; }

        /// <summary>
        /// The cell's row on the room's 5W by 5H interior grid.
        /// </summary>
        public int Z { get; set; }

        /// <summary>
        /// Which wall of that cell the door stands in.
        /// </summary>
        public MissionDoorSide Side { get; set; }

        /// <summary>
        /// The room this socket opens onto, where the template says.
        /// </summary>
        /// <remarks>
        /// The first int16 of the pair. It is 0xFFFF in 1,851 of the 1,889,
        /// which is carried here as -1: a pool room is placed by a generator
        /// that has not been written yet, so the template cannot know its
        /// neighbour. The 38 that do name one are rooms whose two halves are
        /// joined the same way every time.
        ///
        /// The door the server sends carries the answer in its own Room and
        /// AdjoiningRoom, and those are indexes into the placement list the
        /// zone-in packet sent, with -1 for the outside - which is how the
        /// entrance is marked.
        /// </remarks>
        public int AdjoiningRoom { get; set; }
    }
}
