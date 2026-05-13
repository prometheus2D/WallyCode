using System.Text.Json;
using System.Text.Json.Serialization;
using WallyCode.ConsoleApp.Copilot;

namespace WallyCode.ConsoleApp.Project;

internal sealed class LoggingSettings
{
    public bool Enabled { get; set; }
    public bool Verbose { get; set; }
}

internal sealed class RuntimeDefaultsSettings
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? SourcePath { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? MemoryRoot { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? MaxRunIterations { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? MaxTotalIterations { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? MaxStepRepeats { get; set; }
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

    public RuntimeDefaultsSettings RuntimeDefaults { get; set; } = new();

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
        settings.RuntimeDefaults = NormalizeRuntimeDefaults(settings.RuntimeDefaults);
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
        RuntimeDefaults = NormalizeRuntimeDefaults(RuntimeDefaults);
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

    public static (string ProjectRoot, ProjectSettings Settings) ResolveProjectContext(string? sourcePath)
    {
        if (!string.IsNullOrWhiteSpace(sourcePath))
        {
            var explicitRoot = ResolveProjectRoot(sourcePath);
            return (explicitRoot, Load(explicitRoot));
        }

        var currentRoot = ResolveProjectRoot(null);
        var currentSettings = Load(currentRoot);
        var defaultSourcePath = NormalizeRuntimePath(currentSettings.RuntimeDefaults.SourcePath);
        if (string.IsNullOrWhiteSpace(defaultSourcePath))
        {
            return (currentRoot, currentSettings);
        }

        try
        {
            var preferredRoot = ResolveProjectRoot(defaultSourcePath);
            return (preferredRoot, Load(preferredRoot));
        }
        catch (DirectoryNotFoundException)
        {
            return (currentRoot, currentSettings);
        }
    }

    public static ProjectSettings LoadRequired(string projectRoot)
    {
        EnsureSetupInitialized(projectRoot);
        return Load(projectRoot);
    }

    public static (string ProjectRoot, ProjectSettings Settings) ResolveInitializedProjectContext(string? sourcePath)
    {
        if (!string.IsNullOrWhiteSpace(sourcePath))
        {
            var explicitRoot = ResolveProjectRoot(sourcePath);
            EnsureSetupInitialized(explicitRoot);
            return (explicitRoot, Load(explicitRoot));
        }

        var currentRoot = ResolveProjectRoot(null);
        EnsureSetupInitialized(currentRoot);

        var currentSettings = Load(currentRoot);
        var defaultSourcePath = NormalizeRuntimePath(currentSettings.RuntimeDefaults.SourcePath);
        if (string.IsNullOrWhiteSpace(defaultSourcePath))
        {
            return (currentRoot, currentSettings);
        }

        try
        {
            var preferredRoot = ResolveProjectRoot(defaultSourcePath);
            EnsureSetupInitialized(preferredRoot);
            return (preferredRoot, Load(preferredRoot));
        }
        catch (DirectoryNotFoundException)
        {
            return (currentRoot, currentSettings);
        }
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

    public static string ResolveSessionRoot(ProjectSettings settings, string projectRoot, string? memoryRoot)
    {
        var effectiveMemoryRoot = string.IsNullOrWhiteSpace(memoryRoot)
            ? settings.RuntimeDefaults.MemoryRoot
            : memoryRoot;

        var sessionRoot = ResolveRuntimeRoot(projectRoot, effectiveMemoryRoot);
        if (!Directory.Exists(sessionRoot))
        {
            throw new InvalidOperationException($"Session root does not exist: {sessionRoot}. Run 'wallycode setup --source {projectRoot}' first.");
        }

        return sessionRoot;
    }

    private static void EnsureSetupInitialized(string projectRoot)
    {
        var settingsPath = GetFilePath(projectRoot);
        if (!File.Exists(settingsPath))
        {
            throw new InvalidOperationException(
                $"Project is not initialized at '{projectRoot}'. Run 'wallycode setup --source {projectRoot}' first.");
        }

        var runtimeRoot = ResolveRuntimeRoot(projectRoot, memoryRoot: null);
        if (!Directory.Exists(runtimeRoot))
        {
            throw new InvalidOperationException(
                $"Project runtime folder is missing at '{runtimeRoot}'. Run 'wallycode setup --source {projectRoot}' first.");
        }
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

    private static RuntimeDefaultsSettings NormalizeRuntimeDefaults(RuntimeDefaultsSettings? runtimeDefaults)
    {
        var normalized = runtimeDefaults ?? new RuntimeDefaultsSettings();
        normalized.SourcePath = NormalizeRuntimePath(normalized.SourcePath);
        normalized.MemoryRoot = NormalizeRuntimePath(normalized.MemoryRoot);
        normalized.MaxRunIterations = normalized.MaxRunIterations.HasValue && normalized.MaxRunIterations.Value > 0
            ? normalized.MaxRunIterations
            : null;
        normalized.MaxTotalIterations = normalized.MaxTotalIterations.HasValue && normalized.MaxTotalIterations.Value >= 0
            ? normalized.MaxTotalIterations
            : null;
        normalized.MaxStepRepeats = normalized.MaxStepRepeats.HasValue && normalized.MaxStepRepeats.Value >= 0
            ? normalized.MaxStepRepeats
            : null;
        return normalized;
    }

    private static string? NormalizeRuntimePath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        try
        {
            return Path.GetFullPath(path.Trim());
        }
        catch (Exception) when (true)
        {
            return null;
        }
    }
}