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
}
