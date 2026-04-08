using CommandLine;
using WallyCode.ConsoleApp.Commands;
using WallyCode.ConsoleApp.Copilot;
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

	internal static async Task<int> RunAsync(string[] args, CancellationToken cancellationToken)
	{
		var logger = new AppLogger();
		var providerRegistry = ProviderRegistry.Create(logger);

		using var parser = new Parser(settings =>
		{
			settings.CaseSensitive = false;
			settings.CaseInsensitiveEnumValues = true;
			settings.HelpWriter = Console.Out;
		});

		var result = parser.ParseArguments<LoopCommandOptions, PromptCommandOptions, ProviderCommandOptions, RespondCommandOptions, ShellCommandOptions>(args);

		return await result.MapResult(
			(LoopCommandOptions options) => new LoopCommandHandler(providerRegistry, logger).ExecuteAsync(options, cancellationToken),
			(PromptCommandOptions options) => new PromptCommandHandler(providerRegistry, logger).ExecuteAsync(options, cancellationToken),
			(ProviderCommandOptions options) => new ProviderCommandHandler(providerRegistry, logger).ExecuteAsync(options, cancellationToken),
			(RespondCommandOptions options) => new RespondCommandHandler(logger).ExecuteAsync(options, cancellationToken),
			(ShellCommandOptions options) => new ShellCommandHandler(options).ExecuteAsync(cancellationToken),
			errors => Task.FromResult(GetNotParsedExitCode(errors)));
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
