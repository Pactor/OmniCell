# Missions - what is settled, and what a server still cannot do

Nothing in OmniCell answers a mission roll today: there is no QuestAlternative handler, and
a terminal here offers nothing. This page is the standing account of what would be needed
and which parts of it are already evidence rather than intention. Every claim names where it
came from. Where something is not known it says so rather than filling the gap.

Two bodies of work feed it. The captures in `E:\Funcom\sniffs` and `E:\Funcom\captures`,
decoded with `Tools/Capture/bin/PcapDecode.exe`; and the client-data extraction done in the
AOBuddy10 project, which decoded the mission room pools and verified the layout rule against
walked missions. AOBuddy10 is a client, so it knows how to consume a mission the server hands
it - the parts below that it settles are the parts a server has to reproduce.

## The roll

`QuestAlternativeMessage`, both directions, same message id. The client sends the difficulty,
six dimension bytes and the terminal identity with an empty mission list; the server answers
with the same message carrying up to five missions. The client refuses more than five and
says so - "Number of quests = %u" at Gamecode 0x100CB27C.

Dimension bytes are signed and the client maps a percentage to `(percent - 50) * 2`, so 0%
is -100 (0x9C), 50% is 0 and 100% is +100 (0x64). Wire order is GoodBad, Order/Chaos,
Open/Hidden, Physical/Mystical, Headon/Stealth, Money/Experience. The server keeps them
between rolls; a reroll that sends the same bytes is normal and eight of them in a row appear
in one capture. Originator 1 is a solo booth and 2 its team version, and `IsTeamOriginator`
at GameData.dll 0x10002D23 pairs the even values with the odd ones all the way up.

### The answer is always three of one type and one each of two others

Twenty one rolls, expanded through this project's serializer on 2026-09-25:
`20260910-200346_s2` (four rolls, four different settings) and `20260923-201746_s8`
(seventeen rolls, three settings, the owner deliberately reversing every slider between
sets). Every single one is 3 + 1 + 1, with the three at indices 0 to 2. That is the shape
the community slider guides have always described, and it is now measured.

### Type selection is deterministic in the dimensions

Fifteen consecutive rolls at one setting returned the same three types every time, with seeds
from 180,241,537 to 2,141,450,657. Six of those fifteen were sent at difficulty 11 and nine
at difficulty 1. So neither the seed nor the difficulty takes any part in choosing the types -
the dimensions alone do.

This matters more than it looks. It means the function can be mapped exactly with one roll
per setting, with no repeats needed to average anything out.

### The type is the mission's icon

`QuestInfo.MissionIconId` carries it. Five values are named, from the assignment text each
offer carries - the wordings are formulaic and separate cleanly - and they are in
`GameData/MissionType.cs`:

| value | hex | type | the giveaway |
|---|---|---|---|
| 11329 | 0x2C41 | return item | "then bring it back here" |
| 11330 | 0x2C42 | kill person | "kill this monster" / "must be cleansed" |
| 11335 | 0x2C47 | find person | "track him/her down ... observe it" |
| 11337 | 0x2C49 | find item | "pick it up, and destroy it" |
| 11342 | 0x2C4E | repair | "Add some Spiked Food Sacks to the Theft Secure Food Dispenser" |

11335, 11337 and 11342 agree with the type codes read out of the quest record of a mission
that was actually entered, which were found earlier and separately. 11340 appears in captured
quest records but in none of the 105 offers, so it has no name here.

An authored quest carries an ordinary icon in the same field - 244818 and 158429 are the two
Arete ones in the corpus - so this is an icon that happens to be the type for a generated
mission, not a type field.

### The six settings measured

Percentages, not wire bytes. The bold entry is the triple.

| capture | difficulty | Bad | Chaos | Hidden | Myst | Stealth | XP | offered |
|---|---|---|---|---|---|---|---|---|
| 0910 #1 | 6 | 55 | 85 | 50 | 33 | 96 | 49 | **3 find item**, return item, kill |
| 0910 #2 | 6 | 76 | 83 | 7 | 9 | 53 | 2 | **3 kill**, find person, repair |
| 0910 #3 | 11 | 71 | 62 | 81 | 89 | 77 | 11 | **3 repair**, find person, return item |
| 0910 #4 | 9 | 37 | 47 | 43 | 26 | 80 | 88 | **3 repair**, kill, return item |
| 0923 A (2 rolls) | 6 | 100 | 0 | 0 | 100 | 0 | 0 | **3 kill**, find person, return item |
| 0923 B (15 rolls) | 11 and 1 | 0 | 100 | 100 | 0 | 100 | 100 | **3 return item**, find item, find person |

