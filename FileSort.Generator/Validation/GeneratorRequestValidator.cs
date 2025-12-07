using FileSort.Core.Requests;

namespace FileSort.Generator.Validation;

/// <summary>
/// Validates SortRequest parameters before processing.
/// </summary>
internal static class GeneratorRequestValidator
{
    public static void Validate(GeneratorRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.OutputFilePath))
            throw new ArgumentException("OutputFilePath is required.", nameof(request));

        if (request.TargetSizeBytes <= 0)
            throw new ArgumentException("TargetSizeBytes must be greater than 0.", nameof(request));

        if (request.MinNumber < 0)
            throw new ArgumentException("MinNumber must be non-negative.", nameof(request));

        if (request.MaxNumber < request.MinNumber)
            throw new ArgumentException("MaxNumber must be greater than or equal to MinNumber.", nameof(request));

        if (request.DuplicateRatioPercent < 0 || request.DuplicateRatioPercent > 100)
            throw new ArgumentException("DuplicateRatioPercent must be between 0 and 100.", nameof(request));

        if (request.BufferSizeBytes <= 0)
            throw new ArgumentException("BufferSizeBytes must be greater than 0.", nameof(request));
    }
}

