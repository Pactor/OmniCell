# Third party notices

OmniCell is assembled from several independently licensed codebases. Every
original copyright header is retained in the source, and the licences below are
reproduced or referenced as each requires.

The combined work is distributed under the **GNU General Public License v3**
(`LICENSE`). That follows from the components rather than from preference:
`Cell.Core` and `Cell.Util` are GPL "version 2 or any later version", and
msgpack-cli is Apache 2.0, which is compatible with GPLv3 but not GPLv2. Taking
WCell's "any later version" option makes v3 the version that satisfies both.

File counts below were taken from tracked source in this repository and exclude
build output.

---

## CellAO — BSD 3-clause

The great majority of this codebase. OmniCell is a continuation of the CellAO
server emulator, developed from roughly 2005 and last released as
CellAO-NightPredator in 2016.

- Copyright (c) CellAO Team
- Full text: `LICENSE-CellAO.txt`
- Header retained in **532** source files

The notice appears with three different year ranges, all retained as written.
548 occurrences across those 532 files, a few of which carry it twice:

| notice | occurrences |
|---|---|
| Copyright (c) 2005-2014, CellAO Team | 511 |
| Copyright (c) 2005-2013, CellAO Team | 35 |
| Copyright (c) 2005-2012, CellAO Team | 2 |

A further 18 generated documentation files under `OmniCell/Documentation`
carry "Copyright (c) 2014 CellAO Team" in a footer.

CellAO-NightPredator ships no repository-level LICENSE file. The grant instead
lives in the per-file headers, each of which carries the complete BSD 3-clause
text: the copyright notice, all three conditions and the full warranty
disclaimer. `LICENSE-CellAO.txt` reproduces those terms at repository level for
convenience; it does not replace or restate them.

The BSD terms require that the notice, conditions and disclaimer be retained in
redistributed source. They are, in every file that carried them. Per the third
clause, the CellAO Team's name is not used to endorse or promote OmniCell.

Files OmniCell has added to these directories carry a second copyright line,
`Copyright (c) 2026, OmniCell contributors`, under the retained CellAO one.
Until 2026-09-10 ten of them carried only the CellAO notice, which named the
CellAO Team as the copyright holder of work they had not written; they now
carry both.

---

## WCell — GNU GPL v2 or later

The networking core: `OmniCell/Libraries/Source/Cell.Core` and `Cell.Util`.
This is the "Cell" in both CellAO and OmniCell, and it is the reason the
combined work is GPL.

- Copyright (C) The WCell Team, info@wcell.org
- http://www.wcell.org/

The grant, as it appears in the source:

> This program is free software; you can redistribute it and/or modify it under
> the terms of the GNU General Public License as published by the Free Software
> Foundation; either version 2 of the License, or (at your option) any later
> version.

Those two directories hold **37** source files, of which **13** carry that
header explicitly. The remainder are unmarked, but share the same provenance and
are treated as WCell-derived. These directories deliberately keep their original
names so that provenance stays visible.

### Code vendored into WCell by third parties

Six files in `Cell.Util/Collections` were written by others and modified by
WCell. Each reserves rights to its original author and **states no licence
grant**:

| author | files | source |
|---|---|---|
| Julian M Bucknall | `LockfreeQueue.cs`, `LockfreeStack.cs`, `PriorityQueue.cs`, `SingleLinkNode.cs` | boyet.com |
| Wilco Bauwer | `BaseImmutableDictionary.cs`, `ImmutableDictionary.cs` | wilcob.com |

Their headers read "Written by/rights held by <author> ... Modified by WCell".
Both were published as public article code in 2004 and 2008 respectively and
have been redistributed inside WCell, and therefore inside CellAO, for over a
decade. The attribution required by those headers is preserved here, but the
absence of an explicit grant is a genuine open question inherited from upstream
rather than something OmniCell resolved. It is recorded here rather than
papered over. Anyone redistributing this code should be aware of it; the
affected files are small, self-contained collection classes and could be
replaced with `System.Collections.Concurrent` equivalents if that matters.

