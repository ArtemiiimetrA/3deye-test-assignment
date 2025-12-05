using FileSort.Core.Interfaces;
using FileSort.Core.Models;
using FileSort.Core.Requests;

namespace FileSort.Generator;

public sealed class TestFileGenerator : ITestFileGenerator
{
    public async Task GenerateAsync(
        GeneratorRequest request,
        IProgress<GeneratorProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ValidateRequest(request);

        // Ensure output directory exists
        string? directory = Path.GetDirectoryName(request.OutputFilePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var textPool = new TextPool(request.DuplicateRatioPercent, request.Seed);
        var random = request.Seed.HasValue ? new Random(request.Seed.Value) : new Random();

        long bytesWritten = 0;
        long linesWritten = 0;

        await using var fileStream = new FileStream(
            request.OutputFilePath,
            FileMode.Create,
            FileAccess.Write,
            FileShare.None,
            request.BufferSizeBytes,
            FileOptions.SequentialScan | FileOptions.Asynchronous);

        await using var writer = new StreamWriter(fileStream, System.Text.Encoding.UTF8, request.BufferSizeBytes);

        // Write buffer for batching writes
        var writeBuffer = new List<string>(capacity: 10000);

        while (bytesWritten < request.TargetSizeBytes && !cancellationToken.IsCancellationRequested)
        {
            int number = random.Next(request.MinNumber, request.MaxNumber + 1);
            string text = textPool.GetNextText();
            string line = $"{number}. {text}";

            writeBuffer.Add(line);

            // Estimate bytes (UTF-8 encoding, approximate)
            long estimatedBytes = System.Text.Encoding.UTF8.GetByteCount(line) + 2; // +2 for line ending

            // Flush buffer when it's large enough or when approaching target size
            if (writeBuffer.Count >= 10000 || bytesWritten + estimatedBytes >= request.TargetSizeBytes)
            {
                foreach (string bufferedLine in writeBuffer)
                {
                    await writer.WriteLineAsync(bufferedLine);
                    long lineBytes = System.Text.Encoding.UTF8.GetByteCount(bufferedLine) + 2;
                    bytesWritten += lineBytes;
                    linesWritten++;
                }
                writeBuffer.Clear();

                // Report progress
                progress?.Report(new GeneratorProgress
                {
                    BytesWritten = bytesWritten,
                    TargetBytes = request.TargetSizeBytes,
                    LinesWritten = linesWritten
                });

                // Check if we've reached target (with small tolerance)
                if (bytesWritten >= request.TargetSizeBytes * 0.99) // 99% of target
                    break;
            }
        }

        // Flush any remaining lines
        if (writeBuffer.Count > 0)
        {
            foreach (string bufferedLine in writeBuffer)
            {
                await writer.WriteLineAsync(bufferedLine);
                long lineBytes = System.Text.Encoding.UTF8.GetByteCount(bufferedLine) + 2;
                bytesWritten += lineBytes;
                linesWritten++;
            }
        }

        await writer.FlushAsync();
    }

    private static void ValidateRequest(GeneratorRequest request)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

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
