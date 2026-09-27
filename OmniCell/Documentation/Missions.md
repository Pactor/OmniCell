# Missions - what is settled, and what a server still cannot do

OmniCell runs a mission end to end as of 2026-09-26. A player walks to a terminal, clicks it,
reads five assignments, takes one and is handed the key; walks to a mission entrance and in;
fights the monsters; finds, fetches or kills what the assignment named; and is paid. The
building, its floors, its doors and everything standing in them are generated when the
mission is taken.

What is not there is listed at the end, and the largest of it is that none of this has been
in front of a client yet - it is checked by putting every packet through the serializer, not
by playing it.

This page is the standing account: what is evidence, what is written, and what is neither.
Every claim names where it came from. Where something is not known it says so rather than
filling the gap.

## What the server does today

| | where |
|---|---|
| answers a roll with five missions | `QuestAlternativeMessageHandler` |
| draws the dimensions when the panel is untouched, and says which it drew | `MissionRoller.Roll` |
| writes the assignment out of the captured openers and bodies | `MissionText`, `Datafiles/missiontext.tsv` |
| hands over the mission and the key when one is taken | `CreateQuestMessageHandler` |
| generates the building, floors and all | `MissionBuilder`, `MissionBuilding` |
| furnishes and populates it | `MissionFactory` |
| gives one up | `QuestMessageHandler`, inbound |
| serves the mission's playfield | `MissionPlayfields`, `Playfield.Generated` |
| lets a key open a door, and a copy do the same | `MissionPlayfields.Cut`, `MissionCompletion.OnDuplicate` |
| walks a character in and back out | `Playfield.CheckMissionDoor` |
| sends the building on zone-in | `PlayfieldAnarchyFMessageHandler.Mission` |
| spawns the monsters | `MissionSpawner` |
| sends the doors, chests and objective | `MissionContents` |
| finishes it and pays | `MissionCompletion` |

`Tools/Capture/MissionOffers.exe` is the check on all of it: it rolls, takes, builds, and puts
both packets through the real serializer against everything the client refuses a roll for.
`MissionGen.exe` builds 3,000 buildings and asserts what 325 recorded ones satisfy.

Two things are held only in memory, and a restart loses them: which missions a terminal
offered, and which a character is on. Missions are generated, so there is nothing to reload
them from; a table is the fix and there is not one yet. A character who logs out inside a
mission is put at Borealis when they come back, rather than into a playfield that no longer
exists.

### Entering one

The doors work by walking rather than clicking, which is what the capture shows - there is no
`Use` on the entrance at all, only the teleport. Outside, the doors are the playfield pack's
own 2,235 `MissionEntrance` statels and the key in the character's pocket decides which
mission one leads to; inside, there is one door, the socket the building was left open on,
and it leads back to where they came in.

The landing point is half a metre inside that door, on the floor - the captured entrance
stood at 300, 145 and the character landed at 299.9, 5.01, 145.4. Every generated mission is
checked for it, because a landing point in the wall is a character stuck in the geometry.

A floor stands at 5.01 plus 64 metres per floor, and the 64 is the generator's own
`WorldHeight`. A four floor mission recorded on 2026-09-25 put every monster on floor 0 at
5.01 and every one on floor 1 at 69.01.

### Finishing one

| type | what does it | wired |
|---|---|---|
| find item | a `LookAt` on the objective; it is not picked up | yes |
| return item | picked up with `Use`, then used on a mission terminal | yes |
| kill person | killing the one the assignment named | yes |
| find person | `CharacterAction InfoRequest` and a `LookAt` on the NPC | no |
| repair | the part used on the fixture | no |

Paying out is one sequence whatever finished it: the credits and the experience, the reward
item through the overflow window, `FeedbackMessage 108871108`, `MissionChanged`, the
`QuestMessage` that takes it off the list, and then the key and whatever was carried back
destroyed.

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

### The answer is three of one type and one each of two others, nearly always

1,053 of 1,059 logged rolls are 3 + 1 + 1, with the three at indices 0 to 2 - the shape the
community slider guides have always described. The other six are 2 + 1 + 1 + 1, and they are
the same draw with one of the three replaced: `2 find item, kill person, find person, repair`
where the setting's usual answer is `3 find item, kill person, find person`. So the three is
a strong tendency of the draw and not a rule of the format.

