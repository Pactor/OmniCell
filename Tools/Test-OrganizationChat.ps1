[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $PSScriptRoot
$chatServerPath = Join-Path $root 'OmniCell\Server\ChatEngine\CoreServer\ChatServer.cs'
$channelPath = Join-Path $root 'OmniCell\Server\ChatEngine\Channels\ChannelBase.cs'
$messagePath = Join-Path $root 'OmniCell\Server\ChatEngine\PacketHandlers\ChannelMessage.cs'
$characterPath = Join-Path $root 'OmniCell\Server\ChatEngine\CoreClient\Character.cs'

function Assert-Contains {
    param(
        [string]$Text,
        [string]$Needle,
        [string]$Message
    )

    if (-not $Text.Contains($Needle)) {
        throw $Message
    }
}

$chatServer = Get-Content -LiteralPath $chatServerPath -Raw
$channel = Get-Content -LiteralPath $channelPath -Raw
$message = Get-Content -LiteralPath $messagePath -Raw
$character = Get-Content -LiteralPath $characterPath -Raw

Assert-Contains $character 'return organizationStat == null ? 0 : organizationStat.StatValue;' `
    'Characters without stat 5 do not safely resolve to no organization.'
Assert-Contains $chatServer 'int organizationId = client.Character.orgId;' `
    'Chat login does not read the character organization id.'
Assert-Contains $chatServer 'OrganizationChannel organizationChannel = this.GetOrCreateOrganizationChannel(organizationId);' `
    'Chat login does not create or reuse its own organization channel.'
Assert-Contains $chatServer '.FirstOrDefault(channel => channel.ChannelId == (uint)organizationId);' `
    'Organization channel lookup is not scoped to the organization id.'
Assert-Contains $chatServer 'if (OrganizationDao.Instance.Get(organizationId) == null)' `
    'ChatEngine does not reject an organization id that no longer exists.'
Assert-Contains $chatServer 'foreach (ChannelBase channel in cl.Channels.ToArray())' `
    'Disconnect does not remove the client from joined channels.'
Assert-Contains $channel '((Client)client).Channels.Remove(this);' `
    'Channel removal leaves stale membership on the client.'
Assert-Contains $message 'if ((channel == null) || !client.Channels.Contains(channel))' `
    'Channel messages are not rejected when the sender has not joined the channel.'

if ($chatServer.Contains('foreach (ChannelBase channel in this.ChannelsByType<OrganizationChannel>())')) {
    throw 'Chat login still adds every client to every organization channel.'
}

Write-Host 'OK - organization chat isolation and cleanup regression checks passed.'
