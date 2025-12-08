namespace FileSort.Sorter.Options;

/// <summary>
///     Configuration settings for the file sorting operation.
/// </summary>
public sealed class SortOptions
{
    public const string SectionName = "SortOptions";

    // Parameterless constructor for configuration binding
    public SortOptions()
    {
        Files = new FileOptions();
        ChunkCreation = new ChunkCreationOptions();
        Merge = new MergeOptions();
    }

    /// <summary>
    ///     Options for file paths and temporary file management.
    /// </summary>
    public FileOptions Files { get; init; } = new();

    /// <summary>
    ///     Options for the chunk creation phase.
    /// </summary>
    public ChunkCreationOptions ChunkCreation { get; init; } = new();

    /// <summary>
    ///     Options for the merge phase.
    /// </summary>
    public MergeOptions Merge { get; init; } = new();
}