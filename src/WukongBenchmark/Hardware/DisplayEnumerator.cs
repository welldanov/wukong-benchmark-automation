using WukongBenchmark.Interop;

namespace WukongBenchmark.Hardware;

internal sealed record DisplayInfo(
    int Index,
    string AdapterName,
    string MonitorId,
    string MonitorModel,
    int Width,
    int Height,
    int RefreshRate,
    bool IsPrimary)
{
    public long PixelCount => (long)Width * Height;

    public string Resolution => $"{Width}×{Height} @ {RefreshRate} Гц";
}

internal static class DisplayEnumerator
{
    public static IReadOnlyList<DisplayInfo> GetDisplays()
    {
        var displays = new List<DisplayInfo>();
        var adapter = DisplayApi.DisplayDevice.Create();
        for (uint index = 0; DisplayApi.EnumDisplayDevices(null, index, ref adapter, 0); index++)
        {
            if ((adapter.StateFlags & DisplayApi.AttachedToDesktop) != 0 && TryDescribe(adapter, displays.Count) is { } display)
            {
                displays.Add(display);
            }

            adapter = DisplayApi.DisplayDevice.Create();
        }

        return displays;
    }

    public static DisplayInfo? SelectLargest(IReadOnlyList<DisplayInfo> displays) =>
        displays
            .Where(display => !string.IsNullOrEmpty(display.MonitorId))
            .OrderByDescending(display => display.PixelCount)
            .ThenByDescending(display => display.IsPrimary)
            .FirstOrDefault();

    private static DisplayInfo? TryDescribe(DisplayApi.DisplayDevice adapter, int index)
    {
        var mode = DisplayApi.DevMode.Create();
        if (!DisplayApi.EnumDisplaySettings(adapter.DeviceName, DisplayApi.CurrentSettings, ref mode))
        {
            return null;
        }

        var monitor = DisplayApi.DisplayDevice.Create();
        var hasMonitor = DisplayApi.EnumDisplayDevices(adapter.DeviceName, 0, ref monitor, 0);
        var monitorId = hasMonitor ? monitor.DeviceId : string.Empty;

        return new DisplayInfo(
            Index: index,
            AdapterName: adapter.DeviceName,
            MonitorId: monitorId,
            MonitorModel: ModelCode(monitorId),
            Width: (int)mode.PelsWidth,
            Height: (int)mode.PelsHeight,
            RefreshRate: (int)mode.DisplayFrequency,
            IsPrimary: (adapter.StateFlags & DisplayApi.PrimaryDevice) != 0);
    }

    private static string ModelCode(string monitorId)
    {
        var parts = monitorId.Split('\\');
        return parts.Length > 1 ? parts[1] : "—";
    }
}
