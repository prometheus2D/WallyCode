using System.Text;
using System.Text.Json;
using WallyCode.ConsoleApp.App;
using WallyCode.ConsoleApp.Loop;

namespace WallyCode.ConsoleApp.Runtime;

internal sealed class MemoryWorkspace
{
    private static readonly UTF8Encoding Utf8NoBom = new(false);
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public required string RootPath { get; init; }

    public required string MemoryDirectoryPath { get; init; }

    public required string LogsDirectoryPath { get; init; }

    public required string PromptsDirectoryPath { get; init; }

    public required string RawDirectoryPath { get; init; }

    public required string GoalFilePath { get; init; }

    public required string CurrentTasksFilePath { get; init; }

    public required string PerspectivesFilePath { get; init; }

    public required string NextStepsFilePath { get; init; }

    public required string CurrentStateFilePath { get; init; }

    public required string UserResponsesFilePath { get; init; }

    public required string SessionLogFilePath { get; init; }

    public required string SessionStateFilePath { get; init; }

    public static MemoryWorkspace Open(string sourcePath, string? memoryRoot)
    {
        var rootPath = ResolveRootPath(sourcePath, memoryRoot);
        var memoryPath = Path.Combine(rootPath, "memory");
        var logsPath = Path.Combine(rootPath, "logs");
        var promptsPath = Path.Combine(rootPath, "prompts");
        var rawPath = Path.Combine(rootPath, "raw");

        Directory.CreateDirectory(rootPath);
        Directory.CreateDirectory(memoryPath);
        Directory.CreateDirectory(logsPath);
        Directory.CreateDirectory(promptsPath);
        Directory.CreateDirectory(rawPath);

        var workspace = new MemoryWorkspace
        {
            RootPath = rootPath,
            MemoryDirectoryPath = memoryPath,
            LogsDirectoryPath = logsPath,
            PromptsDirectoryPath = promptsPath,
            RawDirectoryPath = rawPath,
            GoalFilePath = Path.Combine(memoryPath, "goal.md"),
            CurrentTasksFilePath = Path.Combine(memoryPath, "current-tasks.md"),
            PerspectivesFilePath = Path.Combine(memoryPath, "perspectives.md"),
            NextStepsFilePath = Path.Combine(memoryPath, "next-steps.md"),
            CurrentStateFilePath = Path.Combine(memoryPath, "current-state.md"),
            UserResponsesFilePath = Path.Combine(memoryPath, "user-responses.md"),
            SessionLogFilePath = Path.Combine(logsPath, "session.log"),
            SessionStateFilePath = Path.Combine(rootPath, "session.json")
        };

        return workspace;
    }

    public LoopSessionState StartNewSession(AppOptions options, LoopTemplate template)
    {
        ResetDirectory(MemoryDirectoryPath);
        ResetDirectory(LogsDirectoryPath);
        ResetDirectory(PromptsDirectoryPath);
        ResetDirectory(RawDirectoryPath);

        WriteDocument(GoalFilePath, $$"""
# Goal

{{options.Goal.Trim()}}
""");

        WriteDocument(CurrentTasksFilePath, string.IsNullOrWhiteSpace(template.InitialCurrentTasks)
            ? "# Current Tasks\n\n1. Run one bounded iteration."
            : template.InitialCurrentTasks);

        WriteDocument(PerspectivesFilePath, string.IsNullOrWhiteSpace(template.InitialPerspectives)
            ? "# Perspectives"
            : template.InitialPerspectives);

        WriteDocument(NextStepsFilePath, string.IsNullOrWhiteSpace(template.InitialNextSteps)
            ? "# Next Steps\n\n1. Run the next iteration."
            : template.InitialNextSteps);

        WriteDocument(CurrentStateFilePath, $$"""
{{(string.IsNullOrWhiteSpace(template.InitialCurrentState) ? "# Current State" : template.InitialCurrentState)}}

- Goal: {{options.Goal.Trim()}}
- Provider: {{options.ProviderName}}
- Source path: {{options.SourcePath ?? "Not provided"}}
- Memory root: {{RootPath}}
- Requested steps in this run: {{options.MaxIterations}}
- Model: {{options.Model ?? "Default"}}
- Loop template: {{template.TemplateId}}
- Session started: {{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss zzz}}
""");

        WriteDocument(UserResponsesFilePath, "# User Responses\n");

        File.WriteAllText(SessionLogFilePath, string.Empty, Utf8NoBom);

        var session = new LoopSessionState
        {
            Goal = options.Goal.Trim(),
            ProviderName = options.ProviderName,
            Model = options.Model,
            SourcePath = options.SourcePath ?? Environment.CurrentDirectory,
            NextIteration = 1,
            IsDone = false,
            DoneReason = string.Empty,
            StartedAtUtc = DateTimeOffset.UtcNow,
            UpdatedAtUtc = DateTimeOffset.UtcNow,
            LoopTemplateId = template.TemplateId
        };

        SaveSession(session);
        return session;
    }

