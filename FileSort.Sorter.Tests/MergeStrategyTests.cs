using System.IO;
using FileSort.Sorter.Strategies;
using Xunit;

namespace FileSort.Sorter.Tests;

public class MergeStrategyTests
{
    [Fact]
    public async Task SinglePassMerger_MergeTwoFiles_MergesCorrectly()
    {
        var file1 = await CreateSortedFileAsync("file1.txt", new[]
        {
            "1. Apple",
            "3. Cherry"
        });
        var file2 = await CreateSortedFileAsync("file2.txt", new[]
        {
            "2. Banana",
            "4. Date"
        });
        var outputPath = Path.GetTempFileName();

        try
        {
            var merger = new SinglePassMerger(4096);
            await merger.MergeAsync(new[] { file1, file2 }, outputPath, null, CancellationToken.None);

            var records = await TestHelpers.ReadRecordsFromFileAsync(outputPath);
            Assert.True(TestHelpers.IsSorted(records));
            Assert.Equal(4, records.Count);
        }
        finally
        {
            Cleanup(file1, file2, outputPath);
        }
    }

    [Fact]
    public async Task SinglePassMerger_MergeMultipleFiles_MergesCorrectly()
    {
        var file1 = await CreateSortedFileAsync("file1.txt", new[] { "1. Apple" });
        var file2 = await CreateSortedFileAsync("file2.txt", new[] { "2. Banana" });
        var file3 = await CreateSortedFileAsync("file3.txt", new[] { "3. Cherry" });
        var outputPath = Path.GetTempFileName();

        try
        {
            var merger = new SinglePassMerger(4096);
            await merger.MergeAsync(new[] { file1, file2, file3 }, outputPath, null, CancellationToken.None);

            var records = await TestHelpers.ReadRecordsFromFileAsync(outputPath);
            Assert.True(TestHelpers.IsSorted(records));
            Assert.Equal(3, records.Count);
        }
        finally
        {
            Cleanup(file1, file2, file3, outputPath);
        }
    }

    [Fact]
    public async Task SinglePassMerger_MergeWithDuplicates_HandlesCorrectly()
    {
        var file1 = await CreateSortedFileAsync("file1.txt", new[]
        {
            "1. Apple",
            "3. Apple"
        });
        var file2 = await CreateSortedFileAsync("file2.txt", new[]
        {
            "2. Apple",
            "4. Banana"
        });
        var outputPath = Path.GetTempFileName();

        try
        {
            var merger = new SinglePassMerger(4096);
            await merger.MergeAsync(new[] { file1, file2 }, outputPath, null, CancellationToken.None);

            var records = await TestHelpers.ReadRecordsFromFileAsync(outputPath);
            Assert.True(TestHelpers.IsSorted(records));
            Assert.Equal(4, records.Count);
            Assert.Equal(1, records[0].Number);
            Assert.Equal(2, records[1].Number);
            Assert.Equal(3, records[2].Number);
        }
        finally
        {
            Cleanup(file1, file2, outputPath);
        }
    }

    [Fact]
    public async Task SinglePassMerger_EmptyFile_HandlesCorrectly()
    {
        var file1 = await CreateSortedFileAsync("file1.txt", new[] { "1. Apple" });
        var file2 = await CreateSortedFileAsync("file2.txt", Array.Empty<string>());
        var outputPath = Path.GetTempFileName();

        try
        {
            var merger = new SinglePassMerger(4096);
            await merger.MergeAsync(new[] { file1, file2 }, outputPath, null, CancellationToken.None);

            var records = await TestHelpers.ReadRecordsFromFileAsync(outputPath);
            Assert.Single(records);
        }
        finally
        {
            Cleanup(file1, file2, outputPath);
        }
    }

    [Fact]
    public async Task SinglePassMerger_Cancellation_ThrowsOperationCanceledException()
    {
        var file1 = await CreateSortedFileAsync("file1.txt", Enumerable.Range(1, 1000).Select(i => $"{i}. Test{i}"));
        var file2 = await CreateSortedFileAsync("file2.txt", Enumerable.Range(1001, 1000).Select(i => $"{i}. Test{i}"));
        var outputPath = Path.GetTempFileName();

        try
        {
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            var merger = new SinglePassMerger(4096);
            await Assert.ThrowsAsync<OperationCanceledException>(
                () => merger.MergeAsync(new[] { file1, file2 }, outputPath, null, cts.Token));
        }
        finally
        {
            Cleanup(file1, file2, outputPath);
        }
    }

    [Fact]
    public async Task SinglePassMerger_InvalidRecordFormat_ThrowsInvalidDataException()
    {
        var file1 = await CreateSortedFileAsync("file1.txt", new[] { "1. Apple" });
        var file2 = await CreateSortedFileAsync("file2.txt", new[] { "Invalid line" });
        var outputPath = Path.GetTempFileName();

        try
        {
            var merger = new SinglePassMerger(4096);
            var exception = await Assert.ThrowsAsync<InvalidDataException>(
                () => merger.MergeAsync(new[] { file1, file2 }, outputPath, null, CancellationToken.None));

            Assert.Contains("Invalid record format", exception.Message);
            Assert.Contains(file2, exception.Message);
        }
        finally
        {
            Cleanup(file1, file2, outputPath);
        }
    }

