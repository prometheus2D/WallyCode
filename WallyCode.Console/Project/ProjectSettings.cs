using System.Text.Json;
using System.Text.Json.Serialization;
using WallyCode.ConsoleApp.Copilot;

namespace WallyCode.ConsoleApp.Project;

internal sealed class LoggingSettings
{
    public bool Enabled { get; set; }
    public bool Verbose { get; set; }
}

internal sealed class ProjectSettings
{
    private static readonly string DefaultProviderName = ProviderRegistry.DefaultProviderName;

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public string Provider { get; set; } = DefaultProviderName;

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Model { get; set; }

    public LoggingSettings Logging { get; set; } = new();

    public DateTimeOffset UpdatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public static ProjectSettings Load(string projectRoot)
    {
        var settingsPath = GetFilePath(projectRoot);

        if (!File.Exists(settingsPath))
        {
            return new ProjectSettings();
        }

        var settings = JsonSerializer.Deserialize<ProjectSettings>(File.ReadAllText(settingsPath), SerializerOptions)
            ?? new ProjectSettings();

        settings.Provider = ResolveProviderName(settings.Provider);
        settings.Model = ResolveModelName(settings.Model);
        settings.Logging ??= new LoggingSettings();

        if (settings.UpdatedAtUtc == default)
        {
            settings.UpdatedAtUtc = DateTimeOffset.UtcNow;
        }

        return settings;
    }

    public void Save(string projectRoot)
    {
        Directory.CreateDirectory(projectRoot);
        Provider = ResolveProviderName(Provider);
        Model = ResolveModelName(Model);
        Logging ??= new LoggingSettings();
        UpdatedAtUtc = DateTimeOffset.UtcNow;

        var json = JsonSerializer.Serialize(this, SerializerOptions);
        File.WriteAllText(GetFilePath(projectRoot), json + Environment.NewLine);
    }

    public static string ResolveProjectRoot(string? sourcePath)
    {
        var projectRoot = string.IsNullOrWhiteSpace(sourcePath)
            ? Environment.CurrentDirectory
            : Path.GetFullPath(sourcePath);

        if (!Directory.Exists(projectRoot))
        {
            throw new DirectoryNotFoundException($"Project root does not exist: {projectRoot}");
        }

        return projectRoot;
    }

    public static string GetFilePath(string projectRoot)
    {
        return Path.Combine(projectRoot, "wallycode.json");
    }

    public static string ResolveRuntimeRoot(string projectRoot, string? memoryRoot)
    {
        return string.IsNullOrWhiteSpace(memoryRoot)
            ? Path.Combine(projectRoot, ".wallycode")
            : Path.GetFullPath(memoryRoot);
    }

    private static string ResolveProviderName(string? providerName)
    {
        if (string.IsNullOrWhiteSpace(providerName))
        {
            return DefaultProviderName;
        }

        return providerName.Trim();
    }

    private static string? ResolveModelName(string? modelName)
    {
        return string.IsNullOrWhiteSpace(modelName)
            ? null
            : modelName.Trim();
    }
}