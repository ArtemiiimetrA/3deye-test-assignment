using FileSort.Sorter.Configuration;

namespace FileSort.Sorter.Helpers;

/// <summary>
///     Provides utilities for size calculations and conversions.
/// </summary>
internal static class SizeHelpers
{
    /// <summary>
    ///     Converts megabytes to bytes.
    /// </summary>
    /// <param name="megabytes">The number of megabytes to convert.</param>
    /// <returns>The equivalent number of bytes.</returns>
    public static long ConvertMegabytesToBytes(int megabytes)
    {
        return (long)megabytes * SortConstants.BytesPerMegabyte;
    }

    /// <summary>
    ///     Calculates the number of bytes required to store a line, including the newline character.
    /// </summary>
    /// <param name="line">The line to calculate bytes for.</param>
    /// <returns>The total number of bytes required for the line including newline.</returns>
    public static long CalculateLineBytes(string line)
    {
        return SortConstants.FileEncoding.GetByteCount(line) +
               SortConstants.FileEncoding.GetByteCount(Environment.NewLine);
    }

    /// <summary>
    ///     Gets the size of a file in bytes.
    /// </summary>
    /// <param name="filePath">The path to the file.</param>
    /// <returns>The size of the file in bytes.</returns>
    public static long GetFileSize(string filePath)
    {
        var fileInfo = new FileInfo(filePath);
        return fileInfo.Length;
    }
}