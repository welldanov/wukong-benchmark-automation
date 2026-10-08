using System.Globalization;

namespace WukongBenchmark.Infrastructure;

internal static class Units
{
    private const double BytesInGigabyte = 1024d * 1024 * 1024;

    public static string Gigabytes(long bytes) =>
        (bytes / BytesInGigabyte).ToString("0.#", CultureInfo.InvariantCulture) + " ГБ";

    public static string Number(double value, string format = "0.#") =>
        double.IsNaN(value) ? "—" : value.ToString(format, CultureInfo.InvariantCulture);

    public static string Percent(double share) =>
        double.IsNaN(share) ? "—" : (share * 100).ToString("0", CultureInfo.InvariantCulture) + " %";
}
