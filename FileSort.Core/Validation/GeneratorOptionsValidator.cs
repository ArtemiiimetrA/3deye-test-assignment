using FileSort.Core.Options;
using Microsoft.Extensions.Options;

namespace FileSort.Core.Validation;

/// <summary>
/// Validates GeneratorOptions. Implements IValidateOptions for startup validation
/// and provides static method for runtime validation.
/// </summary>
public sealed class GeneratorOptionsValidator
{
    /// <summary>
    /// Validates options at runtime. Throws exceptions for invalid values.
    /// </summary>
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
