using FileSort.Core.Models;

namespace FileSort.Sorter.Formatters;

/// <summary>
/// Formats sort progress information for display.
/// </summary>
public static class SortProgressFormatter
{
    /// <summary>
    /// Formats sort progress as a string.
    /// </summary>
    public static string Format(SortProgress progress)
    {
        double percent = progress.TotalBytes > 0 
            ? (double)progress.BytesProcessed / progress.TotalBytes * 100 
            : 0;

        var parts = new List<string>
        {
            $"Progress: {percent:F2}%"
        };

        if (progress.ChunksCreated > 0 || progress.ChunksMerged > 0)
        {
            parts.Add($"Chunks: {progress.ChunksCreated} created, {progress.ChunksMerged} merged");
        }

        if (progress.TotalMergePasses > 0)
        {
            parts.Add($"Pass {progress.CurrentMergePass}/{progress.TotalMergePasses}");
        }

        if (progress.TotalBatchesInPass > 0)
        {
            parts.Add($"Batch {progress.CurrentBatchInPass}/{progress.TotalBatchesInPass}");
        }

        return string.Join(" - ", parts);
    }
}

