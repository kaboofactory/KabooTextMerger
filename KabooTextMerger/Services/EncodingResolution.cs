using System.Text;
using KabooTextMerger.Models;

namespace KabooTextMerger.Services;

public sealed record EncodingResolution(
    TextEncodingOption Option,
    string Label,
    string Reason,
    Encoding Encoding);

