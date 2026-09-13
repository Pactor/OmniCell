@echo off
setlocal enabledelayedexpansion
title OmniCell - decode every AO stream in a capture

rem ---------------------------------------------------------------------------
rem Decodes every game connection in a recording from capture-marked.bat.
rem
rem One account gives one zone stream. Run two accounts at once - to group, to
rem buff someone, to watch a team message arrive - and the recording holds two,
rem so every stream is decoded rather than a chosen one. Each becomes its own
rem pair of files:
rem
rem     stream_<N>.csv        what WireAudit and PcapDecode read
rem     stream_<N>.txt        the decoded messages
rem
rem Two accounts do not spoil a capture. They are the only way to see both ends
rem of the same event: the invite the server sends the inviter and the one it
rem sends the invitee are different messages, and one client only ever shows you
rem half of it. Decode both streams and diff them.
rem
rem Drag a .pcapng onto this file, or double click it to use the newest one.
rem ---------------------------------------------------------------------------

set "TSHARK=C:\Program Files\Wireshark\tshark.exe"
set "BIN=%~dp0bin\"
call "%~dp0capture-dir.bat" || exit /b 1
set "PCAP=%~1"

if "%PCAP%"=="" (
  for /f "delims=" %%F in ('dir /b /o-d "%OUTDIR%\*.pcapng" 2^>nul') do (
    set "PCAP=%OUTDIR%\%%F"
    goto :gotfile
  )
)
:gotfile

if not exist "%PCAP%" (
  echo ERROR: no capture file found.
  pause
  exit /b 1
)

rem A prefix taken from the capture's own name, so decoding a second
rem capture cannot overwrite the first one's streams. That happened on
rem 2026-09-11: two captures an hour apart both wrote stream_3.csv, and the
rem earlier one was gone until it was decoded again.
rem
rem marked-20260911-163012_00001_20260911163013 becomes 20260911-163012.
for %%F in ("%PCAP%") do set "STEM=%%~nF"
set "TAG=%STEM:marked-=%"
for /f "tokens=1 delims=_" %%A in ("%TAG%") do set "TAG=%%A"

echo.
echo   Capture : %PCAP%
echo   Prefix  : %TAG%_
echo.

rem The zone port is not fixed - 7501 and 7512 have both been it - so take every
rem stream in the AO range and let the packet count say which ones are real.
set "PORTS=tcp.port>=7100 and tcp.port<=7999 and tcp.len>0"

"%TSHARK%" -r "%PCAP%" -T fields -e tcp.stream -Y "%PORTS%" > "%CAPTURES%\streams.tmp" 2>nul
rem tcp.stream comes back once per packet, so this is tens of thousands of
rem lines holding a few dozen distinct numbers. Sorted to unique in one call.
rem
rem What stood here walked the whole list and ran a findstr per line to spot
rem the repeats - one process per packet, sixty thousand of them on a quarter
rem hour of play, and it did not finish.
powershell -NoProfile -Command "Get-Content -LiteralPath '%CAPTURES%\streams.tmp' | Where-Object { $_ -match '^\d+$' } | Sort-Object { [int]$_ } -Unique | Set-Content -LiteralPath '%CAPTURES%\streams.txt'"

set "COUNT=0"
for /f "usebackq tokens=1" %%S in ("%CAPTURES%\streams.txt") do (
  if 1==1 (
    set /a COUNT+=1
    echo   --- stream %%S
    "%TSHARK%" -r "%PCAP%" -q -z follow,tcp,raw,%%S > "%CAPTURES%\follow_s%%S.txt" 2>nul
    "%BIN%FollowToCsv.exe" "%CAPTURES%\follow_s%%S.txt" %%S "%CAPTURES%\%TAG%_s%%S.csv"
    "%BIN%PcapDecode.exe" "%CAPTURES%\%TAG%_s%%S.csv" "%BIN%SmokeLounge.AOtomation.Messaging.dll" > "%CAPTURES%\%TAG%_s%%S.txt" 2>&1
    findstr /r /c:"^[0-9][0-9]* packets" "%CAPTURES%\%TAG%_s%%S.txt"
    del "%CAPTURES%\follow_s%%S.txt" >nul 2>&1
  )
)
del "%CAPTURES%\streams.tmp" "%CAPTURES%\streams.txt" >nul 2>&1

echo.
echo   %COUNT% stream(s) decoded into %CAPTURES%\%TAG%_s^<N^>.csv / .txt
echo.
echo   Round-trip them through the model:
echo       bin\WireAudit.exe "%CAPTURES%\%TAG%_s*.csv"
echo.
pause
