@echo off
setlocal enabledelayedexpansion
title OmniCell - create the database

rem ---------------------------------------------------------------------------
rem Creates the database and loads it.
rem
rem The repository distributes table definitions under SqlTables and ordered
rem emulator-content updates under SqlPatches. This script loads both. If an
rem optional Database\omnicell.sql export exists, it imports that instead.
rem
rem It ships with the accounts and characters tables empty. You make your own
rem account with "adduser" in the LoginEngine window once the server is up.
rem
rem It asks for a MySQL or MariaDB account that is allowed to create databases -
rem root, normally. That is used for this and nothing else: the server itself
rem gets its own account with rights to one database, which is what
rem configure-server.bat writes down.
rem
rem Safe to run twice. Existing tables are left alone unless you say to drop
rem them.
rem ---------------------------------------------------------------------------

set "HERE=%~dp0"
set "TABLES=%HERE%OmniCell\Libraries\Source\OmniCell.Database\SqlTables"
set "PATCHES=%HERE%OmniCell\Libraries\Source\OmniCell.Database\SqlPatches"
set "DUMPFILE=%HERE%Database\omnicell.sql"

rem Wherever the client happens to be. The first one that exists wins.
set "MYSQL="
for %%P in (
  "%ProgramFiles%\MySQL\MySQL Server 8.0\bin\mysql.exe"
  "%ProgramFiles%\MySQL\MySQL Server 8.4\bin\mysql.exe"
  "%ProgramFiles%\MariaDB 11.6\bin\mysql.exe"
  "%ProgramFiles%\MariaDB 11.4\bin\mysql.exe"
  "%ProgramFiles%\MariaDB 10.11\bin\mysql.exe"
) do if not defined MYSQL if exist %%P set "MYSQL=%%~P"

if not defined MYSQL (
  for /f "delims=" %%P in ('where mysql 2^>nul') do if not defined MYSQL set "MYSQL=%%P"
)

if not defined MYSQL (
  echo.
  echo   ERROR: could not find the mysql command line client.
  echo.
  echo   Install MySQL or MariaDB, or put its bin directory on PATH, or set
  echo   MYSQL to the full path of mysql.exe before running this.
  echo.
  pause
  exit /b 1
)

if not exist "%DUMPFILE%" if not exist "%TABLES%\login.sql" (
  echo ERROR: found neither Database\omnicell.sql nor the SqlTables directory.
  echo Is this the root of the repository?
  pause
  exit /b 1
)

cls
echo.
echo   ==========================================================
echo    OMNICELL - CREATE THE DATABASE
echo   ==========================================================
echo.
echo    Using : %MYSQL%
echo.

set "DBNAME=omnicell"
set /p "DBNAME=  database to create [omnicell]> "
if "%DBNAME%"=="" set "DBNAME=omnicell"

set "DBUSER=omnicell"
set /p "DBUSER=  account the server will use [omnicell]> "
if "%DBUSER%"=="" set "DBUSER=omnicell"

echo.
echo    A password for that account. Write it down - you need the same
echo    one in configure-server.bat.
for /f "usebackq delims=" %%P in (`powershell -NoProfile -Command "$s = Read-Host -AsSecureString '  new password for %DBUSER%'; [Runtime.InteropServices.Marshal]::PtrToStringAuto([Runtime.InteropServices.Marshal]::SecureStringToBSTR($s))"`) do set "NEWPASS=%%P"

echo.
echo    Now an account that may create databases - root, normally.
set "ADMIN=root"
set /p "ADMIN=  administrator [root]> "
if "%ADMIN%"=="" set "ADMIN=root"

for /f "usebackq delims=" %%P in (`powershell -NoProfile -Command "$s = Read-Host -AsSecureString '  password for %ADMIN%'; [Runtime.InteropServices.Marshal]::PtrToStringAuto([Runtime.InteropServices.Marshal]::SecureStringToBSTR($s))"`) do set "ADMINPASS=%%P"

rem Through a defaults file rather than on the command line. A password given
rem to mysql as -p is visible in the process list to anything else running on
rem the machine, and mysql itself warns about it.
set "CNF=%TEMP%\omnicell-%RANDOM%.cnf"
> "%CNF%" echo [client]
>>"%CNF%" echo user=%ADMIN%
>>"%CNF%" echo password=%ADMINPASS%
set "ADMINPASS="

echo.
echo   Checking the connection...
"%MYSQL%" --defaults-extra-file="%CNF%" -e "SELECT 1" >nul 2>&1
if errorlevel 1 (
  del "%CNF%" >nul 2>&1
  echo.
  echo   Could not connect as %ADMIN%. Wrong password, or the server is not
  echo   running.
  echo.
  pause
  exit /b 1
)

echo   Creating %DBNAME% and the %DBUSER% account...
"%MYSQL%" --defaults-extra-file="%CNF%" -e ^
 "CREATE DATABASE IF NOT EXISTS `%DBNAME%` CHARACTER SET latin1;" 2>&1

