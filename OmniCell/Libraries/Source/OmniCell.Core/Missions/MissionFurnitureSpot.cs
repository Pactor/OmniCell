namespace OmniCell.Core.Missions
{
    /// <summary>
    /// What kind of thing a furniture spot holds.
    /// </summary>
    public enum MissionFurnitureKind : byte
    {
        /// <summary>
        /// A chest. 2,335 of the 2,983 observed objects.
        /// </summary>
        Chest = 0,

        /// <summary>
        /// Something standing on the floor - the objective, or a lift button.
        /// </summary>
        Item = 1
    }

    /// <summary>
    /// A place inside a room template where a mission server has been seen to
    /// put something.
    /// </summary>
    /// <remarks>
    /// Furniture is not scattered. Taking every object a mission spawned back
    /// through the placement transform - the one the door sockets go out
    /// through, run backwards - lands it in its room template's own frame, and
    /// the offsets repeat to the centimetre across buildings that have nothing
    /// else in common. clan_wc put a chest in one of two places over fifty
    /// sightings; clan_stair one of two over fifty five.
    ///
    /// So this is a lookup rather than a rule, and it is measured rather than
    /// derived: 2,983 objects over 276 buildings, all but six of which fell
    /// inside a placed room, giving 612 spots over 230 of the 639 rooms. The
    /// rooms with none are the ones nobody has walked into yet.
    ///
    /// <see cref="X"/> and <see cref="Z"/> are in cells from the template's
    /// origin and are not whole numbers - furniture sits where it was authored,
    /// not on the cell grid.
    /// </remarks>
    public class MissionFurnitureSpot
    {
        /// <summary>
        /// What goes here.
        /// </summary>
        public MissionFurnitureKind Kind { get; set; }

        /// <summary>
        /// Cells from the template's origin, fractional.
        /// </summary>
        public float X { get; set; }

        /// <summary>
        /// Cells from the template's origin, fractional.
        /// </summary>
        public float Z { get; set; }

        /// <summary>
        /// How many times something was seen here.
        /// </summary>
        /// <remarks>
        /// A room usually has one or two spots and picks between them, so this
        /// is the weight to choose by. 94 of the 249 room-and-kind groups have
        /// only one spot, and two thirds of a group's sightings are on its
        /// commonest.
        /// </remarks>
        public int Seen { get; set; }
    }
}
