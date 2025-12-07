namespace FileSort.Sorter.Strategies;

/// <summary>
/// Factory for creating appropriate merge strategies based on file count and constraints.
/// </summary>
internal static class MergeStrategyFactory
{
    /// <summary>
    /// Creates the appropriate merge strategy based on file count and system constraints.
    /// </summary>
    /// <param name="fileCount">Number of files to merge</param>
    /// <param name="maxOpenFiles">Maximum number of files that can be opened simultaneously</param>
    /// <param name="bufferSize">Buffer size in bytes for file I/O operations</param>
    /// <returns>An appropriate merge strategy implementation</returns>
    public static IMergeStrategy CreateStrategy(
        int fileCount,
        int maxOpenFiles,
        int bufferSize)
    {
        if (fileCount <= maxOpenFiles)
        {
            return new SinglePassMerger(bufferSize);
        }

        return new MultiPassMerger(maxOpenFiles, bufferSize);
    }
}

