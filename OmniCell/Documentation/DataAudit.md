# Client data audit — 18.8.50_EP1

Audit of what the Anarchy Online client archive contains versus what OmniCell
actually loads. Run against client **18.8.50_EP1**
(`cd_image/data/db/ResourceDatabase.idx` + 3 `.dat` volumes) on 2026-09-09.

This file is a record for later review. Items marked **FIXED** were changed;
everything else is a finding, not a change.

---

## 1. Nano programs were never in SQL at all — FIXED

The `itemnames` table contained **zero** nano rows. Not "some missing" — none,
ever, in any version:

| source | rows | `ItemType='Item'` | `ItemType='Nano'` |
|---|---|---|---|
| database (as found) | 119,117 | 119,117 | **0** |
| committed `itemnames.sql` (18.8.11) | 119,117 | 119,117 | **0** |
| extracted `itemnames.sql` (18.8.50) | 120,569 | 120,569 | **0** |

The cause was in `Tools/Algorithman/Extractor Serializer/NewParser.cs`.
`ParseNano` reads the nano's name out of the record at the same offset
`ParseItem` does, and then throws it away — the return value was never assigned:

```csharp
short nameLength = this.br.ReadInt16();
short descriptionLength = this.br.ReadInt16();
if (nameLength > 0)
{
    this.br.ReadString(nameLength);   // read, discarded
}
```

`ParseNano` also took a `string sqlFile` parameter that its body never used;
the caller passed `"temp.sql"` and then deleted that file. Both were vestigial.

`NanoFormula` has no `Name` field either, so the old `nanos.dat` extractor
cache did not carry names and they could not be recovered without re-reading
the archive.

**Fixed:** `ParseNano` now captures the name and appends a row in the same
four-column shape `ParseItem` uses, tagged `ItemType='Nano'`, with the icon from
stat 79. The unused `sqlFile` parameter is replaced by the shared name list and
the `temp.sql` delete is gone. This required re-running extraction; nano names
cannot be backfilled from existing `.dat` files.

---

## 2. Item names are 1,452 entries behind the client

Comparing the committed 18.8.11 `itemnames.sql` against a fresh 18.8.50 extract:

| | count |
|---|---|
| added | **1,452** |
| removed | 0 |
| renamed | 32 |
| icon changed only | 1 |

Purely additive, so applying the new file cannot orphan existing references.
New content is mostly the Zenith Commando armour set, Temple of Three Winds
raid-boss weapon entries, and assorted playfield items. The 32 renames are
almost all Funcom adding an `Ammo:` prefix (`Bullets` -> `Ammo: Bullets`).

---

## 3. The emulator reads 7 of the 50 record types in the archive

Full census of the client RDB:

```
types mapped by the emulator : 7 of 50
records in mapped types      : 142,270
records in unmapped types    : 317,119
total records in RDB         : 459,389
```

**69% of the archive is never read.** Mapped types are Item, Nano, Playfield,
Door, Wall, Statel and Icon (`Extractor.RecordType`).

Largest unmapped types, by record count:

| type (dec) | type (hex) | records | notes |
|---|---|---|---|
| 1000013 | 0xF424D | 237,816 | largest single type in the archive, unidentified |
| 1000046 | 0xF426E | 14,274 | unidentified |
| 1010004 | 0xF6954 | 13,007 | unidentified |
| 1010017 | 0xF6961 | 12,375 | unidentified |
| 1010016 | 0xF6960 | 12,137 | unidentified |
| 1010001 | 0xF6951 | 11,190 | unidentified |
| 1010003 | 0xF6953 | 3,318 | unidentified |
| **1000036** | **0xF4264** | **2,063** | **Perks** — identified by the original authors |
| 1040023 | 0xFDE97 | 1,334 | unidentified |
| 1000047 | 0xF426F | 946 | unidentified |

Perks are the one unmapped type whose meaning is known: `Program.cs` carries
commented-out lines reading `// Perks` against `0xF4264`. Perks are a real game
feature and are entirely absent from the server.

The same comments reference `0xF4266 // Nano Strains`, but that type does not
exist in 18.8.50 — the census jumps from 1000037 to 1000039. Nano strain is a
stat on the nano record in this client version, which is what
`NanoFormula.NanoStrain()` reads. Nothing missing there.

---

### Attempt at identifying the unmapped types — inconclusive

I tried probing every unmapped type by applying the layout Item and Nano records
use (skip 16, attribute count, attribute pairs, skip 8, int16 name length, then
the name) to see which types carry readable names. It reported no names in any
unmapped type.

**That result is not usable.** Run as a control against Item and Nano, which
certainly do have names, the same probe also found nothing — so it was measuring
my incorrect reimplementation of the attribute-count read, not the data. The
unmapped types remain genuinely unidentified. Anyone repeating this should drive
it through the real `BufferedReader.Read3F1` rather than reimplementing it.

## 4. Most SQL tables cannot be regenerated from the client

The legacy extractor wrote five intermediate artifacts:

```
items.dat   nanos.dat   playfields.dat   itemnames.sql   itemrelations.txt
```

The three `.dat` object caches and `itemrelations.txt` are no longer runtime or
distribution files. They are one-time migration inputs kept outside the
repository. OmniCell now stores every record from those caches in its own
explicit, versioned content schema as `items.ocp`, `nanos.ocp` and
`playfields.ocp`; item and nano names remain a canonical SQL table.

Every other file in `SqlTables/` is hand-authored or community-sourced and has
no path back to the client archive:

`teleports` (1,244 rows), `tradeskill` (108,783), `shopinventorytemplates`
(2,076), `staticdynels` (294), `mobtemplate` (168), `vendortemplate` (145),
`proxydestinations` (125), `stats` (90), `vendors` (40), `mobdroptable` (25).

