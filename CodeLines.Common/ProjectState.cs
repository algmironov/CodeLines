using System.IO.Compression;
using System.Text;
using System.Text.Json;

using CodeLines.Common.Models;

namespace CodeLines.Common;
public class ProjectState
{
    private readonly string StateDirectory = ".cline";
    private readonly string HistoryFile = "history.json";

    /// <summary>
    /// Save result
    /// </summary>
    public async Task SaveResultsAsync(DirectoryInfo projectPath, AnalysisResult result)
    {
        try
        {
            var stateDir = Path.Combine(projectPath.FullName, StateDirectory);
            if (!Directory.Exists(stateDir))
            {
                Directory.CreateDirectory(stateDir);
            }

            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var resultFilePath = Path.Combine(stateDir, $"scan_{timestamp}.cline");

            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };

            var json = JsonSerializer.Serialize(result, options);

            await CompressAndSaveAsync(json, resultFilePath);

            await UpdateHistoryAsync(projectPath, result);

            Console.WriteLine($"Results saved to {resultFilePath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error while saving results: {ex.Message}");
        }
    }


    /// <summary>
    /// Load previous result
    /// </summary>
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

            if (resultFiles.Length == 0)
            {
                return null; 
            }

            string previousResultFile = resultFiles[0];
            string json = await LoadAndDecompressAsync(previousResultFile);

            return JsonSerializer.Deserialize<AnalysisResult>(json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading previous results: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Load scan history
    /// </summary>
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

    /// <summary>
    /// Compress and save file
    /// </summary>
    private static async Task CompressAndSaveAsync(string content, string filePath)
    {
        byte[] contentBytes = Encoding.UTF8.GetBytes(content);

        using var fs = new FileStream(filePath, FileMode.Create);
        using var gzs = new GZipStream(fs, CompressionLevel.Optimal);
        await gzs.WriteAsync(contentBytes);
    }

    /// <summary>
    /// Load and decompress file
    /// </summary>
    private static async Task<string> LoadAndDecompressAsync(string filePath)
    {
        using var fs = new FileStream(filePath, FileMode.Open);
        using var gzs = new GZipStream(fs, CompressionMode.Decompress);
        using var ms = new MemoryStream();
        await gzs.CopyToAsync(ms);
        return Encoding.UTF8.GetString(ms.ToArray());
    }

    /// <summary>
    /// Updates scan history
    /// </summary>
    /// <param name="projectPath"></param>
    /// <param name="result"></param>
    /// <returns></returns>
    private async Task UpdateHistoryAsync(DirectoryInfo projectPath, AnalysisResult result)
    {
        try
        {
            var stateDir = Path.Combine(projectPath.FullName, StateDirectory);
            var historyFilePath = Path.Combine(stateDir, HistoryFile);

            List<HistoryEntry> history = [];

            if (File.Exists(historyFilePath))
            {
                var json = await File.ReadAllTextAsync(historyFilePath);
                history = JsonSerializer.Deserialize<List<HistoryEntry>>(json) ?? [];
            }

            history.Add(new HistoryEntry
            {
                Date = result.Date,
                LineCount = result.LineCount,
                FileCount = result.FileCount
            });

            history = [.. history.OrderBy(h => h.Date)];

            var updatedJson = JsonSerializer.Serialize(history, new JsonSerializerOptions
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
}