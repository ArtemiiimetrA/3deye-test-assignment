using FileSort.Core.Interfaces;
using FileSort.Core.Models.Progress;
using FileSort.Core.Requests;
using Xunit;
using Record = Xunit.Record;

namespace FileSort.Generator.Tests;

public class TestFileGeneratorProgressTests
{
    private readonly ITestFileGenerator _generator = new TestFileGenerator();

    [Fact]
    public async Task GenerateAsync_ReportsProgress()
    {
        var outputPath = Path.GetTempFileName();
        try
        {
            var progressReports = new List<GeneratorProgress>();
            var progress = new Progress<GeneratorProgress>(p => progressReports.Add(p));

            var request = new GeneratorRequest
            {
                OutputFilePath = outputPath,
                TargetSizeBytes = 10240,
                MinNumber = 1,
                MaxNumber = 1000000,
                DuplicateRatioPercent = 20,
                BufferSizeBytes = 4 * 1024 * 1024,
                Seed = 0
            };

            await _generator.GenerateAsync(request, progress);

            Assert.NotEmpty(progressReports);
            Assert.True(progressReports.Last().BytesWritten >= request.TargetSizeBytes * 0.99);
        }
        finally
        {
            if (File.Exists(outputPath))
                File.Delete(outputPath);
        }
    }

    [Fact]
    public async Task GenerateAsync_NullProgress_DoesNotThrow()
    {
        var outputPath = Path.GetTempFileName();
        try
        {
            var request = new GeneratorRequest
            {
                OutputFilePath = outputPath,
                TargetSizeBytes = 1024,
                MinNumber = 1,
                MaxNumber = 100,
                DuplicateRatioPercent = 20,
                BufferSizeBytes = 4 * 1024 * 1024,
                Seed = 42
            };

            var exception = await Record.ExceptionAsync(() => _generator.GenerateAsync(request));
            Assert.Null(exception);
        }
        finally
        {
            if (File.Exists(outputPath))
                File.Delete(outputPath);
        }
    }

    [Fact]
    public async Task GenerateAsync_ProgressValuesAreAccurate()
    {
        var outputPath = Path.GetTempFileName();
        try
        {
            var progressReports = new List<GeneratorProgress>();
            var progress = new Progress<GeneratorProgress>(p => progressReports.Add(p));

            var request = new GeneratorRequest
            {
                OutputFilePath = outputPath,
                TargetSizeBytes = 10240,
                MinNumber = 1,
                MaxNumber = 1000000,
                DuplicateRatioPercent = 20,
                BufferSizeBytes = 4 * 1024 * 1024,
                Seed = 0
            };

            await _generator.GenerateAsync(request, progress);

            // Progress should be increasing
            for (var i = 1; i < progressReports.Count; i++)
                Assert.True(progressReports[i].BytesWritten >= progressReports[i - 1].BytesWritten);

            // Final progress should be close to target
            var finalProgress = progressReports.Last();
            Assert.True(finalProgress.BytesWritten >= request.TargetSizeBytes * 0.99);
            Assert.Equal(request.TargetSizeBytes, finalProgress.TargetBytes);
        }
        finally
        {
            if (File.Exists(outputPath))
                File.Delete(outputPath);
        }
    }
}