using FileSort.Core.Models;
using FileSort.Core.Parsing;
using Xunit;
using Record = FileSort.Core.Models.Record;

namespace FileSort.Core.Tests;

public class RecordParserTests
{
    [Theory]
    [InlineData("415. Apple", 415, "Apple")]
    [InlineData("1. Apple", 1, "Apple")]
    [InlineData("0. Test", 0, "Test")]
    [InlineData("999999. SingleWord", 999999, "SingleWord")]
    [InlineData("123. Very long text with many words", 123, "Very long text with many words")]
    public void TryParse_ValidFormats_ReturnsTrue(string line, int expectedNumber, string expectedText)
    {
        bool result = RecordParser.TryParse(line, out Record record);

        Assert.True(result);
        Assert.Equal(expectedNumber, record.Number);
        Assert.Equal(expectedText, record.Text);
    }

    [Theory]
    [InlineData("  123.  Padded  ", 123, "Padded")]
    [InlineData("123.  Padded", 123, "Padded")]
    [InlineData("  123. Padded", 123, "Padded")]
    public void TryParse_WithWhitespace_TrimsCorrectly(string line, int expectedNumber, string expectedText)
    {
        bool result = RecordParser.TryParse(line, out Record record);

        Assert.True(result);
        Assert.Equal(expectedNumber, record.Number);
        Assert.Equal(expectedText, record.Text);
    }

    [Theory]
    [InlineData("abc. Test")]
    [InlineData(". No number")]
    [InlineData("123")]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("123.")]
    [InlineData(".Text")]
    [InlineData("-1. Test")]
    public void TryParse_InvalidFormats_ReturnsFalse(string? line)
    {
        bool result = RecordParser.TryParse(line, out Record record);

        Assert.False(result);
        Assert.Equal(default(Record), record);
    }

    [Fact]
    public void TryParse_EmptyTextAfterPeriod_ReturnsFalse()
    {
        bool result = RecordParser.TryParse("123. ", out Record record);

        Assert.False(result);
    }
}
