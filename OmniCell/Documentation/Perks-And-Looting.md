# Perks, corpses, chests, lock picks and traps - what retail sends

What a server has to accept and send to reproduce perk use and every kind of looting inside a
mission, taken from one retail recording. Every line names the message it came from. Where the
recording does not show something, this page says so rather than filling the gap.

Evidence: `marked-20260928-041531_00001_20260928041532.pcapng` (in `<CAPTURES>\pcap`), TCP stream 20,
a Keeper (`50000:759513654`) in a QL250 mission (playfield 2151365), 04:20:44-05:08:36 local.
References below are `time C|S seq=N` (direction, AO packet sequence). The full per-mark
walkthrough with raw bytes is `E:\Funcom\AOBuddy10\docs\evidence-perks-and-looting.md`.
Text ids resolve through `Tools/Capture/LdbText.py` (all category 110).

What OmniCell has today, for orientation: `ZoneEngine/Core/Loot` (CorpseLoot, CorpseLifecycle,
CorpseLootAccess, LootGenerator), `CorpseFullUpdateMessageHandler`, `ChestItemFullUpdateMessageHandler`.
`Missions.md` says mission chests are sent but do not open. No handler for CharacterAction 179
(perks), UseItemOnItem lock picking or trap dynels (51008) was found by a text search of `Server/`.
Those are the gaps this page is for.

## 1. Perks

**Accept** `CharacterActionMessage Action=179` with `Target = the player`, `Parameter1 = perk action id`,
`Parameter2 = 4-char perk code as big-endian int32` (e.g. 10778 / `QJOP` Seppuku Slash, 10190 / `LAON`
Lay On Hands). The Target field is always the player itself (142 of 142 presses). **The perk's target
is the last `LookAtMessage` target**, not the Target field (04:21:15.186 C seq=13 LookAt mob ->
04:21:35.196 C seq=17 perk -> damage on that mob at S seq=504).

**Send, on success**, all with Identity = the player unless noted:

1. `CharacterAction Action=80 P1=2 P2=<delay cs>` on receipt. P2 per perk: 50, 100 or 200; the measured
   gap to execution matched P2 centiseconds (50: 0.30-0.59 s, 100: 0.85-1.32 s, 200: 1.89-2.34 s).
2. at execution, in this order (Seppuku Slash, 04:21:35.768 S seq=501-506):
   - `CastNanoSpell NanoId=<"Affected by X" nano> Target=<LookAt target> Caster=player` (Identity = target)
   - `CharacterAction SetNanoDuration(98) Target=NanoProgram:<nano> P1=player P2=1000` (Identity = target)
   - `CharacterAction Action=207 P1=<perk id - 10000> P2=<cooldown seconds>`
   - for a damage perk: `HealthDamage Delta=-N DamageType=... Source=player` (Identity = target);
     for Lay On Hands: second `CastNanoSpell 215841` on self and `HealthDamage Delta=+N DamageType=None Source=player` on self
     (04:21:55.108 S seq=866-872); for self buffs: the buff nano's CastNanoSpell + SetNanoDuration on self
   - `FormatFeedback 110/707 "%s"` = "You successfully perform <name>." (Seppuku: "... a Seppuku Slash attack.")
   - `TemplateAction ItemLowId=ItemHighId=<perk item> Quality=1 Amount=1 Action=32 Placement=player TargetType=50000 TargetInstance=<target>`
3. when the cooldown ends: `CharacterAction Action=206 P1=0 P2=<perk id - 10000>`.

Cooldowns measured (A207 P2 vs A207->A206): 190 Lay On Hands 40 s (40.8), 193 Devotional Armor 120 (121.3),
773 Blade Whirlwind 110 (111.9), 775 Honoring the Ancients 45 (44.8), 778 Seppuku Slash 95 (96.4).
**No stat carries a perk cooldown** - nothing but Health, IsFightingMe, SocialStatus, Clan, ClanLevel and
Cash was sent for the player in 48 minutes.

**Refusals** (no Action 80, no 207):

| condition | send |
|---|---|
| friendly-only perk, LookAt target hostile | `Feedback 110/106156137` "Item must be applied on a friendly target." (04:21:49.862) |
| same perk already queued | `Feedback 110/171187118` "You are already running this action!" (04:24:27.802) |
| hostile perk, target just died | `Feedback 110/25614836` "This item requires a fighting-target to be applied on." (04:59:00.196) |

Perk code 20010 `UNWR` ("Unhallowed Wrath Item") behaves differently - usually answered with
`FormatFeedback 110/79653355` "Target evaded your Unhallowed Wrath Item!" + `Feedback 110/205237300`
"Target resisted.", never with 206/207. Its mechanism is **not determined**.

## 2. Corpses

**On a kill**: `CharacterAction Death(99)` (Identity = mob), then to the killer
`Feedback 110/249817907` "You can loot these remains." and `CorpseFullUpdate Identity=51050:<n>
Name="Remains of <mob>" Owner=<mob identity> LockDifficulty=50` at the death spot (04:22:21.559-679 S seq=1322-1329).

**Open** - accept `GenericCmd Action=Use(3) Flag=1 Serial=s Target=[51050:<n>]`, send
`InventoryUpdate BagIdentity=51050:<n> NumberOfSlots=21 Access=CanRemove(2) Open=1
SlotnumberInMainInventory=<k> Entries=[...]` then echo the GenericCmd with Verification=1, same Serial
(04:22:26.554 S seq=1417/1418). Corpse items carry Flags=161. `k` is a per-character counter that goes up
by one for every container opened in the session (112 ... 153 here).

