using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using WukongBenchmark.Automation;
using WukongBenchmark.Infrastructure;
using WukongBenchmark.Installation;

namespace WukongBenchmark.Benchmark;

internal sealed class GameProcessManager(BenchmarkInstallation installation)
{
    private const string GameProcessName = "b1-Win64-Shipping";
    private const string LauncherProcessName = "b1_benchmark";
    private const string SteamProcessName = "steam";

    private static readonly string[] SteamLaunchVariables = ["SteamAppId", "SteamGameId", "SteamOverlayGameId"];
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan ExitTimeout = TimeSpan.FromSeconds(15);

    public bool IsAnyRunning => IsRunning(GameProcessName) || IsRunning(LauncherProcessName);

    public bool IsGameRunning => IsRunning(GameProcessName);

    public static bool IsSteamRunning => IsRunning(SteamProcessName);

    public void Launch()
    {
        var startInfo = new ProcessStartInfo(installation.LauncherPath)
        {
            WorkingDirectory = installation.RootDirectory,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };

        var appId = BenchmarkInstallation.SteamAppId.ToString(CultureInfo.InvariantCulture);
        foreach (var variable in SteamLaunchVariables)
        {
            startInfo.Environment[variable] = appId;
        }

        var process = Process.Start(startInfo)
                      ?? throw new BenchmarkException($"Не удалось запустить {installation.LauncherPath}");

        process.OutputDataReceived += (_, _) => { };
        process.ErrorDataReceived += (_, _) => { };
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        process.Exited += (_, _) => process.Dispose();
        process.EnableRaisingEvents = true;
    }

    public async Task<GameWindow> WaitForWindowAsync(TimeSpan timeout, CancellationToken cancellationToken)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            if (FindMainWindow() is { } handle)
            {
                return new GameWindow(handle);
            }

            await Task.Delay(PollInterval, cancellationToken);
        }

        throw new BenchmarkException($"Окно бенчмарка не появилось за {timeout.TotalMinutes:0} мин.");
    }

    public void Stop()
    {
        foreach (var name in new[] { GameProcessName, LauncherProcessName })
        {
            foreach (var process in Process.GetProcessesByName(name))
            {
                using (process)
                {
                    try
                    {
                        process.Kill(entireProcessTree: true);
                        process.WaitForExit(ExitTimeout);
                    }
                    catch (Exception exception) when (exception is InvalidOperationException or Win32Exception)
                    {
                        Log.Warning($"Не удалось завершить процесс {name}: {exception.Message}");
                    }
                }
            }
        }
    }

    private static nint? FindMainWindow()
    {
        foreach (var process in Process.GetProcessesByName(GameProcessName))
        {
            using (process)
            {
                process.Refresh();
                if (process.MainWindowHandle != 0)
                {
                    return process.MainWindowHandle;
                }
            }
        }

        return null;
    }

    private static bool IsRunning(string name)
    {
        var processes = Process.GetProcessesByName(name);
        foreach (var process in processes)
        {
            process.Dispose();
        }

        return processes.Length > 0;
    }
}
