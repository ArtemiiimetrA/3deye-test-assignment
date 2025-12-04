using FileSort.Core.Interfaces;
using FileSort.Core.Options;
using FileSort.Core.Parsing;
using FileSort.Generator;
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
            var options = new GeneratorOptions
            {
                OutputFilePath = outputPath,
                TargetSizeBytes = 1024, // 1KB
                MinNumber = 1,
                MaxNumber = 100,
                DuplicateRatioPercent = 20,
                Seed = 42
            };

            await _generator.GenerateAsync(options);

            Assert.True(File.Exists(outputPath));
            Assert.True(new FileInfo(outputPath).Length >= 1024 * 0.9); // Within 90% of target

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
            var options1 = new GeneratorOptions
            {
                OutputFilePath = outputPath1,
                TargetSizeBytes = 10240, // 10KB
                Seed = 12345
            };

            var options2 = new GeneratorOptions
            {
                OutputFilePath = outputPath2,
                TargetSizeBytes = 10240,
                Seed = 12345
            };

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
            var options = new GeneratorOptions
            {
                OutputFilePath = outputPath,
                TargetSizeBytes = 10240, // 10KB
            };

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
