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

    [Theory]
    [InlineData("2147483647. Test")] // int.MaxValue
    [InlineData("0. Test")]
    [InlineData("999999999. Test")]
    public void TryParse_VeryLargeNumbers_ReturnsTrue(string line)
    {
        bool result = RecordParser.TryParse(line, out Record record);

        Assert.True(result);
    }

    [Theory]
    [InlineData("001. Test", 1, "Test")] // Leading zeros should be parsed as regular number
    [InlineData("000. Test", 0, "Test")]
    public void TryParse_NumbersWithLeadingZeros_ReturnsTrue(string line, int expectedNumber, string expectedText)
    {
        bool result = RecordParser.TryParse(line, out Record record);

        Assert.True(result);
        Assert.Equal(expectedNumber, record.Number);
        Assert.Equal(expectedText, record.Text);
    }

    [Theory]
    [InlineData("123. File.name.txt")]
    [InlineData("123. File-name.txt")]
    [InlineData("123. File_name.txt")]
    [InlineData("123. File\\path\\to\\file.txt")]
    public void TryParse_TextWithSpecialCharacters_ReturnsTrue(string line)
    {
        bool result = RecordParser.TryParse(line, out Record record);

        Assert.True(result);
    }

    [Theory]
    [InlineData("123. Äpple")]
    [InlineData("123. Çherry")]
    [InlineData("123. 测试")]
    public void TryParse_UnicodeCharacters_ReturnsTrue(string line)
    {
        bool result = RecordParser.TryParse(line, out Record record);

        Assert.True(result);
    }

    [Theory]
    [InlineData("123. Text with\nnewline")]
    [InlineData("123. Text with\ttab")]
    public void TryParse_TextWithControlCharacters_ReturnsTrue(string line)
    {
        bool result = RecordParser.TryParse(line, out Record record);

        Assert.True(result);
    }

    [Fact]
    public void TryParse_VeryLongLine_ReturnsTrue()
    {
        string longText = new string('A', 10000);
        string line = $"123. {longText}";

        bool result = RecordParser.TryParse(line, out Record record);

        Assert.True(result);
        Assert.Equal(longText, record.Text);
    }

    [Theory]
    [InlineData("123.   Multiple   Spaces")]
    [InlineData("123. Leading spaces")]
    [InlineData("123. Trailing spaces   ")]
    public void TryParse_TextWithMultipleSpaces_TrimsCorrectly(string line)
    {
        bool result = RecordParser.TryParse(line, out Record record);

        Assert.True(result);
        // Text should be trimmed
        Assert.DoesNotContain("  ", record.Text);
    }

    [Theory]
    [InlineData("123.")]
    [InlineData("123. ")]
    [InlineData("123.\t")]
    public void TryParse_NoTextAfterPeriod_ReturnsFalse(string line)
    {
        bool result = RecordParser.TryParse(line, out Record record);

        Assert.False(result);
    }

    [Theory]
    [InlineData(".Text")]
    [InlineData(" .Text")]
    public void TryParse_NoNumberBeforePeriod_ReturnsFalse(string line)
    {
        bool result = RecordParser.TryParse(line, out Record record);

        Assert.False(result);
    }

    [Theory]
    [InlineData("123.. Double period")]
    [InlineData("123. Text. with. periods")]
    public void TryParse_MultiplePeriods_HandlesCorrectly(string line)
    {
        bool result = RecordParser.TryParse(line, out Record record);

        Assert.True(result);
        // Should parse up to first period as number, rest as text
    }

    [Theory]
    [InlineData("-1. Test")]
    [InlineData("-100. Test")]
    public void TryParse_NegativeNumbers_ReturnsFalse(string line)
    {
        bool result = RecordParser.TryParse(line, out Record record);

        Assert.False(result);
    }

    [Theory]
    [InlineData("abc. Test")]
    [InlineData("12a. Test")]
    [InlineData("a12. Test")]
    public void TryParse_NonNumericPrefix_ReturnsFalse(string line)
    {
        bool result = RecordParser.TryParse(line, out Record record);

        Assert.False(result);
    }

    [Fact]
    public void TryParse_WhitespaceOnlyLine_ReturnsFalse()
    {
        bool result = RecordParser.TryParse("   ", out Record record);

        Assert.False(result);
    }

    [Fact]
    public void TryParse_OnlyPeriod_ReturnsFalse()
    {
        bool result = RecordParser.TryParse(".", out Record record);

        Assert.False(result);
    }

    [Fact]
    public void TryParse_OnlyNumber_ReturnsFalse()
    {
        bool result = RecordParser.TryParse("123", out Record record);

        Assert.False(result);
    }

    [Fact]
    public void TryParse_NumberWithDecimal_ReturnsFalse()
    {
        bool result = RecordParser.TryParse("123.5. Test", out Record record);

        // This should fail because "123.5" is not a valid integer
        Assert.False(result);
    }
    
    [Fact]
    public void TryParse_NumberWithCommaDecimal_ReturnsFalse()
    {
        bool result = RecordParser.TryParse("123,5. Test", out Record record);

        // This should fail because "123.5" is not a valid integer
        Assert.False(result);
    }

    [Theory]
    [InlineData("  123.  Test  ", 123, "Test")]
    [InlineData("\t123.\tTest\t", 123, "Test")]
    public void TryParse_WhitespaceAroundEverything_TrimsCorrectly(string line, int expectedNumber, string expectedText)
    {
        bool result = RecordParser.TryParse(line, out Record record);

        Assert.True(result);
        Assert.Equal(expectedNumber, record.Number);
        Assert.Equal(expectedText, record.Text);
    }

    [Fact]
    public void TryParse_TextWithQuotes_HandlesCorrectly()
    {
        bool result = RecordParser.TryParse("123. \"Quoted text\"", out Record record);

        Assert.True(result);
        Assert.Equal("\"Quoted text\"", record.Text);
    }

    [Fact]
    public void TryParse_TextWithBackslashes_HandlesCorrectly()
    {
        bool result = RecordParser.TryParse("123. Path\\to\\file", out Record record);

        Assert.True(result);
        Assert.Equal("Path\\to\\file", record.Text);
    }
}
