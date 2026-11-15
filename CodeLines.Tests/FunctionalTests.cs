using CodeLines.Common;
using CodeLines.Common.Models;
using Xunit;

namespace CodeLines.Tests;

public sealed class FunctionalTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "cline-tests-" + Guid.NewGuid());
    public FunctionalTests() => Directory.CreateDirectory(root);
    public void Dispose() => Directory.Delete(root, true);
    private DirectoryInfo DirectoryInfo => new(root);

    [Fact]
    public async Task CacheUsesRootAndPersistsWithoutVerbose()
    {
        var path = Path.Combine(root, "a.cs");
        await File.WriteAllTextAsync(path, "var url = \"//\"; /* a */ int x;\r\n/* block\n comment */ int y;\n// comment\n");
        var analyzer = new CodeAnalyzer();
        var first = await analyzer.AnalyzeAsync([new FileInfo(path)], false, null, root);
        Assert.Equal(2, first.LineCount);
        Assert.Equal("a.cs", Assert.Single(first.FileStats).FilePath);
        var second = await analyzer.AnalyzeAsync([new FileInfo(path)], false, first, root);
        Assert.Equal(first.LineCount, second.LineCount);
        Assert.Single(second.FileStats);
        await File.WriteAllTextAsync(path, "class C {}\n");
        Assert.Equal(1, (await analyzer.AnalyzeAsync([new FileInfo(path)], false, second, root)).LineCount);
    }

    [Theory]
    [InlineData("bin/", "sub/bin/a.cs", true)]
    [InlineData("**/*.Designer.cs", "a.Designer.cs", true)]
    [InlineData("**/Generated/*.cs", "sub/Generated/a.cs", true)]
    [InlineData("*.min.js", "a.min.js", true)]
    [InlineData("*.min.js", "a.js", false)]
    public async Task GlobPatterns(string pattern, string path, bool expected)
    {
        await File.WriteAllTextAsync(Path.Combine(root, ".clineIgnore"), pattern);
        var manager = new IgnoreManager();
        await manager.LoadIgnoreRulesAsync(DirectoryInfo);
        Assert.Equal(expected, manager.ShouldIgnore(Path.Combine(root, path.Replace('/', Path.DirectorySeparatorChar))));
    }

    [Fact]
    public async Task HistoryAndOldResultRemainReadableAndCorruptionIsNotOverwritten()
    {
        var state = new ProjectState();
        var dir = Path.Combine(root, ".cline"); Directory.CreateDirectory(dir);
        var history = Path.Combine(dir, "history.json");
        await File.WriteAllTextAsync(history, "[{\"Date\":\"2025-01-01T00:00:00\",\"FileCount\":1,\"LineCount\":2}]");
        Assert.Equal(2, Assert.Single(await state.LoadHistoryAsync(DirectoryInfo, null)).LineCount);
        var old = Path.Combine(dir, "scan_20250101_000000.cline");
        await using (var stream = File.Create(old))
        await using (var gzip = new System.IO.Compression.GZipStream(stream, System.IO.Compression.CompressionMode.Compress))
        await using (var writer = new StreamWriter(gzip))
            await writer.WriteAsync("{\"FileCount\":1,\"LineCount\":2}");
        Assert.Equal(2, (await state.LoadPreviousResultAsync(DirectoryInfo))!.LineCount);
        await File.WriteAllTextAsync(old, "{\"FileCount\":1,\"LineCount\":3}");
        Assert.Equal(3, (await state.LoadPreviousResultAsync(DirectoryInfo))!.LineCount);
        await File.WriteAllTextAsync(history, "broken");
        await Assert.ThrowsAsync<System.Text.Json.JsonException>(() => state.SaveResultsAsync(DirectoryInfo, new AnalysisResult()));
        Assert.Equal("broken", await File.ReadAllTextAsync(history));
    }
}
