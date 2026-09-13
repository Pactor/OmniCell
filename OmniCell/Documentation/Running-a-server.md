# Running a server

From nothing to a character standing in Arete Landing. Build
`OmniCell/OmniCell.sln` in Release first — everything below runs what the build
produces.

```
create-database.bat        make the database and load the schema
configure-server.bat       say who can play and where the database is
start-engines.bat          run it
                           then "adduser" in the LoginEngine window
```

The last three are in the root of the repository and none of them needs you to
edit a file by hand.

---

## 0. Game data

**Nothing to do — it is in the repository.** The data set the server runs on
ships with it:

| | |
| --- | --- |
| `SqlTables` + `SqlPatches` | the schema and ordered emulator-owned database content |
| `OmniCell/Datafiles/*.ocp` | the complete converted item, nano and playfield content packs |
| `SqlTables/itemnames.sql` | canonical item and nano names used by the emulator database |

Step 1 loads the tables and then every patch in filename order. If you have made
an optional `Database/omnicell.sql` snapshot with `export-database.bat`, it can
import that snapshot instead. Go to step 1.

Players need their own Anarchy Online client to connect. The server does not
send them any of its content — the client already has it.

### Moving to a different patch level

The set above represents client 18.8.50_EP1. To rebuild it against another
version, run the extractor from a working directory outside the repository:

```
OmniCell\Built\Release\Extractor Serializer.exe
```

It reads the client record store into model objects and writes the canonical
`items.ocp`, `nanos.ocp` and `playfields.ocp` representation. Copy only those
content packs and the generated `itemnames.sql` into the paths above. Never put
the client database or temporary `items.dat`, `nanos.dat`, `playfields.dat` or
`itemrelations.txt` files in the repository.

> Everything **else** in `SqlTables/` is hand-authored or community-sourced and
> has no path back to a client — `tradeskill` above all, 108,783 rows of what
> combines with what, compiled by the CellAO project over years. Re-extracting
> will never produce those, so a fresh extract adds to the data set rather than
> replacing it. `DataAudit.md` lists exactly which is which.

---

## 1. The database

MySQL 8 or MariaDB 10.11 or newer. Either works; MariaDB is what this is
developed against.

`create-database.bat` asks for four things: the database name, the account the
server should use, a new password for that account, and an existing account
that is allowed to create databases — `root`, normally. It then creates the
database, grants the server's account rights to that one database and nothing
else, and loads the 42 table definitions in the repository - including
`itemnames`, which is required converted content.

It takes a couple of minutes; `tradeskill` alone is eleven megabytes.

Run it again whenever you like. It checks each table before loading it and
leaves the ones that already exist alone, so it will not overwrite a server
that people have been playing on.

### If you would rather do it yourself

```sql
CREATE DATABASE omnicell CHARACTER SET latin1;
CREATE USER 'omnicell'@'localhost' IDENTIFIED BY 'your password here';
GRANT ALL PRIVILEGES ON omnicell.* TO 'omnicell'@'localhost';
```

Then load every file in
`OmniCell/Libraries/Source/OmniCell.Database/SqlTables/` — order does not
matter, there are no foreign keys between them. Ignore the `.obsolete` ones;
they are tables the server no longer reads, kept for reference. Then anything
in `SqlPatches/`, which are changes to tables that already existed and are all
safe to apply twice.

### What the tables hold

| | |
| --- | --- |
| `login` | accounts. One row per account, not per character. |
| `characters`, `characters*` | characters and everything attached to them. |
| `items`, `itemnames`, `instanceditems` | item templates, their names, and the individual ones people own. |
| `mobspawns`, `mobspawns_stats`, `mobspawnsweapons` | spawn points. A row is a *point*, not a creature: what stands there can be killed and another appears. |
| `quests`, `questobjectives`, `charactersquests` | quests, what each asks for, and how far each character has got. |
| `questwire`, `questwireactions`, `questwirerewards` | normalized client-journal fields for captured and explicitly OmniCell-authored quests. |
| `knubotscript` | what characters say. Captured from the live server, or written in the game with `/questedit`. |
| `vendors`, `vendortemplate`, `shopinventorytemplates` | shops and their stock. |
| `playfields`, `teleports`, `proxydestinations`, `staticdynels` | the world, its doors and the things standing in it. |
| `tradeskill` | what combines with what. |

---

## 2. Addresses

`configure-server.bat` asks one question that matters — who is going to play —
and works the rest out.

