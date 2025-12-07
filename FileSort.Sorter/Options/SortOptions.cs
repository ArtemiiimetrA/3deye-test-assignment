namespace FileSort.Sorter.Options;

public sealed class SortOptions
{
    public const string SectionName = "SortOptions";

    // Parameterless constructor for configuration binding
    public SortOptions()
    {
    }

    // Constructor with validation for explicit instantiation
    public SortOptions(
        string inputFilePath,
        string outputFilePath,
        string tempDirectory,
        int maxRamMb,
        int chunkSizeMb,
        int maxDegreeOfParallelism,
        string fileChunkTemplate,
        int bufferSizeBytes,
        bool deleteTempFiles,
        int maxOpenFiles,
        int maxMergeParallelism,
        bool adaptiveChunkSize,
        int minChunkSizeMb,
        int maxChunkSizeMb)
    {
        InputFilePath = inputFilePath;
        OutputFilePath = outputFilePath;
        TempDirectory = tempDirectory;
        MaxRamMb = maxRamMb;
        ChunkSizeMb = chunkSizeMb;
        MaxDegreeOfParallelism = maxDegreeOfParallelism;
        FileChunkTemplate = fileChunkTemplate;
        BufferSizeBytes = bufferSizeBytes;
        DeleteTempFiles = deleteTempFiles;
        MaxOpenFiles = maxOpenFiles;
        MaxMergeParallelism = maxMergeParallelism;
        AdaptiveChunkSize = adaptiveChunkSize;
        MinChunkSizeMb = minChunkSizeMb;
        MaxChunkSizeMb = maxChunkSizeMb;
    }

    public string? InputFilePath { get; init; }
    public string? OutputFilePath { get; init; }
    public string? TempDirectory { get; init; }
    public int MaxRamMb { get; init; }
    public int ChunkSizeMb { get; init; }
    public int MaxDegreeOfParallelism { get; init; }
    public string? FileChunkTemplate { get; init; }
    public int BufferSizeBytes { get; init; }
    public bool DeleteTempFiles { get; init; }
    public int MaxOpenFiles { get; init; }
    public int MaxMergeParallelism { get; init; }
    public bool AdaptiveChunkSize { get; init; }
    public int MinChunkSizeMb { get; init; }
    public int MaxChunkSizeMb { get; init; }
}