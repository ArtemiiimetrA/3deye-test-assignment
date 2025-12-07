using FileSort.Core.Interfaces;
using FileSort.Core.Requests;
using FileSort.Sorter;
using Xunit;

namespace FileSort.Sorter.Tests;

public class ExternalFileSorterErrorHandlingTests
{
    private readonly IExternalSorter _sorter = new ExternalFileSorter();

    private static SortRequest CreateBaseRequest(string inputPath, string outputPath, string tempDir) => new SortRequest
    {
        InputFilePath = inputPath,
        OutputFilePath = outputPath,
        TempDirectory = tempDir,
        MaxRamMb = 100,
        ChunkSizeMb = 10,
        MaxDegreeOfParallelism = 4,
        FileChunkTemplate = "chunk_{0}.tmp",
        BufferSizeBytes = 4096,
        DeleteTempFiles = true,
        MaxOpenFiles = 500,
        MaxMergeParallelism = 2,
        AdaptiveChunkSize = false,
        MinChunkSizeMb = 64,
        MaxChunkSizeMb = 512
    };

    [Fact]
    public async Task SortAsync_NonExistentInputFile_ThrowsFileNotFoundException()
    {
        string nonExistentPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".txt");
        string outputPath = Path.GetTempFileName();
        string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        try
        {
            var request = CreateBaseRequest(nonExistentPath, outputPath, tempDir);

            await Assert.ThrowsAsync<FileNotFoundException>(
                () => _sorter.SortAsync(request));
        }
        finally
        {
            if (File.Exists(outputPath))
                File.Delete(outputPath);
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public async Task SortAsync_InvalidInputPath_ThrowsArgumentException()
    {
        string outputPath = Path.GetTempFileName();
        string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        try
        {
            var request = CreateBaseRequest("", outputPath, tempDir); // Invalid input path

            await Assert.ThrowsAsync<ArgumentException>(
                () => _sorter.SortAsync(request));
        }
        finally
        {
            if (File.Exists(outputPath))
                File.Delete(outputPath);
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public async Task SortAsync_FileWithInvalidLines_HandlesGracefully()
    {
        string inputPath = await TestHelpers.CreateTestFileAsync(new[]
        {
            "1. Apple",
            "Invalid line",
            "2. Banana"
        });
        string outputPath = Path.GetTempFileName();
        string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        try
        {
            var request = CreateBaseRequest(inputPath, outputPath, tempDir) with
            {
                ChunkSizeMb = 1,
                MaxDegreeOfParallelism = Environment.ProcessorCount,
                FileChunkTemplate = "chunk_{0:0000}.tmp",
                BufferSizeBytes = 4 * 1024 * 1024
            };

            // Should complete successfully, skipping invalid lines
            await _sorter.SortAsync(request);

            Assert.True(File.Exists(outputPath));
            var records = await TestHelpers.ReadRecordsFromFileAsync(outputPath);
            Assert.True(TestHelpers.IsSorted(records));
            Assert.Equal(2, records.Count); // Only valid records
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
            var request = CreateBaseRequest(inputPath, outputPath, tempDir) with
            {
                ChunkSizeMb = 1,
                MaxDegreeOfParallelism = Environment.ProcessorCount,
                FileChunkTemplate = "chunk_{0:0000}.tmp",
                BufferSizeBytes = 4 * 1024 * 1024
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
    public async Task SortAsync_AlreadySortedFile_ProducesSameOrder()
    {
        var sortedLines = new[]
        {
            "1. Apple",
            "2. Apple",
            "1. Banana",
            "2. Banana"
        };

        string inputPath = await TestHelpers.CreateTestFileAsync(sortedLines);
        string outputPath = Path.GetTempFileName();
        string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        try
        {
            var request = CreateBaseRequest(inputPath, outputPath, tempDir) with
            {
                ChunkSizeMb = 1,
                MaxDegreeOfParallelism = Environment.ProcessorCount,
                FileChunkTemplate = "chunk_{0:0000}.tmp",
                BufferSizeBytes = 4 * 1024 * 1024
            };

            await _sorter.SortAsync(request);

            var records = await TestHelpers.ReadRecordsFromFileAsync(outputPath);
            Assert.True(TestHelpers.IsSorted(records));
            Assert.Equal(4, records.Count);
        }
        finally
        {
            Cleanup(inputPath, outputPath, tempDir);
        }
    }

    [Fact]
    public async Task SortAsync_ReverseSortedFile_SortsCorrectly()
    {
        var reverseSortedLines = new[]
        {
            "2. Banana",
            "1. Banana",
            "2. Apple",
            "1. Apple"
        };

        string inputPath = await TestHelpers.CreateTestFileAsync(reverseSortedLines);
        string outputPath = Path.GetTempFileName();
        string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        try
        {
            var request = CreateBaseRequest(inputPath, outputPath, tempDir) with
            {
                ChunkSizeMb = 1,
                MaxDegreeOfParallelism = Environment.ProcessorCount,
                FileChunkTemplate = "chunk_{0:0000}.tmp",
                BufferSizeBytes = 4 * 1024 * 1024
            };

            await _sorter.SortAsync(request);

            var records = await TestHelpers.ReadRecordsFromFileAsync(outputPath);
            Assert.True(TestHelpers.IsSorted(records));
            Assert.Equal(4, records.Count);
            Assert.Equal("Apple", records[0].Text);
            Assert.Equal("Apple", records[1].Text);
        }
        finally
        {
            Cleanup(inputPath, outputPath, tempDir);
        }
    }

    [Fact]
    public async Task SortAsync_AllIdenticalRecords_HandlesCorrectly()
    {
        var identicalLines = new[]
        {
            "1. Apple",
            "1. Apple",
            "1. Apple"
        };

        string inputPath = await TestHelpers.CreateTestFileAsync(identicalLines);
        string outputPath = Path.GetTempFileName();
        string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        try
        {
            var request = CreateBaseRequest(inputPath, outputPath, tempDir) with
            {
                ChunkSizeMb = 1,
                MaxDegreeOfParallelism = Environment.ProcessorCount,
                FileChunkTemplate = "chunk_{0:0000}.tmp",
                BufferSizeBytes = 4 * 1024 * 1024
            };

            await _sorter.SortAsync(request);

            var records = await TestHelpers.ReadRecordsFromFileAsync(outputPath);
            Assert.True(TestHelpers.IsSorted(records));
            Assert.Equal(3, records.Count);
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

