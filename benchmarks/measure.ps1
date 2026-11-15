param(
    [Parameter(Mandatory)][string]$PublishRoot,
    [Parameter(Mandatory)][string]$FixtureRoot,
    [Parameter(Mandatory)][string]$OutputDirectory,
    [int]$Warmups = 2,
    [int]$Iterations = 10
)
$ErrorActionPreference = 'Stop'
if ($Warmups -lt 0 -or $Iterations -lt 1) { throw 'Invalid run counts.' }
if (Test-Path -LiteralPath $OutputDirectory) { throw 'Choose a new output directory.' }
foreach ($v in @('JIT-FDD','JIT-SCD','NativeAOT')) { if (!(Test-Path (Join-Path $PublishRoot "$v\CodeLines.Cli.exe"))) { throw "Missing executable: $v" } }
foreach ($size in @('small','medium')) { if (!(Test-Path (Join-Path $FixtureRoot "$size\.clineIgnore"))) { throw "Missing fixture: $size" } }
New-Item -ItemType Directory $OutputDirectory | Out-Null
$records = [Collections.Generic.List[object]]::new()
foreach ($variant in @('JIT-FDD','JIT-SCD','NativeAOT')) {
    $exe = (Resolve-Path (Join-Path $PublishRoot "$variant\CodeLines.Cli.exe")).Path
    foreach ($scenario in @('help','small-first','medium-first','repeat','modified')) {
        for ($i = -$Warmups; $i -lt $Iterations; $i++) {
            $work = Join-Path $OutputDirectory ("work-$variant-$scenario-$($i + $Warmups)")
            $size = if ($scenario -eq 'medium-first') { 'medium' } else { 'small' }
            Copy-Item -LiteralPath (Join-Path $FixtureRoot $size) -Destination $work -Recurse
            if ($scenario -in @('repeat','modified')) {
                & $exe scan --path $work | Out-Null
                if ($LASTEXITCODE -ne 0) { throw "Preparation failed: $variant $scenario" }
            }
            if ($scenario -eq 'modified') { Add-Content -LiteralPath (Join-Path $work 'File00000.cs') -Value '// changed' }
            $watch = [Diagnostics.Stopwatch]::StartNew()
            if ($scenario -eq 'help') { $output = & $exe --help } else { $output = & $exe scan --path $work }
            $code = $LASTEXITCODE
            $watch.Stop()
            if ($code -ne 0) { throw "Run failed: $variant $scenario" }
            $summary = @($output | Where-Object { $_ -match '^(Files total|Lines of code total)' }) -join '; '
            $records.Add([pscustomobject]@{ variant=$variant; scenario=$scenario; iteration=$i; warmup=($i -lt 0); elapsedMs=$watch.Elapsed.TotalMilliseconds; summary=$summary; peakMemoryBytes=$null })
        }
    }
}
foreach ($scenario in @('small-first','medium-first','repeat','modified')) {
    $summaries = @($records | Where-Object { $_.scenario -eq $scenario } | Select-Object -ExpandProperty summary -Unique)
    if ($summaries.Count -ne 1 -or !$summaries[0]) { throw "Results differ for $scenario; inspect the retained fixture copies." }
}
$aggregates = foreach ($group in ($records | Where-Object { !$_.warmup } | Group-Object variant,scenario)) {
    $values = @($group.Group | ForEach-Object { $_.elapsedMs } | Sort-Object)
    $middle = [int][Math]::Floor($values.Count / 2)
    $median = if ($values.Count % 2) { $values[$middle] } else { ($values[$middle - 1] + $values[$middle]) / 2 }
    [pscustomobject]@{ variant=$group.Group[0].variant; scenario=$group.Group[0].scenario; medianMs=$median; minMs=$values[0]; maxMs=$values[-1] }
}
$gitSha = & git -C (Join-Path $PSScriptRoot '..') rev-parse HEAD
if ($LASTEXITCODE -ne 0) { throw 'Could not read Git SHA.' }
$fixtureStats = foreach ($size in @('small','medium')) { $files = @(Get-ChildItem (Join-Path $FixtureRoot $size) -File -Recurse); [pscustomobject]@{name=$size; files=$files.Count; bytes=($files | Measure-Object Length -Sum).Sum} }
$gitStatus = & git -C (Join-Path $PSScriptRoot '..') status --short
$metadata = [ordered]@{ fixtureStats=$fixtureStats; gitStatus=@($gitStatus); os=[Runtime.InteropServices.RuntimeInformation]::OSDescription; architecture=[Runtime.InteropServices.RuntimeInformation]::OSArchitecture.ToString(); sdk=(& dotnet --version); runtimes=(& dotnet --list-runtimes); rid='win-x64'; configuration='Release'; gitSha=$gitSha; publishOptions='FDD/SCD/AOT; ReadyToRun=false; PublishSingleFile=false'; fixtureRoot=(Resolve-Path $FixtureRoot).Path; warmups=$Warmups; iterations=$Iterations; note='Elapsed process invocation; OS cache is not controlled. Peak memory not measured.' }
[pscustomobject]@{ metadata=$metadata; runs=$records; aggregates=$aggregates } | ConvertTo-Json -Depth 6 | Set-Content -Encoding utf8 (Join-Path $OutputDirectory 'results.json')
$records | Export-Csv -NoTypeInformation -Encoding utf8 (Join-Path $OutputDirectory 'results.csv')
Write-Host "Raw results written to $OutputDirectory; fixture copies are retained for inspection."
