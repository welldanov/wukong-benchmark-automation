using WukongBenchmark.Interop;

namespace WukongBenchmark.Automation;

internal sealed class GameWindow(nint handle)
{
    private static readonly TimeSpan ActivationDelay = TimeSpan.FromMilliseconds(300);

    public bool Exists => User32.IsWindow(handle);

    public bool IsForeground => User32.GetForegroundWindow() == handle;

    public ScreenRect GetClientBounds()
    {
        if (!User32.GetClientRect(handle, out var rect))
        {
            return default;
        }

        var origin = new User32.Point();
        User32.ClientToScreen(handle, ref origin);
        return new ScreenRect(origin.X, origin.Y, rect.Right - rect.Left, rect.Bottom - rect.Top);
    }

    public bool EnsureForeground()
    {
        if (IsForeground)
        {
            return true;
        }

        if (User32.IsIconic(handle))
        {
            User32.ShowWindow(handle, User32.ShowRestore);
        }

        if (!User32.SetForegroundWindow(handle))
        {
            InputSimulator.PressKey(VirtualKey.Menu);
            User32.SetForegroundWindow(handle);
        }

        Thread.Sleep(ActivationDelay);
        return IsForeground;
    }
}
