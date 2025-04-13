namespace CodeLines.Common;

public class IgnoreManager
{
    private List<string> _ignorePatterns = [];
    private readonly string IgnoreFileName = ".clineIgnore";

    public async Task InitializeIgnoreFile(DirectoryInfo projectPath)
    {
        string ignoreFilePath = Path.Combine(projectPath.FullName, IgnoreFileName);

        var defaultPatterns = new List<string>
            {
                // folders
                "/bin/",
                "/obj/",
                "/node_modules/",
                "/.vs/",
                "/.vscode/",
                "/.git/",
                "/packages/",
                "/TestResults/",
                "/Migrations/",
                "/wwwroot/lib/",
                "/bower_components/",
                
                // files
                "*.min.js",
                "*.min.css",
                "*.designer.cs",
                "*.generated.cs",
                "*.g.cs",
                "*.g.i.cs",
                "*.AssemblyInfo.cs",
                "*.csproj.user",
                "package-lock.json",
                "yarn.lock",
                "*.dll",
                "*.exe",
                "*.pdb",
                "*.cache",
                "*.tmp",
                "*.log",
                "*.cline"
            };

        string gitIgnorePath = Path.Combine(projectPath.FullName, ".gitignore");

        if (File.Exists(gitIgnorePath))
        {
            var gitIgnorePatterns = await File.ReadAllLinesAsync(gitIgnorePath);
            defaultPatterns.AddRange(gitIgnorePatterns.Where(p => !string.IsNullOrWhiteSpace(p) && !p.StartsWith('#')));
        }

        await File.WriteAllLinesAsync(ignoreFilePath, defaultPatterns.Distinct());
        _ignorePatterns = [.. defaultPatterns.Distinct()];

        Console.WriteLine($"{IgnoreFileName} has ben created with default ignore rules.");
    }

    public async Task LoadIgnoreRulesAsync(DirectoryInfo projectPath)
    {
        string ignoreFilePath = Path.Combine(projectPath.FullName, IgnoreFileName);

        if (!File.Exists(ignoreFilePath))
        {
            throw new FileNotFoundException($"{IgnoreFileName} not found. Execute 'init' command.");
        }

        var patterns = await File.ReadAllLinesAsync(ignoreFilePath);
        _ignorePatterns = [.. patterns.Where(p => !string.IsNullOrWhiteSpace(p) && !p.StartsWith('#'))];
    }

    public async Task AddIgnoreRuleAsync(DirectoryInfo projectPath, string pattern)
    {
        if (!_ignorePatterns.Contains(pattern))
        {
            _ignorePatterns.Add(pattern);
            await SaveIgnoreRulesAsync(projectPath);
        }
    }

    public async Task RemoveIgnoreRuleAsync(DirectoryInfo projectPath, string pattern)
    {
        if (_ignorePatterns.Remove(pattern))
        {
            await SaveIgnoreRulesAsync(projectPath);
        }
    }

    private async Task SaveIgnoreRulesAsync(DirectoryInfo projectPath)
    {
        string ignoreFilePath = Path.Combine(projectPath.FullName, IgnoreFileName);
        await File.WriteAllLinesAsync(ignoreFilePath, _ignorePatterns);
    }

    public List<string> GetIgnoreRules()
    {
        return [.. _ignorePatterns];
    }

    public bool ShouldIgnore(string path)
    {
        string normalizedPath = path.Replace('\\', '/');

        foreach (var pattern in _ignorePatterns)
        {
            if (pattern.StartsWith('/') && pattern.EndsWith('/'))
            {
                string dirPattern = pattern.Trim('/');
                if (normalizedPath.Contains($"/{dirPattern}/") ||
                    normalizedPath.EndsWith($"/{dirPattern}"))
                {
                    return true;
                }
            }
            else if (pattern.StartsWith("*."))
            {
                string extension = pattern.Substring(1);
                if (normalizedPath.EndsWith(extension))
                {
                    return true;
                }
            }
            else if (normalizedPath.EndsWith(pattern) ||
                     normalizedPath.Contains($"/{pattern}") ||
                     normalizedPath == pattern)
            {
                return true;
            }
        }

        return false;
    }
}

