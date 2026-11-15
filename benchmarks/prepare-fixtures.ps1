param([Parameter(Mandatory)][string]$OutputDirectory)
$ErrorActionPreference = 'Stop'
if (Test-Path -LiteralPath $OutputDirectory) { throw 'Choose a new fixture directory; existing data will not be overwritten.' }
foreach ($spec in @(@('small',20), @('medium',500))) {
    $dir = Join-Path $OutputDirectory $spec[0]
    New-Item -ItemType Directory -Path $dir -Force | Out-Null
    Set-Content -LiteralPath (Join-Path $dir '.clineIgnore') -Value @('bin/', 'obj/')
    for ($i = 0; $i -lt $spec[1]; $i++) {
        $file = Join-Path $dir ("File{0:D5}.cs" -f $i)
        $lines = @("// fixture $i", "namespace BenchmarkFixture;", "public class Item$i", '{')
        for ($j = 0; $j -lt 20; $j++) { $lines += "    public int Value$j => $j;" }
        $lines += '}'
        [IO.File]::WriteAllLines($file, $lines, [Text.UTF8Encoding]::new($false))
    }
}
Write-Host "Fixtures created: $OutputDirectory"
