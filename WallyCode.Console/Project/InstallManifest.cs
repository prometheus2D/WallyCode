using System.Text.Json;

namespace WallyCode.ConsoleApp.Project;

internal sealed class InstallManifest
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public List<string> Files { get; set; } = [];

    public List<string> Directories { get; set; } = [];

    public static string GetFilePath(string installRoot)
    {
        return Path.Combine(installRoot, "wallycode.install.json");
    }

    public static InstallManifest? TryLoad(string installRoot)
    {
        var manifestPath = GetFilePath(installRoot);
        if (!File.Exists(manifestPath))
        {
            return null;
        }

        return JsonSerializer.Deserialize<InstallManifest>(File.ReadAllText(manifestPath), SerializerOptions)
            ?? new InstallManifest();
    }

    public static void Save(string installRoot, IEnumerable<string> filePaths, IEnumerable<string> directoryPaths)
    {
        var installRootPath = Path.GetFullPath(installRoot);
        var manifest = new InstallManifest
        {
            Files = [.. filePaths
                .Select(path => Path.GetRelativePath(installRootPath, Path.GetFullPath(path)))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)],
            Directories = [.. directoryPaths
                .Select(path => Path.GetRelativePath(installRootPath, Path.GetFullPath(path)))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)]
        };

        var manifestPath = GetFilePath(installRootPath);
        Directory.CreateDirectory(Path.GetDirectoryName(manifestPath)!);
        var json = JsonSerializer.Serialize(manifest, SerializerOptions);
        File.WriteAllText(manifestPath, json + Environment.NewLine);
    }

    public IReadOnlyList<string> ResolveFilePaths(string installRoot)
    {
        return [.. Files.Select(path => Path.GetFullPath(Path.Combine(installRoot, path)))];
    }

    public IReadOnlyList<string> ResolveDirectoryPaths(string installRoot)
    {
        return [.. Directories.Select(path => Path.GetFullPath(Path.Combine(installRoot, path)))];
    }
}
