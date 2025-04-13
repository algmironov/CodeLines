using CodeLines.Common;

namespace CodeLines.Cli.Commands;
public class HistoryCommand
{
    public async Task Execute(DirectoryInfo path, int? last)
    {
        var projectState = new ProjectState();
        var history = await projectState.LoadHistoryAsync(path, last);

        if (history.Count == 0)
        {
            Console.WriteLine("History is empty.");
            return;
        }

        Console.WriteLine("Scan history:");
        foreach (var entry in history)
        {
            Console.WriteLine($"{entry.Date}: {entry.LineCount} lines ({entry.FileCount} files)");
        }
    }
}
