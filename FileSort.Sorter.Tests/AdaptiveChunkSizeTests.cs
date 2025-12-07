using FileSort.Core.Interfaces;
using FileSort.Core.Requests;
using Xunit;

namespace FileSort.Sorter.Tests;

public class AdaptiveChunkSizeTests
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
            MaxDegreeOfParallelism = Environment.ProcessorCount,
            FileChunkTemplate = "chunk_{0:0000}.tmp",
            BufferSizeBytes = 4 * 1024 * 1024,
            DeleteTempFiles = false,
            MaxOpenFiles = 500,
            MaxMergeParallelism = 2,
            AdaptiveChunkSize = true,
            MinChunkSizeMb = 64,
            MaxChunkSizeMb = 512
        };
    }

    [Fact]
    public async Task SortAsync_AdaptiveChunkSizeEnabled_UsesAdaptiveSizing()
    {
        // Create a file that will require multiple chunks
        var lines = new List<string>();
        var random = new Random(42);
        for (var i = 0; i < 50000; i++)
        {
            var number = random.Next(1, 1000);
            var text = $"Text{random.Next(1, 100)}";
            lines.Add($"{number}. {text}");
        }

        var inputPath = await TestHelpers.CreateTestFileAsync(lines);
        var outputPath = Path.GetTempFileName();
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        try
        {
            var request = CreateBaseRequest(inputPath, outputPath, tempDir);

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
        for (var i = 0; i < 10000; i++) lines.Add($"{i % 100}. Test{i}");

        var inputPath = await TestHelpers.CreateTestFileAsync(lines);
        var outputPath = Path.GetTempFileName();
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        try
        {
            var request = CreateBaseRequest(inputPath, outputPath, tempDir) with
            {
                AdaptiveChunkSize = false
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
                Directory.Delete(tempDir, true);
        }
        catch
        {
            // Ignore cleanup errors
        }
    }
}