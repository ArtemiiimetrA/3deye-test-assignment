using FileSort.Core.Models.Progress;
using FileSort.Sorter.Helpers;

namespace FileSort.Sorter.Strategies;

/// <summary>
///     Performs k-way merge using cascading multi-pass strategy.
///     Files are merged in batches when they exceed the maximum open file limit.
///     Supports parallel batch processing to improve performance.
/// </summary>
internal sealed class MultiPassMerger : IMergeStrategy, IDisposable
{
    private readonly int _maxMergeParallelism;
    private readonly int _maxOpenFiles;
    private readonly SemaphoreSlim _semaphore;
    private readonly SinglePassMerger _singlePassMerger;

    public MultiPassMerger(int maxOpenFiles, int bufferSize, int maxMergeParallelism)
    {
        _maxOpenFiles = maxOpenFiles;
        _singlePassMerger = new SinglePassMerger(bufferSize);
        _maxMergeParallelism = maxMergeParallelism;
        _semaphore = new SemaphoreSlim(maxMergeParallelism);
    }

    public void Dispose()
    {
        _semaphore?.Dispose();
    }

    public async Task MergeAsync(
        IReadOnlyList<string> initialFiles,
        string outputPath,
        IProgress<SortProgress>? progress,
        CancellationToken cancellationToken)
    {
        List<string> currentFiles = new(initialFiles);
        List<string> intermediateFiles = new(); // Track all intermediate files for cleanup
        var tempDir = GetTempDirectory(initialFiles);
        var passNumber = 1;

        var totalPasses = MergeBatchHelpers.CalculateTotalPasses(initialFiles.Count, _maxOpenFiles);
        ReportTotalPasses(totalPasses, progress);

        try
        {
            while (currentFiles.Count > 1)
            {
                ReportCurrentPass(passNumber, totalPasses, progress);

                var nextPassFiles = await ProcessMergePassAsync(
                    currentFiles,
                    tempDir,
                    passNumber,
                    progress,
                    cancellationToken);

                // Track intermediate files for cleanup
                if (passNumber > 1) intermediateFiles.AddRange(currentFiles);

                CleanupPreviousPassFiles(currentFiles, passNumber);

                currentFiles = nextPassFiles;
                passNumber++;
            }

            await CopyFinalFileToOutputAsync(currentFiles, outputPath, cancellationToken);
        }
        finally
        {
            // Ensure cleanup of any remaining intermediate files on exception
            if (intermediateFiles.Count > 0) FileIOHelpers.SafeDeleteFiles(intermediateFiles);
        }
    }

    private static string GetTempDirectory(IReadOnlyList<string> files)
    {
        var directory = Path.GetDirectoryName(files[0]);
        if (string.IsNullOrWhiteSpace(directory))
            throw new InvalidOperationException($"Cannot determine temp directory from file path: {files[0]}");
        return directory;
    }

    private static void ReportTotalPasses(int totalPasses, IProgress<SortProgress>? progress)
    {
        progress?.Report(new SortProgress
        {
            TotalMergePasses = totalPasses
        });
    }

    private static void ReportCurrentPass(
        int passNumber,
        int totalPasses,
        IProgress<SortProgress>? progress)
    {
        progress?.Report(new SortProgress
        {
            CurrentMergePass = passNumber,
            TotalMergePasses = totalPasses
        });
    }

    private async Task<List<string>> ProcessMergePassAsync(
        List<string> currentFiles,
        string tempDir,
        int passNumber,
        IProgress<SortProgress>? progress,
        CancellationToken cancellationToken)
    {
        var batchSize = MergeBatchHelpers.CalculateBatchSize(_maxOpenFiles);
        var totalBatches = MergeBatchHelpers.CalculateTotalBatches(currentFiles.Count, batchSize);
        var batchTasks = new List<Task<string>>();

        for (var i = 0; i < currentFiles.Count; i += batchSize)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var batchIndex = i / batchSize;
            var batch = MergeBatchHelpers.GetBatch(currentFiles, i, batchSize);
            var intermediateFile = FileIOHelpers.GenerateIntermediateFilePath(tempDir, passNumber, batchIndex);

            var task = ProcessBatchWithSemaphoreAsync(
                batch,
                intermediateFile,
                passNumber,
                totalBatches,
                batchIndex,
                progress,
                cancellationToken);

            batchTasks.Add(task);
        }

        var nextPassFiles = await Task.WhenAll(batchTasks);
        return nextPassFiles.ToList();
    }

    private async Task<string> ProcessBatchWithSemaphoreAsync(
        List<string> batch,
        string intermediateFile,
        int passNumber,
        int totalBatches,
        int batchIndex,
        IProgress<SortProgress>? progress,
        CancellationToken cancellationToken)
    {
        try
        {
            await _semaphore.WaitAsync(cancellationToken);
        }
        catch (TaskCanceledException)
        {
            CancellationHelpers.HandleCancellation(cancellationToken);
        }

        try
        {
            ReportBatchProgress(passNumber, totalBatches, batchIndex, progress);
            await MergeBatchAsync(batch, intermediateFile, cancellationToken);
            return intermediateFile;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private static void ReportBatchProgress(
        int passNumber,
        int totalBatches,
        int batchIndex,
        IProgress<SortProgress>? progress)
    {
        progress?.Report(new SortProgress
        {
            CurrentMergePass = passNumber,
            TotalMergePasses = 0, // Will be set by caller
            CurrentBatchInPass = batchIndex + 1,
            TotalBatchesInPass = totalBatches
        });
    }

    private async Task MergeBatchAsync(
        List<string> batch,
        string intermediateFile,
        CancellationToken cancellationToken)
    {
        if (batch.Count == 1)
            await FileIOHelpers.CopyFileAsync(batch[0], intermediateFile, cancellationToken: cancellationToken);
        else
            await _singlePassMerger.MergeAsync(batch, intermediateFile, null, cancellationToken);
    }

    private static void CleanupPreviousPassFiles(List<string> files, int passNumber)
    {
        if (passNumber <= 1) return; // Don't delete initial chunks

        FileIOHelpers.SafeDeleteFiles(files);
    }

    private static async Task CopyFinalFileToOutputAsync(
        List<string> currentFiles,
        string outputPath,
        CancellationToken cancellationToken)
    {
        if (currentFiles.Count == 1)
        {
            await FileIOHelpers.CopyFileAsync(currentFiles[0], outputPath, cancellationToken: cancellationToken);
            FileIOHelpers.SafeDeleteFile(currentFiles[0]);
        }
    }
}