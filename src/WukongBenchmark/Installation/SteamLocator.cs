using System.Text.RegularExpressions;
using Microsoft.Win32;
using WukongBenchmark.Infrastructure;

namespace WukongBenchmark.Installation;

internal static partial class SteamLocator
{
    private const string DefaultInstallDirectoryName = "Black Myth Wukong Benchmark Tool";

    public static BenchmarkInstallation Locate(string? explicitDirectory)
    {
        if (!string.IsNullOrWhiteSpace(explicitDirectory))
        {
            return new BenchmarkInstallation(Path.GetFullPath(explicitDirectory));
        }

        foreach (var library in FindLibraries())
        {
            var steamApps = Path.Combine(library, "steamapps");
            var manifest = Path.Combine(steamApps, $"appmanifest_{BenchmarkInstallation.SteamAppId}.acf");
            if (!File.Exists(manifest))
            {
                continue;
            }

            var installDirectory = ReadValue(File.ReadAllText(manifest), "installdir") ?? DefaultInstallDirectoryName;
            return new BenchmarkInstallation(Path.Combine(steamApps, "common", installDirectory));
        }

        throw new BenchmarkException(
            "Black Myth: Wukong Benchmark Tool не найден в библиотеках Steam. Укажите путь вручную: --install-dir <папка>");
    }

    private static IEnumerable<string> FindLibraries()
    {
        var steamPath = FindSteamPath();
        if (steamPath is null)
        {
            return [];
        }

        var libraries = new List<string> { steamPath };
        var libraryFile = Path.Combine(steamPath, "steamapps", "libraryfolders.vdf");
        if (File.Exists(libraryFile))
        {
            libraries.AddRange(ReadValues(File.ReadAllText(libraryFile), "path").Select(Path.GetFullPath));
        }

        return libraries.Distinct(StringComparer.OrdinalIgnoreCase);
    }

    private static string? FindSteamPath()
    {
        var path = Registry.GetValue(@"HKEY_CURRENT_USER\Software\Valve\Steam", "SteamPath", null) as string
                   ?? Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Valve\Steam", "InstallPath", null) as string;

        return path is null ? null : Path.GetFullPath(path);
    }

    private static string? ReadValue(string vdf, string key) => ReadValues(vdf, key).FirstOrDefault();

    private static IEnumerable<string> ReadValues(string vdf, string key) =>
        VdfEntryPattern()
            .Matches(vdf)
            .Where(match => match.Groups["key"].Value.Equals(key, StringComparison.OrdinalIgnoreCase))
            .Select(match => match.Groups["value"].Value.Replace(@"\\", @"\"));

    [GeneratedRegex("\"(?<key>[^\"]+)\"\\s+\"(?<value>[^\"]*)\"")]
    private static partial Regex VdfEntryPattern();
}
