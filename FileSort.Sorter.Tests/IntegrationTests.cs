using FileSort.Core.Interfaces;
using FileSort.Core.Options;
using FileSort.Generator;
using FileSort.Sorter;
using Xunit;

namespace FileSort.Sorter.Tests;

public class IntegrationTests
{
    [Fact]
    public async Task EndToEnd_GenerateAndSort_ProducesSortedOutput()
    {
        string inputPath = Path.GetTempFileName();
        string outputPath = Path.GetTempFileName();
        string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        try
        {
            // Step 1: Generate test file
            var generator = new TestFileGenerator();
            var genOptions = new GeneratorOptions
            {
                OutputFilePath = inputPath,
                TargetSizeBytes = 100 * 1024, // 100KB
                MinNumber = 1,
                MaxNumber = 1000,
                DuplicateRatioPercent = 30,
                Seed = 42
            };

            await generator.GenerateAsync(genOptions);
            Assert.True(File.Exists(inputPath));

            // Step 2: Sort the file
            var sorter = new ExternalFileSorter();
            var sortOptions = new SortOptions
            {
                InputFilePath = inputPath,
                OutputFilePath = outputPath,
                TempDirectory = tempDir,
                ChunkSizeMb = 1,
                MaxRamMb = 100,
                DeleteTempFiles = true
            };

            await sorter.SortAsync(sortOptions);
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
                Directory.Delete(tempDir, recursive: true);
        }
        catch
        {
            // Ignore cleanup errors
        }
    }
}
