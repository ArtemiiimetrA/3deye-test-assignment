using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using FileSort.Core.Parsing;

namespace FileSort.Benchmarks;

[SimpleJob(RuntimeMoniker.Net90)]
[MemoryDiagnoser]
public class RecordParserBenchmarks
{
    private readonly string[] _testLines;
    private List<string> _largeBatch = null!;

    public RecordParserBenchmarks()
    {
        _testLines = new[]
        {
            "415. Apple",
            "30432. Something something something",
            "1. Apple",
            "32. Cherry is the best",
            "2. Banana is yellow",
            "123456. Very long text with many words that should be parsed correctly",
            "0. Single word",
            "999999. Text with special characters !@#$%^&*()",
        };
    }

    [GlobalSetup]
    public void Setup()
    {
        // Generate large batch of test lines for realistic parsing benchmarks
        var random = new Random(42);
        _largeBatch = new List<string>(100000);
        
        for (var i = 0; i < 100000; i++)
        {
            var number = random.Next(1, 1000000);
            var text = $"Text{random.Next(1, 100)}";
            _largeBatch.Add($"{number}. {text}");
        }
    }

    [Benchmark]
    [ArgumentsSource(nameof(GetTestLines))]
    public bool ParseLine(string line)
    {
        RecordParser.TryParse(line, out var record);
        return true;
    }

    public IEnumerable<string> GetTestLines()
    {
        foreach (var line in _testLines)
            yield return line;
    }

    [Benchmark]
    public void ParseMultipleLines()
    {
        foreach (var line in _testLines)
            RecordParser.TryParse(line, out _);
    }

    [Benchmark]
    public void ParseLargeBatch()
    {
        foreach (var line in _largeBatch)
            RecordParser.TryParse(line, out _);
    }

    [Benchmark]
    [Arguments(10000)]
    [Arguments(100000)]
    public void ParseBatchOfSize(int count)
    {
        for (var i = 0; i < count && i < _largeBatch.Count; i++)
            RecordParser.TryParse(_largeBatch[i], out _);
    }
}

