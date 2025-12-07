using FileSort.Core.Interfaces;
using FileSort.Core.Requests;
using Xunit;

namespace FileSort.Generator.Tests;

public class TestFileGeneratorErrorHandlingTests
{
    private readonly ITestFileGenerator _generator = new TestFileGenerator();

    [Fact]
    public async Task GenerateAsync_NonExistentDirectory_CreatesDirectory()
    {
        var nonExistentDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var outputPath = Path.Combine(nonExistentDir, "output.txt");

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

            await _generator.GenerateAsync(request);

            Assert.True(File.Exists(outputPath));
            Assert.True(Directory.Exists(nonExistentDir));
        }
        finally
        {
            if (File.Exists(outputPath))
                File.Delete(outputPath);
            if (Directory.Exists(nonExistentDir))
                Directory.Delete(nonExistentDir);
        }
    }

    [Fact]
    public async Task GenerateAsync_ExistingFile_Overwrites()
    {
        var outputPath = Path.GetTempFileName();
        File.WriteAllText(outputPath, "Old content");

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

            await _generator.GenerateAsync(request);

            Assert.True(File.Exists(outputPath));
            var content = await File.ReadAllTextAsync(outputPath);
            Assert.DoesNotContain("Old content", content);
        }
        finally
        {
            if (File.Exists(outputPath))
                File.Delete(outputPath);
        }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(50)]
    [InlineData(100)]
    public async Task GenerateAsync_VariousDuplicateRatios_GeneratesFile(int duplicateRatio)
    {
        var outputPath = Path.GetTempFileName();
        try
        {
            var request = new GeneratorRequest
            {
                OutputFilePath = outputPath,
                TargetSizeBytes = 10240,
                MinNumber = 1,
                MaxNumber = 100,
                DuplicateRatioPercent = duplicateRatio,
                BufferSizeBytes = 4 * 1024 * 1024,
                Seed = 42
            };

            await _generator.GenerateAsync(request);

            Assert.True(File.Exists(outputPath));
            Assert.True(new FileInfo(outputPath).Length >= request.TargetSizeBytes * 0.99);
        }
        finally
        {
            if (File.Exists(outputPath))
                File.Delete(outputPath);
        }
    }

    [Fact]
    public async Task GenerateAsync_MinNumberEqualsMaxNumber_GeneratesFile()
    {
        var outputPath = Path.GetTempFileName();
        try
        {
            var request = new GeneratorRequest
            {
                OutputFilePath = outputPath,
                TargetSizeBytes = 1024,
                MinNumber = 50,
                MaxNumber = 50,
                DuplicateRatioPercent = 20,
                BufferSizeBytes = 4 * 1024 * 1024,
                Seed = 42
            };

            await _generator.GenerateAsync(request);

            Assert.True(File.Exists(outputPath));
        }
        finally
        {
            if (File.Exists(outputPath))
                File.Delete(outputPath);
        }
    }

    [Fact]
    public async Task GenerateAsync_VerySmallTargetSize_GeneratesFile()
    {
        var outputPath = Path.GetTempFileName();
        try
        {
            var request = new GeneratorRequest
            {
                OutputFilePath = outputPath,
                TargetSizeBytes = 10, // Very small
                MinNumber = 1,
                MaxNumber = 100,
                DuplicateRatioPercent = 20,
                BufferSizeBytes = 4 * 1024 * 1024,
                Seed = 42
            };

            await _generator.GenerateAsync(request);

            Assert.True(File.Exists(outputPath));
            // Should be at least close to target (within tolerance)
            Assert.True(new FileInfo(outputPath).Length >= request.TargetSizeBytes * 0.99);
        }
        finally
        {
            if (File.Exists(outputPath))
                File.Delete(outputPath);
        }
    }
}