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

What the pack does **not** carry, and would need before a room can be furnished: the heights,
which matter for ramps and are in the same export; and the rooms' object records, which are
decoded but unidentified - 44 bytes that look like a position, a rotation, a second point and
a radius - so there is still no rule for where a lift button stands inside a room.

### Lifts

The floor buttons are items of type 0xC73D, recognised by template id: 159863 `Button (down)`,
159869 `Button (up)`, 159864 `Button (boss)`, which goes straight to the boss room. 159862 and
159865 to 159868 are platforms standing on the same spots and are not used.

Riding one is four messages. The client sends `GenericCmd Use`; the server echoes it, sends
`CharacterAction 170` with p1 54 and p2 10 - stat 54, Level, locked for ten seconds - and an
`N3TeleportMessage` within about 0.3 s to the position of the paired button on the other
floor. `CharacterAction 164` with p2 54 releases it ten seconds later. The same 170/164 pair
locks and releases every skill.

## What a server still cannot do

1. **Choose the types.** The dimensions decide it and the function is unmapped. See the sweep
   above.
2. **Lay out a building.** The pieces are now in the repository - every room, its footprint
   and its floor - but nothing says how retail picks and connects them: how many floors, where
   the elevator and boss rooms go, how connectivity is guaranteed. An algorithm of our own is
   buildable on the pack; it will not be retail's.
3. **Fill the rooms.** Buttons, doors, chests and the objective are server-spawned dynels.
   The pool rooms carry an `objects` array whose 44-byte records are decoded but not
   identified - they look like door or blocker placements - so there is no rule for where a
   button stands inside a room, only four observed positions.
4. **Populate it.** Which creatures a pool spawns at a given level, how many, and how the boss
   is chosen: no data at all. Nothing in any capture here addresses it.
5. **Write the text.** The objective's name is inside server-composed prose and no template
   grammar has been captured. The wordings are formulaic enough to reconstruct by hand.

There is also a smaller one: seventeen of `QuestInfo`'s forty members are still unnamed, and
a server emitting an offer has to put something in all of them.
