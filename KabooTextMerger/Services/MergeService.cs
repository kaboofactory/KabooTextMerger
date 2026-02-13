using System.IO;
using System.Text;
using KabooTextMerger.Models;

namespace KabooTextMerger.Services;

public sealed class MergeService
{
    public MergeResult MergeAndWrite(
        IReadOnlyList<FileMergeRequest> requests,
        string outputPath,
        TextEncodingOption outputEncodingOption,
        LineEndingOption outputLineEndingOption)
    {
        if (requests.Count == 0)
        {
            throw new InvalidOperationException("マージ対象ファイルがありません。");
        }

        var merged = new StringBuilder();
        var errors = new List<FileMergeError>();
        var successCount = 0;

        foreach (var request in requests)
        {
            try
            {
                var bytes = File.ReadAllBytes(request.FilePath);
                var resolution = TextEncodingDetector.ResolveForRead(bytes, request.InputEncodingOption);
                var text = TextEncodingDetector.Decode(bytes, resolution);

                merged.Append(text);
                successCount++;
            }
            catch (Exception ex)
            {
                errors.Add(new FileMergeError(request.FilePath, ex.Message));
            }
        }

        if (successCount == 0)
        {
            throw new InvalidOperationException("すべての入力ファイルで読み込みに失敗しました。");
        }

        var outputEncoding = EncodingOptionMapper.GetOutputEncoding(outputEncodingOption);
        var normalizedText = NormalizeLineEndings(merged.ToString(), outputLineEndingOption);
        File.WriteAllText(outputPath, normalizedText, outputEncoding);

        return new MergeResult(
            successCount,
            errors.Count,
            errors);
    }

    private static string NormalizeLineEndings(string input, LineEndingOption option)
    {
        var normalizedLf = input
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace("\r", "\n", StringComparison.Ordinal);

        if (option == LineEndingOption.Lf)
        {
            return normalizedLf;
        }

        return normalizedLf.Replace("\n", "\r\n", StringComparison.Ordinal);
    }
}

