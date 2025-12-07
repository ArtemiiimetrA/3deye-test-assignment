using FileSort.Core.Models;
using FileSort.Sorter.Configuration;

namespace FileSort.Sorter.Helpers;

/// <summary>
/// Provides utilities for reporting sort progress.
/// </summary>
internal static class SortProgressReporter
{
    public static void ReportIfNeeded(
        long bytesRead,
        int chunkIndex,
        long totalBytes,
        IProgress<SortProgress>? progress)
    {
        if (bytesRead % SortConstants.ProgressReportIntervalBytes == 0)
        {
            Report(chunkIndex, bytesRead, totalBytes, progress);
        }
    }

    public static void Report(
        int chunksCreated,
        long bytesProcessed,
        long totalBytes,
        IProgress<SortProgress>? progress)
    {
        progress?.Report(new SortProgress
        {
            ChunksCreated = chunksCreated,
            BytesProcessed = bytesProcessed,
            TotalBytes = totalBytes
        });
    }

    public static void ReportFinal(
        int chunksCreated,
        long totalBytes,
        IProgress<SortProgress>? progress)
    {
        Report(chunksCreated, totalBytes, totalBytes, progress);
    }

    public static void ReportMergeProgress(
        int recordsMerged,
        IProgress<SortProgress>? progress)
    {
        if (recordsMerged % SortConstants.ProgressReportIntervalRecords == 0)
        {
            progress?.Report(new SortProgress
            {
                ChunksMerged = recordsMerged
            });
        }
    }
}

