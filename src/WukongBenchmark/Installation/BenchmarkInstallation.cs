using WukongBenchmark.Infrastructure;

namespace WukongBenchmark.Installation;

internal sealed record BenchmarkInstallation(string RootDirectory)
{
    public const int SteamAppId = 3132990;

    public string LauncherPath => Path.Combine(RootDirectory, "b1_benchmark.exe");

    public string SettingsPath => Path.Combine(RootDirectory, "b1", "Saved", "Config", "Windows", "GameUserSettings.ini");

    public static string HistoryDirectory => Path.Combine(Path.GetTempPath(), "b1", "BenchMarkHistory", "Tool");

    public void Validate()
    {
        if (!File.Exists(LauncherPath))
        {
            throw new BenchmarkException($"Не найден исполняемый файл бенчмарка: {LauncherPath}");
        }

        if (!File.Exists(SettingsPath))
        {
            throw new BenchmarkException(
                $"Не найден файл настроек {SettingsPath}. Запустите бенчмарк вручную один раз, чтобы он создал конфигурацию.");
        }
    }
}
