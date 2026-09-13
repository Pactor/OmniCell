@echo off
rem ---------------------------------------------------------------------------
rem Where captured traffic lives. Called by every capture and decode script.
rem
rem Outside the git repository, deliberately. A capture is a live Anarchy
rem Online session from a real account: it carries the login handshake, the
rem account name and the credential blob, every private message and every
rem playfield the character walked through. None of that belongs in a
rem repository that is going to be public, and .gitignore is the wrong place to
rem rely on - it only has to be forgotten once.
rem
rem The folder comes from paths.cfg in the root of the repository (copy
rem paths.example.cfg to paths.cfg and set CAPTURES - see SETUP.md). A CAPTURES
rem environment variable, if set, takes precedence. There is no built-in
rem default: nothing here assumes a drive or a folder.
rem
rem     CAPTURES            session data - reassembled streams, decoded text
rem     OUTDIR              the .pcapng files themselves, and their .marks.txt
rem     CAPTURE_INTERFACE   the network adapter to record on, if set; only the
rem                         capture script needs it, and it checks for itself
rem
rem Both folders are created if they do not exist. Returns errorlevel 1 when
rem CAPTURES is not set, and the calling script stops.
rem ---------------------------------------------------------------------------

set "OMNICELL_ROOT=%~dp0..\.."
for %%D in ("%OMNICELL_ROOT%") do set "OMNICELL_ROOT=%%~fD"

if exist "%OMNICELL_ROOT%\paths.cfg" (
  for /f "usebackq eol=# tokens=1,* delims==" %%A in ("%OMNICELL_ROOT%\paths.cfg") do (
    if /i "%%A"=="CAPTURES" if not defined CAPTURES set "CAPTURES=%%B"
    if /i "%%A"=="CAPTURE_INTERFACE" if not defined CAPTURE_INTERFACE set "CAPTURE_INTERFACE=%%B"
  )
)

if not defined CAPTURES (
  echo.
  echo   ERROR: no captures folder is set.
  echo.
  echo   Copy paths.example.cfg to paths.cfg in:
  echo     %OMNICELL_ROOT%
  echo   and set CAPTURES to a folder outside the repository. See SETUP.md.
  echo.
  pause
  exit /b 1
)

rem Collapse any .. so what the scripts echo is readable.
for %%D in ("%CAPTURES%") do set "CAPTURES=%%~fD"

set "OUTDIR=%CAPTURES%\pcap"

if not exist "%CAPTURES%" mkdir "%CAPTURES%"
if not exist "%OUTDIR%" mkdir "%OUTDIR%"
exit /b 0
