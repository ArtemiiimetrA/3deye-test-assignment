namespace FileSort.Core.Requests;

/// <summary>
/// Data Transfer Object for file generation requests.
/// Contains runtime parameters merged from configuration and command-line arguments.
/// </summary>
public sealed record GeneratorRequest
{
    public string OutputFilePath { get; init; } = null!;
    public long TargetSizeBytes { get; init; }
    public int MinNumber { get; init; }
    public int MaxNumber { get; init; }
    public int DuplicateRatioPercent { get; init; }
    public int BufferSizeBytes { get; init; }
    public int? Seed { get; init; }
}

