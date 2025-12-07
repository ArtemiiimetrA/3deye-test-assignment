using FileSort.Core.Interfaces;
using FileSort.Core.Requests;
using FileSort.Sorter;
using Xunit;

namespace FileSort.Sorter.Tests;

public class TempFileCleanupTests
{
    private readonly IExternalSorter _sorter = new ExternalFileSorter();

    private static SortRequest CreateBaseRequest(string inputPath, string outputPath, string tempDir) => new SortRequest
    {
        InputFilePath = inputPath,
        OutputFilePath = outputPath,
        TempDirectory = tempDir,
        MaxRamMb = 100,
        ChunkSizeMb = 1,
        MaxDegreeOfParallelism = Environment.ProcessorCount,
        FileChunkTemplate = "chunk_{0:0000}.tmp",
        BufferSizeBytes = 4 * 1024 * 1024,
        DeleteTempFiles = true,
        MaxOpenFiles = 500,
        MaxMergeParallelism = 2,
        AdaptiveChunkSize = false,
        MinChunkSizeMb = 64,
        MaxChunkSizeMb = 512
    };

    [Fact]
    public async Task SortAsync_DeleteTempFilesTrue_DeletesTempFiles()
    {
        var lines = Enumerable.Range(1, 10000).Select(i => $"{i % 100}. Test{i}").ToList();
        string inputPath = await TestHelpers.CreateTestFileAsync(lines);
        string outputPath = Path.GetTempFileName();
        string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        try
        {
            var request = CreateBaseRequest(inputPath, outputPath, tempDir);

            await _sorter.SortAsync(request);

            // Temp files should be deleted
            var tempFiles = Directory.GetFiles(tempDir, "chunk_*.tmp");
            Assert.Empty(tempFiles);
        }
        finally
        {
            Cleanup(inputPath, outputPath, tempDir);
        }
    }

    [Fact]
    public async Task SortAsync_DeleteTempFilesFalse_KeepsTempFiles()
    {
        var lines = Enumerable.Range(1, 10000).Select(i => $"{i % 100}. Test{i}").ToList();
        string inputPath = await TestHelpers.CreateTestFileAsync(lines);
        string outputPath = Path.GetTempFileName();
        string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        try
        {
            var request = CreateBaseRequest(inputPath, outputPath, tempDir) with
            {
                DeleteTempFiles = false
            };

            await _sorter.SortAsync(request);

            // Temp files should still exist
            var tempFiles = Directory.GetFiles(tempDir, "chunk_*.tmp");
            Assert.NotEmpty(tempFiles);
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
                Directory.Delete(tempDir, recursive: true);
        }
        catch
        {
            // Ignore cleanup errors
        }
    }
}

