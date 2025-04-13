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
    /// <returns>Результат анализа</returns>
    public async Task<AnalysisResult> AnalyzeAsync(List<FileInfo> files, bool verbose, AnalysisResult? previousResult = null)
    {
        var result = new AnalysisResult
        {
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
            string relativePath = GetRelativePath(file);

            bool needsProcessing = true;
            string fileHash = string.Empty;

            if (previousResult != null &&
                previousResult.FileHashes.TryGetValue(relativePath, out string? previousHash))
            {
                fileHash = await FileHasher.CalculateFileHashAsync(file.FullName);

                if (fileHash == previousHash)
                {
                    var previousStats = previousResult.FileStats.FirstOrDefault(s => s.FilePath == relativePath);
                    if (previousStats != null)
                    {
                        result.LineCount += previousStats.LineCount;

                        if (verbose)
                        {
                            result.FileStats.Add(new FileStats
                            {
                                FilePath = relativePath,
                                LineCount = previousStats.LineCount,
                                FileHash = fileHash
                            });
                        }

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

                if (verbose)
                {
                    result.FileStats.Add(new FileStats
                    {
                        FilePath = relativePath,
                        LineCount = lineCount,
                        FileHash = fileHash
                    });
                }

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

    private async Task<int> CountLinesOfCodeAsync(FileInfo file)
    {
        try
        {
            string[] lines = await File.ReadAllLinesAsync(file.FullName);

            int count = 0;
            bool inMultilineComment = false;

            foreach (var line in lines)
            {
                string trimmedLine = line.Trim();

                if (string.IsNullOrWhiteSpace(trimmedLine))
                {
                    continue;
                }

                if (file.Extension.Equals(".cs", StringComparison.CurrentCultureIgnoreCase) ||
                    file.Extension.Equals(".vb", StringComparison.CurrentCultureIgnoreCase) ||
                    file.Extension.Equals(".js", StringComparison.CurrentCultureIgnoreCase) ||
                    file.Extension.Equals(".ts", StringComparison.CurrentCultureIgnoreCase))
                {
                    if (trimmedLine.StartsWith("//") || trimmedLine.StartsWith('\''))
                    {
                        continue;
                    }

                    if (inMultilineComment)
                    {
                        if (trimmedLine.Contains("*/"))
                        {
                            inMultilineComment = false;
                            
                            string afterComment = trimmedLine[(trimmedLine.IndexOf("*/") + 2)..];

                            if (!string.IsNullOrWhiteSpace(afterComment))
                            {
                                count++;
                            }
                        }
                        continue;
                    }

                    if (trimmedLine.StartsWith("/*"))
                    {
                        inMultilineComment = true;
                        if (trimmedLine.Contains("*/"))
                        {
                            inMultilineComment = false;
                            
                            string beforeComment = trimmedLine[..trimmedLine.IndexOf("/*")];
                            string afterComment = trimmedLine[(trimmedLine.IndexOf("*/") + 2)..];
                            if (!string.IsNullOrWhiteSpace(beforeComment) || !string.IsNullOrWhiteSpace(afterComment))
                            {
                                count++;
                            }
                        }
                        continue;
                    }
                }
                else if (file.Extension.Equals(".html", StringComparison.CurrentCultureIgnoreCase) ||
                         file.Extension.Equals(".cshtml", StringComparison.CurrentCultureIgnoreCase) ||
                         file.Extension.Equals(".xml", StringComparison.CurrentCultureIgnoreCase))
                {

                    if (trimmedLine.StartsWith("<!--") && !trimmedLine.EndsWith("-->"))
                    {
                        inMultilineComment = true;
                        continue;
                    }

                    if (inMultilineComment)
                    {
                        if (trimmedLine.Contains("-->"))
                        {
                            inMultilineComment = false;
                        }
                        continue;
                    }

                    if (trimmedLine.StartsWith("<!--") && trimmedLine.EndsWith("-->"))
                    {
                        continue;
                    }
                }

                count++;
            }

            return count;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception during file analyzing {file.FullName}: {ex.Message}");
            return 0;
        }
    }

    private static string GetRelativePath(FileInfo file)
    {
        return file.FullName.Replace(Directory.GetCurrentDirectory(), "").TrimStart('\\', '/');
    }
}
