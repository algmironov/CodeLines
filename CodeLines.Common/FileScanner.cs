namespace CodeLines.Common;
public class FileScanner(IgnoreManager ignoreManager)
{
    private readonly IgnoreManager _ignoreManager = ignoreManager;

    public async Task<List<FileInfo>> ScanProjectAsync(DirectoryInfo projectPath)
    {
        var result = new List<FileInfo>();

        try
        {
            if (!projectPath.Exists)
            {
                throw new DirectoryNotFoundException($"Directory not found: {projectPath.FullName}");
            }

            await ScanDirectoryAsync(projectPath, result);

            return result;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception during directory scan: {ex.Message}");
            return [];
        }
    }

    private async Task ScanDirectoryAsync(DirectoryInfo directory, List<FileInfo> result)
    {
        if (_ignoreManager.ShouldIgnore(directory.FullName))
        {
            return;
        }

        foreach (var file in directory.GetFiles())
        {
            if (_ignoreManager.ShouldIgnore(file.FullName))
            {
                continue;
            }

            if (IsCodeFile(file))
            {
                result.Add(file);
            }
        }

        foreach (var subdirectory in directory.GetDirectories())
        {
            await ScanDirectoryAsync(subdirectory, result);
        }
    }

    private static bool IsCodeFile(FileInfo file)
    {
        var codeExtensions = new[]
        {
            ".cs", ".vb", ".fs", ".js", ".ts", ".jsx", ".tsx", ".html", ".cshtml",
            ".razor", ".css", ".scss", ".less", ".xml", ".json", ".xaml", ".config",
            ".csproj", ".vbproj", ".fsproj", ".sln", ".props", ".targets"
        };

        return codeExtensions.Contains(file.Extension.ToLower());
    }
}

