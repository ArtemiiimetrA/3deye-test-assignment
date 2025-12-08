using System.IO;
using FileSort.Core.Models;
using FileSort.Core.Models.Progress;
using FileSort.Core.Parsing;
using FileSort.Sorter.Configuration;
using FileSort.Sorter.Helpers;

namespace FileSort.Sorter.Strategies;

/// <summary>
///     Performs k-way merge of sorted files in a single pass.
///     All input files are opened simultaneously and merged using a priority queue.
/// </summary>
internal sealed class SinglePassMerger : IMergeStrategy
{
    private readonly int _bufferSize;

    public SinglePassMerger(int bufferSize)
    {
        _bufferSize = bufferSize;
    }

    public async Task MergeAsync(
        IReadOnlyList<string> filePaths,
        string outputPath,
        IProgress<SortProgress>? progress,
        CancellationToken cancellationToken)
    {
        var readers = new StreamReader?[filePaths.Count];
        var priorityQueue = new PriorityQueue<RecordWithSource, RecordKey>();

        try
        {
            await InitializeReadersAsync(filePaths, readers, priorityQueue, cancellationToken);

            await using var writer = FileIOHelpers.CreateFileWriter(outputPath, _bufferSize);

            await MergeRecordsAsync(
                readers,
                priorityQueue,
                writer,
                filePaths,
                progress,
                cancellationToken);
        }
        finally
        {
            DisposeReaders(readers);
        }
    }

    private async Task InitializeReadersAsync(
        IReadOnlyList<string> filePaths,
        StreamReader?[] readers,
        PriorityQueue<RecordWithSource, RecordKey> priorityQueue,
        CancellationToken cancellationToken)
    {
        for (var i = 0; i < filePaths.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var reader = FileIOHelpers.CreateFileReader(filePaths[i], _bufferSize);
            readers[i] = reader;

            await TryReadAndEnqueueRecordAsync(reader, i, filePaths[i], priorityQueue, cancellationToken);
        }
    }

    private async Task MergeRecordsAsync(
        StreamReader?[] readers,
        PriorityQueue<RecordWithSource, RecordKey> priorityQueue,
        StreamWriter writer,
        IReadOnlyList<string> filePaths,
        IProgress<SortProgress>? progress,
        CancellationToken cancellationToken)
    {
        var writeBuffer = new List<string>(SortConstants.WriteBufferCapacity);
        var recordsWritten = 0;

        while (priorityQueue.Count > 0)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var (record, sourceIndex) = priorityQueue.Dequeue();
            writeBuffer.Add(record.ToLine());
            recordsWritten++;

            await TryReadNextRecordAsync(readers, sourceIndex, filePaths[sourceIndex], priorityQueue, cancellationToken);

            if (WriteBufferHelpers.ShouldFlushBuffer(writeBuffer))
                await WriteBufferHelpers.FlushWriteBufferAsync(writeBuffer, writer);

            SortProgressReporter.ReportMergeProgress(recordsWritten, progress);
        }

        await WriteBufferHelpers.FlushWriteBufferAsync(writeBuffer, writer);
        await writer.FlushAsync(cancellationToken);
    }

    private static async Task TryReadNextRecordAsync(
        StreamReader?[] readers,
        int sourceIndex,
        string filePath,
        PriorityQueue<RecordWithSource, RecordKey> priorityQueue,
        CancellationToken cancellationToken)
    {
        var reader = readers[sourceIndex];
        if (reader == null) return;

        var success = await TryReadAndEnqueueRecordAsync(reader, sourceIndex, filePath, priorityQueue, cancellationToken);
        if (!success) CloseReader(readers, sourceIndex);
    }

    private static async Task<bool> TryReadAndEnqueueRecordAsync(
        StreamReader reader,
        int fileIndex,
        string filePath,
        PriorityQueue<RecordWithSource, RecordKey> priorityQueue,
        CancellationToken cancellationToken)
    {
        string? line;
        try
        {
            line = await reader.ReadLineAsync(cancellationToken);
        }
        catch (TaskCanceledException)
        {
            CancellationHelpers.HandleCancellation(cancellationToken);
            line = null; // Unreachable, but satisfies compiler
        }

        // Normal end of file - return false to indicate file is exhausted
        if (line == null)
            return false;

        // Try to parse the line
        if (!RecordParser.TryParse(line, out var record))
        {
            throw new InvalidDataException(
                $"Invalid record format in file '{filePath}'. " +
                $"Expected format: '{{Number}}. {{Text}}', but got: '{line}'");
        }

        var key = new RecordKey(record.Text, record.Number);
        priorityQueue.Enqueue(new RecordWithSource(record, fileIndex), key);
        return true;
    }

    private static void CloseReader(StreamReader?[] readers, int index)
    {
        readers[index]?.Dispose();
        readers[index] = null;
    }

    /// <summary>
    ///     Safely disposes all readers, ignoring any errors that occur during disposal.
    /// </summary>
    /// <param name="readers">Array of readers to dispose.</param>
    /// <remarks>
    ///     Exceptions are silently swallowed to ensure disposal failures don't prevent cleanup of other readers.
    /// </remarks>
    private static void DisposeReaders(StreamReader?[] readers)
    {
        foreach (var reader in readers)
            if (reader != null)
                try
                {
                    reader.Dispose();
                }
                catch
                {
                    // Ignore disposal errors - failures on one reader shouldn't prevent cleanup of others
                }
    }
}