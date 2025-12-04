namespace FileSort.Core.Options;

public sealed class GeneratorOptions
{
    public const string SectionName = "GeneratorOptions";
    
    public string OutputFilePath { get; set; } = "input.txt";
    public long TargetSizeBytes { get; set; } = 10737418240; // 10 GB default
    public int MinNumber { get; set; } = 1;
    public int MaxNumber { get; set; } = 1000000;
    public int DuplicateRatioPercent { get; set; } = 20;
    public int BufferSizeBytes { get; set; } = 4 * 1024 * 1024; // 4MB default (optimized)
    public int Seed { get; set; } = 0; // 0 = random
}
