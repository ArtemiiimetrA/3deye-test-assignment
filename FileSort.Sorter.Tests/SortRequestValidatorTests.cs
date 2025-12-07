using FileSort.Core.Requests;
using FileSort.Sorter.Validation;
using Xunit;

namespace FileSort.Sorter.Tests;

public class SortRequestValidatorTests
{
    private static string CreateTempFile()
    {
        string path = Path.GetTempFileName();
        File.WriteAllText(path, "1. Test");
        return path;
    }

    private static SortRequest CreateBaseRequest(string inputPath) => new SortRequest
    {
        InputFilePath = inputPath,
        OutputFilePath = "output.txt",
        TempDirectory = Path.GetTempPath(),
        MaxRamMb = 100,
        ChunkSizeMb = 10,
        MaxDegreeOfParallelism = 4,
        FileChunkTemplate = "chunk_{0}.tmp",
        BufferSizeBytes = 4096,
        DeleteTempFiles = true,
        MaxOpenFiles = 500,
        MaxMergeParallelism = 2,
        AdaptiveChunkSize = false,
        MinChunkSizeMb = 64,
        MaxChunkSizeMb = 512
    };

    [Fact]
    public void Validate_NullRequest_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => SortRequestValidator.Validate(null!));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_InvalidInputFilePath_ThrowsArgumentException(string? inputPath)
    {
        string tempFile = CreateTempFile();
        try
        {
            var request = CreateBaseRequest(inputPath!);

            var exception = Assert.Throws<ArgumentException>(() => SortRequestValidator.Validate(request));
            Assert.Contains("InputFilePath", exception.Message);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void Validate_NonExistentInputFile_ThrowsFileNotFoundException()
    {
        var request = CreateBaseRequest(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".txt"));

        Assert.Throws<FileNotFoundException>(() => SortRequestValidator.Validate(request));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_InvalidOutputFilePath_ThrowsArgumentException(string? outputPath)
    {
        string tempFile = CreateTempFile();
        try
        {
            var request = CreateBaseRequest(tempFile) with
            {
                OutputFilePath = outputPath!
            };

            var exception = Assert.Throws<ArgumentException>(() => SortRequestValidator.Validate(request));
            Assert.Contains("OutputFilePath", exception.Message);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Validate_InvalidChunkSizeMb_ThrowsArgumentException(int chunkSize)
    {
        string tempFile = CreateTempFile();
        try
        {
            var request = CreateBaseRequest(tempFile) with
            {
                ChunkSizeMb = chunkSize
            };

            var exception = Assert.Throws<ArgumentException>(() => SortRequestValidator.Validate(request));
            Assert.Contains("ChunkSizeMb", exception.Message);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void Validate_ChunkSizeExceedsMaxRam_ThrowsArgumentException()
    {
        string tempFile = CreateTempFile();
        try
        {
            var request = CreateBaseRequest(tempFile) with
            {
                ChunkSizeMb = 150
            };

            var exception = Assert.Throws<ArgumentException>(() => SortRequestValidator.Validate(request));
            Assert.Contains("ChunkSizeMb", exception.Message);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Validate_InvalidMaxRamMb_ThrowsArgumentException(int maxRam)
    {
        string tempFile = CreateTempFile();
        try
        {
            var request = CreateBaseRequest(tempFile) with
            {
                MaxRamMb = maxRam
            };

            var exception = Assert.Throws<ArgumentException>(() => SortRequestValidator.Validate(request));
            Assert.Contains("MaxRamMb", exception.Message);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Validate_InvalidMaxDegreeOfParallelism_ThrowsArgumentException(int parallelism)
    {
        string tempFile = CreateTempFile();
        try
        {
            var request = CreateBaseRequest(tempFile) with
            {
                MaxDegreeOfParallelism = parallelism
            };

            var exception = Assert.Throws<ArgumentException>(() => SortRequestValidator.Validate(request));
            Assert.Contains("MaxDegreeOfParallelism", exception.Message);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Validate_InvalidBufferSizeBytes_ThrowsArgumentException(int bufferSize)
    {
        string tempFile = CreateTempFile();
        try
        {
            var request = CreateBaseRequest(tempFile) with
            {
                BufferSizeBytes = bufferSize
            };

            var exception = Assert.Throws<ArgumentException>(() => SortRequestValidator.Validate(request));
            Assert.Contains("BufferSizeBytes", exception.Message);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Theory]
    [InlineData(1)]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_InvalidMaxOpenFiles_ThrowsArgumentException(int maxOpenFiles)
    {
        string tempFile = CreateTempFile();
        try
        {
            var request = CreateBaseRequest(tempFile) with
            {
                MaxOpenFiles = maxOpenFiles
            };

            var exception = Assert.Throws<ArgumentException>(() => SortRequestValidator.Validate(request));
            Assert.Contains("MaxOpenFiles", exception.Message);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void Validate_MinChunkSizeExceedsMaxChunkSize_ThrowsArgumentException()
    {
        string tempFile = CreateTempFile();
        try
        {
            var request = CreateBaseRequest(tempFile) with
            {
                AdaptiveChunkSize = true,
                MinChunkSizeMb = 512,
                MaxChunkSizeMb = 256
            };

            var exception = Assert.Throws<ArgumentException>(() => SortRequestValidator.Validate(request));
            Assert.Contains("MinChunkSizeMb", exception.Message);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void Validate_ValidRequest_DoesNotThrow()
    {
        string tempFile = CreateTempFile();
        try
        {
            var request = CreateBaseRequest(tempFile);

            var exception = Record.Exception(() => SortRequestValidator.Validate(request));
            Assert.Null(exception);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }
}

