@echo off
setlocal enabledelayedexpansion
title OmniCell - annotated capture

rem ---------------------------------------------------------------------------
rem Captures a session while you label what you are doing.
rem
rem Start this, then alt-tab to the game. Whenever you are about to do something
rem worth identifying, come back here and type what it is:
rem
rem     mark> using extinguisher
rem     mark> equipping weapon
rem
rem Each label is stamped with the time you pressed enter, so the packets that
rem went out around it can be found afterwards. That is how an unnamed message
rem gets identified. Labels are optional; a recording without any is still
rem worth having.
rem
rem The adapter to record on is CAPTURE_INTERFACE in paths.cfg - see SETUP.md.
rem
rem Type stop to end the capture.
rem ---------------------------------------------------------------------------

set "TSHARK=C:\Program Files\Wireshark\tshark.exe"
call "%~dp0capture-dir.bat" || exit /b 1
set "FILTER=ip and tcp and not host 127.0.0.1 and not port 443 and not port 80 and not port 53"

if not exist "%TSHARK%" (
  echo ERROR: tshark not found at:
  echo   %TSHARK%
  pause
  exit /b 1
)

rem Which adapter the game's traffic goes through differs from one machine to
rem the next, so it is never guessed. Recording on the wrong one records
rem nothing and says nothing about it.
if not defined CAPTURE_INTERFACE (
  echo.
  echo   ERROR: no network adapter is set.
  echo.
  echo   Set CAPTURE_INTERFACE in paths.cfg to the adapter your game traffic goes
  echo   through - the name in brackets in this list. See SETUP.md.
  echo.
  "%TSHARK%" -D
  echo.
  pause
  exit /b 1
)


for /f %%T in ('powershell -NoProfile -Command "Get-Date -Format yyyyMMdd-HHmmss"') do set "STAMP=%%T"
set "OUT=%OUTDIR%\marked-%STAMP%.pcapng"
set "MARKS=%OUTDIR%\marked-%STAMP%.marks.txt"

echo # marks for %OUT%> "%MARKS%"

echo.
echo   Adapter : %CAPTURE_INTERFACE%
echo   Output  : %OUT%
echo   Marks   : %MARKS%
echo.
echo   Starting capture...

start "OmniCell capture" /min "%TSHARK%" -i "%CAPTURE_INTERFACE%" -f "%FILTER%" -w "%OUT%" -b filesize:200000 -q

timeout /t 3 /nobreak >nul
tasklist /FI "IMAGENAME eq tshark.exe" 2>nul | find /i "tshark.exe" >nul
if errorlevel 1 (
  echo   ERROR: tshark did not start. Try running as Administrator.
  pause
  exit /b 1
)

cls
echo.
echo   ==========================================================
echo    CAPTURE RUNNING - %OUT%
echo   ==========================================================
echo.
echo    Type what you are about to do, then press enter, then do
echo    it in game. Short labels are best:
echo.
echo        using extinguisher
echo        equipping weapon
echo        opening container
echo.
echo    Type  stop  to finish.
echo.

:loop
set "LABEL="
set /p "LABEL=mark> "
if not defined LABEL goto :loop
if /i "%LABEL%"=="stop" goto :done
if /i "%LABEL%"=="quit" goto :done
if /i "%LABEL%"=="exit" goto :done

for /f %%E in ('powershell -NoProfile -Command "[DateTimeOffset]::Now.ToUnixTimeMilliseconds()"') do set "EPOCH=%%E"
for /f "tokens=*" %%C in ('powershell -NoProfile -Command "Get-Date -Format HH:mm:ss"') do set "CLOCK=%%C"
echo !EPOCH!^|!CLOCK!^|!LABEL!>> "%MARKS%"
echo        [!CLOCK!] marked: !LABEL!
goto :loop

:done
echo.
echo   Stopping capture...
taskkill /F /IM tshark.exe >nul 2>&1
timeout /t 2 /nobreak >nul

echo.
echo   Capture : %OUT%
echo   Marks   : %MARKS%
echo.
type "%MARKS%"
echo.
echo   Next: decode-all-streams.bat to decode it, or prepare-sniff.bat to make a
echo   copy that is safe to share.
echo.
pause