    [Fact]
    public async Task SinglePassMerger_InvalidRecordDuringMerge_ThrowsInvalidDataException()
    {
        var file1 = await CreateSortedFileAsync("file1.txt", new[]
        {
            "1. Apple",
            "3. Cherry"
        });
        var file2 = await CreateSortedFileAsync("file2.txt", new[]
        {
            "2. Banana",
            "Invalid line"
        });
        var outputPath = Path.GetTempFileName();

        try
        {
            var merger = new SinglePassMerger(4096);
            var exception = await Assert.ThrowsAsync<InvalidDataException>(
                () => merger.MergeAsync(new[] { file1, file2 }, outputPath, null, CancellationToken.None));

            Assert.Contains("Invalid record format", exception.Message);
            Assert.Contains(file2, exception.Message);
        }
        finally
        {
            Cleanup(file1, file2, outputPath);
        }
    }

    [Fact]
    public async Task MultiPassMerger_ManyFiles_MergesCorrectly()
    {
        var files = new List<string>();
        for (var i = 0; i < 20; i++) files.Add(await CreateSortedFileAsync($"file{i}.txt", new[] { $"{i}. Test{i}" }));
        var outputPath = Path.GetTempFileName();

        try
        {
            using var merger = new MultiPassMerger(5, 4096, 2);
            await merger.MergeAsync(files, outputPath, null, CancellationToken.None);

            var records = await TestHelpers.ReadRecordsFromFileAsync(outputPath);
            Assert.True(TestHelpers.IsSorted(records));
            Assert.Equal(20, records.Count);
        }
        finally
        {
            foreach (var file in files)
                if (File.Exists(file))
                    File.Delete(file);
            if (File.Exists(outputPath))
                File.Delete(outputPath);
        }
    }

    [Fact]
    public async Task MultiPassMerger_SingleFile_CopiesToOutput()
    {
        var file1 = await CreateSortedFileAsync("file1.txt", new[] { "1. Apple" });
        var outputPath = Path.GetTempFileName();

        try
        {
            using var merger = new MultiPassMerger(5, 4096, 2);
            await merger.MergeAsync(new[] { file1 }, outputPath, null, CancellationToken.None);

            var records = await TestHelpers.ReadRecordsFromFileAsync(outputPath);
            Assert.Single(records);
        }
        finally
        {
            Cleanup(file1, outputPath);
        }
    }

    [Fact]
    public async Task MultiPassMerger_Cancellation_ThrowsOperationCanceledException()
    {
        var files = new List<string>();
        for (var i = 0; i < 10; i++)
            files.Add(await CreateSortedFileAsync($"file{i}.txt",
                Enumerable.Range(i * 100, 100).Select(j => $"{j}. Test{j}")));
        var outputPath = Path.GetTempFileName();

        try
        {
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            using var merger = new MultiPassMerger(3, 4096, 2);
            await Assert.ThrowsAsync<OperationCanceledException>(
                () => merger.MergeAsync(files, outputPath, null, cts.Token));
        }
        finally
        {
            foreach (var file in files)
                if (File.Exists(file))
                    File.Delete(file);
            if (File.Exists(outputPath))
                File.Delete(outputPath);
        }
    }

    [Fact]
    public void MergeStrategyFactory_FileCountWithinLimit_ReturnsSinglePassMerger()
    {
        var strategy = MergeStrategyFactory.CreateStrategy(5, 10, 4096, 2);

        Assert.IsType<SinglePassMerger>(strategy);
    }

    [Fact]
    public void MergeStrategyFactory_FileCountExceedsLimit_ReturnsMultiPassMerger()
    {
        var strategy = MergeStrategyFactory.CreateStrategy(20, 10, 4096, 2);

        Assert.IsType<MultiPassMerger>(strategy);
        if (strategy is IDisposable disposable) disposable.Dispose();
    }

    [Fact]
    public void MergeStrategyFactory_FileCountEqualsLimit_ReturnsSinglePassMerger()
    {
        var strategy = MergeStrategyFactory.CreateStrategy(10, 10, 4096, 2);

        Assert.IsType<SinglePassMerger>(strategy);
    }

    private static async Task<string> CreateSortedFileAsync(string fileName, IEnumerable<string> lines)
    {
        var path = Path.Combine(Path.GetTempPath(), fileName);
        await File.WriteAllLinesAsync(path, lines);
        return path;
    }

    private static void Cleanup(params string[] paths)
    {
        foreach (var path in paths)
            try
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
            catch
            {
                // Ignore cleanup errors
            }
    }
}