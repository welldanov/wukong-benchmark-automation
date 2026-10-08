using System.Runtime.InteropServices;
using WukongBenchmark.Interop;

namespace WukongBenchmark.Automation;

internal enum VirtualKey : ushort
{
    Return = 0x0D,
    Menu = 0x12,
    Escape = 0x1B,
}

internal static class InputSimulator
{
    private static readonly TimeSpan KeyHoldTime = TimeSpan.FromMilliseconds(60);

    public static void PressKey(VirtualKey key)
    {
        var scanCode = (ushort)User32.MapVirtualKey((uint)key, User32.MapVirtualKeyToScanCode);
        Send(KeyboardEvent(key, scanCode, 0));
        Thread.Sleep(KeyHoldTime);
        Send(KeyboardEvent(key, scanCode, User32.KeyEventKeyUp));
    }

    public static void MoveCursor(ScreenPoint point)
    {
        User32.SetCursorPos(point.X, point.Y);
        Send(new User32.Input
        {
            Type = User32.InputMouse,
            Data = new User32.InputUnion { Mouse = new User32.MouseInput { Flags = User32.MouseEventMove } },
        });
    }

    private static User32.Input KeyboardEvent(VirtualKey key, ushort scanCode, uint flags) => new()
    {
        Type = User32.InputKeyboard,
        Data = new User32.InputUnion
        {
            Keyboard = new User32.KeyboardInput { VirtualKey = (ushort)key, ScanCode = scanCode, Flags = flags },
        },
    };

    private static void Send(User32.Input input) =>
        User32.SendInput(1, [input], Marshal.SizeOf<User32.Input>());
}
