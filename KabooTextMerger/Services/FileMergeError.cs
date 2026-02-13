namespace KabooTextMerger.Services;

public sealed record FileMergeError(
    string FilePath,
    string Message);

