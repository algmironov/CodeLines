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

        Console.WriteLine($"{projectFiles.Count} files found for analyze.");

        var projectState = new ProjectState();
        var previousResult = await projectState.LoadPreviousResultAsync(path);

        var analyzer = new CodeAnalyzer();
        var result = await analyzer.AnalyzeAsync(projectFiles, verbose, previousResult);

        if (verbose)
        {
            Console.WriteLine("\nDetailed files info:");

            foreach (var fileStats in result.FileStats)
            {
                Console.WriteLine($"{fileStats.FilePath}: {fileStats.LineCount} lines");
            }
        }

        Console.WriteLine();
        Console.WriteLine($"Results:");
        Console.WriteLine($"Files total: {result.FileCount}");
        Console.WriteLine($"Lines of code total (empty lines excluded): {result.LineCount}");

        await projectState.SaveResultsAsync(path, result);

        if (diff)
        {
            if (previousResult != null)
            {
                var lineDiff = result.LineCount - previousResult.LineCount;
                var fileDiff = result.FileCount - previousResult.FileCount;

                var lineDiffSign = lineDiff >= 0 ? "+" : "";
                var fileDiffSign = fileDiff >= 0 ? "+" : "";

                Console.WriteLine($"Previous scan difference: ");
                Console.WriteLine($"Lines count change: {lineDiffSign}{lineDiff}");
                Console.WriteLine($"Files count change: {fileDiffSign}{fileDiff}");
                Console.WriteLine($"Previous scan: {previousResult.Date}");
            }
            else
            {
                Console.WriteLine("There is no scan history for this project.");
            }
        }
    }
}
