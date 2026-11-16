namespace CodeLines.Common.Models;

/// <summary>
/// Класс, представляющий результат анализа кода
/// </summary>
public class AnalysisResult
{
    public int CacheVersion
    {
        get; set;
    }
    /// <summary>
    /// Дата и время анализа
    /// </summary>
    public DateTime Date
    {
        get; set;
    }

    /// <summary>
    /// Общее количество файлов
    /// </summary>
    public int FileCount
    {
        get; set;
    }

    /// <summary>
    /// Общее количество строк кода (без пустых строк и комментариев)
    /// </summary>
    public int LineCount
    {
        get; set;
    }

    /// <summary>
    /// Статистика по отдельным файлам для вывода и повторного использования
    /// </summary>
    public List<FileStats> FileStats { get; set; } = [];

    /// <summary>
    /// Хэш-суммы файлов для быстрого определения изменений
    /// </summary>
    public Dictionary<string, string> FileHashes { get; set; } = [];
}
