@echo off
setlocal enabledelayedexpansion
title OmniCell - refresh the world from the bot's record

rem ---------------------------------------------------------------------------
rem Rebuilds everything that comes out of the AOBuddy10 bot's record, in the one
rem order that works, and writes the patch that populates the world.
rem
rem Run this whenever the bot has been out for a while. It only ever gets
rem better: the bot logs a body and a facing for every creature it sees now, so
rem the spawns that came out bodyless last time gain one as it re-sights them.
rem
rem Nothing here touches the database. It writes
rem SqlPatches/world-mobspawns.sql, and create-database.bat applies it - or,
rem to apply just this one:
rem
rem     mysql -u <user> -p <database> < ..\..\OmniCell\Libraries\Source\OmniCell.Database\SqlPatches\world-mobspawns.sql
rem
rem That patch clears only the id range it owns (200,000,000 upward), so an NPC
rem placed by hand with /npc survives it, and running it twice is the same as
rem running it once.
rem
rem The order matters:
rem   1. MobExtract   reads the mission recordings -> MissionMobs.tsv
rem   2. PoolExtract  folds that into missionpools.ocp, so a mission spawns the
rem                   creatures the bot actually met
rem   3. WorldMobs    reads the retail captures -> WorldMobs.tsv, the bodies for
rem                   creatures the bot saw before it started logging them
rem   4. WorldSpawns  joins the bot's places to those bodies -> the patch
rem ---------------------------------------------------------------------------

set "HERE=%~dp0"
set "BOT=E:\Funcom\AOBuddy10\Build\Plugins\AOBuddy"
set "NAV=E:\Funcom\AOBuddy10\AOBuddy\GameData\Nav"
set "SNIFFS=E:\Funcom\sniffs"
set "DATA=%HERE%..\..\OmniCell\Datafiles"
set "PATCHES=%HERE%..\..\OmniCell\Libraries\Source\OmniCell.Database\SqlPatches"

if not exist "%BOT%\mobs" (
  echo.
  echo   The bot's record is not where this expects it:
  echo     %BOT%
  echo   Edit BOT at the top of this file.
  echo.
  pause
  exit /b 1
)

echo.
echo   ==========================================================
echo    Refreshing the world from the bot's record
echo   ==========================================================
echo.

echo   1/4  the creatures a mission spawns
"%HERE%bin\MobExtract.exe" "%HERE%MissionMobs.tsv" "%BOT%\missions\records" || goto :failed
echo.

echo   2/4  the mission pool pack
"%HERE%bin\PoolExtract.exe" "%NAV%" "%DATA%\missionpools.ocp" || goto :failed
echo.

echo   3/4  what every creature in the captures looks like
"%HERE%bin\WorldMobs.exe" "%HERE%WorldMobs.tsv" "%SNIFFS%" || goto :failed
echo.

echo   4/4  the world's spawns
"%HERE%bin\WorldSpawns.exe" "%PATCHES%\world-mobspawns.sql" "%BOT%" ^
  --creatures "%HERE%WorldMobs.tsv" --creatures "%HERE%MissionMobs.tsv" --skip 6553 || goto :failed

echo.
echo   ==========================================================
echo    Done. Apply SqlPatches\world-mobspawns.sql and restart.
echo   ==========================================================
echo.
pause
exit /b 0

:failed
echo.
echo   THAT STEP FAILED - nothing further was run.
echo.
pause
exit /b 1
