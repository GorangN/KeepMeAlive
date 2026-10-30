[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
$repository = [IO.Path]::GetFullPath((Split-Path $PSScriptRoot -Parent))
$fixtureRoot = Join-Path $repository ('artifacts/production-gates-' + [Guid]::NewGuid().ToString('N'))
$originalSmtp = $env:KEEP_ME_ALIVE_SMTP_VERIFIED
$originalOauth = $env:KEEP_ME_ALIVE_OAUTH_VERIFIED
try {
    New-Item -ItemType Directory -Path (Join-Path $fixtureRoot 'scripts'), (Join-Path $fixtureRoot 'backend'), (Join-Path $fixtureRoot 'KeepMeAlive') -Force | Out-Null
    $script = Join-Path $fixtureRoot 'scripts/Build-Packages.ps1'
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'Build-Packages.ps1') -Destination $script
    $backend = @{ accountReleaseReady = $false; resource = 'https://example.test/api'; production = @{ apiBaseUrl = 'https://api.example.test/'; issuer = 'https://auth.example.test/auth'; clientId = 'production-public-client' } }
    $desktop = @{ apiBaseUrl = $backend.production.apiBaseUrl; authIssuer = $backend.production.issuer; resource = $backend.resource; clientId = $backend.production.clientId }
    function Assert-Rejected([string]$Expected) {
        $backend | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $fixtureRoot 'backend/neon.json')
        $desktop | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $fixtureRoot 'KeepMeAlive/neon.json')
        $failure = $null
        try { & $script -RequireProduction -SkipBuild -Version '1.0.0' }
        catch { $failure = $_.Exception.Message }
        if (-not $failure -or -not $failure.Contains($Expected, [StringComparison]::Ordinal)) {
            throw "Expected rejection containing '$Expected', received '$failure'."
        }
    }
    $env:KEEP_ME_ALIVE_SMTP_VERIFIED = $null
    $env:KEEP_ME_ALIVE_OAUTH_VERIFIED = $null
    Assert-Rejected 'Production accounts require verified SMTP'
    $env:KEEP_ME_ALIVE_SMTP_VERIFIED = 'true'
    $env:KEEP_ME_ALIVE_OAUTH_VERIFIED = 'true'
    Assert-Rejected 'Production account acceptance has not been recorded'
    $backend.accountReleaseReady = $true
    $backend.production.clientId = ''
    Assert-Rejected 'The shipped desktop configuration must match'
    $backend.production.clientId = 'production-public-client'
    foreach ($field in @('clientId', 'authIssuer', 'apiBaseUrl', 'resource')) {
        $value = $desktop[$field]
        $desktop[$field] = 'https://wrong-environment.example.test/'
        Assert-Rejected 'The shipped desktop configuration must match'
        $desktop[$field] = $value
    }
    Assert-Rejected 'Incomplete self-contained portable output'
    Write-Output 'Production gates passed: missing acceptance, missing client and all four environment mismatches are rejected before packaging.'
}
finally {
    $env:KEEP_ME_ALIVE_SMTP_VERIFIED = $originalSmtp
    $env:KEEP_ME_ALIVE_OAUTH_VERIFIED = $originalOauth
    $resolvedFixture = [IO.Path]::GetFullPath($fixtureRoot)
    $allowedRoot = [IO.Path]::GetFullPath((Join-Path $repository 'artifacts')) + [IO.Path]::DirectorySeparatorChar
    if ($resolvedFixture.StartsWith($allowedRoot, [StringComparison]::OrdinalIgnoreCase) -and (Test-Path -LiteralPath $resolvedFixture)) {
        Remove-Item -LiteralPath $resolvedFixture -Recurse -Force
    }
}