If any of that content is wrong or incomplete for 18.8.50, re-extracting will
not help — it has to be authored. This is the single most important thing to
understand about the data set: the client gives you items, nanos and world
geometry, and nothing else.

---

## 5. Smaller findings

**Four orphaned schema files.** `expansions.sql`, `itemspawn.sql`,
`playfields.sql` and `statnames.sql` exist under `SqlTables/` but are not listed
in `CellAO.Database.csproj`, never reach the build output, and no code
references those table names. They are dead files — either wire them up or
delete them.

**A misleading error message.** `ZoneEngine/Program.cs:577` prints
`"Error reading statels.dat"` from the catch block around
`PlayfieldLoader.CacheAllPlayfieldData()`. There is no `statels.dat` — statels
were embedded in the legacy `playfields.dat` cache and are now fields in
`playfields.ocp`. During bring-up
this message appeared when the real fault was a missing `teleports` table, which
sent the diagnosis in the wrong direction. Reword it.

**The extractor cannot run unattended.** After extraction it prompts
`[1,2]` to copy files into the source tree, then `[Y/N]` for icon extraction. A
run with only the archive path on stdin dies with a `NullReferenceException` at
`Program.cs:674` when `Console.ReadLine()` returns null — after doing all the
work. Feed it `<path>`, `2`, `N`, or give it a non-interactive mode.

**`itemnames.sql` is generated canonical content.** Generate it outside the
tree, review the conversion, then deliberately replace the tracked SQL table.

---

## 6. Open questions for review

1. **Perks** (2,063 records) are extractable and entirely unimplemented. Worth
   deciding whether they are in scope.
2. **Record type 1000013** holds 237,816 records — more than items and nanos
   combined. Identifying it would be the single largest gain in understanding
   the archive.
3. The **hand-authored tables** are all from the 18.8.11 era at best. There is
   no way to validate them against 18.8.50 automatically.

---

## 7. Outcome

Re-extraction with the ParseNano fix, against 18.8.50_EP1, exited cleanly with an
empty error log:

| | rows |
|---|---|
| `ItemType='Item'` | 120,569 |
| `ItemType='Nano'` | **10,815** |
| total | **131,384** |

Applied to a development database in 1,316 statements with zero failures. The database went
from 119,117 rows (all items, no nanos) to 131,384. ZoneEngine confirms it:

```
Cached 131384 item names        (previously 119117)
```

Nano names resolve correctly - `Death's Gaze`, `Enhanced Senses`, `Eagle Eye` -
with their icon ids.

The source caches were updated from 18.8.11 to 18.8.50 and then converted, so a
fresh build matches the database rather than silently reverting to the older set:

```
items.ocp                    2,667,900
nanos.ocp                      442,807
playfields.ocp                 546,285
itemnames.sql   8,446,996 -> 9,163,392
```

The migration round-trip verified all 120,569 item records, 10,815 nano
records, and 616 playfields before the source caches were removed from the
repository.

---

## 8. Perks — record format decoded

Perk records are type **0xF4264**, 2,063 of them, and every one is exactly
**24 bytes**: six little-endian int32s, no strings.

```
recId :  f0        f1        f2   f3       f4      f5
  100 :  210830    0         0    128      0       0     <- start of a line
  101 :  210831    100       0    128      0       0
  ...
  109 :  210839    108       0    128      0       0     <- tenth level
  110 :  211655    0         0    32852    0       0     <- next line
```

| field | meaning |
|---|---|
| f0 | item id holding the perk's name and stats |
| f1 | back-pointer to the previous level; 0 marks the first level of a line |
| f3 | bitmask, 44 distinct values dominated by powers of two (32768, 16384, 512, 256, 128, 32) - almost certainly the profession/breed requirement |
| f2, f4, f5 | populated but unidentified (1,410 / 347 / 1,044 records non-zero) |

Following f1 gives the perk lines:

```
265 chain heads
176 lines of 10 levels     <- the standard AO perk line
 38 lines of 1 level       <- probably AI or special perks
 remainder at 3-8 levels
2,048 of 2,063 records accounted for; 15 unexplained
```

**Names come free.** f0 points at ordinary item records, which we already
extract, so 264 of the 265 lines resolve against `itemnames` today - Accumulator,
Acrobat, Alchemist, Ambidextrous, Assassin's Awareness, Atrox Primary Genome,
Bio Shielding, Bureaucratic Shuffle and so on. One head does not resolve and is
worth a look.

### What this means

Extracting perks is now a **small, well-understood change**: read a fixed 24-byte
struct, follow f1 to build the lines, and join f0 to data already in hand. It is
not a reverse-engineering project. Note that `ParseItem` cannot be reused - it
fails on all perk records, because this is a different layout entirely.

### The actual blocker is the protocol, not the data

`PerkUpdateMessage` (N3MessageType 0x435f7023) has three fields and all three are
called `Unknown1`, `Unknown2`, `Unknown3`. Nothing in the codebase ever
constructs or sends one. By comparison `CharacterActionMessage`, which works, has
seven named members.

So perk data can be extracted and stored, but there is currently no way to tell
the client about it. Anyone picking this up should sort the wire format out
first, because that determines what the data model needs to hold - doing it in
the other order risks building a schema that cannot be delivered.

Also missing on the server side: no perk table, no DAO, no handler, and
`CellAO.Core` has zero perk references. What does exist is enum scaffolding -
`FunctionType.LockPerk` / `ResetAllPerks`, `Operator.HasPerk` / `HasNotPerk` /
`IsPerkLocked` / `IsPerkUnlocked`, and the `LastPerkResetTime` stat (577).
