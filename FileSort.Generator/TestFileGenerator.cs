using FileSort.Core.Interfaces;
using FileSort.Core.Models;
using FileSort.Core.Options;
using FileSort.Core.Validation;

namespace FileSort.Generator;

public sealed class TestFileGenerator : ITestFileGenerator
{
    public async Task GenerateAsync(
        GeneratorOptions options,
        IProgress<GeneratorProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        OptionsValidator.Validate(options);

        // Ensure output directory exists
        string? directory = Path.GetDirectoryName(options.OutputFilePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var textPool = new TextPool(options.DuplicateRatioPercent, options.Seed);
        var random = options.Seed == 0 ? new Random() : new Random(options.Seed);

        long bytesWritten = 0;
        long linesWritten = 0;

        await using var fileStream = new FileStream(
            options.OutputFilePath,
            FileMode.Create,
            FileAccess.Write,
            FileShare.None,
            options.BufferSizeBytes,
            FileOptions.SequentialScan | FileOptions.Asynchronous);

        await using var writer = new StreamWriter(fileStream, System.Text.Encoding.UTF8, options.BufferSizeBytes);

        // Write buffer for batching writes
        var writeBuffer = new List<string>(capacity: 10000);

        while (bytesWritten < options.TargetSizeBytes && !cancellationToken.IsCancellationRequested)
        {
            int number = random.Next(options.MinNumber, options.MaxNumber + 1);
            string text = textPool.GetNextText();
            string line = $"{number}. {text}";

            writeBuffer.Add(line);

            // Estimate bytes (UTF-8 encoding, approximate)
            long estimatedBytes = System.Text.Encoding.UTF8.GetByteCount(line) + 2; // +2 for line ending

            // Flush buffer when it's large enough or when approaching target size
            if (writeBuffer.Count >= 10000 || bytesWritten + estimatedBytes >= options.TargetSizeBytes)
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
                    TargetBytes = options.TargetSizeBytes,
                    LinesWritten = linesWritten
                });

                // Check if we've reached target (with small tolerance)
                if (bytesWritten >= options.TargetSizeBytes * 0.99) // 99% of target
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
}
