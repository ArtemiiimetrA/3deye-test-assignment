namespace FileSort.Sorter.Options;

/// <summary>
///     Configuration options for file paths and temporary file management.
/// </summary>
public sealed class FileOptions
{
    // Parameterless constructor for configuration binding
    public FileOptions()
    {
    }

    /// <summary>
    ///     Path to the input file that will be sorted.
    /// </summary>
    public string? InputFilePath { get; init; }

    /// <summary>
    ///     Path where the sorted output file will be written.
    /// </summary>
    public string? OutputFilePath { get; init; }

    /// <summary>
    ///     Directory path where temporary chunk files will be created during the sorting process.
    /// </summary>
    public string? TempDirectory { get; init; }

    /// <summary>
    ///     Whether to automatically delete temporary chunk files after the sorting operation completes.
    /// </summary>
    public bool DeleteTempFiles { get; init; } = true;
}

