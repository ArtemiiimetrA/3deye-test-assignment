namespace FileSort.Core.Models.Progress;

public sealed class SortProgress
{
    /// <summary>
    /// Gets or sets the number of sorted chunks created during the chunking phase.
    /// </summary>
    public int ChunksCreated { get; set; }
    
    /// <summary>
    /// Gets or sets the number of chunks merged during the merge phase.
    /// </summary>
    public int ChunksMerged { get; set; }
    
    /// <summary>
    /// Gets or sets the current merge pass number. Null during chunking phase, 1+ during merge passes.
    /// </summary>
    public int? CurrentMergePass { get; set; }
    
    /// <summary>
    /// Gets or sets the total number of merge passes required to complete the sort operation.
    /// </summary>
    public int TotalMergePasses { get; set; }
    
    /// <summary>
    /// Gets or sets the current batch number within the current merge pass (1-based).
    /// </summary>
    public int CurrentBatchInPass { get; set; }
    
    /// <summary>
    /// Gets or sets the total number of batches in the current merge pass.
    /// </summary>
    public int TotalBatchesInPass { get; set; }
}


