<#
    Writes Config.local.xml from the tracked Config.xml.

    Called by configure-server.bat. Everything comes in through environment
    variables rather than arguments, so the database password is never on a
    command line where another process could read it out of the process list.

        OMNICELL_TEMPLATE   the tracked Config.xml to start from
        OMNICELL_TARGET     the Config.local.xml to write
        OMNICELL_BIND       what the engines bind to
        OMNICELL_REACH      what players are told to connect to
        OMNICELL_DBNAME     database name
        OMNICELL_DBHOST     database host
        OMNICELL_DBUSER     database user
        OMNICELL_DBPASS     database password
        OMNICELL_DBSSL      optional None, Preferred, or Required override

    Built from the tracked file rather than written from scratch, so a setting
    added to Config.xml later is carried over instead of quietly going missing
    from every configured server.
#>

$ErrorActionPreference = 'Stop'

function Need($name) {
    $value = [Environment]::GetEnvironmentVariable($name)
    if ([string]::IsNullOrEmpty($value)) { throw "$name is not set" }
    return $value
}

$template = Need 'OMNICELL_TEMPLATE'
$target   = Need 'OMNICELL_TARGET'
$bind     = Need 'OMNICELL_BIND'
$reach    = Need 'OMNICELL_REACH'
$dbName   = Need 'OMNICELL_DBNAME'
$dbHost   = Need 'OMNICELL_DBHOST'
$dbUser   = Need 'OMNICELL_DBUSER'

# The password alone may legitimately be empty, so it is not run through Need.
$dbPass = [Environment]::GetEnvironmentVariable('OMNICELL_DBPASS')
$dbSsl = [Environment]::GetEnvironmentVariable('OMNICELL_DBSSL')
if ([string]::IsNullOrWhiteSpace($dbSsl)) {
    # Loopback traffic never leaves this machine. Some Windows MySQL installs
    # advertise TLS but cannot complete the Schannel handshake, which made a
    # freshly generated local setup fail during engine initialization.
    $dbSsl = if ($dbHost -match '^(localhost|127[.]0[.]0[.]1|::1)(;|$)') { 'None' } else { 'Preferred' }
}
if ($dbSsl -notin @('None', 'Preferred', 'Required')) {
    throw 'OMNICELL_DBSSL must be None, Preferred, or Required.'
}

$text = Get-Content -Raw -LiteralPath $template

# The comments in Config.xml explain the choices this script has just made, so
# they would only be confusing in the answer.
$text = $text -replace '(?s)<!--.*?-->', ''

function SetTag([string]$body, [string]$tag, [string]$value) {
    # Ampersand first, or the escapes written by the later two get escaped
    # again and a password with a '<' in it arrives as '&amp;lt;'.
    #
    # These are not cosmetic. A password containing & or < produced a file that
    # is not XML at all, and the server's answer to that is to fall back to an
    # empty configuration and try to reach a database called nothing at all.
    $safe = $value.Replace('&', '&amp;').Replace('<', '&lt;').Replace('>', '&gt;')

    # And a '$' in a -replace replacement is a capture group reference, so it
    # has to be doubled. Otherwise a password with one in it is silently
    # mangled into something that will not connect, with nothing to say why.
    $safe = $safe -replace '\$', '$$$$'

    return $body -replace "<$tag>.*?</$tag>", "<$tag>$safe</$tag>"
}

$text = SetTag $text 'ListenIP'     $bind
$text = SetTag $text 'ZoneIP'       $reach
$text = SetTag $text 'ChatIP'       $reach

# Never follows the others. This channel is unauthenticated, and "make it
# reachable" is the wrong fix for every problem it could appear to cause.
$text = SetTag $text 'CommListenIP' '127.0.0.1'

$connection = "Database=$dbName;Data Source=$dbHost;User ID=$dbUser;Password=$dbPass;SslMode=$dbSsl"
$text = SetTag $text 'MysqlConnection' $connection

# Blank lines left behind where the comments were.
$lines = $text -split "`r`n|`n" | Where-Object { $_.Trim() -ne '' }
$text = ($lines -join [Environment]::NewLine) + [Environment]::NewLine

Set-Content -LiteralPath $target -Value $text -Encoding UTF8

# Said back without the password, as a check that the right file was written.
Write-Output "  binds to    $bind"
Write-Output "  players use $reach"
Write-Output "  database    $dbUser@$dbHost/$dbName"
Write-Output "  database TLS $dbSsl"
