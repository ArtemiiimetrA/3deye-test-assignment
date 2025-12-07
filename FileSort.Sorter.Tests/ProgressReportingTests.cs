using FileSort.Core.Interfaces;
using FileSort.Core.Models;
using FileSort.Core.Requests;
using FileSort.Sorter;
using Xunit;
using Record = Xunit.Record;

namespace FileSort.Sorter.Tests;

public class ProgressReportingTests
{
    private readonly IExternalSorter _sorter = new ExternalFileSorter();

    [Fact]
    public async Task SortAsync_ReportsProgressDuringChunking()
    {
        var lines = Enumerable.Range(1, 10000).Select(i => $"{i % 100}. Test{i}").ToList();
        string inputPath = await TestHelpers.CreateTestFileAsync(lines);
        string outputPath = Path.GetTempFileName();
        string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        try
        {
            var progressReports = new List<SortProgress>();
            var progress = new Progress<SortProgress>(p => progressReports.Add(p));

            var request = new SortRequest
            {
                InputFilePath = inputPath,
                OutputFilePath = outputPath,
                TempDirectory = tempDir,
                MaxRamMb = 100,
                ChunkSizeMb = 1,
                MaxDegreeOfParallelism = Environment.ProcessorCount,
                FileChunkTemplate = "chunk_{0:0000}.tmp",
                BufferSizeBytes = 4 * 1024 * 1024,
                DeleteTempFiles = true,
                MaxOpenFiles = 500,
                AdaptiveChunkSize = false,
                MinChunkSizeMb = 64,
                MaxChunkSizeMb = 512
            };

            await _sorter.SortAsync(request, progress);

            Assert.NotEmpty(progressReports);
            // Should have progress reports for chunking
            Assert.Contains(progressReports, p => p.ChunksCreated > 0);
        }
        finally
        {
            Cleanup(inputPath, outputPath, tempDir);
        }
    }

    [Fact]
    public async Task SortAsync_ReportsProgressDuringMerging()
    {
        var lines = Enumerable.Range(1, 10000).Select(i => $"{i % 100}. Test{i}").ToList();
        string inputPath = await TestHelpers.CreateTestFileAsync(lines);
        string outputPath = Path.GetTempFileName();
        string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        try
        {
            var progressReports = new List<SortProgress>();
            var progress = new Progress<SortProgress>(p => progressReports.Add(p));

            var request = new SortRequest
            {
                InputFilePath = inputPath,
                OutputFilePath = outputPath,
                TempDirectory = tempDir,
                MaxRamMb = 100,
                ChunkSizeMb = 1,
                MaxDegreeOfParallelism = Environment.ProcessorCount,
                FileChunkTemplate = "chunk_{0:0000}.tmp",
                BufferSizeBytes = 4 * 1024 * 1024,
                DeleteTempFiles = true,
                MaxOpenFiles = 500,
                AdaptiveChunkSize = false,
                MinChunkSizeMb = 64,
                MaxChunkSizeMb = 512
            };

            await _sorter.SortAsync(request, progress);

            // Should have progress reports
            Assert.NotEmpty(progressReports);
        }
        finally
        {
            Cleanup(inputPath, outputPath, tempDir);
        }
    }

    [Fact]
    public async Task SortAsync_NullProgress_DoesNotThrow()
    {
        var lines = Enumerable.Range(1, 1000).Select(i => $"{i % 100}. Test{i}").ToList();
        string inputPath = await TestHelpers.CreateTestFileAsync(lines);
        string outputPath = Path.GetTempFileName();
        string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        try
        {
            var request = new SortRequest
            {
                InputFilePath = inputPath,
                OutputFilePath = outputPath,
                TempDirectory = tempDir,
                MaxRamMb = 100,
                ChunkSizeMb = 1,
                MaxDegreeOfParallelism = Environment.ProcessorCount,
                FileChunkTemplate = "chunk_{0:0000}.tmp",
                BufferSizeBytes = 4 * 1024 * 1024,
                DeleteTempFiles = true,
                MaxOpenFiles = 500,
                AdaptiveChunkSize = false,
                MinChunkSizeMb = 64,
                MaxChunkSizeMb = 512
            };

            var exception = await Record.ExceptionAsync(() => _sorter.SortAsync(request, null));
            Assert.Null(exception);
        }
        finally
        {
            Cleanup(inputPath, outputPath, tempDir);
        }
    }

    private static void Cleanup(string inputPath, string outputPath, string tempDir)
    {
        try
        {
            if (File.Exists(inputPath))
                File.Delete(inputPath);
            if (File.Exists(outputPath))
                File.Delete(outputPath);
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, recursive: true);
        }
        catch
        {
            // Ignore cleanup errors
        }
    }
}

