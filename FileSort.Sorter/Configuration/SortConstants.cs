namespace FileSort.Sorter.Configuration;

/// <summary>
/// Constants used throughout the sorting process.
/// </summary>
internal static class SortConstants
{
    public const double AdaptiveMemoryThreshold = 0.8; // 80% of max memory before creating chunk
    public const int BytesPerMegabyte = 1024 * 1024;
    public const int ProgressReportIntervalBytes = 10 * BytesPerMegabyte; // Report progress every 10MB
    public const int ProgressReportIntervalRecords = 100000; // Report progress every 100k records
    public const int WriteBufferCapacity = 10000; // Buffer capacity for writing records
}

