@echo off
setlocal enabledelayedexpansion
title OmniCell - prepare a session for sharing

rem ---------------------------------------------------------------------------
rem Turns a recorded session into a file that is safe to hand to somebody else.
rem
rem Drag a .pcapng onto this file, or double click it to use the newest one.
rem
rem What comes out is not the recording. The recording is a packet capture: it
rem has your addresses, your login exchange, and whatever else on your network
rem the filter let through. What comes out is your game client's conversation
rem with the zone servers, expanded and written down - which is the part that is
rem any use to anybody, and the only part.
rem
rem The safety is in what is never copied, not in editing afterwards. That is
rem deliberate: half of a zone session is one continuous compressed stream, so a
rem name cannot be lifted out of the middle of it without rebuilding everything
rem after it, and a tool that offered to scrub a capture would be promising
rem something it could not do.
rem
rem Every connection in the recording is decoded here, and then each one has to
rem prove what it is before any of it is copied. A zone server's replies are
rem compressed and nothing else's are; that is the test. Anything that fails it
rem is left behind, including anything unrecognised.
rem ---------------------------------------------------------------------------

set "TSHARK=C:\Program Files\Wireshark\tshark.exe"
set "BIN=%~dp0bin\"
call "%~dp0capture-dir.bat" || exit /b 1
set "SHARE=%CAPTURES%\share"
set "WORK=%CAPTURES%\prepare"
if not exist "%SHARE%" mkdir "%SHARE%"

if not exist "%TSHARK%" (
  echo ERROR: tshark not found at:
  echo   %TSHARK%
  echo Install Wireshark, or edit the TSHARK line in this file.
  pause
  exit /b 1
)

if not exist "%BIN%Scrub.exe" (
  echo ERROR: the helpers are not built. Run build.bat first.
  pause
  exit /b 1
)

set "PCAP=%~1"
if "%PCAP%"=="" (
  for /f "delims=" %%F in ('dir /b /o-d "%OUTDIR%\*.pcapng" 2^>nul') do (
    set "PCAP=%OUTDIR%\%%F"
    goto :gotfile
  )
)
:gotfile

if not exist "%PCAP%" (
  echo ERROR: no recording found.
  echo Record one with capture-marked.bat, or drag a .pcapng onto this file.
  pause
  exit /b 1
)

cls
echo.
echo   ==========================================================
echo    PREPARING A SESSION FOR SHARING
echo   ==========================================================
echo.
echo    From : %PCAP%
echo.
echo    These are found and LEFT BEHIND, every time:
echo.
echo      * the login exchange - it carries your account name in
echo        plain text, your encrypted password, and the name of
echo        every character on your account
echo      * the chat server - tells, org chat and private groups
echo      * every packet header - your IP address, the server's,
echo        your network card's hardware address, ports, timings
echo      * every other program's traffic the recording caught
echo      * anything that cannot prove it is a game zone server
echo.
echo    What is kept is your client talking to the zone servers:
echo    where you walked, what you looked at, fought, bought and
echo    talked to. Your character's name is in that, the same as
echo    it is for anyone standing next to you in the game.
echo.
echo   ----------------------------------------------------------
echo.
echo    You can name anything else to keep out - an account name,
echo    an email address, a friend's character. Each one is
echo    searched for and reported, then forgotten. Enter on a
echo    blank line when done, or straight away to skip.
echo.

if exist "%WORK%" rd /s /q "%WORK%"
mkdir "%WORK%"
mkdir "%WORK%\raw"
set "SECRETS=%WORK%\check.txt"
type nul > "%SECRETS%"

:asking
set "WORD="
set /p "WORD=keep out> "
if not defined WORD goto :asked
echo !WORD!>> "%SECRETS%"
goto :asking
:asked

echo.
echo   Reading the recording. Every connection in it is expanded
echo   before any of them is judged.
echo.

rem A coarse net, not a decision. The game's servers live in this range, and
rem skipping everything outside it keeps the browser tabs and printers off the
rem list. What is actually shared is decided afterwards, by content.
set "RANGE=tcp.len>0 and tcp.port>=7000 and tcp.port<=7999"

"%TSHARK%" -r "%PCAP%" -T fields -e tcp.stream -Y "%RANGE%" 2>nul > "%WORK%\streams.tmp"
rem tcp.stream comes back once per packet, so this is tens of thousands of
rem lines holding a few dozen distinct numbers. Sorted to unique in one call:
rem what stood here walked the whole list and ran a findstr per line to spot
rem the repeats, which is one process per packet - sixty thousand of them on a
rem quarter hour of play, and it did not finish.
powershell -NoProfile -Command "Get-Content -LiteralPath '%WORK%\streams.tmp' | Where-Object { $_ -match '^\d+$' } | Sort-Object { [int]$_ } -Unique | Set-Content -LiteralPath '%WORK%\streams.txt'"

