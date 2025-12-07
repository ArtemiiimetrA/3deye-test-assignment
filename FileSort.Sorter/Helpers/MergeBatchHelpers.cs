namespace FileSort.Sorter.Helpers;

/// <summary>
/// Provides utilities for calculating batch sizes and merge passes.
/// </summary>
internal static class MergeBatchHelpers
{
    /// <summary>
    /// Calculates the batch size for merging, reserving one file handle for output.
    /// </summary>
    public static int CalculateBatchSize(int maxOpenFiles)
    {
        return maxOpenFiles - 1; // Reserve 1 for output
    }

    /// <summary>
    /// Calculates the total number of batches needed for a given file count.
    /// </summary>
    public static int CalculateTotalBatches(int fileCount, int batchSize)
    {
        return (int)Math.Ceiling((double)fileCount / batchSize);
    }

    /// <summary>
    /// Gets a batch of files from the list starting at the specified index.
    /// </summary>
    public static List<string> GetBatch(List<string> files, int startIndex, int batchSize)
    {
        int count = Math.Min(batchSize, files.Count - startIndex);
        return files.GetRange(startIndex, count);
    }

    /// <summary>
    /// Calculates the total number of merge passes needed.
    /// </summary>
    public static int CalculateTotalPasses(int fileCount, int maxOpenFiles)
    {
        if (fileCount <= maxOpenFiles)
        {
            return 1;
        }

        int passes = 0;
        int currentCount = fileCount;
        int batchSize = maxOpenFiles - 1;

        while (currentCount > 1)
        {
            currentCount = (int)Math.Ceiling((double)currentCount / batchSize);
            passes++;
        }

        return passes;
    }
}

