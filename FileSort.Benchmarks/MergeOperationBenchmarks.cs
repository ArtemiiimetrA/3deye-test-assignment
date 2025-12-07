using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using FileSort.Core.Comparison;
using FileSort.Core.Models;
using FileSort.Core.Parsing;
using FileSort.Sorter.Helpers;
using FileSort.Sorter.Strategies;

namespace FileSort.Benchmarks;

[SimpleJob(RuntimeMoniker.Net90)]
[MemoryDiagnoser]
public class MergeOperationBenchmarks
{
    private readonly string _tempDir;
    private readonly Dictionary<int, List<string>> _chunkFiles = new();

    public MergeOperationBenchmarks()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "FileSortBenchmarks", "Merge");
    }

    [GlobalSetup]
    public async Task Setup()
    {
        if (!Directory.Exists(_tempDir))
            Directory.CreateDirectory(_tempDir);

        // Create sorted chunk files for different merge scenarios
        var scenarios = new[]
        {
            (FileCount: 2, RecordsPerFile: 1000),
            (FileCount: 5, RecordsPerFile: 1000),
            (FileCount: 10, RecordsPerFile: 1000),
            (FileCount: 50, RecordsPerFile: 500),
            (FileCount: 100, RecordsPerFile: 500),
            (FileCount: 500, RecordsPerFile: 200)
        };

        foreach (var (fileCount, recordsPerFile) in scenarios)
        {
            var files = new List<string>();
            var random = new Random(42);

            for (var fileIndex = 0; fileIndex < fileCount; fileIndex++)
            {
                var filePath = Path.Combine(_tempDir, $"chunk_{fileCount}_{fileIndex}.txt");
                
                // Generate sorted records for this chunk
                var records = new List<Record>();
                for (var i = 0; i < recordsPerFile; i++)
                {
                    var number = random.Next(1, 1000000);
                    var text = $"Text{random.Next(1, 100)}";
                    records.Add(new Record(number, text));
                }

                records.Sort(RecordComparer.Instance);

                // Write to file
                await using var writer = FileIOHelpers.CreateFileWriter(filePath, 4 * 1024 * 1024);
                foreach (var record in records)
                    await writer.WriteLineAsync(record.ToLine());
                await writer.FlushAsync();

                files.Add(filePath);
            }

            _chunkFiles[fileCount] = files;
        }
    }

    [Benchmark]
    [Arguments(2)]
    [Arguments(5)]
    [Arguments(10)]
    public async Task SinglePassMerge(int fileCount)
    {
        var files = _chunkFiles[fileCount];
        var outputPath = Path.Combine(_tempDir, $"merge_output_{fileCount}.txt");
        var merger = new SinglePassMerger(4 * 1024 * 1024);

        await merger.MergeAsync(files, outputPath, null, CancellationToken.None);
    }

    [Benchmark]
    [Arguments(50)]
    [Arguments(100)]
    [Arguments(500)]
    public async Task MultiPassMerge(int fileCount)
    {
        var files = _chunkFiles[fileCount];
        var outputPath = Path.Combine(_tempDir, $"merge_output_{fileCount}.txt");
        using var merger = new MultiPassMerger(maxOpenFiles: 10, bufferSize: 4 * 1024 * 1024, maxMergeParallelism: 2);

        await merger.MergeAsync(files, outputPath, null, CancellationToken.None);
    }

    [Benchmark]
    [Arguments(50, 1)]
    [Arguments(50, 2)]
    [Arguments(50, 4)]
    [Arguments(100, 1)]
    [Arguments(100, 2)]
    [Arguments(100, 4)]
    public async Task MultiPassMergeWithParallelism(int fileCount, int maxMergeParallelism)
    {
        var files = _chunkFiles[fileCount];
        var outputPath = Path.Combine(_tempDir, $"merge_output_{fileCount}_p{maxMergeParallelism}.txt");
        using var merger = new MultiPassMerger(maxOpenFiles: 10, bufferSize: 4 * 1024 * 1024, maxMergeParallelism);

        await merger.MergeAsync(files, outputPath, null, CancellationToken.None);
    }
}