    public LoopSessionState? TryLoadSession()
    {
        if (!File.Exists(SessionStateFilePath))
        {
            return null;
        }

        var session = JsonSerializer.Deserialize<LoopSessionState>(File.ReadAllText(SessionStateFilePath), SerializerOptions)
            ?? throw new InvalidOperationException($"The loop session file is invalid: {SessionStateFilePath}");

        session.Validate();
        return session;
    }

    public void SaveSession(LoopSessionState session)
    {
        session.Validate();
        session.UpdatedAtUtc = DateTimeOffset.UtcNow;

        var json = JsonSerializer.Serialize(session, SerializerOptions);
        File.WriteAllText(SessionStateFilePath, json + Environment.NewLine, Utf8NoBom);
    }

    public MemorySnapshot ReadSnapshot()
    {
        return new MemorySnapshot
        {
            Goal = ReadDocument(GoalFilePath),
            CurrentTasks = ReadDocument(CurrentTasksFilePath),
            Perspectives = ReadDocument(PerspectivesFilePath),
            NextSteps = ReadDocument(NextStepsFilePath),
            CurrentState = ReadDocument(CurrentStateFilePath),
            UserResponses = ReadDocument(UserResponsesFilePath)
        };
    }

    public void SavePrompt(int iteration, string prompt)
    {
        File.WriteAllText(GetPromptPath(iteration), Normalize(prompt), Utf8NoBom);
    }

    public void SaveRawOutput(int iteration, string rawOutput)
    {
        File.WriteAllText(GetRawOutputPath(iteration), Normalize(rawOutput), Utf8NoBom);
    }

    public void ApplyIteration(int iteration, LoopIterationResponse response)
    {
        WriteIfProvided(CurrentTasksFilePath, response.CurrentTasks);
        WriteIfProvided(PerspectivesFilePath, response.Perspectives);
        WriteIfProvided(NextStepsFilePath, response.NextSteps);
        WriteIfProvided(CurrentStateFilePath, response.CurrentState);

        var iterationLogPath = Path.Combine(LogsDirectoryPath, $"iteration-{iteration:000}.md");
        var doneReason = string.IsNullOrWhiteSpace(response.DoneReason) ? "N/A" : response.DoneReason;

        File.WriteAllText(iterationLogPath, $$"""
# Iteration {{iteration}}

- Timestamp: {{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss zzz}}
- Status: {{response.Status}}
- Summary: {{response.Summary}}
- Done reason: {{doneReason}}

## Work Log

{{response.WorkLog}}
""", Utf8NoBom);
    }

    public void AppendUserResponse(string response)
    {
        var content = response.Trim();

        if (string.IsNullOrWhiteSpace(content))
        {
            throw new InvalidOperationException("A non-empty user response is required.");
        }

        if (!File.Exists(UserResponsesFilePath))
        {
            WriteDocument(UserResponsesFilePath, "# User Responses\n");
        }

        File.AppendAllText(UserResponsesFilePath, $$"""

## {{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss zzz}}

{{content}}
""", Utf8NoBom);
    }

    public string GetPromptPath(int iteration)
    {
        return Path.Combine(PromptsDirectoryPath, $"iteration-{iteration:000}.txt");
    }

    public string GetRawOutputPath(int iteration)
    {
        return Path.Combine(RawDirectoryPath, $"iteration-{iteration:000}.txt");
    }

    private static string ResolveRootPath(string sourcePath, string? memoryRoot)
    {
        if (!string.IsNullOrWhiteSpace(memoryRoot))
        {
            return Path.GetFullPath(memoryRoot);
        }

        var basePath = Path.GetFullPath(sourcePath);
        return Path.Combine(basePath, ".wallycode");
    }

    private static string ReadDocument(string path)
    {
        return File.Exists(path) ? File.ReadAllText(path) : string.Empty;
    }

    private static void WriteDocument(string path, string content)
    {
        File.WriteAllText(path, Normalize(content), Utf8NoBom);
    }

    private static void WriteIfProvided(string path, string? content)
    {
        if (!string.IsNullOrWhiteSpace(content))
        {
            File.WriteAllText(path, Normalize(content), Utf8NoBom);
        }
    }

    private static void ResetDirectory(string path)
    {
        Directory.CreateDirectory(path);

        foreach (var filePath in Directory.EnumerateFiles(path))
        {
            File.Delete(filePath);
        }
    }

    private static string Normalize(string content)
    {
        return content.Trim() + Environment.NewLine;
    }
}