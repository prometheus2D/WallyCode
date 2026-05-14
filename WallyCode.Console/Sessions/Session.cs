using System.Text.Json;
using WallyCode.ConsoleApp.Workflow;

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
    public string LastSelectedStep { get; set; } = string.Empty;
    public string LastSummary { get; set; } = string.Empty;
    public int IterationCount { get; set; }
    public Dictionary<string, string> Memory { get; set; } = [];
    public List<string> PendingResponses { get; set; } = [];
}

internal sealed class Session : SessionBase
{
    public string WorkflowName { get; set; } = string.Empty;
    public string ActiveStepName { get; set; } = string.Empty;

    public static Session Start(WorkflowDefinition definition, string goal, string providerName, string? model, string sourcePath)
    {
        return new Session
        {
            WorkflowName = definition.Name,
            Goal = goal.Trim(),
            ProviderName = providerName,
            Model = model,
            SourcePath = sourcePath,
            ActiveStepName = definition.StartStepName,
            Status = SessionStatus.Active
        };
    }

    public static string FilePath(string rootPath) => Path.Combine(rootPath, "session.json");

    public static string SnapshotRoot(string rootPath) => Path.Combine(rootPath, "sessions");

    public static string SnapshotFilePath(string rootPath, int iterationCount) =>
        Path.Combine(SnapshotRoot(rootPath), $"session-{iterationCount:D4}.json");

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

        if (!Directory.Exists(rootPath))
        {
            throw new InvalidOperationException($"Session state folder does not exist: {rootPath}.");
        }

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
        return LoadFile(FilePath(rootPath));
    }

    public static Session LoadFile(string path)
    {
        if (!File.Exists(path))
        {
            throw new InvalidOperationException($"No session at {path}.");
        }

        var session = JsonSerializer.Deserialize<Session>(File.ReadAllText(path), SessionJson.Default)
            ?? throw new InvalidOperationException($"Session file is invalid: {path}");
        session.Normalize();
        session.ValidateShape(path);
        return session;
    }

    public void Save(string rootPath)
    {
        Normalize();
        if (!Directory.Exists(rootPath))
        {
            throw new InvalidOperationException($"Session state folder does not exist: {rootPath}.");
        }

        File.WriteAllText(FilePath(rootPath), JsonSerializer.Serialize(this, SessionJson.Default));
    }

    public void SaveSnapshot(string rootPath)
    {
        if (IterationCount <= 0)
        {
            return;
        }

        Normalize();
        Directory.CreateDirectory(SnapshotRoot(rootPath));
        File.WriteAllText(SnapshotFilePath(rootPath, IterationCount), JsonSerializer.Serialize(this, SessionJson.Default));
    }

    private void Normalize()
    {
        PendingResponses ??= [];
        Memory ??= [];
    }

    private void ValidateShape(string path)
    {
        if (string.IsNullOrWhiteSpace(WorkflowName)
            || string.IsNullOrWhiteSpace(ActiveStepName)
            || string.IsNullOrWhiteSpace(ProviderName)
            || string.IsNullOrWhiteSpace(SourcePath)
            || string.IsNullOrWhiteSpace(Goal)
            || string.IsNullOrWhiteSpace(Status))
        {
            throw new InvalidOperationException(
                $"Session file at {path} is not in the current routed-session schema. Reset session state (for example: shell reset-memory) and start a new session.");
        }

        if (Status is not SessionStatus.Active and not SessionStatus.Blocked and not SessionStatus.Completed and not SessionStatus.Error)
        {
            throw new InvalidOperationException($"Session file at {path} has unsupported status '{Status}'.");
        }
    }
}
