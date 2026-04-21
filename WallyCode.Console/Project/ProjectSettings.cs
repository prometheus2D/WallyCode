using System.Text.Json;
using System.Text.Json.Serialization;
using WallyCode.ConsoleApp.Copilot;

namespace WallyCode.ConsoleApp.Project;

internal sealed class LoggingSettings
{
    public bool Enabled { get; set; }
    public bool Verbose { get; set; }
}

internal sealed class ProviderModelCatalog
{
    public string Name { get; set; } = string.Empty;
    public bool IsPreferredDefault { get; set; }
}

internal sealed class ProviderCatalogEntry
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string DefaultModel { get; set; } = string.Empty;
    public string? PreferredCheapModel { get; set; }
    public List<ProviderModelCatalog> Models { get; set; } = [];
    public DateTimeOffset? RefreshedAtUtc { get; set; }
}

internal sealed class ProviderCatalogSettings
{
    public List<ProviderCatalogEntry> Providers { get; set; } = [];
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

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? GlobalPrompt { get; set; }

    public LoggingSettings Logging { get; set; } = new();

    public ProviderCatalogSettings ProviderCatalog { get; set; } = new();

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
        settings.GlobalPrompt = ResolveGlobalPrompt(settings.GlobalPrompt);
        settings.Logging ??= new LoggingSettings();
        settings.ProviderCatalog ??= new ProviderCatalogSettings();
        settings.ProviderCatalog.Providers ??= [];

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
        GlobalPrompt = ResolveGlobalPrompt(GlobalPrompt);
        Logging ??= new LoggingSettings();
        ProviderCatalog ??= new ProviderCatalogSettings();
        ProviderCatalog.Providers ??= [];
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

    private static string? ResolveGlobalPrompt(string? globalPrompt)
    {
        return string.IsNullOrWhiteSpace(globalPrompt)
            ? null
            : globalPrompt.Trim();
    }
}