using System.Text.Json;
using System.Text.Json.Serialization;

namespace WallyCode.ConsoleApp.Project;

internal sealed class ProjectSettings
{
    private const string LegacySingleProviderName = "gh-copilot";
    private const string LegacyClaudeProviderName = "gh-claude";
    private const string LegacyChatGptProviderName = "gh-chatgpt";
    private const string ClaudeProviderName = "gh-copilot-claude";
    private const string Gpt5ProviderName = "gh-copilot-gpt5";

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public string Provider { get; set; } = ClaudeProviderName;

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Model { get; set; }

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

        settings.Provider = NormalizeProvider(settings.Provider, settings.Model);
        settings.Model = null;

        if (settings.UpdatedAtUtc == default)
        {
            settings.UpdatedAtUtc = DateTimeOffset.UtcNow;
        }

        return settings;
    }

    public void Save(string projectRoot)
    {
        Directory.CreateDirectory(projectRoot);
        Provider = NormalizeProvider(Provider, Model);
        Model = null;
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

    public static string EnsureRuntimeDirectory(string projectRoot, params string[] segments)
    {
        var path = segments.Aggregate(Path.Combine(projectRoot, ".wallycode"), Path.Combine);
        Directory.CreateDirectory(path);
        return path;
    }

    private static string NormalizeProvider(string? providerName, string? legacyModel)
    {
        if (string.IsNullOrWhiteSpace(providerName))
        {
            return ClaudeProviderName;
        }

        var value = providerName.Trim();

        if (string.Equals(value, LegacySingleProviderName, StringComparison.OrdinalIgnoreCase))
        {
            return string.Equals(legacyModel, "gpt-5", StringComparison.OrdinalIgnoreCase)
                ? Gpt5ProviderName
                : ClaudeProviderName;
        }

        if (string.Equals(value, LegacyClaudeProviderName, StringComparison.OrdinalIgnoreCase))
        {
            return ClaudeProviderName;
        }

        if (string.Equals(value, LegacyChatGptProviderName, StringComparison.OrdinalIgnoreCase))
        {
            return Gpt5ProviderName;
        }

        return value;
    }
}