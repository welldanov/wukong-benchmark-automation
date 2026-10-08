using System.Text;

namespace WukongBenchmark.Settings;

internal sealed class IniDocument
{
    private readonly List<string> _lines;

    private IniDocument(List<string> lines) => _lines = lines;

    public static IniDocument Load(string path) => new(File.ReadAllLines(path).ToList());

    public void Save(string path) => File.WriteAllLines(path, _lines, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

    public string? Get(string section, string key)
    {
        var (start, end) = FindSection(section);
        if (start < 0)
        {
            return null;
        }

        for (var i = start + 1; i < end; i++)
        {
            if (TryParseEntry(_lines[i], out var entryKey, out var value) && entryKey == key)
            {
                return value;
            }
        }

        return null;
    }

    public void Set(string section, string key, string value)
    {
        var entry = $"{key}={value}";
        var (start, end) = FindSection(section);
        if (start < 0)
        {
            AppendSection(section, entry);
            return;
        }

        for (var i = start + 1; i < end; i++)
        {
            if (TryParseEntry(_lines[i], out var entryKey, out _) && entryKey == key)
            {
                _lines[i] = entry;
                return;
            }
        }

        var insertAt = end;
        while (insertAt > start + 1 && string.IsNullOrWhiteSpace(_lines[insertAt - 1]))
        {
            insertAt--;
        }

        _lines.Insert(insertAt, entry);
    }

    private void AppendSection(string section, string entry)
    {
        if (_lines.Count > 0 && !string.IsNullOrWhiteSpace(_lines[^1]))
        {
            _lines.Add(string.Empty);
        }

        _lines.Add($"[{section}]");
        _lines.Add(entry);
    }

    private (int Start, int End) FindSection(string section)
    {
        var header = $"[{section}]";
        var start = _lines.FindIndex(line => line.Trim().Equals(header, StringComparison.OrdinalIgnoreCase));
        if (start < 0)
        {
            return (-1, -1);
        }

        var end = _lines.FindIndex(start + 1, line => line.TrimStart().StartsWith('['));
        return (start, end < 0 ? _lines.Count : end);
    }

    private static bool TryParseEntry(string line, out string key, out string value)
    {
        var separator = line.IndexOf('=');
        if (separator <= 0)
        {
            key = value = string.Empty;
            return false;
        }

        key = line[..separator].Trim();
        value = line[(separator + 1)..];
        return true;
    }
}
