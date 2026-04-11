using System.Text;
using WallyCode.ConsoleApp.Copilot;
using WallyCode.ConsoleApp.Project;
using WallyCode.ConsoleApp.Runtime;

namespace WallyCode.ConsoleApp.Commands;

internal sealed class TestProviderCommandHandler
{
	private static readonly UTF8Encoding Utf8NoBom = new(false);
	private const string SmokeTestPrompt = "Reply with a short single-line confirmation that includes the exact text: WallyCode provider test OK.";

	private readonly ProviderRegistry _providerRegistry;
	private readonly AppLogger _logger;

	public TestProviderCommandHandler(ProviderRegistry providerRegistry, AppLogger logger)
	{
		_providerRegistry = providerRegistry;
		_logger = logger;
	}

	public async Task<int> ExecuteAsync(TestProviderCommandOptions commandOptions, CancellationToken cancellationToken)
	{
		try
		{
			var projectRoot = ProjectSettings.ResolveProjectRoot(commandOptions.SourcePath);
			var settings = ProjectSettings.Load(projectRoot);
			var providerName = string.IsNullOrWhiteSpace(commandOptions.Provider)
				? settings.Provider
				: commandOptions.Provider.Trim();
			var provider = _providerRegistry.Get(providerName);
			var resolvedModel = string.IsNullOrWhiteSpace(commandOptions.Model)
				? provider.DefaultModel
				: commandOptions.Model.Trim();
			var timestamp = DateTimeOffset.Now;
			var logDirectoryPath = ProjectSettings.EnsureRuntimeDirectory(projectRoot, "logs");
			var promptDirectoryPath = ProjectSettings.EnsureRuntimeDirectory(projectRoot, "prompts");
			var rawDirectoryPath = ProjectSettings.EnsureRuntimeDirectory(projectRoot, "raw");

			_logger.LogFilePath = Path.Combine(logDirectoryPath, $"provider-test-{timestamp:yyyyMMdd-HHmmss}.log");
			_logger.Section("WallyCode Provider Test");
			_logger.Info($"Provider: {provider.Name}");
			_logger.Info($"Model: {resolvedModel}");
			_logger.Info($"Project root: {projectRoot}");
			await provider.EnsureReadyAsync(cancellationToken);

			var promptPath = Path.Combine(promptDirectoryPath, $"provider-test-{timestamp:yyyyMMdd-HHmmss}.txt");
			File.WriteAllText(promptPath, SmokeTestPrompt + Environment.NewLine, Utf8NoBom);

			var response = await provider.ExecuteAsync(
				new CopilotRequest
				{
					Prompt = SmokeTestPrompt,
					Model = resolvedModel,
					SourcePath = projectRoot
				},
				cancellationToken);

			var trimmedResponse = response.Trim();

			if (string.IsNullOrWhiteSpace(trimmedResponse))
			{
				throw new InvalidOperationException("Provider returned empty output during the smoke test.");
			}

			var rawOutputPath = Path.Combine(rawDirectoryPath, $"provider-test-{timestamp:yyyyMMdd-HHmmss}.txt");
			File.WriteAllText(rawOutputPath, trimmedResponse + Environment.NewLine, Utf8NoBom);

			_logger.Success("Provider test complete.");
			Console.WriteLine();
			Console.WriteLine(trimmedResponse);
			return 0;
		}
		catch (OperationCanceledException)
		{
			_logger.Warning("Provider test cancelled.");
			return 2;
		}
		catch (Exception exception)
		{
			_logger.Error(exception.ToString());
			return 1;
		}
	}
}