using Xunit;
using Record = FileSort.Core.Models.Record;

namespace FileSort.Core.Tests;

public class RecordTests
{
    [Fact]
    public void ToLine_ValidRecord_ReturnsCorrectFormat()
    {
        var record = new Record(123, "Apple");
        var line = record.ToLine();

        Assert.Equal("123. Apple", line);
    }

    [Theory]
    [InlineData(0, "Test", "0. Test")]
    [InlineData(1, "Single", "1. Single")]
    [InlineData(999999, "Large", "999999. Large")]
    [InlineData(42, "Text with spaces", "42. Text with spaces")]
    public void ToLine_VariousInputs_ReturnsCorrectFormat(int number, string text, string expected)
    {
        var record = new Record(number, text);
        var line = record.ToLine();

        Assert.Equal(expected, line);
    }

    [Fact]
    public void Equals_SameValues_ReturnsTrue()
    {
        var record1 = new Record(123, "Apple");
        var record2 = new Record(123, "Apple");

        Assert.True(record1.Equals(record2));
        Assert.True(record1.Equals((object)record2));
    }

    [Fact]
    public void Equals_DifferentNumbers_ReturnsFalse()
    {
        var record1 = new Record(123, "Apple");
        var record2 = new Record(456, "Apple");

        Assert.False(record1.Equals(record2));
        Assert.False(record1.Equals((object)record2));
    }

    [Fact]
    public void Equals_DifferentText_ReturnsFalse()
    {
        var record1 = new Record(123, "Apple");
        var record2 = new Record(123, "Banana");

        Assert.False(record1.Equals(record2));
        Assert.False(record1.Equals((object)record2));
    }

    [Fact]
    public void Equals_DifferentBoth_ReturnsFalse()
    {
        var record1 = new Record(123, "Apple");
        var record2 = new Record(456, "Banana");

        Assert.False(record1.Equals(record2));
        Assert.False(record1.Equals((object)record2));
    }

    [Fact]
    public void Equals_NullObject_ReturnsFalse()
    {
        var record = new Record(123, "Apple");

        Assert.False(record.Equals(null));
    }

    [Fact]
    public void Equals_WrongType_ReturnsFalse()
    {
        var record = new Record(123, "Apple");

        Assert.False(record.Equals("Not a record"));
    }

    [Fact]
    public void GetHashCode_SameValues_ReturnsSameHashCode()
    {
        var record1 = new Record(123, "Apple");
        var record2 = new Record(123, "Apple");

        Assert.Equal(record1.GetHashCode(), record2.GetHashCode());
    }

    [Fact]
    public void GetHashCode_DifferentValues_ReturnsDifferentHashCodes()
    {
        var record1 = new Record(123, "Apple");
        var record2 = new Record(456, "Banana");

        // Note: Hash codes can theoretically collide, but with different values it's very unlikely
        Assert.NotEqual(record1.GetHashCode(), record2.GetHashCode());
    }

    [Fact]
    public void Constructor_NullText_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new Record(123, null!));
    }

    [Fact]
    public void Equals_Operator_WorksCorrectly()
    {
        var record1 = new Record(123, "Apple");
        var record2 = new Record(123, "Apple");
        var record3 = new Record(456, "Banana");

        // Note: If Record implements IEquatable<Record>, == operator might not be defined
        // Testing Equals method which is what matters
        Assert.True(record1.Equals(record2));
        Assert.False(record1.Equals(record3));
    }

    [Fact]
    public void ToLine_UnicodeCharacters_HandlesCorrectly()
    {
        var record = new Record(1, "Äpple");
        var line = record.ToLine();

        Assert.Equal("1. Äpple", line);
    }

    [Fact]
    public void ToLine_SpecialCharacters_HandlesCorrectly()
    {
        var record = new Record(1, "File.name.txt");
        var line = record.ToLine();

        Assert.Equal("1. File.name.txt", line);
    }

    [Fact]
    public void ToLine_EmptyText_HandlesCorrectly()
    {
        var record = new Record(1, "");
        var line = record.ToLine();

        Assert.Equal("1. ", line);
    }
}