Which dimension chooses which type is **not settled**, and is not guessed here. Six settings
against six dimensions does not determine it, and the community tables are not separable as
written either - their "three repair" and "three find person" sections both sit at Good 0%,
Order 0%, Hidden 100%. What would settle it is a sweep: five dimensions pinned at 50% and the
sixth walked across 0, 25, 50, 75 and 100 is thirty rolls and gives the one-at-a-time effects;
a three-level grid over all six is 729 and gives the interactions as well. One roll per point,
because the answer is deterministic.

## Accepting, and finishing

- **Accept**: `CreateQuestMessage` with the identity from the list. The server answers
  `QuestFullUpdateMessage` with `AnnounceAsNew` set and a *new* identity - in the capture the
  list id ending C9CB came back as C9D0.
- **Abandon**: `QuestMessage` version 1 with that identity.
- **Finish**: the reward goes to the overflow window (`TemplateAction 87` then
  `ContainerAddItem`), then `FeedbackMessage 108871108`, then `CharacterAction MissionChanged`
  to the holder only, then `QuestMessage` removing it. A teammate gets the reward and the
  removal too, but not `MissionChanged`.

Objective completion, per type, as captured:

- **find person** - the client sends `CharacterAction InfoRequest` and a `LookAt` with
  ReturnInfo 1 on the named NPC. Only the NPC named in the mission text completes it; two
  wrong targets in one capture did nothing.
- **find item** - one `LookAt` with ReturnInfo 0 on the item. Nothing is picked up and the
  item stays on the floor.
- **repair** - `GenericCmdMessage` UseItemOnItem with two targets, the inventory item first
  and the fixture second.

## The building

Every mission is a generated playfield. `PlayfieldAnarchyF` version 4 ends with an
`ACGBuildingGeneratorData` - see `GameData/BuildingGeneratorData.cs`, where every field is
named off the client's own record dump. It carries the template playfield (the pool), a slot
grid, the world height and, per placed room, a room index, floor, grid x, grid z and a
rotation. A static playfield sends an empty identity there instead, which is why no capture of
one ever contained this.

The geometry to go with it is the pool's own. AOBuddy10 extracted all ten autocontent pools
from the client - 320 Midtech, 321 HiTech, 322 Cave, 324 Clan, 331 tarm, 341 Grey Caves,
346 Omnilab, 351 Subway Ventil, 362 SL ACG, 382 Alien ACG - with each room's floor tiles,
heights, doors and collision. The placement rule was verified against three live missions:
a slot is 10 m, x counts from 0 and z from the far edge, a pool room spans (5k + 1) cells so
it covers k slots plus a shared door cell, and 97.1% to 100% of the walked points landed on
the composed floor within a metre.

Floors count up from the lowest, not from zero: Grey Caves numbers its floors 0, -1, -2 and
they were walked at y 133, 69 and 0, so the height of a floor is `pool y + (floor - lowest) *
worldHeight`.

### The pools are in the repository now

`Datafiles/missionpools.ocp` - ten pools, 639 rooms, ten kilobytes. It is an ordinary content
pack, the fourth kind, read by `MissionPoolLoader` the way items and playfields are read.

Per room it carries the index the zone-in packet sends, the client's name for it, the
footprint in ten metre slots, what the name says the room is for, and one bit per two metre
cell saying whether there is a floor there. Nothing else: which tile id draws a cell is the
client's business, and a server placing rooms does not need it.

Two things were checked over all 639 rooms rather than assumed, and the extractor fails if
either stops being true:

- every room's tile grid is exactly the size its rectangle claims;
- every side is five cells per slot plus one, the extra row and column being the cells shared
  with the rooms beyond.

