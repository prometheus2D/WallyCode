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

    public void LogCommand(string commandName, IEnumerable<string> args, bool verboseOnly = false)
    {
        var renderedArgs = args.Select(RenderArgument);
        LogEvent("COMMAND", $"{commandName} {string.Join(" ", renderedArgs)}".Trim(), verboseOnly);
    }

    public void LogAction(string action, string details, bool verboseOnly = false)
    {
        LogEvent("ACTION", $"{action}: {details}", verboseOnly);
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

    private void LogEvent(string level, string message, bool verboseOnly)
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

            var line = $"[{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss zzz}] [{level}] {message}{Environment.NewLine}";
            File.AppendAllText(_logFilePath, line);
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

    private static string RenderArgument(string arg)
    {
        if (string.IsNullOrEmpty(arg))
        {
            return "\"\"";
        }

        return arg.Any(char.IsWhiteSpace) || arg.Contains('"')
            ? $"\"{arg.Replace("\"", "\\\"")}" + "\""
            : arg;
    }
}