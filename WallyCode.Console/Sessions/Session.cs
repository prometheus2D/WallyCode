using System.Text.Json;
using WallyCode.ConsoleApp.Routing;

namespace WallyCode.ConsoleApp.Sessions;

internal static class SessionStatus
{
    public const string Active = "active";
    public const string Blocked = "blocked";
    public const string Completed = "completed";
    public const string Error = "error";
}

internal abstract class SessionBase
{
    public string Goal { get; set; } = string.Empty;
    public string ProviderName { get; set; } = string.Empty;
    public string? Model { get; set; }
    public string SourcePath { get; set; } = string.Empty;
    public string Status { get; set; } = SessionStatus.Active;
    public string LastSelectedKeyword { get; set; } = string.Empty;
    public string LastSummary { get; set; } = string.Empty;
    public int IterationCount { get; set; }
    public List<string> PendingResponses { get; set; } = [];
}

internal sealed class Session : SessionBase
{
    public string DefinitionName { get; set; } = string.Empty;
    public string ActiveUnitName { get; set; } = string.Empty;

    public static Session Start(RoutingDefinition definition, string goal, string providerName, string? model, string sourcePath)
    {
        return new Session
        {
            DefinitionName = definition.Name,
            Goal = goal.Trim(),
            ProviderName = providerName,
            Model = model,
            SourcePath = sourcePath,
            ActiveUnitName = definition.StartUnitName,
            Status = SessionStatus.Active
        };
    }

    public static string FilePath(string rootPath) => Path.Combine(rootPath, "session.json");

    public static bool Exists(string rootPath) => File.Exists(FilePath(rootPath));

    public static bool IsTerminal(string? status) => status is SessionStatus.Completed or SessionStatus.Error;

    public static string ArchiveRoot(string rootPath) => Path.Combine(rootPath, "archive");

    public static string ArchiveCompletedSession(string rootPath)
    {
        var session = Load(rootPath);
        if (!IsTerminal(session.Status))
        {
            throw new InvalidOperationException("Only completed or error sessions can be archived.");
        }

        Directory.CreateDirectory(rootPath);
        var archiveRoot = ArchiveRoot(rootPath);
        Directory.CreateDirectory(archiveRoot);

        var timestamp = DateTimeOffset.UtcNow.ToString("yyyyMMdd-HHmmss");
        var archiveName = $"session-{timestamp}";
        var archivePath = Path.Combine(archiveRoot, archiveName);
        var suffix = 1;

        while (Directory.Exists(archivePath))
        {
            archivePath = Path.Combine(archiveRoot, $"{archiveName}-{suffix++}");
        }

        Directory.CreateDirectory(archivePath);

        foreach (var entry in Directory.EnumerateFileSystemEntries(rootPath))
        {
            if (string.Equals(Path.GetFileName(entry), "archive", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var destination = Path.Combine(archivePath, Path.GetFileName(entry));
            if (Directory.Exists(entry))
            {
                Directory.Move(entry, destination);
            }
            else
            {
                File.Move(entry, destination);
            }
        }

        return archivePath;
    }

    public static Session Load(string rootPath)
    {
        var path = FilePath(rootPath);
        if (!File.Exists(path))
        {
            throw new InvalidOperationException($"No session at {path}.");
        }

        return JsonSerializer.Deserialize<Session>(File.ReadAllText(path), JsonOptions.Default)
            ?? throw new InvalidOperationException($"Session file is invalid: {path}");
    }

    public void Save(string rootPath)
    {
        Directory.CreateDirectory(rootPath);
        File.WriteAllText(FilePath(rootPath), JsonSerializer.Serialize(this, JsonOptions.Default));
    }
}
