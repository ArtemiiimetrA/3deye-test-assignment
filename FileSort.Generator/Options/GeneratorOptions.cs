namespace FileSort.Generator.Options;

public sealed class GeneratorOptions
{
    public const string SectionName = "GeneratorOptions";

    public GeneratorOptions()
    {
    }

    public GeneratorOptions(
        string? outputFilePath,
        long targetSizeBytes,
        int minNumber,
        int maxNumber,
        int duplicateRatioPercent,
        int bufferSizeBytes,
        int? seed,
        int maxWordsPerString)
    {
        OutputFilePath = outputFilePath;
        TargetSizeBytes = targetSizeBytes;
        MinNumber = minNumber;
        MaxNumber = maxNumber;
        DuplicateRatioPercent = duplicateRatioPercent;
        BufferSizeBytes = bufferSizeBytes;
        Seed = seed;
        MaxWordsPerString = maxWordsPerString;
    }

    public string? OutputFilePath { get; init; }
    public long TargetSizeBytes { get; init; }
    public int MinNumber { get; init; }
    public int MaxNumber { get; init; }
    public int DuplicateRatioPercent { get; init; }
    public int BufferSizeBytes { get; init; }
    public int? Seed { get; init; }
    public int MaxWordsPerString { get; init; }
}