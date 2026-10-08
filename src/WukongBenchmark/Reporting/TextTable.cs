using System.Text;

namespace WukongBenchmark.Reporting;

internal sealed class TextTable(params string[] headers)
{
    private readonly List<string[]> _rows = [];

    public IReadOnlyList<string> Headers => headers;

    public IReadOnlyList<string[]> Rows => _rows;

    public TextTable AddRow(params string[] cells)
    {
        _rows.Add(cells);
        return this;
    }

    public string RenderPlain()
    {
        var widths = headers
            .Select((header, column) => _rows.Select(row => Cell(row, column).Length).Prepend(header.Length).Max())
            .ToArray();

        var separator = "+" + string.Join("+", widths.Select(width => new string('-', width + 2))) + "+";
        var builder = new StringBuilder()
            .AppendLine(separator)
            .AppendLine(FormatRow(headers, widths))
            .AppendLine(separator);

        foreach (var row in _rows)
        {
            builder.AppendLine(FormatRow(row, widths));
        }

        return builder.Append(separator).ToString();
    }

    public string RenderMarkdown()
    {
        var builder = new StringBuilder()
            .AppendLine("| " + string.Join(" | ", headers.Select(Escape)) + " |")
            .AppendLine("|" + string.Join("|", headers.Select(_ => "---")) + "|");

        foreach (var row in _rows)
        {
            builder.AppendLine("| " + string.Join(" | ", headers.Select((_, column) => Escape(Cell(row, column)))) + " |");
        }

        return builder.ToString().TrimEnd();
    }

    private static string FormatRow(IReadOnlyList<string> cells, int[] widths) =>
        "| " + string.Join(" | ", widths.Select((width, column) => Cell(cells, column).PadRight(width))) + " |";

    private static string Cell(IReadOnlyList<string> row, int column) => column < row.Count ? row[column] : string.Empty;

    private static string Escape(string value) => value.Replace("|", "\\|");
}
