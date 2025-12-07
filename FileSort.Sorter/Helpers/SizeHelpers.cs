using System.Text;
using FileSort.Sorter.Configuration;

namespace FileSort.Sorter.Helpers;

/// <summary>
/// Provides utilities for size calculations and conversions.
/// </summary>
internal static class SizeHelpers
{
    private static readonly Encoding FileEncoding = Encoding.UTF8;

    public static long ConvertMegabytesToBytes(int megabytes)
    {
        return (long)megabytes * SortConstants.BytesPerMegabyte;
    }

    public static long CalculateLineBytes(string line)
    {
        return WriteBufferHelpers.FileEncoding.GetByteCount(line) + FileEncoding.GetByteCount(Environment.NewLine);
    }

    public static long GetFileSize(string filePath)
    {
        var fileInfo = new FileInfo(filePath);
        return fileInfo.Length;
    }
}

