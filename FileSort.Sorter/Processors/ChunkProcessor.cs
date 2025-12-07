using FileSort.Core.Comparison;
using FileSort.Core.Models;
using FileSort.Sorter.Configuration;
using FileSort.Sorter.Helpers;

namespace FileSort.Sorter.Processors;

/// <summary>
///     Processes chunks: reads records, sorts them in memory, and writes to temporary files.
/// </summary>
internal sealed class ChunkProcessor
{
    public async Task<string> ProcessChunkAsync(
        List<Record> records,
        string tempDirectory,
        string chunkTemplate,
        int chunkIndex,
        int bufferSize,
        CancellationToken cancellationToken)
    {
        records.Sort(RecordComparer.Instance);
        var chunkFilePath = FileIOHelpers.GenerateFilePath(tempDirectory, chunkTemplate, chunkIndex);
        FileIOHelpers.EnsureDirectoryExists(tempDirectory);

        await using var writer = FileIOHelpers.CreateFileWriter(chunkFilePath, bufferSize);
        await WriteRecordsToFileAsync(records, writer, cancellationToken);

        return chunkFilePath;
    }

    private static async Task WriteRecordsToFileAsync(
        List<Record> records,
        StreamWriter writer,
        CancellationToken cancellationToken)
    {
        var writeBuffer = new List<string>(SortConstants.WriteBufferCapacity);

        foreach (var record in records)
        {
            writeBuffer.Add(record.ToLine());

            if (WriteBufferHelpers.ShouldFlushBuffer(writeBuffer))
                await WriteBufferHelpers.FlushWriteBufferAsync(writeBuffer, writer);

            cancellationToken.ThrowIfCancellationRequested();
        }

        await WriteBufferHelpers.FlushWriteBufferAsync(writeBuffer, writer);
        await writer.FlushAsync(cancellationToken);
    }
}