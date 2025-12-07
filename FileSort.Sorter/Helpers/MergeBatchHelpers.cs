namespace FileSort.Sorter.Helpers;

/// <summary>
///     Provides utilities for calculating batch sizes and merge passes.
/// </summary>
internal static class MergeBatchHelpers
{
    /// <summary>
    ///     Calculates the batch size for merging, reserving one file handle for output.
    /// </summary>
    /// <param name="maxOpenFiles">The maximum number of files that can be opened simultaneously.</param>
    /// <returns>The batch size (maxOpenFiles - 1).</returns>
    public static int CalculateBatchSize(int maxOpenFiles)
    {
        return maxOpenFiles - 1; // Reserve 1 for output
    }

    /// <summary>
    ///     Calculates the total number of batches needed for a given file count.
    /// </summary>
    /// <param name="fileCount">The total number of files to batch.</param>
    /// <param name="batchSize">The size of each batch.</param>
    /// <returns>The number of batches needed (rounded up).</returns>
    public static int CalculateTotalBatches(int fileCount, int batchSize)
    {
        return (int)Math.Ceiling((double)fileCount / batchSize);
    }

    /// <summary>
    ///     Gets a batch of files from the list starting at the specified index.
    /// </summary>
    /// <param name="files">The list of file paths.</param>
    /// <param name="startIndex">The starting index in the list.</param>
    /// <param name="batchSize">The maximum number of files to include in the batch.</param>
    /// <returns>A list containing up to batchSize files starting from startIndex.</returns>
    public static List<string> GetBatch(List<string> files, int startIndex, int batchSize)
    {
        var count = Math.Min(batchSize, files.Count - startIndex);
        return files.GetRange(startIndex, count);
    }

    /// <summary>
    ///     Calculates the total number of merge passes needed to merge all files given the maximum open files constraint.
    /// </summary>
    /// <param name="fileCount">The total number of files to merge.</param>
    /// <param name="maxOpenFiles">The maximum number of files that can be opened simultaneously.</param>
    /// <returns>
    ///     The number of merge passes required (1 if fileCount &lt;= maxOpenFiles, otherwise calculated based on
    ///     cascading merges).
    /// </returns>
    public static int CalculateTotalPasses(int fileCount, int maxOpenFiles)
    {
        if (fileCount <= maxOpenFiles) return 1;

        var passes = 0;
        var currentCount = fileCount;
        var batchSize = maxOpenFiles - 1;

        while (currentCount > 1)
        {
            currentCount = (int)Math.Ceiling((double)currentCount / batchSize);
            passes++;
        }

        return passes;
    }
}