### The mix is a weighted draw, and the dimensions set the weights

The AOBuddy10 bot logs every roll it sends - the difficulty, all six dimension bytes, the
seed - and then all five missions the server offers. Its log holds **1,059 complete rolls**
over nine settings, which is what this section rests on; the handful measured from captures
came first and were not enough to see the shape.

At a setting with any dimension pushed off centre, the mix is fixed:

| difficulty | bad | chaos | hidden | myst | stealth | xp | rolls | offered |
|---|---|---|---|---|---|---|---|---|
| 3 | 0 | 100 | 0 | 0 | 0 | 0 | 147 | 3 find item, kill person, find person |
| 4 | 0 | 100 | 0 | 0 | 0 | 0 | 76 | the same, 75 of 76 |
| 5 | 0 | 100 | 0 | 50 | 50 | 0 | 218 | the same, 217 of 218 |
| 6 | 0 | 100 | 0 | 50 | 50 | 0 | 488 | the same, all 488 |
| 8 | 0 | 100 | 0 | 50 | 50 | 0 | 21 | the same, all 21 |
| 1 | 50 | 50 | 50 | 50 | 50 | 0 | 30 | 3 find person, return item, find item |
| 6 | 50 | 50 | 50 | 50 | 50 | 0 | 39 | the same, all 39 |
| 6 | 50 | 50 | 50 | 50 | 50 | **50** | 25 | **19 different mixes** |

Three things fall out of that.

**Difficulty has nothing to do with it.** Six difficulty values - 1, 3, 4, 5, 6 and 8 - give
the same mix wherever the dimensions match. 954 rolls of it.

**With every dimension at 50 the draw is random.** The last row differs from the one above it
in one byte, the credits/experience dimension, and it goes from one mix in 39 rolls to
nineteen in 25. Every dimension at 50% is every wire byte zero, and the section below says
what that turns out to mean.

**One dimension moved on its own, once.** The two 50/50/50/50/50 rows differ only in
credits/experience, 0% against 50%, and that alone is the difference between "three find
person, every time" and a random draw. The 0/100/0 rows differ from them in three dimensions
at once and dominate with find item instead, so which of those three did it is not separable
yet. It is the first read on any dimension from rolls rather than from the community tables.

### With all six bytes zero the server draws the dimensions itself

The reply carries six dimension bytes of its own, and they are **not always the ones that
were asked for**. Nine rolls have now been read out of captures with both halves paired up,
by `Tools/Capture/MissionRolls.exe`, and the answer echoed the request in one of them.

| asked | rolls | answered |
|---|---|---|
| all six bytes zero (every slider at 50%) | 4 | six unrelated values, different every roll |
| anything else | 5 | the request, byte for byte |

The four that came back changed are the `20260910-200346` rolls, and their answers are the
four capture rows in the table further down - 55/85/50/33/96/49, 76/83/7/9/53/2 and the rest.
Those are not percentages a player can dial. The slider moves in notches and 96%, 7% and 2%
are not among them, which is the plainest sign that the server made them up rather than
receiving them. The rows are still right and are still the values the draw ran on; what was
wrong was reading them as the setting the player chose.

That also settles what the 50% row in the bot's table means. `50/50/50/50/50/0` is
`00 00 00 00 00 9C` - five zero bytes and one not - and it gives one mix in 39 rolls.
`50/50/50/50/50/50` is six zero bytes, and it gives nineteen mixes in 25. So it is not each
zero byte being replaced: it is all six at once or none, and only the untouched slider panel
sends all six.

**What this is worth.** The sweep this page has been asking for does not need a player to
dial anything. An untouched terminal rolled over and over hands out a random point in the six
dimensional space *and* the answer at that point, one pair per roll, and `MissionRolls.exe
--tsv` accumulates them across captures. Twenty five such rolls already produced nineteen
different mixes, so the points are well spread. A few hundred of them, recorded in one
sitting, is the dataset the type function needs - and it is cheaper than the 729 roll grid
this page proposed, because the server is doing the sampling.

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

