# OmniCell

A server emulator for **Anarchy Online**, written in C#.

OmniCell continues the [CellAO](https://github.com/CellAO/CellAO-NightPredator)
project, which was developed from around 2005 and last saw a commit in December
2016. Nearly all of this codebase is theirs. Years of protocol and client data
format reverse engineering sit behind it, and none of that work is ours. The
acknowledgements at the end of this file are CellAO's own, kept in full.

## Status

The server runs. Login, character creation, zoning and movement work against a
current retail client. A great deal does not: combat, nano casting, trading and
most NPC interaction are unimplemented, and roughly two thirds of the client
protocol has no handler. See `OmniCell/Documentation/DataAudit.md` for a survey
of what the client archive contains versus what the server actually reads.

## Running one

Build `OmniCell/OmniCell.sln` in Release with the .NET 10 SDK, and use MySQL 8
or MariaDB 10.11 or newer. The server's data set is in the repository, so a
clone runs as it stands.

From the repository root:

```
dotnet build OmniCell\OmniCell.sln -c Release
```

Visual Studio 2026 (version 18) builds it too. `global.json` pins the .NET 10
SDK, which Visual Studio 2022's MSBuild cannot load.

The first build needs an internet connection. The NuGet packages (Dapper,
MySqlConnector, NLog and the rest, listed in `THIRD-PARTY-NOTICES.md`) are not
kept in the repository; restore downloads them into your NuGet package cache,
and later builds work offline from there.

The folders that hold data on your machine - recorded sessions and your
Anarchy Online client - are set in one file, `paths.cfg`. `SETUP.md` explains
how to make it.

```
create-database.bat     make the database, load its tables and content patches
configure-server.bat    who can play, and where the database is
start-engines.bat       run it
```

The repository ships the schema in `SqlTables` and ordered emulator content in
`SqlPatches`; `create-database.bat` loads both and reports any failed file. The
accounts and characters tables start empty, so make an account with `adduser`
in the LoginEngine window. `export-database.bat` can create an optional
`Database/omnicell.sql` snapshot from a running database for faster re-imports.

Players need their own Anarchy Online client to connect, the same as any other
server. The complete converted item, nano and playfield data set is stored in
the versioned `*.ocp` OmniCell content format; the raw client database and the
extractor's intermediate `*.dat` caches are not part of the repository.

Nothing is hard coded to localhost; `configure-server.bat` asks whether players
are on this machine, your network or the internet, and writes the addresses to
suit. It writes `Config.local.xml`, which git ignores, so a database password
cannot reach a commit.

`OmniCell/Documentation/Running-a-server.md` is the whole of it: the schema,
accounts, GM levels, the two kinds of address and which one causes the client
to hang on entering the world, and what to look at when something is wrong.

## Helping

The protocol is worked out by watching the real server. If you play Anarchy
Online, a recording of an ordinary afternoon is the most useful thing you can
contribute, and you do not need to know anything about the protocol to make
one — see `Tools/Capture/README.md`.

`prepare-sniff.bat` turns a recording into something safe to hand over: your
account name, your password, your character list, your tells and your IP
address are left behind, and it tells you exactly what it kept and what it did
not.

## Layout

```
OmniCell/Server/         LoginEngine, ZoneEngine, ChatEngine, WebEngine
OmniCell/Libraries/      shared libraries, Cell.Core networking, AOtomation protocol
OmniCell/Config/         Config.xml - the tracked defaults
OmniCell/Datafiles/      canonical OmniCell content packs
OmniCell/Documentation/  Running-a-server.md, DataAudit.md, generated enum references
Tools/Capture/           recording sessions and reading them back
Tools/Algorithman/       the client data extractor
Tools/Ashly/             the launcher, which points a client at your server
```

## Licence

The combined work is **GPL v3** (`LICENSE`). That follows from its parts rather
than from preference: `Cell.Core` and `Cell.Util` come from
[WCell](http://www.wcell.org/) under "GPL v2 or any later version", and
msgpack-cli is Apache 2.0, which is compatible with GPLv3 but not v2.

CellAO's own code is **BSD 3-clause**, carried in the per-file headers rather
than a licence file. All 532 of those headers are retained unmodified. See
`LICENSE-CellAO.txt`.

`THIRD-PARTY-NOTICES.md` has the full picture: WCell, the AOtomation protocol
layer (WTFPL), msgpack-cli, the NuGet dependencies, and one inherited open
question about six vendored collection classes that reserve rights to their
original authors without stating a licence.

Anarchy Online is copyright [Funcom](http://www.funcom.com/). This project is
not affiliated with, endorsed by, or supported by Funcom.

This is a server emulator. It ships the server code and OmniCell's converted
content representations, not a game client, art archive, Funcom executable,
raw resource database, or extractor cache. You need your own copy of Anarchy
Online to connect.

The data set is the work of the people who built this. `tradeskill.sql` is
108,783 rows of what combines with what, compiled by the CellAO project over
years and carried under their BSD licence, alongside the teleports, shop
inventories, spawn templates and vendor tables — none of which any tool
produces. `OmniCell/Documentation/DataAudit.md` says which tables were authored
and which came out of a client's record store.

---

This is a thank you from the CellAO Dev Team, we would like to really thank our community for all the support you give us.

We also would like to thank the following people (In no order):

* **Tom** for Supplying us with our Website and being our Webmaster
* **Suiv** for always being there for me :)
* **NV** for being NV
* **HacklerOfDreams** for helping us off on our road to zoneing, giving us our script engine and other things
* **Ashly** (Me) for starting this project
* **n5du** for fixing the login engine and adding multi character support (come back dude)
* **Chaz** for being in our QA And going above and beyond your job when asked
* **Jin** for leaving and joining MSN soo much you flood us with the quit and relogin msgs ;) And for all the help you have given us.. Hope you return soon.
* **Schwuppweg** for doing our German Forum moderating also for his contribution to the future home of our public test server, Danke dir vielmals Schwuppweg für deine große Hilfe
* **DniFan** for joining our team and enriching us with ideas about Lua again.
* **Black** for giving us the infopacket and future stuff to be listed ;)
* **Wargreymon** for implamenting Zoning.. as well as being a support tech.
* **Algorithman** for fixing mods, items, and all future work you will do for cellao, thanks for bringing life back to the project.
* **Andyzweb** for all the time you have contributed to CellAO, and various of things you have done for us... Thank you
* **Sebika** - An old from '08 CellAO Database team member
* **MrSecret** - for being our Forum guy.
* **Spectrome** - `<+Spectrome>` i havn't done anything `<+Spectrome>` ive scratched my ass at the code and annoy chris
* **iphoneprodigy** - For giving us our webcore.
* **T0t4r4** - Thank you coming soon(tm)
* **Estrid** - For continueing the support of the Webcore.
* **Swifty** - For contributing Example Scripts for CellAO.
* **Kyle873** - Note to come later

Others:

* **Freebs** (Where the heck did you disapear to?)
* **Moose**
* **Ruffus**
* and the guys at **WCell**
* We would also like to give a warm and special thank you to [Funcom](http://www.funcom.com/) for giving us [Anarchy Online](http://www.anarchy-online.com/)
