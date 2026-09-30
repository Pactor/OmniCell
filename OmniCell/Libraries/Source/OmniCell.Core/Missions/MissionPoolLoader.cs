namespace OmniCell.Core.Missions
{
    using System;
    using System.Collections.Generic;

    using OmniCell.Core.Content;

    /// <summary>
    /// Holds the room pools missions are built out of.
    /// </summary>
    /// <remarks>
    /// Loaded the way items and playfields are, from a content pack beside the
    /// engine. The pack is small - ten pools, 639 rooms - so it is read whole at
    /// start-up and kept.
    ///
    /// Nothing calls this yet. It exists because the data it carries was pulled
    /// out of the client and is worth having in the repository before there is a
    /// generator to consume it, not because a generator is waiting on it.
    /// </remarks>
    public static class MissionPoolLoader
    {
        /// <summary>
        /// Every pool, by the playfield id the zone-in packet names it with.
        /// </summary>
        public static Dictionary<int, MissionPool> Pools = new Dictionary<int, MissionPool>();

        /// <summary>
        /// Load the pools from the pack beside the engine.
        /// </summary>
        /// <returns>The number of pools loaded.</returns>
        public static int CacheAllMissionPools()
        {
            return CacheAllMissionPools("missionpools.ocp");
        }

        /// <summary>
        /// Load the pools from a named pack.
        /// </summary>
        /// <param name="fname">The pack to read.</param>
        /// <returns>The number of pools loaded.</returns>
        public static int CacheAllMissionPools(string fname)
        {
            if (string.IsNullOrEmpty(fname)) throw new ArgumentNullException("fname");

            var loaded = new Dictionary<int, MissionPool>();
            foreach (MissionPool pool in OmniCellContentPack.ReadMissionPools(fname))
            {
                loaded.Add(pool.Playfield, pool);
            }

            Pools = loaded;
            return Pools.Count;
        }

        /// <summary>
        /// One pool's room, or null when the pool or the index is not known.
        /// </summary>
        /// <param name="playfield">The pool's playfield id.</param>
        /// <param name="index">The room's index within the pool.</param>
        /// <returns>The room, or null.</returns>
        public static MissionPoolRoom Room(int playfield, int index)
        {
            MissionPool pool;
            if (!Pools.TryGetValue(playfield, out pool)) return null;
            if (index < 0 || index >= pool.Rooms.Count) return null;

            // The pack is written in index order and the indexes are dense, but a
            // lookup that trusts that silently returns the wrong room if it ever
            // stops being true.
            MissionPoolRoom room = pool.Rooms[index];
            if (room.Index == index) return room;

            return pool.Rooms.Find(x => x.Index == index);
        }
    }
}
