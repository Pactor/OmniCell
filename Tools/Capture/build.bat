@echo off
setlocal
title OmniCell - build the capture tools

rem ---------------------------------------------------------------------------
rem Compiles the capture helpers, and copies the runtime assemblies they need
rem out of the server build.
rem
rem The binaries are not in git, only the sources, so run this once after
rem cloning and again whenever a .cs here changes. Everything it produces goes
rem into bin\ beside this script; the scripts here run the tools from there.
rem
rem Build the OmniCell solution in Release first - this copies from its output.
rem ---------------------------------------------------------------------------

set "HERE=%~dp0"
set "BIN=%HERE%bin\"
set "BUILT=%HERE%..\..\OmniCell\Built\Release"
set "CSC=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
set "FACADES=%ProgramFiles(x86)%\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.8\Facades"

if not exist "%CSC%" (
  echo ERROR: csc.exe not found at:
  echo   %CSC%
  echo .NET Framework 4.x is required.
  pause
  exit /b 1
)

if not exist "%BUILT%\SmokeLounge.AOtomation.Messaging.dll" (
  echo ERROR: server build output not found at:
  echo   %BUILT%
  echo Build OmniCell.sln in Release first.
  pause
  exit /b 1
)

if not exist "%BIN%" mkdir "%BIN%"

echo.
echo   Copying runtime assemblies...
for %%D in (
  SmokeLounge.AOtomation.Messaging.dll
  ICSharpCode.SharpZipLib.dll
  OmniCell.Enums.dll
  OmniCell.Core.dll
  OmniCell.Database.dll
  OmniCell.Interfaces.dll
  OmniCell.ObjectManager.dll
  OmniCell.Stats.dll
  Cell.Core.dll
  PlayfieldLoader.dll
  Utility.dll
  MsgPack.dll
  NLog.dll
  Dapper.dll
  MySqlConnector.dll
) do (
  if exist "%BUILT%\%%D" (
    copy /Y "%BUILT%\%%D" "%BIN%%%D" >nul
    echo     %%D
  ) else (
    echo     MISSING: %%D
  )
)

echo.
echo   Compiling...

rem Five of these lines used to be broken. A backslash-n inside
rem "%FACADES%\netstandard.dll" had been eaten somewhere along the way and the
rem line split in two, so AreaExtract, AreaDump, QuestGivers, MobMeshes and
rem ZoneProbe could not be built by this script at all - it reported a failure
rem and stopped. Hence NETSTANDARD, built once, used everywhere.
set "NETSTANDARD=%FACADES%\netstandard.dll"
set "ZIP=%BIN%ICSharpCode.SharpZipLib.dll"
set "MSG=%BIN%SmokeLounge.AOtomation.Messaging.dll"

call :one FollowToCsv
if errorlevel 1 goto :failed

call :one PcapDecode "-r:%ZIP%" "-r:%NETSTANDARD%"
if errorlevel 1 goto :failed

call :one MarkReport
if errorlevel 1 goto :failed

call :one ChatMarks
if errorlevel 1 goto :failed

call :one Scrub
if errorlevel 1 goto :failed

call :one WireAudit "-r:%ZIP%" "-r:%NETSTANDARD%"
if errorlevel 1 goto :failed

rem AreaExtract reads the server's own types rather than a copy of them - a
rem playfield, a statel, an identity - so it wants most of what the server was
rem built from. That is on purpose: a second definition of a statel that drifted
rem from the first would extract the wrong thing and look right doing it.
call :one AreaExtract "-r:%ZIP%" "-r:%NETSTANDARD%" "-r:%MSG%" "-r:%BIN%OmniCell.Core.dll" "-r:%BIN%OmniCell.Database.dll" "-r:%BIN%OmniCell.Enums.dll" "-r:%BIN%OmniCell.Interfaces.dll" "-r:%BIN%Utility.dll" "-r:%BIN%MsgPack.dll"
if errorlevel 1 goto :failed

echo.
echo   Done.
echo.
echo     capture-marked.bat       record a session, labelling what you are doing
echo     decode-all-streams.bat   decode a recording
echo     prepare-sniff.bat        turn a recording into a file safe to share
echo.
pause
exit /b 0

:one
set "NAME=%~1"
shift
set "REFS="
:refs
if "%~1"=="" goto :compile
set "REFS=%REFS% "%~1""
shift
goto :refs
:compile
"%CSC%" -nologo -out:"%BIN%%NAME%.exe" %REFS% "%HERE%%NAME%.cs"
if errorlevel 1 (
  echo     %NAME%.exe  FAILED
  exit /b 1
)
echo     %NAME%.exe
exit /b 0

:failed
echo.
echo   BUILD FAILED
echo.
pause
exit /b 1
