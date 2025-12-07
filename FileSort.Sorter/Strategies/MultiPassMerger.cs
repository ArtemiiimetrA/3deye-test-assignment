using FileSort.Core.Models;
using FileSort.Core.Models.Progress;
using FileSort.Sorter.Helpers;
using FileSort.Sorter.Configuration;

namespace FileSort.Sorter.Strategies;

/// <summary>
/// Performs k-way merge using cascading multi-pass strategy.
/// Files are merged in batches when they exceed the maximum open file limit.
/// </summary>
internal sealed class MultiPassMerger : IMergeStrategy
{
    private readonly int _maxOpenFiles;
    private readonly SinglePassMerger _singlePassMerger;

    public MultiPassMerger(int maxOpenFiles, int bufferSize)
    {
        _maxOpenFiles = maxOpenFiles;
        _singlePassMerger = new SinglePassMerger(bufferSize);
    }

    public async Task MergeAsync(
        IReadOnlyList<string> initialFiles,
        string outputPath,
        IProgress<SortProgress>? progress,
        CancellationToken cancellationToken)
    {
        List<string> currentFiles = new(initialFiles);
        List<string> intermediateFiles = new(); // Track all intermediate files for cleanup
        string tempDir = GetTempDirectory(initialFiles);
        int passNumber = 1;

        int totalPasses = CalculateTotalPasses(initialFiles.Count, _maxOpenFiles);
        ReportTotalPasses(totalPasses, progress);

        try
        {
            while (currentFiles.Count > 1)
            {
                ReportCurrentPass(passNumber, totalPasses, progress);

                List<string> nextPassFiles = await ProcessMergePassAsync(
                    currentFiles,
                    tempDir,
                    passNumber,
                    progress,
                    cancellationToken);

                // Track intermediate files for cleanup
                if (passNumber > 1)
                {
                    intermediateFiles.AddRange(currentFiles);
                }

                CleanupPreviousPassFiles(currentFiles, passNumber);

                currentFiles = nextPassFiles;
                passNumber++;
            }

            await CopyFinalFileToOutputAsync(currentFiles, outputPath, cancellationToken);
        }
        finally
        {
            // Ensure cleanup of any remaining intermediate files on exception
            if (intermediateFiles.Count > 0)
            {
                FileIoHelpers.SafeDeleteFiles(intermediateFiles);
            }
        }
    }

    private static string GetTempDirectory(IReadOnlyList<string> files)
    {
        string? directory = Path.GetDirectoryName(files[0]);
        if (string.IsNullOrWhiteSpace(directory))
        {
            throw new InvalidOperationException($"Cannot determine temp directory from file path: {files[0]}");
        }
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
        List<string> nextPassFiles = new();
        int batchSize = CalculateBatchSize();
        int totalBatches = CalculateTotalBatches(currentFiles.Count, batchSize);

        for (int i = 0; i < currentFiles.Count; i += batchSize)
        {
            cancellationToken.ThrowIfCancellationRequested();

            int batchIndex = i / batchSize;
            var batch = GetBatch(currentFiles, i, batchSize);

            ReportBatchProgress(passNumber, totalBatches, batchIndex, progress);

            string intermediateFile = GenerateIntermediateFilePath(tempDir, passNumber, batchIndex);
            await MergeBatchAsync(batch, intermediateFile, cancellationToken);

            nextPassFiles.Add(intermediateFile);
        }

        return nextPassFiles;
    }

    private int CalculateBatchSize()
    {
        return MergeBatchHelpers.CalculateBatchSize(_maxOpenFiles);
    }

    private static int CalculateTotalBatches(int fileCount, int batchSize)
    {
        return MergeBatchHelpers.CalculateTotalBatches(fileCount, batchSize);
    }

    private static List<string> GetBatch(List<string> files, int startIndex, int batchSize)
    {
        return MergeBatchHelpers.GetBatch(files, startIndex, batchSize);
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

    private static string GenerateIntermediateFilePath(string tempDir, int passNumber, int batchIndex)
    {
        return FileIoHelpers.GenerateIntermediateFilePath(tempDir, passNumber, batchIndex);
    }

    private async Task MergeBatchAsync(
        List<string> batch,
        string intermediateFile,
        CancellationToken cancellationToken)
    {
        if (batch.Count == 1)
        {
            await FileIoHelpers.CopyFileAsync(batch[0], intermediateFile, cancellationToken: cancellationToken);
        }
        else
        {
            await _singlePassMerger.MergeAsync(batch, intermediateFile, null, cancellationToken);
        }
    }

    private static void CleanupPreviousPassFiles(List<string> files, int passNumber)
    {
        if (passNumber <= 1)
        {
            return; // Don't delete initial chunks
        }

        FileIoHelpers.SafeDeleteFiles(files);
    }

    private static async Task CopyFinalFileToOutputAsync(
        List<string> currentFiles, 
        string outputPath,
        CancellationToken cancellationToken)
    {
        if (currentFiles.Count == 1)
        {
            await FileIoHelpers.CopyFileAsync(currentFiles[0], outputPath, cancellationToken: cancellationToken);
            FileIoHelpers.SafeDeleteFile(currentFiles[0]);
        }
    }

    private static int CalculateTotalPasses(int fileCount, int maxOpenFiles)
    {
        return MergeBatchHelpers.CalculateTotalPasses(fileCount, maxOpenFiles);
    }
}

