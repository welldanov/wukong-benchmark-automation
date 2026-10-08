using WukongBenchmark.Automation.Vision;
using WukongBenchmark.Infrastructure;

namespace WukongBenchmark.Automation.Menu;

internal sealed class MenuNavigator(TextRecognizer recognizer, string diagnosticsDirectory)
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan HoverDelay = TimeSpan.FromMilliseconds(400);
    private static readonly TimeSpan ActionDelay = TimeSpan.FromSeconds(2);
    private static readonly TimeSpan ConfirmationGracePeriod = TimeSpan.FromSeconds(10);

    public async Task StartBenchmarkAsync(
        GameWindow window,
        Func<bool> isGameAlive,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        var deadline = DateTime.UtcNow + timeout;
        DateTime? confirmedAt = null;
        MenuScreen? reportedScreen = null;
        CapturedFrame? lastFrame = null;
        ScreenObservation? lastObservation = null;

        while (DateTime.UtcNow < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();
            EnsureGameAlive(isGameAlive);

            if (!window.EnsureForeground())
            {
                await Task.Delay(PollInterval, cancellationToken);
                continue;
            }

            var bounds = window.GetClientBounds();
            lastFrame = ScreenCapture.Capture(bounds, recognizer.ScaleFor(bounds));
            lastObservation = ScreenClassifier.Classify(await recognizer.RecognizeAsync(lastFrame, cancellationToken));

            if (lastObservation.Screen != reportedScreen)
            {
                Log.Info(Describe(lastObservation.Screen));
                reportedScreen = lastObservation.Screen;
            }

            var recentlyConfirmed = confirmedAt is { } time && DateTime.UtcNow - time < ConfirmationGracePeriod;

            switch (lastObservation.Screen)
            {
                case MenuScreen.Unknown when confirmedAt is not null:
                    return;

                case MenuScreen.PressAnyKey:
                    InputSimulator.PressKey(VirtualKey.Return);
                    await Task.Delay(ActionDelay, cancellationToken);
                    break;

                case MenuScreen.MainMenu when !recentlyConfirmed:
                    InputSimulator.MoveCursor(lastObservation.Target!.Bounds.Center);
                    await Task.Delay(HoverDelay, cancellationToken);
                    InputSimulator.PressKey(VirtualKey.Return);
                    await Task.Delay(ActionDelay, cancellationToken);
                    break;

                case MenuScreen.ConfirmDialog:
                    InputSimulator.PressKey(VirtualKey.Return);
                    InputSimulator.MoveCursor(bounds.BottomRight);
                    confirmedAt = DateTime.UtcNow;
                    await Task.Delay(ActionDelay, cancellationToken);
                    break;

                default:
                    await Task.Delay(PollInterval, cancellationToken);
                    break;
            }
        }

        var snapshotPath = await SaveSnapshotAsync(lastFrame, cancellationToken);
        throw new BenchmarkException(
            $"Не удалось запустить тест за {timeout.TotalMinutes:0} мин. " +
            $"Последний распознанный текст: {lastObservation?.VisibleText ?? "нет"}." +
            (snapshotPath is null ? string.Empty : $" Снимок экрана: {snapshotPath}"));
    }

    private static void EnsureGameAlive(Func<bool> isGameAlive)
    {
        if (!isGameAlive())
        {
            throw new BenchmarkException("Процесс бенчмарка завершился до начала теста.");
        }
    }

    private async Task<string?> SaveSnapshotAsync(CapturedFrame? frame, CancellationToken cancellationToken)
    {
        if (frame is null)
        {
            return null;
        }

        var path = Path.Combine(diagnosticsDirectory, $"menu-timeout-{DateTime.Now:HHmmss}.png");
        await FrameSnapshot.SaveAsync(frame, path, cancellationToken);
        return path;
    }

    private static string Describe(MenuScreen screen) => screen switch
    {
        MenuScreen.PressAnyKey => "Экран «Нажмите любую кнопку»",
        MenuScreen.MainMenu => "Главное меню, выбираю «Тест быстродействия»",
        MenuScreen.ConfirmDialog => "Диалог подтверждения, подтверждаю запуск",
        _ => "Ожидание: загрузка или заставка",
    };
}
