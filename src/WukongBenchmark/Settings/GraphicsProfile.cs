namespace WukongBenchmark.Settings;

internal sealed record IniSetting(string Section, string Key, string Value);

internal sealed record TargetMonitor(int Index, string DeviceId, int Width, int Height);

internal sealed record DisplayMode(int Width, int Height, bool Windowed, int RenderScalePercent, int DesiredScalePercent)
{
    public int RenderHeight => Height * RenderScalePercent / 100;

    public int DesiredWidth => Width * DesiredScalePercent / 100;

    public int DesiredHeight => Height * DesiredScalePercent / 100;

    public string Resolution => $"{Width} × {Height}";

    public string DesiredResolution => $"{DesiredWidth} × {DesiredHeight}";
}

internal sealed record GraphicsProfile(
    string Id,
    string Title,
    string Rationale,
    DisplayMode Display,
    IReadOnlyDictionary<string, string> UiSettings,
    IReadOnlyList<IniSetting> IniSettings,
    IReadOnlyDictionary<string, string> ExpectedSettings);
