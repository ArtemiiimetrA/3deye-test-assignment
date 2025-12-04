using FileSort.Core.Validation;

namespace FileSort.Core.Options;

public sealed class GeneratorOptions
{
    public const string SectionName = "GeneratorOptions";
    
    public string? OutputFilePath { get; set; }
    public long TargetSizeBytes { get; set; }
    public int MinNumber { get; set; }
    public int MaxNumber { get; set; }
    public int DuplicateRatioPercent { get; set; }
    public int BufferSizeBytes { get; set; }
    public int Seed { get; set; }

    public GeneratorOptions() { }

    public GeneratorOptions(
        string? outputFilePath,
        long targetSizeBytes,
        int minNumber,
        int maxNumber,
        int duplicateRatioPercent,
        int bufferSizeBytes,
        int seed)
    {
        OutputFilePath = outputFilePath;
        TargetSizeBytes = targetSizeBytes;
        MinNumber = minNumber;
        MaxNumber = maxNumber;
        DuplicateRatioPercent = duplicateRatioPercent;
        BufferSizeBytes = bufferSizeBytes;
        Seed = seed;
    }
}