### The seven settings measured from captures

Percentages, not wire bytes; the bold entry is the triple. The first four are the values the
**server answered with** after being asked at all six zero, so they are points it chose; the
rest are what the player asked for and got back unchanged.

| capture | difficulty | Bad | Chaos | Hidden | Myst | Stealth | XP | offered |
|---|---|---|---|---|---|---|---|---|
| 0910 #1 (server's) | 6 | 55 | 85 | 50 | 33 | 96 | 49 | **3 find item**, return item, kill |
| 0910 #2 (server's) | 6 | 76 | 83 | 7 | 9 | 53 | 2 | **3 kill**, find person, repair |
| 0910 #3 (server's) | 11 | 71 | 62 | 81 | 89 | 77 | 11 | **3 repair**, find person, return item |
| 0910 #4 (server's) | 9 | 37 | 47 | 43 | 26 | 80 | 88 | **3 repair**, kill, return item |
| 0923 A (2 rolls) | 6 | 100 | 0 | 0 | 100 | 0 | 0 | **3 kill**, find person, return item |
| 0923 B (15 rolls) | 11 and 1 | 0 | 100 | 100 | 0 | 100 | 100 | **3 return item**, find item, find person |
| 0926 | 1 | 100 | 0 | 0 | 0 | 0 | 0 | **3 find person**, kill, return item |

The last row is new and is the only one with good/bad pushed alone. It answers with three
find person - the same triple the bot's `50/50/50/50/50/0` row gives - so good/bad at either
end is not what picks find person, and that dimension is still unread.

Which dimension chooses which type is still **not settled**. Nine settings between the log and
these, against six dimensions, and only one pair differs in a single dimension. The community
tables are not separable as written either - their "three repair" and "three find person"
sections both sit at Good 0%, Order 0%, Hidden 100%.

What would settle it is a spread of points with their answers, and the section above says
where to get one cheaply: roll at an untouched terminal, inside a capture, as many times as
patience allows. Each roll is a free random point. `MissionRolls.exe <streams.csv> --tsv
MissionRolls.tsv` reads them out and appends them to `Tools/Capture/MissionRolls.tsv`, which
is where they accumulate; the nine known so far are in it.

A deliberate sweep still has a place for the one-at-a-time effects - five dimensions pinned
off centre and the sixth walked across - because a random point never isolates a dimension.
But it is no longer the only way in, and it costs a trip to a terminal per point where the
untouched roll costs a click.

## Accepting, and finishing

- **Accept**: `CreateQuestMessage` with the identity from the list. The server answers
  `QuestFullUpdateMessage` with `AnnounceAsNew` set and a *new* identity - in the capture the
  list id ending C9CB came back as C9D0, and on 2026-09-26 the five offers were instances
  ...3380 to ...3384, the one accepted was ...3383 and the quest granted was ...3385. So the
  new identity is not the offer's and is not derived from it; a server has to allocate one.
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
- **return item** - captured for the first time on 2026-09-26; it has three parts and they
  are below.

### Return item, end to end

From `20260926-135805`, streams 4, 10 and 12: a QL 33 HiTech mission for a prototype pair of
Basic Martial Artist Suit Boots, rolled in Borealis, walked, and handed back.

**The objective lies on the floor**, as a `SimpleItemFullUpdate` of identity type 51022 - not
in a chest, and not something a monster drops:

```
Identity      51022:1046659
Coordinate    231.1  5.1  177.7          the room it was placed in
Playfield     112085                     the mission's playfield instance
BodyLocation  111
Stats         Flags(0)=3221225475  StaticInstance(23)=85639
              ACGItemLevel(701)=33       the mission's QL
              ACGItemTemplateID(702)=85639  ACGItemTemplateID2(703)=22124
              MultipleCount(412)=1       QuestInstance(491)=1
```

`QuestInstance = 1` is what marks it as the objective, and `ACGItemLevel` is the mission's
quality rather than the item's own. It despawns and respawns as the player walks out of and
back into range, like any dynel.

**Picking it up** is four messages, and unlike find item the thing is actually taken:

