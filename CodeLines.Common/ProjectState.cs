using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using CodeLines.Common.Models;

namespace CodeLines.Common;

[JsonSerializable(typeof(AnalysisResult))]
[JsonSerializable(typeof(List<HistoryEntry>))]
internal partial class StateJsonContext : JsonSerializerContext;

public class ProjectState
{
    public async Task SaveResultsAsync(DirectoryInfo projectPath, AnalysisResult result)
    {
        var dir = Path.Combine(projectPath.FullName, ".cline");
        Directory.CreateDirectory(dir);
        // Read first: a corrupt history must never be replaced with an empty one.
        var history = await LoadHistoryAsync(projectPath, null);
        history.Add(new HistoryEntry { Date = result.Date, LineCount = result.LineCount, FileCount = result.FileCount });
        var name = $"scan_{DateTime.Now:yyyyMMdd_HHmmss_fffffff}_{Guid.NewGuid():N}.cline";
        var resultPath = Path.Combine(dir, name);
        var resultTemp = resultPath + ".tmp";
        var historyPath = Path.Combine(dir, "history.json");
        var historyTemp = Path.Combine(dir, $"history.{Guid.NewGuid():N}.tmp");
        try
        {
            await using (var file = new FileStream(resultTemp, FileMode.CreateNew))
            await using (var gzip = new GZipStream(file, CompressionLevel.Optimal))
                await JsonSerializer.SerializeAsync(gzip, result, StateJsonContext.Default.AnalysisResult);
            await File.WriteAllTextAsync(historyTemp, JsonSerializer.Serialize(history, StateJsonContext.Default.ListHistoryEntry));
            File.Move(resultTemp, resultPath);
            File.Move(historyTemp, historyPath, true);
            Console.WriteLine($"Results saved to {resultPath}");
        }
        finally
        {
            if (File.Exists(resultTemp)) File.Delete(resultTemp);
            if (File.Exists(historyTemp)) File.Delete(historyTemp);
        }
    }

    public async Task<AnalysisResult?> LoadPreviousResultAsync(DirectoryInfo projectPath)
    {
        var dir = Path.Combine(projectPath.FullName, ".cline");
        if (!Directory.Exists(dir)) return null;
        var path = Directory.GetFiles(dir, "*.cline").OrderByDescending(f => f, StringComparer.Ordinal).FirstOrDefault();
        if (path is null) return null;
        await using var file = File.OpenRead(path);
        int first = file.ReadByte();
        int second = file.ReadByte();
        file.Position = 0;
        if (first == 0x1f && second == 0x8b)
        {
            await using var gzip = new GZipStream(file, CompressionMode.Decompress);
            return await JsonSerializer.DeserializeAsync(gzip, StateJsonContext.Default.AnalysisResult)
                ?? throw new InvalidDataException("Empty scan result.");
        }
        return await JsonSerializer.DeserializeAsync(file, StateJsonContext.Default.AnalysisResult)
            ?? throw new InvalidDataException("Empty scan result.");
    }

    public async Task<List<HistoryEntry>> LoadHistoryAsync(DirectoryInfo projectPath, int? last)
    {
        if (!projectPath.Exists) throw new DirectoryNotFoundException(projectPath.FullName);
        var path = Path.Combine(projectPath.FullName, ".cline", "history.json");
        if (!File.Exists(path)) return [];
        var history = JsonSerializer.Deserialize(await File.ReadAllTextAsync(path), StateJsonContext.Default.ListHistoryEntry)
            ?? throw new InvalidDataException("Empty scan history.");
        return last is > 0 && last < history.Count ? history.TakeLast(last.Value).ToList() : history;
    }
}
