using System.Runtime.InteropServices;

namespace WukongBenchmark.Infrastructure;

internal static class ConsoleSession
{
    public static bool OwnsConsoleWindow
    {
        get
        {
            var processes = new uint[2];
            return GetConsoleProcessList(processes, (uint)processes.Length) == 1;
        }
    }

    public static void WaitForKeyIfStandalone()
    {
        if (!OwnsConsoleWindow || Console.IsInputRedirected)
        {
            return;
        }

        Console.WriteLine();
        Console.Write("Нажмите любую клавишу, чтобы закрыть окно...");
        Console.ReadKey(intercept: true);
    }

    [DllImport("kernel32.dll")]
    private static extern uint GetConsoleProcessList(uint[] processList, uint count);
}
