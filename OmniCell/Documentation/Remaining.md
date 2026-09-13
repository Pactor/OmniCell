# What is left, and what each one needs

Twenty nine packets are amber. That is not thirty equal problems, and this page
exists so the last pass does not treat them as one. Every one of them has every
byte *read* - all 103 message types round trip byte for byte across 646,372
packets - so what is missing is meaning, and the meaning is missing for three
quite different reasons.

Five of them arrived by demotion on 2026-09-11 rather than by discovery. They
were green while their own tables carried rows that were not, which is the one
thing worse than an amber packet, and the generator now warns about it. Two of
those five have since gone back to green with the question actually answered -
CharDCMove and Clone - and InfoPacket, which was never one of the five and was
the largest amber packet in the set, went green on 2026-09-12, and so did
SimpleCharFullUpdate, which had been down to one open row, and
FormatFeedback, whose one open row was a chat category id that GUI.dll
names twenty three of.

The count went the other way once on the same day and that was right too.
QuestAlternative had no byte table at all - the generator called it a schema
and it sat outside the amber list rather than inside it. It has one now, and it
counts as amber like everything else short of green: eight rows proven and one
open, the byte that follows each mission.

The first cut of that page was written without reading the managed class first,
which is the one rule on this subject that has held from the start. It said no capture contains one - four do - and it left two rows open
that the class had already named from an error message the disassembly pass had
not found. Both are fixed. The class was right and the page was not, which is
the same way round as the QuestInfo offset was in the last session.

The count has moved 116, 118, 119, 120, 121 and then back to 120 over two
days. The step down was StopFight, on 2026-09-12, and it is the kind of move
this page should record loudly: the packet was green because its one field was
believed to mean something, and it does not. The reader normalises a 32-bit
value into a boolean, the writer emits it, and the dispatcher stops the fight
with constants without ever looking at it. A wrong green is worse than an
amber, so it went back.

It was found by auditing the green pages rather than the amber ones - sixteen
packets are marked green while their managed classes still call a member
Unknown, and that turned out to be a good place to look for claims nobody had
re-read. What moved it was not new
captures: it was four places in the client nobody had looked - the second
vtable a message inherits, the stat ids a *reader* pushes, the field names
the developers wrote into their own error messages, and the resource database
the client ships, whose record names spell out the four-character codes the
server sends.

This page is the triage; the
packet pages carry the evidence.

## The twenty nine, by what each still owes

Generated from the pages on 2026-09-12 rather than written by hand, so it does
not drift from them. "Every row is proven" means exactly that: the packet's byte
table has no open row left, and it is amber only because a field it carries is
one the client reads and drops. Those twenty four move together, on one decision.

| packet | open rows |
|---|---|
| DoorStatusUpdate | none - every row is proven |
| FollowTarget | none - every row is proven |
| Fov | none - every row is proven |
| FullAuto | none - every row is proven |
| FullCharacter | none - every row is proven |
| GridDestinationSelect | none - every row is proven |
| InventoryUpdate | none - every row is proven |
| KnuBotOpenChatWindow | none - every row is proven |
| KnuBotRejectedItems | none - every row is proven |
| KnuBotTrade | none - every row is proven |
| LaserTargetList | none - every row is proven |
| MentorInvite | none - every row is proven |
| MineFullUpdate | none - every row is proven |
| N3Teleport | none - every row is proven |
| PetToMaster | none - every row is proven |
| Quest | none - every row is proven |
| QuestAlternative | none - every row is proven |
| SendScore | none - every row is proven |
| ServerPosDebugInfo | none - every row is proven |
| SetName | none - every row is proven |
| SpellList | none - every row is proven |
| StopFight | none - every row is proven |
| TeamInvite | none - every row is proven |
| TrapItemFullUpdate | none - every row is proven |
| ClientRequestBuild | the build plan entry's identity. Not sent empty, as the page said until 2026-09-12 - the guard skips the empty ones, so a server will be handed a value |
| GenericCmd | the ActionData flag's value rule, which is the server's and not the client's |
| Mail | flag bits 2, 3 and 7 |
| OrgServer | the eighth of kind 2's strings, and nothing else |
| QuestFullUpdate | the quest record - shape confirmed, meaning outstanding |

Five packets with a question left, twenty four with none - and that split moved
a long way on 2026-09-12, in the second direction. Eleven rows that were open
became proven, and only one of them by learning anything new.

Ten were simply traces nobody had finished - Fov's two, N3Teleport's three,
SpellList's flag, TrapItemFullUpdate's two, MineFullUpdate's int32 and
FullCharacter's three-byte records. Each is a field the client reads and
drops, which is what the rest of the twenty three already were; they sat in
the wrong group because the trace stopped early.

