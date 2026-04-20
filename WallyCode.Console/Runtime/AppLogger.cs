namespace WallyCode.ConsoleApp.Runtime;

internal sealed class LoggingMode
{
    public bool Enabled { get; init; }
    public bool Verbose { get; init; }
}

internal sealed class AppLogger
{
    private readonly object _sync = new();
    private string? _logFilePath;
    private LoggingMode _loggingMode = new();

    public void ConfigureLogging(string runtimeRoot, LoggingMode loggingMode)
    {
        lock (_sync)
        {
            _loggingMode = loggingMode;
            _logFilePath = loggingMode.Enabled
                ? Path.Combine(runtimeRoot, "transcript.log")
                : null;

            if (_logFilePath is not null)
            {
                Directory.CreateDirectory(runtimeRoot);
            }
        }
    }

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

    public void LogExchange(string direction, string title, string content, bool verboseOnly = false)
    {
        lock (_sync)
        {
            if (_logFilePath is null)
            {
                return;
            }

            if (verboseOnly && !_loggingMode.Verbose)
            {
                return;
            }

            var lines = new List<string>
            {
                $"[{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss zzz}] [{direction}] {title}"
            };

            if (!string.IsNullOrWhiteSpace(content))
            {
                lines.Add(content);
            }

            lines.Add(string.Empty);
            File.AppendAllText(_logFilePath, string.Join(Environment.NewLine, lines));
        }
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
        }
    }
}