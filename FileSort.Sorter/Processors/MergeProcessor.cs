using FileSort.Core.Models;
using FileSort.Sorter.Helpers;
using FileSort.Sorter.Strategies;

namespace FileSort.Sorter.Processors;

/// <summary>
/// Coordinates merge operations, selecting the appropriate merge strategy
/// based on the number of files and system constraints.
/// </summary>
internal sealed class MergeProcessor
{
    private readonly int _maxOpenFiles;
    private readonly int _bufferSize;

    public MergeProcessor(int maxOpenFiles, int bufferSize)
    {
        _maxOpenFiles = maxOpenFiles;
        _bufferSize = bufferSize;
    }

    public async Task MergeChunksAsync(
        IReadOnlyList<string> chunkFilePaths,
        string outputFilePath,
        IProgress<SortProgress>? progress,
        CancellationToken cancellationToken)
    {
        // Handle edge cases
        switch (chunkFilePaths.Count)
        {
            case 0:
                await CreateEmptyOutputFile(outputFilePath);
                return;
            case 1:
                CopySingleFileToOutput(chunkFilePaths[0], outputFilePath);
                return;
        }

        // Use Strategy pattern - factory selects the appropriate strategy
        IMergeStrategy strategy = MergeStrategyFactory.CreateStrategy(
            chunkFilePaths.Count,
            _maxOpenFiles,
            _bufferSize);

        await strategy.MergeAsync(
            chunkFilePaths,
            outputFilePath,
            progress,
            cancellationToken);
    }

    private static async Task CreateEmptyOutputFile(string outputFilePath)
    {
        string? directory = Path.GetDirectoryName(outputFilePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }
        await File.Create(outputFilePath).DisposeAsync();
    }

    private static void CopySingleFileToOutput(string sourceFile, string outputFile)
    {
        FileIoHelpers.CopyFile(sourceFile, outputFile);
    }
}
