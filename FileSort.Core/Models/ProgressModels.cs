namespace FileSort.Core.Models;

public sealed class SortProgress
{
    public int ChunksCreated { get; set; }
    public int ChunksMerged { get; set; }
    public long BytesProcessed { get; set; }
    public long TotalBytes { get; set; }
    public int CurrentMergePass { get; set; } // 0 = chunking phase, 1+ = merge passes
    public int TotalMergePasses { get; set; }
    public int CurrentBatchInPass { get; set; }
    public int TotalBatchesInPass { get; set; }
}

public sealed class GeneratorProgress
{
    public long BytesWritten { get; set; }
    public long TargetBytes { get; set; }
    public long LinesWritten { get; set; }
}
