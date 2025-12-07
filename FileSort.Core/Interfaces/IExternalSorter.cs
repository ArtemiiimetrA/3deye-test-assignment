using FileSort.Core.Models;
using FileSort.Core.Requests;

namespace FileSort.Core.Interfaces;

/// <summary>
/// Performs external merge sort on large files that cannot fit entirely in memory.
/// The sorter processes files by chunking them into manageable pieces, sorting each chunk in memory,
/// and then merging the sorted chunks using a k-way merge algorithm.
/// </summary>
public interface IExternalSorter
{
    /// <summary>
    /// Sorts a large file using external merge sort algorithm.
    /// The file is processed in chunks that fit in memory, each chunk is sorted, and then all chunks are merged into the final sorted output.
    /// </summary>
    /// <param name="request">The sort request containing input file path, output file path, memory limits, chunk sizes, buffer sizes, and other configuration parameters.</param>
    /// <param name="progress">Optional progress reporter to receive updates about chunks created, bytes processed, and merge progress during sorting.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the sorting operation.</param>
    /// <returns>A task that represents the asynchronous sorting operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="request"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="request"/> contains invalid parameters:
    /// <list type="bullet">
    /// <item><description>InputFilePath is null, empty, or whitespace</description></item>
    /// <item><description>OutputFilePath is null, empty, or whitespace</description></item>
    /// <item><description>ChunkSizeMb is less than or equal to zero</description></item>
    /// <item><description>ChunkSizeMb exceeds MaxRamMb</description></item>
    /// <item><description>MaxRamMb is less than or equal to zero</description></item>
    /// <item><description>MaxDegreeOfParallelism is less than or equal to zero</description></item>
    /// <item><description>BufferSizeBytes is less than or equal to zero</description></item>
    /// <item><description>MaxOpenFiles is less than 2</description></item>
    /// <item><description>MinChunkSizeMb exceeds MaxChunkSizeMb</description></item>
    /// </list>
    /// </exception>
    /// <exception cref="FileNotFoundException">Thrown when the input file specified in <paramref name="request"/> does not exist.</exception>
    /// <exception cref="OperationCanceledException">Thrown when the operation is cancelled via <paramref name="cancellationToken"/>.</exception>
    Task SortAsync(SortRequest request, IProgress<SortProgress>? progress = null, CancellationToken cancellationToken = default);
}
