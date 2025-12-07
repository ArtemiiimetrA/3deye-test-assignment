using FileSort.Core.Models;

namespace FileSort.Core.Parsing;

/// <summary>
///     Parses lines in format: "{Number}. {Text}"
///     Uses Span<char> for zero-allocation parsing where possible.
/// </summary>
public static class RecordParser
{
    /// <summary>
    ///     Attempts to parse a line into a Record.
    /// </summary>
    /// <param name="line">The line to parse</param>
    /// <param name="record">The parsed record if successful</param>
    /// <returns>True if parsing succeeded, false otherwise</returns>
    public static bool TryParse(string? line, out Record record)
    {
        record = default;

        if (string.IsNullOrWhiteSpace(line))
            return false;

        var span = line.AsSpan().Trim();

        // Find the period separator
        // We need to find the period that separates the integer number from the text,
        // not a period that's part of a decimal number (e.g., "123.5. Test" should fail)
        var periodIndex = -1;
        var searchStart = 0;

        while (true)
        {
            var nextPeriod = span[searchStart..].IndexOf('.');
            if (nextPeriod == -1)
                return false; // No period found

            periodIndex = searchStart + nextPeriod;

            // Check if this period is part of a decimal number
            if (periodIndex > 0 && periodIndex < span.Length - 1)
            {
                var beforePeriod = span[..periodIndex].Trim();
                var afterPeriod = span[periodIndex + 1];

                // If the part before period is all digits and the character after is a digit,
                // then this period is part of a decimal number, not the separator
                var isAllDigits = true;
                for (var i = 0; i < beforePeriod.Length; i++)
                    if (!char.IsDigit(beforePeriod[i]))
                    {
                        isAllDigits = false;
                        break;
                    }

                if (isAllDigits && char.IsDigit(afterPeriod))
                {
                    // This is a decimal number, look for the next period
                    searchStart = periodIndex + 1;
                    continue;
                }
            }

            // Found a valid separator period
            break;
        }

        if (periodIndex <= 0 || periodIndex >= span.Length - 1)
            return false;

        // Parse number (before period)
        var numberSpan = span[..periodIndex].Trim();
        if (numberSpan.IsEmpty)
            return false;

        // Ensure the number part contains only digits
        for (var i = 0; i < numberSpan.Length; i++)
            if (!char.IsDigit(numberSpan[i]))
                return false;

        if (!int.TryParse(numberSpan, out var number) || number < 0)
            return false;

        // Extract text (after period)
        var textSpan = span[(periodIndex + 1)..].Trim();
        if (textSpan.IsEmpty)
            return false;

        // Normalize whitespace: collapse multiple spaces to single spaces
        var text = NormalizeWhitespace(textSpan);
        record = new Record(number, text);
        return true;
    }

    /// <summary>
    ///     Normalizes whitespace by collapsing multiple consecutive spaces into single spaces.
    /// </summary>
    private static string NormalizeWhitespace(ReadOnlySpan<char> span)
    {
        if (span.IsEmpty)
            return string.Empty;

        // Count how many characters we'll need (accounting for collapsed spaces)
        var resultLength = 0;
        var previousWasSpace = false;
        for (var i = 0; i < span.Length; i++)
            if (char.IsWhiteSpace(span[i]))
            {
                if (!previousWasSpace)
                {
                    resultLength++;
                    previousWasSpace = true;
                }
            }
            else
            {
                resultLength++;
                previousWasSpace = false;
            }

        // Build the normalized string
        Span<char> result = stackalloc char[resultLength];
        var resultIndex = 0;
        previousWasSpace = false;
        for (var i = 0; i < span.Length; i++)
            if (char.IsWhiteSpace(span[i]))
            {
                if (!previousWasSpace)
                {
                    result[resultIndex++] = ' ';
                    previousWasSpace = true;
                }
            }
            else
            {
                result[resultIndex++] = span[i];
                previousWasSpace = false;
            }

        return result.ToString();
    }
}