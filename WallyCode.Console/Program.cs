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
		"set-provider",
		"version"
	};

	private static async Task<int> Main(string[] args)
	{
		var normalizedArgs = NormalizeArgs(args);
		var logger = new AppLogger();
		var providerRegistry = ProviderRegistry.Create(logger);
		var loopCommandHandler = new LoopCommandHandler(providerRegistry, logger);

		using var cancellationTokenSource = new CancellationTokenSource();
		Console.CancelKeyPress += (_, eventArgs) =>
		{
			eventArgs.Cancel = true;
			logger.Warning("Cancellation requested. Attempting a graceful shutdown.");
			cancellationTokenSource.Cancel();
		};

		var parser = new Parser(settings =>
		{
			settings.CaseSensitive = false;
			settings.CaseInsensitiveEnumValues = true;
			settings.HelpWriter = Console.Out;
		});

		var result = parser.ParseArguments<LoopCommandOptions, PromptCommandOptions, ProvidersCommandOptions, SetProviderCommandOptions>(normalizedArgs);

		return await result.MapResult(
			(LoopCommandOptions options) => loopCommandHandler.ExecuteAsync(options, cancellationTokenSource.Token),
			(PromptCommandOptions options) => new PromptCommandHandler(providerRegistry, logger).ExecuteAsync(options, cancellationTokenSource.Token),
			(ProvidersCommandOptions options) => new ProvidersCommandHandler(providerRegistry, logger).ExecuteAsync(options, cancellationTokenSource.Token),
			(SetProviderCommandOptions options) => new SetProviderCommandHandler(providerRegistry, logger).ExecuteAsync(options, cancellationTokenSource.Token),
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
