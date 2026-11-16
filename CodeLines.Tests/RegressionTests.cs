using CodeLines.Common;
using CodeLines.Common.Models;
using Xunit;

namespace CodeLines.Tests;

public sealed class RegressionTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "cline tests " + Guid.NewGuid());
    public RegressionTests() => Directory.CreateDirectory(root);
    public void Dispose() => Directory.Delete(root, true);

    [Theory]
    [InlineData("", 0)]
    [InlineData(" \t\r\n \n", 0)]
    [InlineData("// comment\n/* block */", 0)]
    [InlineData("/* a\r\nb */ int x;", 1)]
    [InlineData("int a; /* a */ int b; /* b */", 1)]
    [InlineData("var x = \"/* not a comment */\"; // comment", 1)]
    [InlineData("// коммент\nvar текст = \"Привет\";", 1)]
    [InlineData("int a;\r\nint b;\nint c;", 3)]
    public async Task CountsCStyleLines(string content, int expected)
    {
        var path = Path.Combine(root, "source.cs");
        await File.WriteAllTextAsync(path, content);
        var result = await new CodeAnalyzer().AnalyzeAsync([new FileInfo(path)], false, projectRoot: root);
        Assert.Equal(expected, result.LineCount);
    }

    [Theory]
    [InlineData(".js", "// comment\nlet x = 1;", 1)]
    [InlineData(".vb", "' comment\nDim x = 1", 1)]
    [InlineData(".xml", "<!-- x -->\n<root />", 1)]
    [InlineData(".json", "  \n{}", 1)]
    public async Task CountsSupportedFileStyles(string extension, string content, int expected)
    {
        var path = Path.Combine(root, "source" + extension);
        await File.WriteAllTextAsync(path, content);
        var result = await new CodeAnalyzer().AnalyzeAsync([new FileInfo(path)], false, projectRoot: root);
        Assert.Equal(expected, result.LineCount);
    }

    [Fact]
    public async Task ScannerHonorsIgnoreRulesAndStateDirectory()
    {
        await File.WriteAllTextAsync(Path.Combine(root, ".clineIgnore"), "bin/\n**/*.Designer.cs\n*.min.js\n**/Generated/*.cs\n");
        foreach (var name in new[] { "keep.cs", "nested space/bin/a.cs", "nested space/Generated/b.cs", "nested space/form.Designer.cs", "a.min.js", ".cline/hidden.cs" })
        {
            var path = Path.Combine(root, name.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            await File.WriteAllTextAsync(path, "class C {}\n");
        }
        var ignore = new IgnoreManager(); await ignore.LoadIgnoreRulesAsync(new DirectoryInfo(root));
        var files = await new FileScanner(ignore).ScanProjectAsync(new DirectoryInfo(root));
        Assert.Equal("keep.cs", Assert.Single(files).Name);
    }

    [Fact]
    public async Task CaseSensitivityFollowsPlatform()
    {
        await File.WriteAllTextAsync(Path.Combine(root, ".clineIgnore"), "*.min.js\n");
        var ignore = new IgnoreManager(); await ignore.LoadIgnoreRulesAsync(new DirectoryInfo(root));
        Assert.Equal(OperatingSystem.IsWindows(), ignore.ShouldIgnore(Path.Combine(root, "A.MIN.JS")));
    }

    [Fact]
    public async Task CacheHandlesAddRemoveAndOldMissingStats()
    {
        var a = Path.Combine(root, "a.cs"); var b = Path.Combine(root, "b.cs");
        await File.WriteAllTextAsync(a, "class A {}\n");
        var analyzer = new CodeAnalyzer();
        var first = await analyzer.AnalyzeAsync([new FileInfo(a)], false, projectRoot: root);
        var old = new AnalysisResult { CacheVersion = 2, FileHashes = first.FileHashes, FileStats = [] };
        Assert.Equal(1, (await analyzer.AnalyzeAsync([new FileInfo(a)], true, old, root)).LineCount);
        await File.WriteAllTextAsync(b, "class B {}\nclass C {}\n");
        var added = await analyzer.AnalyzeAsync([new FileInfo(a), new FileInfo(b)], true, first, root);
        Assert.Equal(3, added.LineCount); Assert.Equal(2, added.FileStats.Count);
        var removed = await analyzer.AnalyzeAsync([new FileInfo(b)], false, added, root);
        Assert.Equal(2, removed.LineCount); Assert.Single(removed.FileStats);
    }

    [Fact]
    public async Task ResultsAreDistinctAndCorruptResultsFail()
    {
        var state = new ProjectState(); var project = new DirectoryInfo(root);
        var result = new AnalysisResult { Date = DateTime.Now, FileCount = 1, LineCount = 1 };
        await state.SaveResultsAsync(project, result); await state.SaveResultsAsync(project, result);
        Assert.Equal(2, Directory.GetFiles(Path.Combine(root, ".cline"), "*.cline").Length);
        Assert.Equal(2, (await state.LoadHistoryAsync(project, null)).Count);
        var corrupt = Path.Combine(root, ".cline", "scan_99999999_999999.cline");
        await File.WriteAllTextAsync(corrupt, "not json");
        await Assert.ThrowsAsync<System.Text.Json.JsonException>(() => state.LoadPreviousResultAsync(project));
    }

    [Fact]
    public async Task MissingProjectFails()
    {
        var scanner = new FileScanner(new IgnoreManager());
        await Assert.ThrowsAsync<DirectoryNotFoundException>(() => scanner.ScanProjectAsync(new DirectoryInfo(Path.Combine(root, "missing"))));
    }
    [Fact]
    public async Task UnreadableMissingFileDoesNotBecomeZeroLines()
    {
        var missing = new FileInfo(Path.Combine(root, "missing.cs"));
        await Assert.ThrowsAsync<FileNotFoundException>(() => new CodeAnalyzer().AnalyzeAsync([missing], false, projectRoot: root));
    }

}
