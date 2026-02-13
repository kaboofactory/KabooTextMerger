using KabooTextMerger.Models;

namespace KabooTextMerger.Services;

public static class LineEndingOptionMapper
{
    private static readonly IReadOnlyList<LineEndingChoice> _choices =
    [
        new(LineEndingOption.Crlf, "CRLF"),
        new(LineEndingOption.Lf, "LF")
    ];

    public static IReadOnlyList<LineEndingChoice> Choices => _choices;

    public static string GetLineEnding(LineEndingOption option)
    {
        return option switch
        {
            LineEndingOption.Crlf => "\r\n",
            LineEndingOption.Lf => "\n",
            _ => throw new ArgumentOutOfRangeException(nameof(option), option, "未対応の改行コードです。")
        };
    }
}

public sealed record LineEndingChoice(LineEndingOption Option, string Label);
