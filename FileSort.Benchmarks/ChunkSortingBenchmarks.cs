using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using FileSort.Core.Comparison;
using FileSort.Core.Models;

namespace FileSort.Benchmarks;

[SimpleJob(RuntimeMoniker.Net90)]
[MemoryDiagnoser]
public class ChunkSortingBenchmarks
{
    private readonly RecordComparer _comparer = RecordComparer.Instance;

    [Benchmark]
    [Arguments(1000)]
    [Arguments(10000)]
    [Arguments(100000)]
    public void SortChunk(int recordCount)
    {
        var random = new Random(42);
        var records = new List<Record>(recordCount);

        for (var i = 0; i < recordCount; i++)
        {
            var number = random.Next(1, 1000000);
            var text = $"Text{random.Next(1, 100)}";
            records.Add(new Record(number, text));
        }

        records.Sort(_comparer);
    }

    [Benchmark]
    [Arguments(1000, 10)]
    [Arguments(10000, 50)]
    [Arguments(100000, 100)]
    public void SortChunkWithDuplicates(int recordCount, int duplicateRatio)
    {
        var random = new Random(42);
        var records = new List<Record>(recordCount);
        var textPool = new List<string>();

        // Create text pool
        for (var i = 0; i < 100; i++)
            textPool.Add($"Text{i}");

        for (var i = 0; i < recordCount; i++)
        {
            var number = random.Next(1, 1000000);
            string text;
            
            if (random.Next(100) < duplicateRatio && textPool.Count > 0)
                text = textPool[random.Next(textPool.Count)];
            else
                text = $"Text{random.Next(1, 1000)}";

            records.Add(new Record(number, text));
        }

        records.Sort(_comparer);
    }
}

