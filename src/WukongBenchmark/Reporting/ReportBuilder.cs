using WukongBenchmark.Benchmark;
using WukongBenchmark.Hardware;
using WukongBenchmark.Infrastructure;

namespace WukongBenchmark.Reporting;

internal static class ReportBuilder
{
    private const string NoValue = "—";
    private const string AppliedMarker = " *";

    public static IReadOnlyList<ReportSection> Build(BenchmarkReport report)
    {
        var sections = new List<ReportSection>
        {
            BuildSystemSection(report),
            BuildResultsSection(report.Passes),
            BuildSettingsSection(report.Passes),
        };

        var failures = report.Passes.Where(pass => !pass.Succeeded).ToList();
        if (failures.Count > 0)
        {
            sections.Add(new ReportSection(
                "Ошибки",
                null,
                failures.Select(pass => $"{pass.Profile.Title}: {pass.Error}").ToList()));
        }

        sections.Add(new ReportSection(
            "Файлы",
            null,
            [$"Отчёт, исходные результаты бенчмарка и резервная копия настроек: {report.OutputDirectory}"]));

        return sections;
    }

    private static ReportSection BuildSystemSection(BenchmarkReport report)
    {
        var system = report.System;
        var table = new TextTable("Компонент", "Характеристики")
            .AddRow("Процессор", system.Cpu.Name)
            .AddRow("  Ядра / потоки", $"{system.Cpu.Cores} / {system.Cpu.Threads}")
            .AddRow("  Базовая частота", system.Cpu.MaxClockMhz > 0 ? $"{system.Cpu.MaxClockMhz} МГц" : NoValue);

        foreach (var gpu in system.Gpus)
        {
            table.AddRow("Видеокарта", gpu.Name)
                .AddRow("  Видеопамять", gpu.VideoMemoryBytes is { } bytes ? Units.Gigabytes(bytes) : NoValue)
                .AddRow("  Драйвер", gpu.DriverVersion);
        }

        var memory = system.Memory;
        table.AddRow("Оперативная память", $"{Units.Gigabytes(memory.TotalBytes)} {memory.Type}")
            .AddRow("  Частота", memory.SpeedMhz > 0 ? $"{memory.SpeedMhz} МТ/с" : NoValue)
            .AddRow("  Модули", DescribeModules(memory));

        foreach (var display in system.Displays)
        {
            var marker = display.MonitorId == report.TargetDisplay.MonitorId ? " — используется для тестов" : string.Empty;
            var primary = display.IsPrimary ? ", основной" : string.Empty;
            table.AddRow("Монитор", $"{display.MonitorModel}: {display.Resolution}{primary}{marker}");
        }

        if (system.BenchmarkDrive is { } drive)
        {
            table.AddRow("Накопитель с бенчмарком", $"{drive.Model} ({drive.MediaType}, {drive.BusType}, {Units.Gigabytes(drive.SizeBytes)})");
        }

        table.AddRow("Материнская плата / система", string.IsNullOrEmpty(system.Motherboard) ? NoValue : system.Motherboard)
            .AddRow("Операционная система", $"{system.Os.Name} {system.Os.Architecture}")
            .AddRow("  Версия", system.Os.Version)
            .AddRow("  Схема электропитания", system.PowerPlan);

        if (report.Passes.FirstOrDefault(pass => pass.Result is not null)?.Result is { } result)
        {
            table.AddRow("Версия бенчмарка", result.Hardware.GameVersion)
                .AddRow("Драйвер видеокарты (по данным бенчмарка)", result.Hardware.GpuDriver);
        }

        var notes = new List<string>();
        if (memory.Modules.Count == 1)
        {
            notes.Add("Установлен один модуль памяти: память работает в одноканальном режиме, это снижает результат CPU-теста.");
        }

        return new ReportSection("Характеристики компьютера", table, notes);
    }

    private static string DescribeModules(MemoryInfo memory) =>
        memory.Modules.Count == 0
            ? NoValue
            : string.Join("; ", memory.Modules.Select(module =>
                $"{module.Manufacturer} {module.PartNumber} {Units.Gigabytes(module.CapacityBytes)}".Trim()));

