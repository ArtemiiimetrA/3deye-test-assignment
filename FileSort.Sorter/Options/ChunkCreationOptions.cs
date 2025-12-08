namespace FileSort.Sorter.Options;

/// <summary>
///     Configuration options for the chunk creation phase.
/// </summary>
public sealed class ChunkCreationOptions
{
    // Parameterless constructor for configuration binding
    public ChunkCreationOptions()
    {
        Adaptive = new AdaptiveChunkSizeOptions();
    }

    /// <summary>
    ///     Maximum amount of RAM (in megabytes) that the sorting process should use.
    /// </summary>
    public int MaxRamMb { get; init; }

    /// <summary>
    ///     Size of each chunk (in megabytes) when using fixed chunk size mode.
    /// </summary>
    public int ChunkSizeMb { get; init; }

    /// <summary>
    ///     Maximum number of concurrent chunk processing operations.
    /// </summary>
    public int MaxDegreeOfParallelism { get; init; }

    /// <summary>
    ///     Template string for naming temporary chunk files.
    ///     Uses string formatting with {0} as the chunk index placeholder.
    /// </summary>
    public string? FileChunkTemplate { get; init; }

    /// <summary>
    ///     Options for adaptive chunk sizing.
    /// </summary>
    public AdaptiveChunkSizeOptions Adaptive { get; init; } = new();
}

