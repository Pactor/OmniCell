@echo off
setlocal enabledelayedexpansion
title OmniCell - configure this server

rem ---------------------------------------------------------------------------
rem Writes Config.local.xml: the database to use, and the address players
rem connect to.
rem
rem That file is read in preference to OmniCell\Config\Config.xml and is ignored
rem by git, so the settings for a real server - including its database password
rem - cannot end up in a commit. Config.xml itself stays as it is in the
rem repository, holding placeholders.
rem
rem Run this again whenever any of it changes. It reads nothing back, so answer
rem every question each time.
rem ---------------------------------------------------------------------------

set "HERE=%~dp0"
set "TEMPLATE=%HERE%OmniCell\Config\Config.xml"
set "TARGET=%HERE%OmniCell\Config\Config.local.xml"

if not exist "%TEMPLATE%" (
  echo ERROR: %TEMPLATE% is missing. Is this the root of the repository?
  pause
  exit /b 1
)

cls
echo.
echo   ==========================================================
echo    OMNICELL - SERVER SETUP
echo   ==========================================================
echo.
echo    Who will be playing on this server?
echo.
echo      1  just me, on this machine
echo      2  people on my home or office network
echo      3  people on the internet
echo.

:whoagain
set "WHO="
set /p "WHO=  1, 2 or 3> "
if "%WHO%"=="1" goto :who1
if "%WHO%"=="2" goto :who2
if "%WHO%"=="3" goto :who3
goto :whoagain

:who1
rem Bound to loopback and handing players back to loopback. Nothing outside
rem this machine can reach it, which is the right default.
set "BIND=127.0.0.1"
set "REACH=127.0.0.1"
set "WHOTEXT=this machine only"
goto :gotwho

:who2
echo.
echo   This machine's addresses on your network:
echo.
for /f "tokens=2 delims=:" %%A in ('ipconfig ^| "%SystemRoot%\System32\findstr.exe" /c:"IPv4 Address"') do echo       %%A
echo.
echo   Players type this one into the launcher, so it has to be the one
echo   they can reach - not 127.0.0.1, which on their machine means their
echo   own machine.
echo.
set "REACH="
set /p "REACH=  address players connect to> "
if "%REACH%"=="" goto :who2
set "BIND=0.0.0.0"
set "WHOTEXT=your network"
goto :gotwho

:who3
echo.
echo   Your public address or a hostname that resolves to it. A hostname
echo   is the better answer if your address changes.
echo.
echo   Forward these ports to this machine first, or nothing will reach it:
echo       7500 login, 7501 zone, 7012 chat
echo   Leave 6996 alone. That one is the engines talking to each other and
echo   it is unauthenticated - anything that can reach it can act as one of
echo   them.
echo.
set "REACH="
set /p "REACH=  address or hostname players connect to> "
if "%REACH%"=="" goto :who3
set "BIND=0.0.0.0"
set "WHOTEXT=the internet"
goto :gotwho

:gotwho
echo.
echo   ----------------------------------------------------------
echo.
echo    The database. Create it first if you have not - see
echo    Documentation\Running-a-server.md, which has the exact
echo    commands.
echo.

set "DBNAME=omnicell"
set /p "DBNAME=  database name [omnicell]> "
if "%DBNAME%"=="" set "DBNAME=omnicell"

set "DBHOST=localhost"
set /p "DBHOST=  database host [localhost]> "
if "%DBHOST%"=="" set "DBHOST=localhost"

set "DBUSER=omnicell"
set /p "DBUSER=  database user [omnicell]> "
if "%DBUSER%"=="" set "DBUSER=omnicell"

rem Not echoed as it is typed. Batch has no way to read a line without showing
rem it, so this is PowerShell's job.
rem
rem It goes into the environment rather than onto a command line, because
rem anything on a command line is readable from the process list by anything
rem else running on the machine.
for /f "usebackq delims=" %%P in (`powershell -NoProfile -Command "$s = Read-Host -AsSecureString '  database password'; [Runtime.InteropServices.Marshal]::PtrToStringAuto([Runtime.InteropServices.Marshal]::SecureStringToBSTR($s))"`) do set "OMNICELL_DBPASS=%%P"

echo.
echo   ----------------------------------------------------------
echo.
echo    Playable by : %WHOTEXT%
echo    Binding to  : %BIND%
echo    Players use : %REACH%
echo    Database    : %DBUSER%@%DBHOST%/%DBNAME%
echo.
set "OK="
set /p "OK=  write this? [y/N]> "
if /i not "%OK%"=="y" (
  echo.
  echo   Nothing was written.
  pause
  exit /b 1
)

rem Written by Tools\WriteConfig.ps1, which builds the answer out of the
rem tracked Config.xml so that a setting added there later is carried over
rem rather than going quietly missing from every configured server.
set "OMNICELL_TEMPLATE=%TEMPLATE%"
set "OMNICELL_TARGET=%TARGET%"
set "OMNICELL_BIND=%BIND%"
set "OMNICELL_REACH=%REACH%"
set "OMNICELL_DBNAME=%DBNAME%"
set "OMNICELL_DBHOST=%DBHOST%"
set "OMNICELL_DBUSER=%DBUSER%"

powershell -NoProfile -ExecutionPolicy Bypass -File "%HERE%Tools\WriteConfig.ps1"
set "WROTE=%ERRORLEVEL%"

rem Out of the environment the moment it is no longer needed, so it is not
rem inherited by anything started from this window afterwards.
set "OMNICELL_DBPASS="

if not "%WROTE%"=="0" (
  echo.
  echo   ERROR: the settings could not be written.
  pause
  exit /b 1
)

if not exist "%TARGET%" (
  echo.
  echo   ERROR: could not write %TARGET%
  pause
  exit /b 1
)

rem The engines read their settings from the directory they run in, which is
rem the build output, so the file has to be beside each of them too - the .NET 10
rem engines in the net10.0 subfolders included.
for %%D in ("%HERE%OmniCell\Built\Debug" "%HERE%OmniCell\Built\Release" "%HERE%OmniCell\Built\Debug\net10.0" "%HERE%OmniCell\Built\Release\net10.0") do (
  if exist "%%~D" copy /Y "%TARGET%" "%%~D\Config.local.xml" >nul
)

cls
echo.
echo   ==========================================================
echo    WRITTEN
echo   ==========================================================
echo.
echo    %TARGET%
echo.
echo    and copied beside each engine that is already built.
echo.
echo    git ignores that file, so your password stays out of the
echo    repository. Config.xml is untouched and still holds the
echo    placeholders it ships with.
echo.
echo    Next:
echo      1. Load the database schema, if you have not - see
echo         Documentation\Running-a-server.md
echo      2. start-engines.bat
echo      3. In the LoginEngine window, adduser to make an account
echo.
if "%BIND%"=="0.0.0.0" (
  echo    Players point the launcher at %REACH% port 7500.
) else (
  echo    Point the launcher at 127.0.0.1 port 7500.
)
echo.
pause
