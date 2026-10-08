using System.ComponentModel;
using System.Runtime.InteropServices;
using WukongBenchmark.Interop;

namespace WukongBenchmark.Automation.Vision;

internal sealed record CapturedFrame(byte[] Pixels, int Width, int Height, ScreenRect Source, double Scale);

internal static class ScreenCapture
{
    private const int BytesPerPixel = 4;

    public static CapturedFrame Capture(ScreenRect area, double scale)
    {
        var width = (int)Math.Round(area.Width * scale);
        var height = (int)Math.Round(area.Height * scale);

        var screen = User32.GetDC(0);
        var memory = Gdi32.CreateCompatibleDC(screen);
        var bitmap = Gdi32.CreateCompatibleBitmap(screen, width, height);
        try
        {
            var previous = Gdi32.SelectObject(memory, bitmap);
            Gdi32.SetStretchBltMode(memory, Gdi32.StretchHalftone);
            Gdi32.SetBrushOrgEx(memory, 0, 0, 0);
            var copied = Gdi32.StretchBlt(
                memory, 0, 0, width, height,
                screen, area.Left, area.Top, area.Width, area.Height,
                Gdi32.SourceCopy);
            var error = Marshal.GetLastWin32Error();
            Gdi32.SelectObject(memory, previous);

            if (!copied)
            {
                throw new Win32Exception(error, "Не удалось сделать снимок окна бенчмарка.");
            }

            return new CapturedFrame(ReadPixels(screen, bitmap, width, height), width, height, area, scale);
        }
        finally
        {
            Gdi32.DeleteObject(bitmap);
            Gdi32.DeleteDC(memory);
            User32.ReleaseDC(0, screen);
        }
    }

    private static byte[] ReadPixels(nint deviceContext, nint bitmap, int width, int height)
    {
        var header = new Gdi32.BitmapInfoHeader
        {
            Size = (uint)Marshal.SizeOf<Gdi32.BitmapInfoHeader>(),
            Width = width,
            Height = -height,
            Planes = 1,
            BitCount = 32,
        };

        var pixels = new byte[width * height * BytesPerPixel];
        Gdi32.GetDIBits(deviceContext, bitmap, 0, (uint)height, pixels, ref header, Gdi32.DibRgbColors);
        return pixels;
    }
}
