using CodeLines.Common.Models;

namespace CodeLines.Common;
public class CodeAnalyzer
{
    public async Task<AnalysisResult> AnalyzeAsync(List<FileInfo> files, bool verbose)
    {
        var result = new AnalysisResult
        {
            Date = DateTime.Now,
            FileCount = files.Count,
            LineCount = 0,
            FileStats = []
        };

        foreach (var file in files)
        {
            int lineCount = await CountLinesOfCodeAsync(file);

            result.LineCount += lineCount;

            if (verbose)
            {
                result.FileStats.Add(new FileStats
                {
                    FilePath = GetRelativePath(file),
                    LineCount = lineCount
                });
            }
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
