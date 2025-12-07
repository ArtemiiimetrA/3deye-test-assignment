using FileSort.Core.Interfaces;
using FileSort.Core.Requests;
using FileSort.Sorter;
using Xunit;

namespace FileSort.Sorter.Tests;

public class AdaptiveChunkSizeTests
{
    private readonly IExternalSorter _sorter = new ExternalFileSorter();

    [Fact]
    public async Task SortAsync_AdaptiveChunkSizeEnabled_UsesAdaptiveSizing()
    {
        // Create a file that will require multiple chunks
        var lines = new List<string>();
        var random = new Random(42);
        for (int i = 0; i < 50000; i++)
        {
            int number = random.Next(1, 1000);
            string text = $"Text{random.Next(1, 100)}";
            lines.Add($"{number}. {text}");
        }

        string inputPath = await TestHelpers.CreateTestFileAsync(lines);
        string outputPath = Path.GetTempFileName();
        string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        try
        {
            var request = new SortRequest
            {
                InputFilePath = inputPath,
                OutputFilePath = outputPath,
                TempDirectory = tempDir,
                MaxRamMb = 100,
                ChunkSizeMb = 1, // Base chunk size
                MaxDegreeOfParallelism = Environment.ProcessorCount,
                FileChunkTemplate = "chunk_{0:0000}.tmp",
                BufferSizeBytes = 4 * 1024 * 1024,
                DeleteTempFiles = false, // Keep temp files to verify
                MaxOpenFiles = 500,
                AdaptiveChunkSize = true,
                MinChunkSizeMb = 64,
                MaxChunkSizeMb = 512
            };

            await _sorter.SortAsync(request);

            var records = await TestHelpers.ReadRecordsFromFileAsync(outputPath);
            Assert.True(TestHelpers.IsSorted(records));
            Assert.Equal(50000, records.Count);

            // Verify chunks were created
            var chunkFiles = Directory.GetFiles(tempDir, "chunk_*.tmp");
            Assert.NotEmpty(chunkFiles);
        }
        finally
        {
            Cleanup(inputPath, outputPath, tempDir);
        }
    }

    [Fact]
    public async Task SortAsync_AdaptiveChunkSizeDisabled_UsesFixedSizing()
    {
        var lines = new List<string>();
        for (int i = 0; i < 10000; i++)
        {
            lines.Add($"{i % 100}. Test{i}");
        }

        string inputPath = await TestHelpers.CreateTestFileAsync(lines);
        string outputPath = Path.GetTempFileName();
        string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        try
        {
            var request = new SortRequest
            {
                InputFilePath = inputPath,
                OutputFilePath = outputPath,
                TempDirectory = tempDir,
                MaxRamMb = 100,
                ChunkSizeMb = 1,
                MaxDegreeOfParallelism = Environment.ProcessorCount,
                FileChunkTemplate = "chunk_{0:0000}.tmp",
                BufferSizeBytes = 4 * 1024 * 1024,
                DeleteTempFiles = false,
                MaxOpenFiles = 500,
                AdaptiveChunkSize = false,
                MinChunkSizeMb = 64,
                MaxChunkSizeMb = 512
            };

            await _sorter.SortAsync(request);

            var records = await TestHelpers.ReadRecordsFromFileAsync(outputPath);
            Assert.True(TestHelpers.IsSorted(records));
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

