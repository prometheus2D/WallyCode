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
    public int? MaxRunIterations { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? MaxTotalIterations { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? MaxStepRepeats { get; set; }
}

internal sealed class ActiveProjectSettings
{
    public string ActiveProjectPath { get; set; } = string.Empty;
    public DateTimeOffset UpdatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
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
    private static string AppDirectoryPath = Path.GetFullPath(AppContext.BaseDirectory);

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

    public static void ConfigureAppDirectory(string? appDirectoryPath)
    {
        AppDirectoryPath = Path.GetFullPath(string.IsNullOrWhiteSpace(appDirectoryPath)
            ? AppContext.BaseDirectory
            : appDirectoryPath);
    }

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
            ? ResolveActiveProjectPath() ?? Environment.CurrentDirectory
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
        return (currentRoot, Load(currentRoot));
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
        return (currentRoot, Load(currentRoot));
    }

    public static string GetFilePath(string projectRoot)
    {
        return Path.Combine(projectRoot, "wallycode.json");
    }

    public static string GetActiveProjectFilePath(string? appDirectoryPath = null)
    {
        var directoryPath = Path.GetFullPath(string.IsNullOrWhiteSpace(appDirectoryPath)
            ? AppDirectoryPath
            : appDirectoryPath);

        return Path.Combine(directoryPath, "wallycode.active.json");
    }

    public static string? ResolveActiveProjectPath(string? appDirectoryPath = null)
    {
        var activeProjectPath = GetActiveProjectFilePath(appDirectoryPath);
        if (!File.Exists(activeProjectPath))
        {
            return null;
        }

        var activeProject = JsonSerializer.Deserialize<ActiveProjectSettings>(File.ReadAllText(activeProjectPath), SerializerOptions)
            ?? new ActiveProjectSettings();

        return NormalizeRuntimePath(activeProject.ActiveProjectPath);
    }

    public static void SaveActiveProjectPath(string projectRoot, string? appDirectoryPath = null)
    {
        var activeProject = new ActiveProjectSettings
        {
            ActiveProjectPath = ResolveProjectRoot(projectRoot),
            UpdatedAtUtc = DateTimeOffset.UtcNow
        };

        var activeProjectPath = GetActiveProjectFilePath(appDirectoryPath);
        Directory.CreateDirectory(Path.GetDirectoryName(activeProjectPath)!);
        var json = JsonSerializer.Serialize(activeProject, SerializerOptions);
        File.WriteAllText(activeProjectPath, json + Environment.NewLine);
    }

    public static void ClearActiveProjectPathIfMatches(string projectRoot, string? appDirectoryPath = null)
    {
        var activeProjectPath = GetActiveProjectFilePath(appDirectoryPath);
        if (!File.Exists(activeProjectPath))
        {
            return;
        }

        var activeProjectRoot = ResolveActiveProjectPath(appDirectoryPath);
        var targetProjectRoot = Path.GetFullPath(projectRoot);
        if (string.Equals(activeProjectRoot, targetProjectRoot, StringComparison.Ordinal))
        {
            File.Delete(activeProjectPath);
        }
    }

    public static string ResolveRuntimeRoot(string projectRoot)
    {
        return Path.Combine(projectRoot, ".wallycode");
    }

    public static string ResolveSessionRoot(string projectRoot)
    {
        var sessionRoot = ResolveRuntimeRoot(projectRoot);
        if (!Directory.Exists(sessionRoot))
        {
            throw new InvalidOperationException($"Session state folder does not exist: {sessionRoot}. Run 'wallycode setup --source {projectRoot}' first.");
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

        var runtimeRoot = ResolveRuntimeRoot(projectRoot);
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