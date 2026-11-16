param([Parameter(Mandatory)][string]$OutputRoot)
$ErrorActionPreference = 'Stop'
if (Test-Path -LiteralPath $OutputRoot) { throw 'Choose a new output directory.' }
$project = Join-Path $PSScriptRoot '..\CodeLines.Cli\CodeLines.Cli.csproj'
$publishRecords = [Collections.Generic.List[object]]::new()
foreach ($variant in @('JIT-FDD','JIT-SCD','NativeAOT')) {
    $out = Join-Path $OutputRoot $variant
    $common = @('publish', $project, '-c', 'Release', '-r', 'win-x64', '-o', $out, '--artifacts-path', (Join-Path $OutputRoot ('.build-' + $variant)), '-p:PublishReadyToRun=false', '-p:PublishSingleFile=false', '-p:GeneratePackageOnBuild=false', '-m:1', '-p:UseSharedCompilation=false')
    if ($variant -eq 'JIT-FDD') { $options = @('--self-contained', 'false', '-p:PublishAot=false') }
    elseif ($variant -eq 'JIT-SCD') { $options = @('--self-contained', 'true', '-p:PublishAot=false') }
    else { $options = @('--self-contained', 'true', '-p:PublishAot=true') }
    $watch = [Diagnostics.Stopwatch]::StartNew()
    & dotnet @common @options
    if ($LASTEXITCODE -ne 0) { throw "Publish failed: $variant" }
    $watch.Stop()
    $bytes = (Get-ChildItem $out -File -Recurse | Measure-Object Length -Sum).Sum
    $publishRecords.Add([pscustomobject]@{variant=$variant; elapsedMs=$watch.Elapsed.TotalMilliseconds; bytes=$bytes})
    Write-Host "$variant publish: $($watch.Elapsed.TotalSeconds) s; $bytes bytes"
}

$publishRecords | ConvertTo-Json | Set-Content -Encoding utf8 (Join-Path $OutputRoot 'publish-results.json')
