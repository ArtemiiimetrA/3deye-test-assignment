using BenchmarkDotNet.Running;
using FileSort.Benchmarks;

namespace FileSort.Benchmarks;

public class Program
{
    public static void Main(string[] args)
    {
        // Run all benchmarks
        var summary = BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);

        // Or run specific benchmarks:
        // BenchmarkRunner.Run<RecordParserBenchmarks>();
        // BenchmarkRunner.Run<ChunkSortingBenchmarks>();
        // BenchmarkRunner.Run<FileSortingBenchmarks>();
        // BenchmarkRunner.Run<MergeOperationBenchmarks>();
        // BenchmarkRunner.Run<ParallelismBenchmarks>();
    }
}

