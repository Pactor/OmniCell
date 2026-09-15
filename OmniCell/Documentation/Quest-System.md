# Quest system - staged, data driven, extracted from captures

Status: design agreed with the project owner on 2026-09-14 ("set up quests correctly, so we do not have to
sniff each quest then fix it by hand"). Replaces the objective/hand-in model described in
`Arete-Quest-Milestones.md` M2-M5 and M8 where they conflict.

## Why the old model is wrong

Two dedicated retail sniffs of the first two Arete Landing quest chains (Rex Larsson, Marcus Stone -
`E:\Funcom\sniffs\20260914-120906_s6` and `20260914-124401_s4`, sequence details in the handoff) show
how quests really work, and the current engine does almost none of it:

| Retail (proven) | OmniCell today |
| --- | --- |
| Each stage is its own quest in the log and completes the instant its objective is met, granting the next stage in the same message run | All objectives of a quest run at once; a finished quest waits in a `Complete` state for a hand-in |
| The NPC's conversation depends on which stages the player has | `ScriptedKnuBot` replays every captured visit as one linear walk from step 0 |
| Answers already asked are removed from the list for the rest of the conversation | Answers are filtered only by quest state |
| Hand-ins are a dialogue line, then a KnuBot trade window; rewards (xp/credits feedback, items into the OverflowWindow) arrive before the thank-you text | Items go straight into inventory with AddTemplate and the player is told in chat |
| Objectives are things of a kind: any Malfunctioning Cleaning Robot, any Gas Fire (fixture ids change every relight), any Wounded Dockworker | Exact target string; fixtures by instance id |
| Quest fixtures despawn when used and relight later; stage NPCs/objects must exist or the player is stuck | Fixtures are permanent and only sent on zone entry |
| Accept = QuestFullUpdate only; MissionChanged + QuestMessage only when a stage finishes; kill progress = FormatFeedback counter | MissionChanged + QuestMessage + QuestFullUpdate on accept and on every progress step, plus chat IMs |
| Quest ids are per character | Template id sent to everyone |

## Principle

Quests are data, and the data comes from captures by a tool, not by hand. One engine plays every quest;
nothing quest-specific lives in C#. A GM can still author content, and the authored rows are the same
shape as extracted ones.

## 1. Model (database)

- **Stage** - one row per retail quest-log entry (`quests`, keyed by a canonical id: first captured id of
  the Name+Description+giver group). Wire fields stay in `questwire*`.
- **Objective** - what completes the stage (`questobjectives`, reworked): `Kind` + a match on what the
  thing *is* - creature name, fixture template, item template used, NPC name - plus a required count.
  Kinds seen in retail: Kill, UseFixture, UseItemOnFixture, UseItemOnCharacter, TalkOnOpen (window opens),
  DialogueAnswer (a specific answer), TradeHandIn (item template x count). Collect/Equip/Reach/Purchase/
  TradeSkill stay supported but are unproven until captured.
- **Transitions** - what a completed stage grants (`questtransitions`: From, To, Order). Chains branch
  (Marcus's return grants "Talk to Flint Novak" while his dialogue also offers the stim side chain).
- **Rewards** - credits/xp on the stage; items with a phase: OnGrant (the extinguisher, the stim) or
  OnComplete (nano transmitter, first-aid kit), delivered through the OverflowWindow.
- **Conversations** - per NPC: *openers* chosen by quest state (e.g. Rex with "Open the Cargo Box" active
  opens on "It is not that difficult..."), *nodes* (lines said, answer list), *answers* (text, next node,
  actions: grant stage, complete a DialogueAnswer objective, give item, open trade for a TradeHandIn
  objective, close with N seconds), and the NPC's *farewell* (what "Goodbye" says before the window closes
  after 5 s). An answer used in a conversation is hidden for the rest of that conversation.
- **Fixture behaviour** - per fixture template: what happens when used (despawn, relight after N seconds,
  feedback text such as "You extinguish the Gas Fire."), so objects are matched and managed by kind.
- **Stage requirements** - the spawns/fixtures a stage needs; a startup validator reports any stage whose
  objective target, NPC or hand-in item cannot exist in the world, so nobody can get stuck silently.

Character state stays in `charactersquests` (per stage: InProgress or Completed, progress count), keeping
completed rows so conversations and prerequisites can see them.

## 2. Engine (ZoneEngine)

Keep: the per-character lock + DB transaction + snapshot rollback, the QuestFullUpdate / QuestMessage /
MissionChanged builders, per-player KnuBot sessions, trade escrow, OverflowWindow item delivery.

Change:
- Events carry identity *and* kind (creature name + spawn id, fixture template + instance, item template
  used, target). Fix the Use path for pooled fixtures.
- On a matching event: advance the count; when met, complete the stage and apply its transitions in the
  retail order - rewards feedback, stats, OverflowWindow items with Feedback 110/108871108,
  MissionChanged + QuestMessage for the finished stage, QuestFullUpdate (announce as new) for each granted
  stage. Kill progress sends the FormatFeedback kill counter; nothing else is sent for progress.
- Grant (from dialogue or a transition) sends only the QuestFullUpdate, after any OnGrant items.
- Per-character quest ids on the wire, stable across reconnects.
- Conversation player: opener by state, nodes, per-conversation answer removal, actions, trade hand-in,
  farewell + close in 5 s. Replaces `ScriptedKnuBot`'s walk and the invented `QuestGiverKnuBot` menu and
  "I am ready to help with mission" rows.
- Fixture manager: despawn on use, relight timer, streaming of fixtures to players after entry.

## 3. Extractor (Tools/Capture)

A quest pass that reads each retail session as one ordered stream of both directions (capture CSV row
order is capture order) and records:
- quest log events: stage granted (with the message run it arrived in), stage completed
  (MissionChanged + QuestMessage) and the stages granted right after;
- the trigger of each completion: the last player event before it - kill (CharacterAction 99 + kill
  counter), GenericCmd Use / UseItemOnItem on a fixture (by template), item used on a character
  (TemplateAction with a target), window open, answer selected, trade finished;
- rewards around a completion: FormatFeedback xp/credits (base-85 ints), TemplateAction +
  ContainerAddItem into the OverflowWindow;
- conversations from open to close: the active stages when it opened, every line and answer list, the
  answer chosen, and what followed (grant, completion, trade, item, close);
- fixtures by template and position, with despawn/relight.

Sessions are merged by canonical stage and NPC name; each derived rule records the captures that prove
it, and conflicts or single-sighting rules are reported for review. Output: SQL for the model above plus
a readable report. Retail sessions only (full-update Playfield 2150461 for Arete); duplicate capture
files are skipped.

## 4. Verification without hand testing

A replay test drives the engine with the client actions of a sniff (answers chosen, kills, uses, trades)
and compares the server messages it produces with the ones retail sent - message types, order, quest
stages, texts, items and rewards. Every captured quest becomes an automated test.

## Phases

1. Extractor quest pass + report; must reproduce the Rex and Marcus chains exactly as analysed by hand.
2. Schema and stage engine (objectives by kind, transitions, rewards, retail message order, per-character ids).
3. Conversation player (openers by state, answer removal, actions, trade hand-in, farewell).
4. Fixture behaviours (despawn/relight, streaming) and stage requirements + startup validator.
5. Replace the Arete quest/dialogue data with extracted output; remove the invented `z-*` rows; adapt
   `/questedit` to the staged model.
6. Replay tests for every captured chain.

## Status

- **Phase 1 - done (2026-09-14).** `Tools/Capture/QuestExtract.cs`. Over every retail Arete capture it
  derives 41 stages, 98 conversations and 10 fixture templates - the whole chain from Rex Larsson to Vaughn
  Hammond plus the side chains - with grant, trigger, rewards and items per stage and conversations keyed
  by quest state. Output: `E:\Funcom\captures\arete-landing\quest-model\`. Open points: items used straight
  from the overflow window cannot always be identified (the client sends no move out of it), and one
  session attributes the Kneebreaker kill to a nearby robot.
- **Phases 2-5 - implemented (2026-09-14), awaiting the owner's live test.**
  - Engine: `questtransitions`, `knubotdialogue`, `knubotopeners`, `fixturebehaviours`; `ConversationKnuBot`
    (openers by quest state, asked questions removed, grant / trade nodes, farewell node -1, 5 s close);
    `FixtureBehaviours` (despawn on use, come back after `RespawnSeconds`); stages finish on their last
    objective and grant their transitions in the retail message order; kill counter and reward FormatFeedback.
  - Objective targets: fixtures by template, or by instance for a fixture the client has from its own
    playfield data (the server never spawns it; Use on it now reaches `QuestManager.OnUse`); items by id;
    a kill counter naming a group is written `label|creature|creature` (`QuestStateRules.TargetNames`).
  - Data: `QuestExtract <out> <captures...> --playfield 2150461 --as 6553` writes `quest-stages.sql`, now
    `SqlPatches/arete-landing-quests.sql` (41 stages, 40 objectives, 38 transitions, 448 dialogue rows,
    56 openers, 1 fixture behaviour - the Gas Fire). It replaces `arete-landing-questwire.sql`, `-dialogue.sql`
    and the invented `z-quest-actions`, `z-quest-items` and `z-objective-items`. Two vendors no capture here
    records (Remi Gallois, Antonio Stacklund) keep their earlier captured lines in
    `arete-landing-z-vendor-dialogue.sql`. A fixture counts as used up only when it despawns within 100
    messages of its use (Gas Fires: 30-64); the Cargo Box and terminals despawned thousands later, walked away from. Inputs: `E:\Funcom\captures\*.csv`
    and `E:\Funcom\sniffs\*.csv`. `--find <value>` lists every decoded message mentioning a value.
  - Rules the generator applies, each written into the SQL comments: the trigger seen most often; the item
    linked in the stage text over an inferred slot item; an item the stage text links arriving just before
    the finish is a purchase (Buy a Lockpick); a creature the stage text names over a bystander's kill.
  - OmniCell-defined, labelled in the SQL: fixture respawn 60 s (no capture timestamps); while a stage granted
    in conversation is on, the NPC repeats its hand-over text (owner's account of Rex Larsson).
  - Open: "Tradeskilling (1/4)" has no captured finish, so no conversation hands it out; the
    `arete-landing-z-strongbox.sql` Strongbox (instance 900000001) duplicates the client's own playfield
    Strongbox 1477021817, which the stage now uses; the per-character wire quest id (plan step 5) is not done.
- **Phase 2 plan (as written before implementation).** Reuses `QuestManager`'s lock + transaction + rollback core.
  1. `questtransitions` (FromQuest, ToQuest, Ordinal) table, entity and DAO; `QuestObjectiveType` gains
     UseItem, UseItemOnCharacter and DialogueAnswer; Use/UseItemOn match a fixture by template or name as
     well as instance; objectives may name the item used (TargetLowId).
  2. Accept: persist, OnGrant items through the OverflowWindow (TemplateAction 87 + ContainerAddItem 0x6F),
     then QuestFullUpdate(only this stage, announce) - no MissionChanged, QuestMessage or chat IMs.
  3. Advance: persist; a Kill objective with a count sends the FormatFeedback kill counter
     (`~&!!!":$nZiAi` + base-85 remaining + `s` + name); nothing else. When every objective of the stage is
     met the stage finishes at once (except trade hand-ins, which finish in the trade).
  4. Finish: persist rewards; send FormatFeedback reward (`$'O"u`, xp, credits), changed stats, each OnComplete
     item into the OverflowWindow followed by Feedback 110/108871108, MissionChanged + QuestMessage for the
     finished stage, then grant every transition target (step 2).
  5. Per-character wire quest id (derived from the character's progress row, reverse-mapped for client
     quest actions) in QuestFullUpdate, QuestMessage and MissionChanged.
  6. Hooks: UseItem and UseItemOnCharacter from `PlayerController.UseItem` (the selected target), fixture
     template passed to OnUse/OnUseItemOn, the pooled-fixture Use path fixed in `GenericCmdMessageHandler`.
  Callers to keep compiling: ChatCommands/Quest.cs, QuestEdit.cs, KnuBot/ScriptedKnuBot.cs and
  QuestGiverKnuBot.cs (replaced in phase 3), Program.cs quest audit, the event hooks listed in the handoff.

## Known unknowns (need a capture, never guessed)

- Relight delay of a fixture (needs capture timestamps - the capture CSV has none yet).
- What an NPC says in a state no capture visited (reported; left to the owner, not invented).
- Abandoning a quest; quests with multiple simultaneous objectives in one stage.
