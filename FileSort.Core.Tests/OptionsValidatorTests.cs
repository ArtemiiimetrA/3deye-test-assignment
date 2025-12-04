using FileSort.Core.Options;
using FileSort.Core.Validation;
using Xunit;

namespace FileSort.Core.Tests;

public class OptionsValidatorTests
{
    [Fact]
    public void Validate_SortOptions_Null_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => SortOptionsValidator.Validate((SortOptions)null!));
    }

    [Fact]
    public void Validate_SortOptions_InvalidInputPath_ThrowsException()
    {
        var options = new SortOptions
        {
            InputFilePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".txt"),
            OutputFilePath = "output.txt",
            TempDirectory = "temp",
            MaxRamMb = 2048,
            ChunkSizeMb = 256,
            MaxDegreeOfParallelism = Environment.ProcessorCount,
            FileChunkTemplate = "chunk_{0:0000}.tmp",
            BufferSizeBytes = 4 * 1024 * 1024,
            DeleteTempFiles = true,
            MaxOpenFiles = 500,
            AdaptiveChunkSize = true,
            MinChunkSizeMb = 64,
            MaxChunkSizeMb = 512
        };

        Assert.Throws<FileNotFoundException>(() => SortOptionsValidator.Validate(options));
    }

    [Fact]
    public void Validate_SortOptions_EmptyInputPath_ThrowsException()
    {
        var options = new SortOptions
        {
            InputFilePath = "",
            OutputFilePath = "output.txt",
            TempDirectory = "temp",
            MaxRamMb = 2048,
            ChunkSizeMb = 256,
            MaxDegreeOfParallelism = Environment.ProcessorCount,
            FileChunkTemplate = "chunk_{0:0000}.tmp",
            BufferSizeBytes = 4 * 1024 * 1024,
            DeleteTempFiles = true,
            MaxOpenFiles = 500,
            AdaptiveChunkSize = true,
            MinChunkSizeMb = 64,
            MaxChunkSizeMb = 512
        };

        Assert.Throws<ArgumentException>(() => SortOptionsValidator.Validate(options));
    }

    [Fact]
    public void Validate_SortOptions_ChunkSizeExceedsMaxRam_ThrowsException()
    {
        var options = new SortOptions
        {
            InputFilePath = "test.txt",
            OutputFilePath = "output.txt",
            TempDirectory = "temp",
            ChunkSizeMb = 3000,
            MaxRamMb = 2048,
            MaxDegreeOfParallelism = Environment.ProcessorCount,
            FileChunkTemplate = "chunk_{0:0000}.tmp",
            BufferSizeBytes = 4 * 1024 * 1024,
            DeleteTempFiles = true,
            MaxOpenFiles = 500,
            AdaptiveChunkSize = true,
            MinChunkSizeMb = 64,
            MaxChunkSizeMb = 512
        };

        // Create test file first
        File.WriteAllText("test.txt", "1. Test");
        try
        {
            Assert.Throws<ArgumentException>(() => SortOptionsValidator.Validate(options));
        }
        finally
        {
            File.Delete("test.txt");
        }
    }

    [Fact]
    public void Validate_GeneratorOptions_Null_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => GeneratorOptionsValidator.Validate((GeneratorOptions)null!));
    }

    [Fact]
    public void Validate_GeneratorOptions_InvalidDuplicateRatio_ThrowsException()
    {
        var options = new GeneratorOptions
        {
            OutputFilePath = "output.txt",
            TargetSizeBytes = 1024,
            DuplicateRatioPercent = 150
        };

        Assert.Throws<ArgumentException>(() => GeneratorOptionsValidator.Validate(options));
    }

    [Fact]
    public void Validate_GeneratorOptions_MaxNumberLessThanMin_ThrowsException()
    {
        var options = new GeneratorOptions
        {
            OutputFilePath = "output.txt",
            TargetSizeBytes = 1024,
            MinNumber = 100,
            MaxNumber = 50
        };

        Assert.Throws<ArgumentException>(() => GeneratorOptionsValidator.Validate(options));
    }
}
