using System.Globalization;

namespace WukongBenchmark.Settings;

internal static class GraphicsProfiles
{
    public const string CpuId = "cpu";
    public const string GpuId = "gpu";
    public const string WindowSizeKey = "WindowSize";
    public const string RenderResolutionKey = "RenderResolution";
    public static readonly IReadOnlyList<string> AllIds = [CpuId, GpuId];

    private const string ScalabilitySection = "ScalabilityGroups";
    private const string RayTracingSection = "RayTracing";
    private const string BorderlessScreenMode = "1";
    private const string WindowedScreenMode = "2";
    private const string BorderlessFullscreenMode = "1";
    private const string WindowedFullscreenMode = "2";
    private const string TemporalSuperResolution = "3";
    private const int CpuWindowWidth = 1280;
    private const int CpuWindowHeight = 720;
    private const int MinimumRenderScalePercent = 25;
    private const int MinimumDesiredScalePercent = 50;
    private const int NativeScalePercent = 100;

    private static readonly string[] UiQualityKeys =
    [
        "ViewDistance", "AntiAliasing", "PostProcessing", "ShadowQuality", "TextureQuality",
        "FxQuality", "MaterialQuality", "VegetationQuality", "GlobalIllumination", "ReflectionQuality",
    ];

    private static readonly string[] ScalabilityQualityKeys =
    [
        "sg.ViewDistanceQuality", "sg.AntiAliasingQuality", "sg.ShadowQuality", "sg.GlobalIlluminationQuality",
        "sg.ReflectionQuality", "sg.PostProcessQuality", "sg.TextureQuality", "sg.EffectsQuality",
        "sg.FoliageQuality", "sg.ShadingQuality",
    ];

    public static IReadOnlyList<GraphicsProfile> Create(TargetMonitor monitor, IEnumerable<string> ids) =>
        ids.Select(id => id switch
            {
                CpuId => Cpu(monitor),
                GpuId => Gpu(monitor),
                _ => throw new ArgumentOutOfRangeException(nameof(ids), id, null),
            })
            .ToList();

    private static GraphicsProfile Cpu(TargetMonitor monitor) => Build(
        id: CpuId,
        title: "CPU-тест",
        rationale: $"Окно {CpuWindowWidth}×{CpuWindowHeight}, минимальный масштаб рендера, все параметры графики " +
                   "на минимуме, без трассировки лучей, генерации кадров и ограничения FPS. Видеокарта рендерит крошечную " +
                   "картинку и простаивает, частоту кадров ограничивает процессор.",
        monitor: monitor,
        display: new DisplayMode(CpuWindowWidth, CpuWindowHeight, Windowed: true, MinimumRenderScalePercent, MinimumDesiredScalePercent),
        quality: QualityLevel.Low,
        rayTracing: false);

    private static GraphicsProfile Gpu(TargetMonitor monitor) => Build(
        id: GpuId,
        title: "GPU-тест",
        rationale: $"Родное разрешение самого большого монитора {monitor.Width}×{monitor.Height}, масштаб рендера 100 %, " +
                   "пресет «Реалистичное», полная трассировка лучей, без генерации кадров и ограничения FPS. " +
                   "Видеокарта загружена максимально.",
        monitor: monitor,
        display: new DisplayMode(monitor.Width, monitor.Height, Windowed: false, NativeScalePercent, NativeScalePercent),
        quality: QualityLevel.Cinematic,
        rayTracing: true);

    private static GraphicsProfile Build(
        string id,
        string title,
        string rationale,
        TargetMonitor monitor,
        DisplayMode display,
        QualityLevel quality,
        bool rayTracing)
    {
        var level = Format((int)quality);
        var toggles = new Dictionary<string, string>
        {
            ["QualityLevel"] = level,
            ["Rtx"] = rayTracing ? "1" : "0",
            ["InsertFrame"] = "0",
            ["Vsync"] = "0",
            ["LockFrameRate"] = "0",
        };

        var uiSettings = new Dictionary<string, string>(toggles)
        {
            ["MainDisplay"] = Format(monitor.Index),
            ["ScreenMode"] = display.Windowed ? WindowedScreenMode : BorderlessScreenMode,
            ["ImageQuality"] = Format(display.RenderHeight),
            ["SuperResolutionSampling"] = TemporalSuperResolution,
        };

        var expectedSettings = new Dictionary<string, string>(toggles)
        {
            ["ScreenMode"] = uiSettings["ScreenMode"],
            [WindowSizeKey] = display.Resolution,
            [RenderResolutionKey] = display.DesiredResolution,
            ["ImageQuality"] = Format(display.RenderScalePercent),
            ["Dlss"] = TemporalSuperResolution,
        };

        if (!display.Windowed)
        {
            expectedSettings["ScreenResolution"] = display.Resolution;
        }

        foreach (var key in UiQualityKeys)
        {
            uiSettings[key] = level;
            expectedSettings[key] = level;
        }

        var scalabilityLevel = Format((int)quality - 1);
        var iniSettings = ScalabilityQualityKeys
            .Select(key => new IniSetting(ScalabilitySection, key, scalabilityLevel))
            .Append(new IniSetting(RayTracingSection, "r.RayTracing.EnableInGame", rayTracing ? "True" : "False"))
            .Concat(DisplayIniSettings(monitor, display))
            .Append(new IniSetting(GameUserSettingsFile.MainSection, "bUseVSync", "False"))
            .Append(new IniSetting(GameUserSettingsFile.MainSection, "FrameRateLimit", "0.000000"))
            .ToList();

        return new GraphicsProfile(id, title, rationale, display, uiSettings, iniSettings, expectedSettings);
    }

    private static IEnumerable<IniSetting> DisplayIniSettings(TargetMonitor monitor, DisplayMode display)
    {
        var section = GameUserSettingsFile.MainSection;
        yield return new IniSetting(section, "MainMonitorID", $"\"{monitor.DeviceId.Replace(@"\", @"\\")}\"");
        yield return new IniSetting(section, "FullscreenMode", display.Windowed ? WindowedFullscreenMode : BorderlessFullscreenMode);
        yield return new IniSetting(section, "ResolutionSizeX", Format(display.Width));
        yield return new IniSetting(section, "ResolutionSizeY", Format(display.Height));
        yield return new IniSetting(section, "LastUserConfirmedResolutionSizeX", Format(display.Width));
        yield return new IniSetting(section, "LastUserConfirmedResolutionSizeY", Format(display.Height));
        yield return new IniSetting(section, "DesiredScreenWidth", Format(display.DesiredWidth));
        yield return new IniSetting(section, "DesiredScreenHeight", Format(display.DesiredHeight));
        yield return new IniSetting(section, "LastUserConfirmedDesiredScreenWidth", Format(display.DesiredWidth));
        yield return new IniSetting(section, "LastUserConfirmedDesiredScreenHeight", Format(display.DesiredHeight));
    }

    private static string Format(int value) => value.ToString(CultureInfo.InvariantCulture);
}
