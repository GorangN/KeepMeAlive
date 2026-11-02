[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
Add-Type -AssemblyName System.IO.Compression.FileSystem
$repository = [IO.Path]::GetFullPath((Split-Path $PSScriptRoot -Parent))
$fixtureRoot = Join-Path $repository ('artifacts/signing-gates-' + [Guid]::NewGuid().ToString('N'))
$packageDirectory = Join-Path $fixtureRoot 'packages'
$signatureScript = Join-Path $PSScriptRoot 'Assert-ArtifactSignature.ps1'
$packageScript = Join-Path $PSScriptRoot 'Test-Package.ps1'
$originalSmtp = $env:KEEP_ME_ALIVE_SMTP_VERIFIED
$originalOauth = $env:KEEP_ME_ALIVE_OAUTH_VERIFIED
$originalPublisher = $env:WINDOWS_SIGNING_PUBLISHER
$observedSignatures = [Collections.Generic.List[string]]::new()
$caseCount = 0
function Get-AuthenticodeSignature {
    [CmdletBinding()]
    param([string]$LiteralPath)
    $observedSignatures.Add([IO.Path]::GetFullPath($LiteralPath))
    $payload = (Get-Content -LiteralPath $LiteralPath -Raw).Trim()
    [pscustomobject]@{
        Status = if ($payload -eq 'UNSIGNED') { 'NotSigned' } elseif ($payload -eq 'TAMPERED') { 'HashMismatch' } else { 'Valid' }
        SignatureType = if ($payload -eq 'CATALOG') { 'Catalog' } else { 'Authenticode' }
        SignerCertificate = [pscustomobject]@{ Subject = if ($payload -eq 'WRONG_PUBLISHER') { 'CN=Unexpected Publisher' } else { 'CN=KeepMeAlive Test Publisher' } }
        TimeStamperCertificate = if ($payload -eq 'NO_TIMESTAMP') { $null } else { [pscustomobject]@{ Subject = 'CN=Fixture Timestamp Authority' } }
    }
}
function Assert-Rejected([scriptblock]$Action, [string]$Expected) {
    $failure = $null
    try { & $Action | Out-Null }
    catch { $failure = $_.Exception.Message }
    if (-not $failure -or -not $failure.Contains($Expected, [StringComparison]::Ordinal)) {
        throw "Expected rejection containing '$Expected', received '$failure'."
    }
    $script:caseCount++
}
function Write-Checksums {
    $lines = foreach ($name in @('KeepMeAlive-Portable.zip','KeepMeAlive-Setup.exe','dependencies.json')) {
        (Get-FileHash -LiteralPath (Join-Path $packageDirectory $name) -Algorithm SHA256).Hash.ToLowerInvariant() + '  ' + $name
    }
    $lines | Set-Content -LiteralPath (Join-Path $packageDirectory 'SHA256SUMS.txt')
}
function New-PackageFixture([string]$PortablePayload = 'VALID', [string]$InstallerPayload = 'VALID', [switch]$DuplicateExecutable) {
    [xml]$profile = Get-Content -LiteralPath (Join-Path $repository 'KeepMeAlive/Properties/PublishProfiles/Portable.pubxml')
    $runtime = @{ runtimeOptions = @{ includedFrameworks = @(@{ name = 'Microsoft.NETCore.App'; version = $profile.SelectSingleNode('/Project/PropertyGroup/RuntimeFrameworkVersion').InnerText }) } } | ConvertTo-Json -Depth 5
    $entries = [ordered]@{
        'KeepMeAlive.exe' = $PortablePayload
        'KeepMeAlive.dll' = 'fixture'
        'KeepMeAlive.runtimeconfig.json' = $runtime
        'KeepMeAlive.portable' = ''
        'coreclr.dll' = 'fixture'
        'LICENSE' = 'fixture'
        'ThirdPartyNotices/Tabler-Icons.LICENSE.txt' = 'fixture'
    }
    $stream = [IO.File]::Open((Join-Path $packageDirectory 'KeepMeAlive-Portable.zip'), [IO.FileMode]::Create)
    $archive = [IO.Compression.ZipArchive]::new($stream, [IO.Compression.ZipArchiveMode]::Create)
    try {
        foreach ($entry in $entries.GetEnumerator()) {
            $writer = [IO.StreamWriter]::new($archive.CreateEntry($entry.Key).Open())
            try { $writer.Write($entry.Value) }
            finally { $writer.Dispose() }
        }
        if ($DuplicateExecutable) {
            $writer = [IO.StreamWriter]::new($archive.CreateEntry('keepmealive.exe').Open())
            try { $writer.Write('UNSIGNED') }
            finally { $writer.Dispose() }
        }
    }
    finally { $archive.Dispose() }
    $InstallerPayload | Set-Content -LiteralPath (Join-Path $packageDirectory 'KeepMeAlive-Setup.exe')
    '{}' | Set-Content -LiteralPath (Join-Path $packageDirectory 'dependencies.json')
    Write-Checksums
}
try {
    New-Item -ItemType Directory -Path $packageDirectory -Force | Out-Null
    $env:KEEP_ME_ALIVE_SMTP_VERIFIED = 'true'
    $env:KEEP_ME_ALIVE_OAUTH_VERIFIED = 'true'
    $env:WINDOWS_SIGNING_PUBLISHER = 'CN=KeepMeAlive Test Publisher'
    $artifact = Join-Path $fixtureRoot 'signature.exe'
    foreach ($payload in @('UNSIGNED', 'TAMPERED', 'CATALOG')) {
        $payload | Set-Content -LiteralPath $artifact
        Assert-Rejected { & $signatureScript -Path $artifact } 'trusted embedded Authenticode signature'
    }
    'WRONG_PUBLISHER' | Set-Content -LiteralPath $artifact
    Assert-Rejected { & $signatureScript -Path $artifact } 'does not match WINDOWS_SIGNING_PUBLISHER'
    'NO_TIMESTAMP' | Set-Content -LiteralPath $artifact
    Assert-Rejected { & $signatureScript -Path $artifact } 'does not have a trusted signing timestamp'
    'VALID' | Set-Content -LiteralPath $artifact
    & $signatureScript -Path $artifact
    $caseCount++
    New-PackageFixture -PortablePayload 'UNSIGNED' -InstallerPayload 'UNSIGNED'
    $observedSignatures.Clear()
    & $packageScript -PackageDirectory $packageDirectory | Out-Null
    if ($observedSignatures.Count -ne 0) { throw 'Unsigned validation packages must not require a signing identity.' }
    $caseCount++
    New-PackageFixture
    & $packageScript -PackageDirectory $packageDirectory -RequireProduction | Out-Null
    if ($observedSignatures.Count -ne 2 -or -not $observedSignatures[0].EndsWith('KeepMeAlive.exe', [StringComparison]::Ordinal) -or
        -not $observedSignatures[1].EndsWith('KeepMeAlive-Setup.exe', [StringComparison]::Ordinal)) {
        throw 'Production validation must inspect the archived executable and final installer.'
    }
    if (Test-Path -LiteralPath $observedSignatures[0]) { throw 'Extracted signature fixture was not cleaned up.' }
    $caseCount++
    foreach ($payload in @('UNSIGNED', 'WRONG_PUBLISHER', 'NO_TIMESTAMP')) {
        $expected = switch ($payload) {
            'UNSIGNED' { 'trusted embedded Authenticode signature' }
            'WRONG_PUBLISHER' { 'does not match WINDOWS_SIGNING_PUBLISHER' }
            'NO_TIMESTAMP' { 'does not have a trusted signing timestamp' }
        }
        New-PackageFixture -PortablePayload $payload
        Assert-Rejected { & $packageScript -PackageDirectory $packageDirectory -RequireProduction } $expected
        New-PackageFixture -InstallerPayload $payload
        Assert-Rejected { & $packageScript -PackageDirectory $packageDirectory -RequireProduction } $expected
    }
    New-PackageFixture -DuplicateExecutable
    Assert-Rejected { & $packageScript -PackageDirectory $packageDirectory -RequireProduction } 'Duplicate portable archive entry'
    New-PackageFixture
    'changed after signing' | Add-Content -LiteralPath (Join-Path $packageDirectory 'KeepMeAlive-Setup.exe')
    Assert-Rejected { & $packageScript -PackageDirectory $packageDirectory -RequireProduction } 'Checksum mismatch: KeepMeAlive-Setup.exe'
    New-PackageFixture
    'changed after ZIP creation' | Add-Content -LiteralPath (Join-Path $packageDirectory 'KeepMeAlive-Portable.zip')
    Assert-Rejected { & $packageScript -PackageDirectory $packageDirectory -RequireProduction } 'Checksum mismatch: KeepMeAlive-Portable.zip'
    New-PackageFixture
    '' | Set-Content -LiteralPath (Join-Path $packageDirectory 'SHA256SUMS.txt')
    Assert-Rejected { & $packageScript -PackageDirectory $packageDirectory -RequireProduction } 'Missing package checksum'
    New-PackageFixture
    $checksum = Get-Content -LiteralPath (Join-Path $packageDirectory 'SHA256SUMS.txt') -TotalCount 1
    $checksum | Add-Content -LiteralPath (Join-Path $packageDirectory 'SHA256SUMS.txt')
    Assert-Rejected { & $packageScript -PackageDirectory $packageDirectory -RequireProduction } 'Duplicate package checksum'
    ('a' * 64 + '  ../outside.exe') | Set-Content -LiteralPath (Join-Path $packageDirectory 'SHA256SUMS.txt')
    Assert-Rejected { & $packageScript -PackageDirectory $packageDirectory -RequireProduction } 'Malformed package checksum entry'
    New-PackageFixture
    $env:WINDOWS_SIGNING_PUBLISHER = $null
    Assert-Rejected { & $packageScript -PackageDirectory $packageDirectory -RequireProduction } 'requires WINDOWS_SIGNING_PUBLISHER'
    Write-Output "Signing gates passed: $caseCount cases covering trust, publisher, timestamps, archived executables and final checksums. Certificate results were mocked; no keys were installed or used."
}
finally {
    $env:KEEP_ME_ALIVE_SMTP_VERIFIED = $originalSmtp
    $env:KEEP_ME_ALIVE_OAUTH_VERIFIED = $originalOauth
    $env:WINDOWS_SIGNING_PUBLISHER = $originalPublisher
    $resolvedFixture = [IO.Path]::GetFullPath($fixtureRoot)
    $allowedRoot = [IO.Path]::GetFullPath((Join-Path $repository 'artifacts')) + [IO.Path]::DirectorySeparatorChar
    if ($resolvedFixture.StartsWith($allowedRoot, [StringComparison]::OrdinalIgnoreCase) -and (Test-Path -LiteralPath $resolvedFixture)) {
        Remove-Item -LiteralPath $resolvedFixture -Recurse -Force
    }
}
