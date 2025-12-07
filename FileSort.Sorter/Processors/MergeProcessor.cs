using FileSort.Core.Models;
using FileSort.Core.Models.Progress;
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
    private readonly int _maxMergeParallelism;

    public MergeProcessor(int maxOpenFiles, int bufferSize, int maxMergeParallelism)
    {
        _maxOpenFiles = maxOpenFiles;
        _bufferSize = bufferSize;
        _maxMergeParallelism = maxMergeParallelism;
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
                await CopySingleFileToOutputAsync(chunkFilePaths[0], outputFilePath, cancellationToken);
                return;
        }

        // Use Strategy pattern - factory selects the appropriate strategy
        IMergeStrategy strategy = MergeStrategyFactory.CreateStrategy(
            chunkFilePaths.Count,
            _maxOpenFiles,
            _bufferSize,
            _maxMergeParallelism);

        try
        {
            await strategy.MergeAsync(
                chunkFilePaths,
                outputFilePath,
                progress,
                cancellationToken);
        }
        finally
        {
            // Dispose strategy if it implements IDisposable (e.g., MultiPassMerger with SemaphoreSlim)
            if (strategy is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
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

    private static async Task CopySingleFileToOutputAsync(
        string sourceFile, 
        string outputFile,
        CancellationToken cancellationToken)
    {
        await FileIoHelpers.CopyFileAsync(sourceFile, outputFile, cancellationToken: cancellationToken);
    }
}
