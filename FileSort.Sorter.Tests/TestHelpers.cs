using FileSort.Core.Models;
using FileSort.Core.Parsing;

namespace FileSort.Sorter.Tests;

internal static class TestHelpers
{
    public static async Task<string> CreateTestFileAsync(IEnumerable<string> lines)
    {
        string path = Path.GetTempFileName();
        await File.WriteAllLinesAsync(path, lines);
        return path;
    }

    public static async Task<List<Record>> ReadRecordsFromFileAsync(string filePath)
    {
        var records = new List<Record>();
        string[] lines = await File.ReadAllLinesAsync(filePath);
        
        foreach (string line in lines)
        {
            if (RecordParser.TryParse(line, out Record record))
            {
                records.Add(record);
            }
        }

        return records;
    }

    public static bool IsSorted(IReadOnlyList<Record> records)
    {
        if (records.Count <= 1)
            return true;

        for (int i = 1; i < records.Count; i++)
        {
            var prev = records[i - 1];
            var curr = records[i];

            int textCompare = string.Compare(prev.Text, curr.Text, StringComparison.Ordinal);
            if (textCompare > 0)
                return false;
            if (textCompare == 0 && prev.Number > curr.Number)
                return false;
        }

        return true;
    }

    public static bool FilesHaveSameContent(string file1, string file2)
    {
        string[] lines1 = File.ReadAllLines(file1);
        string[] lines2 = File.ReadAllLines(file2);

        if (lines1.Length != lines2.Length)
            return false;

        for (int i = 0; i < lines1.Length; i++)
        {
            if (lines1[i] != lines2[i])
                return false;
        }

        return true;
    }
}
