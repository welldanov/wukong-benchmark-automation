using System.Security.Principal;

namespace WukongBenchmark.Infrastructure;

internal static class Elevation
{
    public static bool IsAdministrator
    {
        get
        {
            using var identity = WindowsIdentity.GetCurrent();
            return new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator);
        }
    }

    public static void Require()
    {
        if (!IsAdministrator)
        {
            throw new BenchmarkException(
                "Нужны права администратора: настройки бенчмарка лежат в Program Files. " +
                "Запустите терминал или VS Code от имени администратора.");
        }
    }
}
