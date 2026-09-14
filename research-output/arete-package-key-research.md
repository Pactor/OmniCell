# Arete profession-package SpawnItem keys

Research date: 2026-09-13

This is research evidence, not server configuration. None of these candidate rows has been added to `ItemSpawns.xml`.

## Result

The 20 priority keys now have concrete candidates. The 15 previously opaque, unconditional calls line up with the current-client `Supercharged <Profession> Nano Crystal` family. These are not ordinary single-formula crystals: using one uploads several profession formulas directly.

The remaining five calls are guarded by `HasNotFormula`. Each has one normal crystal and one damaged Shadowlands crystal that upload the same formula. The normal crystal is the coherent package result because the damaged alternative has a random failure condition, but a retail capture is still the final proof.

| Key | Package | Requested QL | Candidate item | Candidate name | Confidence | Reason |
|---|---|---:|---:|---|---|---|
| I4S6 | Adventurer (248255) | 1 | 304505 | Supercharged Adventurer Nano Crystal | strong inference | Unique QL 1 Supercharged item for this profession; uploads 9 formulas |
| BHZS | Agent (248256) | 1 | 304506 | Supercharged Agent Nano Crystal | strong inference | Unique QL 1 Supercharged item for this profession; uploads 5 formulas |
| 36WC | Bureaucrat (248257) | 1 | 304507 | Supercharged Bureaucrat Nano Crystal | strong inference | Unique QL 1 Supercharged item for this profession; uploads 11 formulas |
| VJ83 | Doctor (248258) | 1 | 304508 | Supercharged Doctor Nano Crystal | strong inference | Unique QL 1 Supercharged item for this profession; uploads 13 formulas |
| 9ATM | Enforcer (248259) | 1 | 304509 | Supercharged Enforcer Nano Crystal | strong inference | Unique QL 1 Supercharged item for this profession; uploads 6 formulas |
| K9UZ | Engineer (248260) | 1 | 304510 | Supercharged Engineer Nano Crystal | strong inference | Unique QL 1 Supercharged item for this profession; uploads 8 formulas |
| WK1Q | Fixer (248261) | 1 | 304511 | Supercharged Fixer Nano Crystal | strong inference | Unique QL 1 Supercharged item for this profession; uploads 7 formulas |
| FRG7 | Keeper (300892) | 1 | 304512 | Supercharged Keeper Nano Crystal | strong inference | Unique QL 1 Supercharged item for this profession; uploads 4 formulas |
| APJ5 | Martial Artist (248262) | 1 | 304513 | Supercharged Martial Artist Nano Crystal | strong inference | Unique QL 1 Supercharged item for this profession; uploads 6 formulas |
| EPHO | Meta-Physicist (248263) | 1 | 304514 | Supercharged Metaphysicist Nano Crystal | strong inference | Unique QL 1 Supercharged item for this profession; uploads 8 formulas |
| 834B | Nano-Technician (248264) | 1 | 304515 | Supercharged Nanotechnician Nano Crystal | strong inference | Unique QL 1 Supercharged item for this profession; uploads 16 formulas |
| UNWO | Nano-Technician (248264) | 200 | 303907 | Supercharged Nanotechnician Nano Crystal | strong inference | The same profession family has a unique QL 200 member; uploads 54 formulas |
| 887K | Shade (300893) | 1 | 304516 | Supercharged Shade Nano Crystal | strong inference | Unique QL 1 Supercharged item for this profession; uploads 4 formulas, including Spirit Siphon |
| 7K1M | Soldier (248265) | 1 | 304517 | Supercharged Soldier Nano Crystal | strong inference | Unique QL 1 Supercharged item for this profession; uploads 8 formulas |
| E9VW | Trader (248266) | 1 | 304518 | Supercharged Trader Nano Crystal | strong inference | Unique QL 1 Supercharged item for this profession; uploads 12 formulas |
| RLQU | Agent (248256) | 1 | 56238 | Nano Crystal (Detain Suspect) | likely; capture required | Guard is formula 56218. Alternate 221819 is a damaged crystal with a random-failure path |
| FFXV | Bureaucrat (248257) | 1 | 46456 | Nano Crystal (Basic Worker-Droid) | likely; capture required | Guard is formula 46397. Alternate 220866 is a damaged crystal with a random-failure path |
| MIFS | Martial Artist (248262) | 1 | 28943 | Nano Crystal (Iron Fist) | likely; capture required | Guard is formula 28892. Alternate 221275 is a damaged crystal with a random-failure path |
| NTCB | Keeper (300892) | 1 | 210529 | Nano Crystal (Adaptive Ambient Restoration) | likely; capture required | Guard is formula 210528. Alternate 222559 is a damaged crystal with a random-failure path |
| STVK | Soldier (248265) | 1 | 70401 | Nano Crystal (Partial Deflection Shield) | likely; capture required | Guard is formula 70320. Alternate 221888 is a damaged crystal with a random-failure path |

## Why the Supercharged family fits

- There are exactly 14 QL 1 items, one for every profession represented by the 14 Arete containers.
- Their AOIDs are a continuous profession-ordered range: 304505 through 304518.
- Every container has one unconditional QL 1 `SpawnItem` call; the Nano-Technician container additionally has an unconditional QL 200 call.
- The QL 200 member of the older Supercharged Nano-Technician family is item 303907, exactly matching the extra QL requested by `UNWO`.
- These items call `UploadNano` many times and do not contain further `SpawnItem` calls. This explains how a container with only two or three SpawnItem calls can provide a whole selection of nanos.

## Internet corroboration and limits

- Current AO database sites expose the four-letter calls but not the live server's key-to-item table. They independently show that web tools render the key bytes in reverse order (for example local `VJ83` appears as `38JV`).
- The current Arete guide confirms that profession containers release nanos, and specifically says a Shade may already receive Spirit Siphon from the purchased nano pack. The current QL 1 Supercharged Shade item uploads Spirit Siphon as part of its four-formula set.
- Historical ICC Shuttleport package-content lists predate Arete and are not used as proof for current mappings.

## Safest retail verification

One clean capture per profession is sufficient for the QL 1 rows. Use a fresh character that has not uploaded the guarded starter formula, record inventory immediately before and after opening the profession container, and keep the package template ID in the capture notes. Prioritize Nano-Technician because one opening tests `834B`, guarded `NAIF`, and the unusual QL 200 `UNWO` together.

