using WukongBenchmark.Infrastructure;
using WukongBenchmark.Settings;

namespace WukongBenchmark.App;

internal sealed record CommandLineOptions(
    string? InstallDirectory,
    string OutputDirectory,
    IReadOnlyList<string> ProfileIds,
    bool ShowHelp)
{
    public const string Usage =
        """
        Использование: WukongBenchmark [параметры]

          --install-dir <папка>   папка Black Myth: Wukong Benchmark Tool (по умолчанию ищется в библиотеках Steam)
          --output <папка>        куда сохранить отчёт (по умолчанию ./results)
          --only <cpu|gpu>        выполнить только один проход
          -h, --help              показать эту справку
        """;

    public static CommandLineOptions Parse(IReadOnlyList<string> args)
    {
        string? installDirectory = null;
        var outputDirectory = Path.Combine(Environment.CurrentDirectory, "results");
        var profileIds = GraphicsProfiles.AllIds;
        var showHelp = false;

        for (var i = 0; i < args.Count; i++)
        {
            switch (args[i])
            {
                case "--install-dir":
                    installDirectory = ValueAfter(args, ref i);
                    break;
                case "--output":
                    outputDirectory = Path.GetFullPath(ValueAfter(args, ref i));
                    break;
                case "--only":
                    var id = ValueAfter(args, ref i).ToLowerInvariant();
                    profileIds = GraphicsProfiles.AllIds.Where(known => known == id).ToList();
                    if (profileIds.Count == 0)
                    {
                        throw new BenchmarkException($"Неизвестный проход «{id}». Допустимо: cpu, gpu.");
                    }

                    break;
                case "-h" or "--help":
                    showHelp = true;
                    break;
                default:
                    throw new BenchmarkException($"Неизвестный параметр «{args[i]}».");
            }
        }

        return new CommandLineOptions(installDirectory, outputDirectory, profileIds, showHelp);
    }

    private static string ValueAfter(IReadOnlyList<string> args, ref int index)
    {
        if (index + 1 >= args.Count)
        {
            throw new BenchmarkException($"После «{args[index]}» нужно указать значение.");
        }

        return args[++index];
    }
}