for %%F in ("%PCAP%") do set "STEM=%%~nF"
set "TAG=%STEM:marked-=%"
for /f "tokens=1 delims=_" %%A in ("%TAG%") do set "TAG=%%A"
if "%TAG%"=="" set "TAG=session"

rem Every connection in one pass. tshark re-reads the whole capture for each
rem -z it is given on its own, so asking for them one at a time turned an
rem eighteen megabyte recording into forty minutes of work. Asked for all of
rem them together it is a single pass, and FollowToCsv --split cuts the result
rem back into one file per connection.
set "FOLLOWS="
set "COUNT=0"
for /f "usebackq tokens=1" %%S in ("%WORK%\streams.txt") do (
  set /a COUNT+=1
  set "FOLLOWS=!FOLLOWS! -z follow,tcp,raw,%%S"
)

if %COUNT%==0 (
  echo.
  echo   No game traffic in this recording - was the game running?
  echo   Nothing was written.
  rd /s /q "%WORK%"
  pause
  exit /b 1
)

echo     %COUNT% connection(s) to read...
"%TSHARK%" -r "%PCAP%" -q %FOLLOWS% > "%WORK%\raw\all.txt" 2>nul
"%BIN%FollowToCsv.exe" "%WORK%\raw\all.txt" --split "%WORK%\raw" "%TAG%"
del "%WORK%\raw\all.txt" "%WORK%\streams.tmp" "%WORK%\streams.txt" >nul 2>&1

echo     expanding each one...
set "PAIRS="
for %%F in ("%WORK%\raw\%TAG%_s*.csv") do (
  "%BIN%PcapDecode.exe" "%%~fF" "%BIN%SmokeLounge.AOtomation.Messaging.dll" > "%%~dpnF.txt" 2>&1
  set "PAIRS=!PAIRS! "%%~fF" "%%~dpnF.txt""
)

echo.
echo   Deciding which of them may be shared...
echo.

set "OUTDIR2=%WORK%\out"
"%BIN%Scrub.exe" "%OUTDIR2%" "%WORK%\CONTENTS.txt" "%SECRETS%" %PAIRS%
set "VERDICT=%ERRORLEVEL%"

rem The words to check for go now, before anything else can go wrong, and
rem before anything is packaged.
del "%SECRETS%" >nul 2>&1

if not "%VERDICT%"=="0" (
  echo.
  echo   ==========================================================
  echo    STOPPED - NOTHING WAS PACKAGED
  echo   ==========================================================
  echo.
  echo    The reason is above. No shareable file was made.
  echo.
  echo    The working files are still in:
  echo      %WORK%
  echo    They are NOT safe to share. Delete them when you are done.
  echo.
  pause
  exit /b 1
)

rem Labels typed while recording, if there are any. Your own words about what
rem you were doing, and the most useful thing in the whole file.
set "MARKS=%OUTDIR%\%STEM%.marks.txt"
if not exist "%MARKS%" (
  for %%F in ("%PCAP%") do set "BASE=%%~nF"
  for /f "tokens=1,2 delims=_" %%A in ("!BASE!") do set "MARKS=%OUTDIR%\%%A.marks.txt"
)
if exist "%MARKS%" copy /Y "%MARKS%" "%OUTDIR2%\labels.txt" >nul
copy /Y "%WORK%\CONTENTS.txt" "%OUTDIR2%\CONTENTS.txt" >nul

set "OUT=%SHARE%\omnicell-session-%TAG%.zip"
if exist "%OUT%" del "%OUT%"

powershell -NoProfile -Command "Compress-Archive -Path '%OUTDIR2%\*' -DestinationPath '%OUT%' -Force" >nul 2>&1

if not exist "%OUT%" (
  echo   ERROR: could not make the zip. The files are in %OUTDIR2%.
  pause
  exit /b 1
)

rem Everything that was not packaged goes now. The raw folder holds the login
rem exchange, expanded and readable, and leaving that lying about would undo
rem the whole point of this.
rd /s /q "%WORK%"

cls
echo.
echo   ==========================================================
echo    SUCCESS - SAFE TO SHARE
echo   ==========================================================
echo.
echo    %OUT%
echo.
for %%F in ("%OUT%") do echo    %%~zF bytes
echo.
echo    Your account name, your password and your character list
echo    were found in this recording and were NOT copied into it.
echo    Neither were your tells, your IP address, or anything from
echo    any other program on your machine.
echo.
echo    CONTENTS.txt inside the zip lists every connection that was
echo    included, every one that was left behind and why, and what
echo    kinds of message are in it. It is plain text and it is meant
echo    to be read before you send it.
echo.
echo    The original recording is untouched:
echo      %PCAP%
echo    That one is NOT safe to share - it is the file with
echo    everything still in it. Delete it when you no longer need it.
echo.
pause
