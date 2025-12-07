using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using FileSort.Core.Interfaces;
using FileSort.Core.Requests;
using FileSort.Generator;
using FileSort.Sorter;

namespace FileSort.Benchmarks;

[SimpleJob(RuntimeMoniker.Net90)]
[MemoryDiagnoser]
public class ParallelismBenchmarks
{
    private readonly IExternalSorter _sorter = new ExternalFileSorter();
    private readonly ITestFileGenerator _generator = new TestFileGenerator();
    private readonly Dictionary<long, string> _inputFiles = new();

    [GlobalSetup]
    public async Task Setup()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "FileSortBenchmarks");
        if (!Directory.Exists(tempDir))
            Directory.CreateDirectory(tempDir);

        // Pre-generate test file for parallelism benchmarks
        var sizeBytes = 100L * 1024 * 1024; // 100 MB
        var inputPath = Path.Combine(tempDir, $"input_parallelism_{sizeBytes}.txt");

        if (!File.Exists(inputPath))
        {
            var genRequest = new GeneratorRequest
            {
                OutputFilePath = inputPath,
                TargetSizeBytes = sizeBytes,
                MinNumber = 1,
                MaxNumber = 1000000,
                DuplicateRatioPercent = 20,
                BufferSizeBytes = 4 * 1024 * 1024,
                MaxWordsPerString = 5,
                Seed = 42
            };

            await _generator.GenerateAsync(genRequest);
        }

        _inputFiles[sizeBytes] = inputPath;
    }

    [Benchmark]
    [Arguments(1)]
    [Arguments(2)]
    [Arguments(4)]
    [Arguments(8)]
    public async Task SortWithChunkParallelism(int maxDegreeOfParallelism)
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "FileSortBenchmarks");
        var sizeBytes = 100L * 1024 * 1024; // 100 MB
        var inputPath = _inputFiles[sizeBytes];
        var outputPath = Path.Combine(tempDir, $"output_parallelism_{maxDegreeOfParallelism}.txt");
        var chunkDir = Path.Combine(tempDir, $"chunks_parallelism_{maxDegreeOfParallelism}");

        var sortRequest = new SortRequest
        {
            InputFilePath = inputPath,
            OutputFilePath = outputPath,
            TempDirectory = chunkDir,
            MaxRamMb = 100,
            ChunkSizeMb = 20,
            MaxDegreeOfParallelism = maxDegreeOfParallelism,
            FileChunkTemplate = "chunk_{0:0000}.tmp",
            BufferSizeBytes = 4 * 1024 * 1024,
            DeleteTempFiles = true,
            MaxOpenFiles = 500,
            MaxMergeParallelism = 2,
            AdaptiveChunkSize = false,
            MinChunkSizeMb = 20,
            MaxChunkSizeMb = 20
        };

        await _sorter.SortAsync(sortRequest);
    }

    [Benchmark]
    [Arguments(1)]
    [Arguments(2)]
    [Arguments(4)]
    [Arguments(8)]
    public async Task SortWithMergeParallelism(int maxMergeParallelism)
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "FileSortBenchmarks");
        var sizeBytes = 100L * 1024 * 1024; // 100 MB
        var inputPath = _inputFiles[sizeBytes];
        var outputPath = Path.Combine(tempDir, $"output_merge_parallelism_{maxMergeParallelism}.txt");
        var chunkDir = Path.Combine(tempDir, $"chunks_merge_parallelism_{maxMergeParallelism}");

        var sortRequest = new SortRequest
        {
            InputFilePath = inputPath,
            OutputFilePath = outputPath,
            TempDirectory = chunkDir,
            MaxRamMb = 100,
            ChunkSizeMb = 20,
            MaxDegreeOfParallelism = Environment.ProcessorCount,
            FileChunkTemplate = "chunk_{0:0000}.tmp",
            BufferSizeBytes = 4 * 1024 * 1024,
            DeleteTempFiles = true,
            MaxOpenFiles = 10, // Force multi-pass merge
            MaxMergeParallelism = maxMergeParallelism,
            AdaptiveChunkSize = false,
            MinChunkSizeMb = 20,
            MaxChunkSizeMb = 20
        };

        await _sorter.SortAsync(sortRequest);
    }

    [Benchmark]
    [Arguments(1, 1)]
    [Arguments(2, 2)]
    [Arguments(4, 2)]
    [Arguments(8, 4)]
    public async Task SortWithBothParallelism(int maxDegreeOfParallelism, int maxMergeParallelism)
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "FileSortBenchmarks");
        var sizeBytes = 100L * 1024 * 1024; // 100 MB
        var inputPath = _inputFiles[sizeBytes];
        var outputPath = Path.Combine(tempDir, $"output_both_{maxDegreeOfParallelism}_{maxMergeParallelism}.txt");
        var chunkDir = Path.Combine(tempDir, $"chunks_both_{maxDegreeOfParallelism}_{maxMergeParallelism}");

        var sortRequest = new SortRequest
        {
            InputFilePath = inputPath,
            OutputFilePath = outputPath,
            TempDirectory = chunkDir,
            MaxRamMb = 100,
            ChunkSizeMb = 20,
            MaxDegreeOfParallelism = maxDegreeOfParallelism,
            FileChunkTemplate = "chunk_{0:0000}.tmp",
            BufferSizeBytes = 4 * 1024 * 1024,
            DeleteTempFiles = true,
            MaxOpenFiles = 10, // Force multi-pass merge
            MaxMergeParallelism = maxMergeParallelism,
            AdaptiveChunkSize = false,
            MinChunkSizeMb = 20,
            MaxChunkSizeMb = 20
        };

        await _sorter.SortAsync(sortRequest);
    }
}

