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

	internal static async Task<int> RunAsync(string[] args, CancellationToken cancellationToken, string? appDirectoryPath = null)
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

		var result = parser.ParseArguments<LoopCommandOptions, AskCommandOptions, ActCommandOptions, ProviderCommandOptions, RespondCommandOptions, LoggingCommandOptions, ShellCommandOptions, SetupCommandOptions>(args);

		try
		{
			return await result.MapResult(
				(LoopCommandOptions options) => new LoopCommandHandler(providerRegistry, logger).ExecuteAsync(options, cancellationToken),
				(AskCommandOptions options) => new LoopCommandHandler(providerRegistry, logger).ExecuteAsync(options.ToLoopOptions(), cancellationToken),
				(ActCommandOptions options) => new LoopCommandHandler(providerRegistry, logger).ExecuteAsync(options.ToLoopOptions(), cancellationToken),
				(ProviderCommandOptions options) => new ProviderCommandHandler(providerRegistry, logger).ExecuteAsync(options, cancellationToken),
				(RespondCommandOptions options) => new RespondCommandHandler(logger).ExecuteAsync(options, cancellationToken),
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
			return 2;
		}
		catch (Exception exception)
		{
			logger.Error(exception.Message);
			return 1;
		}
	}
}
