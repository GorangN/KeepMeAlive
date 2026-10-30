[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
$repository = Split-Path $PSScriptRoot -Parent
$outputDirectory = Join-Path $repository 'artifacts\audit'
New-Item -ItemType Directory -Path $outputDirectory -Force | Out-Null
$report = & dotnet list (Join-Path $repository 'KeepMeAlive.sln') package --vulnerable --include-transitive --format json
if ($LASTEXITCODE -ne 0) { throw 'NuGet vulnerability audit failed to run.' }
$report | Set-Content -LiteralPath (Join-Path $outputDirectory 'nuget-vulnerabilities.json') -Encoding utf8
$data = ($report -join "`n") | ConvertFrom-Json -AsHashtable
function Find-Vulnerabilities($Node) {
    if ($Node -is [Collections.IDictionary]) {
        if ($Node.Contains('vulnerabilities') -and @($Node['vulnerabilities']).Count -gt 0) { throw 'Known vulnerable NuGet dependencies were found. See the audit report.' }
        foreach ($child in $Node.Values) { Find-Vulnerabilities $child }
    }
    elseif ($Node -is [Collections.IEnumerable] -and $Node -isnot [string]) {
        foreach ($child in $Node) { Find-Vulnerabilities $child }
    }
}
Find-Vulnerabilities $data
Write-Output 'No vulnerable direct or transitive NuGet dependencies were reported.'
