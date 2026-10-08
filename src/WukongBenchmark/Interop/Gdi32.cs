using System.Runtime.InteropServices;

namespace WukongBenchmark.Interop;

internal static class Gdi32
{
    public const int StretchHalftone = 4;
    public const uint SourceCopy = 0x00CC0020;
    public const uint DibRgbColors = 0;

    [StructLayout(LayoutKind.Sequential)]
    public struct BitmapInfoHeader
    {
        public uint Size;
        public int Width;
        public int Height;
        public ushort Planes;
        public ushort BitCount;
        public uint Compression;
        public uint SizeImage;
        public int XPelsPerMeter;
        public int YPelsPerMeter;
        public uint ClrUsed;
        public uint ClrImportant;
    }

    [DllImport("gdi32.dll")]
    public static extern nint CreateCompatibleDC(nint deviceContext);

    [DllImport("gdi32.dll")]
    public static extern nint CreateCompatibleBitmap(nint deviceContext, int width, int height);

    [DllImport("gdi32.dll")]
    public static extern nint SelectObject(nint deviceContext, nint gdiObject);

    [DllImport("gdi32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool DeleteObject(nint gdiObject);

    [DllImport("gdi32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool DeleteDC(nint deviceContext);

    [DllImport("gdi32.dll")]
    public static extern int SetStretchBltMode(nint deviceContext, int mode);

    [DllImport("gdi32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool SetBrushOrgEx(nint deviceContext, int x, int y, nint previous);

    [DllImport("gdi32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool StretchBlt(
        nint destination, int destinationX, int destinationY, int destinationWidth, int destinationHeight,
        nint source, int sourceX, int sourceY, int sourceWidth, int sourceHeight, uint operation);

    [DllImport("gdi32.dll")]
    public static extern int GetDIBits(
        nint deviceContext, nint bitmap, uint startScan, uint scanLines, [Out] byte[] bits,
        ref BitmapInfoHeader info, uint usage);
}
