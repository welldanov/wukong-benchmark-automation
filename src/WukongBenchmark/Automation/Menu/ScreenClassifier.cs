using WukongBenchmark.Automation.Vision;

namespace WukongBenchmark.Automation.Menu;

internal static class ScreenClassifier
{
    private static readonly string[] PressAnyKeyPhrases =
        ["нажмите любую кнопку", "нажмите любую клавишу", "press any key", "press any button"];

    private static readonly string[] StartBenchmarkPhrases = ["тест быстродействия", "benchmark"];

    private static readonly string[] SettingsPhrases = ["настройки", "settings"];

    private static readonly string[] ConfirmPhrases = ["подтвердить", "confirm"];

    public static ScreenObservation Classify(IReadOnlyList<TextLine> lines)
    {
        if (FindButton(lines, ConfirmPhrases) is { } confirm)
        {
            return new ScreenObservation(MenuScreen.ConfirmDialog, confirm, lines);
        }

        if (FindButton(lines, StartBenchmarkPhrases) is { } start && FindButton(lines, SettingsPhrases) is not null)
        {
            return new ScreenObservation(MenuScreen.MainMenu, start, lines);
        }

        var pressAnyKey = lines.FirstOrDefault(line =>
            PressAnyKeyPhrases.Any(phrase => TextMatcher.ContainsSimilar(line.Text, phrase)));

        return pressAnyKey is not null
            ? new ScreenObservation(MenuScreen.PressAnyKey, pressAnyKey, lines)
            : new ScreenObservation(MenuScreen.Unknown, null, lines);
    }

    private static TextLine? FindButton(IReadOnlyList<TextLine> lines, IReadOnlyList<string> phrases) =>
        lines.FirstOrDefault(line => phrases.Any(phrase => TextMatcher.IsSimilar(line.Text, phrase)));
}
