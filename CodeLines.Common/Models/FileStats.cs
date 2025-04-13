namespace CodeLines.Common.Models;

/// <summary>
/// Класс, представляющий статистику по отдельному файлу
/// </summary>
public class FileStats
{
    /// <summary>
    /// Относительный путь к файлу
    /// </summary>
    public string FilePath { get; set; }

    /// <summary>
    /// Количество строк кода в файле (без пустых строк и комментариев)
    /// </summary>
    public int LineCount { get; set; }

    /// <summary>
    /// Хэш-сумма файла для определения изменений
    /// </summary>
    public string FileHash { get; set; }
}
