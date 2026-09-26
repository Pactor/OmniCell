namespace OmniCell.Core.Missions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using SmokeLounge.AOtomation.Messaging.GameData;

    /// <summary>
    /// Builds a mission out of a room pool, the way retail appears to.
    /// </summary>
    /// <remarks>
    /// Reconstructed from 276 captured buildings rather than designed. What
    /// those say, in short: the grid is always thirty slots square and a floor
    /// is always sixty four units tall; a building is 7 to 42 rooms averaging
    /// 17.5 and occupies about eight slots by nine of the thirty; and it is one
    /// floor for a solo mission and three or four contiguous floors for a team
    /// one, never two.
    ///
    /// The algorithm is the part the sockets give away. Across all 276, 93% of
    /// socket positions have exactly two rooms meeting there and 243 buildings
    /// have exactly one socket left over - the front door. Every one of the
    /// 4,797 paired sockets has the two rooms facing each other. So a building
    /// is grown, not scattered: take a socket that is still open, find a room
    /// and a rotation whose own socket lands on that spot facing back, place it
    /// if it fits, and strike both sockets off. What is left open at the end
    /// gets capped with a room that has only one socket - which is why 66% of
    /// all placements in those buildings are such rooms, though they are only
    /// 70 of the 434 rooms in the pools.
    ///
    /// The one thing the captures do not say is how retail picks which room for
    /// a socket. This weights the choice by how often each room was placed,
    /// which reproduces the look without claiming to be their rule.
    ///
    /// Geometry is the same throughout as everywhere else in this project: a
    /// slot is ten metres, a cell is two, a room spans five cells per slot plus
    /// the shared one, x counts from zero and z from the far edge, and a
    /// quarter turn anticlockwise sends (x, z) in a w by d box to (z, w - x) in
    /// a d by w box and a side to side + 1. That transform is not a guess - it
    /// puts the pools' sockets on all 561 doors of the recorded missions.
    /// </remarks>
    public class MissionBuilder
    {
        private const int SlotMetres = 10;
        private const int CellMetres = 2;

        private readonly MissionPool pool;
        private readonly Random random;

        /// <summary>
        /// Every floored cell the building has taken, and the floor that took
        /// it. Keyed without the floor: the floors compete for one footprint.
        /// </summary>
        private readonly Dictionary<long, int> claimed = new Dictionary<long, int>();
        private readonly List<Open> open = new List<Open>();
        private readonly Dictionary<long, List<int>> meeting = new Dictionary<long, List<int>>();

        private MissionLayout layout;

        public MissionBuilder(MissionPool pool, int seed)
        {
            if (pool == null) throw new ArgumentNullException("pool");
            this.pool = pool;
            this.random = new Random(seed);
        }

        /// <summary>
        /// How many rooms a building should be, drawn the way the captures are
        /// distributed - 7 to 42, humped between 11 and 19.
        /// </summary>
        /// <remarks>
        /// The mean of the 276 is 17.5. Three draws averaged gives a hump of
        /// about the right width without carrying a histogram around.
        /// </remarks>
        public int RollRoomCount()
        {
            int roll = this.random.Next(Counts[Counts.Length - 1]);
            for (int i = 0; i < Counts.Length; i++)
            {
                if (roll < Counts[i]) return i + 7;
            }

            return 42;
        }

        /// <summary>
        /// The room counts of the 276 captured buildings, as a running total
        /// from seven upward, so a draw against it has their distribution
        /// rather than a shape invented to look like it.
        /// </summary>
        private static readonly int[] Counts = Running(
            1, 3, 8, 10, 19, 17, 22, 20, 14, 23, 16, 23, 19, 15, 12, 9, 7, 4, 7, 5,
            1, 3, 0, 3, 2, 1, 5, 2, 0, 2, 0, 0, 0, 1, 0, 2);

        private static int[] Running(params int[] histogram)
        {
            var running = new int[histogram.Length];
            int total = 0;
            for (int i = 0; i < histogram.Length; i++)
            {
                total += histogram[i];
                running[i] = total;
            }

            return running;
        }

        /// <summary>
        /// Build one.
        /// </summary>
        /// <param name="roomCount">How many rooms to aim for.</param>
        /// <param name="floors">
        /// 1 for a solo mission. A team mission is 3 or 4 contiguous floors;
        /// never 2, which no captured building has.
        /// </param>
        /// <returns>The layout, or null when the pool cannot start one.</returns>
        public MissionLayout Build(int roomCount, int floors)
        {
            if (roomCount < 1) throw new ArgumentOutOfRangeException("roomCount");
            if (floors == 2) throw new ArgumentOutOfRangeException(
                "floors", "no captured building has two floors: a mission is one, three or four");

            this.claimed.Clear();
            this.open.Clear();
            this.meeting.Clear();

            this.layout = new MissionLayout
                          {
                              Playfield = this.pool.Playfield,
                              GridWidth = 30,
                              GridHeight = 30,
                              WorldHeight = 64
                          };

            if (floors <= 1)
            {
                if (!this.Floor(0, roomCount, true, true)) return null;
                this.EmitDoors();
                return this.layout;
            }

            // A team building. The floors run away from zero in one direction
            // and the far one holds the boss room by itself.
            int direction = this.random.Next(16) < 9 ? 1 : -1;
            int walked = floors - 1;

            // The boss room goes down first. Its place is fixed - the middle
            // of the grid - and the floors share one footprint, so a floor
            // allowed to grow over the middle first would leave it nowhere to
            // stand.
            if (!this.PlaceBoss(direction * walked)) return null;

            for (int step = 0; step < walked; step++)
            {
                int floor = direction * step;

                // 37 floors over the sixteen captured buildings run 4 to 16
                // rooms, mean 9.9.
                int want = 4 + this.random.Next(13);

                // Not from the middle: that is the boss room's, and no
                // captured team building puts a floor there.
                if (!this.Floor(floor, want, step == 0, false) && step == 0) return null;
            }

            this.EmitDoors();
            return this.layout;
        }

        /// <summary>
        /// One floor, grown and closed.
        /// </summary>
        /// <param name="floor">Which floor.</param>
        /// <param name="roomCount">How many rooms to aim for.</param>
        /// <param name="wayIn">
        /// Whether this floor carries the way into the building. Only floor
        /// zero does: every captured building has exactly one socket nobody
        /// meets and it is the entrance.
        /// </param>
        /// <param name="centre">
        /// Whether to start from the middle of the grid, which only a building
        /// of one floor does.
        /// </param>
        private bool Floor(int floor, int roomCount, bool wayIn, bool centre)
        {
            int before = this.layout.Rooms.Count;
            if (!this.PlaceFirst(floor, wayIn, centre)) return false;

            // Grow. A socket is taken at random rather than in order, which is
            // what keeps a building from turning into a corridor.
            //
            // The target counts the caps: every socket still open at the end
            // becomes a room of its own, so growing all the way to the target
            // and then capping overshoots it by half again.
            while (this.layout.Rooms.Count - before + this.open.Count < roomCount
                   && this.open.Count > 0)
            {
                int pick = this.random.Next(this.open.Count);
                Open socket = this.open[pick];
                this.open.RemoveAt(pick);

                if (!this.Attach(socket, false))
                {
                    // Nothing fits; it has to be capped instead, and if even
                    // that fails the socket becomes a second way out.
                    if (!this.Attach(socket, true)) this.Leave(socket);
                }
            }

            // Everything still open gets a dead end on it. Where no dead end
            // fits - the socket faces the wrong way for every one the pool has,
            // or what would fit collides - anything at all is better than a
            // hole, even though it opens sockets of its own; hence the loop,
            // and the bound on it so a pool that cannot close can still finish.
            for (int guard = 0; this.open.Count > 0 && guard < roomCount * 8; guard++)
            {
                Open socket = this.open[this.open.Count - 1];
                this.open.RemoveAt(this.open.Count - 1);
                if (this.Attach(socket, true)) continue;
                if (!this.Attach(socket, false)) this.Leave(socket);
            }

            return this.layout.Rooms.Count > before;
        }

        /// <summary>
        /// The one room on a team building's far floor.
        /// </summary>
        /// <remarks>
        /// All sixteen multi-floor buildings in the corpus end the same way:
        /// the floor furthest from zero holds exactly one room, it is a boss
        /// room, and it stands at grid 13, 13 - the middle of the thirty by
        /// thirty. Nine of the sixteen count upwards from zero and seven
        /// downwards, and none of them mixes the two.
        /// </remarks>
        private bool PlaceBoss(int floor)
        {
            List<MissionPoolRoom> bosses = this.pool.Rooms
                .Where(r => r.Role == MissionRoomRole.BossRoom).ToList();
            if (bosses.Count == 0) return false;

            // Room by room until one fits, because the middle may be taken by
            // a floor that grew into it.
            bosses = bosses.OrderBy(x => this.random.Next()).ToList();

            foreach (MissionPoolRoom room in bosses)
            {
                for (int rot = 0; rot < 4; rot++)
                {
                    if (!this.Put(room, floor, BossGridX, BossGridZ, rot)) continue;

                    // Nothing hangs off it: it is the whole floor.
                    this.open.Clear();
                    return true;
                }
            }

            return false;
        }

        #region growing

        /// <summary>
        /// Where the single room on a team building's far floor stands, in
        /// every one of the sixteen captured multi-floor buildings.
        /// </summary>
        private const int BossGridX = 13;

        /// <summary>
        /// Where the single room on a team building's far floor stands, in
        /// every one of the sixteen captured multi-floor buildings.
        /// </summary>
        private const int BossGridZ = 13;

        private bool PlaceFirst(int floor, bool wayIn, bool centre)
        {
            // An entrance if the pool names one, else anything with a socket.
            // Two sockets at least: one becomes the way in and the other has
            // to carry the rest of the building.
            List<MissionPoolRoom> starts = this.pool.Rooms
                .Where(r => r.Role == MissionRoomRole.Entrance && r.Doors.Count > 1).ToList();
            if (starts.Count == 0)
            {
                starts = this.pool.Rooms.Where(r => r.Doors.Count > 1).ToList();
            }

            if (starts.Count == 0) return false;

            MissionPoolRoom room = starts[this.random.Next(starts.Count)];
            int rot = this.random.Next(4);
            int w = rot % 2 == 0 ? room.SlotsWidth : room.SlotsHeight;
            int h = rot % 2 == 0 ? room.SlotsHeight : room.SlotsWidth;

            // Near the middle for a building of one floor, so it can grow in
            // every direction. A floor of a team building has to go somewhere
            // no other floor has been, because no two floors of any captured
            // building share a grid cell - so it is tried in the open until it
            // lands, and the collision test does the keeping apart.
            int gx = (this.layout.GridWidth - w) / 2;
            int gz = (this.layout.GridHeight - h) / 2;
            if (centre)
            {
                if (!this.Put(room, floor, gx, gz, rot)) return false;
            }
            else
            {
                bool placed = false;
                for (int attempt = 0; attempt < 200 && !placed; attempt++)
                {
                    rot = this.random.Next(4);
                    w = rot % 2 == 0 ? room.SlotsWidth : room.SlotsHeight;
                    h = rot % 2 == 0 ? room.SlotsHeight : room.SlotsWidth;
                    gx = this.random.Next(this.layout.GridWidth - w + 1);
                    gz = this.random.Next(this.layout.GridHeight - h + 1);
                    placed = this.Put(room, floor, gx, gz, rot);
                }

                if (!placed) return false;
            }

            // One socket is the way in and nothing may be hung on it. Without
            // this a quarter of the buildings came out sealed, and every
            // captured one has at least one socket nobody meets - the door
            // whose Room is -1. Only the floor with the way in gives one up;
            // the others are closed all round and reached by lift.
            if (wayIn && this.open.Count > 0)
            {
                this.open.RemoveAt(this.random.Next(this.open.Count));
            }

            return true;
        }

        /// <summary>
        /// Try to hang a room on an open socket.
        /// </summary>
        /// <param name="socket">The socket to fill.</param>
        /// <param name="capOnly">Only rooms with a single socket - a dead end.</param>
        private bool Attach(Open socket, bool capOnly)
        {
            int want = Opposite(socket.Side);

            foreach (MissionPoolRoom room in this.Candidates(capOnly))
            {
                for (int rot = 0; rot < 4; rot++)
                {
                    for (int s = 0; s < room.Doors.Count; s++)
                    {
                        Placed p = Project(room, room.Doors[s], rot);
                        if ((int)p.Side != want) continue;

                        int x0 = socket.X - p.X;
                        int z0 = socket.Z - p.Z;
                        if (x0 % SlotMetres != 0 || z0 % SlotMetres != 0) continue;

                        int w = rot % 2 == 0 ? room.SlotsWidth : room.SlotsHeight;
                        int h = rot % 2 == 0 ? room.SlotsHeight : room.SlotsWidth;
                        int gx = x0 / SlotMetres;
                        int gz = this.layout.GridHeight - h - (z0 / SlotMetres);

                        if (gx < 0 || gz < 0) continue;
                        if (gx + w > this.layout.GridWidth) continue;
                        if (gz + h > this.layout.GridHeight) continue;

                        if (this.Put(room, socket.Floor, gx, gz, rot, socket)) return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// The rooms to try, in a shuffled order weighted toward the ones retail
        /// places most.
        /// </summary>
        /// <remarks>
        /// Weighting by socket count is the closest stand-in the pack supports:
        /// a single-socket room is placed 47 times on average in the captures
        /// against about 5 for everything else, and a dead end is a
        /// single-socket room by definition.
        /// </remarks>
        private IEnumerable<MissionPoolRoom> Candidates(bool capOnly)
        {
            // Growing and capping want opposite things, and the arithmetic
            // says how opposite. A building of R rooms hanging off one entrance
            // has R - 1 doors between rooms and one to the outside, so its
            // rooms carry 2R - 1 sockets between them: two apiece on average.
            // Two thirds of retail's placements are single-socket rooms, so the
            // rest must average about four. Hence: while growing, only rooms
            // that carry the frontier forward; at the end, only rooms that
            // close it.
            IEnumerable<MissionPoolRoom> source = this.pool.Rooms.Where(
                r => capOnly ? r.Doors.Count == 1 : r.Doors.Count >= 2);

            if (capOnly)
            {
                return source.OrderBy(r => this.random.Next()).ToList();
            }

            // Size matters as much as socket count. 73.6% of retail's 4,863
            // placements are one slot square and the mean is 3.07 slots, which
            // is what makes their buildings about seven slots across; choosing
            // uniformly made ours half again as wide.
            //
            // And when the frontier is down to a single socket, a room that
            // closes more than it opens ends the building there - which had a
            // third of them finishing at three rooms. Three sockets is the
            // right amount of widening; reaching for the ten socket halls
            // instead leaves a frontier the capping pass cannot close.
            bool widen = this.open.Count < 2;
            return source
                .OrderBy(r => (r.SlotsWidth * r.SlotsHeight)
                              + (widen ? 2 * Math.Abs(r.Doors.Count - 3) : r.Doors.Count)
                              + this.random.Next(4))
                .ToList();
        }

        private bool Put(MissionPoolRoom room, int floor, int gx, int gz, int rot, Open filled = null)
        {
            var cells = new List<long>();
            int originX = gx * SlotMetres;
            int originZ = (this.layout.GridHeight - gz
                           - (rot % 2 == 0 ? room.SlotsHeight : room.SlotsWidth)) * SlotMetres;

            for (int cz = 0; cz < room.CellsHeight; cz++)
            {
                for (int cx = 0; cx < room.CellsWidth; cx++)
                {
                    if (!room.HasFloor(cx, cz)) continue;

                    // The cell's middle, turned with the room and put in place.
                    int px = (cx * CellMetres) + 1;
                    int pz = (cz * CellMetres) + 1;
                    Turn(room, rot, ref px, ref pz);

                    int wx = (originX + px) / CellMetres;
                    int wz = (originZ + pz) / CellMetres;
                    long key = FloorlessKey(wx, wz);

                    // Rooms do share cells - 3.5% of them in the captures, at
                    // the seams where they meet - but only round their edges,
                    // and only with a room on their own floor. Two floors of a
                    // captured building never share a cell at all.
                    bool edge = cx == 0 || cz == 0
                                || cx == room.CellsWidth - 1 || cz == room.CellsHeight - 1;
                    int owner;
                    if (this.claimed.TryGetValue(key, out owner) && (!edge || owner != floor))
                    {
                        return false;
                    }

                    cells.Add(key);
                }
            }

            int slot = this.layout.Rooms.Count;
            foreach (long key in cells)
            {
                this.claimed[key] = floor;
            }

            this.layout.Rooms.Add(new BuildingRoomInfo
                                  {
                                      Room = (short)room.Index,
                                      Floor = (sbyte)floor,
                                      X = (byte)gx,
                                      Z = (byte)gz,
                                      Rotation = (byte)rot
                                  });

            // Every socket this room brings, minus the one it was hung on.
            for (int s = 0; s < room.Doors.Count; s++)
            {
                Placed p = Project(room, room.Doors[s], rot);
                int x = originX + p.X;
                int z = originZ + p.Z;

                long key = Key(floor, x, z);
                List<int> users;
                if (!this.meeting.TryGetValue(key, out users))
                {
                    users = new List<int>();
                    this.meeting[key] = users;
                }

                users.Add(slot);

                if (filled != null && x == filled.X && z == filled.Z && floor == filled.Floor)
                {
                    continue;
                }

                this.open.Add(new Open { Room = slot, X = x, Z = z, Floor = floor, Side = p.Side });
            }

            return true;
        }

        /// <summary>
        /// A socket nothing could be hung on. It stays a door to nowhere, which
        /// is what the entrance is.
        /// </summary>
        private void Leave(Open socket)
        {
        }

        private void EmitDoors()
        {
            foreach (KeyValuePair<long, List<int>> kv in this.meeting)
            {
                List<int> users = kv.Value;
                int x, z, floor;
                Unkey(kv.Key, out floor, out x, out z);

                this.layout.Doors.Add(new MissionLayoutDoor
                                      {
                                          X = x,
                                          Z = z,
                                          Floor = floor,
                                          Room = users.Count > 1 ? users[1] : -1,
                                          AdjoiningRoom = users[0]
                                      });
            }
        }

        #endregion

        #region geometry

        private struct Placed
        {
            public int X;
            public int Z;
            public MissionDoorSide Side;
        }

        private sealed class Open
        {
            public int Room;
            public int X;
            public int Z;
            public int Floor;
            public MissionDoorSide Side;
        }

        /// <summary>
        /// Where a socket ends up in the room's own frame once the room is
        /// turned, and which way it then faces.
        /// </summary>
        private static Placed Project(MissionPoolRoom room, MissionDoorSocket door, int rot)
        {
            int px = (door.X * CellMetres) + 1
                     + (door.Side == MissionDoorSide.East ? 1 : 0)
                     - (door.Side == MissionDoorSide.West ? 1 : 0);
            int pz = (door.Z * CellMetres) + 1
                     + (door.Side == MissionDoorSide.South ? 1 : 0)
                     - (door.Side == MissionDoorSide.North ? 1 : 0);

            int side = (int)door.Side;
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
                side = (side + 1) % 4;
            }

            return new Placed { X = px, Z = pz, Side = (MissionDoorSide)side };
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

        private static int Opposite(MissionDoorSide side)
        {
            return ((int)side + 2) % 4;
        }

        /// <summary>
        /// A cell, for the collision map and the socket map.
        /// </summary>
        /// <remarks>
        /// The floor is in the key for sockets - two floors meeting at the
        /// same coordinate are not a door - but the floors of a captured team
        /// building never share a grid cell at all, over all sixteen of them
        /// and every adjacent pair. Sixteen buildings of eight by nine slots
        /// in a thirty by thirty grid do not miss each other forty times by
        /// luck, so it is a rule, and <see cref="FloorlessKey"/> is what makes
        /// the placement obey it.
        /// </remarks>
        private static long Key(int floor, int x, int z)
        {
            return ((long)(floor + 8) << 40) | ((long)(x + 4096) << 20) | (uint)(z + 4096);
        }

        /// <summary>
        /// The same cell on any floor, which is what the floors compete for.
        /// </summary>
        private static long FloorlessKey(int x, int z)
        {
            return Key(0, x, z);
        }

        private static void Unkey(long key, out int floor, out int x, out int z)
        {
            floor = (int)(key >> 40) - 8;
            x = (int)((key >> 20) & 0xFFFFF) - 4096;
            z = (int)(key & 0xFFFFF) - 4096;
        }

        #endregion
    }
}
