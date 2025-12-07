using FileSort.Core.Requests;
using FileSort.Generator.Validation;
using Xunit;

namespace FileSort.Generator.Tests;

public class GeneratorRequestValidatorTests
{
    [Fact]
    public void Validate_NullRequest_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => GeneratorRequestValidator.Validate(null!));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_InvalidOutputFilePath_ThrowsArgumentException(string? outputPath)
    {
        var request = new GeneratorRequest
        {
            OutputFilePath = outputPath!,
            TargetSizeBytes = 1000,
            MinNumber = 1,
            MaxNumber = 100,
            DuplicateRatioPercent = 20,
            BufferSizeBytes = 4096
        };

        var exception = Assert.Throws<ArgumentException>(() => GeneratorRequestValidator.Validate(request));
        Assert.Contains("OutputFilePath", exception.Message);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Validate_InvalidTargetSizeBytes_ThrowsArgumentException(long targetSize)
    {
        var request = new GeneratorRequest
        {
            OutputFilePath = "test.txt",
            TargetSizeBytes = targetSize,
            MinNumber = 1,
            MaxNumber = 100,
            DuplicateRatioPercent = 20,
            BufferSizeBytes = 4096
        };

        var exception = Assert.Throws<ArgumentException>(() => GeneratorRequestValidator.Validate(request));
        Assert.Contains("TargetSizeBytes", exception.Message);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Validate_NegativeMinNumber_ThrowsArgumentException(int minNumber)
    {
        var request = new GeneratorRequest
        {
            OutputFilePath = "test.txt",
            TargetSizeBytes = 1000,
            MinNumber = minNumber,
            MaxNumber = 100,
            DuplicateRatioPercent = 20,
            BufferSizeBytes = 4096
        };

        var exception = Assert.Throws<ArgumentException>(() => GeneratorRequestValidator.Validate(request));
        Assert.Contains("MinNumber", exception.Message);
    }

    [Fact]
    public void Validate_MaxNumberLessThanMinNumber_ThrowsArgumentException()
    {
        var request = new GeneratorRequest
        {
            OutputFilePath = "test.txt",
            TargetSizeBytes = 1000,
            MinNumber = 100,
            MaxNumber = 50,
            DuplicateRatioPercent = 20,
            BufferSizeBytes = 4096
        };

        var exception = Assert.Throws<ArgumentException>(() => GeneratorRequestValidator.Validate(request));
        Assert.Contains("MaxNumber", exception.Message);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    [InlineData(150)]
    public void Validate_InvalidDuplicateRatioPercent_ThrowsArgumentException(int ratio)
    {
        var request = new GeneratorRequest
        {
            OutputFilePath = "test.txt",
            TargetSizeBytes = 1000,
            MinNumber = 1,
            MaxNumber = 100,
            DuplicateRatioPercent = ratio,
            BufferSizeBytes = 4096
        };

        var exception = Assert.Throws<ArgumentException>(() => GeneratorRequestValidator.Validate(request));
        Assert.Contains("DuplicateRatioPercent", exception.Message);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Validate_InvalidBufferSizeBytes_ThrowsArgumentException(int bufferSize)
    {
        var request = new GeneratorRequest
        {
            OutputFilePath = "test.txt",
            TargetSizeBytes = 1000,
            MinNumber = 1,
            MaxNumber = 100,
            DuplicateRatioPercent = 20,
            BufferSizeBytes = bufferSize
        };

        var exception = Assert.Throws<ArgumentException>(() => GeneratorRequestValidator.Validate(request));
        Assert.Contains("BufferSizeBytes", exception.Message);
    }

    [Fact]
    public void Validate_ValidRequest_DoesNotThrow()
    {
        var request = new GeneratorRequest
        {
            OutputFilePath = "test.txt",
            TargetSizeBytes = 1000,
            MinNumber = 1,
            MaxNumber = 100,
            DuplicateRatioPercent = 20,
            BufferSizeBytes = 4096
        };

        var exception = Record.Exception(() => GeneratorRequestValidator.Validate(request));
        Assert.Null(exception);
    }

    [Fact]
    public void Validate_MinNumberEqualsMaxNumber_DoesNotThrow()
    {
        var request = new GeneratorRequest
        {
            OutputFilePath = "test.txt",
            TargetSizeBytes = 1000,
            MinNumber = 50,
            MaxNumber = 50,
            DuplicateRatioPercent = 20,
            BufferSizeBytes = 4096
        };

        var exception = Record.Exception(() => GeneratorRequestValidator.Validate(request));
        Assert.Null(exception);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(50)]
    [InlineData(100)]
    public void Validate_ValidDuplicateRatioPercent_DoesNotThrow(int ratio)
    {
        var request = new GeneratorRequest
        {
            OutputFilePath = "test.txt",
            TargetSizeBytes = 1000,
            MinNumber = 1,
            MaxNumber = 100,
            DuplicateRatioPercent = ratio,
            BufferSizeBytes = 4096
        };

        var exception = Record.Exception(() => GeneratorRequestValidator.Validate(request));
        Assert.Null(exception);
    }
}