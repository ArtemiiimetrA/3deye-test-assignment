using FileSort.Core.Models;
using FileSort.Core.Models.Progress;
using FileSort.Core.Parsing;
using FileSort.Sorter.Helpers;
using FileSort.Sorter.Configuration;

namespace FileSort.Sorter.Strategies;

/// <summary>
/// Performs k-way merge of sorted files in a single pass.
/// All input files are opened simultaneously and merged using a priority queue.
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

            await using var writer = FileIoHelpers.CreateFileWriter(outputPath, _bufferSize);

            await MergeRecordsAsync(
                readers,
                priorityQueue,
                writer,
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
        for (int i = 0; i < filePaths.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            StreamReader reader = FileIoHelpers.CreateFileReader(filePaths[i], _bufferSize);
            readers[i] = reader;

            await TryReadAndEnqueueRecordAsync(reader, i, priorityQueue, cancellationToken);
        }
    }

    private static async Task MergeRecordsAsync(
        StreamReader?[] readers,
        PriorityQueue<RecordWithSource, RecordKey> priorityQueue,
        StreamWriter writer,
        IProgress<SortProgress>? progress,
        CancellationToken cancellationToken)
    {
        var writeBuffer = new List<string>(capacity: SortConstants.WriteBufferCapacity);
        int recordsWritten = 0;

        while (priorityQueue.Count > 0)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var (record, sourceIndex) = priorityQueue.Dequeue();
            writeBuffer.Add(record.ToLine());
            recordsWritten++;

            await TryReadNextRecordAsync(readers, sourceIndex, priorityQueue, cancellationToken);

            if (WriteBufferHelpers.ShouldFlushBuffer(writeBuffer))
            {
                await WriteBufferHelpers.FlushWriteBufferAsync(writeBuffer, writer);
            }

            SortProgressReporter.ReportMergeProgress(recordsWritten, progress);
        }

        await WriteBufferHelpers.FlushRemainingLinesAsync(writeBuffer, writer);
        
        await HandleCancellationAsync(
            ct => writer.FlushAsync(ct),
            cancellationToken);
    }

    private static async Task TryReadNextRecordAsync(
        StreamReader?[] readers,
        int sourceIndex,
        PriorityQueue<RecordWithSource, RecordKey> priorityQueue,
        CancellationToken cancellationToken)
    {
        StreamReader? reader = readers[sourceIndex];
        if (reader == null)
        {
            return;
        }

        bool success = await TryReadAndEnqueueRecordAsync(reader, sourceIndex, priorityQueue, cancellationToken);
        if (!success)
        {
            CloseReader(readers, sourceIndex);
        }
    }

    private static async Task<bool> TryReadAndEnqueueRecordAsync(
        StreamReader reader,
        int fileIndex,
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
            cancellationToken.ThrowIfCancellationRequested();
            throw new OperationCanceledException("Operation was cancelled.", cancellationToken);
        }
        
        if (line != null && RecordParser.TryParse(line, out Record record))
        {
            var key = new RecordKey(record.Text, record.Number);
            priorityQueue.Enqueue(new RecordWithSource(record, fileIndex), key);
            return true;
        }
        
        return false;
    }

    private static async Task<T> HandleCancellationAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken)
    {
        try
        {
            return await operation(cancellationToken);
        }
        catch (TaskCanceledException)
        {
            cancellationToken.ThrowIfCancellationRequested();
            throw new OperationCanceledException("Operation was cancelled.", cancellationToken);
        }
    }

    private static async Task HandleCancellationAsync(
        Func<CancellationToken, Task> operation,
        CancellationToken cancellationToken)
    {
        try
        {
            await operation(cancellationToken);
        }
        catch (TaskCanceledException)
        {
            cancellationToken.ThrowIfCancellationRequested();
            throw new OperationCanceledException("Operation was cancelled.", cancellationToken);
        }
    }

    private static void CloseReader(StreamReader?[] readers, int index)
    {
        readers[index]?.Dispose();
        readers[index] = null;
    }

    private static void DisposeReaders(StreamReader?[] readers)
    {
        foreach (var reader in readers)
        {
            if (reader != null)
            {
                try
                {
                    reader.Dispose();
                }
                catch
                {
                    // Ignore disposal errors
                }
            }
        }
    }
}

