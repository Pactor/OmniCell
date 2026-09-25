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

            OmniCellContentPack.WriteMissionPools(output, pools);

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

            return 0;
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
                        || !x.Floor.SequenceEqual(y.Floor))
                    {
                        throw new InvalidDataException(
                            "pool " + a.Playfield + " room " + x.Index + " did not survive the round trip.");
                    }
                }
            }
        }
    }
}
