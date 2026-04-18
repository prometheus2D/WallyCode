namespace WallyCode.ConsoleApp.Commands;

internal sealed class TutorialDocument
{
    public required string Name { get; init; }
    public required string Title { get; init; }
    public required string Summary { get; init; }
    public required string Content { get; init; }
    public required string FilePath { get; init; }
}

internal static class TutorialCatalog
{
    public static string GetDefaultPath()
    {
        return Path.Combine(AppContext.BaseDirectory, "Tutorials");
    }

    public static IReadOnlyList<TutorialDocument> Load(string tutorialsPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tutorialsPath);

        if (!Directory.Exists(tutorialsPath))
        {
            return [];
        }

        return Directory
            .GetFiles(tutorialsPath, "*.md", SearchOption.TopDirectoryOnly)
            .Where(IsTutorialFile)
            .Select(ReadDocument)
            .OrderBy(tutorial => tutorial.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static bool IsTutorialFile(string filePath)
    {
        return !string.Equals(Path.GetFileName(filePath), "README.md", StringComparison.OrdinalIgnoreCase);
    }

    private static TutorialDocument ReadDocument(string filePath)
    {
        var content = File.ReadAllText(filePath);
        var name = Path.GetFileNameWithoutExtension(filePath);

        return new TutorialDocument
        {
            Name = name,
            Title = ExtractTitle(content, name),
            Summary = ExtractSummary(content, name),
            Content = content,
            FilePath = filePath
        };
    }

    private static string ExtractTitle(string content, string fallbackName)
    {
        foreach (var line in EnumerateLines(content))
        {
            var trimmed = line.Trim();

            if (trimmed.StartsWith("# ", StringComparison.Ordinal))
            {
                return trimmed[2..].Trim();
            }
        }

        return fallbackName;
    }

    private static string ExtractSummary(string content, string fallbackName)
    {
        var paragraphLines = new List<string>();
        var inCodeFence = false;

        foreach (var line in EnumerateLines(content))
        {
            var trimmed = line.Trim();

            if (trimmed.StartsWith("```", StringComparison.Ordinal))
            {
                inCodeFence = !inCodeFence;

                if (paragraphLines.Count > 0)
                {
                    break;
                }

                continue;
            }

            if (inCodeFence)
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(trimmed))
            {
                if (paragraphLines.Count > 0)
                {
                    break;
                }

                continue;
            }

            if (trimmed.StartsWith("#", StringComparison.Ordinal))
            {
                if (paragraphLines.Count > 0)
                {
                    break;
                }

                continue;
            }

            if (trimmed.StartsWith("<!--", StringComparison.Ordinal))
            {
                continue;
            }

            paragraphLines.Add(trimmed);
        }

        return paragraphLines.Count == 0
            ? $"Open tutorial {fallbackName} for details."
            : string.Join(" ", paragraphLines);
    }

    private static IEnumerable<string> EnumerateLines(string content)
    {
        return content.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');
    }
}