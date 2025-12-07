using FileSort.Core.Models;
using FileSort.Core.Models.Progress;
using FileSort.Core.Requests;

namespace FileSort.Core.Interfaces;

/// <summary>
/// Generates test files containing records in the format "{Number}. {Text}" for testing file sorting algorithms.
/// The generator creates files of a specified target size with configurable duplicate ratios and random number ranges.
/// </summary>
public interface ITestFileGenerator
{
    /// <summary>
    /// Generates a test file with records in the format "{Number}. {Text}" until the target file size is reached.
    /// </summary>
    /// <param name="request">The generation request containing output path, target size, number ranges, duplicate ratio, and other configuration parameters.</param>
    /// <param name="progress">Optional progress reporter to receive updates about bytes and lines written during generation.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the generation operation.</param>
    /// <returns>A task that represents the asynchronous generation operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="request"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="request"/> contains invalid parameters:
    /// <list type="bullet">
    /// <item><description>OutputFilePath is null, empty, or whitespace</description></item>
    /// <item><description>TargetSizeBytes is less than or equal to zero</description></item>
    /// <item><description>MinNumber is negative</description></item>
    /// <item><description>MaxNumber is less than MinNumber</description></item>
    /// <item><description>DuplicateRatioPercent is not between 0 and 100</description></item>
    /// <item><description>BufferSizeBytes is less than or equal to zero</description></item>
    /// </list>
    /// </exception>
    /// <exception cref="OperationCanceledException">Thrown when the operation is cancelled via <paramref name="cancellationToken"/>.</exception>
    Task GenerateAsync(GeneratorRequest request, IProgress<GeneratorProgress>? progress = null, CancellationToken cancellationToken = default);
}
