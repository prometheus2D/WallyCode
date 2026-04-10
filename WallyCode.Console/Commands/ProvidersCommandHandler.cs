using WallyCode.ConsoleApp.Copilot;
using WallyCode.ConsoleApp.Project;

namespace WallyCode.ConsoleApp.Commands;

internal sealed class ProvidersCommandHandler
{
	private readonly ProviderRegistry _providerRegistry;

	public ProvidersCommandHandler(ProviderRegistry providerRegistry)
	{
		_providerRegistry = providerRegistry;
	}

	public async Task<int> ExecuteAsync(ProvidersCommandOptions commandOptions, CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();

		var projectRoot = ProjectSettings.ResolveProjectRoot(commandOptions.SourcePath);
		var settings = ProjectSettings.Load(projectRoot);
		var currentProvider = _providerRegistry.Get(settings.Provider);

		foreach (var provider in _providerRegistry.All)
		{
			var readinessError = await provider.GetReadinessErrorAsync(cancellationToken);
			var status = string.IsNullOrWhiteSpace(readinessError) ? "ready" : "unavailable";
			var activeMarker = string.Equals(provider.Name, currentProvider.Name, StringComparison.OrdinalIgnoreCase) ? " *" : string.Empty;
			var detail = string.IsNullOrWhiteSpace(readinessError) ? string.Empty : $" - {readinessError}";

			Console.WriteLine($"  {provider.Name} [{status}] model:{provider.DefaultModel}{activeMarker}{detail}");
		}

		return 0;
	}
}