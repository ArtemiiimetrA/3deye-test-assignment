using FileSort.Core.Comparison;
using FileSort.Core.Models;
using FileSort.Core.Parsing;

namespace FileSort.Sorter;

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
        // Sort records in memory
        records.Sort(RecordComparer.Instance);

        // Generate chunk file path
        string chunkFilePath = Path.Combine(tempDirectory, string.Format(chunkTemplate, chunkIndex));

        // Ensure temp directory exists
        Directory.CreateDirectory(tempDirectory);

        // Write sorted records to chunk file
        await using var fileStream = new FileStream(
            chunkFilePath,
            FileMode.Create,
            FileAccess.Write,
            FileShare.None,
            bufferSize,
            FileOptions.SequentialScan | FileOptions.Asynchronous);

        await using var writer = new StreamWriter(fileStream, System.Text.Encoding.UTF8, bufferSize);

        // Write buffer for batching
        var writeBuffer = new List<string>(capacity: 10000);

        foreach (Record record in records)
        {
            writeBuffer.Add(record.ToLine());

            if (writeBuffer.Count >= 10000)
            {
                foreach (string line in writeBuffer)
                {
                    await writer.WriteLineAsync(line);
                }
                writeBuffer.Clear();
            }

            cancellationToken.ThrowIfCancellationRequested();
        }

        // Flush remaining lines
        if (writeBuffer.Count > 0)
        {
            foreach (string line in writeBuffer)
            {
                await writer.WriteLineAsync(line);
            }
        }

        await writer.FlushAsync();

        return chunkFilePath;
    }
}