```
client  LookAtMessage        Target=51022:1046659
client  GenericCmdMessage    Action=Use  Target=[51022:1046659]
client  ClientGetItemMessage Item=51022:1046659
server  ContainerAddItemMessage SourceContainer=51022:1046659 Target=None:0 TargetPlacement=0
```

It keeps its world identity in the inventory - the same 51022:1046659 is what the hand-in
names later.

**Handing it in** is the inventory item used on the terminal, which is what the client's own
hint describes as holding it on the cursor and right clicking the terminal:

```
client  GenericCmdMessage Action=UseItemOnItem Target=[Inventory:71, 56001:0xC0000320]
server  FormatFeedbackMessage                            the reward line
server  StatMessage       Cash=194381                    the new balance, not the payment
server  TemplateActionMessage ItemLowId=121650 ItemHighId=121651 Quality=33 Amount=1
                              Action=87 Placement=OverflowWindow:0
server  ContainerAddItemMessage SourceContainer=OverflowWindow:0
                                Target=OverflowWindow:<player> TargetPlacement=111
server  FeedbackMessage   CategoryId=110 MessageId=108871108
server  CharacterActionMessage Action=MissionChanged Target=Quest:281817989
server  QuestMessage      QuestIdentity=Quest:281817989   the mission is gone
server  StatMessage       SocialStatus=3
server  CharacterActionMessage Action=47 Target=51053:2681454   the key
server  CharacterActionMessage Action=47 Target=51022:1046659   the objective
server  DespawnMessage    51053:2681454
server  DespawnMessage    51022:1046659
```

The reward path - `TemplateAction 87` into the overflow window, then `ContainerAddItem`, then
`FeedbackMessage 108871108` - is the same one every other mission type and the apartment
move-in gift use. What is new here is the tail: **finishing a mission destroys the key and
the objective**, each by `CharacterAction 47` followed by a despawn.

### The mission key, and the duplicator

Accepting delivers a key into the inventory alongside the quest:

```
SimpleItemFullUpdate  Identity=51053:2681454  InventoryId=113  BodyLocation=111
                      StaticInstance/Template 28577   ACGItemLevel 1
                      Name="Mission key to A building in Borealis"
```

Identity type **51053** is the key, template **28577**, quality 1 whatever the mission's QL
is, and the name names the building rather than the mission.

The **Mission Key Duplicator** is identity type **51054**, template **28564**, also quality
1. Using it is the ordinary two-target form, both targets inventory slots:

```
client  GenericCmdMessage Action=UseItemOnItem Target=[Inventory:68, Inventory:69]
server  SimpleItemFullUpdate  Identity=51053:2681455  ... same template, same name
```

Three things the capture settles about it. The copy is **a new instance of the same
template** - nothing on it distinguishes it from the original except `TimeExist`. The
duplicator is **not consumed**: it is still in slot 68 two zones later. And the copy is an
ordinary item, so it trades - `TradeMessage` None, AddItem `Container=Inventory:70`, action
3, End - and the character who receives it can enter a mission that is not theirs.

**Entering** needs no use of the key at all. Walking into the `MissionEntrance` door -
identity type 56006, here instance 0xC0020320 - is answered with the pair every zone change
uses:

```
server  N3TeleportMessage  Destination=638.555 73.025 507.109
                           Playfield=51103:2224708          the building
                           ChangePlayfield=Playfield2:112085 the playfield instance
                           Playfield2=100002:1
server  ZoneRedirectionMessage 37.18.193.20:7512
```

The second character, holding only the duplicate, got the same building instance 2224708 and
the same playfield 112085. So the check is on the key being held, and the key names a
building rather than a player.

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

