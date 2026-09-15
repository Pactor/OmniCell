[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $PSScriptRoot
$programPath = Join-Path $root 'OmniCell\Server\ZoneEngine\Program.cs'
$program = Get-Content -LiteralPath $programPath -Raw

function Assert-True {
    param(
        [bool]$Condition,
        [string]$Message
    )

    if (-not $Condition) {
        throw $Message
    }
}

Assert-True (-not $program.Contains('Console.WriteLine(locales.ItemLoaderLoadedItems, ItemLoader.CacheAllItems());')) `
    'ZoneEngine prints a duplicate item-loader summary.'
Assert-True (-not $program.Contains('Console.WriteLine(locales.NanoLoaderLoadedNanos, NanoLoader.CacheAllNanos());')) `
    'ZoneEngine prints a duplicate nano-loader summary.'
Assert-True (-not $program.Contains('ScriptCompiler.Instance.Compile(false);')) `
    'ZoneEngine compiles scripts before StartTheServer and then compiles them again.'
Assert-True ($program.Contains('ScriptCompiler.Instance.Compile(true);')) `
    'ZoneEngine no longer compiles scripts during server startup.'

Write-Host 'OK - ZoneEngine startup output and script compilation checks passed.'
