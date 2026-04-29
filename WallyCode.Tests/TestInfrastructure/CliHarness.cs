using WallyCode.ConsoleApp;
using WallyCode.ConsoleApp.Copilot;

namespace WallyCode.Tests.TestInfrastructure;

/// <summary>
/// Harness for end-to-end tests that drive the real CLI argv pipeline (<see cref="Program.RunAsync"/>)
/// against a temp workspace with a scripted <see cref="MockLlmProvider"/>.
///
/// The harness:
///   * Pins <see cref="Environment.CurrentDirectory"/> to the workspace for the lifetime of the harness.
///   * Builds a real <see cref="ProviderRegistry"/> containing only the supplied <see cref="MockLlmProvider"/>
///     and passes it into <see cref="Program.RunAsync"/> via its optional <c>providerRegistry</c> parameter
///     so the CLI does not shell out to <c>gh copilot</c>.
///   * Captures stdout per invocation so tests can assert on user-visible output.
///   * Restores <see cref="Environment.CurrentDirectory"/> on dispose.
///
/// This type is part of the "Console" xUnit collection because it manipulates process-global state
/// (<see cref="Console.Out"/> and <see cref="Environment.CurrentDirectory"/>).
/// </summary>
internal sealed class CliHarness : IDisposable
{
    private readonly string _originalDirectory;
    private readonly ProviderRegistry _registry;

    public string WorkspaceRoot { get; }
    public MockLlmProvider Provider { get; }

    private CliHarness(string workspaceRoot, MockLlmProvider provider, ProviderRegistry registry, string originalDirectory)
    {
        WorkspaceRoot = workspaceRoot;
        Provider = provider;
        _registry = registry;
        _originalDirectory = originalDirectory;
    }

    public static CliHarness Create(string workspaceRoot, MockLlmProvider provider)
    {
        var registry = new ProviderRegistry([provider]);
        var originalDirectory = Environment.CurrentDirectory;
        Environment.CurrentDirectory = workspaceRoot;
        return new CliHarness(workspaceRoot, provider, registry, originalDirectory);
    }

    public async Task<CliRunResult> InvokeAsync(params string[] args)
    {
        var writer = new StringWriter();
        var originalOut = Console.Out;

        try
        {
            Console.SetOut(writer);
            var exitCode = await Program.RunAsync(args, CancellationToken.None, appDirectoryPath: null, providerRegistry: _registry);
            return new CliRunResult(exitCode, writer.ToString(), args);
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    public void Dispose()
    {
        Environment.CurrentDirectory = _originalDirectory;
    }
}

internal sealed record CliRunResult(int ExitCode, string Output, IReadOnlyList<string> Args)
{
    public void AssertSucceeded()
    {
        if (ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"CLI invocation `wallycode {string.Join(' ', Args)}` exited with code {ExitCode}. Output:{Environment.NewLine}{Output}");
        }
    }
}
