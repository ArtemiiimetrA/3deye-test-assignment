using FileSort.Sorter.Configuration;

namespace FileSort.Sorter.Helpers;

/// <summary>
///     Provides utilities for managing write buffers when writing records to files.
/// </summary>
internal static class WriteBufferHelpers
{
    /// <summary>
    ///     Determines whether the write buffer should be flushed based on its capacity.
    /// </summary>
    /// <param name="writeBuffer">The write buffer to check.</param>
    /// <returns>True if the buffer has reached or exceeded its capacity threshold.</returns>
    public static bool ShouldFlushBuffer(List<string> writeBuffer)
    {
        return writeBuffer.Count >= SortConstants.WriteBufferCapacity;
    }

    /// <summary>
    ///     Flushes all lines in the write buffer to the writer and clears the buffer.
    /// </summary>
    /// <param name="writeBuffer">The write buffer containing lines to write.</param>
    /// <param name="writer">The StreamWriter to write the lines to.</param>
    public static async Task FlushWriteBufferAsync(
        List<string> writeBuffer,
        StreamWriter writer)
    {
        if (writeBuffer.Count == 0) return;

        foreach (var line in writeBuffer) await writer.WriteLineAsync(line);

        await writer.FlushAsync();
        writeBuffer.Clear();
    }
}