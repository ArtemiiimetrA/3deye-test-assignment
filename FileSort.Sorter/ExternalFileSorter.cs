using FileSort.Core.Interfaces;
using FileSort.Core.Models;
using FileSort.Core.Parsing;
using FileSort.Core.Requests;

namespace FileSort.Sorter;

/// <summary>
/// External merge sort implementation that handles very large files by:
/// 1. Chunking: Reading chunks, sorting in memory, writing to temp files
/// 2. Merging: K-way merge of sorted chunks (single-pass or multi-pass cascading)
/// </summary>
public sealed class ExternalFileSorter : IExternalSorter
{
    public async Task SortAsync(
        SortRequest request,
        IProgress<SortProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ValidateRequest(request);

        // Ensure temp directory exists
        Directory.CreateDirectory(request.TempDirectory);

        // Get input file size for progress tracking
        var inputFileInfo = new FileInfo(request.InputFilePath);
        long totalBytes = inputFileInfo.Length;

        // Phase 1: Create sorted chunks
        List<string> chunkFiles = await CreateChunksAsync(request, totalBytes, progress, cancellationToken);

        try
        {
            // Phase 2: Merge chunks
            var mergeProcessor = new MergeProcessor(request.MaxOpenFiles, request.BufferSizeBytes);
            await mergeProcessor.MergeChunksAsync(chunkFiles, request.OutputFilePath, progress, cancellationToken);
        }
        finally
        {
            // Cleanup: Delete temporary chunk files
            if (request.DeleteTempFiles)
            {
                foreach (string chunkFile in chunkFiles)
                {
                    try
                    {
                        if (File.Exists(chunkFile))
                            File.Delete(chunkFile);
                    }
                    catch
                    {
                        // Ignore deletion errors
                    }
                }
            }
        }
    }

    private async Task<List<string>> CreateChunksAsync(
        SortRequest request,
        long totalBytes,
        IProgress<SortProgress>? progress,
        CancellationToken cancellationToken)
    {
        var chunkFiles = new List<string>();
        var chunkProcessor = new ChunkProcessor();

        long chunkSizeBytes = (long)request.ChunkSizeMb * 1024 * 1024;
        long bytesRead = 0;
        int chunkIndex = 0;

        await using var fileStream = new FileStream(
            request.InputFilePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            request.BufferSizeBytes,
            FileOptions.SequentialScan | FileOptions.Asynchronous);

        using var reader = new StreamReader(fileStream, System.Text.Encoding.UTF8, bufferSize: request.BufferSizeBytes);

        var records = new List<Record>();
        long currentChunkBytes = 0;
        long estimatedRecordSize = 0;
        int recordCount = 0;

        // Semaphore to limit concurrent chunk processing
        var semaphore = new SemaphoreSlim(request.MaxDegreeOfParallelism);
        var chunkTasks = new List<Task<string>>();

        string? line;
        while ((line = await reader.ReadLineAsync()) != null)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (RecordParser.TryParse(line, out Record record))
            {
                records.Add(record);
                long lineBytes = System.Text.Encoding.UTF8.GetByteCount(line) + 2; // +2 for line ending
                currentChunkBytes += lineBytes;
                bytesRead += lineBytes;
                recordCount++;

                // Estimate average record size for adaptive chunking
                if (recordCount > 0)
                    estimatedRecordSize = currentChunkBytes / recordCount;
            }

            // Check if we should create a chunk
            bool shouldCreateChunk = false;

            if (request.AdaptiveChunkSize)
            {
                // Adaptive: Check memory pressure and adjust
                long minChunkBytes = (long)request.MinChunkSizeMb * 1024 * 1024;
                long maxChunkBytes = (long)request.MaxChunkSizeMb * 1024 * 1024;

                if (currentChunkBytes >= minChunkBytes)
                {
                    // Check if we're approaching memory limit or have enough data
                    long estimatedMemory = EstimateMemoryUsage(records.Count, estimatedRecordSize);
                    long maxMemoryBytes = (long)request.MaxRamMb * 1024 * 1024;

                    if (estimatedMemory >= maxMemoryBytes * 0.8 || currentChunkBytes >= maxChunkBytes)
                    {
                        shouldCreateChunk = true;
                    }
                }
            }
            else
            {
                // Fixed chunk size
                if (currentChunkBytes >= chunkSizeBytes)
                {
                    shouldCreateChunk = true;
                }
            }

            if (shouldCreateChunk && records.Count > 0)
            {
                // Process chunk asynchronously
                var recordsCopy = new List<Record>(records);
                records.Clear();
                currentChunkBytes = 0;
                recordCount = 0;

                int currentChunkIndex = chunkIndex++;
                var task = ProcessChunkWithSemaphoreAsync(
                    semaphore,
                    chunkProcessor,
                    recordsCopy,
                    request.TempDirectory,
                    request.FileChunkTemplate,
                    currentChunkIndex,
                    request.BufferSizeBytes,
                    cancellationToken);

                chunkTasks.Add(task);
            }

            // Report progress
            if (bytesRead % (10 * 1024 * 1024) == 0) // Every 10MB
            {
                progress?.Report(new SortProgress
                {
                    ChunksCreated = chunkIndex,
                    BytesProcessed = bytesRead,
                    TotalBytes = totalBytes
                });
            }
        }

