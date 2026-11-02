[CmdletBinding()]
param(
    [string]$Version,
    [string]$InnoCompiler,
    [switch]$SkipBuild,
    [switch]$RequireProduction,
    [switch]$ValidateOnly
)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$repository = [IO.Path]::GetFullPath((Split-Path $PSScriptRoot -Parent))
$artifactRoot = Join-Path $repository 'artifacts'
$publishDirectory = Join-Path $artifactRoot 'publish-portable'
$packageDirectory = Join-Path $artifactRoot 'packages'
$project = Join-Path $repository 'KeepMeAlive\KeepMeAlive.csproj'
$solution = Join-Path $repository 'KeepMeAlive.sln'
if (-not $Version) {
    [xml]$projectXml = Get-Content -LiteralPath $project
    $Version = [string]$projectXml.Project.PropertyGroup.Version
}
if ($Version -notmatch '^\d+\.\d+\.\d+(\.\d+)?$') { throw 'Version must contain three or four numeric components.' }
if ($RequireProduction -and ($env:KEEP_ME_ALIVE_SMTP_VERIFIED -ne 'true' -or $env:KEEP_ME_ALIVE_OAUTH_VERIFIED -ne 'true')) {
    throw 'Production accounts require verified SMTP delivery and a completed staging OAuth acceptance run.'
}
if ($RequireProduction) {
    $backendConfiguration = Get-Content -LiteralPath (Join-Path $repository 'backend/neon.json') -Raw | ConvertFrom-Json
    $desktopConfiguration = Get-Content -LiteralPath (Join-Path $repository 'KeepMeAlive/neon.json') -Raw | ConvertFrom-Json
    if ($backendConfiguration.accountReleaseReady -isnot [bool] -or -not $backendConfiguration.accountReleaseReady) {
        throw 'Production account acceptance has not been recorded in backend/neon.json.'
    }
    $production = $backendConfiguration.production
    if (-not $production.PSObject.Properties['clientId'] -or [string]::IsNullOrWhiteSpace($production.clientId) -or
        -not [string]::Equals($desktopConfiguration.clientId, $production.clientId, [StringComparison]::Ordinal) -or
        -not [string]::Equals($desktopConfiguration.apiBaseUrl, $production.apiBaseUrl, [StringComparison]::Ordinal) -or
        -not [string]::Equals($desktopConfiguration.authIssuer, $production.issuer, [StringComparison]::Ordinal) -or
        -not [string]::Equals($desktopConfiguration.resource, $backendConfiguration.resource, [StringComparison]::Ordinal)) {
        throw 'The shipped desktop configuration must match the verified production OAuth client, issuer, API and audience.'
    }
}
$signingCertificate = $null
$signToolPath = $null
if ($RequireProduction -or -not [string]::IsNullOrWhiteSpace($env:WINDOWS_SIGNING_CERT_THUMBPRINT)) {
    if ([string]::IsNullOrWhiteSpace($env:WINDOWS_SIGNING_CERT_THUMBPRINT)) {
        throw 'Production packages require WINDOWS_SIGNING_CERT_THUMBPRINT for a trusted signing certificate.'
    }
    if ([string]::IsNullOrWhiteSpace($env:WINDOWS_SIGNING_PUBLISHER)) {
        throw 'Signing requires WINDOWS_SIGNING_PUBLISHER with the exact expected certificate subject.'
    }
    if ($env:WINDOWS_SIGNING_CERT_THUMBPRINT -notmatch '^[0-9A-Fa-f]{40}$') {
        throw 'WINDOWS_SIGNING_CERT_THUMBPRINT must be a SHA-1 certificate thumbprint.'
    }
    $signingCertificate = Get-Item -LiteralPath ('Cert:\CurrentUser\My\' + $env:WINDOWS_SIGNING_CERT_THUMBPRINT)
    if ($signingCertificate.Subject -eq $signingCertificate.Issuer -or -not $signingCertificate.HasPrivateKey -or
        $signingCertificate.NotBefore -gt (Get-Date) -or $signingCertificate.NotAfter -lt (Get-Date) -or
        $signingCertificate.EnhancedKeyUsageList.ObjectId -notcontains '1.3.6.1.5.5.7.3.3') {
        throw 'The configured signing certificate must be current, non-self-issued, code-signing enabled and have its private key.'
    }
    if (-not [string]::Equals($signingCertificate.Subject, $env:WINDOWS_SIGNING_PUBLISHER, [StringComparison]::Ordinal)) {
        throw 'The configured signing certificate does not match WINDOWS_SIGNING_PUBLISHER.'
    }
    $signTool = Get-Command signtool.exe -ErrorAction SilentlyContinue
    if ($signTool) { $signToolPath = $signTool.Source }
    else {
        $sdkBin = Join-Path ${env:ProgramFiles(x86)} 'Windows Kits\10\bin'
        if (Test-Path -LiteralPath $sdkBin) {
            $signToolPath = Get-ChildItem -LiteralPath $sdkBin -Filter signtool.exe -Recurse |
                Where-Object FullName -Match '\\x64\\signtool.exe$' | Sort-Object FullName -Descending | Select-Object -First 1 -ExpandProperty FullName
        }
    }
    if (-not $signToolPath) { throw 'Windows SDK signtool.exe was not found.' }
}
if ($ValidateOnly) {
    if (-not $RequireProduction) { throw '-ValidateOnly requires -RequireProduction.' }
    Write-Output 'Production account configuration and signing prerequisites passed validation.'
    return
}
function Invoke-Checked([string]$Program, [string[]]$Arguments) {
    & $Program @Arguments
    if ($LASTEXITCODE -ne 0) { throw "$Program failed with exit code $LASTEXITCODE." }
}
function Assert-ArtifactPath([string]$Path) {
    $resolved = [IO.Path]::GetFullPath($Path)
    if (-not $resolved.StartsWith($artifactRoot + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) {
        throw 'A package output resolved outside the repository artifacts directory.'
    }
    $ancestor = $resolved
    while ($ancestor -and $ancestor -ne $repository) {
        if ((Test-Path -LiteralPath $ancestor) -and ((Get-Item -LiteralPath $ancestor -Force).Attributes -band [IO.FileAttributes]::ReparsePoint)) {
            throw 'Package output must not traverse a symbolic link or directory junction.'
        }
        $ancestor = [IO.Path]::GetDirectoryName($ancestor)
    }
}
function Sign-Artifact([string]$Path) {
    if (-not $signingCertificate) { return }
    Invoke-Checked $signToolPath @('sign','/sha1',$signingCertificate.Thumbprint,'/s','My','/fd','SHA256','/tr','http://timestamp.digicert.com','/td','SHA256',$Path)
    & (Join-Path $PSScriptRoot 'Assert-ArtifactSignature.ps1') -Path $Path -ExpectedPublisher $env:WINDOWS_SIGNING_PUBLISHER
}
Assert-ArtifactPath $publishDirectory
Assert-ArtifactPath $packageDirectory
if (-not $SkipBuild) {
    if (Test-Path -LiteralPath $publishDirectory) { Remove-Item -LiteralPath $publishDirectory -Recurse -Force }
    Invoke-Checked 'dotnet' @('restore',$solution,'--locked-mode')
    Invoke-Checked 'dotnet' @('test',$solution,'-c','Release','--no-restore')
    # Publish restores its runtime-specific assets; a RID-less restore cannot satisfy --no-restore here.
    Invoke-Checked 'dotnet' @('publish',$project,'-c','Release','-p:PublishProfile=Portable','-p:RestoreLockedMode=true',"-p:Version=$Version",'-o',$publishDirectory)
}
$executable = Join-Path $publishDirectory 'KeepMeAlive.exe'
foreach ($file in @('KeepMeAlive.exe','KeepMeAlive.dll','KeepMeAlive.runtimeconfig.json','coreclr.dll','KeepMeAlive.portable')) {
    if (-not (Test-Path -LiteralPath (Join-Path $publishDirectory $file))) { throw "Incomplete self-contained portable output: $file" }
}
Get-ChildItem -LiteralPath $publishDirectory -Filter '*.pdb' | Remove-Item -Force
New-Item -ItemType Directory -Path $packageDirectory -Force | Out-Null
Sign-Artifact $executable
$inventory = & dotnet list $solution package --include-transitive --format json
if ($LASTEXITCODE -ne 0) { throw 'Dependency inventory generation failed.' }
$inventory | Set-Content -LiteralPath (Join-Path $packageDirectory 'dependencies.json') -Encoding utf8
Copy-Item -LiteralPath (Join-Path $repository 'LICENSE') -Destination $publishDirectory -Force
Add-Type -AssemblyName System.IO.Compression
$zipPath = Join-Path $packageDirectory 'KeepMeAlive-Portable.zip'
if (Test-Path -LiteralPath $zipPath) { Remove-Item -LiteralPath $zipPath -Force }
$archiveStream = [IO.File]::Open($zipPath, [IO.FileMode]::CreateNew)
try {
    $archive = [IO.Compression.ZipArchive]::new($archiveStream, [IO.Compression.ZipArchiveMode]::Create, $true)
    try {
        foreach ($file in Get-ChildItem -LiteralPath $publishDirectory -File -Recurse | Sort-Object FullName) {
            $relative = [IO.Path]::GetRelativePath($publishDirectory, $file.FullName).Replace('\','/')
            $entry = $archive.CreateEntry($relative, [IO.Compression.CompressionLevel]::Optimal)
            $entry.LastWriteTime = [DateTimeOffset]::new(2000,1,1,0,0,0,[TimeSpan]::Zero)
            $destinationStream = $entry.Open()
            $sourceStream = $file.OpenRead()
            try { $sourceStream.CopyTo($destinationStream) }
            finally { $sourceStream.Dispose(); $destinationStream.Dispose() }
        }
    }
    finally { $archive.Dispose() }
}
finally { $archiveStream.Dispose() }
if (-not $InnoCompiler) {
    foreach ($candidate in @("$env:ProgramFiles\Inno Setup 7\ISCC.exe", "${env:ProgramFiles(x86)}\Inno Setup 7\ISCC.exe", "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe")) {
        if (Test-Path -LiteralPath $candidate) { $InnoCompiler = $candidate; break }
    }
}
if (-not $InnoCompiler) { throw 'Install Inno Setup from https://jrsoftware.org/isdl.php or pass -InnoCompiler.' }
Invoke-Checked $InnoCompiler @('/Q',"/DAppVersion=$Version","/DPublishDirectory=$publishDirectory","/DPackageDirectory=$packageDirectory",(Join-Path $repository 'installer\KeepMeAlive.iss'))
Sign-Artifact (Join-Path $packageDirectory 'KeepMeAlive-Setup.exe')
$checksums = Get-ChildItem -LiteralPath $packageDirectory -File | Where-Object Name -In @('KeepMeAlive-Portable.zip','KeepMeAlive-Setup.exe','dependencies.json') | Sort-Object Name | ForEach-Object {
    $hash = Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256
    $hash.Hash.ToLowerInvariant() + '  ' + $_.Name
}
$checksums | Set-Content -LiteralPath (Join-Path $packageDirectory 'SHA256SUMS.txt') -Encoding utf8
& (Join-Path $PSScriptRoot 'Test-Package.ps1') -PackageDirectory $packageDirectory -RequireProduction:$RequireProduction
Write-Output "Packages verified: $packageDirectory"
