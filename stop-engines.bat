@echo off
setlocal
title OmniCell - stop the server

rem Stops all three engines. Closing an engine's own window does the same for
rem that one; this is the blunt version for when they are already gone from the
rem taskbar or wedged.

echo.
for %%E in (ZoneEngine LoginEngine ChatEngine WebEngine) do (
  tasklist /FI "IMAGENAME eq %%E.exe" 2>nul | find /i "%%E.exe" >nul
  if errorlevel 1 (
    echo   %%E    not running
  ) else (
    taskkill /F /IM %%E.exe >nul 2>&1
    echo   %%E    stopped
  )
)
echo.
pause
