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
        /// Build the mission this offer describes.
        /// </summary>
        /// <remarks>
        /// Everything about a mission follows from the offer, so the building
        /// is not made when the terminal is asked - it is made when somebody
        /// takes the mission, and from a seed the offer's own identity gives,
        /// which means the same mission always builds the same building.
        /// </remarks>
        public static Mission Build(MissionOffer offer)
        {
            if (offer == null) throw new ArgumentNullException("offer");

            MissionPool pool;
            if (!MissionPoolLoader.Pools.TryGetValue(offer.Pool, out pool))
            {
                return null;
            }

            var factory = new MissionFactory(pool, offer.Instance);
            return factory.Build(offer.Quality, offer.Type, Math.Max(1, offer.Floors));
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
