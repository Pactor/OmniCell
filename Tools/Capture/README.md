# Recording a session

The server is written by watching the real one. A recorded session of somebody
playing Anarchy Online is the only source of truth for what the protocol
actually does, and one afternoon of ordinary play answers questions that months
of guessing would not.

If you want to help, this is the most useful thing you can do, and you do not
have to know anything about the protocol to do it.

You need [Wireshark](https://www.wireshark.org/) installed, including Npcap,
which its installer offers by default. Set `CAPTURES` and `CAPTURE_INTERFACE`
in `paths.cfg` first — `SETUP.md` in the root of the repository explains both.

## Recording one

```
build.bat              once, after cloning
capture-marked.bat     start this, then play
prepare-sniff.bat      turn the recording into something you can send
```

`capture-marked.bat` records while you play and lets you label what you are
doing. Alt-tab back to it whenever you are about to do something worth
identifying and type what it is:

```
mark> using the extinguisher
mark> equipping the weapon
mark> handing the quest in
```

Each label is stamped with the moment you pressed enter, which is how an
unnamed message gets identified: something happened at 14:32:07 and one message
went out at 14:32:07. Type `stop` to finish.

Labelling is worth doing but is not required. An unlabelled recording of
ordinary play is still worth having — most of what is learned from these comes
out of traffic nobody thought to annotate.

## Sending it

**Do not send the `.pcapng`.** That is the raw recording and it has everything
in it — your account name, your encrypted password, your IP address, and
whatever else on your network the capture caught.

Run `prepare-sniff.bat` instead. It produces a zip in the `share` folder inside
your captures folder that is safe to hand over, and tells you exactly what it
left out.

### What it leaves behind

- **The login exchange.** Your account name travels in it in plain text, along
  with the encrypted password and the name of every character on the account.
  It is not copied.
- **The chat server.** Tells, org chat and private groups are carried on a
  separate connection. It is not copied.
- **Every packet header** — your IP address, the server's, your network card's
  hardware address, ports, timings, routing. The format that comes out holds
  message payload and nothing else, so none of that exists in it to remove.
- **Everything else your machine was doing.** Other programs, other tabs.
- **Anything that cannot prove it is a game zone server.**

You can also name anything else you would rather keep out — an account name, an
email address, a friend's character. It searches for each one and reports what
it found, then forgets them.

### What is in it, on purpose

- Your character's name, which is what anyone standing next to you already
  sees.
- Where you walked, what you looked at, fought, bought and talked to.
- Public and area chat you were close enough to hear.

### How it decides

Not by port number, which cannot tell them apart. On the live servers 7505 is a
zone server on one machine and the login server on another: one recorded
session held fourteen connections on port 7505, of which exactly one was the
login. Deciding by port would have shared an account name, an encrypted
password and a list of every character on the account.

So a connection is included only if it proves what it is. A zone server's
replies are compressed and nothing else's are, and that is the test. Anything
that fails it is left out, including anything unrecognised — the failure mode
is a smaller file, never a leakier one.

Nothing is edited. That is deliberate, and it is why the tool works this way
round: half of a zone session is one continuous compressed stream, so a name
cannot be lifted out of the middle of it without rebuilding every byte that
follows. Anything claiming to have scrubbed a packet capture would be claiming
something it had not done.

`CONTENTS.txt` inside the zip lists every connection that was included, every
one that was left behind and why, and what kinds of message are in it. It is
plain text and it is meant to be read before you send anything.

The original recording is left where it is, untouched. Delete it when you no
longer need it.

## Where things go

Nothing recorded is written inside the repository. `capture-dir.bat` puts it
all in the folder `CAPTURES` names in `paths.cfg`:

```
<CAPTURES>\pcap\      the raw recordings, and the labels typed while making them
<CAPTURES>\share\     what prepare-sniff.bat produced - the safe ones
<CAPTURES>\           decoded streams, for working on
```

Not in `.gitignore` — outside the tree entirely. A list of ignore patterns only
has to be forgotten once, and it was: eighty-one stream files and twenty-seven
decoded transcripts were tracked in this directory until September 2026.

---

# Reading one back

For working on the protocol rather than contributing to it.

`decode-all-streams.bat` names and expands every game connection in a
recording, one file each — which also covers a recording with two clients in it.

| | |
| --- | --- |
| `FollowToCsv.cs` | tshark's reassembly into the csv everything else reads |
| `PcapDecode.cs` | inflates the server's half and names every message |
| `Scrub.cs` | decides what may be shared, and writes CONTENTS.txt |
| `MarkReport.cs`, `ChatMarks.cs` | labels, from the console and from in-game chat |
| `WireAudit.cs` | the round-trip audit |
| `AreaExtract.cs` | a whole playfield out of the traffic — spawn points, shops, quests, dialogue — as SQL |

`build.bat` compiles all of the above into `bin/`. Build `OmniCell.sln` in
Release first — it copies the assemblies it needs out of `OmniCell/Built/Release`
into `bin/` as well.
