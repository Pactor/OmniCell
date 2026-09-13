# Arete Landing Quest Readiness Milestones

This document is the completion contract for the Arete Landing proof of concept.
The work is not "ready" merely because it compiles or because database rows exist.
It is ready when every required scenario below has repeatable evidence and no
unsupported gameplay rule is being presented as captured behavior.

## Evidence policy

Allowed sources, in descending order of authority:

1. Decoded local packet captures and capture-derived records.
2. Emulator-owned SQL transcribed from those records.
3. Existing generic OmniCell protocol and server behavior.
4. A clearly labelled OmniCell design decision requested by the project owner.

Community guides may help locate evidence but do not establish an exact packet,
identifier, quantity, or probability. Unknown values remain documented as unknown.
New emulator-owned values must be labelled as such. Cleaning Robot and
Malfunctioning Cleaning Robot must never be treated as Robot Junk sources.

## Definition of ready

- A new character can enter Arete Landing and complete the supported quest chain
  through ordinary client interactions.
- Quest offers, journal updates, objectives, transitions, item hand-ins, rewards,
  and failure handling survive reconnects without duplication or loss.
- A GM can create an NPC, select its model, define dialogue, offer a quest, define
  objectives and rewards, and configure an item hand-in without changing C#.
- Item hand-ins open the client trade box with the required number of slots and
  complete only after the exact required items are supplied.
- Robot Junk is obtained from ordinary full-bodied `32-V Docker` corpses at the
  explicitly emulator-owned rate, never from either cleaning-robot variant.
- Raw client extraction files are absent from the repository, including untracked
  files, while all required converted representations remain available.
- The solution and relevant tests pass from a clean database setup.

## Milestones

### M0 — Evidence and repository hygiene

- [x] Raw `items.dat`, `nanos.dat`, and `playfield.dat` removed from the repository.
- [x] No raw client extraction file or extractor cache is tracked, and
      `.gitignore` keeps them out (`ResourceDatabase.*`,
      `OmniCell/Datafiles/*.dat`, `itemrelations.txt`).
- [ ] Every Arete-specific gameplay row has a traceable capture source or an
      explicit `OmniCell-defined` annotation.
- [ ] No obsolete or contradictory Arete patch remains loadable.

### M1 — Reproducible database installation

- [x] A clean database import loads every table and ordered Arete patch without an
      SQL error.
- [ ] Imported counts and referential checks are recorded by an automated verifier.
      The earlier verifier scripts were removed from the shared repository.
- [x] Loot, quest, dialogue, tradeskill, spawn, vendor, and corpse rows all resolve
      their referenced records.

### M2 — Generic quest state engine

- [x] Accepting a quest creates exactly one active persisted state.
- [x] Re-accept, abandon, reconnect, completion, and reward are idempotent.
- [x] Prerequisite chains cannot be skipped and completed quests cannot be farmed.
- [x] No Arete quest or item ID is hard-coded in the generic C# state engine.

### M3 — Objective event coverage

- [x] Kill, talk, use-target, use-location, acquire, purchase, tradeskill, and
      hand-in objectives advance only from their corresponding server event.
- [ ] Required counts, target identity, order, and quest transitions are verified.
- [x] Unsupported objective shapes fail closed and are reported clearly.

### M4 — Dialogue and GM authoring

- [ ] Captured Arete dialogue branches retain their text and actions.
- [x] Offers and turn-ins appear only in valid quest states.
- [x] GM commands can create/edit/remove NPC models, dialogue branches, quest
      offers, objectives, hand-ins, and rewards without C# changes.
- [x] GM-authored content survives restart and has validation/error messages.

### M5 — Hand-ins, rewards, and tradeskills

- [ ] The client receives the correct trade-box slot count for each hand-in.
- [x] Wrong, partial, duplicate, and excess items do not complete a hand-in.
- [x] Accepted items are consumed exactly once; rewards are granted exactly once.
- [ ] Arete tradeskill recipes consume the captured inputs and create the captured
      outputs at the captured QL behavior.

### M6 — Corpse loot

