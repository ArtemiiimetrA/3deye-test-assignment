namespace FileSort.Sorter.Options;

public sealed class SortOptions
{
    public const string SectionName = "SortOptions"; 
    
    public string? InputFilePath { get; set; }
    public string? OutputFilePath { get; set; }
    public string? TempDirectory { get; set; }
    public int MaxRamMb { get; set; }
    public int ChunkSizeMb { get; set; }
    public int MaxDegreeOfParallelism { get; set; }
    public string? FileChunkTemplate { get; set; }
    public int BufferSizeBytes { get; set; }
    public bool DeleteTempFiles { get; set; }
    public int MaxOpenFiles { get; set; }
    public bool AdaptiveChunkSize { get; set; }
    public int MinChunkSizeMb { get; set; }
    public int MaxChunkSizeMb { get; set; }

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
        AdaptiveChunkSize = adaptiveChunkSize;
        MinChunkSizeMb = minChunkSizeMb;
        MaxChunkSizeMb = maxChunkSizeMb;
    }
}
