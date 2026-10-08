using WukongBenchmark.Settings;

namespace WukongBenchmark.Reporting;

internal static class SettingFormatter
{
    private const string CustomPreset = "6";

    private static readonly (string Key, string Title)[] KnownSettings =
    [
        ("ScreenResolution", "Разрешение экрана"),
        ("ScreenMode", "Режим отображения"),
        (GraphicsProfiles.WindowSizeKey, "Размер окна"),
        (GraphicsProfiles.RenderResolutionKey, "Разрешение рендера"),
        ("QualityLevel", "Набор настроек графики"),
        ("ImageQuality", "Масштаб рендера"),
        ("ViewDistance", "Детализация объектов вдали"),
        ("AntiAliasing", "Качество сглаживания"),
        ("PostProcessing", "Качество постобработки"),
        ("ShadowQuality", "Качество теней"),
        ("TextureQuality", "Качество текстур"),
        ("MaterialQuality", "Качество волос"),
        ("VegetationQuality", "Качество растительности"),
        ("FxQuality", "Качество эффектов"),
        ("GlobalIllumination", "Глобальное освещение"),
        ("ReflectionQuality", "Качество отражений"),
        ("MotionBlur", "Размытие при движении"),
        ("Rtx", "Полная трассировка лучей"),
        ("Dlss", "Технология масштабирования"),
        ("InsertFrame", "Генерация кадров"),
        ("Dx12", "DirectX 12"),
        ("Vsync", "Вертикальная синхронизация"),
        ("LockFrameRate", "Ограничение частоты кадров"),
    ];

    private static readonly HashSet<string> QualityKeys =
    [
        "QualityLevel", "ViewDistance", "AntiAliasing", "PostProcessing", "ShadowQuality", "TextureQuality",
        "MaterialQuality", "VegetationQuality", "FxQuality", "GlobalIllumination", "ReflectionQuality",
    ];

    private static readonly HashSet<string> ToggleKeys = ["Rtx", "InsertFrame", "Dx12", "Vsync"];

    private static readonly string[] QualityNames = ["Низкое", "Среднее", "Высокое", "Очень высокое", "Реалистичное"];

    public static IEnumerable<string> Order(IEnumerable<string> keys)
    {
        var known = KnownSettings.Select(setting => setting.Key).ToList();
        return keys
            .Distinct()
            .OrderBy(key => known.IndexOf(key) is >= 0 and var index ? index : int.MaxValue)
            .ThenBy(key => key, StringComparer.Ordinal);
    }

    public static string Title(string key) =>
        KnownSettings.FirstOrDefault(setting => setting.Key == key).Title ?? key;

    public static string Value(string key, string value)
    {
        if (key == "QualityLevel" && value == CustomPreset)
        {
            return "Пользовательский";
        }

        if (QualityKeys.Contains(key) && int.TryParse(value, out var level) && level >= 1 && level <= QualityNames.Length)
        {
            return QualityNames[level - 1];
        }

        if (ToggleKeys.Contains(key))
        {
            return value switch { "0" => "Выкл.", "1" => "Вкл.", _ => value };
        }

        return (key, value) switch
        {
            ("ScreenMode", "1") => "Окно без рамок",
            ("ScreenMode", "2") => "Оконный",
            ("Dlss", "2") => "DLSS",
            ("Dlss", "3") => "TSR",
            ("MotionBlur", "2") => "Очень заметное",
            ("ImageQuality", _) => $"{value} %",
            ("LockFrameRate", "0") => "Нет",
            _ => value,
        };
    }
}
