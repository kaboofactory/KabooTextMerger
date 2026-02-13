using System.Text;
using KabooTextMerger.Models;

namespace KabooTextMerger.Services;

public static class TextEncodingDetector
{
    private static readonly UTF8Encoding Utf8Strict = new(false, true);
    private static readonly Encoding Cp932Strict = Encoding.GetEncoding(
        932,
        EncoderFallback.ExceptionFallback,
        DecoderFallback.ExceptionFallback);
    private static readonly Encoding ShiftJisStrict = Encoding.GetEncoding(
        "shift_jis",
        EncoderFallback.ExceptionFallback,
        DecoderFallback.ExceptionFallback);

    public static EncodingResolution ResolveForRead(byte[] bytes, TextEncodingOption preferredOption)
    {
        return preferredOption == TextEncodingOption.Auto
            ? ResolveAutomatically(bytes)
            : ResolveManual(preferredOption);
    }

    public static string Decode(byte[] bytes, EncodingResolution resolution)
    {
        ReadOnlySpan<byte> source = bytes;
        if (resolution.Option == TextEncodingOption.Utf8Bom && HasUtf8Bom(source))
        {
            source = source[3..];
        }

        return resolution.Encoding.GetString(source);
    }

    private static EncodingResolution ResolveAutomatically(byte[] bytes)
    {
        var data = bytes.AsSpan();

        if (HasUtf8Bom(data))
        {
            return new EncodingResolution(
                TextEncodingOption.Utf8Bom,
                "UTF-8 (BOMあり)",
                "BOM検出",
                Utf8Strict);
        }

        if (CanDecode(Utf8Strict, data))
        {
            return new EncodingResolution(
                TextEncodingOption.Utf8NoBom,
                "UTF-8 (BOMなし)",
                "UTF-8妥当",
                Utf8Strict);
        }

        var cp932Valid = CanDecode(Cp932Strict, data);
        var shiftJisValid = CanDecode(ShiftJisStrict, data);

        if (cp932Valid && shiftJisValid)
        {
            return new EncodingResolution(
                TextEncodingOption.Cp932,
                "CP932",
                "CP932/Shift_JIS判定困難のためCP932採用",
                Cp932Strict);
        }

        if (cp932Valid)
        {
            return new EncodingResolution(
                TextEncodingOption.Cp932,
                "CP932",
                "CP932として妥当",
                Cp932Strict);
        }

        if (shiftJisValid)
        {
            return new EncodingResolution(
                TextEncodingOption.ShiftJis,
                "Shift_JIS",
                "Shift_JISとして妥当",
                ShiftJisStrict);
        }

        return new EncodingResolution(
            TextEncodingOption.Cp932,
            "CP932",
            "判定不能のためCP932フォールバック",
            Cp932Strict);
    }

    private static EncodingResolution ResolveManual(TextEncodingOption option)
    {
        return option switch
        {
            TextEncodingOption.ShiftJis => new EncodingResolution(
                option,
                "Shift_JIS",
                "ユーザー指定",
                ShiftJisStrict),
            TextEncodingOption.Cp932 => new EncodingResolution(
                option,
                "CP932",
                "ユーザー指定",
                Cp932Strict),
            TextEncodingOption.Utf8NoBom => new EncodingResolution(
                option,
                "UTF-8 (BOMなし)",
                "ユーザー指定",
                Utf8Strict),
            TextEncodingOption.Utf8Bom => new EncodingResolution(
                option,
                "UTF-8 (BOMあり)",
                "ユーザー指定",
                Utf8Strict),
            TextEncodingOption.Auto => throw new ArgumentOutOfRangeException(nameof(option), option, "Auto は自動判定でのみ利用可能です。"),
            _ => throw new ArgumentOutOfRangeException(nameof(option), option, "未対応の文字コードです。")
        };
    }

    private static bool HasUtf8Bom(ReadOnlySpan<byte> data)
    {
        return data.Length >= 3 &&
               data[0] == 0xEF &&
               data[1] == 0xBB &&
               data[2] == 0xBF;
    }

    private static bool CanDecode(Encoding encoding, ReadOnlySpan<byte> data)
    {
        try
        {
            _ = encoding.GetString(data);
            return true;
        }
        catch (DecoderFallbackException)
        {
            return false;
        }
    }
}

