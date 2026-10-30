[CmdletBinding()]
param([Parameter(Mandatory)][string]$BaseUrl)
$ErrorActionPreference = 'Stop'
$uri = [Uri]$BaseUrl
if ($uri.Scheme -ne 'https' -or $uri.UserInfo) { throw 'A credential-free HTTPS development API URL is required.' }
$base = $BaseUrl.TrimEnd('/')
$platformHealth = Invoke-WebRequest -Uri ($base + '/healthz') -TimeoutSec 20
if ($platformHealth.StatusCode -ne 200) { throw 'Development platform health check failed.' }
$readiness = Invoke-RestMethod -Uri ($base + '/readyz') -TimeoutSec 20
if ($readiness.status -ne 'ok' -or $readiness.service -ne 'keepmealive') { throw 'Development backend readiness check failed.' }
$response = Invoke-WebRequest -Uri ($base + '/v1/account') -SkipHttpErrorCheck -TimeoutSec 20
if ($response.StatusCode -ne 401) { throw 'The account endpoint did not reject an unauthenticated caller.' }
Write-Output 'Development API health and unauthenticated account rejection passed.'
