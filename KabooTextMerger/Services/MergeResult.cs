namespace KabooTextMerger.Services;

public sealed record MergeResult(
    int SuccessCount,
    int FailureCount,
    IReadOnlyList<FileMergeError> Errors);

