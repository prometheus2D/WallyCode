using WallyCode.ConsoleApp.Copilot;
using WallyCode.ConsoleApp.Project;
using WallyCode.ConsoleApp.Runtime;

namespace WallyCode.ConsoleApp.Commands;

internal sealed class SetProviderCommandHandler
{
	private readonly ProviderRegistry _providerRegistry;
	private readonly AppLogger _logger;

	public SetProviderCommandHandler(ProviderRegistry providerRegistry, AppLogger logger)
	{
		_providerRegistry = providerRegistry;
		_logger = logger;
	}

	public Task<int> ExecuteAsync(SetProviderCommandOptions commandOptions, CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();

		var projectRoot = ProjectSettings.ResolveProjectRoot(commandOptions.SourcePath);
		var settings = ProjectSettings.Load(projectRoot);
		var provider = _providerRegistry.Get(commandOptions.Name);

		settings.Provider = provider.Name;
		settings.Save(projectRoot);

		_logger.Success($"Saved default provider: {provider.Name} (model: {provider.DefaultModel})");
		return Task.FromResult(0);
	}
}