using Windows.Graphics.Imaging;
using Windows.Storage.Streams;

namespace WukongBenchmark.Automation.Vision;

internal static class FrameSnapshot
{
    private const double Dpi = 96;

    public static async Task SaveAsync(CapturedFrame frame, string path, CancellationToken cancellationToken)
    {
        using var stream = new InMemoryRandomAccessStream();
        var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, stream).AsTask(cancellationToken);
        encoder.SetPixelData(
            BitmapPixelFormat.Bgra8, BitmapAlphaMode.Ignore,
            (uint)frame.Width, (uint)frame.Height, Dpi, Dpi, frame.Pixels);
        await encoder.FlushAsync().AsTask(cancellationToken);

        stream.Seek(0);
        await using var file = File.Create(path);
        await stream.AsStreamForRead().CopyToAsync(file, cancellationToken);
    }
}
