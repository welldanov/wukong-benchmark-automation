using System.Management;
using System.Text;
using Microsoft.Win32;
using WukongBenchmark.Interop;

namespace WukongBenchmark.Hardware;

internal static class SystemInfoCollector
{
    private const string DefaultScope = @"root\cimv2";
    private const string StorageScope = @"root\Microsoft\Windows\Storage";
    private const string DisplayAdaptersClassKey = @"SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}";
    private const string PowerSchemesKey = @"SYSTEM\CurrentControlSet\Control\Power\User\PowerSchemes";

    private static readonly Dictionary<long, string> MemoryTypes = new()
    {
        [20] = "DDR", [21] = "DDR2", [24] = "DDR3", [26] = "DDR4", [30] = "LPDDR4", [34] = "DDR5", [35] = "LPDDR5",
    };

    private static readonly Dictionary<long, string> MediaTypes = new() { [3] = "HDD", [4] = "SSD", [5] = "SCM" };

    private static readonly Dictionary<long, string> BusTypes = new()
    {
        [7] = "USB", [8] = "RAID", [10] = "SAS", [11] = "SATA", [17] = "NVMe",
    };

    public static SystemInfo Collect(string benchmarkDirectory) => new(
        CollectCpu(),
        CollectGpus(),
        CollectMemory(),
        DisplayEnumerator.GetDisplays(),
        CollectStorage(benchmarkDirectory),
        CollectOs(),
        CollectPowerPlan(),
        CollectMotherboard());

    private static CpuInfo CollectCpu()
    {
        var processors = Query("Win32_Processor", "Name", "NumberOfCores", "NumberOfLogicalProcessors", "MaxClockSpeed");
        var first = processors.FirstOrDefault() ?? [];
        return new CpuInfo(
            Name: Text(first, "Name"),
            Cores: processors.Sum(row => Integer(row, "NumberOfCores")),
            Threads: processors.Sum(row => Integer(row, "NumberOfLogicalProcessors")),
            MaxClockMhz: Integer(first, "MaxClockSpeed"));
    }

    private static List<GpuInfo> CollectGpus() =>
        Query("Win32_VideoController", "Name", "DriverVersion", "AdapterRAM")
            .Select(row => new GpuInfo(
                Name: Text(row, "Name"),
                DriverVersion: Text(row, "DriverVersion"),
                VideoMemoryBytes: ReadDedicatedVideoMemory(Text(row, "Name")) ?? NullIfZero(Long(row, "AdapterRAM"))))
            .ToList();

    private static MemoryInfo CollectMemory()
    {
        var rows = Query("Win32_PhysicalMemory", "Capacity", "ConfiguredClockSpeed", "Speed", "SMBIOSMemoryType", "Manufacturer", "PartNumber");
        var speed = rows
            .Select(row => Integer(row, "ConfiguredClockSpeed") is > 0 and var configured ? configured : Integer(row, "Speed"))
            .DefaultIfEmpty(0)
            .Max();

        var type = rows
            .Select(row => MemoryTypes.GetValueOrDefault(Long(row, "SMBIOSMemoryType")))
            .FirstOrDefault(name => name is not null) ?? "—";

        var modules = rows
            .Select(row => new MemoryModule(Text(row, "Manufacturer"), Text(row, "PartNumber"), Long(row, "Capacity")))
            .ToList();

        return new MemoryInfo(modules.Sum(module => module.CapacityBytes), type, speed, modules);
    }

