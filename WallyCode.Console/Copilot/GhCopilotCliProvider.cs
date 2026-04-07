using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using WallyCode.ConsoleApp.Runtime;

namespace WallyCode.ConsoleApp.Copilot;

internal sealed class GhCopilotCliProvider : ILlmProvider
{
    private readonly AppLogger _logger;
    private static readonly Lazy<string> GhExecutablePath = new(ResolveGhExecutablePath);

    public GhCopilotCliProvider(string name, string defaultModel, string description, AppLogger logger)
    {
        Name = name;
        DefaultModel = defaultModel;
        Description = description;
        _logger = logger;
    }

    public string Name { get; }

    public string Description { get; }

    public string DefaultModel { get; }

    public async Task<string?> GetReadinessErrorAsync(CancellationToken cancellationToken)
    {
        var ghError = await CheckCommandAsync(
            ["--version"],
            $"{Name} requires GitHub CLI (`gh`) to be installed and available on PATH.",
            cancellationToken);

        if (ghError is not null)
        {
            return ghError;
        }

        var copilotError = await CheckCommandAsync(
            ["copilot", "--help"],
            $"{Name} requires the `gh copilot` command to be available.",
            cancellationToken);

        if (copilotError is not null)
        {
            return copilotError;
        }

        return await CheckCommandAsync(
            ["auth", "status"],
            $"{Name} requires GitHub CLI authentication. Run `gh auth login`.",
            cancellationToken);
    }

    public async Task<string> ExecuteAsync(CopilotRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.Prompt))
        {
            throw new ArgumentException("Prompt is required.", nameof(request));
        }

        var model = string.IsNullOrWhiteSpace(request.Model)
            ? DefaultModel
            : request.Model.Trim();
        var arguments = new List<string>
        {
            "copilot",
            "--model",
            model
        };

        string? workingDirectory = null;

        if (!string.IsNullOrWhiteSpace(request.SourcePath))
        {
            var sourcePath = Path.GetFullPath(request.SourcePath);

            if (!Directory.Exists(sourcePath))
            {
                throw new DirectoryNotFoundException($"Source path does not exist: {sourcePath}");
            }

            arguments.Add("--add-dir");
            arguments.Add(sourcePath);
            workingDirectory = sourcePath;
        }

        arguments.Add("--yolo");
        arguments.Add("-s");
        arguments.Add("-p");
        arguments.Add(request.Prompt);

        _logger.Info($"Launching {Name} with model {model}.");
        var result = await RunGhAsync(arguments, workingDirectory, cancellationToken);

        if (result.ExitCode != 0)
        {
            var errorDetail = GetErrorDetail(result.StandardError, result.StandardOutput);
            throw new InvalidOperationException(
                string.IsNullOrWhiteSpace(errorDetail)
                    ? $"gh copilot exited with code {result.ExitCode}."
                    : errorDetail);
        }

        return result.StandardOutput;
    }

    private async Task<string?> CheckCommandAsync(
        IReadOnlyList<string> arguments,
        string failureMessage,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await RunGhAsync(arguments, workingDirectory: null, cancellationToken);

            if (result.ExitCode == 0)
            {
                return null;
            }

            var errorDetail = GetErrorDetail(result.StandardError, result.StandardOutput);
            return string.IsNullOrWhiteSpace(errorDetail)
                ? failureMessage
                : $"{failureMessage} {errorDetail}";
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            return $"{failureMessage} {exception.Message}";
        }
    }

    private static async Task<GhCommandResult> RunGhAsync(
        IReadOnlyList<string> arguments,
        string? workingDirectory,
        CancellationToken cancellationToken)
    {
        var startInfo = CreateStartInfo(arguments, workingDirectory);

        using var process = new Process
        {
            StartInfo = startInfo
        };

        if (!process.Start())
        {
            throw new InvalidOperationException("Failed to start gh.");
        }

        process.StandardInput.Close();

        var stdoutTask = process.StandardOutput.ReadToEndAsync();
        var stderrTask = process.StandardError.ReadToEndAsync();

        using var registration = cancellationToken.Register(() => TryKillProcessTree(process));

        try
        {
            await process.WaitForExitAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            TryKillProcessTree(process);
            await Task.WhenAll(stdoutTask, stderrTask);
            throw;
        }

        await Task.WhenAll(stdoutTask, stderrTask);

        return new GhCommandResult(
            process.ExitCode,
            stdoutTask.Result.Trim(),
            stderrTask.Result.Trim());
    }

    private static ProcessStartInfo CreateStartInfo(IReadOnlyList<string> arguments, string? workingDirectory)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = GhExecutablePath.Value,
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

        return startInfo;
    }

    private static string ResolveGhExecutablePath()
    {
        var commandName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "where" : "which";
        var executableName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "gh.exe" : "gh";

        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = commandName,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            };
            startInfo.ArgumentList.Add(executableName);

            using var process = new Process { StartInfo = startInfo };

            if (!process.Start())
            {
                throw new InvalidOperationException("Failed to locate gh.");
            }

            var standardOutput = process.StandardOutput.ReadToEnd();
            var standardError = process.StandardError.ReadToEnd();
            process.WaitForExit();

            if (process.ExitCode != 0)
            {
                var detail = string.IsNullOrWhiteSpace(standardError) ? standardOutput : standardError;
                throw new InvalidOperationException(string.IsNullOrWhiteSpace(detail) ? "Failed to locate gh." : detail.Trim());
            }

            var path = standardOutput
                .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .FirstOrDefault();

            if (string.IsNullOrWhiteSpace(path))
            {
                throw new InvalidOperationException("Failed to locate gh.");
            }

            return path;
        }
        catch (Win32Exception)
        {
            return executableName;
        }
    }

    private static string GetErrorDetail(string standardError, string standardOutput)
    {
        return string.IsNullOrWhiteSpace(standardError) ? standardOutput : standardError;
    }

    private static void TryKillProcessTree(Process process)
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

    private sealed record GhCommandResult(int ExitCode, string StandardOutput, string StandardError);
}