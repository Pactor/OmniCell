@echo off
setlocal
title OmniCell - build the capture tools

rem ---------------------------------------------------------------------------
rem Builds the capture helpers, and copies the runtime assemblies they need
rem out of the server build.
rem
rem The binaries are not in git, only the sources, so run this once after
rem cloning and again whenever a .cs here changes. Everything it produces goes
rem into bin\ beside this script; the scripts here run the tools from there.
rem
rem Build the OmniCell solution in Release first - this copies from its output.
rem The tools are .NET 10 programs, like the server; each has a project in
rem projects\ that compiles its one .cs file.
rem ---------------------------------------------------------------------------

set "HERE=%~dp0"
set "BIN=%HERE%bin\"
set "BUILT=%HERE%..\..\OmniCell\Built\Release"

where dotnet >nul 2>nul
if errorlevel 1 (
  echo ERROR: dotnet was not found.
  echo The .NET 10 SDK is required, the same one the server builds with.
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

rem AreaExtract reads the server's own types rather than a copy of them - a
rem playfield, a statel, an identity - so it wants most of what the server was
rem built from. That is on purpose: a second definition of a statel that drifted
rem from the first would extract the wrong thing and look right doing it. Its
rem project lists those assemblies; the copies above are what it compiles against.
for %%T in (FollowToCsv PcapDecode MarkReport ChatMarks Scrub WireAudit AreaExtract) do (
  call :one %%T
  if errorlevel 1 goto :failed
)

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
dotnet build "%HERE%projects\%~1.csproj" -c Release -nologo -v q
if errorlevel 1 (
  echo     %~1.exe  FAILED
  exit /b 1
)
echo     %~1.exe
exit /b 0

:failed
echo.
echo   BUILD FAILED
echo.
pause
exit /b 1
