# Leaving Arete Landing

Nobody could get out of the tutorial playfield, which meant nobody could reach a mission
terminal - there are 407 of them in the playfield pack and none is in Arete. This is what the
way out is made of, which of it was missing, and what is still in the way.

## The way out, as the client data has it

Three statels in playfield 6553, and they run in this order.

**1. "Exit Arete Landing"**, template 297303. Using it does three things, each with its own
requirement:

```
OnUse: CastNano(295602)                             [Self stat 685 BitAnd 16384]
       Teleport(3364, 18, 835, 0)                   [Self stat 685 BitAnd 16384]
       SystemText("You need an ID card to enter ICC HQ.")
                                                    [Self stat 685 NotBitAnd 16384]
```

So bit 16384 of stat 685 is the ID card. With it you are given nano 295602 and moved to 3364,
18, 835; without it you are told to come back with one.

**2. The door**, template 41565, standing at 3362.9, 17.8, 834.9 - which is where that
teleport puts you, within a metre.

```
OnTargetInVicinity: 53142(3337, 38, 866, 655)
                        [Self stat 685 BitAnd 16384, And Self HasRunningNano 295602]
```

**3. Rubi-Ka.** Playfield 655, at 3337, 38, 866. That is in among 655's own doors - one of
them stands at 3365, 18, 835 and teleports to 3351, 36, 866, fourteen metres from where
Arete's door sends you.

## What was missing

**Function 53142 had no name and nothing ran it.** It is a teleport to another playfield,
taking the same four arguments `Teleport` does. It is named `TeleportToPlayfield` now, and
the name is ours - read off what it is called with, not off a client string. The evidence is
the argument shape, all four in all thirteen uses, and the destination landing among 655's
doors.

The other twelve uses are "Exit the Grid" and a repeating animator script and every one reads
`(0, 65000, 0, 0)`. A playfield of zero means "this one", which would put a player at an
altitude of 65,000 where they stand, so those are refused rather than run. Whatever the
Grid's exit does, it is not this.

**`Operator.HasRunningNano` threw.** `RequirementLambdaCreator` had no case for it and
`CheckRequirement` catches the exception and returns false, so the requirement did not fail
loudly - it just never held. It asks whether a nano is running on the character now, which is
`ActiveNanos`, and is not the question `HasNotFormula` asks - that one is about what has been
uploaded. 315 more requirements across the pack use the negative of it, `HasNotRunningNano`,
which is still unimplemented.

## The card is what the quests are for

Every stage of the Arete chain says so in its own description - "In order to leave Arete
Landing and become a citizen of Rubi-Ka, you need an identity. Your mission is to create a
fake ID Card so you can leave this place" - and the parts are handed over one at a time:
an Unprogrammed Identification Chip, Biological Survey Nanobots, a Blank ICC ID Chip, a
Personalized ICC ID Chip, and at last item 296692, the Identification Card itself. The last
stage is "Talk to Vaughn Hammond", whose text is "Your ID card is finally complete! Talk to
Vaughn Hammond about leaving Arete Landing."

Three things were missing between that and the door.

**Stat 685 did not exist.** Not a gap in the data - the server had no such stat, and reading
a stat it does not have throws. `CheckRequirement` catches and returns false, so every
behaviour that asks about 685 quietly refused, and Arete's exit asks three times. It is on
the character now, as `ProgressFlags`, and persists like any other.

**A quest could not change a stat.** Cash, experience and items were all a quest could give.
`queststatrewards` is a new table - quest, stat, value, whether the value is bits to set or
the whole value, and whether it lands on accept or on completion - and `QuestManager` applies
it at both points. Bits rather than a write is the usual case, because 685 is a flag word: a
character out on Rubi-Ka carries eleven more bits in it and overwriting would take them away.

**Nothing granted the bit.** `SqlPatches/arete-landing-z-id-card.sql` now does, on finishing
1439635506, the last quest of the chain. That placement is **OmniCell-defined**: no capture
shows the moment retail grants it, because every capture we hold is of a character who had
already left. What the captures do agree on is the value - one recorded inside an Arete
instance reads 685 = 16384 exactly, and characters out on Rubi-Ka read 28668, which contains
it. Granting it one quest earlier, with the card item, would let the last stage be skipped.

A GM can still hand it over directly, which is the quick way to test the door without walking
the chain:

    /set 685 16384

## What else the world asks for and does not get

`PlayfieldDump.exe <playfields.ocp> 0 --missing` counts it. As of 2026-09-27:

| | uses | |
|---|---|---|
| function 53200 | 24 | only ever on "Exit the Grid", one argument, the Grid's playfield |
| `Operator.And` as a requirement's own operator | 828 | see below |
| operator 106 | 583 | unnamed in the enum |
| `HasNotRunningNano` | 315 | |
| `OnSecondaryItem` | 216 | |
| operator 121 | 124 | unnamed |
| `HasRunningNanoLine` | 72 | |
| operator 117 | 26 | unnamed |
| `Operator.Or` as a requirement's own operator | 3 | |

The And and Or rows are worth a second look before anybody implements them. A requirement
list is joined by each entry's **ChildOperator**, which `CheckAll` already does; an entry
whose own **Operator** is And or Or is something else - a grouping row, most likely - and
today it evaluates false and drags the whole list down with it. That is 828 behaviours that
may never run. Nothing here changes it, because getting the grouping wrong would make things
happen that should not, which is worse than a door that does nothing.

## Reading a playfield without a server

`Tools/Capture/PlayfieldDump.exe` is what the above was found with. It reads the content pack
and a copy of `itemnames.sql`, so it answers "what is standing in here and what does it do"
without a database or a running engine.

    PlayfieldDump <playfields.ocp> <playfield> [--names itemnames.sql]
    PlayfieldDump <playfields.ocp> <playfield> --find Door --names itemnames.sql
    PlayfieldDump <playfields.ocp> <playfield> --near <x> <z> <radius> --names itemnames.sql
    PlayfieldDump <playfields.ocp> 0 --function <id> --names itemnames.sql
    PlayfieldDump <playfields.ocp> 0 --missing
