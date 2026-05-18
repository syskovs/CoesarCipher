using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using CoesarCipher.Core;

namespace CoesarCipher.Cli;

public static class CliRunner
{
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool AttachConsole(int processId);
    private const int ATTACH_PARENT_PROCESS = -1;

    public static int Run(string[] args)
    {
        AttachConsoleAndRedirect();

        try
        {
            var opts = ParseArgs(args);
            if (opts.Help)
            {
                PrintHelp();
                return 0;
            }
            return Execute(opts);
        }
        catch (CliException ex)
        {
            Console.Error.WriteLine($"Ошибка: {ex.Message}");
            Console.Error.WriteLine("Подсказка: запустите с --help для справки.");
            return 1;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Непредвиденная ошибка: {ex.Message}");
            return 2;
        }
    }

    private static void AttachConsoleAndRedirect()
    {
        if (!AttachConsole(ATTACH_PARENT_PROCESS)) return;

        Console.OutputEncoding = Encoding.UTF8;
        Console.InputEncoding = Encoding.UTF8;

        var stdout = new StreamWriter(Console.OpenStandardOutput(), Encoding.UTF8) { AutoFlush = true };
        Console.SetOut(stdout);
        var stderr = new StreamWriter(Console.OpenStandardError(), Encoding.UTF8) { AutoFlush = true };
        Console.SetError(stderr);
    }

    private static int Execute(Options opts)
    {
        if (!opts.ModeSet)
            throw new CliException("Не указан режим (--encrypt или --decrypt)");
        if (!opts.KeySet)
            throw new CliException("Не указан ключ (--key N)");

        if (opts.BatchDir != null)
        {
            if (opts.OutDir == null)
                throw new CliException("Для пакетной обработки укажите --out-dir <папка>");

            var result = BatchProcessor.ProcessDirectory(
                opts.BatchDir, opts.OutDir, opts.Key, opts.Mode,
                progress: (file, i, total) =>
                    Console.WriteLine($"[{i}/{total}] {Path.GetFileName(file)}"));

            Console.WriteLine($"Готово. Обработано: {result.Processed}, ошибок: {result.Failed}");
            foreach (var err in result.Errors)
                Console.Error.WriteLine($"  - {err}");
            return result.Failed > 0 ? 3 : 0;
        }

        string input;
        if (opts.Text != null)
        {
            input = opts.Text;
        }
        else if (opts.InputFile != null)
        {
            if (!File.Exists(opts.InputFile))
                throw new CliException($"Файл не найден: {opts.InputFile}");
            input = File.ReadAllText(opts.InputFile, Encoding.UTF8);
        }
        else
        {
            if (Console.IsInputRedirected)
                input = Console.In.ReadToEnd();
            else
                throw new CliException("Не указан ввод (--text, --in или поток из pipe)");
        }

        string output = CaesarCipher.Process(input, opts.Key, opts.Mode);

        if (opts.OutputFile != null)
        {
            File.WriteAllText(opts.OutputFile, output, new UTF8Encoding(false));
            Console.WriteLine($"Записано: {opts.OutputFile}");
        }
        else
        {
            Console.WriteLine(output);
        }
        return 0;
    }

    private static Options ParseArgs(string[] args)
    {
        var opts = new Options();
        for (int i = 0; i < args.Length; i++)
        {
            string a = args[i];
            switch (a)
            {
                case "-h":
                case "--help":
                case "/?":
                    opts.Help = true;
                    break;

                case "-e":
                case "--encrypt":
                    opts.Mode = CipherMode.Encrypt;
                    opts.ModeSet = true;
                    break;

                case "-d":
                case "--decrypt":
                    opts.Mode = CipherMode.Decrypt;
                    opts.ModeSet = true;
                    break;

                case "-k":
                case "--key":
                    opts.Key = ParseIntArg(args, ref i, "--key");
                    opts.KeySet = true;
                    break;

                case "-t":
                case "--text":
                    opts.Text = NextArg(args, ref i, "--text");
                    break;

                case "-i":
                case "--in":
                    opts.InputFile = NextArg(args, ref i, "--in");
                    break;

                case "-o":
                case "--out":
                    opts.OutputFile = NextArg(args, ref i, "--out");
                    break;

                case "-b":
                case "--batch":
                    opts.BatchDir = NextArg(args, ref i, "--batch");
                    break;

                case "--out-dir":
                    opts.OutDir = NextArg(args, ref i, "--out-dir");
                    break;

                default:
                    throw new CliException($"Неизвестный аргумент: {a}");
            }
        }
        return opts;
    }

    private static string NextArg(string[] args, ref int i, string name)
    {
        if (++i >= args.Length)
            throw new CliException($"После {name} ожидалось значение");
        return args[i];
    }

    private static int ParseIntArg(string[] args, ref int i, string name)
    {
        string v = NextArg(args, ref i, name);
        if (!int.TryParse(v, out int n))
            throw new CliException($"{name}: ожидалось целое число, получено '{v}'");
        return n;
    }

    private static void PrintHelp()
    {
        Console.WriteLine("""
            Шифр Цезаря — GUI/CLI утилита

            Использование:
              CoesarCipher.exe                            запуск GUI
              CoesarCipher.exe [опции]                    запуск CLI

            Режим (обязательно один):
              -e, --encrypt              шифровать
              -d, --decrypt              дешифровать

            Параметры:
              -k, --key <N>              ключ сдвига (целое число, может быть отрицательным)
              -t, --text <строка>        текст для обработки
              -i, --in  <файл>           прочитать ввод из файла (UTF-8)
              -o, --out <файл>           записать результат в файл (UTF-8)
              -b, --batch <папка>        пакетная обработка всех *.txt в папке
                  --out-dir <папка>      папка для результатов пакетной обработки
              -h, --help                 показать эту справку

            Примеры:
              CoesarCipher.exe -e -k 3 -t "Привет, мир!"
              CoesarCipher.exe -d -k 3 -i secret.txt -o plain.txt
              CoesarCipher.exe -e -k 5 -b .\input -o .\output
              echo Hello | CoesarCipher.exe -e -k 2

            Примечание:
              В cmd.exe вывод появится после возврата приглашения. Используйте
              "start /wait CoesarCipher.exe ..." или запускайте из PowerShell.
            """);
    }

    private sealed class Options
    {
        public bool Help;
        public bool ModeSet;
        public CipherMode Mode;
        public bool KeySet;
        public int Key;
        public string? Text;
        public string? InputFile;
        public string? OutputFile;
        public string? BatchDir;
        public string? OutDir;
    }

    private sealed class CliException : Exception
    {
        public CliException(string message) : base(message) { }
    }
}
