using WukongBenchmark.Results;
using WukongBenchmark.Settings;

namespace WukongBenchmark.Benchmark;

internal static class SettingsVerifier
{
    public static IReadOnlyList<SettingMismatch> Compare(GraphicsProfile profile, BenchmarkResult result) =>
        profile.ExpectedSettings
            .Where(expected => result.Settings.TryGetValue(expected.Key, out var actual) && !Matches(expected.Value, actual))
            .Select(expected => new SettingMismatch(expected.Key, expected.Value, result.Settings[expected.Key]))
            .ToList();

    private static bool Matches(string expected, string actual) =>
        string.Equals(Normalize(expected), Normalize(actual), StringComparison.OrdinalIgnoreCase);

    private static string Normalize(string value) => value.Replace(" ", string.Empty);
}
