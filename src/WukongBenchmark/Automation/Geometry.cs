namespace WukongBenchmark.Automation;

internal readonly record struct ScreenPoint(int X, int Y);

internal readonly record struct ScreenRect(int Left, int Top, int Width, int Height)
{
    public ScreenPoint Center => new(Left + Width / 2, Top + Height / 2);

    public ScreenPoint BottomRight => new(Left + Width - 1, Top + Height - 1);
}
