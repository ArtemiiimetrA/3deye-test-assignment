using FileSort.Core.Models;
using Xunit;

namespace FileSort.Core.Tests;

public class RecordKeyTests
{
    [Fact]
    public void CompareTo_SameValues_ReturnsZero()
    {
        var key1 = new RecordKey("Apple", 123);
        var key2 = new RecordKey("Apple", 123);

        Assert.Equal(0, key1.CompareTo(key2));
    }

    [Fact]
    public void CompareTo_DifferentText_ReturnsNonZero()
    {
        var key1 = new RecordKey("Apple", 123);
        var key2 = new RecordKey("Banana", 123);

        Assert.True(key1.CompareTo(key2) < 0);
        Assert.True(key2.CompareTo(key1) > 0);
    }

    [Fact]
    public void CompareTo_SameTextDifferentNumbers_UsesNumberAsTieBreaker()
    {
        var key1 = new RecordKey("Apple", 1);
        var key2 = new RecordKey("Apple", 2);

        Assert.True(key1.CompareTo(key2) < 0);
        Assert.True(key2.CompareTo(key1) > 0);
    }

    [Fact]
    public void CompareTo_TextComparisonIsCaseSensitive()
    {
        var key1 = new RecordKey("apple", 123);
        var key2 = new RecordKey("Apple", 123);

        Assert.NotEqual(0, key1.CompareTo(key2));
    }

    [Fact]
    public void CompareTo_EmptyStrings_HandlesCorrectly()
    {
        var key1 = new RecordKey("", 1);
        var key2 = new RecordKey("A", 1);

        Assert.True(key1.CompareTo(key2) < 0);
        Assert.True(key2.CompareTo(key1) > 0);
    }

    [Fact]
    public void CompareTo_UnicodeCharacters_HandlesCorrectly()
    {
        var key1 = new RecordKey("Äpple", 1);
        var key2 = new RecordKey("Banana", 1);

        Assert.NotEqual(0, key1.CompareTo(key2));
    }

    [Fact]
    public void CompareTo_SpecialCharacters_HandlesCorrectly()
    {
        var key1 = new RecordKey("File.name", 1);
        var key2 = new RecordKey("File-name", 1);

        Assert.NotEqual(0, key1.CompareTo(key2));
    }

    [Fact]
    public void CompareTo_VeryLargeNumbers_HandlesCorrectly()
    {
        var key1 = new RecordKey("Test", int.MaxValue - 1);
        var key2 = new RecordKey("Test", int.MaxValue);

        Assert.True(key1.CompareTo(key2) < 0);
    }

    [Fact]
    public void CompareTo_ZeroNumbers_HandlesCorrectly()
    {
        var key1 = new RecordKey("Test", 0);
        var key2 = new RecordKey("Test", 1);

        Assert.True(key1.CompareTo(key2) < 0);
    }

    [Fact]
    public void Constructor_NullText_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new RecordKey(null!, 123));
    }

    [Theory]
    [InlineData("Apple", "Banana", -1)]
    [InlineData("Banana", "Apple", 1)]
    [InlineData("Apple", "Apple", 0)]
    public void CompareTo_TextComparison_ReturnsExpected(string text1, string text2, int expectedSign)
    {
        var key1 = new RecordKey(text1, 1);
        var key2 = new RecordKey(text2, 1);

        int result = key1.CompareTo(key2);
        if (expectedSign < 0)
            Assert.True(result < 0);
        else if (expectedSign > 0)
            Assert.True(result > 0);
        else
            Assert.Equal(0, result);
    }

    [Fact]
    public void CompareTo_IdenticalKeys_ReturnsZero()
    {
        var key1 = new RecordKey("Test", 123);
        var key2 = new RecordKey("Test", 123);

        Assert.Equal(0, key1.CompareTo(key2));
    }
}

