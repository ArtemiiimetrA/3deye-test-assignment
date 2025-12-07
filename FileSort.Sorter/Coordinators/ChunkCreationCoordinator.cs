using System.Threading;
using FileSort.Core.Interfaces;
using FileSort.Core.Models;
using FileSort.Core.Models.Progress;
using FileSort.Core.Parsing;
using FileSort.Core.Requests;
using FileSort.Sorter.Helpers;
using FileSort.Sorter.Processors;
using FileSort.Sorter.Configuration;

namespace FileSort.Sorter.Coordinators;

/// <summary>
/// Coordinates the creation of sorted chunks from input file.
/// </summary>
internal sealed class ChunkCreationCoordinator : IDisposable
{
    private readonly SortRequest _request;
    private readonly long _totalBytes;
    private readonly IProgress<SortProgress>? _progress;
    private readonly ChunkProcessor _chunkProcessor;
    private readonly SemaphoreSlim _semaphore;
    // List of chunk processing tasks. Size is bounded by (file size / chunk size), which is reasonable.
    // Tasks complete as chunks are processed, so memory usage is controlled by the semaphore limiting concurrency.
    private readonly List<Task<string>> _chunkTasks;
    private readonly ChunkState _chunkState;
    private readonly long _chunkSizeBytes;

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

    public async Task<List<string>> ProcessAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var reader = FileIoHelpers.CreateFileReader(_request.InputFilePath, _request.BufferSizeBytes);

            await ProcessInputLinesAsync(reader, cancellationToken);
            ProcessFinalChunk(cancellationToken);

            List<string> chunkFiles = await WaitForChunksAsync();
            return chunkFiles;
        }
        finally
        {
            // Ensure semaphore is released even on exception
            // The Dispose method will handle cleanup
        }
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
                cancellationToken.ThrowIfCancellationRequested();
                throw new OperationCanceledException("Operation was cancelled.", cancellationToken);
            }
            
            if (line == null)
                break;

            if (RecordParser.TryParse(line, out Record record))
            {
                ProcessRecord(line, record);
            }

            if (ShouldCreateChunk())
            {
                ScheduleChunkProcessing(cancellationToken);
            }

            SortProgressReporter.ReportIfNeeded(_chunkState.BytesRead, _chunkState.ChunkIndex, _totalBytes, _progress);
        }
    }

    private void ProcessRecord(string line, Record record)
    {
        _chunkState.Records.Add(record);
        long lineBytes = SizeHelpers.CalculateLineBytes(line);
        _chunkState.AddCurrentChunkBytes(lineBytes);
        _chunkState.AddBytesRead(lineBytes);
    }

    private bool ShouldCreateChunk()
    {
        if (_request.AdaptiveChunkSize)
        {
            return ShouldCreateAdaptiveChunk();
        }

        return _chunkState.CurrentChunkBytes >= _chunkSizeBytes;
    }

    private bool ShouldCreateAdaptiveChunk()
    {
        long minChunkBytes = SizeHelpers.ConvertMegabytesToBytes(_request.MinChunkSizeMb);
        long maxChunkBytes = SizeHelpers.ConvertMegabytesToBytes(_request.MaxChunkSizeMb);

        if (_chunkState.CurrentChunkBytes < minChunkBytes)
        {
            return false;
        }

        // Use CurrentChunkBytes directly as memory estimate (file size is a good proxy for memory usage)
        long maxMemoryBytes = SizeHelpers.ConvertMegabytesToBytes(_request.MaxRamMb);

        return _chunkState.CurrentChunkBytes >= maxMemoryBytes * SortConstants.AdaptiveMemoryThreshold
            || _chunkState.CurrentChunkBytes >= maxChunkBytes;
    }

    private void ScheduleChunkProcessing(CancellationToken cancellationToken)
    {
        if (_chunkState.Records.Count == 0)
        {
            return;
        }

        var recordsCopy = new List<Record>(_chunkState.Records);
        _chunkState.Records.Clear();
        _chunkState.CurrentChunkBytes = 0;

        int currentChunkIndex = _chunkState.ChunkIndex++;
        var task = ProcessChunkWithSemaphoreAsync(
            recordsCopy,
            currentChunkIndex,
            cancellationToken);

        _chunkTasks.Add(task);
    }

    private void ProcessFinalChunk(CancellationToken cancellationToken)
    {
        if (_chunkState.Records.Count > 0)
        {
            ScheduleChunkProcessing(cancellationToken);
        }
    }

    private async Task<List<string>> WaitForChunksAsync()
    {
        string[] chunkFilePaths = await Task.WhenAll(_chunkTasks);
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

    public void Dispose()
    {
        _semaphore?.Dispose();
    }

    /// <summary>
    /// Tracks state during chunk creation process.
    /// Thread-safe for read operations, but write operations should be single-threaded.
    /// </summary>
    private sealed class ChunkState
    {
        private int _chunkIndex;
        private long _bytesRead;
        private long _currentChunkBytes;

        public int ChunkIndex
        {
            get => _chunkIndex;
            set => _chunkIndex = value;
        }

        public long BytesRead
        {
            get => Interlocked.Read(ref _bytesRead);
            set => Interlocked.Exchange(ref _bytesRead, value);
        }

        public List<Record> Records { get; set; } = new();

        public long CurrentChunkBytes
        {
            get => Interlocked.Read(ref _currentChunkBytes);
            set => Interlocked.Exchange(ref _currentChunkBytes, value);
        }

        public void AddBytesRead(long bytes)
        {
            Interlocked.Add(ref _bytesRead, bytes);
        }

        public void AddCurrentChunkBytes(long bytes)
        {
            Interlocked.Add(ref _currentChunkBytes, bytes);
        }
    }
}

