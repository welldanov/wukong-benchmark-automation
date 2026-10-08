using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Globalization;
using Windows.Graphics.Imaging;
using Windows.Media.Ocr;
using WukongBenchmark.Infrastructure;

namespace WukongBenchmark.Automation.Vision;

internal sealed record TextLine(string Text, ScreenRect Bounds);

internal sealed class TextRecognizer
{
    private const double PreferredUpscale = 2.0;

    private readonly IReadOnlyList<OcrEngine> _engines;

    private TextRecognizer(IReadOnlyList<OcrEngine> engines) => _engines = engines;

    public static TextRecognizer Create(params string[] languageTags)
    {
        var engines = languageTags
            .Select(tag => new Language(tag))
            .Where(OcrEngine.IsLanguageSupported)
            .Select(OcrEngine.TryCreateFromLanguage)
            .OfType<OcrEngine>()
            .ToList();

        if (engines.Count == 0 && OcrEngine.TryCreateFromUserProfileLanguages() is { } fallback)
        {
            engines.Add(fallback);
        }

        if (engines.Count == 0)
        {
            throw new BenchmarkException(
                "В Windows не установлен ни один язык распознавания текста. Добавьте русский или английский языковой пакет.");
        }

        return new TextRecognizer(engines);
    }

    public double ScaleFor(ScreenRect area)
    {
        var longestSide = Math.Max(area.Width, area.Height);
        return longestSide == 0 ? 1 : Math.Min(PreferredUpscale, OcrEngine.MaxImageDimension / (double)longestSide);
    }

    public async Task<IReadOnlyList<TextLine>> RecognizeAsync(CapturedFrame frame, CancellationToken cancellationToken)
    {
        using var bitmap = SoftwareBitmap.CreateCopyFromBuffer(
            frame.Pixels.AsBuffer(), BitmapPixelFormat.Bgra8, frame.Width, frame.Height, BitmapAlphaMode.Ignore);

        var lines = new List<TextLine>();
        foreach (var engine in _engines)
        {
            var result = await engine.RecognizeAsync(bitmap).AsTask(cancellationToken);
            lines.AddRange(result.Lines.Where(line => line.Words.Count > 0).Select(line => ToTextLine(line, frame)));
        }

        return lines;
    }

    private static TextLine ToTextLine(OcrLine line, CapturedFrame frame)
    {
        var words = line.Words.Select(word => word.BoundingRect).ToList();
        var left = words.Min(rect => rect.X);
        var top = words.Min(rect => rect.Y);
        var right = words.Max(rect => rect.X + rect.Width);
        var bottom = words.Max(rect => rect.Y + rect.Height);

        var bounds = new ScreenRect(
            frame.Source.Left + (int)Math.Round(left / frame.Scale),
            frame.Source.Top + (int)Math.Round(top / frame.Scale),
            (int)Math.Round((right - left) / frame.Scale),
            (int)Math.Round((bottom - top) / frame.Scale));

        return new TextLine(line.Text, bounds);
    }
}
