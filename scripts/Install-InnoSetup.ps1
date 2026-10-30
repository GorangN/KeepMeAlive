[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
$repository = [IO.Path]::GetFullPath((Split-Path $PSScriptRoot -Parent))
$toolDirectory = Join-Path $repository 'artifacts\tools\inno-6.7.4'
$compiler = Join-Path $toolDirectory 'ISCC.exe'
if (-not (Test-Path -LiteralPath $compiler)) {
    $downloadPath = Join-Path ([IO.Path]::GetTempPath()) ('keepmealive-inno-' + [Guid]::NewGuid().ToString('N') + '.exe')
    try {
        Invoke-WebRequest -Uri 'https://github.com/jrsoftware/issrc/releases/download/is-6_7_4/innosetup-6.7.4.exe' -OutFile $downloadPath
        $signature = Get-AuthenticodeSignature -LiteralPath $downloadPath
        if ($signature.Status -ne 'Valid' -or $signature.SignerCertificate.Subject -notmatch 'Pyrsys B\.V\.') {
            throw 'The Inno Setup download did not have the expected trusted publisher signature.'
        }
        $process = Start-Process -FilePath $downloadPath -ArgumentList @('/VERYSILENT','/SUPPRESSMSGBOXES','/NORESTART','/SP-','/CURRENTUSER',('/DIR="' + $toolDirectory + '"')) -WindowStyle Hidden -Wait -PassThru
        if ($process.ExitCode -ne 0) { throw "Inno Setup installation failed: $($process.ExitCode)" }
    }
    finally { if (Test-Path -LiteralPath $downloadPath) { Remove-Item -LiteralPath $downloadPath -Force } }
}
if (-not (Test-Path -LiteralPath $compiler)) { throw 'The pinned Inno Setup compiler is unavailable.' }
Write-Output $compiler
