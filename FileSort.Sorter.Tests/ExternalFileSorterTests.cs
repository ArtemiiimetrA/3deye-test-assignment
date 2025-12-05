using FileSort.Core.Interfaces;
using FileSort.Core.Models;
using FileSort.Core.Requests;
using FileSort.Sorter;
using Xunit;

namespace FileSort.Sorter.Tests;

public class ExternalFileSorterTests
{
    private readonly IExternalSorter _sorter = new ExternalFileSorter();

    [Fact]
    public async Task SortAsync_SingleLine_SortsCorrectly()
    {
        string inputPath = await TestHelpers.CreateTestFileAsync(new[] { "415. Apple" });
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
                DeleteTempFiles = true,
                MaxOpenFiles = 500,
                AdaptiveChunkSize = true,
                MinChunkSizeMb = 64,
                MaxChunkSizeMb = 512
            };

            await _sorter.SortAsync(request);

            Assert.True(File.Exists(outputPath));
            string[] lines = await File.ReadAllLinesAsync(outputPath);
            Assert.Single(lines);
            Assert.Equal("415. Apple", lines[0]);
        }
        finally
        {
            Cleanup(inputPath, outputPath, tempDir);
        }
    }

    [Fact]
    public async Task SortAsync_TwoLinesReverseOrder_SortsCorrectly()
    {
        string inputPath = await TestHelpers.CreateTestFileAsync(new[]
        {
            "2. Banana",
            "1. Apple"
        });
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
                DeleteTempFiles = true,
                MaxOpenFiles = 500,
                AdaptiveChunkSize = true,
                MinChunkSizeMb = 64,
                MaxChunkSizeMb = 512
            };

            await _sorter.SortAsync(request);

            string[] lines = await File.ReadAllLinesAsync(outputPath);
            Assert.Equal(2, lines.Length);
            Assert.Equal("1. Apple", lines[0]);
            Assert.Equal("2. Banana", lines[1]);
        }
        finally
        {
            Cleanup(inputPath, outputPath, tempDir);
        }
    }

    [Fact]
    public async Task SortAsync_MultipleLinesWithDuplicates_SortsCorrectly()
    {
        string inputPath = await TestHelpers.CreateTestFileAsync(new[]
        {
            "3. Apple",
            "1. Apple",
            "2. Banana",
            "5. Apple",
            "4. Banana"
        });
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
                DeleteTempFiles = true,
                MaxOpenFiles = 500,
                AdaptiveChunkSize = true,
                MinChunkSizeMb = 64,
                MaxChunkSizeMb = 512
            };

            await _sorter.SortAsync(request);

            var records = await TestHelpers.ReadRecordsFromFileAsync(outputPath);
            Assert.True(TestHelpers.IsSorted(records));
            Assert.Equal(5, records.Count);
        }
        finally
        {
            Cleanup(inputPath, outputPath, tempDir);
        }
    }

    [Fact]
    public async Task SortAsync_EmptyFile_CreatesEmptyOutput()
    {
        string inputPath = await TestHelpers.CreateTestFileAsync(Array.Empty<string>());
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
                DeleteTempFiles = true,
                MaxOpenFiles = 500,
                AdaptiveChunkSize = true,
                MinChunkSizeMb = 64,
                MaxChunkSizeMb = 512
            };

            await _sorter.SortAsync(request);

            Assert.True(File.Exists(outputPath));
            string[] lines = await File.ReadAllLinesAsync(outputPath);
            Assert.Empty(lines);
        }
        finally
        {
            Cleanup(inputPath, outputPath, tempDir);
        }
    }

    [Fact]
    public async Task SortAsync_LargeFile_CreatesMultipleChunks()
    {
        // Create a file that will require multiple chunks
        var lines = new List<string>();
        var random = new Random(42);
        for (int i = 0; i < 10000; i++)
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
                ChunkSizeMb = 1, // Small chunk size to force multiple chunks
                MaxDegreeOfParallelism = Environment.ProcessorCount,
                FileChunkTemplate = "chunk_{0:0000}.tmp",
                BufferSizeBytes = 4 * 1024 * 1024,
                DeleteTempFiles = true,
                MaxOpenFiles = 10, // Small to test multi-pass merge
                AdaptiveChunkSize = true,
                MinChunkSizeMb = 64,
                MaxChunkSizeMb = 512
            };

            await _sorter.SortAsync(request);

            var records = await TestHelpers.ReadRecordsFromFileAsync(outputPath);
            Assert.True(TestHelpers.IsSorted(records));
            Assert.Equal(10000, records.Count);
        }
        finally
        {
            Cleanup(inputPath, outputPath, tempDir);
        }
    }

    [Fact]
    public async Task SortAsync_PreservesAllLines()
    {
        var inputLines = new[]
        {
            "3. Apple",
            "1. Apple",
            "2. Banana",
            "5. Apple",
            "4. Banana",
            "10. Cherry",
            "7. Date"
        };

        string inputPath = await TestHelpers.CreateTestFileAsync(inputLines);
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
                DeleteTempFiles = true,
                MaxOpenFiles = 500,
                AdaptiveChunkSize = true,
                MinChunkSizeMb = 64,
                MaxChunkSizeMb = 512
            };

            await _sorter.SortAsync(request);

            var inputRecords = await TestHelpers.ReadRecordsFromFileAsync(inputPath);
            var outputRecords = await TestHelpers.ReadRecordsFromFileAsync(outputPath);

            Assert.Equal(inputRecords.Count, outputRecords.Count);
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
