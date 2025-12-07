using FileSort.Core.Models;
using FileSort.Sorter.Processors;
using Xunit;
using Record = FileSort.Core.Models.Record;

namespace FileSort.Sorter.Tests;

public class ChunkProcessorTests
{
    [Fact]
    public async Task ProcessChunkAsync_SortsAndWritesRecords()
    {
        var records = new List<Record>
        {
            new(3, "Apple"),
            new(1, "Banana"),
            new(2, "Cherry")
        };

        string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);
        string chunkTemplate = "chunk_{0}.tmp";

        try
        {
            var processor = new ChunkProcessor();
            string chunkPath = await processor.ProcessChunkAsync(
                records,
                tempDir,
                chunkTemplate,
                chunkIndex: 0,
                bufferSize: 4096,
                CancellationToken.None);

            Assert.True(File.Exists(chunkPath));
            var outputRecords = await TestHelpers.ReadRecordsFromFileAsync(chunkPath);
            Assert.True(TestHelpers.IsSorted(outputRecords));
            Assert.Equal(3, outputRecords.Count);
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public async Task ProcessChunkAsync_EmptyRecords_CreatesEmptyFile()
    {
        var records = new List<Record>();
        string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        try
        {
            var processor = new ChunkProcessor();
            string chunkPath = await processor.ProcessChunkAsync(
                records,
                tempDir,
                "chunk_{0}.tmp",
                chunkIndex: 0,
                bufferSize: 4096,
                CancellationToken.None);

            Assert.True(File.Exists(chunkPath));
            var outputRecords = await TestHelpers.ReadRecordsFromFileAsync(chunkPath);
            Assert.Empty(outputRecords);
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public async Task ProcessChunkAsync_Cancellation_ThrowsOperationCanceledException()
    {
        var records = Enumerable.Range(1, 10000)
            .Select(i => new Record(i, $"Test{i}"))
            .ToList();

        string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        try
        {
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            var processor = new ChunkProcessor();
            await Assert.ThrowsAsync<OperationCanceledException>(
                () => processor.ProcessChunkAsync(
                    records,
                    tempDir,
                    "chunk_{0}.tmp",
                    chunkIndex: 0,
                    bufferSize: 4096,
                    cts.Token));
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public async Task ProcessChunkAsync_LargeChunk_HandlesCorrectly()
    {
        var records = Enumerable.Range(1, 50000)
            .Select(i => new Record(i % 1000, $"Test{i}"))
            .ToList();

        string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        try
        {
            var processor = new ChunkProcessor();
            string chunkPath = await processor.ProcessChunkAsync(
                records,
                tempDir,
                "chunk_{0}.tmp",
                chunkIndex: 0,
                bufferSize: 4096,
                CancellationToken.None);

            var outputRecords = await TestHelpers.ReadRecordsFromFileAsync(chunkPath);
            Assert.True(TestHelpers.IsSorted(outputRecords));
            Assert.Equal(50000, outputRecords.Count);
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, recursive: true);
        }
    }
}

