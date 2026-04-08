using WallyCode.ConsoleApp.Runtime;

namespace WallyCode.ConsoleApp.Copilot;

internal sealed class GhCopilotInstaller : IProviderInstaller
{
    private readonly AppLogger _logger;

    public GhCopilotInstaller(AppLogger logger)
    {
        _logger = logger;
    }

    public string Key => "gh-copilot";

    public async Task<ProviderDiagnostic> DiagnoseAsync(CancellationToken cancellationToken)
    {
        var steps = new List<ProviderCheckStep>();

        var gh = await Probe(["--version"], cancellationToken);
        if (!gh.Success)
        {
            steps.Add(ProviderCheckStep.Fail("gh", "not found on PATH — https://cli.github.com/"));
            return new ProviderDiagnostic(false, steps);
        }
        steps.Add(ProviderCheckStep.Pass("gh", gh.StandardOutput.Split('\n')[0]));

        var copilot = await Probe(["copilot", "--help"], cancellationToken);
        if (!copilot.Success)
        {
            steps.Add(ProviderCheckStep.Fail("gh copilot", "extension missing — run: gh extension install github/gh-copilot"));
            return new ProviderDiagnostic(false, steps);
        }
        steps.Add(ProviderCheckStep.Pass("gh copilot"));

        var auth = await Probe(["auth", "status"], cancellationToken);
        if (!auth.Success)
        {
            steps.Add(ProviderCheckStep.Fail("gh auth", "not authenticated — run: gh auth login"));
            return new ProviderDiagnostic(false, steps);
        }
        steps.Add(ProviderCheckStep.Pass("gh auth"));

        return new ProviderDiagnostic(true, steps);
    }

    public async Task<ProviderDiagnostic> InstallAsync(CancellationToken cancellationToken)
    {
        var steps = new List<ProviderCheckStep>();

        var gh = await Probe(["--version"], cancellationToken);
        if (!gh.Success)
        {
            steps.Add(ProviderCheckStep.Fail("gh", "not found on PATH — install manually from https://cli.github.com/"));
            return new ProviderDiagnostic(false, steps);
        }
        steps.Add(ProviderCheckStep.Pass("gh", gh.StandardOutput.Split('\n')[0]));

        var copilot = await Probe(["copilot", "--help"], cancellationToken);
        if (!copilot.Success)
        {
            _logger.Info("Installing gh copilot extension...");
            var install = await GhProcess.RunAsync(["extension", "install", "github/gh-copilot"], cancellationToken);
            if (!install.Success)
            {
                steps.Add(ProviderCheckStep.Fail("gh copilot", $"install failed — {GhProcess.ErrorDetail(install)}"));
                return new ProviderDiagnostic(false, steps);
            }
            steps.Add(ProviderCheckStep.Pass("gh copilot", "installed"));
        }
        else
        {
            steps.Add(ProviderCheckStep.Pass("gh copilot", "already installed"));
        }

        var auth = await Probe(["auth", "status"], cancellationToken);
        if (!auth.Success)
        {
            steps.Add(ProviderCheckStep.Fail("gh auth", "not authenticated — run: gh auth login"));
            return new ProviderDiagnostic(false, steps);
        }
        steps.Add(ProviderCheckStep.Pass("gh auth"));

        return new ProviderDiagnostic(true, steps);
    }

    private static async Task<GhProcess.GhResult> Probe(IReadOnlyList<string> arguments, CancellationToken cancellationToken)
    {
        try
        {
            return await GhProcess.RunAsync(arguments, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return new GhProcess.GhResult(-1, string.Empty, exception.Message);
        }
    }
}
