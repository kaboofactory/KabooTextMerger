using KabooTextMerger.Models;

namespace KabooTextMerger.Services;

public sealed record FileMergeRequest(
    string FilePath,
    TextEncodingOption InputEncodingOption);

