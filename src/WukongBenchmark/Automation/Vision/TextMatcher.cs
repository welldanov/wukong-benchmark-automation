using System.Text;

namespace WukongBenchmark.Automation.Vision;

internal static class TextMatcher
{
    private const double DefaultThreshold = 0.8;

    public static bool IsSimilar(string text, string phrase, double threshold = DefaultThreshold) =>
        Similarity(Normalize(text), Normalize(phrase)) >= threshold;

    public static bool ContainsSimilar(string text, string phrase, double threshold = DefaultThreshold)
    {
        var words = Normalize(text).Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var target = Normalize(phrase);
        var windowSize = target.Split(' ').Length;

        if (words.Length <= windowSize)
        {
            return Similarity(string.Join(' ', words), target) >= threshold;
        }

        for (var start = 0; start + windowSize <= words.Length; start++)
        {
            if (Similarity(string.Join(' ', words, start, windowSize), target) >= threshold)
            {
                return true;
            }
        }

        return false;
    }

    private static string Normalize(string text)
    {
        var builder = new StringBuilder(text.Length);
        foreach (var symbol in text.ToLowerInvariant().Replace('ё', 'е'))
        {
            builder.Append(char.IsLetterOrDigit(symbol) ? symbol : ' ');
        }

        return string.Join(' ', builder.ToString().Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }

    private static double Similarity(string first, string second)
    {
        var longest = Math.Max(first.Length, second.Length);
        return longest == 0 ? 1 : 1 - (double)Distance(first, second) / longest;
    }

    private static int Distance(string first, string second)
    {
        var previous = new int[second.Length + 1];
        var current = new int[second.Length + 1];
        for (var j = 0; j <= second.Length; j++)
        {
            previous[j] = j;
        }

        for (var i = 1; i <= first.Length; i++)
        {
            current[0] = i;
            for (var j = 1; j <= second.Length; j++)
            {
                var substitution = first[i - 1] == second[j - 1] ? 0 : 1;
                current[j] = Math.Min(Math.Min(current[j - 1] + 1, previous[j] + 1), previous[j - 1] + substitution);
            }

            (previous, current) = (current, previous);
        }

        return previous[second.Length];
    }
}
