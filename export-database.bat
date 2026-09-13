@echo off
setlocal enabledelayedexpansion
title OmniCell - export the database

rem ---------------------------------------------------------------------------
rem Dumps the server's database to Database\omnicell.sql - the one file another
rem operator imports to get a working OmniCell database.
rem
rem That file is what this project distributes alongside the source. It is
rem tracked in git, so run this whenever the data set changes and commit the
rem result.
rem
rem SCHEMA for every table. DATA for the content tables only.
rem
rem The tables holding accounts and players are dumped empty, on purpose. Your
rem login table has your operators' and players' account names and password
rem hashes in it; your characters tables have their characters. None of that is
rem content and none of it belongs in somebody else's database. They are listed
rem in NODATA below - the structure ships, the rows do not.
rem
rem The dump is written with --skip-dump-date and --order-by-primary so that
rem exporting an unchanged database produces an identical file. Without those
rem every export is a diff.
rem ---------------------------------------------------------------------------

set "HERE=%~dp0"
set "OUT=%HERE%Database"
set "OUTFILE=%OUT%\omnicell.sql"

rem Tables whose structure ships but whose rows do not.
set "NODATA=login characters charactersactivenanos charactersmeshs charactersquests characterstimers charactersuploadednanos items instanceditems mobcorpses receivedmessages organizations socialtab"

rem Wherever the client happens to be. The first one that exists wins. Same
rem list as create-database.bat, looking for mysqldump beside mysql.
set "DUMP="
for %%P in (
  "%ProgramFiles%\MySQL\MySQL Server 8.0\bin\mysqldump.exe"
  "%ProgramFiles%\MySQL\MySQL Server 8.4\bin\mysqldump.exe"
  "%ProgramFiles%\MariaDB 11.6\bin\mysqldump.exe"
  "%ProgramFiles%\MariaDB 11.4\bin\mysqldump.exe"
  "%ProgramFiles%\MariaDB 10.11\bin\mysqldump.exe"
) do if not defined DUMP if exist %%P set "DUMP=%%~P"

if not defined DUMP (
  for /f "delims=" %%P in ('where mysqldump 2^>nul') do if not defined DUMP set "DUMP=%%P"
)

if not defined DUMP (
  echo.
  echo   ERROR: could not find mysqldump.
  echo.
  echo   Install MySQL or MariaDB, or put its bin directory on PATH, or set
  echo   DUMP to the full path of mysqldump.exe before running this.
  echo.
  pause
  exit /b 1
)

cls
echo.
echo   ==========================================================
echo    OMNICELL - EXPORT THE DATABASE
echo   ==========================================================
echo.
echo    Using : %DUMP%
echo    To    : %OUTFILE%
echo.

set "DBNAME=omnicell"
set /p "DBNAME=  database to export [omnicell]> "
if "%DBNAME%"=="" set "DBNAME=omnicell"

echo.
echo    An account that may read it - root, normally.
set "ADMIN=root"
set /p "ADMIN=  account [root]> "
if "%ADMIN%"=="" set "ADMIN=root"

for /f "usebackq delims=" %%P in (`powershell -NoProfile -Command "$s = Read-Host -AsSecureString '  password for %ADMIN%'; [Runtime.InteropServices.Marshal]::PtrToStringAuto([Runtime.InteropServices.Marshal]::SecureStringToBSTR($s))"`) do set "ADMINPASS=%%P"

rem Through a defaults file rather than on the command line, for the same
rem reason create-database.bat does it: -p is visible in the process list.
set "CNF=%TEMP%\omnicell-dump-%RANDOM%.cnf"
> "%CNF%" echo [client]
>>"%CNF%" echo user=%ADMIN%
>>"%CNF%" echo password=%ADMINPASS%
set "ADMINPASS="

if not exist "%OUT%" mkdir "%OUT%"

set "COMMON=--defaults-extra-file=%CNF% --default-character-set=latin1 --skip-dump-date --no-tablespaces --single-transaction"

echo.
echo   Structure of every table...
"%DUMP%" %COMMON% --no-data --databases "%DBNAME%" > "%OUTFILE%.tmp" 2>"%TEMP%\omnicell-dump.err"
if errorlevel 1 (
  del "%CNF%" >nul 2>&1
  echo.
  echo   FAILED. mysqldump said:
  type "%TEMP%\omnicell-dump.err"
  del "%OUTFILE%.tmp" "%TEMP%\omnicell-dump.err" >nul 2>&1
  echo.
  pause
  exit /b 1
)

rem The --databases form above wrote the CREATE DATABASE and USE lines, so the
rem data pass must not repeat them.
set "SKIP="
for %%T in (%NODATA%) do set "SKIP=!SKIP! --ignore-table=%DBNAME%.%%T"

echo   Rows of the content tables...
"%DUMP%" %COMMON% --no-create-info --order-by-primary %SKIP% "%DBNAME%" >> "%OUTFILE%.tmp" 2>"%TEMP%\omnicell-dump.err"
if errorlevel 1 (
  del "%CNF%" >nul 2>&1
  echo.
  echo   FAILED. mysqldump said:
  type "%TEMP%\omnicell-dump.err"
  del "%OUTFILE%.tmp" "%TEMP%\omnicell-dump.err" >nul 2>&1
  echo.
  pause
  exit /b 1
)

del "%CNF%" "%TEMP%\omnicell-dump.err" >nul 2>&1

move /Y "%OUTFILE%.tmp" "%OUTFILE%" >nul

rem A dump that still has an account name in it is the one thing this must not
rem produce. The NODATA list should have prevented it; this checks rather than
rem assumes, because the cost of being wrong is publishing a password hash.
echo.
echo   Checking no account or character rows got in...
set "LEAK="
for %%T in (%NODATA%) do (
  findstr /c:"INSERT INTO `%%T`" "%OUTFILE%" >nul 2>&1 && (
    set "LEAK=1"
    echo     FOUND ROWS FOR %%T
  )
)

if defined LEAK (
  echo.
  echo   ==========================================================
  echo    STOPPED - THE DUMP HAS ACCOUNT OR CHARACTER DATA IN IT
  echo   ==========================================================
  echo.
  echo    %OUTFILE%
  echo.
  echo    Do not commit or share that file. The tables listed above
  echo    should have been dumped empty and were not.
  echo.
  pause
  exit /b 1
)

for %%F in ("%OUTFILE%") do set "SIZE=%%~zF"

cls
echo.
echo   ==========================================================
echo    EXPORTED
echo   ==========================================================
echo.
echo    %OUTFILE%
echo    %SIZE% bytes
echo.
echo    Structure for every table. Rows for the content tables.
echo    These shipped empty, by design - structure only:
echo.
for %%T in (%NODATA%) do echo      %%T
echo.
echo    Commit it. That file is what another operator imports with
echo    create-database.bat to get a working database.
echo.
pause
