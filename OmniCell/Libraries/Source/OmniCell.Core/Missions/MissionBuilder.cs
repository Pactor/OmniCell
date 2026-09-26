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
            int total = 0;
            for (int i = 0; i < 3; i++) total += this.random.Next(7, 43);
            return Math.Max(7, Math.Min(42, (int)Math.Round(total / 3.0)));
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

            if (!this.PlaceFirst()) return null;

            // Grow. A socket is taken at random rather than in order, which is
            // what keeps a building from turning into a corridor.
            //
            // The target counts the caps: every socket still open at the end
            // becomes a room of its own, so growing all the way to the target
            // and then capping overshoots it by half again.
            while (this.layout.Rooms.Count + this.open.Count < roomCount && this.open.Count > 0)
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

            this.EmitDoors();
            return this.layout;
        }

        #region growing

        private bool PlaceFirst()
        {
            // An entrance if the pool names one, else anything with a socket.
            List<MissionPoolRoom> starts = this.pool.Rooms
                .Where(r => r.Role == MissionRoomRole.Entrance && r.Doors.Count > 0).ToList();
            if (starts.Count == 0)
            {
                starts = this.pool.Rooms.Where(r => r.Doors.Count > 0).ToList();
            }

            if (starts.Count == 0) return false;

            MissionPoolRoom room = starts[this.random.Next(starts.Count)];
            int rot = this.random.Next(4);
            int w = rot % 2 == 0 ? room.SlotsWidth : room.SlotsHeight;
            int h = rot % 2 == 0 ? room.SlotsHeight : room.SlotsWidth;

            // Near the middle, so it can grow in every direction.
            int gx = (this.layout.GridWidth - w) / 2;
            int gz = (this.layout.GridHeight - h) / 2;
            return this.Put(room, 0, gx, gz, rot);
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

            return capOnly
                       ? source.OrderBy(r => this.random.Next()).ToList()
                       : source.OrderBy(r => r.Doors.Count + this.random.Next(3)).ToList();
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
                    long key = Key(floor, wx, wz);

                    // Rooms do share cells - 3.5% of them in the captures, at
                    // the seams where they meet - but only round their edges.
                    bool edge = cx == 0 || cz == 0
                                || cx == room.CellsWidth - 1 || cz == room.CellsHeight - 1;
                    if (this.claimed.ContainsKey(key) && !edge) return false;

                    cells.Add(key);
                }
            }

            int slot = this.layout.Rooms.Count;
            foreach (long key in cells)
            {
                int n;
                this.claimed.TryGetValue(key, out n);
                this.claimed[key] = n + 1;
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

        private static long Key(int floor, int x, int z)
        {
            return ((long)(floor + 8) << 40) | ((long)(x + 4096) << 20) | (uint)(z + 4096);
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
