using CommandLine;
using WallyCode.ConsoleApp.Commands;
using WallyCode.ConsoleApp.Copilot;
using WallyCode.ConsoleApp.Project;
using WallyCode.ConsoleApp.Runtime;

namespace WallyCode.ConsoleApp;

internal static class Program
{
	private static async Task<int> Main(string[] args)
	{
		using var cancellationTokenSource = new CancellationTokenSource();
		Console.CancelKeyPress += (_, eventArgs) =>
		{
			eventArgs.Cancel = true;
			cancellationTokenSource.Cancel();
		};

		return await RunAsync(args, cancellationTokenSource.Token);
	}

	internal static async Task<int> RunAsync(
		string[] args,
		CancellationToken cancellationToken,
		string? appDirectoryPath = null,
		ProviderRegistry? providerRegistry = null)
	{
		var logger = new AppLogger();
		providerRegistry ??= ProviderRegistry.Create(logger);
		ConfigureInvocationLogging(args, logger);

		var parser = new Parser(settings =>
		{
			settings.CaseSensitive = false;
			settings.CaseInsensitiveEnumValues = true;
			settings.HelpWriter = Console.Out;
			settings.AutoHelp = true;
			settings.AutoVersion = true;
		});

		var result = parser.ParseArguments<RunCommandOptions, StepCommandOptions, ResumeCommandOptions, AskCommandOptions, ActCommandOptions, ProviderCommandOptions, RespondCommandOptions, RecoverCommandOptions, LoggingCommandOptions, ShellCommandOptions, SetupCommandOptions>(args);

		try
		{
			return await result.MapResult(
				(RunCommandOptions options) => new WorkflowRunCommandHandler(providerRegistry, logger).ExecuteAsync(options, cancellationToken),
				(StepCommandOptions options) => new StepCommandHandler(providerRegistry, logger).ExecuteAsync(options, cancellationToken),
				(ResumeCommandOptions options) => new ResumeCommandHandler(new WorkflowRunCommandHandler(providerRegistry, logger)).ExecuteAsync(options, cancellationToken),
				(AskCommandOptions options) => new WorkflowRunCommandHandler(providerRegistry, logger).ExecuteAsync(options.ToRunOptions(), cancellationToken),
				(ActCommandOptions options) => new WorkflowRunCommandHandler(providerRegistry, logger).ExecuteAsync(options.ToRunOptions(), cancellationToken),
				(ProviderCommandOptions options) => new ProviderCommandHandler(providerRegistry, logger).ExecuteAsync(options, cancellationToken),
				(RespondCommandOptions options) => new RespondCommandHandler(new WorkflowRunCommandHandler(providerRegistry, logger), logger).ExecuteAsync(options, cancellationToken),
				(RecoverCommandOptions options) => new RecoverCommandHandler(new WorkflowRunCommandHandler(providerRegistry, logger), logger).ExecuteAsync(options, cancellationToken),
				(LoggingCommandOptions options) => new LoggingCommandHandler(logger).ExecuteAsync(options, cancellationToken),
				(ShellCommandOptions options) => new ShellCommandHandler(options, appDirectoryPath).ExecuteAsync(cancellationToken),
				(SetupCommandOptions options) => new SetupCommandHandler(providerRegistry, logger, appDirectoryPath).ExecuteAsync(options, cancellationToken),
				errors => Task.FromResult(errors.All(e =>
					e.Tag == ErrorType.HelpRequestedError
					|| e.Tag == ErrorType.HelpVerbRequestedError
					|| e.Tag == ErrorType.VersionRequestedError)
					? 0
					: 1));
		}
		catch (OperationCanceledException)
		{
			logger.Warning("Cancelled.");
			logger.LogAction("Invocation cancelled", "OperationCanceledException observed.");
			return 2;
		}
		catch (Exception exception)
		{
			logger.Error(exception.Message);
			logger.LogAction("Invocation failed", exception.ToString());
			return 1;
		}
	}

	private static void ConfigureInvocationLogging(string[] args, AppLogger logger)
	{
		if (args.Length == 0)
		{
			return;
		}

		var commandName = args[0];
		if (!SupportsInvocationLogging(commandName))
		{
			return;
		}

		var loggingEnabled = HasOption(args, "log");
		var verboseLogging = HasOption(args, "verbose");
		if (!loggingEnabled)
		{
			return;
		}

		var sourcePath = TryGetOptionValue(args, "source");
		var projectRoot = ProjectSettings.ResolveProjectRoot(sourcePath);
		var memoryRoot = TryGetOptionValue(args, "memory-root");
		var runtimeRoot = ProjectSettings.ResolveRuntimeRoot(projectRoot, memoryRoot);
		logger.ConfigureLogging(runtimeRoot, new LoggingMode
		{
			Enabled = true,
			Verbose = verboseLogging
		});
		logger.LogCommand(commandName, args.Skip(1));
		logger.LogAction("Invocation logging", $"runtimeRoot={runtimeRoot}; verbose={verboseLogging}");
	}

	private static bool SupportsInvocationLogging(string commandName)
	{
		return string.Equals(commandName, "run", StringComparison.OrdinalIgnoreCase)
			|| string.Equals(commandName, "step", StringComparison.OrdinalIgnoreCase)
			|| string.Equals(commandName, "resume", StringComparison.OrdinalIgnoreCase)
			|| string.Equals(commandName, "ask", StringComparison.OrdinalIgnoreCase)
			|| string.Equals(commandName, "act", StringComparison.OrdinalIgnoreCase)
			|| string.Equals(commandName, "respond", StringComparison.OrdinalIgnoreCase)
			|| string.Equals(commandName, "recover", StringComparison.OrdinalIgnoreCase)
			|| string.Equals(commandName, "shell", StringComparison.OrdinalIgnoreCase);
	}

	private static bool HasOption(IEnumerable<string> args, string optionName)
	{
		var longOption = $"--{optionName}";
		return args.Any(arg => string.Equals(arg, longOption, StringComparison.OrdinalIgnoreCase));
	}

	private static string? TryGetOptionValue(string[] args, string optionName)
	{
		var longOption = $"--{optionName}";
		for (var i = 0; i < args.Length - 1; i++)
		{
			if (string.Equals(args[i], longOption, StringComparison.OrdinalIgnoreCase))
			{
				return args[i + 1];
			}
		}

		return null;
	}
}
