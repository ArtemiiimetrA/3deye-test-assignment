namespace FileSort.Sorter.Helpers;

/// <summary>
///     Provides utilities for handling cancellation tokens consistently across the codebase.
/// </summary>
internal static class CancellationHelpers
{
    /// <summary>
    ///     Wraps a TaskCanceledException and converts it to OperationCanceledException with proper cancellation token.
    ///     Call this in catch blocks when catching TaskCanceledException.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token to check and use in the exception.</param>
    /// <exception cref="OperationCanceledException">Thrown when the operation is cancelled.</exception>
    public static void HandleCancellation(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        throw new OperationCanceledException("Operation was cancelled.", cancellationToken);
    }
}