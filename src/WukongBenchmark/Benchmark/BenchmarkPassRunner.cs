using WukongBenchmark.Automation.Menu;
using WukongBenchmark.Infrastructure;
using WukongBenchmark.Installation;
using WukongBenchmark.Results;
using WukongBenchmark.Settings;

namespace WukongBenchmark.Benchmark;

internal sealed class BenchmarkPassRunner(
    GameProcessManager processes,
    GameUserSettingsFile settingsFile,
    MenuNavigator navigator,
    string outputDirectory)
{
    private static readonly TimeSpan WindowTimeout = TimeSpan.FromMinutes(3);
    private static readonly TimeSpan MenuTimeout = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan ResultTimeout = TimeSpan.FromMinutes(15);

    public async Task<PassOutcome> RunAsync(GraphicsProfile profile, CancellationToken cancellationToken)
    {
        try
        {
            Log.Step($"{profile.Title}: применение настроек графики");
            settingsFile.Apply(profile);

            var watcher = ResultFileWatcher.Snapshot(BenchmarkInstallation.HistoryDirectory);

            Log.Step($"{profile.Title}: запуск бенчмарка");
            processes.Launch();
            var window = await processes.WaitForWindowAsync(WindowTimeout, cancellationToken);

            Log.Step($"{profile.Title}: переход к тесту через меню");
            await navigator.StartBenchmarkAsync(window, () => processes.IsGameRunning, MenuTimeout, cancellationToken);

            Log.Step($"{profile.Title}: тест запущен, ожидание результата");
            var result = await watcher.WaitForResultAsync(() => processes.IsGameRunning, ResultTimeout, cancellationToken);

            File.Copy(result.SourceFile, Path.Combine(outputDirectory, $"{profile.Id}-result.json"), overwrite: true);
            Log.Success($"{profile.Title}: средний FPS {Units.Number(result.Fps.Average)}");

            return PassOutcome.Success(profile, result);
        }
        catch (BenchmarkException exception)
        {
            Log.Error($"{profile.Title}: {exception.Message}");
            return PassOutcome.Failure(profile, exception.Message);
        }
        finally
        {
            processes.Stop();
        }
    }
}
