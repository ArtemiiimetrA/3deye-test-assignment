namespace FileSort.Core.Models;

/// <summary>
///     Record with source file index for k-way merge tracking.
/// </summary>
public readonly struct RecordWithSource
{
    public Record Record { get; init; }
    public int SourceFileIndex { get; init; }

    public RecordWithSource(Record record, int sourceFileIndex)
    {
        Record = record;
        SourceFileIndex = sourceFileIndex;
    }

    public void Deconstruct(out Record record, out int sourceFileIndex)
    {
        record = Record;
        sourceFileIndex = SourceFileIndex;
    }
}