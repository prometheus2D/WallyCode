using Xunit;

namespace WallyCode.ConsoleApp.Tests.Infrastructure;

public sealed class ProviderTestPolicyTests
{
    [Fact]
    public void TestsDoNotUseRealProviderRegistryOrCopilotProvider()
    {
        var testsRoot = FindTestsRoot();
        var forbiddenPatterns = new[]
        {
            "ProviderRegistry." + "Create",
            "new " + "GhCopilotCliProvider",
            "ProviderDefinition." + "LoadAll"
        };

        var violations = Directory
            .EnumerateFiles(testsRoot, "*.cs", SearchOption.AllDirectories)
            .Where(path => !string.Equals(Path.GetFileName(path), nameof(ProviderTestPolicyTests) + ".cs", StringComparison.Ordinal))
            .SelectMany(path => forbiddenPatterns
                .Where(pattern => File.ReadAllText(path).Contains(pattern, StringComparison.Ordinal))
                .Select(pattern => $"{Path.GetRelativePath(testsRoot, path)} contains {pattern}"))
            .ToList();

        Assert.True(violations.Count == 0, string.Join(Environment.NewLine, violations));
    }

    private static string FindTestsRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, "WallyCode.Console.Tests");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Could not find WallyCode.Console.Tests source directory.");
    }
}