The eleventh is FullCharacter's research and perk records, and that one was a
real correction. The page said all three of the record's int32s are read and
dropped. That holds for the plain form, whose three go into a stack local
nothing touches again, and not for the marker form: its second int32 goes
straight into the slot's + 0x1C, the eighth word, which sits past the seven
the rep movsd overwrites. N3Msg_GetPerkProgress reads it back as (total minus
it) over total, so it is the experience still owed. FullCharacterEntry had
said as much since 2026-09-11; the page had not caught up, which is the third
time in one day that the class was ahead of the page.

Of the five that remain, three want something the client cannot give:
ClientRequestBuild needs a city, OrgServer needs one of the eight kinds no
capture holds, and GenericCmd's flag is chosen by the server, not the client.

## 1. Provably inert: the client reads it and nothing ever looks at it

The largest group, and the one that needs a decision rather than work. In each
of these the field is read, often written back, and no consumer exists anywhere
in the shipped binaries - checked with a signal scan for signals and a member-reference scan for
members, both of which report "no reader found" rather than "no reader exists",
which is the honest limit of the tooling.

| packet | the field |
|---|---|
| Fov | the first float and the trailing int32 |
| SpellList | the unread flag, passed through two calls and dropped |
| MineFullUpdate | the trailing int32, read and written back |
| TrapItemFullUpdate | two Identities nothing consumes |
| Mail | the int32 at record + 0x10 |
| InventoryUpdate | flag bit 0x80 - a getter and a setter nothing calls |
| GridDestinationSelect | two ints stored, written back, never read |
| ServerPosDebugInfo | the third Vector3 |
| DoorStatusUpdate | a byte and a list, with doors actually opening in the capture |
| PetToMaster | the signal at + 0x40, no subscriber |
| KnuBotOpenChatWindow | the signal at + 0xEC, no subscriber |
| FollowTarget | the variant 2 movement parameter |
| Quest, SetName, TeamInvite, MentorInvite, LaserTargetList, KnuBotTrade, KnuBotRejectedItems, SendScore | fields the write-ups already call reserved |
| FullAuto | an int32 "the dispatcher ignores" - a fact about the client, not the field |
| N3Teleport | two Identities and the trailer blob: three references each in the class, all of them construct, fill and write back |

On 2026-09-12 the tooling caught up with the claim. The member-reference scan now
follows what the reader does with a member address - the `lea` and the `push`
that hand it to a nested reader, and the `add reg, k` the compiler uses to walk
a run of adjacent fields - and checks every vtable the class owns. Run over the
classes in this group it confirms, mechanically, that nothing but the reader and
the writer touches:

| class | member |
|---|---|
| FullAutoIIR_t | + 0x1C |
| MineFullUpdateIIR_t | + 0x8C |
| SetNameIIR_t | + 0x38, and one more |
| QuestIIR_t | + 0x24 |
| MailIIR_c | + 0x58, + 0x78, + 0x84, + 0xF8 |
| n3TeleportIIR_t | + 0x4C, + 0x54, + 0x5C |
| SimpleChar_t | + 0x1CC, FullCharacters three-byte records |

Those are message-class members, which do not line up one for one with the
record offsets the packet pages quote. What the table settles is the claim
itself: the field is read, written back, and read by nothing, in a check that
would have caught the second-vtable mistake.

There is a second distinction inside this group and it matters for the ruling.
A field that is read, stored and written back can still leave the client - if
the client ever sends that message. For most of these it never does.

The constructor answers it: whoever calls the function that
writes a class's vtable is whoever makes one, and a class whose only maker is
the message factory is one the client only ever reads. Run over this group:

**Never sent by the client** - the write-back is dead code and the field cannot
leave: DoorStatusUpdate, FullAuto, GridDestinationSelect, InventoryUpdate,
KnuBotRejectedItems, MentorInvite, PetToMaster, SendScore, SetName,
TeamInvite, MineFullUpdate, TrapItemFullUpdate.

**Sent by the client**, so the inert field really is emitted and the value rule
on the page is what a server should expect: KnuBotTrade through
N3Msg_NPCChatAddTradeItem and N3Msg_NPCChatRemoveTradeItem, Quest through
N3Msg_RemoveQuest, and Mail through its four senders.

That is the same list the pages arrived at one at a time, which is worth
something: the tool found the four Mail senders, the two KnuBot senders and
Quest's single construction site without being told they were there.

