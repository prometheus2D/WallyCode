using CommandLine;
using WallyCode.ConsoleApp.Commands;
using WallyCode.ConsoleApp.Copilot;
using WallyCode.ConsoleApp.Runtime;

namespace WallyCode.ConsoleApp;

internal static class Program
{
	private static readonly HashSet<string> KnownCommands = new(StringComparer.OrdinalIgnoreCase)
	{
		"help",
		"loop",
		"prompt",
		"providers",
		"respond",
		"set-provider",
		"shell",
		"version"
	};

	private static Task<int> Main(string[] args)
	{
		using var cancellationTokenSource = new CancellationTokenSource();
		Console.CancelKeyPress += (_, eventArgs) =>
		{
			eventArgs.Cancel = true;
			cancellationTokenSource.Cancel();
		};

		return RunAsync(args, cancellationTokenSource.Token);
	}

	internal static async Task<int> RunAsync(string[] args, CancellationToken cancellationToken)
	{
		var normalizedArgs = NormalizeArgs(args);
		var logger = new AppLogger();
		var providerRegistry = ProviderRegistry.Create(logger);
		var loopCommandHandler = new LoopCommandHandler(providerRegistry, logger);

		if (cancellationToken.CanBeCanceled)
		{
			logger.Warning("Press Ctrl+C for graceful cancellation.");
		}

		var parser = new Parser(settings =>
		{
			settings.CaseSensitive = false;
			settings.CaseInsensitiveEnumValues = true;
			settings.HelpWriter = Console.Out;
		});

		var result = parser.ParseArguments<LoopCommandOptions, PromptCommandOptions, ProvidersCommandOptions, RespondCommandOptions, SetProviderCommandOptions, ShellCommandOptions>(normalizedArgs);

		return await result.MapResult(
			(LoopCommandOptions options) => loopCommandHandler.ExecuteAsync(options, cancellationToken),
			(PromptCommandOptions options) => new PromptCommandHandler(providerRegistry, logger).ExecuteAsync(options, cancellationToken),
			(ProvidersCommandOptions options) => new ProvidersCommandHandler(providerRegistry, logger).ExecuteAsync(options, cancellationToken),
			(RespondCommandOptions options) => new RespondCommandHandler(logger).ExecuteAsync(options, cancellationToken),
			(SetProviderCommandOptions options) => new SetProviderCommandHandler(providerRegistry, logger).ExecuteAsync(options, cancellationToken),
			(ShellCommandOptions options) => new ShellCommandHandler().ExecuteAsync(normalizedArgs, cancellationToken),
			errors => Task.FromResult(GetNotParsedExitCode(errors)));
	}

	private static string[] NormalizeArgs(string[] args)
	{
		if (args.Length == 0)
		{
			return ["--help"];
		}

		if (IsHelpToken(args[0]))
		{
			return ["--help"];
		}

		if (args[0].StartsWith("-", StringComparison.Ordinal) || KnownCommands.Contains(args[0]))
		{
			return args;
		}

		return ["loop", .. args];
	}

	private static bool IsHelpToken(string value)
	{
		return string.Equals(value, "--help", StringComparison.OrdinalIgnoreCase)
			|| string.Equals(value, "-h", StringComparison.OrdinalIgnoreCase)
			|| string.Equals(value, "/?", StringComparison.OrdinalIgnoreCase);
	}

	private static int GetNotParsedExitCode(IEnumerable<Error> errors)
	{
		return errors.All(error =>
			error.Tag == ErrorType.HelpRequestedError
			|| error.Tag == ErrorType.HelpVerbRequestedError
			|| error.Tag == ErrorType.VersionRequestedError)
			? 0
			: 1;
	}
}
