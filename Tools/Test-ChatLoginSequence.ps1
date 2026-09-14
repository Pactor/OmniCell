[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $PSScriptRoot
$authenticatePath = Join-Path $root 'OmniCell\Server\ChatEngine\PacketHandlers\Authenticate.cs'
$loginCharacterPath = Join-Path $root 'OmniCell\Server\ChatEngine\PacketHandlers\LoginCharacter.cs'
$builtPath = Join-Path $root 'OmniCell\Built\Debug'
$chatEnginePath = Join-Path $builtPath 'ChatEngine.dll'

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
    'ChatEngine.dll is missing. Build ChatEngine Debug before running this test.'

# ChatEngine is a .NET 10 assembly, and Windows PowerShell runs on .NET Framework, which cannot load
# it. A throwaway console program references the built ChatEngine.dll and prints the LOGIN_OK packet
# as hex. Anything ChatEngine needs besides itself is loaded from the build folder.
$probe = Join-Path ([IO.Path]::GetTempPath()) ('OmniCell-LoginOk-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $probe | Out-Null
try {
    $project = @"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
  </PropertyGroup>
  <ItemGroup>
    <Reference Include="ChatEngine"><HintPath>$chatEnginePath</HintPath></Reference>
  </ItemGroup>
</Project>
"@
    $program = @'
string built = args[0];
System.Runtime.Loader.AssemblyLoadContext.Default.Resolving += (context, name) =>
{
    string path = System.IO.Path.Combine(built, name.Name + ".dll");
    return System.IO.File.Exists(path) ? context.LoadFromAssemblyPath(path) : null;
};
Print();

static void Print()
{
    System.Console.WriteLine(System.Convert.ToHexString(ChatEngine.Packets.LoginOk.Create()));
}
'@
    [IO.File]::WriteAllText((Join-Path $probe 'LoginOkProbe.csproj'), $project)
    [IO.File]::WriteAllText((Join-Path $probe 'Program.cs'), $program)

    $output = & dotnet run --project (Join-Path $probe 'LoginOkProbe.csproj') -c Release -- $builtPath 2>&1
    if ($LASTEXITCODE -ne 0) {
        throw "The LOGIN_OK probe did not run:`n$($output -join "`n")"
    }

    $hex = ([string]($output | Select-Object -Last 1)).Trim()
}
finally {
    Remove-Item -LiteralPath $probe -Recurse -Force -ErrorAction SilentlyContinue
}

Assert-True ($hex -eq '00050000') `
    "LOGIN_OK packet is not the four-byte header for message type 5 with an empty payload (got $hex)."

Write-Host 'OK - normal client and bot login packet ordering checks passed.'
