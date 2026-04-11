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

		var parser = new Parser(settings =>
		{
			settings.CaseSensitive = false;
			settings.CaseInsensitiveEnumValues = true;
			settings.HelpWriter = Console.Out;
			settings.AutoHelp = true;
			settings.AutoVersion = true;
		});

		var result = parser.ParseArguments<LoopCommandOptions, PromptCommandOptions, ProvidersCommandOptions, SetProviderCommandOptions, TestProviderCommandOptions, RespondCommandOptions, ShellCommandOptions>(args);

		try
		{
			return await result.MapResult(
				(LoopCommandOptions options) => new LoopCommandHandler(providerRegistry, logger).ExecuteAsync(options, cancellationToken),
				(PromptCommandOptions options) => new PromptCommandHandler(providerRegistry, logger).ExecuteAsync(options, cancellationToken),
				(ProvidersCommandOptions options) => new ProvidersCommandHandler(providerRegistry).ExecuteAsync(options, cancellationToken),
				(SetProviderCommandOptions options) => new SetProviderCommandHandler(providerRegistry, logger).ExecuteAsync(options, cancellationToken),
				(TestProviderCommandOptions options) => new TestProviderCommandHandler(providerRegistry, logger).ExecuteAsync(options, cancellationToken),
				(RespondCommandOptions options) => new RespondCommandHandler(logger).ExecuteAsync(options, cancellationToken),
				(ShellCommandOptions options) => new ShellCommandHandler(options).ExecuteAsync(cancellationToken),
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
			return 2;
		}
		catch (Exception exception)
		{
			logger.Error(exception.Message);
			return 1;
		}
	}
}
