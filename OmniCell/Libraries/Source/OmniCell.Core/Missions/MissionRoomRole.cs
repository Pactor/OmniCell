namespace OmniCell.Core.Missions
{
    /// <summary>
    /// What a pool room is for, as far as its own name says.
    /// </summary>
    /// <remarks>
    /// Read off the room name and nothing else, because nothing else says it.
    /// The pools name their rooms plainly - militaryot_elevator, hitech_bossroom2,
    /// Subway_Ent_1, clan_startroom3 - and the words are consistent within a pool
    /// even though they differ between pools. A room whose name carries none of
    /// them is <see cref="Ordinary"/>, which is 532 of the 639.
    ///
    /// This is a convenience for a generator, not a fact off the wire. A mission
    /// carries only a room index, so the client does not care what we call these;
    /// the value of the classification is that a generator has to put the
    /// entrance somewhere and the boss somewhere else.
    /// </remarks>
    public enum MissionRoomRole : byte
    {
        /// <summary>
        /// Nothing in the name says what it is for. 532 of the 639 rooms.
        /// </summary>
        Ordinary = 0,

        /// <summary>
        /// The room a mission is entered through. 41 rooms, in every pool.
        /// </summary>
        Entrance = 1,

        /// <summary>
        /// Where the player lands. 30 rooms, and six pools have none.
        /// </summary>
        /// <remarks>
        /// Whether retail uses these as the landing room or as something else is
        /// not established here - the name is the whole of the evidence.
        /// </remarks>
        StartRoom = 2,

        /// <summary>
        /// The room at the far end. 27 rooms, and every pool has at least one.
        /// </summary>
        /// <remarks>
        /// In the three missions walked on 2026-09-23 the boss room was on top in
        /// HiTech and Midtech and at the bottom in Grey Caves, so a generator
        /// cannot assume it goes on the highest floor.
        /// </remarks>
        BossRoom = 3,

        /// <summary>
        /// A room that joins two floors. Three rooms, in pools 320, 321 and 324.
        /// </summary>
        /// <remarks>
        /// Seven pools name no elevator at all, and the two missions walked in
        /// Grey Caves still had working lifts - the up and down buttons are items
        /// the server spawns and they need not stand in a room named for them. So
        /// an absent elevator room does not mean an absent lift.
        /// </remarks>
        Elevator = 4,

        /// <summary>
        /// A stair or a ramp between floors. Six rooms, in pools 320, 321, 324
        /// and 351.
        /// </summary>
        Ramp = 5
    }
}
