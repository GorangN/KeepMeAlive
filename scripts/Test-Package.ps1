[CmdletBinding()]
param([string]$PackageDirectory = (Join-Path (Split-Path $PSScriptRoot -Parent) 'artifacts\packages'), [switch]$RequireProduction)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
Add-Type -AssemblyName System.IO.Compression.FileSystem
foreach ($file in @('KeepMeAlive-Portable.zip','KeepMeAlive-Setup.exe','dependencies.json','SHA256SUMS.txt')) {
    if (-not (Test-Path -LiteralPath (Join-Path $PackageDirectory $file))) { throw "Missing release artifact: $file" }
}
$archive = [IO.Compression.ZipFile]::OpenRead((Join-Path $PackageDirectory 'KeepMeAlive-Portable.zip'))
try {
    $names = @($archive.Entries | ForEach-Object FullName)
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
}
finally { $archive.Dispose() }
foreach ($line in Get-Content -LiteralPath (Join-Path $PackageDirectory 'SHA256SUMS.txt')) {
    if (-not $line.Trim()) { continue }
    $hash, $name = $line -split '  ', 2
    $actual = (Get-FileHash -LiteralPath (Join-Path $PackageDirectory $name) -Algorithm SHA256).Hash
    if ($actual -ne $hash) { throw "Checksum mismatch: $name" }
}
if ($RequireProduction) {
    if ($env:KEEP_ME_ALIVE_SMTP_VERIFIED -ne 'true' -or $env:KEEP_ME_ALIVE_OAUTH_VERIFIED -ne 'true') { throw 'Production account acceptance has not been recorded.' }
    if ((Get-AuthenticodeSignature -LiteralPath (Join-Path $PackageDirectory 'KeepMeAlive-Setup.exe')).Status -ne 'Valid') { throw 'Installer is not signed by a trusted publisher.' }
}
Write-Output 'Portable contents, installer, dependency inventory, and checksums passed validation.'
