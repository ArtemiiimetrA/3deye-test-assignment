namespace FileSort.Core.Interfaces;

/// <summary>
/// Validates that a file is properly sorted according to the Record comparison rules.
/// Primary sort: Text (ordinal, case-sensitive)
/// Secondary sort: Number (ascending)
/// </summary>
public interface IFileValidator
{
    /// <summary>
    /// Validates that the specified file is properly sorted.
    /// </summary>
    /// <param name="filePath">The path to the file to validate</param>
    /// <param name="cancellationToken">Cancellation token to cancel the validation operation</param>
    /// <returns>A validation result indicating whether the file is sorted and any errors found</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="filePath"/> is null or empty</exception>
    /// <exception cref="FileNotFoundException">Thrown when the file specified in <paramref name="filePath"/> does not exist</exception>
    Task<ValidationResult> ValidateAsync(string filePath, CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents the result of file validation.
/// </summary>
public class ValidationResult
{
    public bool IsValid { get; init; }
    public long TotalRecords { get; init; }
    public long InvalidRecords { get; init; }
    public List<ValidationError> Errors { get; init; } = new();

    public ValidationResult(bool isValid, long totalRecords, long invalidRecords, List<ValidationError>? errors = null)
    {
        IsValid = isValid;
        TotalRecords = totalRecords;
        InvalidRecords = invalidRecords;
        Errors = errors ?? new List<ValidationError>();
    }
}

/// <summary>
/// Represents a validation error found during file validation.
/// </summary>
public class ValidationError
{
    public long LineNumber { get; init; }
    public string Line { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;

    public ValidationError(long lineNumber, string line, string message)
    {
        LineNumber = lineNumber;
        Line = line;
        Message = message;
    }
}