A third was checked and is worth writing down because it decides how connectivity is
computed. The shared row and column - a room's last - hold only two values anywhere in the ten
pools: zero in 15,269 cells and 0x80 in 110. Every ordinary tile id is in the interior. So the
shared cell is normally claimed by the *neighbour*, whose own first row or column carries a
real tile, and the composition rule is the one AOBuddy10 arrived at: a cell is floor when any
room covering it says so. Two placed rooms therefore connect wherever the composed grid has
floor across their boundary, and the pool's undecoded door pairs are not needed for placement.

Roles come off the room names and nothing else, which is all that says them: 41 entrances, 30
start rooms, 27 boss rooms, 3 elevators, 6 ramps, and 532 rooms whose names say nothing. Seven
pools name no elevator at all, and the Grey Caves missions walked on 2026-09-23 still had
working lifts, so an absent elevator room does not mean an absent lift - the buttons are items
the server spawns and they need not stand in a room named for them.

The decode behind this is not ours. The client keeps the pools in a tilemap record in its
resource database, and that record was cracked in the AOBuddy10 project, which exports a
`rooms.json` per playfield; `Tools/Capture/PoolExtract.cs` reads that export and writes the
pack. Pointing it at the client instead would mean a second implementation of a decode that is
already checked against three walked missions. The pack is what the repository carries, the
same arrangement as the item and playfield packs.

    PoolExtract <nav directory> <output.ocp>

It reads the pack back and compares it field by field before it reports success.

What the pack does **not** carry: the per-cell heights, which matter for ramps and are in the
same export.

The rooms' 44-byte object records are not in it either, and they are not the fixture
placements they were taken for. Only the cave pool has any - 52 records over 42 rooms, none at
all in the other nine - while a mission in the Grey Caves sent seventeen doors. Each is a
position, an identity rotation, a second point and a radius of one and a half to five metres,
which is a capsule, and a capsule in a cave and nowhere else is a rock to walk round. They are
blockers, not sockets.

### Where a door can stand: solved

The room record in the client's tilemap ends with a count and that many pairs of int16, and
the second of each pair is:

    value = 4 * (z * 5W + x) + side        side: 0 south, 1 east, 2 north, 3 west

with W the room's width in slots and (x, z) a cell on the room's 5W by 5H **interior** grid -
the floor mask less its shared last row and column. The door stands in the named wall of that
cell. The first int16 is the room it opens onto, 0xFFFF where the template does not say, which
is 1,851 of the 1,889.

Two tests, and the extractor runs both every time the pack is built:

- **Every one of the 1,889 records in the ten pools decodes to a cell inside its own room.**
  None out of range. 589 north, 476 south, 425 east, 399 west.
- **Placing the sockets through the room lists of twenty six recorded missions reproduces
  every door those servers sent.** Six pools, 451 doors, from the AOBuddy10 bot's own run
  recordings, which pair each mission's zone-in packet with the doors it then received. All
  451, to the metre.

That second test began as one mission and had to be widened, which is worth recording because
the narrow version passed while being wrong. Rotating a placed room clockwise reproduces all
seventeen doors of the Grey Caves mission it was written against - and 362 of the 451.
Anticlockwise gives 451 of 451. Grey Caves could not separate them because its rooms are
nearly all square; the extractor now also checks a HiTech building, which scores 21 of 21 one
way and 8 of 21 the other.

A socket is not a door: which neighbour each one opens onto is the generator's business. But
the *count* is not - the number of sockets in a template is the number of doors the room gets,
which agreed room by room with that capture everywhere the capture was complete. 229 of the
1,889 sit on an interior cell rather than the boundary, which is a door between parts of one
room.

Two more things the same capture settles about a door:

- `DoorFullUpdate` carries `Room` and `AdjoiningRoom`, and they are **indexes into the
  placement list the zone-in packet sent**, with -1 for the outside. The one door with
  `Room = -1` is the way in.
- `LockDifficulty` was 50 on thirteen of them and 184 on four, with no keyholders. A mission
  therefore has ordinary doors and hard ones in the same building. The bot's 451 doors are
  almost all unlocked - one of them - so whatever sets the difficulty, those runs barely
  touch it.

A third thing it appeared to settle, it did not. Every one of those seventeen doors sits at
the midpoint of a slot edge, one coordinate a multiple of ten and the other a multiple of ten
plus five, which read like a rule. It is only true of a socket on a room's boundary. The
HiTech building has a door at (241, 176), which is on no ten metre line at all, and it comes
from one of the 229 sockets that sit on an interior cell.

