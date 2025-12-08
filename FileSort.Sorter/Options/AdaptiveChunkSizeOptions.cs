namespace FileSort.Sorter.Options;

/// <summary>
///     Configuration options for adaptive chunk sizing.
/// </summary>
public sealed class AdaptiveChunkSizeOptions
{
    // Parameterless constructor for configuration binding
    public AdaptiveChunkSizeOptions()
    {
    }

    /// <summary>
    ///     Enables adaptive chunk sizing based on memory pressure.
    ///     When enabled, chunk sizes will vary between MinChunkSizeMb and MaxChunkSizeMb.
    /// </summary>
    public bool Enabled { get; init; }

    /// <summary>
    ///     Minimum chunk size (in megabytes) when using adaptive chunk sizing.
    ///     Used only when Enabled is true.
    /// </summary>
    public int MinChunkSizeMb { get; init; }

    /// <summary>
    ///     Maximum chunk size (in megabytes) when using adaptive chunk sizing.
    ///     Used only when Enabled is true.
    /// </summary>
    public int MaxChunkSizeMb { get; init; }
}