Nothing is hard coded. Everything below lives in `Config.local.xml`, which the
batch file writes and which is read in preference to the `Config.xml` that
ships in the repository. `Config.local.xml` is ignored by git, so your database
password cannot end up in a commit.

The generated MySQL connection uses `SslMode=None` only when the database host
is loopback (`localhost`, `127.0.0.1`, or `::1`), because that traffic never
leaves the machine. A remote database defaults to `SslMode=Preferred`. Automated
or advanced setups may set `OMNICELL_DBSSL` to `None`, `Preferred`, or `Required`
before calling `Tools/WriteConfig.ps1`; the writer rejects any other value. It
does not force a TLS protocol version, leaving negotiation to the operating
system and connector.

### The two kinds of address, which are easy to confuse

**`ListenIP`** is what the engines **bind** to. It decides who can reach them
at all.

- `127.0.0.1` — only this machine.
- `0.0.0.0` — every network card. Needed for anybody else to connect.

**`ZoneIP`** and **`ChatIP`** are what the server **tells the client to connect
to next**. Logging in is a relay: the login engine hands the client to a zone
server by address, and the zone hands it to chat the same way. So these have to
be an address *the player* can reach, which is not always the one the server
binds to.

That distinction has one very recognisable symptom when it is wrong: **login
works, the character list appears, you pick a character, and then the client
hangs or drops.** That is the handoff, sending the player to an address only
the server itself can reach. If you set `ZoneIP` to `127.0.0.1` on a server
other people play on, every one of them is told to connect to their own
machine.

| Who plays | ListenIP | ZoneIP and ChatIP |
| --- | --- | --- |
| just you | `127.0.0.1` | `127.0.0.1` |
| your network | `0.0.0.0` | this machine's LAN address, e.g. `192.168.1.20` |
| the internet | `0.0.0.0` | your public address, or a hostname |

A hostname is the better answer for the last one — it is resolved at each
handoff, so an address that changes does not strand anybody.

**`CommListenIP`** is not one of these and should be left on `127.0.0.1`. It is
the channel the engines use to talk to each other and it is unauthenticated:
anything that can reach that port can act as one of the engines. `0.0.0.0` is
never the fix for a problem it appears to be causing.

### Ports

| Port | |
| --- | --- |
| 7500 | login |
| 7501 | zone |
| 7012 | chat |
| 80 | the in-game browser panels (shop, market, petition) |
| 6996 | the engines talking to each other — **do not forward this** |

Forward the first four to the machine if players are coming from the internet.

### The client end

Players connect with the launcher in `Tools/Ashly/OmniCell-Launcher`, which
patches the address into a running client. It reads `Info.xml` beside it:

```xml
<ServerIP>the address you gave configure-server.bat</ServerIP>
<ServerPort>7500</ServerPort>
<AOExecutable>C:\Path\To\AnarchyOnline\AnarchyOnline.exe</AOExecutable>
```

It is not part of `OmniCell.sln`; open
`Tools/Ashly/OmniCell-Launcher/OmniCell-Launcher.sln` and build it separately.

---

## 3. Accounts

There is no self-registration and no web signup. Accounts are made at the
LoginEngine console, which is the window `start-engines.bat` opens.

```
adduser
```

on its own asks for each field in turn, which is the easier way. Or give them
all at once:

```
adduser <username> <password> <characters> <expansions> <gm> <email> <first> <last>
```

| | |
| --- | --- |
| `characters` | how many characters the account may make. 6 is the usual answer. |
| `expansions` | a bit field. `127` is everything, and is what you want. |
| `gm` | 0 for a player. See below. |
| `email`, `first`, `last` | stored, never checked, never sent anywhere. |

So a plain account for somebody:

```
adduser someone theirpassword 6 127 0 them@example.com Some One
```

Passwords are stored as a hash, not as text. Nothing in the server can tell you
what somebody's password is; `setpass <username>` is how you change one.

### GM status

```
setgm <username> <level>
```

0 to 511. Anything above 0 is a GM; the number is compared against what each
command asks for, and every GM command in the tree currently asks for 1. So:

```
setgm someone 1     an ordinary GM
setgm someone 0     take it away again
setgm someone 511   everything, for your own account
```

It takes effect the next time that account logs in — the level is read into the
character as it enters the world, so a GM already standing in the game keeps
whatever they had until they come back.

Once in the game, GM commands are typed into the chat window with a leading
slash. `/listcommands` shows what is available. A few worth knowing:

