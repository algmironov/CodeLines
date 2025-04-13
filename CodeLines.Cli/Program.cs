using System.CommandLine;

using CodeLines.Cli.Commands;

namespace CodeLines.Cli
{
    public class Program
    {
        static async Task<int> Main(string[] args)
        {
            var rootCommand = new RootCommand("Calculate your .Net project code lines");

            var initCommand = new Command("init", "Init project direcctory");

            var initPathOption = new Option<DirectoryInfo>(
                "--path",
                getDefaultValue: () => new DirectoryInfo(Directory.GetCurrentDirectory()),
                description: "Path to project directory");
            initPathOption.AddAlias("-p");

            var forceOption = new Option<bool>(
                "--force",
                "Create new .clineIgnore file");
            forceOption.AddAlias("-f");

            initCommand.AddOption(initPathOption);
            initCommand.AddOption(forceOption);
            initCommand.SetHandler(async (path, force) =>
            {
                Console.WriteLine($"Directory initialization: {path.FullName}");
                await new InitCommand().Execute(path, force);
            }, initPathOption, forceOption);

            // scan command
            var scanCommand = new Command("scan", "Scan and calculate lines of code count");
            var scanPathOption = new Option<DirectoryInfo>(
                "--path",
                getDefaultValue: () => new DirectoryInfo(Directory.GetCurrentDirectory()),
                description: "Path to project directory");
            scanPathOption.AddAlias("-p");

            var diffOption = new Option<bool>(
                "--diff",
                "Show difference with previous scan results");
            diffOption.AddAlias("-d");

            var verboseOption = new Option<bool>(
                "--verbose",
                "Detailed output");
            verboseOption.AddAlias("-v");

            scanCommand.AddOption(scanPathOption);
            scanCommand.AddOption(diffOption);
            scanCommand.AddOption(verboseOption);
            scanCommand.SetHandler(async (path, diff, verbose) =>
            {
                Console.WriteLine($"Processing directory scan: {path.FullName}");
                await new ScanCommand().Execute(path, diff, verbose);
            }, scanPathOption, diffOption, verboseOption);

            // history command
            var historyCommand = new Command("history", "Show scan history");
            var historyPathOption = new Option<DirectoryInfo>(
                "--path",
                getDefaultValue: () => new DirectoryInfo(Directory.GetCurrentDirectory()),
                description: "Path to project directory");
            historyPathOption.AddAlias("-p");

            var lastOption = new Option<int?>(
                "--last",
                "History entries to show");
            lastOption.AddAlias("-l");

            historyCommand.AddOption(historyPathOption);
            historyCommand.AddOption(lastOption);
            historyCommand.SetHandler(async (path, last) =>
            {
                Console.WriteLine($"Scan history in: {path.FullName}");
                await new HistoryCommand().Execute(path, last);
            }, historyPathOption, lastOption);

            // ignore command
            var ignoreCommand = new Command("ignore", "Manage ignored files");
            var ignoreAddCommand = new Command("add", "Add ignore pattern to .clineIgnore");
            var ignoreRemoveCommand = new Command("remove", "Remove pattern from .clineIgnore");
            var ignoreListCommand = new Command("list", "Show ignore patterns");

            var patternArgument = new Argument<string>("pattern", "Directory or file pattern");
            var ignorePathOption = new Option<DirectoryInfo>(
                "--path",
                getDefaultValue: () => new DirectoryInfo(Directory.GetCurrentDirectory()),
                description: "Path to project directory");
            ignorePathOption.AddAlias("-p");

            ignoreAddCommand.AddArgument(patternArgument);
            ignoreAddCommand.AddOption(ignorePathOption);
            ignoreAddCommand.SetHandler(async (pattern, path) =>
            {
                Console.WriteLine($"Adding pattern '{pattern}' to .clineIgnore");
                await new IgnoreCommand().Add(pattern, path);
            }, patternArgument, ignorePathOption);

            ignoreRemoveCommand.AddArgument(patternArgument);
            ignoreRemoveCommand.AddOption(ignorePathOption);
            ignoreRemoveCommand.SetHandler(async (pattern, path) =>
            {
                Console.WriteLine($"Removing pattern '{pattern}' from .clineIgnore");
                await new IgnoreCommand().Remove(pattern, path);
            }, patternArgument, ignorePathOption);

            ignoreListCommand.AddOption(ignorePathOption);
            ignoreListCommand.SetHandler(async (path) =>
            {
                Console.WriteLine($"List of ignored patterns:");
                await new IgnoreCommand().List(path);
            }, ignorePathOption);

            ignoreCommand.AddCommand(ignoreAddCommand);
            ignoreCommand.AddCommand(ignoreRemoveCommand);
            ignoreCommand.AddCommand(ignoreListCommand);

            rootCommand.AddCommand(initCommand);
            rootCommand.AddCommand(scanCommand);
            rootCommand.AddCommand(historyCommand);
            rootCommand.AddCommand(ignoreCommand);

            return await rootCommand.InvokeAsync(args);
        }
    }
}
