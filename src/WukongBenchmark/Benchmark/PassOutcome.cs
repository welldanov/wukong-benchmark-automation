using WukongBenchmark.Results;
using WukongBenchmark.Settings;

namespace WukongBenchmark.Benchmark;

internal sealed record SettingMismatch(string Key, string Expected, string Actual);

internal sealed record PassOutcome(
    GraphicsProfile Profile,
    BenchmarkResult? Result,
    FrameAnalysis? Analysis,
    IReadOnlyList<SettingMismatch> Mismatches,
    string? Error)
{
    public bool Succeeded => Result is not null;

    public static PassOutcome Success(GraphicsProfile profile, BenchmarkResult result) =>
        new(profile, result, FrameAnalysis.From(result.Frames), SettingsVerifier.Compare(profile, result), null);

    public static PassOutcome Failure(GraphicsProfile profile, string error) =>
        new(profile, null, null, [], error);
}
