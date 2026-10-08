using System.Text;

namespace WukongBenchmark.Reporting;

internal static class MarkdownReportRenderer
{
    public static string Save(IReadOnlyList<ReportSection> sections, DateTimeOffset createdAt, string path)
    {
        var builder = new StringBuilder()
            .AppendLine("# Black Myth: Wukong Benchmark — отчёт")
            .AppendLine()
            .AppendLine($"Дата: {createdAt:dd.MM.yyyy HH:mm}");

        foreach (var section in sections)
        {
            builder.AppendLine().AppendLine($"## {section.Title}").AppendLine();

            if (section.Table is not null)
            {
                builder.AppendLine(section.Table.RenderMarkdown()).AppendLine();
            }

            foreach (var note in section.Notes)
            {
                builder.AppendLine($"- {note}");
            }
        }

        File.WriteAllText(path, builder.ToString(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        return path;
    }
}
