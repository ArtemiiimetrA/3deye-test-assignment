namespace FileSort.Core.Models;

/// <summary>
/// Represents a parsed record from the input file.
/// </summary>
public readonly struct Record : IEquatable<Record>
{
    public int Number { get; init; }
    public string Text { get; init; }

    public Record(int number, string text)
    {
        Number = number;
        Text = text ?? throw new ArgumentNullException(nameof(text));
    }

    /// <summary>
    /// Reconstructs the original line format: "{Number}. {Text}"
    /// </summary>
    public string ToLine() => $"{Number}. {Text}";

    public bool Equals(Record other) => Number == other.Number && Text == other.Text;

    public override bool Equals(object? obj) => obj is Record other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(Number, Text);
}
