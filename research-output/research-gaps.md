# Item research gaps

Updated: 2026-09-13

This file tracks evidence that is still missing. It is deliberately separate
from the generated unresolved-key list so conclusions and capture work are not
lost when that list is rebuilt.

## Missing key-to-output evidence

The current dataset contains 2,199 unresolved four-character `SpawnItem` keys.
For each of these keys, the missing fact is the item line returned by the live
server. The client record contains the lookup key and requested QL, but no
verified output template ID.

We do **not** need to manually use 2,199 items on retail. A useful capture or
external mapping source should be processed in bulk by correlating:

1. the source item and its `SpawnItem` key;
2. the requested QL;
3. the resulting inventory item/template ID and QL; and
4. the packet/capture filename that proves the relationship.

Each verified relationship should be recorded as:

| Key | Source template | Requested QL | Output template | Output QL | Evidence | Status |
|---|---:|---:|---:|---:|---|---|
|  |  |  |  |  |  |  |

## Arete profession packages

The 15 unconditional package calls have strong client-data candidates in
[`arete-package-key-research.md`](arete-package-key-research.md). They still
need capture-backed confirmation before being treated as proven mappings.

Five guarded calls have a likely normal nano crystal and a damaged alternative
that uploads the same formula. The expected result is the normal crystal, but
the exact key-to-template relationship is not yet backed by a retained capture:

| Key | Expected normal item | Alternative | Missing evidence |
|---|---:|---:|---|
| `FFXV` | 46456 | 220866 | Package-open inventory result |
| `MIFS` | 28943 | 221275 | Package-open inventory result |
| `NTCB` | 210529 | 222559 | Package-open inventory result |
| `RLQU` | 56238 | 221819 | Package-open inventory result |
| `STVK` | 70401 | 221888 | Package-open inventory result |

## Evidence handling still needed

- Keep raw captures outside Git; commit the small extracted tables and research
  conclusions that can be reproduced from them.
- Add the capture filename or stable evidence reference to every accepted
  mapping so an inference is never mistaken for an observed retail result.
- Rebuild `unresolved-spawn-keys.csv`, `.md`, and `.xlsx` together after proven
  mappings are added, then update the total recorded above.
