using FileSort.Core.Models;
using FileSort.Core.Models.Progress;
using FileSort.Core.Parsing;
using FileSort.Core.Requests;
using FileSort.Sorter.Configuration;
using FileSort.Sorter.Helpers;
using FileSort.Sorter.Processors;

namespace FileSort.Sorter.Coordinators;

/// <summary>
///     Coordinates the creation of sorted chunks from input file.
/// </summary>
internal sealed class ChunkCreationCoordinator : IDisposable
{
    private readonly ChunkProcessor _chunkProcessor;
    private readonly long _chunkSizeBytes;

    private readonly ChunkState _chunkState;

    // List of chunk processing tasks. Size is bounded by (file size / chunk size), which is reasonable.
    // Tasks complete as chunks are processed, so memory usage is controlled by the semaphore limiting concurrency.
    private readonly List<Task<string>> _chunkTasks;
    private readonly IProgress<SortProgress>? _progress;
    private readonly SortRequest _request;
    private readonly SemaphoreSlim _semaphore;
    private readonly long _totalBytes;

    public ChunkCreationCoordinator(
        SortRequest request,
        long totalBytes,
        IProgress<SortProgress>? progress)
    {
        _request = request;
        _totalBytes = totalBytes;
        _progress = progress;
        _chunkProcessor = new ChunkProcessor();
        _semaphore = new SemaphoreSlim(request.MaxDegreeOfParallelism);
        _chunkTasks = new List<Task<string>>();
        _chunkState = new ChunkState
        {
            ChunkIndex = 0,
            BytesRead = 0,
            Records = new List<Record>(),
            CurrentChunkBytes = 0
        };
        _chunkSizeBytes = SizeHelpers.ConvertMegabytesToBytes(request.ChunkSizeMb);
    }

    public void Dispose()
    {
        _semaphore?.Dispose();
    }

    public async Task<List<string>> ProcessAsync(CancellationToken cancellationToken)
    {
        using var reader = FileIOHelpers.CreateFileReader(_request.InputFilePath, _request.BufferSizeBytes);

        await ProcessInputLinesAsync(reader, cancellationToken);
        ProcessFinalChunk(cancellationToken);

        var chunkFiles = await WaitForChunksAsync();
        return chunkFiles;
    }

    private async Task ProcessInputLinesAsync(StreamReader reader, CancellationToken cancellationToken)
    {
        string? line;
        while (true)
        {
            try
            {
                line = await reader.ReadLineAsync(cancellationToken);
            }
            catch (TaskCanceledException)
            {
                CancellationHelpers.HandleCancellation(cancellationToken);
                line = null; // Unreachable, but satisfies compiler
            }

            if (line == null)
                break;

            if (RecordParser.TryParse(line, out var record)) ProcessRecord(line, record);

            if (ShouldCreateChunk()) ScheduleChunkProcessing(cancellationToken);

            SortProgressReporter.ReportIfNeeded(_chunkState.BytesRead, _chunkState.ChunkIndex, _totalBytes, _progress);
        }
    }

    private void ProcessRecord(string line, Record record)
    {
        _chunkState.Records.Add(record);
        var lineBytes = SizeHelpers.CalculateLineBytes(line);
        _chunkState.AddCurrentChunkBytes(lineBytes);
        _chunkState.AddBytesRead(lineBytes);
    }

    private bool ShouldCreateChunk()
    {
        if (_request.AdaptiveChunkSize) return ShouldCreateAdaptiveChunk();

        return _chunkState.CurrentChunkBytes >= _chunkSizeBytes;
    }

    private bool ShouldCreateAdaptiveChunk()
    {
        var minChunkBytes = SizeHelpers.ConvertMegabytesToBytes(_request.MinChunkSizeMb);
        var maxChunkBytes = SizeHelpers.ConvertMegabytesToBytes(_request.MaxChunkSizeMb);

        if (_chunkState.CurrentChunkBytes < minChunkBytes) return false;

        // Use CurrentChunkBytes directly as memory estimate (file size is a good proxy for memory usage)
        var maxMemoryBytes = SizeHelpers.ConvertMegabytesToBytes(_request.MaxRamMb);

        return _chunkState.CurrentChunkBytes >= maxMemoryBytes * SortConstants.AdaptiveMemoryThreshold
               || _chunkState.CurrentChunkBytes >= maxChunkBytes;
    }

    private void ScheduleChunkProcessing(CancellationToken cancellationToken)
    {
        if (_chunkState.Records.Count == 0) return;

        var recordsCopy = new List<Record>(_chunkState.Records);
        _chunkState.Records.Clear();
        _chunkState.CurrentChunkBytes = 0;

        var currentChunkIndex = _chunkState.ChunkIndex++;
        var task = ProcessChunkWithSemaphoreAsync(
            recordsCopy,
            currentChunkIndex,
            cancellationToken);

        _chunkTasks.Add(task);
    }

    private void ProcessFinalChunk(CancellationToken cancellationToken)
    {
        if (_chunkState.Records.Count > 0) ScheduleChunkProcessing(cancellationToken);
    }

    private async Task<List<string>> WaitForChunksAsync()
    {
        var chunkFilePaths = await Task.WhenAll(_chunkTasks);
        var chunkFiles = new List<string>(chunkFilePaths);

        SortProgressReporter.ReportFinal(chunkFiles.Count, _progress);

        return chunkFiles;
    }

    private async Task<string> ProcessChunkWithSemaphoreAsync(
        List<Record> records,
        int chunkIndex,
        CancellationToken cancellationToken)
    {
        try
        {
            await _semaphore.WaitAsync(cancellationToken);
        }
        catch (TaskCanceledException)
        {
            cancellationToken.ThrowIfCancellationRequested();
            throw new OperationCanceledException("Operation was cancelled.", cancellationToken);
        }

        try
        {
            return await _chunkProcessor.ProcessChunkAsync(
                records,
                _request.TempDirectory,
                _request.FileChunkTemplate,
                chunkIndex,
                _request.BufferSizeBytes,
                cancellationToken);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <summary>
    ///     Tracks state during chunk creation process.
    ///     All operations on this class are single-threaded (called from the main processing loop).
    /// </summary>
    private sealed class ChunkState
    {
        public int ChunkIndex { get; set; }
        public long BytesRead { get; set; }
        public List<Record> Records { get; set; } = new();
        public long CurrentChunkBytes { get; set; }

        public void AddBytesRead(long bytes)
        {
            BytesRead += bytes;
        }

        public void AddCurrentChunkBytes(long bytes)
        {
            CurrentChunkBytes += bytes;
        }
    }
}