### Lifts

The floor buttons are items of type 0xC73D, recognised by template id: 159863 `Button (down)`,
159869 `Button (up)`, 159864 `Button (boss)`, which goes straight to the boss room. 159862 and
159865 to 159868 are platforms standing on the same spots and are not used.

Riding one is four messages. The client sends `GenericCmd Use`; the server echoes it, sends
`CharacterAction 170` with p1 54 and p2 10 - stat 54, Level, locked for ten seconds - and an
`N3TeleportMessage` within about 0.3 s to the position of the paired button on the other
floor. `CharacterAction 164` with p2 54 releases it ten seconds later. The same 170/164 pair
locks and releases every skill.

## What lives in a mission: what 28 recorded runs say

The AOBuddy10 bot records every mission it runs, gated on `MissionRecord`. Each run leaves a
`.pkt` of every packet since the zone-in and a `.json` index: the roll, the pool, the mission
type, the server's clear percentage, and per-mob and per-door lists. Twenty eight runs,
12.3 MB, 135,877 packets, in that project's `Plugins/AOBuddy/missions/records`.

What they settle:

- **A building holds roughly 11 to 69 monsters.** The clear percentage the server sends is
  against the building's full count, so seen ÷ clear% recovers it: 11, 12, 13, 14, 15, 16,
  17, 17, 17, 18, 18, 20, 21, 30, 40, 44, 69 over the runs that reached a clean reading.
- **They stand one or two to a room.** 102 rooms held one, 98 held two, 13 three, 6 four, one
  five. Nothing holds a crowd.
- **They are on the entrance floor.** 356 of 366 on floor 0, 10 on floor 1.
- **The creature set is mostly but not only per pool.** Of 55 names, 39 appear in one pool
  only; 8 appear in three or four - `A-500 soldier`, `A-500 elite`, `Rhinoman Smasher`,
  `Hellhound`, the `Claw-C22` pair. So a generator needs a shared pool of creatures plus
  per-pool ones, not one table per pool.
- **Level tracks the mission, tightly.** 366 monsters between 29 and 40, and the split is
  clean: every difficulty 3 run came out 29 to 33 and every difficulty 4 run 34 to 40.
  Whether that is the difficulty or the character's own level is not separable here - he was
  levelling through the same window - but one of the two sets it, and it is not the pool.

And one negative result worth as much: **the dimensions do not pick the pool.** All 28 runs
were rolled at the same setting - difficulty 3 or 4, and 0%, 100%, 0%, 0%, 0%, 0% across the
six - and came back as seven different pools and two different mission types. The bot rolls
one configuration, so this corpus says nothing else about the dimensions; the sweep is still
the sweep.

Two gaps in the corpus to fill when convenient: 18 of the runs are find item and 8 find
person, with no repair, kill or return item; and one single door out of 451 was locked, so
nothing here bears on lock difficulty.

## What a server still cannot do

1. **Choose the types.** The dimensions decide it and the function is unmapped. See the sweep
   above.
2. **Lay out a building.** The pieces are now in the repository - every room, its footprint
   and its floor - but nothing says how retail picks and connects them: how many floors, where
   the elevator and boss rooms go, how connectivity is guaranteed. An algorithm of our own is
   buildable on the pack; it will not be retail's.
3. **Fill the rooms.** Doors are solved - the sockets are in the pack and the placement
   reproduces a real mission exactly. What is left is everything else the server spawns:
   chests, traps, lift buttons and the objective itself. The pool rooms say nothing about
   those, and the only positions on record are the four buttons observed in the 2026-09-23
   runs.
4. **Populate it.** Narrowed, not closed - see the section above. The counts, the spacing,
   the floor and the level band are measured; what is not is the creature table itself (which
   names are eligible at a level, and how the shared ones divide from the per-pool ones) and
   how the boss is chosen.
5. **Write the text.** The objective's name is inside server-composed prose and no template
   grammar has been captured. The wordings are formulaic enough to reconstruct by hand.

There is also a smaller one: seventeen of `QuestInfo`'s forty members are still unnamed, and
a server emitting an offer has to put something in all of them.
