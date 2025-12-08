namespace FileSort.Sorter.Options;

/// <summary>
///     Configuration options for the merge phase.
/// </summary>
public sealed class MergeOptions
{
    // Parameterless constructor for configuration binding
    public MergeOptions()
    {
    }

    /// <summary>
    ///     Size of the I/O buffer (in bytes) used for reading and writing files.
    ///     Used in both Single-Pass and Multi-Pass merge strategies.
    /// </summary>
    public int BufferSizeBytes { get; init; }

    /// <summary>
    ///     Maximum number of file handles that can be open simultaneously during the merge phase.
    ///     Determines which merge strategy is used:
    ///     - If number of chunks ≤ MaxOpenFiles: Single-Pass Merge is used
    ///     - If number of chunks > MaxOpenFiles: Multi-Pass Merge is used
    ///     Used in Multi-Pass merge strategy only.
    /// </summary>
    public int MaxOpenFiles { get; init; }

    /// <summary>
    ///     Maximum number of concurrent merge operations during the merge phase.
    ///     Used in Multi-Pass merge strategy only.
    /// </summary>
    public int MaxMergeParallelism { get; init; } = 1;
}

