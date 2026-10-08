namespace WukongBenchmark.Reporting;

internal static class ConsoleReportRenderer
{
    public static void Render(IReadOnlyList<ReportSection> sections)
    {
        foreach (var section in sections)
        {
            Console.WriteLine();
            WriteTitle(section.Title);

            if (section.Table is not null)
            {
                Console.WriteLine(section.Table.RenderPlain());
            }

            foreach (var note in section.Notes)
            {
                Console.WriteLine($"  {note}");
            }
        }

        Console.WriteLine();
    }

    private static void WriteTitle(string title)
    {
        var previous = Console.ForegroundColor;
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine(title.ToUpperInvariant());
        Console.ForegroundColor = previous;
    }
}