---

## SmokeLounge AOtomation — WTFPL v2

The Anarchy Online protocol layer: message definitions and serialisation, under
`OmniCell/Libraries/Source/AOtomation`.

- Copyright (c) 2013 SmokeLounge
- Do What The Fuck You Want To Public License, Version 2
- Full text: `OmniCell/Libraries/Source/AOtomation.Messaging/COPYING`
- Copyright notice retained in **170** of the 250 vendored files
- A further 70 name SmokeLounge only in a namespace or a path and carried
  no notice upstream either; 10 mention it nowhere

This was a git submodule and is now vendored into the repository, at upstream
`github.com/CellAO/AOtomation.Messaging` commit
`107173e580b26414ff4bd5e18b993cd0659ea21e` (Algorithman, 2014-06-14), which is
where to look to diff against the original.

The layout is no longer upstream's. AOtomation.Messaging was its own repository
and arrived with its own solution, key file, StyleCop settings and a `src`/
`test` split, which left the project sitting four directories down inside
`Libraries/Source` with a `src` in the middle of it. On 2026-09-10 that was
flattened: `src` and `test` are gone, the project and its tests sit directly
under `Libraries/Source/AOtomation.Messaging`, and upstream's own solution file
and its ReSharper settings were deleted - nothing referenced them, since
`OmniCell.sln` names the project file directly. `COPYING` and the key file
moved with it. Diffing against upstream now means accounting for that move; the
files themselves are unchanged by it. Its StyleCop settings file was later
deleted too: no project runs StyleCop, and the copyright text it held is in
`COPYING` and in every SmokeLounge file header.

It was brought in-tree because OmniCell has to extend it. The serialiser finds
message types through `type.Assembly.GetTypes()`, so a definition declared
anywhere else is never discovered, and live captures contain messages the 2013
definitions do not cover. Pinning a submodule to commits that exist only
locally would break cloning for everyone else.

The WTFPL permits this without qualification. Every SmokeLounge copyright
notice is retained unchanged, and `COPYING` travels with the code.

OmniCell has added **35** files to this tree since vendoring. Every one of them
carries an OmniCell copyright and says in its header that it is an addition to
SmokeLounge.AOtomation.Messaging, so the two are never confused - for example
`Messages/N3Messages/MoveItemMessage.cs`. A SmokeLounge notice appears only on
files SmokeLounge wrote.

Until 2026-09-10 that was the stated rule and not the practice: 22 of those 35
had been started by copying a neighbour and carried a 2013 SmokeLounge
copyright, and four carried no header at all. Both are fixed. The count above
was also wrong - it counted every file with the word SmokeLounge in it, most of
which only have it in a namespace.

---

## msgpack-cli — Apache License 2.0

MessagePack serialisation, used for the extracted client datafiles. Included as
a git submodule.

- Full text: `OmniCell/Libraries/Source/msgpack-cli/LICENSE.txt`

---

## NuGet dependencies

Resolved at build time and not redistributed in this repository. Licences below
were verified against the nuspec for the exact version referenced.

| package | version | licence | used by |
|---|---|---|---|
| Dapper | 1.13 | Apache 2.0 | Database, ZoneEngine |
| MySqlConnector | 2.4.0 | MIT | Database |
| NLog | 2.1.0 | BSD 3-clause | all engines |
| SharpZipLib | 1.4.2 | MIT | Utility, ZoneEngine |
| MemBus | 2.0.2 | Apache 2.0 | Core, Communication, Interfaces, ZoneEngine |
| MathNet.Numerics | 2.6.2 | MIT | Core |
| IrcDotNet | 0.4.1 | MIT | ChatEngine |

---

## Anarchy Online

Anarchy Online is copyright [Funcom](http://www.funcom.com/). OmniCell is not
affiliated with, endorsed by, or supported by Funcom, and contains no Funcom
content. Game data is extracted from a client that the operator supplies
themselves.
