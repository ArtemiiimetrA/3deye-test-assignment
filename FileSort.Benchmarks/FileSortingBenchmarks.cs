using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using FileSort.Core.Interfaces;
using FileSort.Core.Requests;
using FileSort.Generator;
using FileSort.Sorter;

namespace FileSort.Benchmarks;

[SimpleJob(RuntimeMoniker.Net90)]
[MemoryDiagnoser]
public class FileSortingBenchmarks
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

        // Pre-generate test files for all benchmark sizes
        var sizes = new[] { 10L * 1024 * 1024, 100L * 1024 * 1024, 1024L * 1024 * 1024 };
        
        foreach (var sizeBytes in sizes)
        {
            var inputPath = Path.Combine(tempDir, $"input_{sizeBytes}.txt");
            
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
    }

    [Benchmark]
    [Arguments(10 * 1024 * 1024)] // 10 MB
    [Arguments(100 * 1024 * 1024)] // 100 MB
    [Arguments(1024 * 1024 * 1024)] // 1 GB
    public async Task SortFile(long fileSizeBytes)
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "FileSortBenchmarks");
        var inputPath = _inputFiles[fileSizeBytes];
        var outputPath = Path.Combine(tempDir, $"output_{fileSizeBytes}.txt");
        var chunkDir = Path.Combine(tempDir, $"chunks_{fileSizeBytes}");

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
            MaxOpenFiles = 500,
            MaxMergeParallelism = 2,
            AdaptiveChunkSize = true,
            MinChunkSizeMb = 1,
            MaxChunkSizeMb = 30
        };

        await _sorter.SortAsync(sortRequest);
    }
}

