using System.IO;
using System.Text;

namespace CoesarCipher.Core;

public sealed record BatchResult(int Processed, int Failed, IReadOnlyList<string> Errors);

public static class BatchProcessor
{
    public static BatchResult ProcessDirectory(
        string inputDir,
        string outputDir,
        int key,
        CipherMode mode,
        string searchPattern = "*.txt",
        Action<string, int, int>? progress = null)
    {
        if (string.IsNullOrWhiteSpace(inputDir))
            throw new ArgumentException("Не указана папка с исходными файлами", nameof(inputDir));
        if (string.IsNullOrWhiteSpace(outputDir))
            throw new ArgumentException("Не указана папка для результатов", nameof(outputDir));
        if (!Directory.Exists(inputDir))
            throw new DirectoryNotFoundException($"Папка не найдена: {inputDir}");

        Directory.CreateDirectory(outputDir);

        var files = Directory.GetFiles(inputDir, searchPattern);
        int processed = 0, failed = 0;
        var errors = new List<string>();

        for (int i = 0; i < files.Length; i++)
        {
            var file = files[i];
            try
            {
                string content = File.ReadAllText(file, Encoding.UTF8);
                string result = CaesarCipher.Process(content, key, mode);
                string outFile = Path.Combine(outputDir, Path.GetFileName(file));
                File.WriteAllText(outFile, result, new UTF8Encoding(false));
                processed++;
            }
            catch (Exception ex)
            {
                failed++;
                errors.Add($"{Path.GetFileName(file)}: {ex.Message}");
            }
            progress?.Invoke(file, i + 1, files.Length);
        }
        return new BatchResult(processed, failed, errors);
    }
}
