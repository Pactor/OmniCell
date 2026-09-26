namespace OmniCell.Core.Missions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using SmokeLounge.AOtomation.Messaging.GameData;

    /// <summary>
    /// Builds a whole mission: the building, and everything standing in it.
    /// </summary>
    /// <remarks>
    /// <see cref="MissionBuilder"/> lays the rooms out; this furnishes and
    /// populates them from the same pack. Every number it works to was
    /// measured off recorded missions rather than chosen, and
    /// Documentation/Missions.md says where each came from:
    ///
    ///   * a building holds roughly 11 to 69 monsters, one or two to a room,
    ///     almost all on the entrance floor;
    ///   * their level sits within three or four of the mission's QL, and not
    ///     of the player's level;
    ///   * it holds 1 to 22 chests, in the places that room template has been
    ///     seen to put them, which is usually one or two places;
    ///   * a lock reads 50 when there is no lock, which is 220 chests of 255,
    ///     and otherwise runs 77 to 95;
    ///   * the objective, for the types that have one in the world, is an item
    ///     on the floor.
    ///
    /// What it cannot do is invent placements for a room nobody has walked
    /// into. 230 of the 639 rooms have a furniture spot on record, so a room
    /// without one simply gets nothing - which is honest, and fixed by running
    /// more missions rather than by guessing a spot.
    /// </remarks>
    public class MissionFactory
    {
        private readonly MissionPool pool;
        private readonly Random random;

        public MissionFactory(MissionPool pool, int seed)
        {
            if (pool == null) throw new ArgumentNullException("pool");
            this.pool = pool;
            this.random = new Random(seed);
        }

        /// <summary>
        /// Build one.
        /// </summary>
        /// <param name="quality">
        /// The mission's QL, which is the character's level times a multiplier
        /// the difficulty sets - about 0.79 at difficulty 3 and 1.00 at 6.
        /// </param>
        /// <param name="type">What the player is here to do.</param>
        /// <param name="floors">1 for solo; 3 or 4 for a team mission.</param>
        public Mission Build(int quality, MissionType type, int floors = 1)
        {
            var builder = new MissionBuilder(this.pool, this.random.Next());
            MissionLayout layout = builder.Build(builder.RollRoomCount(), floors);
            if (layout == null) return null;

            var mission = new Mission { Layout = layout, Quality = quality, Type = type };

            this.Furnish(mission);
            this.Populate(mission);
            this.PlaceObjective(mission);
            return mission;
        }

        #region contents

        private void Furnish(Mission mission)
        {
            foreach (Placed p in this.Placements(mission))
            {
                // A room gets a chest or it does not. Retail gives a building
                // of seventeen rooms about nine or ten, so most rooms do.
                if (this.random.Next(100) >= 55) continue;

                List<MissionFurnitureSpot> chests = p.Room.Furniture
                    .Where(s => s.Kind == MissionFurnitureKind.Chest).ToList();

                float x, z;
                bool approximate = false;
                if (chests.Count > 0)
                {
                    MissionFurnitureSpot spot = Weighted(chests, s => s.Seen, this.random);
                    x = p.X + (spot.X * 2f);
                    z = p.Z + (spot.Z * 2f);
                }
                else if (this.SomewhereInside(p, out x, out z))
                {
                    // Nobody has walked into this room, so where the chest goes
                    // is a guess. It is marked as one.
                    approximate = true;
                }
                else
                {
                    continue;
                }

                mission.Furniture.Add(new MissionFurniture
                                      {
                                          Kind = MissionFurnitureKind.Chest,
                                          Room = p.Slot,
                                          Floor = p.Floor,
                                          X = x,
                                          Z = z,
                                          LockDifficulty = this.RollLock(),
                                          Approximate = approximate
                                      });
            }
        }

        /// <summary>
        /// 220 of 255 captured chests read 50, which is no lock at all. The
        /// rest are spread over 77 to 95.
        /// </summary>
        private int RollLock()
        {
            return this.random.Next(255) < 220 ? 50 : 77 + this.random.Next(19);
        }

        private void Populate(Mission mission)
        {
            if (this.pool.Creatures.Count == 0) return;

            List<Placed> rooms = this.Placements(mission).Where(p => p.Floor == 0).ToList();
            if (rooms.Count == 0) return;

            // 11 to 69 in the recordings, and a building of about seventeen
            // rooms held them one or two apiece.
            int wanted = Math.Min(rooms.Count * 2, 11 + this.random.Next(59));

            var order = rooms.OrderBy(r => this.random.Next()).ToList();
            int placed = 0;
            for (int pass = 0; pass < 2 && placed < wanted; pass++)
            {
                foreach (Placed p in order)
                {
                    if (placed >= wanted) break;

                    MissionCreature c = Weighted(this.pool.Creatures, x => x.Seen, this.random);
                    int spread = c.MaxLevelOffset - c.MinLevelOffset;
                    int level = mission.Quality + c.MinLevelOffset
                                + (spread > 0 ? this.random.Next(spread + 1) : 0);

                    // Somewhere on the room's own floor, away from the walls.
                    float x, z;
                    if (!this.SomewhereInside(p, out x, out z)) continue;

                    mission.Monsters.Add(new MissionMonster
                                         {
                                             Room = p.Slot,
                                             Floor = p.Floor,
                                             X = x,
                                             Z = z,
                                             Monster = c.Monster,
                                             Name = c.Name,
                                             Level = Math.Max(1, level)
                                         });
                    placed++;
                }
            }
        }

        private void PlaceObjective(Mission mission)
        {
            if (mission.Type != MissionType.FindItem
                && mission.Type != MissionType.ReturnItem
                && mission.Type != MissionType.Repair)
            {
                return;
            }

            // As far from the way in as the building goes, which is what a
            // blitz is about: the captured find item objectives were tens of
            // metres from the entrance, in the last room.
            List<Placed> rooms = this.Placements(mission).ToList();
            if (rooms.Count == 0) return;

            Placed last = rooms[rooms.Count - 1];
            List<MissionFurnitureSpot> spots = last.Room.Furniture
                .Where(s => s.Kind == MissionFurnitureKind.Item).ToList();

            float x, z;
            bool approximate = false;
            if (spots.Count > 0)
            {
                MissionFurnitureSpot spot = Weighted(spots, s => s.Seen, this.random);
                x = last.X + (spot.X * 2f);
                z = last.Z + (spot.Z * 2f);
            }
            else if (!this.SomewhereInside(last, out x, out z))
            {
                return;
            }
            else
            {
                approximate = true;
            }

            mission.Objective = new MissionFurniture
                                {
                                    Kind = MissionFurnitureKind.Item,
                                    Room = last.Slot,
                                    Floor = last.Floor,
                                    X = x,
                                    Z = z,
                                    LockDifficulty = 50,
                                    Approximate = approximate
                                };
            mission.Furniture.Add(mission.Objective);
        }

        #endregion

        #region geometry

        private struct Placed
        {
            public int Slot;
            public MissionPoolRoom Room;
            public int Floor;

            /// <summary>The room's world origin, in metres.</summary>
            public int X;

            /// <summary>The room's world origin, in metres.</summary>
            public int Z;

            public int Rotation;
        }

        private IEnumerable<Placed> Placements(Mission mission)
        {
            for (int slot = 0; slot < mission.Layout.Rooms.Count; slot++)
            {
                BuildingRoomInfo info = mission.Layout.Rooms[slot];
                MissionPoolRoom room = this.pool.Rooms.FirstOrDefault(r => r.Index == info.Room);
                if (room == null) continue;

                int h = info.Rotation % 2 == 0 ? room.SlotsHeight : room.SlotsWidth;
                yield return new Placed
                             {
                                 Slot = slot,
                                 Room = room,
                                 Floor = info.Floor,
                                 X = info.X * 10,
                                 Z = (mission.Layout.GridHeight - info.Z - h) * 10,
                                 Rotation = info.Rotation
                             };
            }
        }

        /// <summary>
        /// A point on one of the room's own floor cells, in world metres.
        /// </summary>
        private bool SomewhereInside(Placed p, out float x, out float z)
        {
            var floor = new List<(int X, int Z)>();
            for (int cz = 1; cz < p.Room.CellsHeight - 1; cz++)
            {
                for (int cx = 1; cx < p.Room.CellsWidth - 1; cx++)
                {
                    if (p.Room.HasFloor(cx, cz)) floor.Add((cx, cz));
                }
            }

            if (floor.Count == 0)
            {
                x = 0;
                z = 0;
                return false;
            }

            (int X, int Z) cell = floor[this.random.Next(floor.Count)];
            int px = (cell.X * 2) + 1;
            int pz = (cell.Z * 2) + 1;

            int width = p.Room.SlotsWidth * 10;
            int depth = p.Room.SlotsHeight * 10;
            for (int turn = 0; turn < p.Rotation % 4; turn++)
            {
                int nx = pz;
                pz = width - px;
                px = nx;
                int swap = width;
                width = depth;
                depth = swap;
            }

            x = p.X + px;
            z = p.Z + pz;
            return true;
        }

        private static T Weighted<T>(IList<T> items, Func<T, int> weight, Random random)
        {
            int total = items.Sum(i => Math.Max(1, weight(i)));
            int roll = random.Next(total);
            foreach (T item in items)
            {
                roll -= Math.Max(1, weight(item));
                if (roll < 0) return item;
            }

            return items[items.Count - 1];
        }

        #endregion
    }
}
