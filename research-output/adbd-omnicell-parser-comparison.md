# ADBD versus OmniCell item-record extraction

Compared 2026-09-13 against `bitnykk/adbd` commit
`62e5fd6ae0b39a586f333606179a1e5a30936a3c`.

## Result

ADBD is a raw record dumper, not the parser that produces Auno's web fields. It
opens the AO C-Tree database, walks item/nano records, and writes each complete
record as:

```text
AOID: <record id> LEN: <byte count>
==========
<raw record bytes>
==========
```

It also exports icon and texture payloads. It does not decode names, stats,
events, functions, requirements, relations, or SpawnItem results. Auno's
server-side importer, which is not in the ADBD repository, interprets the raw
item and nano records afterward.

Consequently ADBD contains no separate key-to-item-line table and cannot turn
`UWL1` into an output template ID. Its advantage is that it preserves every raw
byte for later interpretation.

## Field comparison

| Data | ADBD | Current OmniCell model/parser |
|---|---|---|
| Record ID and complete raw blob | Preserved | Complete record is walked; decoded fields and formerly omitted blocks are stored, but not as one opaque blob |
| Item/nano name | Raw bytes only | Decoded into `itemnames.sql` |
| Description | Raw bytes only | Decoded and preserved in `RecordData` |
| Quality and stats | Raw bytes only | Decoded |
| Attack/defense | Raw bytes only | Groups 12 and 13 decoded; every raw group is preserved |
| Events and functions | Raw bytes only | Decoded |
| Function target | Raw bytes only | Decoded |
| Requirement triples | Raw bytes only | Decoded into requirements; numeric values survive even when enum names are unknown |
| Function arguments | Raw bytes only | Known argument fields decoded according to `FunctionSets.cfg` |
| Function bytes marked `x` | Preserved | Preserved in `FunctionRecordData.Arguments` while known fields are decoded |
| `SpawnItem` layout | Preserves hash, two integers, and 20 trailing bytes | Decodes `1h,2n` and preserves the complete argument bytes, including `20x` |
| Action requirements | Raw bytes only | Cooked requirements decoded and original triples preserved |
| Animation/sound blocks 14 and 20 | Preserved | Fully preserved in `RecordData` |
| Block 6 and unmodeled body blocks | Preserved | Block 6 is preserved; an unknown block now fails with record ID, offset, and nearby bytes instead of silently stopping |
| Shop block entries | Preserved | Existing shop-hash representation retained and exact entry bytes preserved |
| Icons/textures | Exported as files | Icons can be read separately; not part of item templates |
| Item-line relations | Not interpreted | Added separately from `itemrelations.txt`, not derived by ADBD |
| SpawnItem key to output item line | Not present | Not present unless recorded/inferred separately |

`NewParser` now populates `RecordData` and `FunctionRecordData`. Content-pack
version 3 also preserves a function encountered directly in the body without
inventing an event type. Versions 1 and 2 remain readable.

## `Spoils of War` sample

Auno's display for item 304954 and OmniCell's converted record agree on all
displayed fields:

- event: `OnEnter`
- target: `User` / numeric target 2
- function 53064: `SpawnItem [UWL1, 1, 0]`
- four requirements, including operator 117 with value 1312965191
- companion function 53226: `SpawnQuest [NBBG, 6, 0]`

Decimal 1312965191 is hexadecimal `4E424247`; interpreted in the record's hash
byte order it is `NBBG`. It is the companion quest key used by the requirement
and `SpawnQuest`, not the output item ID behind `UWL1`.

The five-record family is:

| Source item | SpawnItem key | SpawnQuest key |
|---:|---|---|
| 304953 | `ME7A` | `1W6C` |
| 304954 | `UWL1` | `NBBG` |
| 304955 | `V5EF` | `1IKQ` |
| 304956 | `0HUG` | `77ND` |
| 304957 | `6755` | `7LHT` |

## What the comparison changes

Dumping Auno pages would not add the missing SpawnItem mappings. The useful
technical follow-up was to finish the complete-record walk in `NewParser` and
preserve the `x` bytes and descriptions. That is now complete and verified
against all 120,842 items and 10,965 nanos in client 18.8.62_EP1. The next step
is to analyze the preserved bytes across captured known mappings. This must
be treated as a possible new evidence source, not assumed to contain output IDs:
Auno itself receives the same raw records yet still displays `UWL1` rather than
an item template.

No ADBD executable was run. OmniCell's newly added noninteractive parser
verification mode was run against the current client database; it does not
write extraction output or enter the interactive workflow.
