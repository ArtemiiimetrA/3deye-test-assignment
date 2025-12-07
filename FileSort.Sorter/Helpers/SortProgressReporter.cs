using FileSort.Core.Models;
using FileSort.Core.Models.Progress;
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
        if (progress == null)
            return;

        // Use interval-based reporting: report when bytes processed is a multiple of the interval
        // or when we've reached the total. This ensures we don't miss final updates.
        // Use division to check if we've crossed an interval boundary
        long currentInterval = bytesRead / SortConstants.ProgressReportIntervalBytes;
        long previousInterval = (bytesRead == 0) ? -1 : (bytesRead - 1) / SortConstants.ProgressReportIntervalBytes;
        
        if (currentInterval > previousInterval || bytesRead >= totalBytes)
        {
            Report(chunkIndex, progress);
        }
    }

    public static void Report(
        int chunksCreated,
        IProgress<SortProgress>? progress)
    {
        progress?.Report(new SortProgress
        {
            ChunksCreated = chunksCreated
        });
    }

    public static void ReportFinal(
        int chunksCreated,
        IProgress<SortProgress>? progress)
    {
        Report(chunksCreated, progress);
    }

    public static void ReportMergeProgress(
        int recordsMerged,
        IProgress<SortProgress>? progress)
    {
        if (progress == null)
            return;

        // Use interval-based reporting: report when records merged is a multiple of the interval
        // Use division to check if we've crossed an interval boundary
        int currentInterval = recordsMerged / SortConstants.ProgressReportIntervalRecords;
        int previousInterval = (recordsMerged == 0) ? -1 : (recordsMerged - 1) / SortConstants.ProgressReportIntervalRecords;
        
        if (currentInterval > previousInterval)
        {
            progress.Report(new SortProgress
            {
                ChunksMerged = recordsMerged
            });
        }
    }
}

