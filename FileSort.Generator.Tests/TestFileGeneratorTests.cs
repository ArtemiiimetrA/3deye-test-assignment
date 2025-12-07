using FileSort.Core.Interfaces;
using FileSort.Core.Models;
using FileSort.Core.Models.Progress;
using FileSort.Core.Parsing;
using FileSort.Core.Requests;
using Xunit;

namespace FileSort.Generator.Tests;

public class TestFileGeneratorTests
{
    private readonly ITestFileGenerator _generator = new TestFileGenerator();

    [Fact]
    public async Task GenerateAsync_SmallFile_GeneratesValidFile()
    {
        string outputPath = Path.GetTempFileName();
        try
        {
            var request = new GeneratorRequest
            {
                OutputFilePath = outputPath,
                TargetSizeBytes = 1024, // 1KB
                MinNumber = 1,
                MaxNumber = 100,
                DuplicateRatioPercent = 20,
                BufferSizeBytes = 4 * 1024 * 1024,
                Seed = 42
            };

            await _generator.GenerateAsync(request);

            Assert.True(File.Exists(outputPath));
            Assert.True(new FileInfo(outputPath).Length >= request.TargetSizeBytes);

            // Verify file format
            string[] lines = await File.ReadAllLinesAsync(outputPath);
            Assert.NotEmpty(lines);

            foreach (string line in lines)
            {
                Assert.True(RecordParser.TryParse(line, out _), $"Invalid line format: {line}");
            }
        }
        finally
        {
            if (File.Exists(outputPath))
                File.Delete(outputPath);
        }
    }

    [Fact]
    public async Task GenerateAsync_WithSeed_ProducesDeterministicOutput()
    {
        string outputPath1 = Path.GetTempFileName();
        string outputPath2 = Path.GetTempFileName();
        try
        {
            var request1 = new GeneratorRequest
            {
                OutputFilePath = outputPath1,
                TargetSizeBytes = 10240, // 10KB
                MinNumber = 1,
                MaxNumber = 1000000,
                DuplicateRatioPercent = 20,
                BufferSizeBytes = 4 * 1024 * 1024,
                Seed = 12345
            };

            var request2 = new GeneratorRequest
            {
                OutputFilePath = outputPath2,
                TargetSizeBytes = 10240,
                MinNumber = 1,
                MaxNumber = 1000000,
                DuplicateRatioPercent = 20,
                BufferSizeBytes = 4 * 1024 * 1024,
                Seed = 12345
            };

            await _generator.GenerateAsync(request1);
            await _generator.GenerateAsync(request2);

            byte[] file1 = await File.ReadAllBytesAsync(outputPath1);
            byte[] file2 = await File.ReadAllBytesAsync(outputPath2);

            Assert.Equal(file1, file2);
        }
        finally
        {
            if (File.Exists(outputPath1))
                File.Delete(outputPath1);
            if (File.Exists(outputPath2))
                File.Delete(outputPath2);
        }
    }

    [Fact]
    public async Task GenerateAsync_ReportsProgress()
    {
        string outputPath = Path.GetTempFileName();
        try
        {
            var request = new GeneratorRequest
            {
                OutputFilePath = outputPath,
                TargetSizeBytes = 10240, // 10KB
                MinNumber = 1,
                MaxNumber = 1000000,
                DuplicateRatioPercent = 20,
                BufferSizeBytes = 4 * 1024 * 1024,
                Seed = 0
            };

            var progressReports = new List<GeneratorProgress>();
            var progress = new Progress<GeneratorProgress>(p => progressReports.Add(p));

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
}