    private static ReportSection BuildResultsSection(IReadOnlyList<PassOutcome> passes)
    {
        var table = new TextTable(["Показатель", .. passes.Select(pass => pass.Profile.Title)]);

        AddMetric(table, passes, "Средний FPS", pass => Units.Number(pass.Result!.Fps.Average));
        AddMetric(table, passes, "Минимальный FPS", pass => Units.Number(pass.Result!.Fps.Min));
        AddMetric(table, passes, "Максимальный FPS", pass => Units.Number(pass.Result!.Fps.Max));
        AddMetric(table, passes, "5-й перцентиль FPS", pass => Units.Number(pass.Result!.Fps.Percentile5));
        AddMetric(table, passes, "1% low FPS", pass => Analysis(pass, analysis => Units.Number(analysis.OnePercentLowFps)));
        AddMetric(table, passes, "Видеопамять", pass => $"{Units.Number(pass.Result!.VideoMemoryUsedGb)} ГБ");
        AddMetric(table, passes, "Время кадра CPU (среднее)", pass => Analysis(pass, analysis => $"{Units.Number(analysis.AverageCpuFrameTimeMs, "0.0")} мс"));
        AddMetric(table, passes, "Время кадра GPU (среднее)", pass => Analysis(pass, analysis => $"{Units.Number(analysis.AverageGpuFrameTimeMs, "0.0")} мс"));
        AddMetric(table, passes, "Кадров, ограниченных CPU", pass => Analysis(pass, analysis => Units.Percent(analysis.CpuBoundShare)));
        AddMetric(table, passes, "Узкое место", pass => Analysis(pass, analysis => analysis.Bottleneck));
        AddMetric(table, passes, "Кадров записано", pass => Analysis(pass, analysis => $"{analysis.FrameCount} за {analysis.Duration.TotalSeconds:0} с"));
        AddMetric(table, passes, "Время завершения", pass => pass.Result!.CompletedAt.ToString("dd.MM.yyyy HH:mm:ss"));

        return new ReportSection(
            "Результаты тестов",
            table,
            [
                "FPS и видеопамять взяты из отчёта бенчмарка, остальные показатели рассчитаны по его покадровым данным.",
                "«Узкое место» — компонент, который дольше готовит кадр в большинстве кадров.",
            ]);
    }

    private static ReportSection BuildSettingsSection(IReadOnlyList<PassOutcome> passes)
    {
        var keys = SettingFormatter.Order(passes.SelectMany(pass =>
            (pass.Result?.Settings.Keys ?? Enumerable.Empty<string>()).Concat(pass.Profile.ExpectedSettings.Keys)));

        var table = new TextTable(["Настройка", .. passes.Select(pass => pass.Profile.Title)]);
        foreach (var key in keys)
        {
            table.AddRow([SettingFormatter.Title(key), .. passes.Select(pass => SettingValue(pass, key))]);
        }

        var notes = new List<string>
        {
            "Значения взяты из отчёта бенчмарка. Отмеченные * записаны инструментом в конфигурацию, но бенчмарк их не сообщает.",
        };
        notes.AddRange(passes.Select(pass => $"{pass.Profile.Title}: {pass.Profile.Rationale}"));
        notes.AddRange(passes.SelectMany(pass => pass.Mismatches.Select(mismatch =>
            $"Внимание, {pass.Profile.Title}: «{SettingFormatter.Title(mismatch.Key)}» = " +
            $"{SettingFormatter.Value(mismatch.Key, mismatch.Actual)}, ожидалось {SettingFormatter.Value(mismatch.Key, mismatch.Expected)}.")));

        return new ReportSection("Настройки тестов", table, notes);
    }

    private static string SettingValue(PassOutcome pass, string key)
    {
        if (pass.Result?.Settings.TryGetValue(key, out var reported) == true)
        {
            return SettingFormatter.Value(key, reported);
        }

        return pass.Profile.ExpectedSettings.TryGetValue(key, out var applied)
            ? SettingFormatter.Value(key, applied) + AppliedMarker
            : NoValue;
    }

    private static void AddMetric(TextTable table, IReadOnlyList<PassOutcome> passes, string title, Func<PassOutcome, string> value) =>
        table.AddRow([title, .. passes.Select(pass => pass.Succeeded ? value(pass) : "ошибка")]);

    private static string Analysis(PassOutcome pass, Func<Results.FrameAnalysis, string> format) =>
        pass.Analysis is { } analysis ? format(analysis) : NoValue;
}