GridDestinationSelect was in that sent list for one commit and should not
have been. The first cut of the tool found calls by searching for the 0xE8
byte, and one such byte inside an immediate a few bytes above a second
constructor decoded as a call to it. Disassembling each candidate before
believing it removed that caller and no other: the message is server to
client, as its name always suggested.

**These are green under a looser definition and amber under ours.** The decision
was taken deliberately: green means a byte has a known meaning, and "the client
ignores it" is not one. Roughly a dozen packets would move if that changed.

## 2. Waiting on a capture nobody has taken

The client's own reader gives the layout; only the values are missing, and no
amount of disassembly will supply them.

| packet | what would produce one |
|---|---|
| OrgServer | an organization. Every one of the 404 captures is kind 6; kinds 1 to 9 exist and eight have never been seen |
| ClientRequestBuild | a city - the sender is guarded on owning one |
| InfoPacket | nothing. It is green: the reader files its fields into named stats, which covered both blocks no capture has ever carried |
| LaserTargetList, TrapItemFullUpdate, TeamInvite, SetName | no capture contains one at all |
| FullCharacter | a character with perks used and nanos running, for the lock lists |
| SimpleCharFullUpdate | the CAT texture list behind flag 0x40000000, which is empty in every session on disk |

### A caution on this group

Nineteen rows across the pages say nothing reads their field, and that claim
was found wrong twice on 2026-09-12 - both times because it had been measured
against one consumer when there were two. GridDestination’s last two int32s
were reserved metadata in three places because the grid window ignores them,
and OrgServer’s kind 2 feeds them to a template that calls them Type and
Level. OrgServer’s own first Identity was written up as read by no branch,
and kind 9 resolves it to a dynel and gives up when that fails.

The check ran over all nineteen on 2026-09-12 and every one of them stands.
QuestAlternative's trailing byte joined them the same day, followed from the
reader to the mission window and out again: nothing there reads it, and the
one path back to the server carries only the mission's Identity.
Two of the reasons given for them did not, and are fixed.

- LaserTargetList’s three: the record is not shared, and the one walker in
  GUI.dll reads six of the nine fields and never these.
- MentorInvite’s four: the signal they reach, GlobalSignals_c + 0x26C, has an
  emit in Gamecode and no connect anywhere, GUI included.
- SendScore’s trailing entries: this one had a live consumer to check, since
  the battlestation window does subscribe to + 0x238. Its handler reads exactly
  two dwords, the two the page already names ClanVP and OmniVP.
- DoorStatusUpdate’s identity list, FullAuto’s int32, PetToMaster’s Identity,
  SetName’s three and FollowTarget’s two: no method of any vtable those
  classes own touches the member at all, and none of their dispatchers emits a
  global signal, so there is no path out.
- N3Teleport’s two Identities and its trailer: the same, in N3.dll rather than
  Gamecode - the class lives there and the first scan of it looked in the wrong
  module.
- FullCharacter’s three-byte records: the holder they go into is character
  + 0x1CC, and a scan of every instruction in Gamecode using that displacement
  finds thirty, of which three load it - 0x10058C7E allocates it when it is
  null, 0x1005F84E and 0x1008A4F8 free it. Allocate, reuse and free, and never
  the contents.
- SpellList’s unread flag: it really is passed as an argument and dropped. It
  arrives at 0x100A5F4B as the third argument, and that function - all 0xD80
  bytes of it - never mentions [ebp + 0x10].
- TrapItemFullUpdate’s second Identity: nothing reads the member.
- TrapItemFullUpdate’s first, which goes to TrapItem_t + 0x1D4: this one nearly
  became a finding. A member-reference scan reports slot 12 reading [eax + 0x1d4],
  and eax there is the result of a dynamic_cast to SimpleChar_t four
  instructions earlier - a character’s weapon holder, not the trap. The tool
  matches a displacement, not an object.
- Fov’s two: right answer, wrong reason. The page said the block the reader
  fills is not handed to anything else. It is - 0x100524F4 parks it at the
  receiver’s + 0x24 rather than copying out of it, so the receiver keeps it
  and could read the two at any time. It never does, and the page now says
  that instead.

So the group is sound, and the two things worth carrying out of the exercise
are that a claim of this kind is only as good as the consumer it was measured
against, and that a scan matching an offset is not the same as a scan matching
an object.

A third arrived later the same day and is worse, because it was the tool and
not the reading. The signal scan matched two of the three ways code reaches
GetInstance and missed the third - load it into a register once, call the
register for each signal - which is what every window that connects several
handlers does. Fixed, GUI.dll goes from 212 subscription sites to 468.

