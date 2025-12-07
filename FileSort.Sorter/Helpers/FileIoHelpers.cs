namespace FileSort.Sorter.Helpers;

/// <summary>
/// Provides utilities for creating file I/O streams and performing file operations.
/// </summary>
internal static class FileIoHelpers
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

        return new StreamReader(fileStream, WriteBufferHelpers.FileEncoding, bufferSize: bufferSize, leaveOpen: false);
    }

    public static StreamWriter CreateFileWriter(string filePath, int bufferSize)
    {
        var fileStream = new FileStream(
            filePath,
            FileMode.Create,
            FileAccess.Write,
            FileShare.None,
            bufferSize,
            FileOptions.SequentialScan | FileOptions.Asynchronous);

        return new StreamWriter(fileStream, WriteBufferHelpers.FileEncoding, bufferSize: bufferSize);
    }

    public static void CopyFile(string sourceFile, string destinationFile)
    {
        string? directory = Path.GetDirectoryName(destinationFile);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }
        File.Copy(sourceFile, destinationFile, overwrite: true);
    }

    public static async Task CopyFileAsync(
        string sourceFile,
        string destinationFile,
        int bufferSize = 81920,
        CancellationToken cancellationToken = default)
    {
        string? directory = Path.GetDirectoryName(destinationFile);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

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

    public static void SafeDeleteFile(string filePath)
    {
        try
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
        catch
        {
            // Ignore deletion errors
        }
    }

    public static void SafeDeleteFiles(IEnumerable<string> filePaths)
    {
        foreach (string filePath in filePaths)
        {
            SafeDeleteFile(filePath);
        }
    }

    public static void EnsureDirectoryExists(string directory)
    {
        if (string.IsNullOrWhiteSpace(directory))
        {
            return;
        }
        Directory.CreateDirectory(directory);
    }

    public static string GenerateFilePath(string directory, string template, int index)
    {
        return Path.Combine(directory, string.Format(template, index));
    }

    public static string GenerateIntermediateFilePath(string directory, int passNumber, int batchIndex)
    {
        return Path.Combine(directory, $"merge_pass{passNumber}_batch{batchIndex:0000}.tmp");
    }
}

