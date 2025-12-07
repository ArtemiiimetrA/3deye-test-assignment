namespace FileSort.Core.Models.Progress;

public sealed class GeneratorProgress
{
    /// <summary>
    /// Gets or sets the total number of bytes written to the output file.
    /// </summary>
    public long BytesWritten { get; set; }
    
    /// <summary>
    /// Gets or sets the target size in bytes for the generated file.
    /// </summary>
    public long TargetBytes { get; set; }
    
    /// <summary>
    /// Gets or sets the total number of lines written to the output file.
    /// </summary>
    public long LinesWritten { get; set; }
}