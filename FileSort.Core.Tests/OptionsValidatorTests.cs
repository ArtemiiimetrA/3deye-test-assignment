using FileSort.Core.Options;
using FileSort.Core.Validation;
using Xunit;

namespace FileSort.Core.Tests;

public class OptionsValidatorTests
{
    [Fact]
    public void Validate_SortOptions_Null_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => OptionsValidator.Validate((SortOptions)null!));
    }

    [Fact]
    public void Validate_SortOptions_InvalidInputPath_ThrowsException()
    {
        var options = new SortOptions
        {
            InputFilePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".txt"),
            OutputFilePath = "output.txt"
        };

        Assert.Throws<FileNotFoundException>(() => OptionsValidator.Validate(options));
    }

    [Fact]
    public void Validate_SortOptions_EmptyInputPath_ThrowsException()
    {
        var options = new SortOptions
        {
            InputFilePath = "",
            OutputFilePath = "output.txt"
        };

        Assert.Throws<ArgumentException>(() => OptionsValidator.Validate(options));
    }

    [Fact]
    public void Validate_SortOptions_ChunkSizeExceedsMaxRam_ThrowsException()
    {
        var options = new SortOptions
        {
            InputFilePath = "test.txt",
            OutputFilePath = "output.txt",
            ChunkSizeMb = 3000,
            MaxRamMb = 2048
        };

        // Create test file first
        File.WriteAllText("test.txt", "1. Test");
        try
        {
            Assert.Throws<ArgumentException>(() => OptionsValidator.Validate(options));
        }
        finally
        {
            File.Delete("test.txt");
        }
    }

    [Fact]
    public void Validate_GeneratorOptions_Null_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => OptionsValidator.Validate((GeneratorOptions)null!));
    }

    [Fact]
    public void Validate_GeneratorOptions_InvalidDuplicateRatio_ThrowsException()
    {
        var options = new GeneratorOptions
        {
            OutputFilePath = "output.txt",
            DuplicateRatioPercent = 150
        };

        Assert.Throws<ArgumentException>(() => OptionsValidator.Validate(options));
    }

    [Fact]
    public void Validate_GeneratorOptions_MaxNumberLessThanMin_ThrowsException()
    {
        var options = new GeneratorOptions
        {
            OutputFilePath = "output.txt",
            MinNumber = 100,
            MaxNumber = 50
        };

        Assert.Throws<ArgumentException>(() => OptionsValidator.Validate(options));
    }
}
