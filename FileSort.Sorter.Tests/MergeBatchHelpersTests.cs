using FileSort.Sorter.Helpers;
using Xunit;

namespace FileSort.Sorter.Tests;

public class MergeBatchHelpersTests
{
    [Theory]
    [InlineData(10, 9)]
    [InlineData(100, 99)]
    [InlineData(500, 499)]
    public void CalculateBatchSize_ReservesOneForOutput(int maxOpenFiles, int expectedBatchSize)
    {
        var result = MergeBatchHelpers.CalculateBatchSize(maxOpenFiles);
        Assert.Equal(expectedBatchSize, result);
    }

    [Theory]
    [InlineData(10, 5, 2)]
    [InlineData(20, 5, 4)]
    [InlineData(25, 5, 5)]
    [InlineData(24, 5, 5)]
    public void CalculateTotalBatches_ReturnsCorrectCount(int fileCount, int batchSize, int expectedBatches)
    {
        var result = MergeBatchHelpers.CalculateTotalBatches(fileCount, batchSize);
        Assert.Equal(expectedBatches, result);
    }

    [Fact]
    public void GetBatch_ReturnsCorrectBatch()
    {
        var files = new List<string> { "file1", "file2", "file3", "file4", "file5" };
        var startIndex = 1;
        var batchSize = 2;

        var batch = MergeBatchHelpers.GetBatch(files, startIndex, batchSize);

        Assert.Equal(2, batch.Count);
        Assert.Equal("file2", batch[0]);
        Assert.Equal("file3", batch[1]);
    }

    [Fact]
    public void GetBatch_LastBatch_ReturnsRemainingFiles()
    {
        var files = new List<string> { "file1", "file2", "file3" };
        var startIndex = 2;
        var batchSize = 5; // Larger than remaining

        var batch = MergeBatchHelpers.GetBatch(files, startIndex, batchSize);

        Assert.Single(batch);
        Assert.Equal("file3", batch[0]);
    }

    [Theory]
    [InlineData(10, 10, 1)] // File count equals max, single pass
    [InlineData(5, 10, 1)] // File count less than max, single pass
    [InlineData(20, 10, 2)] // Requires 2 passes
    [InlineData(100, 10, 3)] // Requires 3 passes
    [InlineData(1000, 10, 4)] // Requires 4 passes
    public void CalculateTotalPasses_ReturnsCorrectPassCount(int fileCount, int maxOpenFiles, int expectedPasses)
    {
        var result = MergeBatchHelpers.CalculateTotalPasses(fileCount, maxOpenFiles);
        Assert.Equal(expectedPasses, result);
    }
}