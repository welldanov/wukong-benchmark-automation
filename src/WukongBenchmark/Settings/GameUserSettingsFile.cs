using WukongBenchmark.Infrastructure;

namespace WukongBenchmark.Settings;

internal sealed class GameUserSettingsFile(string filePath)
{
    public const string MainSection = "/Script/GSGameSettings.GSGameUserSettings";

    private const string UiSettingDataKey = "UISettingData";

    public void Apply(GraphicsProfile profile)
    {
        var document = IniDocument.Load(filePath);
        var rawUiSettings = document.Get(MainSection, UiSettingDataKey)
                            ?? throw new BenchmarkException($"В {filePath} нет ключа {UiSettingDataKey}. Запустите бенчмарк вручную один раз.");

        var uiSettings = UiSettingData.Parse(rawUiSettings);
        foreach (var (key, value) in profile.UiSettings)
        {
            uiSettings.Set(key, value);
        }

        document.Set(MainSection, UiSettingDataKey, uiSettings.ToString());
        foreach (var setting in profile.IniSettings)
        {
            document.Set(setting.Section, setting.Key, setting.Value);
        }

        document.Save(filePath);
    }

    public string Backup(string directory)
    {
        var backupPath = Path.Combine(directory, "GameUserSettings.backup.ini");
        File.Copy(filePath, backupPath, overwrite: true);
        return backupPath;
    }

    public void Restore(string backupPath) => File.Copy(backupPath, filePath, overwrite: true);
}
