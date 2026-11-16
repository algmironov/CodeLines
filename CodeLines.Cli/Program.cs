using System.CommandLine;
using CodeLines.Cli.Commands;

namespace CodeLines.Cli;

public class Program
{
    static async Task<int> Main(string[] args)
    {
        var root = new RootCommand("Calculate your .Net project code lines");
        static Option<DirectoryInfo> PathOption() => new("--path", "-p")
        {
            Description = "Path to project directory",
            DefaultValueFactory = _ => new DirectoryInfo(Directory.GetCurrentDirectory())
        };
        var initPath = PathOption();
        var force = new Option<bool>("--force", "-f") { Description = "Create new .clineIgnore file" };
        var init = new Command("init", "Init project directory") { initPath, force };
        init.SetAction(async result => await new InitCommand().Execute(result.GetValue(initPath)!, result.GetValue(force)));

        var scanPath = PathOption();
        var diff = new Option<bool>("--diff", "-d") { Description = "Show difference with previous scan results" };
        var verbose = new Option<bool>("--verbose", "-v") { Description = "Detailed output" };
        var scan = new Command("scan", "Scan and calculate lines of code count") { scanPath, diff, verbose };
        scan.SetAction(async result => await new ScanCommand().Execute(result.GetValue(scanPath)!, result.GetValue(diff), result.GetValue(verbose)));

        var historyPath = PathOption();
        var last = new Option<int?>("--last", "-l") { Description = "History entries to show" };
        var history = new Command("history", "Show scan history") { historyPath, last };
        history.SetAction(async result => await new HistoryCommand().Execute(result.GetValue(historyPath)!, result.GetValue(last)));

        var ignore = new Command("ignore", "Manage ignored files");
        var addPath = PathOption();
        var addPattern = new Argument<string>("pattern") { Description = "Directory or file pattern" };
        var add = new Command("add", "Add ignore pattern to .clineIgnore") { addPattern, addPath };
        add.SetAction(async result => await new IgnoreCommand().Add(result.GetValue(addPattern)!, result.GetValue(addPath)!));
        var removePath = PathOption();
        var removePattern = new Argument<string>("pattern") { Description = "Directory or file pattern" };
        var remove = new Command("remove", "Remove pattern from .clineIgnore") { removePattern, removePath };
        remove.SetAction(async result => await new IgnoreCommand().Remove(result.GetValue(removePattern)!, result.GetValue(removePath)!));
        var listPath = PathOption();
        var list = new Command("list", "Show ignore patterns") { listPath };
        list.SetAction(async result => await new IgnoreCommand().List(result.GetValue(listPath)!));
        ignore.Add(add); ignore.Add(remove); ignore.Add(list);
        root.Add(init); root.Add(scan); root.Add(history); root.Add(ignore);
        try { return await root.Parse(args).InvokeAsync(new InvocationConfiguration { EnableDefaultExceptionHandler = false }); }
        catch (Exception ex) when (ex is IOException or InvalidDataException or UnauthorizedAccessException or System.Text.Json.JsonException or ArgumentException)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
            return 1;
        }
    }
}
