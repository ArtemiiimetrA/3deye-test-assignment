using FileSort.Core.Models;
using FileSort.Sorter.Strategies;
using Xunit;

namespace FileSort.Sorter.Tests;

public class MergeStrategyTests
{
    [Fact]
    public async Task SinglePassMerger_MergeTwoFiles_MergesCorrectly()
    {
        string file1 = await CreateSortedFileAsync("file1.txt", new[]
        {
            "1. Apple",
            "3. Cherry"
        });
        string file2 = await CreateSortedFileAsync("file2.txt", new[]
        {
            "2. Banana",
            "4. Date"
        });
        string outputPath = Path.GetTempFileName();

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
        string file1 = await CreateSortedFileAsync("file1.txt", new[] { "1. Apple" });
        string file2 = await CreateSortedFileAsync("file2.txt", new[] { "2. Banana" });
        string file3 = await CreateSortedFileAsync("file3.txt", new[] { "3. Cherry" });
        string outputPath = Path.GetTempFileName();

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
        string file1 = await CreateSortedFileAsync("file1.txt", new[]
        {
            "1. Apple",
            "3. Apple"
        });
        string file2 = await CreateSortedFileAsync("file2.txt", new[]
        {
            "2. Apple",
            "4. Banana"
        });
        string outputPath = Path.GetTempFileName();

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
        string file1 = await CreateSortedFileAsync("file1.txt", new[] { "1. Apple" });
        string file2 = await CreateSortedFileAsync("file2.txt", Array.Empty<string>());
        string outputPath = Path.GetTempFileName();

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
        string file1 = await CreateSortedFileAsync("file1.txt", Enumerable.Range(1, 1000).Select(i => $"{i}. Test{i}"));
        string file2 = await CreateSortedFileAsync("file2.txt", Enumerable.Range(1001, 1000).Select(i => $"{i}. Test{i}"));
        string outputPath = Path.GetTempFileName();

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
    public async Task MultiPassMerger_ManyFiles_MergesCorrectly()
    {
        var files = new List<string>();
        for (int i = 0; i < 20; i++)
        {
            files.Add(await CreateSortedFileAsync($"file{i}.txt", new[] { $"{i}. Test{i}" }));
        }
        string outputPath = Path.GetTempFileName();

        try
        {
            using var merger = new MultiPassMerger(maxOpenFiles: 5, bufferSize: 4096, maxMergeParallelism: 2);
            await merger.MergeAsync(files, outputPath, null, CancellationToken.None);

            var records = await TestHelpers.ReadRecordsFromFileAsync(outputPath);
            Assert.True(TestHelpers.IsSorted(records));
            Assert.Equal(20, records.Count);
        }
        finally
        {
            foreach (var file in files)
            {
                if (File.Exists(file))
                    File.Delete(file);
            }
            if (File.Exists(outputPath))
                File.Delete(outputPath);
        }
    }

    [Fact]
    public async Task MultiPassMerger_SingleFile_CopiesToOutput()
    {
        string file1 = await CreateSortedFileAsync("file1.txt", new[] { "1. Apple" });
        string outputPath = Path.GetTempFileName();

        try
        {
            using var merger = new MultiPassMerger(maxOpenFiles: 5, bufferSize: 4096, maxMergeParallelism: 2);
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
        for (int i = 0; i < 10; i++)
        {
            files.Add(await CreateSortedFileAsync($"file{i}.txt", Enumerable.Range(i * 100, 100).Select(j => $"{j}. Test{j}")));
        }
        string outputPath = Path.GetTempFileName();

        try
        {
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            using var merger = new MultiPassMerger(maxOpenFiles: 3, bufferSize: 4096, maxMergeParallelism: 2);
            await Assert.ThrowsAsync<OperationCanceledException>(
                () => merger.MergeAsync(files, outputPath, null, cts.Token));
        }
        finally
        {
            foreach (var file in files)
            {
                if (File.Exists(file))
                    File.Delete(file);
            }
            if (File.Exists(outputPath))
                File.Delete(outputPath);
        }
    }

    [Fact]
    public void MergeStrategyFactory_FileCountWithinLimit_ReturnsSinglePassMerger()
    {
        var strategy = MergeStrategyFactory.CreateStrategy(fileCount: 5, maxOpenFiles: 10, bufferSize: 4096, maxMergeParallelism: 2);

        Assert.IsType<SinglePassMerger>(strategy);
    }

    [Fact]
    public void MergeStrategyFactory_FileCountExceedsLimit_ReturnsMultiPassMerger()
    {
        var strategy = MergeStrategyFactory.CreateStrategy(fileCount: 20, maxOpenFiles: 10, bufferSize: 4096, maxMergeParallelism: 2);

        Assert.IsType<MultiPassMerger>(strategy);
        if (strategy is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }

    [Fact]
    public void MergeStrategyFactory_FileCountEqualsLimit_ReturnsSinglePassMerger()
    {
        var strategy = MergeStrategyFactory.CreateStrategy(fileCount: 10, maxOpenFiles: 10, bufferSize: 4096, maxMergeParallelism: 2);

        Assert.IsType<SinglePassMerger>(strategy);
    }

    private static async Task<string> CreateSortedFileAsync(string fileName, IEnumerable<string> lines)
    {
        string path = Path.Combine(Path.GetTempPath(), fileName);
        await File.WriteAllLinesAsync(path, lines);
        return path;
    }

    private static void Cleanup(params string[] paths)
    {
        foreach (var path in paths)
        {
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
}

