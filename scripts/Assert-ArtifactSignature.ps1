[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$Path,
    [string]$ExpectedPublisher = $env:WINDOWS_SIGNING_PUBLISHER
)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
if ([string]::IsNullOrWhiteSpace($ExpectedPublisher)) {
    throw 'Signature validation requires WINDOWS_SIGNING_PUBLISHER with the exact expected certificate subject.'
}
$signature = Get-AuthenticodeSignature -LiteralPath $Path
if ($signature.Status -ne 'Valid' -or $signature.SignatureType -ne 'Authenticode' -or -not $signature.SignerCertificate) {
    throw "Artifact does not have a trusted embedded Authenticode signature: $Path"
}
if (-not [string]::Equals($signature.SignerCertificate.Subject, $ExpectedPublisher, [StringComparison]::Ordinal)) {
    throw "Artifact signer does not match WINDOWS_SIGNING_PUBLISHER: $Path"
}
if (-not $signature.TimeStamperCertificate) {
    throw "Artifact does not have a trusted signing timestamp: $Path"
}
