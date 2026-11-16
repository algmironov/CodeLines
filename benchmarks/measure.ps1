param(
    [Parameter(Mandatory)][string]$PublishRoot,
    [Parameter(Mandatory)][string]$FixtureRoot,
    [Parameter(Mandatory)][string]$OutputDirectory,
    [int]$Warmups = 2,
    [int]$Iterations = 10,
    [int]$Series = 1,
    [ValidateSet('Rotating','Grouped')][string]$Order = 'Rotating'
)
$ErrorActionPreference = 'Stop'
if ($Warmups -lt 0 -or $Iterations -lt 1 -or $Series -lt 1) { throw 'Invalid run counts.' }
if (Test-Path -LiteralPath $OutputDirectory) { throw 'Choose a new output directory.' }
$variants = @('JIT-FDD','JIT-SCD','NativeAOT')
foreach ($v in $variants) { if (!(Test-Path (Join-Path $PublishRoot "$v\CodeLines.Cli.exe"))) { throw "Missing executable: $v" } }
$manifestPath = Join-Path $FixtureRoot 'manifest.json'
if (Test-Path -LiteralPath $manifestPath) {
    $manifest = Get-Content -Raw -LiteralPath $manifestPath | ConvertFrom-Json
} else {
    Write-Warning 'Legacy fixtures: deriving expected counts; large scenario is omitted if absent.'
    $sets = foreach ($size in @('small','medium','large')) {
        $dir = Join-Path $FixtureRoot $size
        if (Test-Path $dir) {
            $files = @(Get-ChildItem -LiteralPath $dir -Filter '*.cs' -File -Recurse)
            [pscustomobject]@{ name=$size; files=$files.Count; sourceBytes=($files | Measure-Object Length -Sum).Sum; expectedLines=24*$files.Count }
        }
    }
    $manifest = [pscustomobject]@{ seed=$null; requestedAverageFileBytes=$null; sets=@($sets); legacy=$true }
}
$fixtureSizes = if ($manifest.legacy -and !(Test-Path (Join-Path $FixtureRoot 'large'))) { @('small','medium') } else { @('small','medium','large') }
foreach ($size in $fixtureSizes) {
    $dir = Join-Path $FixtureRoot $size
    if (!(Test-Path (Join-Path $dir '.clineIgnore'))) { throw "Missing fixture: $size" }
    if (Test-Path (Join-Path $dir '.cline')) { throw "Fixture contains state: $size" }
    $expected = $manifest.sets | Where-Object name -eq $size | Select-Object -First 1
    $files = @(Get-ChildItem -LiteralPath $dir -Filter '*.cs' -File -Recurse)
    if (!$expected -or $files.Count -ne $expected.files -or ($files | Measure-Object Length -Sum).Sum -ne $expected.sourceBytes) { throw "Fixture differs from manifest: $size" }
}
New-Item -ItemType Directory $OutputDirectory | Out-Null
$records = [Collections.Generic.List[object]]::new()
$scenarios = @('help','small-first','medium-first','repeat','modified')
if ($fixtureSizes -contains 'large') { $scenarios += 'large-first' }
$sequence = 0
function Run-One($variant, $scenario, $seriesIndex, $iteration) {
    $script:sequence++
    $exe = (Resolve-Path (Join-Path $PublishRoot "$variant\CodeLines.Cli.exe")).Path
    $size = if ($scenario -eq 'medium-first') { 'medium' } elseif ($scenario -eq 'large-first') { 'large' } else { 'small' }
    $work = Join-Path $OutputDirectory ("work-{0:D7}" -f $script:sequence)
    Copy-Item -LiteralPath (Join-Path $FixtureRoot $size) -Destination $work -Recurse
    $expected = $manifest.sets | Where-Object name -eq $size | Select-Object -First 1
    if ($scenario -in @('repeat','modified')) {
        $prepared = & $exe scan --path $work
        if ($LASTEXITCODE -ne 0 -or "$prepared" -notmatch "Files total: $($expected.files)(\s|$)" -or "$prepared" -notmatch "Lines of code total .*: $($expected.expectedLines)(\s|$)") { throw "Preparation failed: $variant $scenario" }
    }
    if ($scenario -eq 'modified') { Add-Content -LiteralPath (Join-Path $work 'File00000.cs') -Value '// changed' }
    $watch = [Diagnostics.Stopwatch]::StartNew()
    if ($scenario -eq 'help') { $output = & $exe --help } else { $output = & $exe scan --path $work }
    $code = $LASTEXITCODE
    $watch.Stop()
    if ($code -ne 0) { throw "Run failed: $variant $scenario, exit $code" }
    if ($scenario -eq 'help' -and "$output" -notmatch 'scan') { throw "Help missing scan command: $variant" }
    if ($scenario -ne 'help') {
        if ("$output" -notmatch "Files total: $($expected.files)(\s|$)" -or "$output" -notmatch "Lines of code total .*: $($expected.expectedLines)(\s|$)") { throw "Unexpected totals: $variant $scenario" }
        $skip = if ($scenario -eq 'repeat') { $expected.files } elseif ($scenario -eq 'modified') { $expected.files - 1 } else { 0 }
        if ($skip -gt 0 -and "$output" -notmatch "Skipped: $skip unchanged files") { throw "Unexpected cache use: $variant $scenario" }
    }
    $summary = @($output | Where-Object { $_ -match '^(Files total|Lines of code total)' }) -join '; '
    $records.Add([pscustomobject]@{ sequence=$script:sequence; variant=$variant; scenario=$scenario; series=$seriesIndex; iteration=$iteration; warmup=($iteration -lt 0); elapsedMs=$watch.Elapsed.TotalMilliseconds; summary=$summary; peakMemoryBytes=$null })
}
foreach ($seriesIndex in 1..$Series) {
    foreach ($scenario in $scenarios) {
        if ($Order -eq 'Grouped') {
            foreach ($variant in $variants) { for ($i = -$Warmups; $i -lt $Iterations; $i++) { Run-One $variant $scenario $seriesIndex $i } }
        } else {
            for ($i = -$Warmups; $i -lt $Iterations; $i++) {
                $offset = ($i + $Warmups + $seriesIndex - 1) % $variants.Count
                for ($v = 0; $v -lt $variants.Count; $v++) { Run-One $variants[($v + $offset) % $variants.Count] $scenario $seriesIndex $i }
            }
        }
    }
}
foreach ($scenario in $scenarios | Where-Object { $_ -ne 'help' }) {
    $summaries = @($records | Where-Object scenario -eq $scenario | Select-Object -ExpandProperty summary -Unique)
    if ($summaries.Count -ne 1 -or !$summaries[0]) { throw "Results differ for $scenario; inspect retained fixture copies." }
}
$aggregates = foreach ($group in ($records | Where-Object { !$_.warmup } | Group-Object variant,scenario)) {
    $values = @($group.Group | ForEach-Object { $_.elapsedMs } | Sort-Object)
    $middle = [int][Math]::Floor($values.Count / 2)
    $median = if ($values.Count % 2) { $values[$middle] } else { ($values[$middle - 1] + $values[$middle]) / 2 }
    [pscustomobject]@{ variant=$group.Group[0].variant; scenario=$group.Group[0].scenario; medianMs=$median; minMs=$values[0]; maxMs=$values[-1]; p90Ms=$values[[int][Math]::Ceiling($values.Count * .90)-1]; p95Ms=$values[[int][Math]::Ceiling($values.Count * .95)-1] }
}
$gitSha = & git -C (Join-Path $PSScriptRoot '..') rev-parse HEAD
if ($LASTEXITCODE -ne 0) { throw 'Could not read Git SHA.' }
$gitStatus = & git -C (Join-Path $PSScriptRoot '..') status --short
$executables = foreach ($variant in $variants) { $path = (Resolve-Path (Join-Path $PublishRoot "$variant\CodeLines.Cli.exe")).Path; [pscustomobject]@{variant=$variant; path=$path; sha256=(Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash} }
$metadata = [ordered]@{ executables=@($executables); fixtureManifest=$manifest; fixtureStats=$manifest.sets; gitStatus=@($gitStatus); os=[Runtime.InteropServices.RuntimeInformation]::OSDescription; architecture=[Runtime.InteropServices.RuntimeInformation]::OSArchitecture.ToString(); sdk=(& dotnet --version); runtimes=(& dotnet --list-runtimes); rid='win-x64'; configuration='Release'; gitSha=$gitSha; publishOptions='FDD/SCD/AOT; ReadyToRun=false; PublishSingleFile=false'; fixtureRoot=(Resolve-Path $FixtureRoot).Path; warmups=$Warmups; iterations=$Iterations; series=$Series; order=$Order; note='Elapsed process invocation; OS cache is not controlled. Peak memory not measured.' }
[pscustomobject]@{ metadata=$metadata; runs=$records; aggregates=$aggregates } | ConvertTo-Json -Depth 8 | Set-Content -Encoding utf8 (Join-Path $OutputDirectory 'results.json')
$records | Export-Csv -NoTypeInformation -Encoding utf8 (Join-Path $OutputDirectory 'results.csv')
Write-Host "Raw results written to $OutputDirectory; fixture copies are retained for inspection."
