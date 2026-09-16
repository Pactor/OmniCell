# Implants - milestones

What the server does with implants, what is left, and the evidence behind each
item. Sources are the Arete Landing implant session
(`20260909-142713`, stream s3), the two Shade sessions (`20260914-220505`,
`20260915-042412`), and the client's own data (`items.ocp`, `playfields.ocp`,
the `tradeskill` table).

## Done

- [x] **Implant slots.** An implant or spirit goes only in a slot its Placement
      (stat 298) names: slot 33 + n - 1 is bit n, eye 1 to feet 13. Every item
      retail was seen equipping fits (legs 2048 in 43, chest 32 in 37, right arm
      16 in 36, waist 256 in 40, feet 8192 in 45). Two-slot implants fit either.
- [x] **Requirements.** Wear, wield and use requirements are read left to right
      with each entry's own And/Or; only the And entries used to count. Swapping
      into an occupied slot checked the wrong page's requirements, so it checked
      none.
- [x] **Equip finishes a quest stage.** Retail completes "Install the implant"
      when the implant lands in its slot (s3 8042-8043), and the Shade's "become
      its vessel" the same way (220505 26820-26823). QuestExtract now writes an
      Equip objective for a stage that finishes on a TemplateAction onto an
      equipment page.
- [x] **Surgery Clinic.** Using one runs the clinic statel's own OnUse - 5
      credits at Arete Landing, 300 at the city clinic (template 43553), the
      "5 minutes (or until you leave the playfield)" text, and nano 157490 with
      its Treatment +100 - and opens a five minute window. An implant moves into
      or out of an implant slot only while that window is open. Spirits are not
      gated: both Shade sessions put them in with no clinic in sight.
- [x] **Shades.** A Shade (profession 15) wears spirits (item class 5) and no
      implant (class 3); nobody else wears a spirit. 842 of the 844 spirits say
      so in their own ToWear data, no implant says anything about Shades, and
      the implant trainer tells a Shade "You know your kind can't use implants,
      right?" (220505 24538).
- [x] **CastNano** is implemented, so any item or fixture whose data casts a
      nano now does.

## Done (continued)

- [x] **Cluster removal.** It is not a hand-written feature: the `tradeskill`
      table already holds 11,007 rows for it, all with source 161867 (Implant
      Disassembly Clinic), QL range 10%, delete flag 2 (the implant is consumed,
      the tool is kept), skills 165 Breaking and Entering at 425% and 160
      Nanoprogramming at 100% of the implant's QL, MaxBump 0. The result is the
      basic implant the clusters went into. Retail guides agree: all clusters
      are destroyed, the implant returns to basic, and the tool must be at least
      90% of the implant's QL.
      The path already existed (`TradeSkillReceiver`); the work was making it
      behave, and these are fixed:
  - [x] The result quality came from the client, capped only by the result
        template's own maximum, so a QL 10 implant could be handed to the clinic
        and asked for a QL 200 Basic Implant. It now follows the target, raised
        only by the builder's own skill.
  - [x] `MinTargetQL` was tested inverted against the column's meaning
        ("the item must be at least this QL", 0 = any).
  - [x] `leastbump` kept the last skill's value instead of the lowest across
        skills, and could go negative.
  - [x] The recipe's own `MaxBump` was ignored; only the implant QL tiers were
        used, and only when `IsImplant` is set.
  - [x] `CalculateXP` divided two ints before multiplying, so most recipes paid
        `MinXP` flat.
  - [x] The quality offered in the window and the quality built now come from
        the same calculation, so what is offered is what is made.

## Not started

- [x] **The Shade questline** (`SqlPatches/arete-landing-shade-quests.sql`).
      Generated with QuestExtract from the Shade session (37 stages, 40
      conversations; the capture's playfield is instance 2150461, written as
      6553 with `--as 6553`), then trimmed by hand: the generator rewrites a
      whole playfield, and only Lady Sheila Black's four stages and her
      conversation (36 dialogue rows, 6 openers) are new.
  - [x] The chain forks at "Return to Vernon Godfray" (1439635802): Dr. Mason
        and the implant chain for everyone else, Lady Sheila Black for a Shade.
        The Shade session was granted no Dr. Mason stage and the implant session
        never met Sheila Black. `quests.RequiresProfession` (0 anyone, 15 Shade
        only, -15 everyone but Shades) keeps each branch to its own, checked
        where a quest is accepted, so a transition to the other branch is passed
        over. Emulator-owned, like `Requires`.
  - [x] Stages: Talk to Lady Sheila Black (dialogue answer) -> Give a Soul
        Capsule (hand-in; the Spirit Siphon 297333 is given on accepting, the
        Comfortless Spirit of Defense 295715 on finishing) -> Become a Vessel for
        the Spirit (Equip 295715, the equip trigger added for implants) -> Talk
        to Lady Sheila Black (hand-in; 1400 credits, 2596 xp, Personalized ICC ID
        Chip 296576).
  - [x] Applied to the local database and re-applied to prove the patch loads
        twice cleanly. The column is added by a guarded ALTER in the patch and is
        in `SqlTables/quests.sql` for a fresh database.
- [ ] **Shade questline, what the captures do not show:**
  - [ ] Soul Capsule drops. The stage text asks for one (236635/238946), but
        no capture shows a capsule dropping or its id; every spirit seen came
        from using an item that turned into a spirit. The chain does not wait
        on it: the captured hand-in objective is the Spirit Siphon (297333),
        which the stage gives on accepting. Every stage was checked to be
        finishable with what the chain itself hands over - her dialogue answer
        is in her conversation, the siphon comes with stage 2, the spirit with
        finishing stage 2, and the ID chip (296575) with "Return to Vernon
        Godfray".
  - [ ] The final hand-in: Sheila asks for a Blank ICC ID Chip and Biological
        Survey Nanobots, but only the chip (296575) came back as a captured
        hand-in objective, so only the chip is required.
  - [ ] Godfray tells a Shade about Sheila Black in his own line; that line is
        node 10 of his conversation, where everyone else's answer sits, and the
        dialogue tables cannot branch on profession. The quest branch is right;
        his Shade-only line is not reproduced.
  - [ ] The Spirit Siphon's effect (nano 301114 on a dying mob) and turning it
        into a capsule are not implemented.
- [ ] **Refusal messages.** No capture shows what retail says when a swap is
      refused (no clinic window, wrong slot, a Shade trying an implant, not
      enough credits at the clinic). Everything refused is silent for now.
- [ ] **Client statel ids.** The client names playfield-file statels by runtime
      ids (the clinic is Terminal:1477021820) that the data does not carry - it
      holds 0xC00E1999, index 14 of playfield 6553. The Surgery Clinic is found
      by looking for the nearest clinic statel within 8 metres instead. Every
      other client-owned statel (exits, shuttle doors) is unreachable for the
      same reason.
- [ ] **A capture session** covering: taking an implant and a spirit out,
      cluster removal at a Disassembly Clinic, and each refusal above.

## How to check a change without a server

Scratch console projects that reference `OmniCell.Core` and `ZoneEngine`, load
`items.ocp` / `playfields.ocp` directly and call the rules
(`ImplantInventoryPage.Fits`, `ProfessionMayWear`, `SurgeryClinic.FindNear`,
`QuestStateRules.TryAdvance`) need neither the database nor a running engine.
