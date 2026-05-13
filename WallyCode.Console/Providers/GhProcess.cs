using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace WallyCode.ConsoleApp.Copilot;

internal static class GhProcess
{
    internal static readonly string Executable = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "gh.exe" : "gh";
    private static readonly string[] WindowsCopilotExecutableNames = ["copilot.exe", "copilot.cmd", "copilot.bat"];
    private static readonly string[] UnixCopilotExecutableNames = ["copilot"];
    private static readonly string? ManagedCopilotExecutablePath = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
        ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "GitHub CLI", "copilot", "copilot.exe")
        : null;

    internal static async Task<GhResult> RunAsync(
        IReadOnlyList<string> arguments,
        string? workingDirectory,
        CancellationToken cancellationToken) =>
        await RunAsync(Executable, arguments, workingDirectory, cancellationToken);

    internal static async Task<GhResult> RunCopilotAsync(
        IReadOnlyList<string> arguments,
        string? workingDirectory,
        CancellationToken cancellationToken)
    {
        var executablePath = TryResolveCopilotExecutablePath();

        if (string.IsNullOrWhiteSpace(executablePath))
        {
            throw new FileNotFoundException("GitHub Copilot CLI executable was not found.");
        }

        return await RunAsync(executablePath, arguments, workingDirectory, cancellationToken);
    }

    internal static string? TryResolveCopilotExecutablePath()
    {
        if (!string.IsNullOrWhiteSpace(ManagedCopilotExecutablePath) && File.Exists(ManagedCopilotExecutablePath))
        {
            return ManagedCopilotExecutablePath;
        }

        var candidateNames = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? WindowsCopilotExecutableNames
            : UnixCopilotExecutableNames;

        return TryResolveExecutablePath(candidateNames);
    }

    private static async Task<GhResult> RunAsync(
        string executablePath,
        IReadOnlyList<string> arguments,
        string? workingDirectory,
        CancellationToken cancellationToken)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = executablePath,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = true,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };

        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        if (!string.IsNullOrWhiteSpace(workingDirectory))
        {
            startInfo.WorkingDirectory = workingDirectory;
        }

        using var process = new Process { StartInfo = startInfo };

        if (!process.Start())
        {
            throw new InvalidOperationException("Failed to start gh.");
        }

        process.StandardInput.Close();

        var stdoutTask = process.StandardOutput.ReadToEndAsync();
        var stderrTask = process.StandardError.ReadToEndAsync();

        using var registration = cancellationToken.Register(() => TryKill(process));

        try
        {
            await process.WaitForExitAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            TryKill(process);
            await Task.WhenAll(stdoutTask, stderrTask);
            throw;
        }

        await Task.WhenAll(stdoutTask, stderrTask);

        return new GhResult(
            process.ExitCode,
            stdoutTask.Result.Trim(),
            stderrTask.Result.Trim());
    }

    internal static Task<GhResult> RunAsync(
        IReadOnlyList<string> arguments,
        CancellationToken cancellationToken) =>
        RunAsync(arguments, workingDirectory: null, cancellationToken);

    internal static Task<GhResult> RunCopilotAsync(
        IReadOnlyList<string> arguments,
        CancellationToken cancellationToken) =>
        RunCopilotAsync(arguments, workingDirectory: null, cancellationToken);

    internal static string ErrorDetail(GhResult result) =>
        string.IsNullOrWhiteSpace(result.StandardError) ? result.StandardOutput : result.StandardError;

    private static string? TryResolveExecutablePath(IReadOnlyList<string> candidateNames)
    {
        var pathValue = Environment.GetEnvironmentVariable("PATH");

        if (string.IsNullOrWhiteSpace(pathValue))
        {
            return null;
        }

        foreach (var directory in pathValue.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var normalizedDirectory = directory.Trim().Trim('"');

            if (string.IsNullOrWhiteSpace(normalizedDirectory))
            {
                continue;
            }

            foreach (var candidateName in candidateNames)
            {
                var candidatePath = Path.Combine(normalizedDirectory, candidateName);

                if (File.Exists(candidatePath))
                {
                    return candidatePath;
                }
            }
        }

        return null;
    }

    private static void TryKill(Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
        }
        catch (InvalidOperationException)
        {
        }
        catch (NotSupportedException)
        {
        }
    }

    internal sealed record GhResult(int ExitCode, string StandardOutput, string StandardError)
    {
        public bool Success => ExitCode == 0;
    }
}