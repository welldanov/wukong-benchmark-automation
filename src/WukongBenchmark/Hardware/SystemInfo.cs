namespace WukongBenchmark.Hardware;

internal sealed record CpuInfo(string Name, int Cores, int Threads, int MaxClockMhz);

internal sealed record GpuInfo(string Name, string DriverVersion, long? VideoMemoryBytes);

internal sealed record MemoryModule(string Manufacturer, string PartNumber, long CapacityBytes);

internal sealed record MemoryInfo(long TotalBytes, string Type, int SpeedMhz, IReadOnlyList<MemoryModule> Modules);

internal sealed record StorageInfo(string Model, string MediaType, string BusType, long SizeBytes);

internal sealed record OsInfo(string Name, string Version, string Architecture);

internal sealed record SystemInfo(
    CpuInfo Cpu,
    IReadOnlyList<GpuInfo> Gpus,
    MemoryInfo Memory,
    IReadOnlyList<DisplayInfo> Displays,
    StorageInfo? BenchmarkDrive,
    OsInfo Os,
    string PowerPlan,
    string Motherboard);
