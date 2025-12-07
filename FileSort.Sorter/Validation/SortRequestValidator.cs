using FileSort.Core.Requests;

namespace FileSort.Sorter.Validation;

/// <summary>
///     Validates SortRequest parameters before processing.
/// </summary>
internal static class SortRequestValidator
{
    public static void Validate(SortRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.InputFilePath))
            throw new ArgumentException("InputFilePath is required.", nameof(request));

        if (!File.Exists(request.InputFilePath))
            throw new FileNotFoundException($"Input file not found: {request.InputFilePath}");

        if (string.IsNullOrWhiteSpace(request.OutputFilePath))
            throw new ArgumentException("OutputFilePath is required.", nameof(request));

        if (request.ChunkSizeMb <= 0)
            throw new ArgumentException("ChunkSizeMb must be greater than 0.", nameof(request));

        if (request.ChunkSizeMb > request.MaxRamMb)
            throw new ArgumentException("ChunkSizeMb cannot exceed MaxRamMb.", nameof(request));

        if (request.MaxRamMb <= 0)
            throw new ArgumentException("MaxRamMb must be greater than 0.", nameof(request));

        if (request.MaxDegreeOfParallelism <= 0)
            throw new ArgumentException("MaxDegreeOfParallelism must be greater than 0.", nameof(request));

        if (request.BufferSizeBytes <= 0)
            throw new ArgumentException("BufferSizeBytes must be greater than 0.", nameof(request));

        if (request.MaxOpenFiles < 2)
            throw new ArgumentException("MaxOpenFiles must be at least 2.", nameof(request));

        if (request.MaxMergeParallelism <= 0)
            throw new ArgumentException("MaxMergeParallelism must be greater than 0.", nameof(request));

        if (request.MinChunkSizeMb > request.MaxChunkSizeMb)
            throw new ArgumentException("MinChunkSizeMb cannot exceed MaxChunkSizeMb.", nameof(request));
    }
}