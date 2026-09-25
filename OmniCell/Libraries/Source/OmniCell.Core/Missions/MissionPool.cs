namespace OmniCell.Core.Missions
{
    using System.Collections.Generic;

    /// <summary>
    /// The set of rooms one kind of mission is built out of.
    /// </summary>
    /// <remarks>
    /// A mission's zone-in packet names its pool as a playfield id in
    /// BuildingGeneratorData's TemplatePlayfield, and there are ten of them:
    /// 320 Midtech, 321 HiTech, 322 Cave, 324 Clan, 331 tarm, 341 Grey Caves,
    /// 346 Omnilab, 351 Subway Ventil, 362 Shadowlands, 382 Alien.
    ///
    /// The client holds the geometry, so a server sends only the pool and the
    /// placements. This class is what a server needs to make placements that
    /// the client can actually build: which rooms exist, how large each is and
    /// where its floor is.
    /// </remarks>
    public class MissionPool
    {
        /// <summary>
        /// The playfield id the zone-in packet carries for this pool.
        /// </summary>
        public int Playfield { get; set; }

        /// <summary>
        /// The client's name for the pool playfield, such as
        /// "AutocontentMidtech".
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The rooms, in the order the zone-in packet's room index counts.
        /// </summary>
        public List<MissionPoolRoom> Rooms { get; set; }

        public MissionPool()
        {
            this.Rooms = new List<MissionPoolRoom>();
        }
    }
}
