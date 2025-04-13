using System.Security.Cryptography;

namespace CodeLines.Common;

/// <summary>
/// Класс для расчета хэшей файлов
/// </summary>
public class FileHasher
{
    /// <summary>
    /// Вычисляет MD5 хэш файла
    /// </summary>
    /// <param name="filePath">Путь к файлу</param>
    /// <returns>Строковое представление хэша</returns>
    public static async Task<string> CalculateFileHashAsync(string filePath)
    {
        try
        {
            using var md5 = MD5.Create();
            using var stream = File.OpenRead(filePath);
            byte[] hashBytes = await md5.ComputeHashAsync(stream);

            return Convert.ToHexStringLower(hashBytes);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error hashing file {filePath}: {ex.Message}");
            return string.Empty;
        }
    }

    /// <summary>
    /// Проверяет, изменился ли файл, сравнивая его текущий хэш с сохраненным
    /// </summary>
    /// <param name="filePath">Путь к файлу</param>
    /// <param name="savedHash">Сохраненный хэш</param>
    /// <returns>true, если файл изменился; иначе false</returns>
    public static async Task<bool> HasFileChangedAsync(string filePath, string savedHash)
    {
        if (string.IsNullOrEmpty(savedHash))
        {
            return true;
        }

        string currentHash = await CalculateFileHashAsync(filePath);
        return !string.Equals(currentHash, savedHash, StringComparison.OrdinalIgnoreCase);
    }
}
