<#
.SYNOPSIS
Writes repo.json, the custom plugin repository feed Dalamud reads, for one released version.

.DESCRIPTION
Players add the feed's URL in /xlsettings > Experimental > Custom Plugin Repositories. Dalamud reads an array of
plugin manifests from it: the manifest DalamudPackager writes (EmoteIntent.json), plus where to download the zip,
when it was released and what changed. The release workflow (.github/workflows/release.yaml) runs this and attaches
the result to the GitHub release, so the feed always describes the latest release.

.EXAMPLE
./tools/New-RepoJson.ps1 -Manifest EmoteIntent/bin/Release/EmoteIntent/EmoteIntent.json -Tag v0.17.0.1 `
    -Repository denis-coccodi/ffxiv-emote-intent -Out repo.json
#>
param(
    [Parameter(Mandatory)] [string] $Manifest,
    [Parameter(Mandatory)] [string] $Tag,
    [Parameter(Mandatory)] [string] $Repository,
    [Parameter(Mandatory)] [string] $Out,
    [string] $Changelog = ''
)

$ErrorActionPreference = 'Stop'
$plugin = Get-Content -Raw -Path $Manifest | ConvertFrom-Json
if ("v$($plugin.AssemblyVersion)" -ne $Tag) {
    throw "Tag $Tag doesn't match the plugin's version $($plugin.AssemblyVersion) (Version in EmoteIntent.csproj)."
}

$zip = "https://github.com/$Repository/releases/download/$Tag/latest.zip"
$entry = [ordered]@{}
foreach ($property in $plugin.PSObject.Properties) {
    $entry[$property.Name] = $property.Value
}

$entry.DownloadLinkInstall = $zip
$entry.DownloadLinkUpdate = $zip
$entry.DownloadLinkTesting = $zip
# The icon as committed at this release's tag, so an older feed never shows a newer icon.
$entry.IconUrl = "https://raw.githubusercontent.com/$Repository/$Tag/EmoteIntent/images/icon.png"
$entry.IsHide = $false
$entry.IsTestingExclusive = $false
$entry.LastUpdate = [DateTimeOffset]::UtcNow.ToUnixTimeSeconds()
if ($Changelog) {
    $entry.Changelog = $Changelog
}

# An array even for one plugin: Dalamud reads a list of manifests.
$json = ConvertTo-Json -InputObject @($entry) -Depth 5
[IO.File]::WriteAllText((Join-Path (Get-Location) $Out), $json, [Text.UTF8Encoding]::new($false))
Write-Output "Wrote $Out for $($plugin.Name) $($plugin.AssemblyVersion)"
