using FileSort.Core.Models.Progress;

namespace FileSort.Sorter.Strategies;

/// <summary>
///     Defines the contract for merge strategies.
/// </summary>
/// <remarks>
///     Different strategies are used based on system constraints:
///     - SinglePass: When file count ≤ maxOpenFiles (all files can be opened simultaneously)
///     - MultiPass: When file count > maxOpenFiles (requires cascading merge passes)
/// </remarks>
internal interface IMergeStrategy
{
    /// <summary>
    ///     Merges sorted files into a single output file.
    /// </summary>
    /// <param name="filePaths">Paths to the sorted input files to merge</param>
    /// <param name="outputPath">Path where the merged output file will be written</param>
    /// <param name="progress">Optional progress reporter for tracking merge progress</param>
    /// <param name="cancellationToken">Cancellation token to cancel the merge operation</param>
    Task MergeAsync(
        IReadOnlyList<string> filePaths,
        string outputPath,
        IProgress<SortProgress>? progress,
        CancellationToken cancellationToken);
}