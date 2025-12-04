namespace FileSort.Core.Options;

public sealed class SortOptions
{
    public const string SectionName = "SortOptions"; 
    
    public string InputFilePath { get; set; } = string.Empty;
    public string OutputFilePath { get; set; } = string.Empty;
    public string TempDirectory { get; set; } = "temp";
    public int MaxRamMb { get; set; } = 2048;
    public int ChunkSizeMb { get; set; } = 256;
    public int MaxDegreeOfParallelism { get; set; } = Environment.ProcessorCount;
    public string FileChunkTemplate { get; set; } = "chunk_{0:0000}.tmp";
    public int BufferSizeBytes { get; set; } = 4 * 1024 * 1024; // 4MB default (optimized)
    public bool DeleteTempFiles { get; set; } = true;
    public int MaxOpenFiles { get; set; } = 500; // Maximum files to open simultaneously for merge
    public bool AdaptiveChunkSize { get; set; } = true;
    public int MinChunkSizeMb { get; set; } = 64;
    public int MaxChunkSizeMb { get; set; } = 512;
}
