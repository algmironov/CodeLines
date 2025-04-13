using CodeLines.Common;

namespace CodeLines.Cli.Commands;
public class InitCommand
{
    public async Task Execute(DirectoryInfo path, bool force)
    {
        string ignoreFilePath = Path.Combine(path.FullName, ".clineIgnore");

        if (File.Exists(ignoreFilePath) && !force)
        {
            Console.WriteLine(".clineIgnore already exists. Use --force to create new .clineIgnore");
            return;
        }

        var ignoreManager = new IgnoreManager();
        await ignoreManager.InitializeIgnoreFile(path);

        var scanner = new FileScanner(ignoreManager);
        var projectFiles = await scanner.ScanProjectAsync(path);

        Console.WriteLine($"Initialization finished. {projectFiles.Count} files found for analysis.");
    }
}
