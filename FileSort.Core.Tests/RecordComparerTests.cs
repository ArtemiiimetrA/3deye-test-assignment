using FileSort.Core.Comparison;
using FileSort.Core.Models;
using Xunit;
using Record = FileSort.Core.Models.Record;

namespace FileSort.Core.Tests;

public class RecordComparerTests
{
    private readonly RecordComparer _comparer = RecordComparer.Instance;

    [Theory]
    [InlineData("Apple", "Banana", -1)]
    [InlineData("Banana", "Apple", 1)]
    [InlineData("Apple", "Apple", 0)]
    public void Compare_TextComparison_ReturnsExpected(string text1, string text2, int expected)
    {
        var record1 = new Record(1, text1);
        var record2 = new Record(1, text2);

        int result = _comparer.Compare(record1, record2);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void Compare_CaseSensitive_ReturnsNonZero()
    {
        var record1 = new Record(1, "apple");
        var record2 = new Record(1, "Apple");

        int result = _comparer.Compare(record1, record2);

        Assert.NotEqual(0, result);
    }

    [Fact]
    public void Compare_SameTextDifferentNumbers_UsesNumberAsTieBreaker()
    {
        var record1 = new Record(1, "Apple");
        var record2 = new Record(2, "Apple");

        int result = _comparer.Compare(record1, record2);

        Assert.True(result < 0); // 1 < 2
    }

    [Fact]
    public void Compare_IdenticalRecords_ReturnsZero()
    {
        var record1 = new Record(123, "Test");
        var record2 = new Record(123, "Test");

        int result = _comparer.Compare(record1, record2);

        Assert.Equal(0, result);
    }

    [Fact]
    public void Compare_EmptyStrings_HandlesCorrectly()
    {
        var record1 = new Record(1, "");
        var record2 = new Record(1, "A");

        int result = _comparer.Compare(record1, record2);

        Assert.True(result < 0); // Empty < "A"
    }

    [Fact]
    public void Compare_UnicodeCharacters_HandlesCorrectly()
    {
        var record1 = new Record(1, "Äpple");
        var record2 = new Record(1, "Banana");

        int result = _comparer.Compare(record1, record2);

        Assert.NotEqual(0, result);
    }

    [Fact]
    public void Compare_SpecialCharacters_HandlesCorrectly()
    {
        var record1 = new Record(1, "File.name");
        var record2 = new Record(1, "File-name");

        int result = _comparer.Compare(record1, record2);

        Assert.NotEqual(0, result);
    }

    [Fact]
    public void Compare_VeryLongText_HandlesCorrectly()
    {
        string longText1 = new string('A', 10000);
        string longText2 = new string('B', 10000);
        var record1 = new Record(1, longText1);
        var record2 = new Record(1, longText2);

        int result = _comparer.Compare(record1, record2);

        Assert.True(result < 0);
    }

    [Fact]
    public void Compare_LargeNumbers_HandlesCorrectly()
    {
        var record1 = new Record(int.MaxValue - 1, "Test");
        var record2 = new Record(int.MaxValue, "Test");

        int result = _comparer.Compare(record1, record2);

        Assert.True(result < 0);
    }

    [Fact]
    public void Compare_ZeroNumbers_HandlesCorrectly()
    {
        var record1 = new Record(0, "Test");
        var record2 = new Record(1, "Test");

        int result = _comparer.Compare(record1, record2);

        Assert.True(result < 0);
    }

    [Fact]
    public void Compare_WhitespaceOnlyText_HandlesCorrectly()
    {
        var record1 = new Record(1, "   ");
        var record2 = new Record(1, "A");

        int result = _comparer.Compare(record1, record2);

        Assert.NotEqual(0, result);
    }

    [Fact]
    public void Compare_TextWithLeadingTrailingSpaces_HandlesCorrectly()
    {
        var record1 = new Record(1, " Apple");
        var record2 = new Record(1, "Apple");

        int result = _comparer.Compare(record1, record2);

        Assert.NotEqual(0, result); // Space before "Apple" is different
    }

    [Fact]
    public void Compare_MultipleIdenticalTexts_OrdersByNumber()
    {
        var record1 = new Record(1, "Apple");
        var record2 = new Record(2, "Apple");
        var record3 = new Record(3, "Apple");

        Assert.True(_comparer.Compare(record1, record2) < 0);
        Assert.True(_comparer.Compare(record2, record3) < 0);
        Assert.True(_comparer.Compare(record1, record3) < 0);
    }

    [Fact]
    public void Compare_TextComparisonWithNumbers_HandlesCorrectly()
    {
        var record1 = new Record(1, "1Text");
        var record2 = new Record(2, "2Text");

        int result = _comparer.Compare(record1, record2);

        Assert.True(result < 0); // "1Text" < "2Text" lexicographically
    }
}
