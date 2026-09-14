# Contributing to OmniCell

Thank you for helping. This page is what you need to get a working copy, make a
change, and hand it back without putting anyone's private data or Funcom's files
into the repository.

## Get the code

```
git clone <url>
```

## Build it

Install what `SETUP.md` lists under "Programs to install" - the .NET 10 SDK, and
MySQL 8 or MariaDB 10.11 if you want to run a server.

Build `OmniCell/OmniCell.sln` in Release, from the repository root:

```
dotnet build OmniCell\OmniCell.sln -c Release
```

Visual Studio 2026 (version 18) builds it too. The first build needs an internet
connection to restore the NuGet packages; they are not kept in the repository.
`global.json` pins the .NET 10 SDK, so an older toolset stops with an error
instead of quietly building with an older SDK. The build output goes to
`OmniCell/Built/Release`.

## Run the tests

```
python Tools\RunTests.py
```

It builds the protocol test project first and refuses to report a result from a
DLL older than the source, so a passing run always means the current code. It
needs only the `dotnet` command from the .NET 10 SDK.

## Run a server

```
create-database.bat
configure-server.bat
start-engines.bat
```

`OmniCell/Documentation/Running-a-server.md` covers the database, the two kinds
of address, accounts, GM levels and what to check when something is wrong.

## Record the live game

The protocol is worked out by watching the real server, and a recording of
ordinary play is one of the most useful things you can contribute. Set
`CAPTURES` and `CAPTURE_INTERFACE` in `paths.cfg` (see `SETUP.md`), then follow
`Tools/Capture/README.md`. Send the zip `prepare-sniff.bat` makes, never the raw
`.pcapng`.

## What never goes into a commit

- **Recordings and anything decoded from them.** They live in your `CAPTURES`
  folder, outside the repository. A recording carries an account name, a login
  exchange and private messages.
- **Anybody's real character or account.** When a test or a document needs a
  captured packet, replace the character names, character ids and account
  details with made-up values of the same length and update the bytes to match -
  the protocol tests use ids like `0x0A0B0C01` and names like `Alpha`. Dialogue
  the live server addressed to the recording player uses `{name}`, which the
  server fills in with whoever is talking.
- **Funcom's raw client files.** `ResourceDatabase.dat` and its `.idx`, and the
  extractor's intermediate `.dat` caches, are ignored and stay that way. What the
  extractor converts them into is OmniCell's own format and is committed: the
  `OmniCell/Datafiles/*.ocp` content packs and `itemnames.sql`.
- **Your own settings.** `paths.cfg` and `Config.local.xml` hold your folders and
  your database password. Both are ignored; edit those, not `paths.example.cfg`
  or `Config.xml`.
- **Build output and downloaded tools** - `Built/`, `bin/`, `obj/`,
  `packages/`, `NuGet.exe`.

Run `git status` before you commit. If something shows up that is not a change
you meant to make, find out why before adding it.

## Conventions

- **Match the code around you** - its naming, its comments, its layout.
- **Keep existing copyright headers exactly as they are.** CellAO's BSD headers,
  WCell's and SmokeLounge's stay unchanged. A file OmniCell adds carries an
  OmniCell header; one added inside `AOtomation.Messaging` also says it is an
  addition to SmokeLounge.AOtomation.Messaging. `THIRD-PARTY-NOTICES.md` explains
  why.
- **Say where a protocol fact came from.** A capture, or the client's own code.
  A value nobody has observed stays unknown rather than guessed, and a value
  OmniCell chose, such as a drop rate, is labelled as OmniCell's.
- **Line endings are handled by `.gitattributes`.** Batch files are checked out
  with CRLF, which cmd needs.

## Where things stand

`OmniCell/Documentation/Remaining.md` is the triage of protocol work still open,
and `OmniCell/Documentation/protocol.html` holds the packet pages behind it.
`OmniCell/Documentation/DataAudit.md` says what the client archive contains
against what the server actually reads.

## Licence

OmniCell is GPL v3 (`LICENSE`). By contributing you agree your contribution is
distributed under the same licence. `THIRD-PARTY-NOTICES.md` describes the parts
that come from elsewhere.
