using CodeLines.Common.Models;

namespace CodeLines.Common;

public class CodeAnalyzer
{
    /// <summary>
    /// Анализирует файлы с кодом и вычисляет общее количество строк
    /// </summary>
    /// <param name="files">Список файлов для анализа</param>
    /// <param name="verbose">Подробный вывод информации</param>
    /// <param name="previousResult">Предыдущий результат анализа (для оптимизации)</param>
    /// <param name="projectRoot">Корень сканируемого проекта для относительных путей</param>
    /// <returns>Результат анализа</returns>
    public async Task<AnalysisResult> AnalyzeAsync(List<FileInfo> files, bool verbose, AnalysisResult? previousResult = null, string? projectRoot = null)
    {
        var result = new AnalysisResult
        {
            CacheVersion = 2,
            Date = DateTime.Now,
            FileCount = files.Count,
            LineCount = 0,
            FileStats = [],
            FileHashes = []
        };

        var filesProcessed = 0;
        var filesSkipped = 0;

        foreach (var file in files)
        {
            string relativePath = Path.GetRelativePath(projectRoot ?? Directory.GetCurrentDirectory(), file.FullName);

            bool needsProcessing = true;
            string fileHash = string.Empty;

            if (previousResult != null && previousResult.CacheVersion == 2 && previousResult.FileStats != null && previousResult.FileHashes != null &&
                previousResult.FileHashes.TryGetValue(relativePath, out string? previousHash))
            {
                fileHash = await FileHasher.CalculateFileHashAsync(file.FullName);

                if (fileHash == previousHash)
                {
                    var previousStats = previousResult.FileStats.FirstOrDefault(s => s.FilePath == relativePath);
                    if (previousStats != null)
                    {
                        result.LineCount += previousStats.LineCount;

                        result.FileStats.Add(new FileStats
                        {
                            FilePath = relativePath,
                            LineCount = previousStats.LineCount,
                            FileHash = fileHash
                        });

                        result.FileHashes[relativePath] = fileHash;
                        needsProcessing = false;
                        filesSkipped++;
                    }
                }
            }

            if (needsProcessing)
            {
                int lineCount = await CountLinesOfCodeAsync(file);

                result.LineCount += lineCount;

                if (string.IsNullOrEmpty(fileHash))
                {
                    fileHash = await FileHasher.CalculateFileHashAsync(file.FullName);
                }

                result.FileStats.Add(new FileStats
                {
                    FilePath = relativePath,
                    LineCount = lineCount,
                    FileHash = fileHash
                });

                result.FileHashes[relativePath] = fileHash;
                filesProcessed++;
            }

            if (files.Count > 100 && filesProcessed % 10 == 0)
            {
                Console.Write($"\rProcessed: {filesProcessed + filesSkipped}/{files.Count} files.");
            }
        }
        if (files.Count > 100)
        {
            Console.WriteLine($"\rProcessed: {filesProcessed + filesSkipped}/{files.Count} files.");
        }

        if (filesSkipped > 0)
        {
            Console.WriteLine($"Skipped: {filesSkipped} unchanged files.");
        }

        return result;
    }

    private static async Task<int> CountLinesOfCodeAsync(FileInfo file)
    {
        var extension = file.Extension.ToLowerInvariant();
        bool cStyle = extension is ".cs" or ".js" or ".ts" or ".jsx" or ".tsx" or ".css" or ".scss" or ".less";
        bool xmlStyle = extension is ".html" or ".xml" or ".xaml" or ".config" or ".csproj" or ".vbproj" or ".fsproj" or ".props" or ".targets";
        bool vb = extension == ".vb";
        bool inBlock = false;
        char quote = '\0';
        bool verbatim = false;
        int count = 0;
        foreach (var line in await File.ReadAllLinesAsync(file.FullName))
        {
            bool code = false;
            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                if (inBlock)
                {
                    string end = xmlStyle ? "-->" : "*/";
                    if (line.AsSpan(i).StartsWith(end))
                    {
                        inBlock = false;
                        i += end.Length - 1;
                    }
                    continue;
                }
                if (quote != '\0')
                {
                    code = true;
                    if (verbatim && c == '"' && i + 1 < line.Length && line[i + 1] == '"')
                    {
                        i++;
                        continue;
                    }
                    if (!verbatim && c == '\\' && i + 1 < line.Length)
                    {
                        i++;
                        continue;
                    }
                    if (c == quote)
                    {
                        quote = '\0';
                        verbatim = false;
                    }
                    continue;
                }
                if (xmlStyle && line.AsSpan(i).StartsWith("<!--"))
                {
                    inBlock = true;
                    i += 3;
                    continue;
                }
                if (cStyle && line.AsSpan(i).StartsWith("//"))
                    break;
                if (cStyle && line.AsSpan(i).StartsWith("/*"))
                {
                    inBlock = true;
                    i++;
                    continue;
                }
                if (vb && c == '\'')
                    break;
                if ((cStyle || vb) && (c == '"' || (cStyle && c == '\'')))
                {
                    quote = c;
                    verbatim = c == '"' && ((vb) || (extension == ".cs" && i > 0 && line[i - 1] == '@'));
                    code = true;
                    continue;
                }
                if (!char.IsWhiteSpace(c))
                    code = true;
            }
            if (code)
                count++;
            if (!verbatim)
                quote = '\0';
        }
        return count;
    }
}
