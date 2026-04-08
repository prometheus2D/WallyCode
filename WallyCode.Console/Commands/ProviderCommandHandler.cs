using WallyCode.ConsoleApp.Copilot;
using WallyCode.ConsoleApp.Project;
using WallyCode.ConsoleApp.Runtime;

namespace WallyCode.ConsoleApp.Commands;

internal sealed class ProviderCommandHandler
{
    private readonly ProviderRegistry _registry;
    private readonly AppLogger _logger;

    public ProviderCommandHandler(ProviderRegistry registry, AppLogger logger)
    {
        _registry = registry;
        _logger = logger;
    }

    public async Task<int> ExecuteAsync(ProviderCommandOptions options, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            if (options.Set)
            {
                return SetProvider(options);
            }

            if (options.Check || options.Install)
            {
                return await DiagnoseAsync(options, cancellationToken);
            }

            return await ListAsync(options, cancellationToken);
        }
        catch (Exception exception)
        {
            _logger.Error(exception.Message);
            return 1;
        }
    }

    private int SetProvider(ProviderCommandOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.Name))
        {
            _logger.Error("Provider name required. Usage: provider <name> --set");
            return 1;
        }

        var projectRoot = ProjectSettings.ResolveProjectRoot(options.SourcePath);
        var settings = ProjectSettings.Load(projectRoot);
        var provider = _registry.Get(options.Name);

        settings.Provider = provider.Name;
        settings.Save(projectRoot);

        _logger.Success($"{provider.Name} (model: {provider.DefaultModel})");
        return 0;
    }

    private async Task<int> DiagnoseAsync(ProviderCommandOptions options, CancellationToken cancellationToken)
    {
        var keys = ResolveInstallerKeys(options.Name);
        var failed = false;

        foreach (var key in keys)
        {
            var installer = _registry.GetInstaller(key);

            if (installer is null)
            {
                _logger.Error($"{key}: no installer registered");
                failed = true;
                continue;
            }

            var providers = _registry.All
                .Where(p => string.Equals(p.InstallerKey, key, StringComparison.OrdinalIgnoreCase))
                .Select(p => p.Name);

            _logger.Info($"{key} ({string.Join(", ", providers)})");

            var diagnostic = options.Install
                ? await installer.InstallAsync(cancellationToken)
                : await installer.DiagnoseAsync(cancellationToken);

            foreach (var step in diagnostic.Steps)
            {
                var mark = step.Passed ? "?" : "?";
                var detail = step.Detail is not null ? $" — {step.Detail}" : "";
                var line = $"  {mark} {step.Name}{detail}";

                if (step.Passed)
                    _logger.Success(line);
                else
                    _logger.Error(line);
            }

            if (!diagnostic.Ok)
            {
                failed = true;
            }
        }

        return failed ? 1 : 0;
    }

    private async Task<int> ListAsync(ProviderCommandOptions options, CancellationToken cancellationToken)
    {
        var projectRoot = ProjectSettings.ResolveProjectRoot(options.SourcePath);
        var settings = ProjectSettings.Load(projectRoot);
        var current = _registry.Get(settings.Provider);

        foreach (var provider in _registry.All)
        {
            var readinessError = await provider.GetReadinessErrorAsync(cancellationToken);
            var status = string.IsNullOrWhiteSpace(readinessError) ? "ready" : "unavailable";
            var active = string.Equals(provider.Name, current.Name, StringComparison.OrdinalIgnoreCase) ? " *" : "";

            Console.WriteLine($"  {provider.Name} [{status}] model:{provider.DefaultModel}{active}");
        }

        return 0;
    }

    private IReadOnlyList<string> ResolveInstallerKeys(string? providerFilter)
    {
        if (!string.IsNullOrWhiteSpace(providerFilter))
        {
            var provider = _registry.Get(providerFilter);
            return [provider.InstallerKey];
        }

        return _registry.InstallerKeys;
    }
}
