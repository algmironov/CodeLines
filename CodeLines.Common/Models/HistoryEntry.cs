namespace CodeLines.Common.Models;

/// <summary>
/// Класс, представляющий запись в истории анализа
/// </summary>
public class HistoryEntry
{
    /// <summary>
    /// Дата и время анализа
    /// </summary>
    public DateTime Date { get; set; }

    /// <summary>
    /// Общее количество файлов
    /// </summary>
    public int FileCount { get; set; }

    /// <summary>
    /// Общее количество строк кода (без пустых строк и комментариев)
    /// </summary>
    public int LineCount { get; set; }
}
