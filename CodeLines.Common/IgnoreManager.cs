using System.Text.RegularExpressions;

namespace CodeLines.Common;

public class IgnoreManager
{
    private List<string> _ignorePatterns = [];
    private string _root = Directory.GetCurrentDirectory();
    private readonly string IgnoreFileName = ".clineIgnore";

    public async Task InitializeIgnoreFile(DirectoryInfo projectPath)
    {
        _root = projectPath.FullName;
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
        _root = projectPath.FullName;
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
        var relative = Path.GetRelativePath(_root, path).Replace('\\', '/');
        if (relative == ".")
            return false;
        foreach (var raw in _ignorePatterns)
        {
            var pattern = raw.Trim().Replace('\\', '/').TrimStart('/');
            if (pattern.Length == 0 || pattern.StartsWith('#') || pattern.StartsWith('!'))
                continue;
            bool directory = pattern.EndsWith('/');
            pattern = pattern.TrimEnd('/');
            var regex = new System.Text.StringBuilder();
            for (int i = 0; i < pattern.Length; i++)
            {
                char c = pattern[i];
                if (c == '*' && i + 1 < pattern.Length && pattern[i + 1] == '*')
                {
                    i++;
                    if (i + 1 < pattern.Length && pattern[i + 1] == '/')
                    {
                        i++;
                        regex.Append("(?:.*/)?");
                    }
                    else
                        regex.Append(".*");
                }
                else if (c == '*')
                    regex.Append("[^/]*");
                else if (c == '?')
                    regex.Append("[^/]");
                else
                    regex.Append(Regex.Escape(c.ToString()));
            }
            // Legacy rules with a leading slash matched directory names at any depth.
            var prefix = pattern.Contains('/') ? "^" : "(?:^|.*/)";
            var suffix = directory ? "(?:/.*)?$" : "$";
            var options = OperatingSystem.IsWindows() ? RegexOptions.IgnoreCase | RegexOptions.CultureInvariant : RegexOptions.CultureInvariant;
            if (Regex.IsMatch(relative, prefix + regex + suffix, options))
                return true;
        }
        return false;
    }
}
