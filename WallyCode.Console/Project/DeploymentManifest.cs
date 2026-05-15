using System.Text.Json;

namespace WallyCode.ConsoleApp.Project;

internal sealed class DeploymentManifest
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public List<string> Files { get; set; } = [];

    public List<string> Directories { get; set; } = [];

    public static string GetFilePath(string projectRoot)
    {
        return Path.Combine(ProjectSettings.ResolveRuntimeRoot(projectRoot), "deploy-manifest.json");
    }

    public static DeploymentManifest? TryLoad(string projectRoot)
    {
        var manifestPath = GetFilePath(projectRoot);
        if (!File.Exists(manifestPath))
        {
            return null;
        }

        return JsonSerializer.Deserialize<DeploymentManifest>(File.ReadAllText(manifestPath), SerializerOptions)
            ?? new DeploymentManifest();
    }

    public static void Save(string projectRoot, IEnumerable<string> filePaths, IEnumerable<string> directoryPaths)
    {
        var projectRootPath = Path.GetFullPath(projectRoot);
        var manifest = new DeploymentManifest
        {
            Files = [.. filePaths
                .Select(path => Path.GetRelativePath(projectRootPath, Path.GetFullPath(path)))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)],
            Directories = [.. directoryPaths
                .Select(path => Path.GetRelativePath(projectRootPath, Path.GetFullPath(path)))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)]
        };

        var manifestPath = GetFilePath(projectRootPath);
        Directory.CreateDirectory(Path.GetDirectoryName(manifestPath)!);
        var json = JsonSerializer.Serialize(manifest, SerializerOptions);
        File.WriteAllText(manifestPath, json + Environment.NewLine);
    }

    public IReadOnlyList<string> ResolveFilePaths(string projectRoot)
    {
        return [.. Files.Select(path => Path.GetFullPath(Path.Combine(projectRoot, path)))];
    }

    public IReadOnlyList<string> ResolveDirectoryPaths(string projectRoot)
    {
        return [.. Directories.Select(path => Path.GetFullPath(Path.Combine(projectRoot, path)))];
    }
}