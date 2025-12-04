using FileSort.Core.Models;
using System.Collections;

namespace FileSort.Core.Comparison;

/// <summary>
/// Comparer for Record objects.
/// Primary sort: Text (ordinal, case-sensitive)
/// Secondary sort: Number (ascending)
/// </summary>
public sealed class RecordComparer : IComparer<Record>
{
    public static readonly RecordComparer Instance = new();

    private RecordComparer() { }

    public int Compare(Record x, Record y)
    {
        // Primary: Text comparison (ordinal, case-sensitive)
        int textComparison = string.Compare(x.Text, y.Text, StringComparison.Ordinal);
        if (textComparison != 0)
            return textComparison;

        // Secondary: Number comparison (ascending)
        return x.Number.CompareTo(y.Number);
    }
}
