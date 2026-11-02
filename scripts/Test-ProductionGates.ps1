[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
$repository = [IO.Path]::GetFullPath((Split-Path $PSScriptRoot -Parent))
$fixtureRoot = Join-Path $repository ('artifacts/production-gates-' + [Guid]::NewGuid().ToString('N'))
$originalSmtp = $env:KEEP_ME_ALIVE_SMTP_VERIFIED
$originalOauth = $env:KEEP_ME_ALIVE_OAUTH_VERIFIED
$originalThumbprint = $env:WINDOWS_SIGNING_CERT_THUMBPRINT
$originalPublisher = $env:WINDOWS_SIGNING_PUBLISHER
$caseCount = 0
$testCertificate = [pscustomobject]@{
    Thumbprint = 'A' * 40
    Subject = 'CN=KeepMeAlive Test Publisher'
    Issuer = 'CN=Fixture Certificate Authority'
    HasPrivateKey = $true
    NotBefore = (Get-Date).AddDays(-1)
    NotAfter = (Get-Date).AddDays(30)
    EnhancedKeyUsageList = @([pscustomobject]@{ ObjectId = '1.3.6.1.5.5.7.3.3' })
}
function Get-Item {
    [CmdletBinding()]
    param([string]$LiteralPath, [switch]$Force)
    if ($LiteralPath.StartsWith('Cert:\', [StringComparison]::OrdinalIgnoreCase)) { return $testCertificate }
    Microsoft.PowerShell.Management\Get-Item @PSBoundParameters
}
function Get-Command {
    [CmdletBinding()]
    param([string]$Name)
    if ($Name -eq 'signtool.exe') { return [pscustomobject]@{ Source = 'fixture-signtool.exe' } }
    Microsoft.PowerShell.Core\Get-Command @PSBoundParameters
}
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
        $script:caseCount++
    }
    $env:KEEP_ME_ALIVE_SMTP_VERIFIED = $null
    $env:KEEP_ME_ALIVE_OAUTH_VERIFIED = $null
    $env:WINDOWS_SIGNING_CERT_THUMBPRINT = $null
    $env:WINDOWS_SIGNING_PUBLISHER = $null
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
    Assert-Rejected 'Production packages require WINDOWS_SIGNING_CERT_THUMBPRINT'
    $env:WINDOWS_SIGNING_CERT_THUMBPRINT = $testCertificate.Thumbprint
    Assert-Rejected 'Signing requires WINDOWS_SIGNING_PUBLISHER'
    $env:WINDOWS_SIGNING_PUBLISHER = 'CN=Wrong Publisher'
    Assert-Rejected 'does not match WINDOWS_SIGNING_PUBLISHER'
    $env:WINDOWS_SIGNING_PUBLISHER = $testCertificate.Subject
    $env:WINDOWS_SIGNING_CERT_THUMBPRINT = '../invalid-certificate'
    Assert-Rejected 'must be a SHA-1 certificate thumbprint'
    $env:WINDOWS_SIGNING_CERT_THUMBPRINT = $testCertificate.Thumbprint
    foreach ($invalidProperty in @('HasPrivateKey', 'NotBefore', 'NotAfter', 'Issuer', 'EnhancedKeyUsageList')) {
        $value = $testCertificate.$invalidProperty
        $testCertificate.$invalidProperty = switch ($invalidProperty) {
            'HasPrivateKey' { $false }
            'NotBefore' { (Get-Date).AddDays(1) }
            'NotAfter' { (Get-Date).AddDays(-1) }
            'Issuer' { $testCertificate.Subject }
            'EnhancedKeyUsageList' { @([pscustomobject]@{ ObjectId = '1.3.6.1.5.5.7.3.1' }) }
        }
        Assert-Rejected 'must be current, non-self-issued, code-signing enabled and have its private key'
        $testCertificate.$invalidProperty = $value
    }
    & $script -RequireProduction -ValidateOnly -Version '1.0.0'
    $caseCount++
    Assert-Rejected 'Incomplete self-contained portable output'
    Write-Output "Production gates passed: $caseCount cases covering acceptance, environment isolation, signer inputs, publisher, certificate capability and preflight with isolated mocks."
}
finally {
    $env:KEEP_ME_ALIVE_SMTP_VERIFIED = $originalSmtp
    $env:KEEP_ME_ALIVE_OAUTH_VERIFIED = $originalOauth
    $env:WINDOWS_SIGNING_CERT_THUMBPRINT = $originalThumbprint
    $env:WINDOWS_SIGNING_PUBLISHER = $originalPublisher
    $resolvedFixture = [IO.Path]::GetFullPath($fixtureRoot)
    $allowedRoot = [IO.Path]::GetFullPath((Join-Path $repository 'artifacts')) + [IO.Path]::DirectorySeparatorChar
    if ($resolvedFixture.StartsWith($allowedRoot, [StringComparison]::OrdinalIgnoreCase) -and (Test-Path -LiteralPath $resolvedFixture)) {
        Remove-Item -LiteralPath $resolvedFixture -Recurse -Force
    }
}
