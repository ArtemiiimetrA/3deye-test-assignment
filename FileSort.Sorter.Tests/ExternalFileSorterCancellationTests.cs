using FileSort.Core.Interfaces;
using FileSort.Core.Requests;
using Xunit;

namespace FileSort.Sorter.Tests;

public class ExternalFileSorterCancellationTests
{
    private readonly IExternalSorter _sorter = new ExternalFileSorter();

    private static SortRequest CreateBaseRequest(string inputPath, string outputPath, string tempDir)
    {
        return new SortRequest()
        {
            InputFilePath = inputPath,
            OutputFilePath = outputPath,
            TempDirectory = tempDir,
            MaxRamMb = 100,
            ChunkSizeMb = 1,
            MaxDegreeOfParallelism = 1,
            FileChunkTemplate = "chunk_{0:0000}.tmp",
            BufferSizeBytes = 4 * 1024 * 1024,
            DeleteTempFiles = true,
            MaxOpenFiles = 500,
            MaxMergeParallelism = 2,
            AdaptiveChunkSize = false,
            MinChunkSizeMb = 64,
            MaxChunkSizeMb = 512
        };
    }

    [Fact]
    public async Task SortAsync_CancellationDuringChunking_ThrowsOperationCanceledException()
    {
        var inputPath = await TestHelpers.CreateTestFileAsync(
            Enumerable.Range(1, 10000).Select(i => $"{i}. Test{i}"));
        var outputPath = Path.GetTempFileName();
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        try
        {
            var request = CreateBaseRequest(inputPath, outputPath, tempDir);

            using var cts = new CancellationTokenSource();
            cts.CancelAfter(10); // Cancel quickly

            await Assert.ThrowsAnyAsync<OperationCanceledException>(
                () => _sorter.SortAsync(request, cancellationToken: cts.Token));
        }
        finally
        {
            Cleanup(inputPath, outputPath, tempDir);
        }
    }

    [Fact]
    public async Task SortAsync_AlreadyCancelled_ThrowsImmediately()
    {
        var inputPath = await TestHelpers.CreateTestFileAsync(new[] { "1. Test" });
        var outputPath = Path.GetTempFileName();
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        try
        {
            var request = CreateBaseRequest(inputPath, outputPath, tempDir);

            using var cts = new CancellationTokenSource();
            cts.Cancel();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(
                () => _sorter.SortAsync(request, cancellationToken: cts.Token));
        }
        finally
        {
            Cleanup(inputPath, outputPath, tempDir);
        }
    }

    private static void Cleanup(string inputPath, string outputPath, string tempDir)
    {
        try
        {
            if (File.Exists(inputPath))
                File.Delete(inputPath);
            if (File.Exists(outputPath))
                File.Delete(outputPath);
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
        }
        catch
        {
            // Ignore cleanup errors
        }
    }
}