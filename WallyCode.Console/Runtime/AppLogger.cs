using System.Text;

namespace WallyCode.ConsoleApp.Runtime;

internal sealed class AppLogger
{
    private readonly object _sync = new();

    public string? LogFilePath { get; set; }

    public void Section(string title)
    {
        Write("SECTION", ConsoleColor.Blue, $"==== {title} ====");
    }

    public void Info(string message)
    {
        Write("INFO", ConsoleColor.Cyan, message);
    }

    public void Success(string message)
    {
        Write("OK", ConsoleColor.Green, message);
    }

    public void Warning(string message)
    {
        Write("WARN", ConsoleColor.Yellow, message);
    }

    public void Error(string message)
    {
        Write("ERROR", ConsoleColor.Red, message);
    }

    private void Write(string level, ConsoleColor color, string message)
    {
        var line = $"[{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss zzz}] [{level}] {message}";

        lock (_sync)
        {
            var originalColor = Console.ForegroundColor;
            Console.ForegroundColor = color;
            Console.WriteLine(line);
            Console.ForegroundColor = originalColor;

            if (!string.IsNullOrWhiteSpace(LogFilePath))
            {
                File.AppendAllText(LogFilePath, line + Environment.NewLine, new UTF8Encoding(false));
            }
        }
    }
}