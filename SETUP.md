# Setting up your folders

Nothing in this repository knows where anything lives on your machine. The
folders that hold data - your recorded sessions and your Anarchy Online client -
are set in one file, `paths.cfg`, in the root of the repository, and every tool
that needs one of them reads it from there.

## Make your paths.cfg

1. Copy `paths.example.cfg` to `paths.cfg`, in the same folder.
2. Open `paths.cfg` and fill in each line with a folder on your machine.

`paths.cfg` is listed in `.gitignore`, so your folders never reach a commit.
Leave `paths.example.cfg` as it is - it is the blank template everyone copies.

The format is one setting per line, `NAME=path`, with no quotes. Lines that
start with `#` are notes. For example:

```
CAPTURES=D:\ao-captures
CAPTURE_INTERFACE=Ethernet
AO_CLIENT=C:\Games\Anarchy Online
```

An environment variable with the same name, if it is set, is used instead of
the value in the file.

## The settings

| Setting | What it is | Read by |
| --- | --- | --- |
| `CAPTURES` | Where recorded sessions and everything decoded from them are written. Pick a folder **outside** the repository; recordings carry account details and must never be committed. It is created if it does not exist, with the raw recordings in a `pcap` folder inside it. | `Tools\Capture\capture-dir.bat`, and through it every capture and decode script (`capture-marked.bat`, `decode-all-streams.bat`, `prepare-sniff.bat`) |
| `CAPTURE_INTERFACE` | The network adapter your game traffic goes through, as Wireshark names it - for example `Wi-Fi` or `Ethernet`. `tshark -D` lists them, with the name in brackets. Only needed for recording. | `Tools\Capture\capture-marked.bat`, through `capture-dir.bat` |
| `AO_CLIENT` | Your Anarchy Online client install - the folder that contains `cd_image`. Only read, never copied into the repository. | `Tools\Algorithman\Extractor Serializer` (builds `items.ocp`, `nanos.ocp` and `playfields.ocp`) |

If a tool needs a setting that is not set, it stops and says which one, rather
than guessing a folder:

- the capture scripts print where `paths.cfg` should be and exit;
- `capture-marked.bat` lists the adapters Wireshark can see when
  `CAPTURE_INTERFACE` is not set, and exits without recording;
- the Extractor Serializer asks for the client folder when `AO_CLIENT` is
  missing or does not point at a client install.

## Programs to install

These are not part of the repository.

- **Visual Studio 2022** (or its Build Tools) with .NET Framework 4.8, to build
  `OmniCell/OmniCell.sln` - in Visual Studio, or from a Developer Command Prompt
  with `msbuild OmniCell\OmniCell.sln -restore -p:Configuration=Release`. The
  first build needs an internet connection: it downloads `NuGet.exe` and the
  NuGet packages. See `README.md`.
- **MySQL 8** or **MariaDB 10.11** or newer, for the server's database. See
  `OmniCell/Documentation/Running-a-server.md`.
- **Wireshark**, including Npcap, only if you record sessions. The capture
  scripts expect it in its default install folder; if you installed it
  elsewhere, change the `TSHARK` line at the top of the script you run. See
  `Tools/Capture/README.md`.

Players connecting to a server need their own Anarchy Online client, as with
any other server.
