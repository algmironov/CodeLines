param(
    [Parameter(Mandatory)][string]$OutputDirectory,
    [int]$Seed = 12345,
    [int]$AverageFileBytes = 1024
)
$ErrorActionPreference = 'Stop'
if ($AverageFileBytes -lt 256) { throw 'AverageFileBytes must be at least 256.' }
if (Test-Path -LiteralPath $OutputDirectory) { throw 'Choose a new fixture directory; existing data will not be overwritten.' }
$random = [Random]::new($Seed)
$manifest = [ordered]@{ seed=$Seed; requestedAverageFileBytes=$AverageFileBytes; sets=@() }
foreach ($spec in @(@('small',20), @('medium',500), @('large',5000))) {
    $dir = Join-Path $OutputDirectory $spec[0]
    New-Item -ItemType Directory -Path $dir -Force | Out-Null
    [IO.File]::WriteAllText((Join-Path $dir '.clineIgnore'), "bin/`nobj/`n", [Text.UTF8Encoding]::new($false))
    $total = 0L
    for ($i = 0; $i -lt $spec[1]; $i++) {
        $file = Join-Path $dir ("File{0:D5}.cs" -f $i)
        $lines = @("// fixture $i", "namespace BenchmarkFixture;", "public class Item$i", '{')
        for ($j = 0; $j -lt 20; $j++) { $lines += "    public int Value$j => $j;" }
        $lines += '}'
        $content = ($lines -join "`n") + "`n"
        $padding = [Math]::Max(0, $AverageFileBytes - [Text.Encoding]::UTF8.GetByteCount($content) - 4)
        $content += '// ' + ([char]([int][char]'a' + $random.Next(26)) | ForEach-Object { $_.ToString() * $padding }) + "`n"
        [IO.File]::WriteAllText($file, $content, [Text.UTF8Encoding]::new($false))
        $total += (Get-Item -LiteralPath $file).Length
    }
    $manifest.sets += [ordered]@{ name=$spec[0]; files=$spec[1]; sourceBytes=$total; expectedLines=24*$spec[1] }
}
$manifest | ConvertTo-Json -Depth 4 | Set-Content -Encoding utf8 (Join-Path $OutputDirectory 'manifest.json')
Write-Host "Fixtures created: $OutputDirectory"
