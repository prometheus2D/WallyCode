using System.Text;
using WallyCode.ConsoleApp.Copilot;
using WallyCode.ConsoleApp.Project;
using WallyCode.ConsoleApp.Runtime;

namespace WallyCode.ConsoleApp.Commands;

internal sealed class PromptCommandHandler
{
    private static readonly UTF8Encoding Utf8NoBom = new(false);

    private readonly ProviderRegistry _providerRegistry;
    private readonly AppLogger _logger;

    public PromptCommandHandler(ProviderRegistry providerRegistry, AppLogger logger)
    {
        _providerRegistry = providerRegistry;
        _logger = logger;
    }

    public async Task<int> ExecuteAsync(PromptCommandOptions commandOptions, CancellationToken cancellationToken)
    {
        var projectRoot = ProjectSettings.ResolveProjectRoot(commandOptions.SourcePath);
        var settings = ProjectSettings.Load(projectRoot);
        var providerName = string.IsNullOrWhiteSpace(commandOptions.Provider)
            ? settings.Provider
            : commandOptions.Provider.Trim();
        var provider = _providerRegistry.Get(providerName);
        var resolvedModel = string.IsNullOrWhiteSpace(commandOptions.Model)
            ? (string.IsNullOrWhiteSpace(settings.Model) ? provider.DefaultModel : settings.Model)
            : commandOptions.Model.Trim();
        var timestamp = DateTimeOffset.Now;
        var logDirectoryPath = ProjectSettings.EnsureRuntimeDirectory(projectRoot, "logs");
        var promptDirectoryPath = ProjectSettings.EnsureRuntimeDirectory(projectRoot, "prompts");
        var rawDirectoryPath = ProjectSettings.EnsureRuntimeDirectory(projectRoot, "raw");
        var runtimeRootPath = Path.Combine(projectRoot, ".wallycode");

        _logger.LogFilePath = Path.Combine(logDirectoryPath, $"prompt-{timestamp:yyyyMMdd-HHmmss}.log");
        _logger.Section("WallyCode Prompt");
        _logger.Info($"Initialized source: {projectRoot}");
        _logger.Info($"Initialized runtime workspace: {runtimeRootPath}");
        _logger.Info($"Provider: {provider.Name}");
        _logger.Info($"Model: {resolvedModel}");
        _logger.Info($"Project root: {projectRoot}");
        await provider.EnsureReadyAsync(cancellationToken);

        var prompt = commandOptions.Prompt.Trim();
        var promptPath = Path.Combine(promptDirectoryPath, $"prompt-{timestamp:yyyyMMdd-HHmmss}.txt");
        File.WriteAllText(promptPath, prompt + Environment.NewLine, Utf8NoBom);

        var response = await provider.ExecuteAsync(
            new CopilotRequest
            {
                Prompt = prompt,
                Model = resolvedModel,
                SourcePath = projectRoot
            },
            cancellationToken);

        var rawOutputPath = Path.Combine(rawDirectoryPath, $"prompt-{timestamp:yyyyMMdd-HHmmss}.txt");
        File.WriteAllText(rawOutputPath, response.Trim() + Environment.NewLine, Utf8NoBom);

        _logger.Success("Prompt complete.");
        Console.WriteLine();
        Console.WriteLine(response.Trim());
        return 0;
    }
}