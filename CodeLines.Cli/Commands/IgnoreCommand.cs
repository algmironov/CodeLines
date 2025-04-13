using CodeLines.Common;

namespace CodeLines.Cli.Commands;

public class IgnoreCommand
{
    public async Task Add(string pattern, DirectoryInfo path)
    {
        var ignoreManager = new IgnoreManager();
        await ignoreManager.LoadIgnoreRulesAsync(path);
        await ignoreManager.AddIgnoreRuleAsync(path, pattern);
        Console.WriteLine($"Added pattern '{pattern}' to .clineIgnore");
    }

    public async Task Remove(string pattern, DirectoryInfo path)
    {
        var ignoreManager = new IgnoreManager();
        await ignoreManager.LoadIgnoreRulesAsync(path);
        await ignoreManager.RemoveIgnoreRuleAsync(path, pattern);
        Console.WriteLine($"Removed '{pattern}' from .clineIgnore");
    }

    public async Task List(DirectoryInfo path)
    {
        var ignoreManager = new IgnoreManager();
        await ignoreManager.LoadIgnoreRulesAsync(path);

        var rules = ignoreManager.GetIgnoreRules();
        foreach (var rule in rules)
        {
            Console.WriteLine(rule);
        }
    }
}
