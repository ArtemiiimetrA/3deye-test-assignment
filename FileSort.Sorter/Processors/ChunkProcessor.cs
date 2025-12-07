using FileSort.Core.Comparison;
using FileSort.Core.Models;
using FileSort.Sorter.Helpers;
using FileSort.Sorter.Configuration;

namespace FileSort.Sorter.Processors;

/// <summary>
/// Processes chunks: reads records, sorts them in memory, and writes to temporary files.
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
        string chunkFilePath = FileIoHelpers.GenerateFilePath(tempDirectory, chunkTemplate, chunkIndex);
        FileIoHelpers.EnsureDirectoryExists(tempDirectory);

        await using var writer = FileIoHelpers.CreateFileWriter(chunkFilePath, bufferSize);
        await WriteRecordsToFileAsync(records, writer, cancellationToken);

        return chunkFilePath;
    }

    private static async Task WriteRecordsToFileAsync(
        List<Record> records,
        StreamWriter writer,
        CancellationToken cancellationToken)
    {
        var writeBuffer = new List<string>(capacity: SortConstants.WriteBufferCapacity);

        foreach (Record record in records)
        {
            writeBuffer.Add(record.ToLine());

            if (WriteBufferHelpers.ShouldFlushBuffer(writeBuffer))
            {
                await WriteBufferHelpers.FlushWriteBufferAsync(writeBuffer, writer);
            }

            cancellationToken.ThrowIfCancellationRequested();
        }

        await WriteBufferHelpers.FlushRemainingLinesAsync(writeBuffer, writer);
        await writer.FlushAsync(cancellationToken);
    }

}

