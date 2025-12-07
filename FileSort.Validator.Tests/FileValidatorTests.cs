using FileSort.Core.Comparison;
using FileSort.Core.Interfaces;
using Xunit;
using Record = FileSort.Core.Models.Record;

namespace FileSort.Validator.Tests;

public class FileValidatorTests
{
    private readonly IFileValidator _validator = new FileValidator();

    [Fact]
    public async Task ValidateAsync_ValidSortedFile_ReturnsValid()
    {
        var filePath = await CreateTestFileAsync(new[]
        {
            "1. Apple",
            "2. Apple",
            "1. Banana",
            "2. Banana"
        });

        try
        {
            var result = await _validator.ValidateAsync(filePath);

            Assert.True(result.IsValid);
            Assert.Equal(4, result.TotalRecords);
            Assert.Equal(0, result.InvalidRecords);
            Assert.Empty(result.Errors);
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    [Fact]
    public async Task ValidateAsync_OutOfOrderFile_ReturnsInvalid()
    {
        // Create a file where line 3 is out of order
        // Sorted order should be: "1. Apple", "2. Apple", "1. Banana"
        // But we have: "1. Apple", "1. Banana", "2. Apple" (line 3 is out of order)
        var filePath = await CreateTestFileAsync(new[]
        {
            "1. Apple",
            "1. Banana",
            "2. Apple" // Out of order - "2. Apple" should come before "1. Banana"
        });

        try
        {
            var result = await _validator.ValidateAsync(filePath);

            Assert.False(result.IsValid);
            // All 3 records are parsed successfully, but line 3 is out of order
            Assert.Equal(3, result.TotalRecords);
            Assert.Equal(1, result.InvalidRecords);
            Assert.Single(result.Errors);
            Assert.Contains("out of order", result.Errors[0].Message, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    [Fact]
    public async Task ValidateAsync_EmptyFile_ReturnsValid()
    {
        var filePath = await CreateTestFileAsync(Array.Empty<string>());

        try
        {
            var result = await _validator.ValidateAsync(filePath);

            Assert.True(result.IsValid);
            Assert.Equal(0, result.TotalRecords);
            Assert.Equal(0, result.InvalidRecords);
            Assert.Empty(result.Errors);
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    [Fact]
    public async Task ValidateAsync_SingleRecord_ReturnsValid()
    {
        var filePath = await CreateTestFileAsync(new[] { "1. Apple" });

        try
        {
            var result = await _validator.ValidateAsync(filePath);

            Assert.True(result.IsValid);
            Assert.Equal(1, result.TotalRecords);
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    [Fact]
    public async Task ValidateAsync_InvalidRecordFormat_ReturnsInvalid()
    {
        var filePath = await CreateTestFileAsync(new[]
        {
            "1. Apple",
            "Invalid line",
            "2. Banana"
        });

        try
        {
            var result = await _validator.ValidateAsync(filePath);

            Assert.False(result.IsValid);
            Assert.Equal(1, result.TotalRecords);
            Assert.Equal(1, result.InvalidRecords);
            Assert.Single(result.Errors);
            Assert.Contains("Invalid record format", result.Errors[0].Message);
            Assert.Equal(2, result.Errors[0].LineNumber);
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    [Fact]
    public async Task ValidateAsync_FileNotFound_ThrowsFileNotFoundException()
    {
        var nonExistentPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".txt");

        await Assert.ThrowsAsync<FileNotFoundException>(
            () => _validator.ValidateAsync(nonExistentPath));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task ValidateAsync_InvalidFilePath_ThrowsArgumentException(string? filePath)
    {
        await Assert.ThrowsAsync<ArgumentException>(
            () => _validator.ValidateAsync(filePath!));
    }

    [Fact]
    public async Task ValidateAsync_UnicodeCharacters_HandlesCorrectly()
    {
        // Sort by Text (ordinal) then Number: "Banana" < "Çherry" < "Äpple" in ordinal comparison
        var records = new List<Record>
        {
            new(2, "Banana"),
            new(1, "Çherry"),
            new(1, "Äpple")
        };
        records.Sort(RecordComparer.Instance);

        var lines = records.Select(r => r.ToLine());
        var filePath = await CreateTestFileAsync(lines);

        try
        {
            var result = await _validator.ValidateAsync(filePath);

            Assert.True(result.IsValid);
            Assert.Equal(3, result.TotalRecords);
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    [Fact]
    public async Task ValidateAsync_SpecialCharacters_HandlesCorrectly()
    {
        // Sort by Text (ordinal) then Number: "File-name.txt" < "File.name.txt" < "File_name.txt" in ordinal comparison
        var records = new List<Record>
        {
            new(2, "File-name.txt"),
            new(1, "File.name.txt"),
            new(1, "File_name.txt")
        };
        records.Sort(RecordComparer.Instance);

        var lines = records.Select(r => r.ToLine());
        var filePath = await CreateTestFileAsync(lines);

        try
        {
            var result = await _validator.ValidateAsync(filePath);

            Assert.True(result.IsValid);
            Assert.Equal(3, result.TotalRecords);
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    [Fact]
    public async Task ValidateAsync_StopsOnFirstError()
    {
        var filePath = await CreateTestFileAsync(new[]
        {
            "1. Apple",
            "2. Banana",
            "Invalid1",
            "Invalid2"
        });

        try
        {
            var result = await _validator.ValidateAsync(filePath);

            Assert.False(result.IsValid);
            // Should stop after first invalid record
            Assert.Single(result.Errors);
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    [Fact]
    public async Task ValidateAsync_StopsOnFirstOutOfOrder()
    {
        var filePath = await CreateTestFileAsync(new[]
        {
            "1. Apple",
            "2. Banana",
            "1. Apple", // Out of order
            "3. Cherry"
        });

        try
        {
            var result = await _validator.ValidateAsync(filePath);

            Assert.False(result.IsValid);
            Assert.Single(result.Errors);
            Assert.Equal(3, result.Errors[0].LineNumber);
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    [Fact]
    public async Task ValidateAsync_LargeFile_HandlesCorrectly()
    {
        // Generate records in sorted order: Text (ordinal) then Number (ascending)
        var records = new List<Record>();
        for (var i = 1; i <= 10000; i++) records.Add(new Record(i % 100, $"Text{i}"));

        // Sort the records using the same comparer as the validator
        records.Sort(RecordComparer.Instance);

        // Convert to lines
        var lines = records.Select(r => r.ToLine());

        var filePath = await CreateTestFileAsync(lines);

        try
        {
            var result = await _validator.ValidateAsync(filePath);

            Assert.True(result.IsValid);
            Assert.Equal(10000, result.TotalRecords);
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    [Fact]
    public async Task ValidateAsync_IdenticalRecords_HandlesCorrectly()
    {
        var filePath = await CreateTestFileAsync(new[]
        {
            "1. Apple",
            "1. Apple",
            "1. Apple"
        });

        try
        {
            var result = await _validator.ValidateAsync(filePath);

            Assert.True(result.IsValid);
            Assert.Equal(3, result.TotalRecords);
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    [Fact]
    public async Task ValidateAsync_ErrorContainsLineNumber()
    {
        var filePath = await CreateTestFileAsync(new[]
        {
            "1. Apple",
            "Invalid line"
        });

        try
        {
            var result = await _validator.ValidateAsync(filePath);

            Assert.False(result.IsValid);
            Assert.Equal(2, result.Errors[0].LineNumber);
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    [Fact]
    public async Task ValidateAsync_ErrorContainsLineContent()
    {
        var filePath = await CreateTestFileAsync(new[]
        {
            "1. Apple",
            "Invalid line"
        });

        try
        {
            var result = await _validator.ValidateAsync(filePath);

            Assert.False(result.IsValid);
            Assert.Equal("Invalid line", result.Errors[0].Line);
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    private static async Task<string> CreateTestFileAsync(IEnumerable<string> lines)
    {
        var path = Path.GetTempFileName();
        await File.WriteAllLinesAsync(path, lines);
        return path;
    }
}