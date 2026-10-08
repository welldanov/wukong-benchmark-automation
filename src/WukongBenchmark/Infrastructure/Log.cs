namespace WukongBenchmark.Infrastructure;

internal static class Log
{
    private static readonly Lock Sync = new();

    public static void Step(string message) => Write(ConsoleColor.Cyan, "==>", message);

    public static void Info(string message) => Write(ConsoleColor.DarkGray, "   ", message);

    public static void Success(string message) => Write(ConsoleColor.Green, " ok", message);

    public static void Warning(string message) => Write(ConsoleColor.Yellow, " !!", message);

    public static void Error(string message) => Write(ConsoleColor.Red, "err", message);

    private static void Write(ConsoleColor color, string marker, string message)
    {
        lock (Sync)
        {
            var previous = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write($"[{DateTime.Now:HH:mm:ss}] ");
            Console.ForegroundColor = color;
            Console.Write($"{marker} ");
            Console.ForegroundColor = previous;
            Console.WriteLine(message);
        }
    }
}
