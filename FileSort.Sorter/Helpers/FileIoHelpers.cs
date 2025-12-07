using FileSort.Sorter.Configuration;

namespace FileSort.Sorter.Helpers;

/// <summary>
///     Provides utilities for creating file I/O streams and performing file operations.
/// </summary>
internal static class FileIOHelpers
{
    public static StreamReader CreateFileReader(string filePath, int bufferSize)
    {
        var fileStream = new FileStream(
            filePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize,
            FileOptions.SequentialScan | FileOptions.Asynchronous);

        return new StreamReader(fileStream, SortConstants.FileEncoding, bufferSize: bufferSize, leaveOpen: false);
    }

    /// <summary>
    ///     Creates a StreamWriter for writing to a file with the specified buffer size.
    /// </summary>
    /// <param name="filePath">The path to the file to write.</param>
    /// <param name="bufferSize">The buffer size in bytes for the file stream.</param>
    /// <returns>A StreamWriter configured for sequential, asynchronous writing.</returns>
    public static StreamWriter CreateFileWriter(string filePath, int bufferSize)
    {
        var fileStream = new FileStream(
            filePath,
            FileMode.Create,
            FileAccess.Write,
            FileShare.None,
            bufferSize,
            FileOptions.SequentialScan | FileOptions.Asynchronous);

        return new StreamWriter(fileStream, SortConstants.FileEncoding, bufferSize);
    }

    /// <summary>
    ///     Synchronously copies a file from source to destination, creating the destination directory if needed.
    /// </summary>
    /// <param name="sourceFile">The path of the source file.</param>
    /// <param name="destinationFile">The path of the destination file.</param>
    public static void CopyFile(string sourceFile, string destinationFile)
    {
        var directory = Path.GetDirectoryName(destinationFile);
        if (!string.IsNullOrWhiteSpace(directory)) Directory.CreateDirectory(directory);
        File.Copy(sourceFile, destinationFile, true);
    }

    /// <summary>
    ///     Asynchronously copies a file from source to destination, creating the destination directory if needed.
    /// </summary>
    /// <param name="sourceFile">The path of the source file.</param>
    /// <param name="destinationFile">The path of the destination file.</param>
    /// <param name="bufferSize">
    ///     The buffer size in bytes for the copy operation. Defaults to
    ///     <see cref="SortConstants.DefaultCopyBufferSize" />.
    /// </param>
    /// <param name="cancellationToken">Cancellation token to cancel the copy operation.</param>
    public static async Task CopyFileAsync(
        string sourceFile,
        string destinationFile,
        int bufferSize = SortConstants.DefaultCopyBufferSize,
        CancellationToken cancellationToken = default)
    {
        var directory = Path.GetDirectoryName(destinationFile);
        if (!string.IsNullOrWhiteSpace(directory)) Directory.CreateDirectory(directory);

        await using var sourceStream = new FileStream(
            sourceFile,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize,
            FileOptions.SequentialScan | FileOptions.Asynchronous);

        await using var destinationStream = new FileStream(
            destinationFile,
            FileMode.Create,
            FileAccess.Write,
            FileShare.None,
            bufferSize,
            FileOptions.SequentialScan | FileOptions.Asynchronous);

        await sourceStream.CopyToAsync(destinationStream, bufferSize, cancellationToken);
    }

    /// <summary>
    ///     Safely deletes a file, ignoring any errors that occur during deletion.
    ///     This is used for cleanup operations where failures should not affect the main operation.
    ///     For better error visibility, consider using <see cref="TempFileCleanupService" /> with logging.
    /// </summary>
    /// <param name="filePath">The path of the file to delete.</param>
    /// <remarks>
    ///     Exceptions are silently swallowed to ensure cleanup failures don't break the main operation.
    ///     If logging is needed, use <see cref="TempFileCleanupService" /> instead.
    /// </remarks>
    public static void SafeDeleteFile(string filePath)
    {
        try
        {
            if (File.Exists(filePath)) File.Delete(filePath);
        }
        catch
        {
            // Ignore deletion errors - cleanup failures shouldn't break the main operation
            // For logging, use TempFileCleanupService instead
        }
    }

    /// <summary>
    ///     Safely deletes multiple files, ignoring any errors that occur during deletion.
    ///     This is used for cleanup operations where failures should not affect the main operation.
    ///     For better error visibility, consider using <see cref="TempFileCleanupService" /> with logging.
    /// </summary>
    /// <param name="filePaths">The paths of the files to delete.</param>
    /// <remarks>
    ///     Exceptions are silently swallowed to ensure cleanup failures don't break the main operation.
    ///     If logging is needed, use <see cref="TempFileCleanupService" /> instead.
    /// </remarks>
    public static void SafeDeleteFiles(IEnumerable<string> filePaths)
    {
        foreach (var filePath in filePaths) SafeDeleteFile(filePath);
    }

    /// <summary>
    ///     Ensures that the specified directory exists, creating it if necessary.
    /// </summary>
    /// <param name="directory">The path of the directory to ensure exists.</param>
    public static void EnsureDirectoryExists(string directory)
    {
        if (string.IsNullOrWhiteSpace(directory)) return;
        Directory.CreateDirectory(directory);
    }

    /// <summary>
    ///     Generates a file path by combining the directory with a formatted template string.
    /// </summary>
    /// <param name="directory">The directory path.</param>
    /// <param name="template">The template string (e.g., "chunk_{0}.tmp").</param>
    /// <param name="index">The index value to format into the template.</param>
    /// <returns>The generated file path.</returns>
    public static string GenerateFilePath(string directory, string template, int index)
    {
        return Path.Combine(directory, string.Format(template, index));
    }

    /// <summary>
    ///     Generates an intermediate file path for merge operations.
    /// </summary>
    /// <param name="directory">The directory path.</param>
    /// <param name="passNumber">The merge pass number.</param>
    /// <param name="batchIndex">The batch index within the pass.</param>
    /// <returns>The generated intermediate file path (e.g., "merge_pass1_batch0000.tmp").</returns>
    public static string GenerateIntermediateFilePath(string directory, int passNumber, int batchIndex)
    {
        return Path.Combine(directory, $"merge_pass{passNumber}_batch{batchIndex:0000}.tmp");
    }
}