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
            var genOptions = new GeneratorOptions(
                outputFilePath: inputPath,
                targetSizeBytes: 100 * 1024, // 100KB
                minNumber: 1,
                maxNumber: 1000,
                duplicateRatioPercent: 30,
                bufferSizeBytes: 4 * 1024 * 1024,
                seed: 42);

            await generator.GenerateAsync(genOptions);
            Assert.True(File.Exists(inputPath));

            // Step 2: Sort the file
            var sorter = new ExternalFileSorter();
            var sortOptions = new SortOptions(
                inputFilePath: inputPath,
                outputFilePath: outputPath,
                tempDirectory: tempDir,
                maxRamMb: 100,
                chunkSizeMb: 1,
                maxDegreeOfParallelism: Environment.ProcessorCount,
                fileChunkTemplate: "chunk_{0:0000}.tmp",
                bufferSizeBytes: 4 * 1024 * 1024,
                deleteTempFiles: true,
                maxOpenFiles: 500,
                adaptiveChunkSize: true,
                minChunkSizeMb: 64,
                maxChunkSizeMb: 512);

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
