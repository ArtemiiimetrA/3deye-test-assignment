using FileSort.Core.Models;

namespace FileSort.Generator.Formatters;

/// <summary>
/// Formats generator progress information for display.
/// </summary>
public static class GeneratorProgressFormatter
{
    /// <summary>
    /// Formats generator progress as a string.
    /// </summary>
    public static string Format(GeneratorProgress progress)
    {
        double percent = progress.TargetBytes > 0 
            ? (double)progress.BytesWritten / progress.TargetBytes * 100 
            : 0;
        
        return $"Progress: {percent:F2}% ({progress.BytesWritten:N0} / {progress.TargetBytes:N0} bytes, {progress.LinesWritten:N0} lines)";
    }
}

