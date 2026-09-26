// --------------------------------------------------------------------------------------------------------------------
// <copyright file="PoolExtract.cs" company="OmniCell">
//   Copyright © 2026 OmniCell contributors.
// </copyright>
// <summary>
//   Turns the extracted mission room pools into Datafiles/missionpools.ocp.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace OmniCell.Tools.Capture
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Text.Json;
    using System.Text.RegularExpressions;

    using OmniCell.Core.Content;
    using OmniCell.Core.Missions;

    /// <summary>
    /// Builds the mission pool pack from a nav export.
    /// </summary>
    /// <remarks>
    /// Every mission is generated from one of ten pool playfields, and the
    /// zone-in packet places rooms by index into that pool's room list. To make
    /// a placement a server has to know what each of those rooms is - how large,
    /// and where its floor is. That does not come off the wire; it is in the
    /// client's own resource database, in a tilemap record.
    ///
    /// Decoding that record is not done here. It was done in the AOBuddy10
    /// project, which exports each pool as a rooms.json, and this reads that
    /// export. Pointing this at the client directly would mean a second
    /// implementation of a decode that is already written and already checked
    /// against three walked missions, so it reads the export and the pack is
    /// what the repository carries - the same arrangement as the item and
    /// playfield packs, which are also converted client data rather than a
    /// second reader of it.
    ///
    ///     PoolExtract &lt;nav directory&gt; &lt;output.ocp&gt;
    ///
    /// The nav directory is the one holding a numbered folder per playfield.
    /// Only the ten autocontent pools are taken; the other ninety dungeons in
    /// that export are static playfields and no mission is built from them.
    ///
    /// Two things are checked rather than assumed, and the run fails if either
    /// is false. Every room's tile grid must match the size its rectangle
    /// claims, and every side must be five cells per slot plus one - the extra
    /// row and column being the cells shared with the rooms beyond. Both held
    /// for all 639 rooms when this was written.
    /// </remarks>
    internal static class PoolExtract
    {
        /// <summary>
        /// The ten pools a mission can be generated from, with the playfield id
        /// the zone-in packet carries for each.
        /// </summary>
        /// <remarks>
        /// Named in the AOBuddy10 mission notes and confirmed on three live
        /// missions, whose zone-in packets carried 320, 321 and 346.
        /// </remarks>
        private static readonly int[] Pools =
            { 320, 321, 322, 324, 331, 341, 346, 351, 362, 382 };

        /// <summary>
        /// The words a pool room's name uses to say what it is for.
        /// </summary>
        /// <remarks>
        /// Ordered, because a name can carry more than one and the first match
        /// wins: "Mine_Startroom_Short8_1" is a start room and not a ramp.
        /// </remarks>
        private static readonly KeyValuePair<string, MissionRoomRole>[] RoleWords =
        {
            new KeyValuePair<string, MissionRoomRole>("bossroom", MissionRoomRole.BossRoom),
            new KeyValuePair<string, MissionRoomRole>("startroom", MissionRoomRole.StartRoom),
            new KeyValuePair<string, MissionRoomRole>("elevator", MissionRoomRole.Elevator),
            new KeyValuePair<string, MissionRoomRole>("entrance", MissionRoomRole.Entrance),
            new KeyValuePair<string, MissionRoomRole>("stair", MissionRoomRole.Ramp),
            new KeyValuePair<string, MissionRoomRole>("ramp", MissionRoomRole.Ramp),
        };

        private static int Main(string[] args)
        {
            if (args.Length != 2)
            {
                Console.Error.WriteLine("usage: PoolExtract <nav directory> <output.ocp>");
                Console.Error.WriteLine();
                Console.Error.WriteLine(
                    "  the nav directory holds one numbered folder per playfield, each with a rooms.json");
                return 1;
            }

            string navRoot = args[0];
            string output = args[1];

            // Where the furniture goes is not in the client - it is what a
            // mission server did, measured from recordings and kept beside
            // this file. Without it the pack is still complete, just without
            // anywhere to put a chest.
            string spotsFile = Path.Combine(
                Path.GetDirectoryName(Path.GetFullPath(typeof(PoolExtract).Assembly.Location)) ?? ".",
                "..", "MissionSpots.tsv");
            if (!File.Exists(spotsFile)) spotsFile = "MissionSpots.tsv";
            Dictionary<(int, int), List<MissionFurnitureSpot>> spots = ReadSpots(spotsFile);

            var pools = new List<MissionPool>();
            int totalRooms = 0;
            int totalCells = 0;

            foreach (int playfield in Pools)
            {
                string path = Path.Combine(navRoot, playfield.ToString(CultureInfo.InvariantCulture), "rooms.json");
                if (!File.Exists(path))
                {
                    Console.Error.WriteLine("missing: " + path);
                    return 1;
                }

                MissionPool pool = ReadPool(playfield, path);
                foreach (MissionPoolRoom room in pool.Rooms)
                {
                    List<MissionFurnitureSpot> mine;
                    if (spots.TryGetValue((playfield, room.Index), out mine))
                    {
                        room.Furniture.AddRange(mine);
                    }
                }

                pools.Add(pool);
                totalRooms += pool.Rooms.Count;
                totalCells += pool.Rooms.Sum(r => r.CellsWidth * r.CellsHeight);

                Console.WriteLine(
                    "  {0,-5} {1,-26} {2,3} rooms   {3}",
                    playfield,
                    pool.Name.Length > 26 ? pool.Name.Substring(0, 26) : pool.Name,
                    pool.Rooms.Count,
                    Roles(pool));
            }

            Console.WriteLine();
            Console.WriteLine(
                "{0} furniture spots over {1} rooms (from {2})",
                pools.Sum(p => p.Rooms.Sum(r => r.Furniture.Count)),
                pools.Sum(p => p.Rooms.Count(r => r.Furniture.Count > 0)),
                spots.Count == 0 ? "nothing - MissionSpots.tsv not found" : spotsFile);

            OmniCellContentPack.WriteMissionPools(output, pools);

            Console.WriteLine();
            Console.WriteLine(
                "{0} door sockets, {1} of them on an interior cell rather than the boundary",
                pools.Sum(p => p.Rooms.Sum(r => r.Doors.Count)),
                pools.Sum(p => p.Rooms.Sum(r => r.Doors.Count(
                    s => s.X != 0 && s.Z != 0
                         && s.X != (r.SlotsWidth * 5) - 1 && s.Z != (r.SlotsHeight * 5) - 1))));

            Console.WriteLine();
            Console.WriteLine(
                "{0} pools, {1} rooms, {2} cells -> {3} ({4:N0} bytes)",
                pools.Count,
                totalRooms,
                totalCells,
                output,
                new FileInfo(output).Length);

            // Read it straight back. A pack that cannot be read is worse than no
            // pack, and the failure would otherwise turn up in the engine.
            List<MissionPool> reread = OmniCellContentPack.ReadMissionPools(output);
            Verify(pools, reread);
            Console.WriteLine("round trip: " + reread.Sum(p => p.Rooms.Count) + " rooms read back identical");

            bool ok = CapturedMission(reread, 341, Placed, Sent, "Grey Caves 20260923-201746");
            ok &= CapturedMission(reread, 321, HiTechPlaced, HiTechSent, "HiTech 2224626");
            return ok ? 0 : 1;
        }

        /// <summary>
        /// The 18 rooms of a HiTech mission, instance 2224626, recorded by the
        /// bot on 2026-09-25. Room index, grid x, grid z, rotation.
        /// </summary>
        /// <remarks>
        /// This case is here because the Grey Caves one below could not tell a
        /// clockwise placement rotation from a counter-clockwise one - its rooms
        /// are nearly all square, so both give the same answer. This one does
        /// not: 21 of 21 the right way round and 8 of 21 the wrong way. The
        /// first version of this test used the wrong way and passed.
        /// </remarks>
        private static readonly int[][] HiTechPlaced =
        {
            new[] { 50, 29, 15, 2 }, new[] { 65, 23, 13, 3 }, new[] { 39, 23, 11, 1 },
            new[] { 67, 22, 17, 3 }, new[] { 17, 26, 13, 2 }, new[] { 33, 27, 13, 1 },
            new[] { 9, 23, 16, 3 }, new[] { 37, 23, 14, 3 }, new[] { 1, 25, 14, 3 },
            new[] { 15, 22, 13, 3 }, new[] { 16, 25, 12, 1 }, new[] { 2, 23, 17, 0 },
            new[] { 53, 26, 21, 1 }, new[] { 2, 21, 20, 3 }, new[] { 34, 24, 20, 1 },
            new[] { 16, 22, 19, 3 }, new[] { 9, 28, 19, 1 }, new[] { 37, 26, 12, 0 }
        };

        /// <summary>
        /// The 21 door positions that mission's server sent.
        /// </summary>
        /// <remarks>
        /// Note (241, 176), which is on no ten metre line at all: it comes from
        /// a socket on an interior cell rather than the room's boundary, so a
        /// door is not always at the midpoint of a slot edge. All seventeen in
        /// the Grey Caves case are, which is what made that look like a rule.
        /// </remarks>
        private static readonly int[][] HiTechSent =
        {
            new[] { 220, 95 }, new[] { 230, 105 }, new[] { 230, 165 }, new[] { 235, 120 },
            new[] { 240, 95 }, new[] { 240, 135 }, new[] { 240, 155 }, new[] { 241, 176 },
            new[] { 245, 170 }, new[] { 250, 155 }, new[] { 250, 175 }, new[] { 255, 130 },
            new[] { 260, 155 }, new[] { 265, 150 }, new[] { 265, 170 }, new[] { 270, 85 },
            new[] { 270, 165 }, new[] { 275, 160 }, new[] { 280, 105 }, new[] { 290, 145 },
            new[] { 300, 145 }
        };

        /// <summary>
        /// The 19 rooms a captured mission placed: pool 341 on a 30 by 30 grid,
        /// from 20260923-201746 stream 12. Room index, grid x, grid z, rotation.
        /// </summary>
        private static readonly int[][] Placed =
        {
            new[] { 18, 29, 3, 1 }, new[] { 96, 26, 2, 3 }, new[] { 42, 25, 2, 3 },
            new[] { 34, 27, 5, 0 }, new[] { 36, 24, 4, 1 }, new[] { 33, 23, 3, 1 },
            new[] { 31, 27, 0, 0 }, new[] { 24, 24, 0, 1 }, new[] { 34, 22, 2, 3 },
            new[] { 101, 25, 8, 0 }, new[] { 9, 26, 1, 0 }, new[] { 56, 25, 6, 2 },
            new[] { 7, 23, 4, 2 }, new[] { 56, 23, 0, 3 }, new[] { 59, 21, 2, 3 },
            new[] { 11, 24, 9, 3 }, new[] { 11, 27, 10, 1 }, new[] { 13, 26, 8, 0 },
            new[] { 11, 26, 11, 2 }
        };

        /// <summary>
        /// The 17 door positions that mission's server sent, as whole metres.
        /// </summary>
        private static readonly int[][] Sent =
        {
            new[] { 300, 265 }, new[] { 290, 265 }, new[] { 265, 270 }, new[] { 275, 250 },
            new[] { 270, 255 }, new[] { 255, 270 }, new[] { 260, 265 }, new[] { 270, 275 },
            new[] { 280, 275 }, new[] { 255, 280 }, new[] { 250, 275 }, new[] { 275, 220 },
            new[] { 265, 280 }, new[] { 260, 285 }, new[] { 255, 240 }, new[] { 235, 260 },
            new[] { 240, 255 }
        };

        /// <summary>
        /// Place the pack's door sockets through a real mission's room list and
        /// check they land where that mission's doors actually stood.
        /// </summary>
        /// <remarks>
        /// This is the whole evidence for the socket encoding, so it runs every
        /// time the pack is built rather than sitting in a note. A capture of the
        /// first minutes of a mission does not contain every door - distant ones
        /// stream in later - so the test is that every door that WAS sent is
        /// predicted, not that nothing else is.
        ///
        /// Two buildings are checked because one was not enough. The AOBuddy10
        /// bot records every mission it runs, and putting the pack through all
        /// twenty six of those recordings - 451 doors over six pools - is what
        /// showed the placement rotation had been going the wrong way: 362 of
        /// 451 the old way and 451 of 451 the new one. Both cases here pass
        /// either way except the HiTech one, which is the point of it.
        ///
        /// The grid is 30 slots square and z counts from the far edge, which is
        /// what puts the landing point inside the entrance room.
        /// </remarks>
        private static bool CapturedMission(
            List<MissionPool> pools,
            int playfield,
            int[][] placed,
            int[][] sent,
            string label)
        {
            const int Grid = 30;
            MissionPool pool = pools.Single(p => p.Playfield == playfield);
            var predicted = new HashSet<(int X, int Z)>();

            foreach (int[] p in placed)
            {
                MissionPoolRoom room = pool.Rooms.Single(r => r.Index == p[0]);
                int gx = p[1], gz = p[2], rot = p[3];

                int slotsW = rot % 2 == 0 ? room.SlotsWidth : room.SlotsHeight;
                int slotsH = rot % 2 == 0 ? room.SlotsHeight : room.SlotsWidth;
                int x0 = gx * 10;
                int z0 = (Grid - gz - slotsH) * 10;

                foreach (MissionDoorSocket door in room.Doors)
                {
                    // The cell's centre, pushed half a cell into the wall the
                    // door stands in. Cells are two metres.
                    int px = (door.X * 2) + 1 + (door.Side == MissionDoorSide.East ? 1 : 0)
                             - (door.Side == MissionDoorSide.West ? 1 : 0);
                    int pz = (door.Z * 2) + 1 + (door.Side == MissionDoorSide.South ? 1 : 0)
                             - (door.Side == MissionDoorSide.North ? 1 : 0);

                    int width = room.SlotsWidth * 10;
                    int depth = room.SlotsHeight * 10;
                    for (int turn = 0; turn < rot % 4; turn++)
                    {
                        // A quarter turn anticlockwise. Which way round it goes
                        // was settled by scoring both against 451 doors; see the
                        // remarks on HiTechPlaced.
                        int nx = pz;
                        pz = width - px;
                        px = nx;
                        int swap = width;
                        width = depth;
                        depth = swap;
                    }

                    predicted.Add((x0 + px, z0 + pz));
                }
            }

            var missing = sent.Where(s => !predicted.Contains((s[0], s[1]))).ToList();
            Console.WriteLine(
                "{0}: {1} of {2} sent doors predicted from the pack ({3} sockets placed)",
                label,
                sent.Length - missing.Count,
                sent.Length,
                predicted.Count);

            foreach (int[] s in missing)
            {
                Console.Error.WriteLine("  MISSED ({0},{1})", s[0], s[1]);
            }

            return missing.Count == 0;
        }

        /// <summary>
        /// The measured furniture spots, by pool and room.
        /// </summary>
        /// <remarks>
        /// A plain tab separated file rather than anything cleverer, because it
        /// is the output of reading recordings and wants to stay diffable when
        /// more runs extend it.
        /// </remarks>
        private static Dictionary<(int, int), List<MissionFurnitureSpot>> ReadSpots(string path)
        {
            var spots = new Dictionary<(int, int), List<MissionFurnitureSpot>>();
            if (!File.Exists(path)) return spots;

            foreach (string line in File.ReadLines(path))
            {
                if (line.Length == 0 || line[0] == '#') continue;
                string[] parts = line.Split('\t');
                if (parts.Length < 6) continue;

                var key = (int.Parse(parts[0], CultureInfo.InvariantCulture),
                           int.Parse(parts[1], CultureInfo.InvariantCulture));
                List<MissionFurnitureSpot> list;
                if (!spots.TryGetValue(key, out list))
                {
                    list = new List<MissionFurnitureSpot>();
                    spots[key] = list;
                }

                list.Add(new MissionFurnitureSpot
                         {
                             Kind = parts[2] == "item"
                                        ? MissionFurnitureKind.Item
                                        : MissionFurnitureKind.Chest,
                             X = float.Parse(parts[3], CultureInfo.InvariantCulture),
                             Z = float.Parse(parts[4], CultureInfo.InvariantCulture),
                             Seen = int.Parse(parts[5], CultureInfo.InvariantCulture)
                         });
            }

            return spots;
        }

        private static MissionPool ReadPool(int playfield, string path)
        {
            using JsonDocument document = JsonDocument.Parse(File.ReadAllBytes(path));
            JsonElement root = document.RootElement;

            var pool = new MissionPool
            {
                Playfield = playfield,
                Name = root.TryGetProperty("name", out JsonElement name) ? name.GetString() : string.Empty
            };

            foreach (JsonElement source in root.GetProperty("rooms").EnumerateArray())
            {
                pool.Rooms.Add(ReadRoom(playfield, source));
            }

            return pool;
        }

        private static MissionPoolRoom ReadRoom(int playfield, JsonElement source)
        {
            int index = source.GetProperty("index").GetInt32();
            string name = source.GetProperty("name").GetString() ?? string.Empty;

            int[] rect = source.GetProperty("rect").EnumerateArray().Select(x => x.GetInt32()).ToArray();
            int cellsWidth = rect[2] - rect[0] + 1;
            int cellsHeight = rect[3] - rect[1] + 1;

            string where = "pool " + playfield + " room " + index + " (" + name + ")";
            if ((cellsWidth - 1) % 5 != 0 || (cellsHeight - 1) % 5 != 0)
            {
                throw new InvalidDataException(
                    where + " is " + cellsWidth + " by " + cellsHeight
                    + " cells, which is not five per slot plus one.");
            }

            var room = new MissionPoolRoom
            {
                Index = index,
                Name = name,
                SlotsWidth = (cellsWidth - 1) / 5,
                SlotsHeight = (cellsHeight - 1) / 5,
                Role = Role(name)
            };

            JsonElement tiles = source.GetProperty("tile");
            if (tiles.GetArrayLength() != cellsHeight)
            {
                throw new InvalidDataException(
                    where + " has " + tiles.GetArrayLength() + " tile rows for a rectangle "
                    + cellsHeight + " cells deep.");
            }

            var floor = new byte[room.FloorBytes];
            int z = 0;
            foreach (JsonElement row in tiles.EnumerateArray())
            {
                if (row.GetArrayLength() != cellsWidth)
                {
                    throw new InvalidDataException(
                        where + " row " + z + " has " + row.GetArrayLength() + " tiles for a rectangle "
                        + cellsWidth + " cells wide.");
                }

                int x = 0;
                foreach (JsonElement tile in row.EnumerateArray())
                {
                    // Any tile id at all is a floor. Which one it is says how the
                    // client draws the cell, and a server placing rooms does not
                    // care; zero is the absence of one.
                    if (tile.GetInt32() != 0)
                    {
                        int bit = (z * cellsWidth) + x;
                        floor[bit >> 3] |= (byte)(1 << (bit & 7));
                    }

                    x++;
                }

                z++;
            }

            room.Floor = floor;

            // The record's door pairs. The second of each is
            //     4 * (z * 5W + x) + side
            // over the room's 5W by 5H interior grid, and the first is the room
            // it opens onto or 0xFFFF where the template does not say. The
            // decode is checked rather than trusted: a cell outside the room
            // would mean the encoding is wrong, and that has to stop the run
            // rather than reach the pack.
            JsonElement doors;
            if (source.TryGetProperty("doors", out doors))
            {
                int row = room.SlotsWidth * 5;
                foreach (JsonElement pair in doors.EnumerateArray())
                {
                    int[] values = pair.EnumerateArray().Select(v => v.GetInt32()).ToArray();
                    int adjoining = values[0];
                    int value = values[1];

                    int cell = value >> 2;
                    var socket = new MissionDoorSocket
                    {
                        X = cell % row,
                        Z = cell / row,
                        Side = (MissionDoorSide)(value & 3),
                        AdjoiningRoom = adjoining == 0xFFFF ? -1 : adjoining
                    };

                    if (socket.X >= row || socket.Z >= room.SlotsHeight * 5)
                    {
                        throw new InvalidDataException(
                            where + " door value " + value + " decodes to cell " + socket.X
                            + "," + socket.Z + ", which is outside a room of "
                            + row + " by " + (room.SlotsHeight * 5) + " interior cells.");
                    }

                    room.Doors.Add(socket);
                }
            }

            return room;
        }

        private static MissionRoomRole Role(string name)
        {
            foreach (KeyValuePair<string, MissionRoomRole> word in RoleWords)
            {
                if (name.IndexOf(word.Key, StringComparison.OrdinalIgnoreCase) >= 0) return word.Value;
            }

            return MissionRoomRole.Ordinary;
        }

        private static string Roles(MissionPool pool)
        {
            return string.Join(
                " ",
                pool.Rooms.GroupBy(r => r.Role)
                    .Where(g => g.Key != MissionRoomRole.Ordinary)
                    .OrderBy(g => g.Key)
                    .Select(g => g.Key + "=" + g.Count()));
        }

        private static void Verify(List<MissionPool> written, List<MissionPool> read)
        {
            if (written.Count != read.Count)
            {
                throw new InvalidDataException(
                    "wrote " + written.Count + " pools and read back " + read.Count);
            }

            foreach (MissionPool a in written)
            {
                MissionPool b = read.Single(x => x.Playfield == a.Playfield);
                if (a.Rooms.Count != b.Rooms.Count)
                {
                    throw new InvalidDataException(
                        "pool " + a.Playfield + ": wrote " + a.Rooms.Count + " rooms, read " + b.Rooms.Count);
                }

                for (int i = 0; i < a.Rooms.Count; i++)
                {
                    MissionPoolRoom x = a.Rooms[i];
                    MissionPoolRoom y = b.Rooms[i];
                    if (x.Index != y.Index || x.Name != y.Name || x.SlotsWidth != y.SlotsWidth
                        || x.SlotsHeight != y.SlotsHeight || x.Role != y.Role
                        || !x.Floor.SequenceEqual(y.Floor)
                        || x.Furniture.Count != y.Furniture.Count
                        || x.Furniture.Where((s, n) => s.Kind != y.Furniture[n].Kind
                                                       || s.X != y.Furniture[n].X
                                                       || s.Z != y.Furniture[n].Z
                                                       || s.Seen != y.Furniture[n].Seen).Any()
                        || x.Doors.Count != y.Doors.Count
                        || x.Doors.Where((s, n) => s.X != y.Doors[n].X || s.Z != y.Doors[n].Z
                                                   || s.Side != y.Doors[n].Side
                                                   || s.AdjoiningRoom != y.Doors[n].AdjoiningRoom).Any())
                    {
                        throw new InvalidDataException(
                            "pool " + a.Playfield + " room " + x.Index + " did not survive the round trip.");
                    }
                }
            }
        }
    }
}
