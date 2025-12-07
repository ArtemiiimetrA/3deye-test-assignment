# FileSort Benchmarks

This project contains performance benchmarks for the FileSort solution using [BenchmarkDotNet](https://benchmarkdotnet.org/).

## Overview

The benchmarks are designed to measure and analyze the performance of critical operations in the FileSort solution, including:

- **Record Parsing** - Performance of parsing individual lines and large batches
- **Chunk Sorting** - In-memory sorting performance for different chunk sizes
- **File Sorting** - End-to-end sorting performance for various file sizes
- **Merge Operations** - K-way merge performance (single-pass and multi-pass)
- **Parallelism** - Impact of different parallelism configurations

## Prerequisites

- .NET 9.0 SDK
- BenchmarkDotNet (automatically restored via NuGet)

## Running Benchmarks

### Run All Benchmarks

```bash
cd FileSort.Benchmarks
dotnet run -c Release
```

**Note:** Always run benchmarks in Release configuration for accurate results. Debug builds will produce misleading performance data.

### Run Specific Benchmark

```bash
dotnet run -c Release -- --filter "*RecordParserBenchmarks*"
dotnet run -c Release -- --filter "*ChunkSortingBenchmarks*"
dotnet run -c Release -- --filter "*FileSortingBenchmarks*"
dotnet run -c Release -- --filter "*MergeOperationBenchmarks*"
dotnet run -c Release -- --filter "*ParallelismBenchmarks*"
```

**Note:** Use wildcards (`*`) around the class name for reliable filtering. You can also use the full namespace path:
```bash
dotnet run -c Release -- --filter "FileSort.Benchmarks.ParallelismBenchmarks"
```

### Quick Test Run (Dry Job)

For quick validation without full benchmarking:

```bash
dotnet run -c Release -- --job Dry
```

### Filter by Method Name

```bash
dotnet run -c Release -- --filter "*ParseLargeBatch*"
dotnet run -c Release -- --filter "*SortWithChunkParallelism*"
dotnet run -c Release -- --filter "*SortWithMergeParallelism*"
dotnet run -c Release -- --filter "*SortWithBothParallelism*"
```

### List Available Benchmarks

To see all available benchmarks and their exact names:

```bash
dotnet run -c Release -- --list flat
```

**Note:** Benchmarks with async `GlobalSetup` may not appear in the list but will still execute when filtered or run directly.

## Benchmark Descriptions

### RecordParserBenchmarks

Tests the performance of parsing lines in the format `<Number>. <Text>`.

**Benchmarks:**
- `ParseLine` - Parses individual lines with various formats
- `ParseMultipleLines` - Parses a small batch of test lines
- `ParseLargeBatch` - Parses 100,000 lines (realistic scenario)
- `ParseBatchOfSize` - Parses configurable batch sizes (10k, 100k)

**Use Case:** Identifies parsing bottlenecks when processing large files.

### ChunkSortingBenchmarks

Tests in-memory sorting performance for chunks of records.

**Benchmarks:**
- `SortChunk` - Sorts chunks of different sizes (1k, 10k, 100k records)
- `SortChunkWithDuplicates` - Sorts chunks with varying duplicate ratios

**Use Case:** Measures sorting performance during the chunk creation phase.

### FileSortingBenchmarks

Tests end-to-end file sorting performance for various file sizes.

**Benchmarks:**
- `SortFile` - Sorts files of different sizes (10MB, 100MB, 1GB)

**Use Case:** Provides overall performance metrics for the complete sorting operation.

**Note:** Test files are pre-generated in `GlobalSetup` to avoid including file generation time in benchmark results.

### MergeOperationBenchmarks

Tests k-way merge performance for different merge strategies.

**Benchmarks:**
- `SinglePassMerge` - Single-pass merge for small file counts (2, 5, 10 files)
- `MultiPassMerge` - Multi-pass merge for large file counts (50, 100, 500 files)
- `MultiPassMergeWithParallelism` - Multi-pass merge with different parallelism levels

**Use Case:** Analyzes merge strategy performance and identifies optimal merge configurations.

### ParallelismBenchmarks

Tests the impact of different parallelism configurations on sorting performance.

**Benchmarks:**
- `SortWithChunkParallelism` - Tests different `MaxDegreeOfParallelism` values (1, 2, 4, 8)
- `SortWithMergeParallelism` - Tests different `MaxMergeParallelism` values (1, 2, 4, 8)
- `SortWithBothParallelism` - Tests combined parallelism configurations

**Use Case:** Determines optimal parallelism settings for different hardware configurations.

## Benchmark Results

Results are automatically generated in the `BenchmarkDotNet.Artifacts` folder:

- **HTML Report** - `BenchmarkDotNet.Artifacts/results/*-report.html` (open in browser)
- **CSV Report** - `BenchmarkDotNet.Artifacts/results/*-report.csv` (for analysis)
- **Markdown Report** - `BenchmarkDotNet.Artifacts/results/*-report-github.md` (for documentation)

## Interpreting Results

### Key Metrics

- **Mean** - Average execution time
- **Error** - Standard error of the mean
- **StdDev** - Standard deviation
- **Gen 0/1/2** - Garbage collection counts
- **Allocated** - Memory allocated during execution

### Performance Analysis

1. **Compare Mean values** - Lower is better for execution time
2. **Check Allocated memory** - Lower is better for memory efficiency
3. **Monitor GC counts** - Fewer collections indicate better memory management
4. **Compare StdDev** - Lower variance indicates more consistent performance

## Best Practices

1. **Run in Release mode** - Always use `-c Release` for accurate results
2. **Warm up** - First run may be slower due to JIT compilation
3. **Multiple runs** - BenchmarkDotNet runs multiple iterations automatically
4. **Stable environment** - Close unnecessary applications for consistent results
5. **Compare relatively** - Focus on relative improvements rather than absolute numbers

## Troubleshooting

### Out of Memory Errors

If benchmarks fail with out-of-memory errors:
- Reduce file sizes in `FileSortingBenchmarks`
- Reduce record counts in `ChunkSortingBenchmarks`
- Close other applications to free memory

### Long Execution Times

Large file benchmarks (1GB+) can take significant time:
- Use `--filter` to run only specific benchmarks
- Start with smaller file sizes
- Use `--job Dry` for quick validation

### File Not Found Errors

If test files are missing:
- Ensure `GlobalSetup` methods complete successfully
- Check temp directory permissions: `%TEMP%\FileSortBenchmarks`
- Re-run benchmarks to regenerate test files

### Filter Not Working

If filters don't seem to work:
- Ensure `Program.cs` uses `BenchmarkSwitcher` (not `BenchmarkRunner` directly)
- Use wildcards: `--filter "*ClassName*"` instead of `--filter "ClassName"`
- Use full namespace: `--filter "FileSort.Benchmarks.ClassName"`
- Check exact names with: `dotnet run -c Release -- --list flat`

## Configuration

### Program.cs

The `Program.cs` uses `BenchmarkSwitcher` to handle command-line arguments, which enables:
- Filtering benchmarks with `--filter`
- Listing benchmarks with `--list`
- All other BenchmarkDotNet command-line options

### Benchmark Classes

Benchmark configuration can be modified in the benchmark classes:

- `[SimpleJob(RuntimeMoniker.Net90)]` - Runtime version
- `[MemoryDiagnoser]` - Enable memory allocation tracking
- `[Arguments(...)]` - Test different parameter values
- `[GlobalSetup]` - One-time setup before benchmarks run (can be async)

## Contributing

When adding new benchmarks:

1. Follow existing naming conventions
2. Use `[GlobalSetup]` for expensive initialization
3. Include multiple test scenarios with `[Arguments]`
4. Add `[MemoryDiagnoser]` for memory analysis
5. Document the benchmark purpose in code comments

## See Also

- [BenchmarkDotNet Documentation](https://benchmarkdotnet.org/articles/overview.html)
- [FileSort Main README](../README.md)

