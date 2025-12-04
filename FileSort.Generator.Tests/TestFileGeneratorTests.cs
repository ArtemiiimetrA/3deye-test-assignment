using FileSort.Core.Interfaces;
using FileSort.Core.Options;
using FileSort.Core.Parsing;
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
            var options = new GeneratorOptions(
                outputFilePath: outputPath,
                targetSizeBytes: 1024, // 1KB
                minNumber: 1,
                maxNumber: 100,
                duplicateRatioPercent: 20,
                bufferSizeBytes: 4 * 1024 * 1024,
                seed: 42);

            await _generator.GenerateAsync(options);

            Assert.True(File.Exists(outputPath));
            Assert.True(new FileInfo(outputPath).Length >= options.TargetSizeBytes);

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
            var options1 = new GeneratorOptions(
                outputFilePath: outputPath1,
                targetSizeBytes: 10240, // 10KB
                minNumber: 1,
                maxNumber: 1000000,
                duplicateRatioPercent: 20,
                bufferSizeBytes: 4 * 1024 * 1024,
                seed: 12345);

            var options2 = new GeneratorOptions(
                outputFilePath: outputPath2,
                targetSizeBytes: 10240,
                minNumber: 1,
                maxNumber: 1000000,
                duplicateRatioPercent: 20,
                bufferSizeBytes: 4 * 1024 * 1024,
                seed: 12345);

            await _generator.GenerateAsync(options1);
            await _generator.GenerateAsync(options2);

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
            var options = new GeneratorOptions(
                outputFilePath: outputPath,
                targetSizeBytes: 10240, // 10KB
                minNumber: 1,
                maxNumber: 1000000,
                duplicateRatioPercent: 20,
                bufferSizeBytes: 4 * 1024 * 1024,
                seed: 0);

            var progressReports = new List<FileSort.Core.Models.GeneratorProgress>();
            var progress = new Progress<FileSort.Core.Models.GeneratorProgress>(p => progressReports.Add(p));

            await _generator.GenerateAsync(options, progress);

            Assert.NotEmpty(progressReports);
            Assert.True(progressReports.Last().BytesWritten >= options.TargetSizeBytes * 0.99);
        }
        finally
        {
            if (File.Exists(outputPath))
                File.Delete(outputPath);
        }
    }
}
