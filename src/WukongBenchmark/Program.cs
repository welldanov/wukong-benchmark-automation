using System.Text;
using WukongBenchmark.App;
using WukongBenchmark.Infrastructure;

Console.OutputEncoding = Encoding.UTF8;

var exitCode = await RunAsync(args);
ConsoleSession.WaitForKeyIfStandalone();
return exitCode;

static async Task<int> RunAsync(string[] args)
{
    CommandLineOptions options;
    try
    {
        options = CommandLineOptions.Parse(args);
    }
    catch (BenchmarkException exception)
    {
        Log.Error(exception.Message);
        Console.WriteLine(CommandLineOptions.Usage);
        return ExitCodes.InvalidArguments;
    }

    if (options.ShowHelp)
    {
        Console.WriteLine(CommandLineOptions.Usage);
        return ExitCodes.Success;
    }

    using var cancellation = new CancellationTokenSource();
    Console.CancelKeyPress += (_, eventArgs) =>
    {
        eventArgs.Cancel = true;
        cancellation.Cancel();
    };

    try
    {
        return await new BenchmarkApplication(options).RunAsync(cancellation.Token);
    }
    catch (OperationCanceledException)
    {
        Log.Warning("Выполнение прервано пользователем.");
        return ExitCodes.Cancelled;
    }
    catch (BenchmarkException exception)
    {
        Log.Error(exception.Message);
        return ExitCodes.Error;
    }
}