        // Process remaining records as final chunk
        if (records.Count > 0)
        {
            int currentChunkIndex = chunkIndex++;
            var task = ProcessChunkWithSemaphoreAsync(
                semaphore,
                chunkProcessor,
                records,
                request.TempDirectory,
                request.FileChunkTemplate,
                currentChunkIndex,
                request.BufferSizeBytes,
                cancellationToken);

            chunkTasks.Add(task);
        }

        // Wait for all chunks to be processed
        string[] chunkFilePaths = await Task.WhenAll(chunkTasks);
        chunkFiles.AddRange(chunkFilePaths);

        progress?.Report(new SortProgress
        {
            ChunksCreated = chunkFiles.Count,
            BytesProcessed = totalBytes,
            TotalBytes = totalBytes
        });

        return chunkFiles;
    }

    private static async Task<string> ProcessChunkWithSemaphoreAsync(
        SemaphoreSlim semaphore,
        ChunkProcessor chunkProcessor,
        List<Record> records,
        string tempDirectory,
        string chunkTemplate,
        int chunkIndex,
        int bufferSize,
        CancellationToken cancellationToken)
    {
        await semaphore.WaitAsync(cancellationToken);
        try
        {
            return await chunkProcessor.ProcessChunkAsync(
                records,
                tempDirectory,
                chunkTemplate,
                chunkIndex,
                bufferSize,
                cancellationToken);
        }
        finally
        {
            semaphore.Release();
        }
    }

    private static long EstimateMemoryUsage(int recordCount, long averageRecordSize)
    {
        // Rough estimate: record count * (average size + overhead)
        // Overhead includes: Record struct, string references, list overhead
        const long overheadPerRecord = 50; // bytes
        return recordCount * (averageRecordSize + overheadPerRecord);
    }

    private static void ValidateRequest(SortRequest request)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        if (string.IsNullOrWhiteSpace(request.InputFilePath))
            throw new ArgumentException("InputFilePath is required.", nameof(request));

        if (!File.Exists(request.InputFilePath))
            throw new FileNotFoundException($"Input file not found: {request.InputFilePath}");

        if (string.IsNullOrWhiteSpace(request.OutputFilePath))
            throw new ArgumentException("OutputFilePath is required.", nameof(request));

        if (request.ChunkSizeMb <= 0)
            throw new ArgumentException("ChunkSizeMb must be greater than 0.", nameof(request));

        if (request.ChunkSizeMb > request.MaxRamMb)
            throw new ArgumentException("ChunkSizeMb cannot exceed MaxRamMb.", nameof(request));

        if (request.MaxRamMb <= 0)
            throw new ArgumentException("MaxRamMb must be greater than 0.", nameof(request));

        if (request.MaxDegreeOfParallelism <= 0)
            throw new ArgumentException("MaxDegreeOfParallelism must be greater than 0.", nameof(request));

        if (request.BufferSizeBytes <= 0)
            throw new ArgumentException("BufferSizeBytes must be greater than 0.", nameof(request));

        if (request.MaxOpenFiles < 2)
            throw new ArgumentException("MaxOpenFiles must be at least 2.", nameof(request));

        if (request.MinChunkSizeMb > request.MaxChunkSizeMb)
            throw new ArgumentException("MinChunkSizeMb cannot exceed MaxChunkSizeMb.", nameof(request));
    }
}
