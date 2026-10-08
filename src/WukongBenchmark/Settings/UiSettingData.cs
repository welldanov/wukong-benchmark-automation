using System.Text.RegularExpressions;

namespace WukongBenchmark.Settings;

internal sealed partial class UiSettingData
{
    private readonly List<KeyValuePair<string, string>> _entries;

    private UiSettingData(List<KeyValuePair<string, string>> entries) => _entries = entries;

    public static UiSettingData Parse(string raw) =>
        new(EntryPattern()
            .Matches(raw)
            .Select(match => KeyValuePair.Create(match.Groups["key"].Value, match.Groups["value"].Value))
            .ToList());

    public void Set(string key, string value)
    {
        var index = _entries.FindIndex(entry => entry.Key == key);
        var updated = KeyValuePair.Create(key, value);
        if (index >= 0)
        {
            _entries[index] = updated;
        }
        else
        {
            _entries.Add(updated);
        }
    }

    public override string ToString() =>
        "(" + string.Join(",", _entries.Select(entry => $"(\"{entry.Key}\", \"{entry.Value}\")")) + ")";

    [GeneratedRegex("""\("(?<key>[^"]+)",\s*"(?<value>[^"]*)"\)""")]
    private static partial Regex EntryPattern();
}