A third building was added on 2026-09-26 and it is the first from a packet capture rather
than from the bot: the return item mission, HiTech instance 2224708, 23 rooms and 24 doors.
**24 of 24 predicted.** It matters because the pcap and the bot's recordings share no code at
all - different reader, different reassembly, different machine - so the encoding is not
being confirmed by the thing that produced it. Its first room is 50 at (29, 15) turned twice,
which is the other HiTech building's first room and placement as well; the entrance room of a
pool looks to be fixed.

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
- **Level follows the mission's QL, and the QL follows the character and the difficulty
  together.** The bot keeps a `qlmap.json` of level and difficulty against the QL the missions
  came back at, which is what separates the two:

  | character level | difficulty | mission QL | QL / level |
  |---|---|---|---|
  | 40, 41, 42 | 3 | 32, 32, 33 | 0.78 - 0.80 |
  | 44, 45, 46, 47 | 4 | 37, 38, 39, 39 | 0.83 - 0.85 |
  | 40 | 5 | 36 | 0.90 |
  | 39 | 6 | 39 | 1.00 |

  So the difficulty is a multiplier on the character's level, rising by roughly a twentieth a
  step. And the monsters sit on the QL, not on the player: the difficulty 3 runs averaged
  **31.3** against a QL of 32 while the character was 40 to 42, and the difficulty 4 runs
  **37.2** against a QL of 37 to 39 while he was 44 to 47. Spread is about three either way -
  74 and 438 monsters respectively.

And one negative result worth as much: **the dimensions do not pick the pool.** All 28 runs
were rolled at the same setting - difficulty 3 or 4, and 0%, 100%, 0%, 0%, 0%, 0% across the
six - and came back as seven different pools and two different mission types. The bot rolls
one configuration, so this corpus says nothing else about the dimensions; the sweep is still
the sweep.

Two gaps in the corpus to fill when convenient: 18 of the runs are find item and 8 find
person, with no repair, kill or return item; and one single door out of 451 was locked, so
nothing here bears on lock difficulty.

## What a mission is furnished with

The same recordings, read as packets rather than as their index. 101,574 server packets over
the 28 runs, and the message model reads every one of them - nothing unparsed.

Per building, counting each object once:

| | per building | over 28 runs |
|---|---|---|
| doors | 7 to 27 | 481 |
| chests | 1 to 22 | 255, over 20 templates |
| world items | 0 to 7, usually 1 | 31, over 10 templates |
| traps | none | none |

- **The objective is an item on the floor and it is nearly always the same template.** 19 of
  the 31 world items are template 100341, which is the `Urgent Sensitive Information` of the
  2026-09-23 find-item capture, and 18 of the 28 runs were find item.
- **Lift buttons show up as world items too** - 159863, 159867 and 159869 - and only six
  times, because these buildings are nearly all one floor.
- **50 is what a lock reads when there is no lock.** 476 of 481 doors and 220 of 255 chests
  sit at exactly 50. The rest are real: five doors at 39, 45 and 46, and thirty five chests
  spread over 77 to 95.

### The first evidence for two of the dimensions

Every one of these runs was rolled at open/hidden 0% and head-on/stealth 0%, and over 28
buildings they produced **five locked doors out of 481, and not one trap**.

Set that against the one mission sniffed at the other end - `20260923-201746`, hidden 100% and
stealth 100% - which carried four locked doors out of seventeen at difficulty 184, and a trap.

That is one-sided: it shows the low end of both dimensions buys almost nothing, not that the
high end is what buys it, since that mission moved every slider at once. But it is the first
measurement that touches either, and it agrees with what the community tables have always
said - open/hidden is locks, head-on/stealth is traps.

### Where in a room they stand: a table, not a rule

Taking every spawned object back through the placement transform - the same one the door
sockets go out through, run backwards - puts it in its room template's own frame. If the
generator scattered things, those offsets would be all over the room. They are not.

- **`clan_wc`, fifteen chests over fifteen buildings, two positions.** Nine at (3.35, 4.30)
  and six at (0.70, 3.40), in cells from the template's origin, repeating to the centimetre.
- **`clan_stair`, twelve chests, two positions.** Seven and five.
- **`clan_elevator`, fourteen chests, three positions.** Six, five and three.

Over the whole corpus - 2,983 objects, once the 1,937 single-object captures the bot saves
beside its streams are read as well as the 28 full recordings - **612 spots over 230 of the 639
rooms**. All but six of the 2,983 fell inside a placed room. 94 of the 249 room-and-kind groups
have exactly one spot, and two thirds of a group's sightings are on its commonest. Doors come
out at 94 of 128 groups with one position, which is what sockets should look like and is a
second check on the transform.

