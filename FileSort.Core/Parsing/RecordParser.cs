using FileSort.Core.Models;

namespace FileSort.Core.Parsing;

/// <summary>
/// Parses lines in format: "{Number}. {Text}"
/// Uses Span<char> for zero-allocation parsing where possible.
/// </summary>
public static class RecordParser
{
    /// <summary>
    /// Attempts to parse a line into a Record.
    /// </summary>
    /// <param name="line">The line to parse</param>
    /// <param name="record">The parsed record if successful</param>
    /// <returns>True if parsing succeeded, false otherwise</returns>
    public static bool TryParse(string? line, out Record record)
    {
        record = default;

        if (string.IsNullOrWhiteSpace(line))
            return false;

        ReadOnlySpan<char> span = line.AsSpan().Trim();

        // Find the period separator
        int periodIndex = span.IndexOf('.');
        if (periodIndex <= 0 || periodIndex >= span.Length - 1)
            return false;

        // Parse number (before period)
        ReadOnlySpan<char> numberSpan = span[..periodIndex].Trim();
        if (numberSpan.IsEmpty)
            return false;

        if (!int.TryParse(numberSpan, out int number) || number < 0)
            return false;

        // Extract text (after period)
        ReadOnlySpan<char> textSpan = span[(periodIndex + 1)..].Trim();
        if (textSpan.IsEmpty)
            return false;

        string text = textSpan.ToString(); // Convert to string (needed for Record)
        record = new Record(number, text);
        return true;
    }
}
