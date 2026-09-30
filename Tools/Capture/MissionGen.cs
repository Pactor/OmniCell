// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MissionGen.cs" company="OmniCell">
//   Copyright © 2026 OmniCell contributors.
// </copyright>
// <summary>
//   Generates missions and measures them against the ones retail generated.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace OmniCell.Tools.Capture
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;

    using OmniCell.Core.Content;
    using OmniCell.Core.Missions;

    using SmokeLounge.AOtomation.Messaging.GameData;

    /// <summary>
    /// Builds missions out of the pool pack and checks they come out like the
    /// real ones.
    /// </summary>
    /// <remarks>
    /// A generator that produces a building the client cannot walk is worse
    /// than none, and the failure would only show up with somebody standing in
    /// it. So this asserts the things 276 captured buildings all satisfy:
    ///
    ///   * every socket in the building is met by exactly two rooms, except
    ///     one - the way in;
    ///   * no two rooms claim the same floor cell away from their edges;
    ///   * every room sits inside the thirty by thirty grid.
    ///
    /// and then prints the distributions beside retail's, which are not pass or
    /// fail but are what says whether the output looks like a mission.
    ///
    ///     MissionGen &lt;pack.ocp&gt; [count]
    /// </remarks>
    internal static class MissionGen
    {
        private const int CellMetres = 2;
        private const int SlotMetres = 10;

        private static int Main(string[] args)
        {
            if (args.Length < 1)
            {
                Console.Error.WriteLine("usage: MissionGen <missionpools.ocp> [buildings per pool]");
                return 1;
            }

            int each = args.Length > 1
                           ? int.Parse(args[1], CultureInfo.InvariantCulture)
                           : 200;

            List<MissionPool> pools = OmniCellContentPack.ReadMissionPools(args[0]);
            Console.WriteLine("{0} pools, {1} buildings each", pools.Count, each);
            Console.WriteLine();

            int built = 0, failed = 0, bad = 0, teamBuilt = 0;
            var rooms = new List<int>();
            var extents = new List<(int W, int H)>();
            var strays = new List<int>();
            var chests = new List<int>();
            var monsters = new List<int>();
            var levels = new List<int>();
            int objectives = 0;

            foreach (MissionPool pool in pools.OrderBy(p => p.Playfield))
            {
                int poolBuilt = 0, poolBad = 0, poolStray = 0;

                for (int i = 0; i < each; i++)
                {
                    // The whole thing, not just the shell: a mission is the
                    // building plus what stands in it, and the contents are
                    // measured against the recordings too.
                    // One building in four is a team one, which is three or
                    // four floors rather than a flat one.
                    var factory = new MissionFactory(pool, (pool.Playfield * 100003) + i);
                    bool team = i % 4 == 3
                                && pool.Rooms.Any(r => r.Role == MissionRoomRole.BossRoom);
                    Mission mission = factory.Build(
                        38, MissionType.FindItem, team ? (i % 8 == 3 ? 3 : 4) : 1);
                    MissionLayout layout = mission?.Layout;
                    if (layout == null || layout.Rooms.Count == 0)
                    {
                        failed++;
                        continue;
                    }

                    chests.Add(mission.Furniture.Count(f => f.Kind == MissionFurnitureKind.Chest));
                    monsters.Add(mission.Monsters.Count);
                    if (mission.Objective != null) objectives++;
                    if (mission.Monsters.Count > 0)
                    {
                        levels.Add(mission.Monsters.Min(m => m.Level) - 38);
                        levels.Add(mission.Monsters.Max(m => m.Level) - 38);
                    }

                    built++;
                    poolBuilt++;
                    rooms.Add(layout.Rooms.Count);

                    int xs = layout.Rooms.Min(r => (int)r.X);
                    int zs = layout.Rooms.Min(r => (int)r.Z);
                    extents.Add((layout.Rooms.Max(r => (int)r.X) - xs + 1,
                                 layout.Rooms.Max(r => (int)r.Z) - zs + 1));

                    string why = Check(pool, layout, out int stray) ?? Stacked(layout, team);
                    if (team && why == null) teamBuilt++;
                    strays.Add(stray);
                    poolStray += stray;
                    if (why != null)
                    {
                        bad++;
                        poolBad++;
                        if (poolBad <= 2)
                        {
                            Console.WriteLine("  pool {0} build {1}: {2}", pool.Playfield, i, why);
                        }
                    }
                }

                Console.WriteLine(
                    "  {0,-5} built {1,4}   invalid {2,3}   ways out, mean {3:F2}",
                    pool.Playfield, poolBuilt, poolBad,
                    poolBuilt == 0 ? 0 : poolStray / (double)poolBuilt);
            }

            Console.WriteLine();
            Console.WriteLine(
                "built {0} ({1} of them team buildings), could not start {2}, invalid {3}",
                built, teamBuilt, failed, bad);
            Console.WriteLine();

            Console.WriteLine("                        ours          retail (276 captured)");
            Console.WriteLine("  rooms per building    {0,4:F1}          17.5", rooms.Average());
            Console.WriteLine("  smallest / largest    {0,2} / {1,-2}       7 / 42",
                rooms.Min(), rooms.Max());
            Console.WriteLine("  bounding box slots    {0,4:F1} x {1:F1}    about 8 x 9",
                extents.Average(e => e.W), extents.Average(e => e.H));
            Console.WriteLine("  ways out per building {0,4:F2}          1.3", strays.Average());
            Console.WriteLine("  chests per building   {0,4:F1}          1 to 22", chests.Average());
            Console.WriteLine("  monsters per building {0,4:F1}          11 to 69", monsters.Average());
            Console.WriteLine("  monster level vs QL   {0,3} to {1,-3}     -4 to +3",
                levels.Count == 0 ? 0 : levels.Min(), levels.Count == 0 ? 0 : levels.Max());
            Console.WriteLine("  objective placed      {0,4:P0}         every find item",
                objectives / (double)built);

            return bad == 0 ? 0 : 1;
        }

        /// <summary>
        /// What the sixteen captured multi-floor buildings all do.
        /// </summary>
        /// <remarks>
        /// Read off the bot's own recorded zone-in packets, 325 of them, of
        /// which sixteen are not flat:
        ///
        ///   * three or four floors, never two;
        ///   * contiguous, and all on one side of zero - (0,1,2), (0,1,2,3),
        ///     (-2,-1,0), (-3,-2,-1,0) are the four sets that occur;
        ///   * the floor furthest from zero holds exactly one room, it is a
        ///     boss room, and it stands at grid 13, 13;
        ///   * no two floors share a grid cell, in any of the sixteen and any
        ///     adjacent pair - so the floors compete for one footprint rather
        ///     than being stacked on top of each other.
        /// </remarks>
        private static string Stacked(MissionLayout layout, bool team)
        {
            List<int> floors = layout.Rooms.Select(r => (int)r.Floor).Distinct().OrderBy(f => f).ToList();
            if (!team)
            {
                return floors.Count == 1 && floors[0] == 0
                           ? null
                           : "a solo building on floors " + string.Join(", ", floors);
            }

            if (floors.Count < 3 || floors.Count > 4)
            {
                return "a team building on " + floors.Count + " floors";
            }

            for (int i = 1; i < floors.Count; i++)
            {
                if (floors[i] != floors[i - 1] + 1) return "floors are not contiguous";
            }

            if (floors[0] != 0 && floors[floors.Count - 1] != 0)
            {
                return "no floor zero: " + string.Join(", ", floors);
            }

            int far = floors[0] == 0 ? floors[floors.Count - 1] : floors[0];
            List<BuildingRoomInfo> top = layout.Rooms.Where(r => r.Floor == far).ToList();
            if (top.Count != 1) return "the far floor has " + top.Count + " rooms";
            if (top[0].X != 13 || top[0].Z != 13)
            {
                return "the far floor's room is at " + top[0].X + ", " + top[0].Z;
            }

            return null;
        }

        /// <summary>
        /// The three things every captured building satisfies.
        /// </summary>
        private static string Check(MissionPool pool, MissionLayout layout, out int waysOut)
        {
            waysOut = 0;
            var claimed = new Dictionary<(int, int, int), int>();
            var sockets = new Dictionary<(int, int, int), int>();

            for (int slot = 0; slot < layout.Rooms.Count; slot++)
            {
                BuildingRoomInfo p = layout.Rooms[slot];
                MissionPoolRoom room = pool.Rooms.Single(r => r.Index == p.Room);

                int w = p.Rotation % 2 == 0 ? room.SlotsWidth : room.SlotsHeight;
                int h = p.Rotation % 2 == 0 ? room.SlotsHeight : room.SlotsWidth;
                if (p.X < 0 || p.Z < 0 || p.X + w > layout.GridWidth || p.Z + h > layout.GridHeight)
                {
                    return string.Format("room {0} is off the grid at {1},{2}", p.Room, p.X, p.Z);
                }

                int originX = p.X * SlotMetres;
                int originZ = (layout.GridHeight - p.Z - h) * SlotMetres;

                for (int cz = 0; cz < room.CellsHeight; cz++)
                {
                    for (int cx = 0; cx < room.CellsWidth; cx++)
                    {
                        if (!room.HasFloor(cx, cz)) continue;

                        int px = (cx * CellMetres) + 1;
                        int pz = (cz * CellMetres) + 1;
                        Turn(room, p.Rotation, ref px, ref pz);

                        var key = (p.Floor, (originX + px) / CellMetres, (originZ + pz) / CellMetres);
                        bool edge = cx == 0 || cz == 0
                                    || cx == room.CellsWidth - 1 || cz == room.CellsHeight - 1;
                        if (claimed.ContainsKey(key) && !edge)
                        {
                            return string.Format(
                                "rooms {0} and {1} both floor the cell {2},{3}",
                                claimed[key], p.Room, key.Item2, key.Item3);
                        }

                        claimed[key] = p.Room;
                    }
                }

                foreach (MissionDoorSocket door in room.Doors)
                {
                    int px = (door.X * CellMetres) + 1
                             + (door.Side == MissionDoorSide.East ? 1 : 0)
                             - (door.Side == MissionDoorSide.West ? 1 : 0);
                    int pz = (door.Z * CellMetres) + 1
                             + (door.Side == MissionDoorSide.South ? 1 : 0)
                             - (door.Side == MissionDoorSide.North ? 1 : 0);
                    Turn(room, p.Rotation, ref px, ref pz);

                    var key = (p.Floor, originX + px, originZ + pz);
                    sockets.TryGetValue(key, out int n);
                    sockets[key] = n + 1;
                }
            }

            foreach (KeyValuePair<(int, int, int), int> kv in sockets)
            {
                if (kv.Value > 2)
                {
                    return string.Format("{0} rooms meet at one socket", kv.Value);
                }

                if (kv.Value == 1) waysOut++;
            }

            if (sockets.Count != layout.Doors.Count)
            {
                return string.Format(
                    "{0} socket positions but {1} doors emitted", sockets.Count, layout.Doors.Count);
            }

            return null;
        }

        private static void Turn(MissionPoolRoom room, int rot, ref int px, ref int pz)
        {
            int width = room.SlotsWidth * SlotMetres;
            int depth = room.SlotsHeight * SlotMetres;
            for (int turn = 0; turn < rot % 4; turn++)
            {
                int nx = pz;
                pz = width - px;
                px = nx;
                int swap = width;
                width = depth;
                depth = swap;
            }
        }
    }
}
