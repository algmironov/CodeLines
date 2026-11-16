param([Parameter(Mandatory)][string]$PublishRoot)
$ErrorActionPreference = 'Stop'
$base = Join-Path ([IO.Path]::GetTempPath()) ('cline-smoke-' + [guid]::NewGuid())
New-Item -ItemType Directory $base | Out-Null
try {
    $outputs = @()
    foreach ($variant in @('JIT-FDD','JIT-SCD','NativeAOT')) {
        $exe = Join-Path $PublishRoot "$variant\CodeLines.Cli.exe"
        if (!(Test-Path $exe)) { throw "Missing $exe" }
        $fixture = Join-Path $base $variant
        New-Item -ItemType Directory $fixture | Out-Null
        Set-Content -LiteralPath (Join-Path $fixture '.clineIgnore') -Value 'bin/'
        Set-Content -LiteralPath (Join-Path $fixture 'a.cs') -Value @('class A {', ' // comment', ' string s = "//";', '}')
        $help = & $exe --help
        if ($LASTEXITCODE -ne 0 -or "$help" -notmatch 'scan') { throw "$variant help failed" }
        $first = & $exe scan --path $fixture
        if ($LASTEXITCODE -ne 0) { throw "$variant first scan failed" }
        $second = & $exe scan -p $fixture -v
        if ($LASTEXITCODE -ne 0 -or "$second" -notmatch 'Skipped: 1 unchanged files') { throw "$variant cache failed" }
        $summary = @($first | Where-Object { $_ -match '^(Files total|Lines of code total)' }) -join "`n"
        if (!$summary) { throw "$variant summary missing" }
        $outputs += $summary
        & $exe scan -p (Join-Path $fixture 'missing') 2>$null | Out-Null
        if ($LASTEXITCODE -eq 0) { throw "$variant accepted a missing directory" }
    }
    if (($outputs | Select-Object -Unique).Count -ne 1) { throw 'Results differ between publish variants' }
    Write-Host 'Smoke tests passed for all variants.'
} finally {
    $resolved = [IO.Path]::GetFullPath($base)
    $tempRoot = [IO.Path]::GetFullPath([IO.Path]::GetTempPath())
    if (!$resolved.StartsWith($tempRoot, [StringComparison]::OrdinalIgnoreCase)) { throw 'Unsafe temporary path.' }
    Remove-Item -LiteralPath $resolved -Recurse -Force
}
