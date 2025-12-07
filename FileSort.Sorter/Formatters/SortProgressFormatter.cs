using FileSort.Core.Models.Progress;

namespace FileSort.Sorter.Formatters;

/// <summary>
///     Formats sort progress information for display.
/// </summary>
public static class SortProgressFormatter
{
    /// <summary>
    ///     Formats sort progress as a string.
    /// </summary>
    public static string Format(SortProgress progress)
    {
        var parts = new List<string>();

        if (progress.ChunksCreated > 0 || progress.ChunksMerged > 0)
            parts.Add($"Chunks: {progress.ChunksCreated} created, {progress.ChunksMerged} merged");

        if (progress.CurrentMergePass.HasValue && progress.TotalMergePasses > 0)
            parts.Add($"Pass {progress.CurrentMergePass.Value}/{progress.TotalMergePasses}");

        if (progress.TotalBatchesInPass > 0)
            parts.Add($"Batch {progress.CurrentBatchInPass}/{progress.TotalBatchesInPass}");

        return string.Join(" - ", parts);
    }
}