| | |
| --- | --- |
| `/goto <playfield id or name>` | anywhere in the world |
| `/spawn list <filter>` | find a creature template |
| `/npc create <name>` | put a new character where you stand |
| `/questedit new <name>` | give the targeted character a quest to hand out |
| `/quest log` | what you are carrying |

### Writing a quest in game

`/npc create` immediately writes a spawn row, so the NPC returns after a restart.
Target that NPC while using the remaining commands. This example creates a
two-item hand-in whose selected sentence opens a two-slot box and whose successful
turn-in grants credits, experience, and a converted item template:

```
/npc create Quartermaster Test
/npc model 26151
/questedit new Recover robot parts
/questedit desc Recover two pieces of Robot Junk and return them.
/questedit handin 2 42620
/questedit reward 500 250
/questedit itemreward 42620 1 turnin
/questedit say I need both pieces returned.
/questedit option I will find them.
/questedit accept
/questedit option Goodbye.
/questedit step
/questedit say Did you recover both pieces?
/questedit option I found the robot parts.
/questedit trade Put both pieces in the box.
/questedit option Goodbye.
/questedit show
/questedit done
```

The item-reward ID must exist in the converted `items.ocp`; invalid IDs are
rejected. A hand-in also accepts a converted item ID (preferred) or a name that
resolves to exactly one converted template. Ambiguous names are rejected and the
command prints the valid IDs instead of choosing one. `/questedit show` prints
the stored low ID, high ID, and QL beside each exact hand-in, plus item-reward row IDs, and
`/questedit unitemreward <row>` removes one. It also prints objective numbers and
dialogue row IDs; use `/questedit unobjective <number>` or
`/questedit unline <row>` to remove and replace older authored content. Quest
definitions, objectives, dialogue actions, hand-ins, and rewards all use database
rows and the same runtime interpreter as imported Arete quests.

Use `/questedit purchase <n> <item>` when the objective must advance only from a
vendor purchase, and `/questedit tradeskill <n> <item>` when it must advance only
after the production tradeskill receiver creates that output. `/questedit collect`
means route-independent acquisition and deliberately accepts any supported way of
obtaining the item. These event types are not interchangeable.

> **`setgm` used to set every account at once.** The statement behind it had no
> `WHERE` clause: the username was accepted and ignored, and running it once
> made every account on the server a GM with the level you asked for, while
> reporting that it had set one. It now changes one account and says how many
> rows it actually changed. If you ran the old one, check the `login` table:
> `SELECT Username, GM FROM login WHERE GM <> 0;`

---

## 4. Running it

```
start-engines.bat
```

Four windows: ChatEngine, LoginEngine, ZoneEngine, WebEngine. Order matters and
the batch file handles it — ZoneEngine polls for ChatEngine on startup and will
sit retrying until it appears.

`start-engines.bat debug` runs the Debug build instead.

Each engine reads commands from its own window. `stop-engines.bat` closes them
all; closing a window stops that one.

The last thing `start-engines.bat` prints is which ports are actually
listening. If one is missing, the reason is in that engine's window.

### WebEngine and port 80

The shop, market, petition and daily panels inside the client are web pages.
WebEngine serves them, and the launcher rewrites the client's URLs to point at
it. Without it those panels reach nothing.

It wants port 80, because the patched URLs have to fit inside the strings they
replace and `:8080` costs five characters that the shortest of them do not
have. If something else already holds port 80:

```
start-engines.bat port=8080
```

and set the same port in the launcher.

---

## When something is wrong

### Checking a build

From the repository root, this runs the protocol tests:

```
python Tools\RunTests.py
```

With the engines running against your database, enter `quests 6553` in the
ZoneEngine console. The expected result is:

```
Arete Landing (6553): 38 of 38 quests can be started, finished and handed in.
```

**Login works, then the client hangs on entering the world.** `ZoneIP`. See
above — the client is being sent to an address it cannot reach.

**"Error parsing configuration".** `Config.local.xml` is not valid XML. A
password with `&` or `<` in it used to do this; `configure-server.bat` escapes
them now, but a hand-edited file will not.

**Nothing listening on 7500.** Look at the LoginEngine window. Usually the
database: wrong password, or the schema was never loaded.

**The client connects and immediately drops.** Version mismatch between the
client and what the server expects, which is a different problem from anything
on this page.

**No NPCs anywhere.** The playfield loaded with no spawns. `pf <id>` in the
ZoneEngine window says what it thinks is standing there.
