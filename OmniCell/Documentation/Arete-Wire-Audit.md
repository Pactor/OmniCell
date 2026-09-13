# Arete Landing wire audit

This is the evidence ledger for packets involved in entering and using the first zone. It deliberately separates captured facts from inferences. A field is not considered correct merely because the client tolerates it.

Status meanings:

- **Confirmed**: observed in retail captures and reproduced locally.
- **Observed / meaning unknown**: value and position are confirmed; semantic name is not.
- **Inferred**: required behavior is supported by captures, but the exact production rule is not known.
- **Divergent**: local deliberately differs and the reason is recorded.

## Movement and FollowTarget

| Field or behavior | Status | Evidence / local behavior |
|---|---|---|
| Client sends `CharDCMove` | **Confirmed** | `MOVE_RX` records the graphical client's move type, position and three trailing fields. The prior claim that no movement packet was sent was false. |
| Echo movement to the originating client | **Confirmed absent** | Local now sends the accepted movement only to other clients that know the mover. |
| `FollowTargetMessage.Unknown` | **Confirmed: 0** | Retail path and full-stop packets use zero. Local value 1 preceded the disconnect. |
| Full-stop `FollowTargetInfo` layout | **Confirmed** | Target identity, move type, one byte, three alignment bytes, XYZ floats, coordinate count, then coordinate list. |
| Full-stop coordinate count/list | **Confirmed: 1 + stop XYZ** | The prior writer omitted this tail and shifted the float block. |
| Three bytes after the one-byte field | **Observed / meaning unknown: zero padding** | Their byte position and zero values are confirmed; they are treated as alignment, not an invented integer. |
| `CharDCMove.Unknown1/2/3` semantics | **Observed / meaning unknown** | Captured and logged without rewriting. `Unknown1` varies heavily; `Unknown2` and `Unknown3` have so far been zero locally. |

## Player weapon inventory and equip

| Field or behavior | Status | Evidence / local behavior |
|---|---|---|
| `FullCharacter.InventorySlot.Flags` for a weapon | **Confirmed: 2** | Retail backpack and equipped weapons use 2; ordinary inventory objects use 1. |
| Inventory weapon identity type | **Confirmed: `WeaponInstance`** | Retail never represents an equippable weapon here as `None`. |
| Matching `WeaponItemFullUpdate` before `FullCharacter` | **Confirmed required** | Retail sends one definition for every referenced player weapon identity before the inventory snapshot that refers to it. The client previously disconnected before constructing `MoveItem`; the local probe now constructs and completes the request. |
| Weapon identity allocation | **Inferred** | Local derives a stable identity from character and placement. Retail's allocator is unknown. Type, uniqueness and equality between `FullCharacter` and `WeaponItemFullUpdate` are confirmed. |
| `MsgVersion` | **Confirmed: 11** | Same in repeated retail player-weapon packets. |
| `Character` | **Confirmed** | Owning character identity. |
| `Unknown2` | **Confirmed value source** | Retail uses the playfield instance. Local uses its currently advertised Arete id, 6553; see the playfield divergence below. |
| `Unknown3` | **Observed / meaning unknown: 1000015** | Constant in compared player-weapon packets. |
| `Unknown4` | **Observed / meaning unknown: 0** | Constant in compared player-weapon packets. |
| `Unknown5` | **Confirmed formula: `0x100 + placement`** | Retail examples: backpack placement 66 gives 322; equipment placements 6 and 8 give 262 and 264. |
| `Unknown6` | **Observed / meaning unknown: 8072** | Constant for player-owned inventory weapons in two captured playfields. |
| `Flags` | **Confirmed: 0** | Same across compared packets. |
| `ItemFlags` | **Confirmed for samples: template flags OR `0x400`** | Reproduces captured Worn Blade and other player-weapon values. |
| Stat-id/value run | **Confirmed** | IDs 23, 701, 702, 703, 412 and 26 occur in the captured order with item/quality/count/ammo values. |
| `Unknown7` | **Observed / meaning unknown: 0** | Constant in compared packets. |
| Graphical-client equip after fix | **Awaiting user test** | The headless client received matching identities and completed `MoveItem 69 -> 6`; this does not prove the renderer accepts the object. |

## Marcus Stone Gas Fires (`SimpleItemFullUpdate`)

Four rows are installed for playfield template 6553 and verified through the complete login plus `CharInPlay` handshake.

| Field or behavior | Status | Evidence / local behavior |
|---|---|---|
| Packet count | **Confirmed: 4** | Four landing-platform fires are present in the marked retail quest run and four local packets are emitted. |
| Message size | **Confirmed retail: 155 bytes** | Header size is `0x009b` for each captured packet. |
| Identity type | **Confirmed: `Terminal` (51005)** | All captured fires use this type. |
| Identity instances | **Observed / allocator meaning unknown** | Local fixtures use four unique observed retail values. Retail changes some values across sessions, so the allocation rule is not claimed. |
| Owner | **Confirmed: `None:0`** | Exact captured value. |
| Message version | **Confirmed: 11** | Exact captured value. |
| `Identitytype`, `Instance` | **Observed / meaning unknown: 0, 0** | Exact captured values. |
| Coordinates and headings | **Confirmed** | Copied from the marked retail quest capture for all four fires. |
| `Unknown1` | **Observed / meaning unknown: `1000015:0`** | Exact captured identity. |
| `Unknown2`, `Unknown3`, trailing `Unknown` | **Observed / meaning unknown: 0, 111, 0** | Exact captured values. |
| Name | **Confirmed: empty** | Exact captured value. |
| Stat count and order | **Confirmed: 8** | `Flags`, `StaticInstance`, `ACGItemLevel`, `ACGItemTemplateID`, `ACGItemTemplateID2`, `MultipleCount`, `AnimPlay`, `AnimPos`. Local previously merged seven extra template stats and sorted the list; both divergences are fixed. |
| Stat values | **Confirmed** | `0x800A2221`, 295883, 1, 295883, 295883, 1, 0, 0. |
| Quest interaction events | **Confirmed present in item data** | Gas Fire 295883 has `OnUseItemOn`: require secondary item 296780, solve quest hash `9JRA`, show the extinguish text, then set state 8 to 0. End-to-end graphical interaction is still awaiting a user test. |

## Known Arete playfield divergence

Retail identifies Arete as template 6553 and runtime instance 2150461. Local currently advertises 6553 consistently. Enabling 2150461 only in the zone packet sequence repeatedly left the client on the loading screen, while 6553 allowed entry. This remains **divergent**, not “confirmed correct.” The login character list and redirect must be audited together with the zone sequence before changing it again.

## Next evidence gates

1. Graphical client: log in, move, unequip/equip the Worn Blade, and report whether the client remains connected.
2. Graphical client: visually confirm four fires, accept Marcus's quest, apply item 296780 to a fire, and verify the quest advances and the fire switches off.
3. If either fails, preserve the first failing capture and compare the final client/server packets against the corresponding marked retail action before changing fields.
