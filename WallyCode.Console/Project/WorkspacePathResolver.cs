namespace WallyCode.ConsoleApp.Project;

internal static class WorkspacePathResolver
{
    public static string ResolveVsBuildWorkspaceRoot(string appDirectoryPath)
    {
        var normalizedAppDirectoryPath = Path.GetFullPath(appDirectoryPath);
        var buildProjectDirectory = FindBuildProjectDirectory(normalizedAppDirectoryPath)
            ?? throw new InvalidOperationException(
                $"--vs-build requires the app directory to be under bin{Path.DirectorySeparatorChar}Debug or bin{Path.DirectorySeparatorChar}Release. Current app directory: {normalizedAppDirectoryPath}");

        return FindTopmostWorkspaceMarker(buildProjectDirectory)
            ?? throw new InvalidOperationException(
                $"Could not find a workspace root above {buildProjectDirectory}. Looked for .git, *.sln, or wallycode.json.");
    }

    private static string? FindBuildProjectDirectory(string appDirectoryPath)
    {
        var trimmedPath = appDirectoryPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        for (var current = new DirectoryInfo(trimmedPath); current is not null; current = current.Parent)
        {
            if (current.Parent is null || current.Parent.Parent is null)
            {
                continue;
            }

            var isConfigurationDirectory = string.Equals(current.Name, "Debug", StringComparison.OrdinalIgnoreCase)
                || string.Equals(current.Name, "Release", StringComparison.OrdinalIgnoreCase);

            if (isConfigurationDirectory && string.Equals(current.Parent.Name, "bin", StringComparison.OrdinalIgnoreCase))
            {
                return current.Parent.Parent.FullName;
            }
        }

        return null;
    }

    private static string? FindTopmostWorkspaceMarker(string startDirectory)
    {
        string? candidate = null;

        for (var current = new DirectoryInfo(startDirectory); current is not null; current = current.Parent)
        {
            if (ContainsWorkspaceMarker(current.FullName))
            {
                candidate = current.FullName;
            }
        }

        return candidate;
    }

    private static bool ContainsWorkspaceMarker(string directoryPath)
    {
        return Directory.Exists(Path.Combine(directoryPath, ".git"))
            || File.Exists(Path.Combine(directoryPath, "wallycode.json"))
            || Directory.EnumerateFiles(directoryPath, "*.sln", SearchOption.TopDirectoryOnly).Any();
    }
}
