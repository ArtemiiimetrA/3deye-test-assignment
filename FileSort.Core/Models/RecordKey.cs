namespace FileSort.Core.Models;

/// <summary>
/// Key for priority queue comparison in k-way merge.
/// Implements comparison logic: Text (ordinal) then Number (ascending).
/// </summary>
public readonly struct RecordKey : IComparable<RecordKey>
{
    public string Text { get; init; }
    public int Number { get; init; }

    public RecordKey(string text, int number)
    {
        Text = text ?? throw new ArgumentNullException(nameof(text));
        Number = number;
    }

    public int CompareTo(RecordKey other)
    {
        // Primary: Text comparison (ordinal, case-sensitive)
        int textComparison = string.Compare(Text, other.Text, StringComparison.Ordinal);
        if (textComparison != 0)
            return textComparison;

        // Secondary: Number comparison (ascending)
        return Number.CompareTo(other.Number);
    }
}
