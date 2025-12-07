using Microsoft.Extensions.Logging;

namespace FileSort.Sorter.Helpers;

/// <summary>
///     Provides centralized file cleanup operations with logging support.
/// </summary>
internal sealed class TempFileCleanupService
{
    private readonly ILogger<TempFileCleanupService>? _logger;

    public TempFileCleanupService(ILogger<TempFileCleanupService>? logger = null)
    {
        _logger = logger;
    }

    /// <summary>
    ///     Safely deletes a single file, logging any errors that occur.
    /// </summary>
    /// <param name="filePath">The path of the file to delete.</param>
    public void SafeDeleteFile(string filePath)
    {
        try
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                _logger?.LogDebug("Deleted temporary file: {FilePath}", filePath);
            }
        }
        catch (Exception ex)
        {
            // Log but don't throw - cleanup failures shouldn't break the main operation
            _logger?.LogWarning(ex, "Failed to delete temporary file: {FilePath}", filePath);
        }
    }

    /// <summary>
    ///     Safely deletes multiple files, logging any errors that occur.
    /// </summary>
    /// <param name="filePaths">The paths of the files to delete.</param>
    public void SafeDeleteFiles(IEnumerable<string> filePaths)
    {
        foreach (var filePath in filePaths) SafeDeleteFile(filePath);
    }
}