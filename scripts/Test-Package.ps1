[CmdletBinding()]
param([string]$PackageDirectory = (Join-Path (Split-Path $PSScriptRoot -Parent) 'artifacts\packages'), [switch]$RequireProduction)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
Add-Type -AssemblyName System.IO.Compression.FileSystem
if ($RequireProduction) {
    if ($env:KEEP_ME_ALIVE_SMTP_VERIFIED -ne 'true' -or $env:KEEP_ME_ALIVE_OAUTH_VERIFIED -ne 'true') { throw 'Production account acceptance has not been recorded.' }
    if ([string]::IsNullOrWhiteSpace($env:WINDOWS_SIGNING_PUBLISHER)) { throw 'Production signature validation requires WINDOWS_SIGNING_PUBLISHER.' }
}
foreach ($file in @('KeepMeAlive-Portable.zip','KeepMeAlive-Setup.exe','dependencies.json','SHA256SUMS.txt')) {
    if (-not (Test-Path -LiteralPath (Join-Path $PackageDirectory $file))) { throw "Missing release artifact: $file" }
}
$expectedChecksums = @('KeepMeAlive-Portable.zip','KeepMeAlive-Setup.exe','dependencies.json')
$verifiedChecksums = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
foreach ($line in Get-Content -LiteralPath (Join-Path $PackageDirectory 'SHA256SUMS.txt')) {
    if (-not $line.Trim()) { continue }
    if ($line -cnotmatch '^(?<Hash>[0-9a-fA-F]{64})  (?<Name>KeepMeAlive-Portable\.zip|KeepMeAlive-Setup\.exe|dependencies\.json)$') {
        throw 'Malformed package checksum entry.'
    }
    $hash = $Matches.Hash
    $name = $Matches.Name
    if (-not $verifiedChecksums.Add($name)) { throw "Duplicate package checksum: $name" }
    $actual = (Get-FileHash -LiteralPath (Join-Path $PackageDirectory $name) -Algorithm SHA256).Hash
    if ($actual -ne $hash) { throw "Checksum mismatch: $name" }
}
foreach ($name in $expectedChecksums) {
    if (-not $verifiedChecksums.Contains($name)) { throw "Missing package checksum: $name" }
}
$archive = [IO.Compression.ZipFile]::OpenRead((Join-Path $PackageDirectory 'KeepMeAlive-Portable.zip'))
try {
    $names = @($archive.Entries | ForEach-Object FullName)
    $uniqueNames = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
    foreach ($name in $names) {
        if (-not $uniqueNames.Add($name)) { throw "Duplicate portable archive entry: $name" }
    }
    foreach ($file in @('KeepMeAlive.exe','KeepMeAlive.dll','KeepMeAlive.runtimeconfig.json','KeepMeAlive.portable','coreclr.dll','LICENSE','ThirdPartyNotices/Tabler-Icons.LICENSE.txt')) {
        if ($names -notcontains $file) { throw "Portable archive is incomplete: $file" }
    }
    if ($names -match '(^|/)(Data|Secrets)/|bootstrap\.json$|(^|/)settings\.json$|(^|/)\.env') { throw 'A package contains local user data.' }
    [xml]$profile = Get-Content -LiteralPath (Join-Path (Split-Path $PSScriptRoot -Parent) 'KeepMeAlive\Properties\PublishProfiles\Portable.pubxml')
    $expectedRuntime = $profile.SelectSingleNode('/Project/PropertyGroup/RuntimeFrameworkVersion').InnerText
    $reader = [IO.StreamReader]::new($archive.GetEntry('KeepMeAlive.runtimeconfig.json').Open())
    try { $runtime = $reader.ReadToEnd() | ConvertFrom-Json }
    finally { $reader.Dispose() }
    foreach ($framework in $runtime.runtimeOptions.includedFrameworks) {
        if ($framework.version -ne $expectedRuntime) { throw "The portable archive bundles an unexpected runtime patch: $($framework.version)" }
    }
    if ($RequireProduction) {
        $signatureRoot = [IO.Path]::GetFullPath([IO.Path]::GetTempPath()).TrimEnd([IO.Path]::DirectorySeparatorChar) + [IO.Path]::DirectorySeparatorChar
        $signatureDirectory = [IO.Path]::GetFullPath((Join-Path $signatureRoot ('keepmealive-signature-' + [Guid]::NewGuid().ToString('N'))))
        if (-not $signatureDirectory.StartsWith($signatureRoot, [StringComparison]::OrdinalIgnoreCase)) { throw 'Signature extraction resolved outside the temporary directory.' }
        New-Item -ItemType Directory -Path $signatureDirectory | Out-Null
        try {
            $archivedExecutable = Join-Path $signatureDirectory 'KeepMeAlive.exe'
            [IO.Compression.ZipFileExtensions]::ExtractToFile($archive.GetEntry('KeepMeAlive.exe'), $archivedExecutable)
            & (Join-Path $PSScriptRoot 'Assert-ArtifactSignature.ps1') -Path $archivedExecutable
        }
        finally { Remove-Item -LiteralPath $signatureDirectory -Recurse -Force }
    }
}
finally { $archive.Dispose() }
if ($RequireProduction) {
    & (Join-Path $PSScriptRoot 'Assert-ArtifactSignature.ps1') -Path (Join-Path $PackageDirectory 'KeepMeAlive-Setup.exe')
}
Write-Output 'Portable contents, installer, dependency inventory, and checksums passed validation.'
