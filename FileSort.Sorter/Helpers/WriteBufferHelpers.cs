using System.Text;
using FileSort.Sorter.Configuration;

namespace FileSort.Sorter.Helpers;

/// <summary>
/// Provides utilities for managing write buffers when writing records to files.
/// </summary>
internal static class WriteBufferHelpers
{
    public static readonly Encoding FileEncoding = Encoding.UTF8;

    public static bool ShouldFlushBuffer(List<string> writeBuffer)
    {
        return writeBuffer.Count >= SortConstants.WriteBufferCapacity;
    }

    public static async Task FlushWriteBufferAsync(
        List<string> writeBuffer,
        StreamWriter writer)
    {
        if (writeBuffer.Count == 0)
        {
            return;
        }

        // Batch write all lines efficiently using StringBuilder
        // Estimate average line length to pre-allocate StringBuilder capacity
        var sb = new StringBuilder(writeBuffer.Count * 80); // Average line estimate
        foreach (string line in writeBuffer)
        {
            sb.AppendLine(line);
        }
        
        await writer.WriteAsync(sb.ToString());
        writeBuffer.Clear();
    }

    public static async Task FlushRemainingLinesAsync(
        List<string> writeBuffer,
        StreamWriter writer)
    {
        await FlushWriteBufferAsync(writeBuffer, writer);
    }
}

