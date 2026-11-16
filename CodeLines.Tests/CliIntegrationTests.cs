using System.Diagnostics;
using Xunit;

namespace CodeLines.Tests;

public sealed class CliIntegrationTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "cline cli " + Guid.NewGuid());
    public CliIntegrationTests() => Directory.CreateDirectory(root);
    public void Dispose() => Directory.Delete(root, true);

    private async Task<(int Code, string Output)> Run(params string[] args)
    {
        var assembly = typeof(CliIntegrationTests).Assembly.Location;
        var testBase = Path.Combine(Path.GetDirectoryName(assembly)!, Path.GetFileNameWithoutExtension(assembly));
        var start = new ProcessStartInfo("dotnet") { WorkingDirectory = root, RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false };
        foreach (var value in new[] { "exec", "--runtimeconfig", testBase + ".runtimeconfig.json", "--depsfile", testBase + ".deps.json", typeof(CodeLines.Cli.Program).Assembly.Location }.Concat(args)) start.ArgumentList.Add(value);
        using var process = Process.Start(start) ?? throw new InvalidOperationException("Unable to start CLI");
        var stdout = process.StandardOutput.ReadToEndAsync(); var stderr = process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();
        return (process.ExitCode, (await stdout) + (await stderr));
    }

    [Fact]
    public async Task CliHandlesPathsCacheIgnoreHistoryAndErrors()
    {
        Directory.CreateDirectory(Path.Combine(root, "project folder"));
        var project = Path.Combine(root, "project folder");
        await File.WriteAllTextAsync(Path.Combine(project, "a.cs"), "class A {}\n");
        var init = await Run("init", "-p", "project folder"); Assert.Equal(0, init.Code);
        var first = await Run("scan", "--path", project); Assert.Equal(0, first.Code); Assert.Contains("Files total: 1", first.Output);
        var second = await Run("scan", "-p", "project folder", "-v"); Assert.Equal(0, second.Code); Assert.Contains("Skipped: 1 unchanged files", second.Output);
        await File.WriteAllTextAsync(Path.Combine(project, "b.cs"), "class B {}\n");
        var added = await Run("scan", "-p", project); Assert.Equal(0, added.Code); Assert.Contains("Files total: 2", added.Output);
        var ignore = await Run("ignore", "add", "b.cs", "-p", project); Assert.Equal(0, ignore.Code);
        var list = await Run("ignore", "list", "-p", project); Assert.Equal(0, list.Code); Assert.Contains("b.cs", list.Output);
        var excluded = await Run("scan", "-p", project); Assert.Equal(0, excluded.Code); Assert.Contains("Files total: 1", excluded.Output);
        var remove = await Run("ignore", "remove", "b.cs", "-p", project); Assert.Equal(0, remove.Code);
        var restored = await Run("scan", "-p", project); Assert.Equal(0, restored.Code); Assert.Contains("Files total: 2", restored.Output);
        var reignore = await Run("ignore", "add", "b.cs", "-p", project); Assert.Equal(0, reignore.Code);
        File.Delete(Path.Combine(project, "a.cs"));
        var deleted = await Run("scan", "-p", project); Assert.Equal(0, deleted.Code); Assert.Contains("Files total: 0", deleted.Output);
        var history = await Run("history", "-p", project, "-l", "1"); Assert.Equal(0, history.Code); Assert.Contains("Scan history", history.Output);
        await File.WriteAllTextAsync(Path.Combine(project, ".cline", "history.json"), "broken");
        var broken = await Run("scan", "-p", project); Assert.NotEqual(0, broken.Code);
        Assert.Equal("broken", await File.ReadAllTextAsync(Path.Combine(project, ".cline", "history.json")));
        var missing = await Run("scan", "-p", Path.Combine(root, "missing")); Assert.NotEqual(0, missing.Code);
    }
}
