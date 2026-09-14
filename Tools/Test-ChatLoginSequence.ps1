[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'

if ([IntPtr]::Size -ne 4) {
    $powerShell32 = Join-Path $env:WINDIR 'SysWOW64\WindowsPowerShell\v1.0\powershell.exe'
    if (-not (Test-Path -LiteralPath $powerShell32)) {
        throw 'The 32-bit Windows PowerShell executable is required to inspect the x86 ChatEngine build.'
    }

    & $powerShell32 -NoProfile -ExecutionPolicy Bypass -File $PSCommandPath
    exit $LASTEXITCODE
}

$root = Split-Path -Parent $PSScriptRoot
$authenticatePath = Join-Path $root 'OmniCell\Server\ChatEngine\PacketHandlers\Authenticate.cs'
$loginCharacterPath = Join-Path $root 'OmniCell\Server\ChatEngine\PacketHandlers\LoginCharacter.cs'
$chatEnginePath = Join-Path $root 'OmniCell\Built\Debug\ChatEngine.exe'

function Assert-True {
    param(
        [bool]$Condition,
        [string]$Message
    )

    if (-not $Condition) {
        throw $Message
    }
}

function Find-Once {
    param(
        [string]$Text,
        [string]$Needle,
        [string]$Description
    )

    $first = $Text.IndexOf($Needle, [StringComparison]::Ordinal)
    Assert-True ($first -ge 0) "Missing $Description."
    Assert-True ($Text.IndexOf($Needle, $first + $Needle.Length, [StringComparison]::Ordinal) -lt 0) `
        "Found more than one $Description."
    return $first
}

$authenticate = Get-Content -LiteralPath $authenticatePath -Raw
$normalLoginOk = $authenticate.IndexOf('byte[] loginok = LoginOk.Create();', [StringComparison]::Ordinal)
$normalChannels = $authenticate.IndexOf('client.ChatServer().AddClientToChannels(client);', [StringComparison]::Ordinal)
Assert-True ($normalLoginOk -ge 0) 'Normal game-client login no longer creates LOGIN_OK.'
Assert-True ($normalChannels -ge 0) 'Normal game-client login no longer joins channels.'
Assert-True ($normalLoginOk -lt $normalChannels) 'Normal game-client login announces channels before LOGIN_OK.'

$botLogin = Get-Content -LiteralPath $loginCharacterPath -Raw
$botLoginOk = Find-Once $botLogin 'client.Send(LoginOk.Create());' 'bot LOGIN_OK send'
$botChannels = Find-Once $botLogin 'client.ChatServer().AddClientToChannels(client);' 'bot channel-add pass'
Assert-True ($botLoginOk -lt $botChannels) 'Bot login announces channels before LOGIN_OK.'
Assert-True (-not $botLogin.Contains('ChannelJoin.Create(')) `
    'Bot login explicitly sends ChannelJoin in addition to ChannelBase.AddClient.'

Assert-True (Test-Path -LiteralPath $chatEnginePath) `
    'ChatEngine.exe is missing. Build ChatEngine Debug before running this test.'

[void][Reflection.Assembly]::LoadFrom($chatEnginePath)
$packet = [ChatEngine.Packets.LoginOk]::Create()
Assert-True ($packet.Length -eq 4) 'LOGIN_OK packet is not the expected four-byte header-only packet.'
Assert-True ($packet[0] -eq 0 -and $packet[1] -eq 5 -and $packet[2] -eq 0 -and $packet[3] -eq 0) `
    'LOGIN_OK packet header is not message type 5 with an empty payload.'

Write-Host 'OK - normal client and bot login packet ordering checks passed.'