rem Localhost only. An account that can be used from anywhere is not something
rem to create quietly on somebody's behalf; if the database is on another
rem machine, grant it there and to the host that needs it.
"%MYSQL%" --defaults-extra-file="%CNF%" -e ^
 "CREATE USER IF NOT EXISTS '%DBUSER%'@'localhost' IDENTIFIED BY '%NEWPASS%'; ALTER USER '%DBUSER%'@'localhost' IDENTIFIED BY '%NEWPASS%'; GRANT ALL PRIVILEGES ON `%DBNAME%`.* TO '%DBUSER%'@'localhost'; FLUSH PRIVILEGES;" 2>&1
if errorlevel 1 (
  del "%CNF%" >nul 2>&1
  set "NEWPASS="
  echo.
  echo   Could not create the account. The message above says why.
  echo.
  pause
  exit /b 1
)
set "NEWPASS="

set "OK=0"
set "FAILED=0"
set "SKIPPED=0"
set "NONAMES="

rem An optional full export is fastest to import. A normal checkout instead
rem loads the distributed table definitions followed by every ordered patch.
rem export-database.bat creates the optional export from a running database.
if not exist "%DUMPFILE%" goto :pertable

echo.
echo   Importing the database. This is the whole data set in one
echo   file and takes a couple of minutes.
echo.
<nul set /p "=    Database\omnicell.sql ... "
"%MYSQL%" --defaults-extra-file="%CNF%" "%DBNAME%" < "%DUMPFILE%" 2>"%TEMP%\omnicell-sql.err"
if errorlevel 1 (
  echo FAILED
  type "%TEMP%\omnicell-sql.err"
  del "%CNF%" "%TEMP%\omnicell-sql.err" >nul 2>&1
  echo.
  echo   The database was not loaded. The error is above.
  echo.
  pause
  exit /b 1
)
echo done
set "OK=1"
goto :loaded

:pertable

rem itemnames.sql ships with the repository like every other table. If it is
rem missing the checkout is broken, but that is worth saying plainly at the end
rem rather than failing here.
if not exist "%TABLES%\itemnames.sql" set "NONAMES=1"

echo.
echo   Loading the tables. tradeskill is eleven megabytes and takes a minute.
echo.

for %%F in ("%TABLES%\*.sql") do (
  set "NAME=%%~nF"
  "%MYSQL%" --defaults-extra-file="%CNF%" "%DBNAME%" -e "SELECT 1 FROM `!NAME!` LIMIT 0" >nul 2>&1
  if errorlevel 1 (
    <nul set /p "=    !NAME! ... "
    "%MYSQL%" --defaults-extra-file="%CNF%" "%DBNAME%" < "%%~fF" 2>"%TEMP%\omnicell-sql.err"
    if errorlevel 1 (
      set /a FAILED+=1
      echo FAILED
      type "%TEMP%\omnicell-sql.err"
    ) else (
      set /a OK+=1
      echo done
    )
  ) else (
    set /a SKIPPED+=1
  )
)

:loaded

rem Changes to tables that already existed. Each one has to be safe to run
rem again, because there is no record of which have been applied.
if exist "%PATCHES%\*.sql" (
  echo.
  echo   Applying patches...
  for %%F in ("%PATCHES%\*.sql") do (
    <nul set /p "=    %%~nF ... "
    "%MYSQL%" --defaults-extra-file="%CNF%" "%DBNAME%" < "%%~fF" >nul 2>"%TEMP%\omnicell-sql.err"
    if errorlevel 1 (
      set /a FAILED+=1
      echo FAILED
      type "%TEMP%\omnicell-sql.err"
    ) else (
      echo done
    )
  )
)

del "%CNF%" "%TEMP%\omnicell-sql.err" >nul 2>&1

echo.
echo   ==========================================================
if exist "%DUMPFILE%" (
  echo    Database imported
) else (
  echo    %OK% table(s^) created, %SKIPPED% already there, %FAILED% failed
)
echo   ==========================================================
echo.
if not "%FAILED%"=="0" (
  echo    Something did not load. The errors are above.
  echo.
  pause
  exit /b 1
)
echo    Database  : %DBNAME%
echo    Account   : %DBUSER%@localhost
echo.
if defined NONAMES (
  echo    ----------------------------------------------------------
  echo     NO ITEM NAMES YET
  echo    ----------------------------------------------------------
  echo.
  echo     itemnames.sql was not there, so the itemnames table has
  echo     not been created. The server will run without it and
  echo     every item in the game will be nameless.
  echo.
  echo     It ships with the repository, so this means the checkout
  echo     is incomplete. Restore it with:
  echo.
  echo       git checkout -- OmniCell/Libraries/Source/OmniCell.Database/SqlTables/itemnames.sql
  echo.
  echo     then run this again - it will pick up the file and leave
  echo     everything else alone.
  echo.
)
echo    Next:
echo      1. configure-server.bat, with those details
echo      2. start-engines.bat
echo      3. adduser in the LoginEngine window, to make an account
echo.
echo    Documentation\Running-a-server.md has the whole of it.
echo.
pause
