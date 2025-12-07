using FileSort.Core.Models.Progress;
using FileSort.Sorter.Configuration;

namespace FileSort.Sorter.Helpers;

/// <summary>
///     Provides utilities for reporting sort progress.
/// </summary>
internal static class SortProgressReporter
{
    /// <summary>
    ///     Reports progress if the interval threshold has been crossed or the total has been reached.
    ///     Uses interval-based reporting to avoid excessive progress updates.
    /// </summary>
    /// <param name="bytesRead">The number of bytes read so far.</param>
    /// <param name="chunkIndex">The current chunk index.</param>
    /// <param name="totalBytes">The total number of bytes to process.</param>
    /// <param name="progress">The progress reporter to use, or null to skip reporting.</param>
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
        var currentInterval = bytesRead / SortConstants.ProgressReportIntervalBytes;
        var previousInterval = bytesRead == 0 ? -1 : (bytesRead - 1) / SortConstants.ProgressReportIntervalBytes;

        if (currentInterval > previousInterval || bytesRead >= totalBytes) Report(chunkIndex, progress);
    }

    /// <summary>
    ///     Reports the number of chunks created.
    /// </summary>
    /// <param name="chunksCreated">The number of chunks created so far.</param>
    /// <param name="progress">The progress reporter to use, or null to skip reporting.</param>
    public static void Report(
        int chunksCreated,
        IProgress<SortProgress>? progress)
    {
        progress?.Report(new SortProgress
        {
            ChunksCreated = chunksCreated
        });
    }

    /// <summary>
    ///     Reports the final number of chunks created (alias for Report).
    /// </summary>
    /// <param name="chunksCreated">The final number of chunks created.</param>
    /// <param name="progress">The progress reporter to use, or null to skip reporting.</param>
    public static void ReportFinal(
        int chunksCreated,
        IProgress<SortProgress>? progress)
    {
        Report(chunksCreated, progress);
    }

    /// <summary>
    ///     Reports merge progress if the interval threshold has been crossed.
    ///     Uses interval-based reporting to avoid excessive progress updates.
    /// </summary>
    /// <param name="recordsMerged">The number of records merged so far.</param>
    /// <param name="progress">The progress reporter to use, or null to skip reporting.</param>
    public static void ReportMergeProgress(
        int recordsMerged,
        IProgress<SortProgress>? progress)
    {
        if (progress == null)
            return;

        // Use interval-based reporting: report when records merged is a multiple of the interval
        // Use division to check if we've crossed an interval boundary
        var currentInterval = recordsMerged / SortConstants.ProgressReportIntervalRecords;
        var previousInterval =
            recordsMerged == 0 ? -1 : (recordsMerged - 1) / SortConstants.ProgressReportIntervalRecords;

        if (currentInterval > previousInterval)
            progress.Report(new SortProgress
            {
                ChunksMerged = recordsMerged
            });
    }
}