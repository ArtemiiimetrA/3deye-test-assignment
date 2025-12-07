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

        foreach (string line in writeBuffer)
        {
            await writer.WriteLineAsync(line);
        }
        
        await writer.FlushAsync();
        writeBuffer.Clear();
    }

    public static async Task FlushRemainingLinesAsync(
        List<string> writeBuffer,
        StreamWriter writer)
    {
        await FlushWriteBufferAsync(writeBuffer, writer);
    }
}

