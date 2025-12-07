namespace FileSort.Core.Requests;

/// <summary>
///     Data Transfer Object for file sorting requests.
///     Contains runtime parameters merged from configuration and command-line arguments.
/// </summary>
public sealed record SortRequest
{
    public string InputFilePath { get; init; } = null!;
    public string OutputFilePath { get; init; } = null!;
    public string TempDirectory { get; init; } = null!;
    public int MaxRamMb { get; init; }
    public int ChunkSizeMb { get; init; }
    public int MaxDegreeOfParallelism { get; init; }
    public string FileChunkTemplate { get; init; } = null!;
    public int BufferSizeBytes { get; init; }
    public bool DeleteTempFiles { get; init; }
    public int MaxOpenFiles { get; init; }
    public int MaxMergeParallelism { get; init; } = 1;
    public bool AdaptiveChunkSize { get; init; }
    public int MinChunkSizeMb { get; init; }
    public int MaxChunkSizeMb { get; init; }
}