- [x] Loot profiles match an exact mob name rather than a shared classification.
- [x] `32-V Docker` has a QL 1 Robot Junk rule at a labelled OmniCell 25% rate.
- [x] Cleaning Robot, Malfunctioning Cleaning Robot, and the named Docker boss are
      excluded from that rule.
- [ ] Corpse opening, item transfer, emptying, expiry, and reconnect behavior are
      covered by repeatable tests. The lifecycle tests that covered them were
      removed from the shared repository.

### M7 — Client protocol and mission journal

- [x] Quest offer/accept packets use the exact capture-derived per-quest fields and
      pass the production serializer/deserializer contract.
- [ ] Equipped weapons are visible and ordinary attacks animate in the client for
      each represented weapon placement/type.
- [ ] Nano casting animates in the client for represented self-target and
      other-target formulas.
- [ ] Objective progress and mission-journal updates are visible after every event.
- [ ] Completed/failed/abandoned states and reconnect reconstruction are correct.
- [ ] No on-screen client error occurs in the full supported path.

### M8 — Arete scenario matrix

- [ ] Every imported Arete quest has a source-backed accept path; the current
      reachability layer includes explicitly OmniCell-authored fallback answers
      where the captured dialogue walk is incomplete.
- [x] Every objective resolves to a runtime actor, fixture, item, or hand-in path.
- [x] Every hand-in resolves its captured recipient, item IDs, quantities, and slots.
- [x] Every reward and next-quest transition resolves its converted content.
- [ ] The main supported chain is completed end-to-end on a fresh character.

### M9 — Release gate

- [x] No raw client extraction file is tracked in the repository.
- [ ] A clean database import is verified automatically.
- [x] Release solution build passes.
- [x] Protocol tests pass (`python Tools\RunTests.py`).
- [ ] Automated quest tests exist and pass.
- [ ] An end-to-end client run passes with logs retained as evidence.
- [x] Setup documentation contains exact install, GM-authoring, and known-limit
      instructions.

## Current blocking evidence gaps

- The captures establish that Robot Junk is a chance drop but do not expose the
  original server probability; 25% is intentionally an OmniCell-defined rate.
- Runtime reachability currently uses clearly labelled OmniCell fallback answers
  for capture gaps. A 38/38 audit therefore proves that no quest is mechanically
  stranded, but does not by itself prove every offer sentence is captured text.
- The captures identify which dialogue sentence the client selected before 21
  quest-log transitions. They do not identify whether each transition was an
  accept, a preceding-quest hand-in, or another state change. Four candidate
  accept mappings collided with captured item-hand-in actions during clean-database
  verification, so no accept action is inferred from sequence alone.
- Database consistency and state-machine checks are not a substitute for an actual
  client end-to-end run. M7 and M8 remain open until that run is recorded.
- The production full-update packet now uses the normalized SQL-backed values for
  all 38 captured Arete quests and passes exact materialization and serializer
  preflight. Client use remains gated on the remaining non-graphical checks and a
  controlled end-to-end run; journal progress presentation cannot be proven by
  packet/database tests alone.
- The production lifecycle integration now exercises database-backed reload and
  exact-once cash/XP payout. Item-reward reconnect reconstruction and client-side
  journal reconstruction remain open under M5/M7.
- Hand-in matching, partial/excess handling, persisted consumption, item-reward
  reload, and duplicate payout are now covered by production integration. The
  graphical client still must prove its trade-window slot presentation and the
  disconnect path during a live exchange.
- Captured mission text and wire state prove the 1/4 Screwdriver + Robot Junk
  combine and the Hacking Skills combine, including Screwdriver QL 10 and the
  QL 1 hacking inputs/result. The available captures do not contain the 2/4 or
  3/4 mission stages/builds, so those two configured intermediate formulas remain
  explicitly unverified even though their production receiver paths now pass.
- The capture-derived quest text requires extinguishing one of the four spawned
  Gas Fires. The exact four captured fixture identities and positions are enforced
  by the clean-database verifier; the graphical client still needs to confirm all
  four are visible and individually targetable.
