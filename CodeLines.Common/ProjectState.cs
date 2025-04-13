using System.Text.Json;

using CodeLines.Common.Models;

namespace CodeLines.Common;
public class ProjectState
{
    private readonly string StateDirectory = ".cline";
    private readonly string HistoryFile = "history.json";

    public async Task SaveResultsAsync(DirectoryInfo projectPath, AnalysisResult result)
    {
        try
        {
            string stateDir = Path.Combine(projectPath.FullName, StateDirectory);
            if (!Directory.Exists(stateDir))
            {
                Directory.CreateDirectory(stateDir);
            }

            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string resultFilePath = Path.Combine(stateDir, $"scan_{timestamp}.cline");

            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };

            string json = JsonSerializer.Serialize(result, options);

            await File.WriteAllTextAsync(resultFilePath, json);

            await UpdateHistoryAsync(projectPath, result);

            Console.WriteLine($"Results saved to {resultFilePath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error while saving results: {ex.Message}");
        }
    }

    private async Task UpdateHistoryAsync(DirectoryInfo projectPath, AnalysisResult result)
    {
        try
        {
            string stateDir = Path.Combine(projectPath.FullName, StateDirectory);
            string historyFilePath = Path.Combine(stateDir, HistoryFile);

            List<HistoryEntry> history = [];

            if (File.Exists(historyFilePath))
            {
                string json = await File.ReadAllTextAsync(historyFilePath);
                history = JsonSerializer.Deserialize<List<HistoryEntry>>(json) ?? [];
            }

            history.Add(new HistoryEntry
            {
                Date = result.Date,
                LineCount = result.LineCount,
                FileCount = result.FileCount
            });

            history = [.. history.OrderBy(h => h.Date)];

            string updatedJson = JsonSerializer.Serialize(history, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            await File.WriteAllTextAsync(historyFilePath, updatedJson);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error while updating results: {ex.Message}");
        }
    }

    public async Task<AnalysisResult?> LoadPreviousResultAsync(DirectoryInfo projectPath)
    {
        try
        {
            string stateDir = Path.Combine(projectPath.FullName, StateDirectory);
            if (!Directory.Exists(stateDir))
            {
                return null;
            }

            var resultFiles = Directory.GetFiles(stateDir, "*.cline")
                .OrderByDescending(f => f)
                .ToArray();

            if (resultFiles.Length < 2)
            {
                return null; 
            }

            string previousResultFile = resultFiles[1];
            string json = await File.ReadAllTextAsync(previousResultFile);

            return JsonSerializer.Deserialize<AnalysisResult>(json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading previous results: {ex.Message}");
            return null;
        }
    }

    public async Task<List<HistoryEntry>> LoadHistoryAsync(DirectoryInfo projectPath, int? last)
    {
        try
        {
            string stateDir = Path.Combine(projectPath.FullName, StateDirectory);
            string historyFilePath = Path.Combine(stateDir, HistoryFile);

            if (!File.Exists(historyFilePath))
            {
                return [];
            }

            string json = await File.ReadAllTextAsync(historyFilePath);
            var history = JsonSerializer.Deserialize<List<HistoryEntry>>(json) ?? [];

            if (last.HasValue && last.Value > 0 && last.Value < history.Count)
            {
                return [.. history.Skip(history.Count - last.Value)];
            }

            return history;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading history: {ex.Message}");
            return [];
        }
    }
}