    private static StorageInfo? CollectStorage(string benchmarkDirectory)
    {
        var driveLetter = Path.GetPathRoot(Path.GetFullPath(benchmarkDirectory))?.FirstOrDefault();
        if (driveLetter is null or '\\')
        {
            return null;
        }

        try
        {
            var partition = QueryScope(StorageScope, $"MSFT_Partition WHERE DriveLetter = '{driveLetter}'", "DiskNumber").FirstOrDefault();
            if (partition is null)
            {
                return null;
            }

            var disk = QueryScope(StorageScope, $"MSFT_PhysicalDisk WHERE DeviceId = '{Long(partition, "DiskNumber")}'",
                "FriendlyName", "MediaType", "BusType", "Size").FirstOrDefault();

            return disk is null
                ? null
                : new StorageInfo(
                    Model: Text(disk, "FriendlyName"),
                    MediaType: MediaTypes.GetValueOrDefault(Long(disk, "MediaType"), "—"),
                    BusType: BusTypes.GetValueOrDefault(Long(disk, "BusType"), "—"),
                    SizeBytes: Long(disk, "Size"));
        }
        catch (ManagementException)
        {
            return null;
        }
    }

    private static OsInfo CollectOs()
    {
        var os = Query("Win32_OperatingSystem", "Caption", "Version", "BuildNumber", "OSArchitecture").FirstOrDefault() ?? [];
        return new OsInfo(Text(os, "Caption"), $"{Text(os, "Version")} (сборка {Text(os, "BuildNumber")})", Text(os, "OSArchitecture"));
    }

    private static string CollectPowerPlan()
    {
        using var schemes = Registry.LocalMachine.OpenSubKey(PowerSchemesKey);
        if (schemes?.GetValue("ActivePowerScheme") is not string activeScheme)
        {
            return "—";
        }

        using var scheme = schemes.OpenSubKey(activeScheme);
        var friendlyName = scheme?.GetValue("FriendlyName") as string;
        return string.IsNullOrEmpty(friendlyName) ? activeScheme : ResolveIndirectString(friendlyName);
    }

    private static string CollectMotherboard()
    {
        var board = Query("Win32_BaseBoard", "Manufacturer", "Product").FirstOrDefault() ?? [];
        return $"{Text(board, "Manufacturer")} {Text(board, "Product")}".Trim();
    }

    private static long? ReadDedicatedVideoMemory(string adapterName)
    {
        using var adapters = Registry.LocalMachine.OpenSubKey(DisplayAdaptersClassKey);
        if (adapters is null)
        {
            return null;
        }

        foreach (var subKeyName in adapters.GetSubKeyNames().Where(name => name.All(char.IsDigit)))
        {
            using var adapter = adapters.OpenSubKey(subKeyName);
            if (adapter?.GetValue("DriverDesc") as string != adapterName)
            {
                continue;
            }

            return adapter.GetValue("HardwareInformation.qwMemorySize") switch
            {
                long size => size,
                byte[] { Length: >= 8 } bytes => BitConverter.ToInt64(bytes),
                _ => null,
            };
        }

        return null;
    }

    private static string ResolveIndirectString(string value)
    {
        if (!value.StartsWith('@'))
        {
            return value;
        }

        var buffer = new StringBuilder(256);
        return DisplayApi.SHLoadIndirectString(value, buffer, buffer.Capacity, 0) == 0
            ? buffer.ToString()
            : value[(value.LastIndexOf(',') + 1)..];
    }

    private static List<Dictionary<string, object?>> Query(string source, params string[] properties) =>
        QueryScope(DefaultScope, source, properties);

    private static List<Dictionary<string, object?>> QueryScope(string scope, string source, params string[] properties)
    {
        using var searcher = new ManagementObjectSearcher(scope, $"SELECT {string.Join(", ", properties)} FROM {source}");
        using var results = searcher.Get();

        var rows = new List<Dictionary<string, object?>>();
        foreach (var item in results)
        {
            using (item)
            {
                rows.Add(properties.ToDictionary(property => property, object? (property) => item[property]));
            }
        }

        return rows;
    }

    private static string Text(Dictionary<string, object?> row, string key) =>
        row.GetValueOrDefault(key)?.ToString()?.Trim() ?? string.Empty;

    private static int Integer(Dictionary<string, object?> row, string key) => (int)Long(row, key);

    private static long Long(Dictionary<string, object?> row, string key) =>
        row.GetValueOrDefault(key) is { } value ? Convert.ToInt64(value) : 0;

    private static long? NullIfZero(long value) => value == 0 ? null : value;
}
