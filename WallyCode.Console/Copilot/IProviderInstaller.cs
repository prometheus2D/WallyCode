namespace WallyCode.ConsoleApp.Copilot;

internal interface IProviderInstaller
{
    string Key { get; }

    Task<ProviderDiagnostic> DiagnoseAsync(CancellationToken cancellationToken);

    Task<ProviderDiagnostic> InstallAsync(CancellationToken cancellationToken);
}

internal sealed record ProviderDiagnostic(bool Ok, IReadOnlyList<ProviderCheckStep> Steps);

internal sealed record ProviderCheckStep(string Name, bool Passed, string? Detail = null)
{
    public static ProviderCheckStep Pass(string name, string? detail = null) => new(name, true, detail);
    public static ProviderCheckStep Fail(string name, string detail) => new(name, false, detail);
}