So furniture placement is **authored per room template and chosen from a short list**, not
computed. A generator does not need a rule for it; it needs the list. The list is now in the
pack, alongside the sockets, and `Tools/Capture/MissionSpots.tsv` is the readable form it is
built from - extend that file with more recorded runs and the other 409 rooms fill in.

Two caveats on the reduction. 216 of the 767 objects sit inside more than one room's bounding
box - pool rooms overlap, which is why the composition rule is "floor if any room covering the
cell says so" - and the smallest covering room was taken, so a few assignments may be to the
wrong room. And six objects fell inside no room box at all.

## How retail lays a building out

276 distinct mission zone-in packets, which is 276 finished buildings with every room's
index, floor, cell and rotation. Enough to say what their generator does.

**The frame never varies.** Every one of the 276 is a 30 by 30 slot grid with a world height
of 64. Those are constants, not parameters.

**A building is 7 to 42 rooms, mean 17.5**, in a single hump peaked between 11 and 19. It
occupies about 8 by 9 slots of the 30 by 30, so the grid is far larger than anything put in
it.

**It is nearly always one floor.** 260 of 276 are flat. Of the sixteen that are not, none has
two floors: they have three or four, and always contiguous - (0,1,2), (-2,-1,0),
(-3,-2,-1,0), (0,1,2,3). So a building is flat or it is a tower, never a mezzanine.

### A team building, in four rules

The sixteen that are not flat are the team missions, and read together they leave no room for
invention. Nine of them count upwards from floor zero and seven downwards, and none mixes the
two. Then:

- **The floor furthest from zero holds exactly one room.** All sixteen. It is at grid 13, 13 -
  the middle of the thirty by thirty - in all sixteen, and it is a **boss room** in all
  sixteen: 320 rooms 71 and 72, 321 room 60, 324 rooms 45 and 46, 341 rooms 93 and 94, 346
  room 60, 351 room 63, and the pack has every one of those down as `BossRoom` from the client
  data, which is a different source agreeing.
- **No two floors share a grid cell.** Not one pair of the forty adjacent pairs. A floor
  occupies about eight by nine slots of a thirty by thirty grid, so two of them miss each
  other by luck perhaps half the time; forty in a row is not luck. The floors compete for one
  footprint, which is what the builder's collision map does by dropping the floor from its
  key.
- **The other floors are ordinary buildings.** 4 to 16 rooms each, mean 9.9 over 37 of them,
  grown and capped exactly like a flat one.
- **Only floor zero has a way in.** Every captured building has exactly one socket nobody
  meets; the floors above and below are closed all round and reached by lift.

A mission is a team one when the originator is even - 1 is a solo booth and 2 its team
version, per `IsTeamOriginator` - and one of the ten pools has no boss room in it at all, so
no team mission can be built there.

The floor a room is on is the third field of the room record, and reading the record one place
over is what made this look shapeless at first: every floor's z came out between 0 and 3,
because it was the rotation being read. The record is room, floor, x, z, rotation.

**Rotation is near enough uniform**, 1086 / 1389 / 1052 / 1316 over 4,843 placed rooms, with a
mild lean toward the quarter turns - which is what happens when oblong rooms get turned to
fit.

### It grows by matching door sockets

This is the part worth having, and the sockets prove it. Taking every placed room's sockets to
world coordinates across all 276 buildings:

- **93.0% of socket positions have exactly two rooms meeting there.** 4,797 of 5,156.
- **243 of the 276 buildings have exactly one socket left unpaired**, and that is the way in -
  the door whose `Room` is -1.

So a building is not rooms scattered on a grid and then joined. It is grown: take a free
socket, choose a room that has a socket able to land on it, rotate it so the two coincide,
place it if it fits, and strike both sockets off. The one you never fill is the front door.

The room usage confirms it from the other side. **66.3% of all 4,843 placements are rooms with
exactly one socket** - though such rooms are only 70 of the 434 in these pools. Each is placed
47 times on average against about 5 for everything else. A growth algorithm that must leave no
socket open needs caps, and dead ends are what it spends most of its rooms on:
`militaryot_endblock_one`, `office_endblock_one_bigger`, `clan_endblock_one_bigger`,
`Mine_Endblock_One_Small_07`.