**Take** - accept `MoveItem Source=(107, k<<16 | slot) Destination=111`, send
`ContainerAddItem SourceContainer=(107, k<<16 | slot) Target=player TargetPlacement=111`
(04:22:43.710 C seq=54 -> 04:22:44.085 S seq=1682). No other message acknowledged the take.

**Close**, two forms:
- `GenericCmd Use` on the corpse again -> `ActionMessage Action=102 FieldMask=1 Instigator=player Identity=corpse`,
  `CharacterAction Action=110 P1=P2=0`, GenericCmd echo (04:22:45.200 S seq=1707-1709).
- `CharacterAction Action=142 P1=51050 P2=<n>` -> only `CharacterAction Action=110` (04:27:00.910 S seq=8309).

**Lifetime**: empty or emptied corpse -> Despawn 0.2-5.7 s after its close (five cases). Corpse left with
items -> Despawn ~182-185 s after its CorpseFullUpdate (13 cases, e.g. 04:24:31.981 -> 04:27:36.100).
Corpse instance numbers are reused once despawned.

## 3. Mission containers - one type for chests and static loot

Treasure chests, barrels, boxes, small crates, bags, crashed androids, skeletons and skulls are all
`ChestItemFullUpdate Identity=51017:<n>`, sent from zone-in and again when their room comes back into
view: Owner None, Coordinates, `Stats = Flags, StaticInstance(23), ACGItemLevel(701)=1, ACGItemTemplateID(702),
ACGItemTemplateID2(703), MultipleCount(412)=1`, `LockDifficulty`, `Keyholders=[]`, Marker `1000015:7` for
Treasure and `1000015:0` for everything else. Templates seen: Treasure 162447 (locked) / 162448, Barrel 23163,
Box 212906, Small Crate 212908, A bag 212909, A Crashed Android 213437, Skeleton 213433, A Skull 213439.

**Locked** = Flags bit **0x40** plus LockDifficulty 546-550 (Treasure 0x20001841 vs unlocked 0x20001801;
Barrel 0x20001C61 vs 0x20001C21). Static loot has 0x400|0x20 more than Treasure (meaning not determined).

**Open (unlocked)**: as a corpse - `GenericCmd Use` -> `InventoryUpdate` (Access=CanRemove(2), items Flags=33)
+ echo; no ActionMessage on open (04:41:45.388 C seq=641 -> S seq=16238/16239).
**Take**: as a corpse (MoveItem/ContainerAddItem).
**Close**: `Use` again -> `ActionMessage 102` + echo, **no** Action 110. `Action 142` on a 51017 gets no reply.
**After close**: an emptied container is despawned 0.2-2.3 s later and never sent again (11 cases,
e.g. 04:28:43.207 -> 04:28:45.077); one closed with items in it stays.

**Refused in combat**: `Feedback 110/131926324` "You can't open a chest while you're in a fight." and the
GenericCmd echo with **Verification=2** (05:01:29.410 S seq=32204/32205).

**Lock pick** (item 95577 "Lock Pick" in inventory slot 84): accept
`GenericCmd Action=UseItemOnItem(5) Flag=0 Target=[Inventory(104):<slot>, 51017:<n>]`, send (04:28:33.742-04:28:34.171 S seq=9132-9142):
1. GenericCmd echo Verification=1
2. `ActionMessage Action=115 FieldMask=1 Instigator=player Identity=51017:<n>`
3. `InventoryUpdate` of the container's contents - **picking also opens it**
4. `Feedback 110/265781900` "Lockpicking successful."
5. `FormatFeedback 110/5087712` "<n> of your XP were allocated to your personal research." (ChatCategory 1107296284) + `PerkUpdate`

Five picks, five successes; the pick was not consumed. **Not in the recording**: a failed pick, and the
reply to a plain Use on a locked container.

## 4. Traps

A trap is its own dynel, `TrapItemFullUpdate Identity=51008:<n> Owner=51017:<container>`, template
212967 "A lair of lizards", ACGItemLevel 250, TrapDifficulty=800, Armed=True, Revealed=False, sent with the
room (first 04:22:51.084 S seq=1831). The trapped box's own ChestItemFullUpdate is identical to an untrapped one.

Sprung (05:01:24.035 open -> empty; 05:01:25.115 close -> ActionMessage 102 + echo at 05:01:25.200), then at 05:01:25.799:
- `SimpleCharFullUpdate` of a new NPC "Garbage Salamander" (50000:2057957151) beside the box
- `FormatFeedback 110/707` "Doh! Lizards!"
- `ActionMessage Action=109 FieldMask=1 Instigator=player Identity=51008:<trap>`
- `Despawn 51017:<box>`
- the salamander's `SpecialAttackWeapon` and an immediate `AttackMessage Target=player`

**Not determined**: whether it fires on close, on the box's despawn or on a timer after open (one sample);
whether the trap dynel is removed; detection and disarm.

## 5. Unexplained client traffic to tolerate

The client sometimes sends 14-19 `CharacterAction Action=152 Target=<mob>` within ~0.4 s immediately after
that mob's Death(99) (04:58:59.932, 05:00:41.105, 05:07:01.742). Retail sent nothing back.
