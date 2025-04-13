using CodeLines.Common;

namespace CodeLines.Cli.Commands;

public class ScanCommand
{
    public async Task Execute(DirectoryInfo path, bool diff, bool verbose)
    {
        string ignoreFilePath = Path.Combine(path.FullName, ".clineIgnore");
        if (!File.Exists(ignoreFilePath))
        {
            Console.WriteLine(".clineIgnore not found. Execute 'init' command before scan.");
            return;
        }

        var ignoreManager = new IgnoreManager();
        await ignoreManager.LoadIgnoreRulesAsync(path);

        var scanner = new FileScanner(ignoreManager);
        var projectFiles = await scanner.ScanProjectAsync(path);

        var analyzer = new CodeAnalyzer();
        var result = await analyzer.AnalyzeAsync(projectFiles, verbose);

        Console.WriteLine($"Files total: {result.FileCount}");
        Console.WriteLine($"Lines of code total (empty lines excluded): {result.LineCount}");

        if (verbose)
        {
            foreach (var fileStats in result.FileStats)
            {
                Console.WriteLine($"{fileStats.FilePath}: {fileStats.LineCount} lines");
            }
        }

        var projectState = new ProjectState();
        await projectState.SaveResultsAsync(path, result);

        if (diff)
        {
            var previousResult = await projectState.LoadPreviousResultAsync(path);
            if (previousResult != null)
            {
                int lineDiff = result.LineCount - previousResult.LineCount;
                string diffDirection = lineDiff >= 0 ? "+" : "";
                Console.WriteLine($"Previous scan difference: {diffDirection}{lineDiff} lines");
            }
            else
            {
                Console.WriteLine("There is no scan history for this project.");
            }
        }
    }
}