### What a generator of ours needs, then

Nothing that is not already in `missionpools.ocp`:

1. Pick a pool, and a room count from the distribution above.
2. Place an entrance room. Keep a list of its free sockets.
3. While under the room count: pop a free socket, pick a room and a rotation whose socket
   lands on it, reject the placement if its floor cells collide with something already placed
   that is not the shared cell, otherwise place it and add its other sockets to the list.
4. When the count is reached, close every socket still open with a single-socket room.
5. Emit one door per distinct socket position, and `Room`/`AdjoiningRoom` are the indexes of
   the two rooms that meet there, with -1 for the entrance.

The one thing the recordings do not say is how it chooses *which* room for a socket. The usage
counts give a workable weighting, and nothing about the format requires retail's exact choice.

## The prose a terminal writes

An offer is an **opener bolted onto a body**, and sometimes a closer. The openers are
interchangeable - the same ones turn up on every kind of mission - and the body is chosen by
the type. `Datafiles/missiontext.tsv` has them, taken out of 105 captured offers: sixteen
openers, three closers, and two or three bodies for each of the five types. The server reads
that file and writes its offers out of it.

The slots are few and obvious once the texts are aligned: `{item}`, `{name}`, `{place}`,
`{playfield}`, `{fixture}`, `{credits}`, `{xp}`. A find item reads

> We have gained knowledge of a serious threat to the environment. According to our sources,
> a **{item}** found in **{place}** in **{playfield}** is leaking heavily. If you go there and
> pick it up, your sub-space containment field (in your inventory) should hold it long enough
> for you to destroy it.

and a repair

> Every year mutants crawl out of their hideout in **{playfield}**. Someone had grown tired of
> the situation and would like you to go to **{place}** and take care of the problem. Add some
> good old **{item}** to the **{fixture}** to make them docile enough through this breeding
> season, and get out of there as soon as possible before 48 hours.

Worth knowing while reading it: the objective's name is **in the prose and nowhere else** for
a team member. The quest record a mission holder gets names the target outright, but the copy
sent to the rest of the team carries the type and the building and no target, so a client
that wants to know what to kill has to read the sentence. That is how the bot does it.

This is what 105 offers contain and no more. Other bodies certainly exist - only seven repair
offers were captured, against fifty return item - and a slot filled the same way in every
captured copy may still be a slot.

## Building one

`MissionFactory` puts it together: `MissionBuilder` lays the rooms out and the factory
furnishes and populates them, all from the same pack. `MissionGen` builds 3,000 - 300 a pool -
and measures them against the 276 recorded ones.

| | ours | retail |
|---|---|---|
| rooms per building | 17.0 | 17.5 |
| bounding box, slots | 6.6 x 6.8 | about 8 x 9 |
| chests per building | 9.4 | about 9.5, range 1 to 22 |
| monsters per building | 16.9 | 11 to 69 |
| monster level against the QL | -4 to +3 | -4 to +3 |
| ways out per building | 2.50 | 1.3 |

All 3,000 pass the checks every captured building satisfies: no two rooms flooring the same
cell away from their edges, never three rooms at one socket, nothing off the grid, a door per
socket position.

Two things are still off, and both are known rather than mysterious.

**Ways out, 2.50 against 1.3.** These are sockets the capping pass could not fill, so they
stay doors onto nothing - retail has 0.3 a building and we have 1.5. Two attempts at it have failed and both are
worth knowing. Refusing any placement that opens a socket no dead end in the pool could serve
changed the figure by nothing at all, because every socket passes that test. Refusing any
placement that opens a socket no dead end could be hung on *as things stand* - the same test
with the collision included - made it worse, 2.89, because a placement refused is a growth
step that caps instead. So the cause is collision, and the fix is backtracking: undoing the
room that stranded the socket rather than being cleverer about which room to try next.

**Chests in rooms nobody has walked into.** 230 of the 639 rooms have a recorded spot. Using
only those gave a building 2.3 chests where retail gives it nine, so a room with no record
gets one on a floor cell instead - and that furniture is flagged `Approximate`, because it is
a guess. The flag stops being set as more runs fill the table in.

## What a server still cannot do

