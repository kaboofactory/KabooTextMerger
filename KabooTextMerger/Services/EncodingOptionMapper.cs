using System.Text;
using KabooTextMerger.Models;

namespace KabooTextMerger.Services;

public static class EncodingOptionMapper
{
    private static readonly IReadOnlyList<TextEncodingOption> _inputOptions =
    [
        TextEncodingOption.Auto,
        TextEncodingOption.ShiftJis,
        TextEncodingOption.Cp932,
        TextEncodingOption.Utf8NoBom,
        TextEncodingOption.Utf8Bom
    ];

    private static readonly IReadOnlyList<TextEncodingOption> _outputOptions =
    [
        TextEncodingOption.ShiftJis,
        TextEncodingOption.Cp932,
        TextEncodingOption.Utf8NoBom,
        TextEncodingOption.Utf8Bom
    ];

    public static IReadOnlyList<TextEncodingOption> InputOptions => _inputOptions;

    public static IReadOnlyList<TextEncodingOption> OutputOptions => _outputOptions;

    public static string ToLabel(TextEncodingOption option)
    {
        return option switch
        {
            TextEncodingOption.Auto => "Auto",
            TextEncodingOption.ShiftJis => "Shift_JIS",
            TextEncodingOption.Cp932 => "CP932",
            TextEncodingOption.Utf8NoBom => "UTF-8 (BOMなし)",
            TextEncodingOption.Utf8Bom => "UTF-8 (BOMあり)",
            _ => option.ToString()
        };
    }

    public static Encoding GetOutputEncoding(TextEncodingOption option)
    {
        return option switch
        {
            TextEncodingOption.ShiftJis => Encoding.GetEncoding("shift_jis"),
            TextEncodingOption.Cp932 => Encoding.GetEncoding(932),
            TextEncodingOption.Utf8NoBom => new UTF8Encoding(false),
            TextEncodingOption.Utf8Bom => new UTF8Encoding(true),
            _ => throw new ArgumentOutOfRangeException(nameof(option), option, "出力には Auto を指定できません。")
        };
    }
}

