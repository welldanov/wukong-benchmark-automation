using WukongBenchmark.Automation.Vision;

namespace WukongBenchmark.Automation.Menu;

internal enum MenuScreen
{
    Unknown,
    PressAnyKey,
    MainMenu,
    ConfirmDialog,
}

internal sealed record ScreenObservation(MenuScreen Screen, TextLine? Target, IReadOnlyList<TextLine> Lines)
{
    public string VisibleText => Lines.Count == 0 ? "(текст не найден)" : string.Join(" | ", Lines.Select(line => line.Text));
}
