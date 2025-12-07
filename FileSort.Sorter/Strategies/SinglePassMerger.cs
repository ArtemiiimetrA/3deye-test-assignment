using FileSort.Core.Models;
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

            await EnqueueFirstRecordAsync(reader, i, priorityQueue, cancellationToken);
        }
    }

    private static async Task EnqueueFirstRecordAsync(
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
        
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            await writer.FlushAsync(cancellationToken);
        }
        catch (TaskCanceledException)
        {
            cancellationToken.ThrowIfCancellationRequested();
            throw new OperationCanceledException("Operation was cancelled.", cancellationToken);
        }
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

        string? nextLine;
        try
        {
            nextLine = await reader.ReadLineAsync(cancellationToken);
        }
        catch (TaskCanceledException)
        {
            cancellationToken.ThrowIfCancellationRequested();
            throw new OperationCanceledException("Operation was cancelled.", cancellationToken);
        }
        
        if (nextLine != null && RecordParser.TryParse(nextLine, out Record nextRecord))
        {
            var key = new RecordKey(nextRecord.Text, nextRecord.Number);
            priorityQueue.Enqueue(new RecordWithSource(nextRecord, sourceIndex), key);
        }
        else
        {
            CloseReader(readers, sourceIndex);
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

