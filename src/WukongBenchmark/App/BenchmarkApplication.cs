using WukongBenchmark.Automation.Menu;
using WukongBenchmark.Automation.Vision;
using WukongBenchmark.Benchmark;
using WukongBenchmark.Hardware;
using WukongBenchmark.Infrastructure;
using WukongBenchmark.Installation;
using WukongBenchmark.Reporting;
using WukongBenchmark.Settings;

namespace WukongBenchmark.App;

internal sealed class BenchmarkApplication(CommandLineOptions options)
{
    private static readonly string[] RecognitionLanguages = ["ru", "en-US"];

    public async Task<int> RunAsync(CancellationToken cancellationToken)
    {
        Elevation.Require();

        Log.Step("Поиск Black Myth: Wukong Benchmark Tool");
        var installation = SteamLocator.Locate(options.InstallDirectory);
        installation.Validate();
        Log.Info(installation.RootDirectory);

        var processes = new GameProcessManager(installation);
        if (processes.IsAnyRunning)
        {
            throw new BenchmarkException("Бенчмарк уже запущен. Закройте его и запустите инструмент снова.");
        }

        if (!GameProcessManager.IsSteamRunning)
        {
            Log.Warning("Steam не запущен, бенчмарк может не стартовать.");
        }

        var outputDirectory = Directory.CreateDirectory(
            Path.Combine(options.OutputDirectory, DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss"))).FullName;

        Log.Step("Сбор характеристик компьютера");
        var system = SystemInfoCollector.Collect(installation.RootDirectory);

        var targetDisplay = DisplayEnumerator.SelectLargest(system.Displays)
                            ?? throw new BenchmarkException("Не удалось определить подключённые мониторы.");
        Log.Info($"Монитор для тестов: {targetDisplay.MonitorModel}, {targetDisplay.Resolution}");

        var settingsFile = new GameUserSettingsFile(installation.SettingsPath);
        var monitor = new TargetMonitor(targetDisplay.Index, targetDisplay.MonitorId, targetDisplay.Width, targetDisplay.Height);
        var profiles = GraphicsProfiles.Create(monitor, options.ProfileIds);
        var backupPath = settingsFile.Backup(outputDirectory);
        var navigator = new MenuNavigator(TextRecognizer.Create(RecognitionLanguages), outputDirectory);
        var runner = new BenchmarkPassRunner(processes, settingsFile, navigator, outputDirectory);

        Log.Warning("Не трогайте мышь и клавиатуру, пока идут тесты.");

        var outcomes = new List<PassOutcome>();
        try
        {
            foreach (var profile in profiles)
            {
                outcomes.Add(await runner.RunAsync(profile, cancellationToken));
            }
        }
        finally
        {
            processes.Stop();
            settingsFile.Restore(backupPath);
            Log.Info("Исходные настройки бенчмарка восстановлены.");
        }

        var report = new BenchmarkReport(DateTimeOffset.Now, system, targetDisplay, outcomes, outputDirectory);
        var sections = ReportBuilder.Build(report);
        ConsoleReportRenderer.Render(sections);

        var reportPath = MarkdownReportRenderer.Save(sections, report.CreatedAt, Path.Combine(outputDirectory, "report.md"));
        Log.Success($"Отчёт сохранён: {reportPath}");

        return outcomes.All(outcome => outcome.Succeeded) ? ExitCodes.Success : ExitCodes.PassFailed;
    }
}
