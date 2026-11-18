<#
.SYNOPSIS
  Bulk de-list NuGet packages matching a glob pattern.

.DESCRIPTION
  Queries nuget.org for all versions of packages matching -Glob,
  then unlists each version using the NuGet API.

.PARAMETER Key
  Your NuGet API key.

.PARAMETER Glob
  Package ID glob pattern (e.g. "Terminal.Gui.Text*").
  Supports * and ? wildcards.

.PARAMETER VersionFilter
  Optional wildcard pattern to filter versions (e.g. "*-develop.*").
  Only versions matching this pattern will be delisted. If omitted, all listed versions are delisted.

.PARAMETER WhatIf
  Show what would be delisted without actually doing it.

.EXAMPLE
  .\delist-nuget.ps1 -Key "my-api-key" -Glob "Terminal.Gui.Text*"
  .\delist-nuget.ps1 -Key "my-api-key" -Glob "Terminal.Gui.Text*" -VersionFilter "*-develop.*" -WhatIf
#>
[CmdletBinding(SupportsShouldProcess)]
param(
    [Parameter(Mandatory)]
    [string]$Key,

    [Parameter(Mandatory)]
    [string]$Glob,

    [Parameter()]
    [string]$VersionFilter
)

$ErrorActionPreference = 'Stop'

# Convert glob to regex
$regexPattern = '^' + [regex]::Escape($Glob).Replace('\*', '.*').Replace('\?', '.') + '$'

Write-Host "Searching nuget.org for packages matching: $Glob" -ForegroundColor Cyan
Write-Host "  (regex: $regexPattern)" -ForegroundColor DarkGray
if ($VersionFilter) {
    Write-Host "  Version filter: $VersionFilter" -ForegroundColor DarkGray
}

# Search NuGet for matching package IDs (paginated, up to 1000)
$skip = 0
$take = 100
$packageIds = @()

do {
    $searchUrl = "https://azuresearch-usnc.nuget.org/query?q=$([uri]::EscapeDataString($Glob))&skip=$skip&take=$take&prerelease=true&semVerLevel=2.0.0"
    $response = Invoke-RestMethod -Uri $searchUrl -Method Get
    foreach ($pkg in $response.data) {
        if ($pkg.id -match $regexPattern) {
            $packageIds += $pkg.id
        }
    }
    $skip += $take
} while ($response.data.Count -eq $take)

if ($packageIds.Count -eq 0) {
    Write-Host "No packages found matching '$Glob'." -ForegroundColor Yellow
    exit 0
}

Write-Host "`nFound $($packageIds.Count) package(s): $($packageIds -join ', ')" -ForegroundColor Green

$totalDelisted = 0

foreach ($id in $packageIds) {
    # Get all versions from the registration API
    $lowerPkgId = $id.ToLowerInvariant()
    $regUrl = "https://api.nuget.org/v3/registration5-gz-semver2/$lowerPkgId/index.json"

    try {
        $reg = Invoke-RestMethod -Uri $regUrl -Method Get
    }
    catch {
        Write-Warning "Could not fetch versions for $id — skipping. $_"
        continue
    }

    $versions = @()
    foreach ($page in $reg.items) {
        # Pages may be inlined or require a separate fetch
        $items = $page.items
        if (-not $items -and $page.'@id') {
            $items = (Invoke-RestMethod -Uri $page.'@id' -Method Get).items
        }
        foreach ($entry in $items) {
            $ver = $entry.catalogEntry.version
            $listed = $entry.catalogEntry.listed
            if ($listed -ne $false) {
                if ($VersionFilter -and $ver -notlike $VersionFilter) {
                    continue
                }
                $versions += $ver
            }
        }
    }

    if ($versions.Count -eq 0) {
        Write-Host "  $id — no listed versions found." -ForegroundColor DarkGray
        continue
    }

    Write-Host "`n  $id — $($versions.Count) listed version(s):" -ForegroundColor Cyan

    foreach ($ver in $versions) {
        if ($PSCmdlet.ShouldProcess("$id $ver", "Delist from NuGet")) {
            Write-Host "    Delisting $id $ver ... " -NoNewline
            try {
                $deleteUrl = "https://www.nuget.org/api/v2/package/$id/$ver"
                Invoke-RestMethod -Uri $deleteUrl -Method Delete -Headers @{ 'X-NuGet-ApiKey' = $Key }
                Write-Host "done" -ForegroundColor Green
                $totalDelisted++
            }
            catch {
                Write-Host "FAILED: $_" -ForegroundColor Red
            }
        }
        else {
            Write-Host "    Would delist $id $ver" -ForegroundColor Yellow
            $totalDelisted++
        }
    }
}

Write-Host "`nTotal: $totalDelisted version(s) delisted." -ForegroundColor Cyan
