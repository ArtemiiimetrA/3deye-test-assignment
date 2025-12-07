using FileSort.Core.Requests;
using FileSort.Generator;
using Xunit;

namespace FileSort.Sorter.Tests;

public class IntegrationTests
{
    private static SortRequest CreateBaseSortRequest(string inputPath, string outputPath, string tempDir)
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
            DeleteTempFiles = true,
            MaxOpenFiles = 500,
            MaxMergeParallelism = 2,
            AdaptiveChunkSize = true,
            MinChunkSizeMb = 64,
            MaxChunkSizeMb = 512
        };
    }

    [Fact]
    public async Task EndToEnd_GenerateAndSort_ProducesSortedOutput()
    {
        var inputPath = Path.GetTempFileName();
        var outputPath = Path.GetTempFileName();
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        try
        {
            // Step 1: Generate test file
            var generator = new TestFileGenerator();
            var genRequest = new GeneratorRequest
            {
                OutputFilePath = inputPath,
                TargetSizeBytes = 100 * 1024, // 100KB
                MinNumber = 1,
                MaxNumber = 1000,
                DuplicateRatioPercent = 30,
                BufferSizeBytes = 4 * 1024 * 1024,
                Seed = 42
            };

            await generator.GenerateAsync(genRequest);
            Assert.True(File.Exists(inputPath));

            // Step 2: Sort the file
            var sorter = new ExternalFileSorter();
            var sortRequest = CreateBaseSortRequest(inputPath, outputPath, tempDir);

            await sorter.SortAsync(sortRequest);
            Assert.True(File.Exists(outputPath));

            // Step 3: Verify output is sorted
            var records = await TestHelpers.ReadRecordsFromFileAsync(outputPath);
            Assert.True(TestHelpers.IsSorted(records));
            Assert.True(records.Count > 0);
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