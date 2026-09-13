@echo off
setlocal
title OmniCell - start the server

rem ---------------------------------------------------------------------------
rem Starts the four engines, each in its own window.
rem
rem Order matters. ZoneEngine polls for ChatEngine on startup and will sit
rem retrying until it appears, so ChatEngine goes first.
rem
rem Each engine reads commands from its own console, so they are started as
rem separate windows rather than in the background. Closing a window stops that
rem engine.
rem
rem Pass "debug" as an argument to run the Debug build instead of Release.
rem ---------------------------------------------------------------------------

set "CONFIG=Release"
set "AUTO="
set "WEBPORT="
for %%A in (%*) do (
  if /i "%%~A"=="debug" set "CONFIG=Debug"
  if /i "%%~A"=="auto" set "AUTO=1"
  echo %%~A| findstr /b /i "port=" >nul && set "WEBPORT=%%~A"
)

set "BUILT=%~dp0OmniCell\Built\%CONFIG%"

if not exist "%BUILT%\LoginEngine.exe" (
  echo ERROR: %CONFIG% build not found at:
  echo   %BUILT%
  echo Build OmniCell.sln first.
  if not defined AUTO pause
  exit /b 1
)

echo.
echo   Configuration : %CONFIG%
echo   From          : %BUILT%
echo.

tasklist /FI "IMAGENAME eq ZoneEngine.exe" 2>nul | find /i "ZoneEngine.exe" >nul
if not errorlevel 1 (
  echo   Engines are already running. Close their windows first, or run
  echo   stop-engines.bat.
  echo.
  if not defined AUTO pause
  exit /b 1
)

echo   Starting ChatEngine...
start "OmniCell ChatEngine" /D "%BUILT%" "%BUILT%\ChatEngine.exe" -autostart
timeout /t 4 /nobreak >nul

echo   Starting LoginEngine...
start "OmniCell LoginEngine" /D "%BUILT%" "%BUILT%\LoginEngine.exe" -autostart
timeout /t 3 /nobreak >nul

echo   Starting ZoneEngine...
start "OmniCell ZoneEngine" /D "%BUILT%" "%BUILT%\ZoneEngine.exe" -autostart
timeout /t 6 /nobreak >nul

rem The in-game browser panels - shop, market, petition, daily - point at
rem this. The launcher rewrites the client's URLs to reach it, so without it
rem running those panels reach nothing at all.
rem
rem Port 80 by default, because the patched URLs have to fit inside the
rem strings they replace and ":8080" costs five characters the shortest ones
rem do not have. If something else already holds 80, pass port=8080 here and
rem set the same port in the launcher.
echo   Starting WebEngine...
start "OmniCell WebEngine" /D "%BUILT%" "%BUILT%\WebEngine.exe" %WEBPORT%
timeout /t 2 /nobreak >nul

echo.
echo   Listening ports:
netstat -an | findstr /R /C:":7500 .*LISTENING" /C:":7501 .*LISTENING" /C:":6996 .*LISTENING"
echo.
echo   Login 7500, Zone 7501, Chat 6996, Web 80. If a port is missing, look at that
echo   engine's window for the reason.
echo.
echo   Point the launcher at 127.0.0.1 port 7500.
echo.
if not defined AUTO pause