**The one that has not been tried.** None of this has been in front of a client. Every packet
is built by the server's own code and put through the real serializer, which catches a
message that cannot be written - but not a message the client does not like. The one thing
that could stop the whole path is the same thing that stopped Arete Landing being instanced:
its entry in `Playfields.xml` says the client would not leave the loading screen when it was
told an instance the login half had never mentioned, and a mission is always such an
instance. Retail plainly does it, so it is a question of what else the login half has to say,
and finding out wants a client rather than more reading.

**Chests do not open and the objective of a repair mission does nothing.** They are sent so
the building looks like a building. A chest with loot in it is the loot system's shape rather
than the mission system's.

The rest, in the order they would be wanted:

1. **Choose the types.** The dimensions decide it and the function is unmapped. What changed
   on 2026-09-26 is the price of the evidence: a terminal rolled with the sliders untouched
   sends six zero bytes, and the server then picks the six dimensions itself and tells you
   which it picked. So every such roll is a labelled sample, and a capture of a few hundred
   of them is the whole dataset. `MissionRolls.exe` reads them out. Until then the roller
   answers the ten measured settings from a table and draws at random elsewhere.
2. **Lay out a building.** No longer a blank - see the section above, which reconstructs the
   algorithm from 276 real layouts. What is left is writing it, and one open choice inside it:
   which room to pick for a given socket. Nothing here is waiting on data.
3. **Fill the rooms.** Doors are solved - the sockets are in the pack and the placement
   reproduces 451 real ones exactly. For the rest, the section above says how many of each a
   building gets, which templates they are and what a lock reads. Where inside a room they
   stand turns out to be a short authored list per room template rather than a rule, so what
   is missing is the list itself for the other 559 rooms - which is more recorded runs, not
   more analysis.
4. **Populate it.** Counts, spacing, floor, level and now the creature table itself are all
   measured, and the table is in the pack: 113 creatures over 38 bodies and the six pools the
   bot has run, each with the levels it appeared at relative to the mission QL - the whole
   table spans -4 to +3. `Tools/Capture/MissionMobs.tsv` is the readable form. What is missing
   is the four pools nobody has run, and how the boss is chosen, and both want runs rather
   than analysis.

   One trap worth knowing if you extend it: a recording starts at the *previous* zone-in, so
   the creatures walked past on the way are in the stream. Testing each against the building's
   own room list cuts them - 208 of them, and with them every absurd level.
5. **Write the text.** Done to the extent 105 captured offers allow - see the section above.
   What is missing is coverage: more bodies exist, especially for the types the captures are
   thin on.

6. **Hand out and honour the key.** New on 2026-09-26 and mostly settled: accepting gives an
   identity type 51053 item naming the building, entering is the plain teleport pair with no
   use of the key on the door, and finishing destroys the key. What is not known is what the
   entrance door actually checks - the wire shows a character holding a duplicate walking in,
   so it is not ownership, but nothing captured shows the check failing.

7. **Hand out a reward item.** Every captured offer carries one - a template pair at the
   mission's quality - and ours carries none, because nothing says how retail picks it and an
   item picked at random out of the pack would be a door as often as a weapon. Credits and
   experience are paid.

8. **Say what the reward is worth.** 25 captured offers at four qualities are all there is,
   and credits scatter from 2,500 to 13,300 with no shape - which is what a slider trading
   credits against experience would do. Experience is interpolated between the measured
   qualities and credits are a spread around the measured mean. Neither is a formula anyone
   has read out of the client, and a mission above QL 53 is extrapolated past the evidence.

9. **Name a person.** A find person mission names a human NPC and nothing here has a list of
   them. The four out of the captures are crossed to make sixteen, which is a placeholder and
   the only content on the roll path not read from data. Retail's list is in the client's own
   resource database.

There is also a smaller one: seventeen of `QuestInfo`'s forty members are still unnamed, and
a server emitting an offer has to put something in all of them. The offers settle several of
them - `UnknownHash` is the four characters "MSRE" in all 25, `Unknown20` is 6, `Unknown26` is
1, and the quest action's version is the mission's type: 16 find person, 15 find item, 8
return item and repair, 1 kill person.