Every claim in the tree that rested on the tool's silence was re-run against
the fixed version. One changed: OrgServer's + 0x24C, which turned out to carry
the leadership-transfer prompt and named the packet's leading Identity.
MentorInvite's + 0x26C, QuestFullUpdate's + 0x154, KnuBotOpenChatWindow's
+ 0xEC and PlaySound's + 0x190 all still have no subscriber and stand.

The fix was then pushed the other way, in case a subscriber that had been
invisible could name an inert field and take a packet green outright. Every
amber dispatcher was walked for the signals it reaches, one call deep, and the
new ones checked: InventoryUpdate's + 0x8C, PetToMaster's + 0x40, SetName's
+ 0x58 and TeamInvite's + 0x118 all do have GUI subscribers now. None of them
is a lead. Two reasons, both worth knowing before repeating the exercise.

One call deep is too loose. InventoryUpdate's + 0x8C is emitted at 0x1001174B,
which is a shared inventory-changed helper the dispatcher happens to call and
not the packet's own path at all - the same mistake in a different shape as the
flat window that credited Mail's signals to TrapItemFullUpdate.

And it does not matter here anyway, because the fields cannot reach a signal.
The member-reference scan reports the inert members of SetName, PetToMaster, FullAuto and
TeamInvite as touched by no method of any vtable those classes own, the
dispatcher included, so nothing can hand them to a subscriber whatever the
subscriber does. That is a stronger negative than chasing the signal, and it is
the check to reach for first.

## 3. Genuinely unknown, and the client has been asked

Work, not waiting. Each of these has had a trace end without an answer, and the
dead end is recorded on the packet page so it is not walked twice.

Four rows left this section on 2026-09-12 without being answered - FullCharacter's
three-byte records, N3Teleport's three, MentorInvite's four and Mail's record
int32. They were not unknown, they were unfinished: each trace was carried to a
dead end and they belong in group 1 above with the rest of what the client reads
and drops. What is below is what is genuinely still open.

| packet | the question |
|---|---|
| GenericCmd | the ActionData boolean's value-selection rule. Three callers, all inside the ActionData family; the bool is dead on every path before the stack slot is reused |
| Mail | the flags word's remaining bits - 1, 4, 5 and 6 are named |
| QuestFullUpdate | meaning only. The shape is confirmed as of 2026-09-12: a decoder built from the client's reader decodes the record, knowing nothing about the model, and all 32 captured copies consume to the byte. The remaining fields are read, written back and read by nothing, and the four-character codes among them are generated per mission rather than authored - they are in no resource-database record, so the client cannot name them |

## What went green after this page was written

CharDCMove's two "reserved" int32s turned out to be floats the client passes to
n3Dynel_t::VehicleForwardUpdate as extra rotations on the sent heading. Clone's
two extra cloth fields were an overlay texture and an alpha mode, and its own
GameData class had said so for weeks - the page was simply stale. Both are green.

Along the way SimpleCharFullUpdate lost four of its six open rows: the flag-0x20
identity is the parent dynel the character is attached to, the flag-0x40000000
list is CAT texture swaps, the counted blob is the vehicle state, and the five
bits above Race are stat 423.

The last two went on 2026-09-12 and took the packet green. The weapon pair's
fourth int32 is the four-character code of the record that supplied the weapon
and the third is the key it is filed under; they agree for a weapon a monster
equipper granted, and the three entries where they do not are martial arts,
dimach and brawl, whose keys are the stat ids 100, 144 and 142. That came out of
`cd_image/data/db/ResourceDatabase.dat` rather than out of the code, which is a
seventh place to look.

## The six sweeps are done

Everything above is what survived them. Each covers a way a byte-exact round
trip can be silently wrong:

- always-empty lists, which hide a wrong element type
- selectors that enumerate fewer values than the client accepts, which leave
  whole bodies unread
- version fields that gate rather than validate, which leave tails read flat
- reader addresses shared between packets, where one write-up has already
  answered another's question
- arithmetic between a read and a store, where a named field is a transform of
  the value on the wire
- a second vtable, where the base a message inherits reads its members at a
  shifted offset and a grep of the dispatcher finds nothing

The first three found nine defects between them. The fourth found three answers
already sitting in the tree. The sixth found three fields in SimpleCharFullUpdate
that had been written off as read-and-never-used, two of which were stats.

Two instruments were added with the last of those and both are worth reaching
for first: a search of every vtable a class owns, and
a listing of the strings a function pushes - the client names
its own fields in its error messages, and that is how FullCharacter's team
records got side and organisation.
