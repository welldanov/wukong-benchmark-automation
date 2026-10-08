using WukongBenchmark.Benchmark;
using WukongBenchmark.Hardware;

namespace WukongBenchmark.Reporting;

internal sealed record BenchmarkReport(
    DateTimeOffset CreatedAt,
    SystemInfo System,
    DisplayInfo TargetDisplay,
    IReadOnlyList<PassOutcome> Passes,
    string OutputDirectory);

internal sealed record ReportSection(string Title, TextTable? Table, IReadOnlyList<string> Notes);
