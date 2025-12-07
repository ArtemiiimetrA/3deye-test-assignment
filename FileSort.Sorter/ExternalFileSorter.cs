using FileSort.Core.Interfaces;
using FileSort.Core.Models;
using FileSort.Core.Models.Progress;
using FileSort.Core.Requests;
using FileSort.Sorter.Coordinators;
using FileSort.Sorter.Helpers;
using FileSort.Sorter.Processors;
using FileSort.Sorter.Validation;

namespace FileSort.Sorter;

/// <summary>
/// External merge sort implementation that handles very large files by:
/// 1. Chunking: Reading chunks, sorting in memory, writing to temp files
/// 2. Merging: K-way merge of sorted chunks (single-pass or multi-pass cascading)
/// </summary>
public sealed class ExternalFileSorter : IExternalSorter
{
    /// <inheritdoc />
    public async Task SortAsync(
        SortRequest request,
        IProgress<SortProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        SortRequestValidator.Validate(request);
        FileIoHelpers.EnsureDirectoryExists(request.TempDirectory);

        long totalBytes = SizeHelpers.GetFileSize(request.InputFilePath);

        // Phase 1: Create sorted chunks
        using var coordinator = new ChunkCreationCoordinator(request, totalBytes, progress);
        List<string> chunkFiles = await coordinator.ProcessAsync(cancellationToken);

        try
        {
            // Phase 2: Merge chunks
            await MergeChunksAsync(request, chunkFiles, progress, cancellationToken);
        }
        finally
        {
            CleanupTempFiles(chunkFiles, request.DeleteTempFiles);
        }
    }

    private static async Task MergeChunksAsync(
        SortRequest request,
        List<string> chunkFiles,
        IProgress<SortProgress>? progress,
        CancellationToken cancellationToken)
    {
        var mergeProcessor = new MergeProcessor(request.MaxOpenFiles, request.BufferSizeBytes);
        await mergeProcessor.MergeChunksAsync(chunkFiles, request.OutputFilePath, progress, cancellationToken);
    }

    private static void CleanupTempFiles(List<string> chunkFiles, bool deleteTempFiles)
    {
        if (!deleteTempFiles)
        {
            return;
        }

        FileIoHelpers.SafeDeleteFiles(chunkFiles);
    }
}
