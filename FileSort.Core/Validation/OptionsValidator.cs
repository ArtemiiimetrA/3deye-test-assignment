using FileSort.Core.Options;

namespace FileSort.Core.Validation;

public static class OptionsValidator
{
    public static void Validate(SortOptions options)
    {
        if (options == null)
            throw new ArgumentNullException(nameof(options));

        if (string.IsNullOrWhiteSpace(options.InputFilePath))
            throw new ArgumentException("InputFilePath is required.", nameof(options));

        if (!File.Exists(options.InputFilePath))
            throw new FileNotFoundException($"Input file not found: {options.InputFilePath}");

        if (string.IsNullOrWhiteSpace(options.OutputFilePath))
            throw new ArgumentException("OutputFilePath is required.", nameof(options));

        if (options.ChunkSizeMb <= 0)
            throw new ArgumentException("ChunkSizeMb must be greater than 0.", nameof(options));

        if (options.ChunkSizeMb > options.MaxRamMb)
            throw new ArgumentException("ChunkSizeMb cannot exceed MaxRamMb.", nameof(options));

        if (options.MaxRamMb <= 0)
            throw new ArgumentException("MaxRamMb must be greater than 0.", nameof(options));

        if (options.MaxDegreeOfParallelism <= 0)
            throw new ArgumentException("MaxDegreeOfParallelism must be greater than 0.", nameof(options));

        if (options.BufferSizeBytes <= 0)
            throw new ArgumentException("BufferSizeBytes must be greater than 0.", nameof(options));

        if (options.MaxOpenFiles < 2)
            throw new ArgumentException("MaxOpenFiles must be at least 2.", nameof(options));

        if (options.MinChunkSizeMb > options.MaxChunkSizeMb)
            throw new ArgumentException("MinChunkSizeMb cannot exceed MaxChunkSizeMb.", nameof(options));
    }

    public static void Validate(GeneratorOptions options)
    {
        if (options == null)
            throw new ArgumentNullException(nameof(options));

        if (string.IsNullOrWhiteSpace(options.OutputFilePath))
            throw new ArgumentException("OutputFilePath is required.", nameof(options));

        if (options.TargetSizeBytes <= 0)
            throw new ArgumentException("TargetSizeBytes must be greater than 0.", nameof(options));

        if (options.MinNumber < 0)
            throw new ArgumentException("MinNumber must be non-negative.", nameof(options));

        if (options.MaxNumber < options.MinNumber)
            throw new ArgumentException("MaxNumber must be greater than or equal to MinNumber.", nameof(options));

        if (options.DuplicateRatioPercent < 0 || options.DuplicateRatioPercent > 100)
            throw new ArgumentException("DuplicateRatioPercent must be between 0 and 100.", nameof(options));

        if (options.BufferSizeBytes <= 0)
            throw new ArgumentException("BufferSizeBytes must be greater than 0.", nameof(options));
    }
}
