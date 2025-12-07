using System.Text;
using FileSort.Core.Comparison;
using FileSort.Core.Interfaces;
using FileSort.Core.Models;
using FileSort.Core.Parsing;

namespace FileSort.Validator;

/// <summary>
///     Validates that a file is properly sorted according to the Record comparison rules.
///     Uses streaming to handle files of any size without loading them into memory.
///     Exits immediately on the first comparison error for performance.
/// </summary>
public class FileValidator : IFileValidator
{
    private const int DefaultBufferSize = 8192;
    private static readonly RecordComparer Comparer = RecordComparer.Instance;

    // <inheritdoc />
    public async Task<ValidationResult> ValidateAsync(string filePath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("File path cannot be null or empty", nameof(filePath));

        if (!File.Exists(filePath))
            throw new FileNotFoundException($"File not found: {filePath}", filePath);

        var errors = new List<ValidationError>();
        long totalRecords = 0;
        long invalidRecords = 0;
        Record? previousRecord = null;
        long lineNumber = 0;

        await using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read,
            DefaultBufferSize, true);
        using var reader = new StreamReader(fileStream, Encoding.UTF8, true, DefaultBufferSize);

        string? previousLine = null;
        string? line;

        while ((line = await reader.ReadLineAsync(cancellationToken)) != null)
        {
            lineNumber++;
            cancellationToken.ThrowIfCancellationRequested();

            // Try to parse the line
            if (!RecordParser.TryParse(line, out var currentRecord))
            {
                invalidRecords++;
                errors.Add(new ValidationError(
                    lineNumber,
                    line,
                    "Invalid record format. Expected format: '{Number}. {Text}'"
                ));
                // Exit on first parse error
                break;
            }

            totalRecords++;

            // Compare with previous record if available
            if (previousRecord.HasValue)
            {
                var comparison = Comparer.Compare(previousRecord.Value, currentRecord);
                if (comparison > 0)
                {
                    invalidRecords++;
                    errors.Add(new ValidationError(
                        lineNumber,
                        line,
                        $"Record is out of order. Previous record: '{previousLine}'"
                    ));
                    // Exit immediately on first comparison error
                    break;
                }
            }

            previousRecord = currentRecord;
            previousLine = line;
        }

        var isValid = invalidRecords == 0;
        return new ValidationResult(isValid, totalRecords, invalidRecords, errors);
    }
}