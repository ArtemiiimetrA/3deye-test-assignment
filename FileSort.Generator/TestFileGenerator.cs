using FileSort.Core.Interfaces;
using FileSort.Core.Models;
using FileSort.Core.Requests;
using System.Text;
using FileSort.Core.Models.Progress;
using FileSort.Generator.Validation;

namespace FileSort.Generator;

// <inheritdoc />
public sealed class TestFileGenerator : ITestFileGenerator
{
    private const int WriteBufferCapacity = 10000;
    private const double TargetSizeTolerance = 0.99;
    private static readonly Encoding FileEncoding = Encoding.UTF8;

    // <inheritdoc />
    public async Task GenerateAsync(
        GeneratorRequest request,
        IProgress<GeneratorProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        GeneratorRequestValidator.Validate(request);
        EnsureOutputDirectoryExists(request.OutputFilePath);

        var textPool = CreateTextPool(request);
        var random = CreateRandom(request.Seed);

        await using var writer = CreateFileWriter(request.OutputFilePath, request.BufferSizeBytes);

        var writeBuffer = new List<string>(capacity: WriteBufferCapacity);
        long bytesWritten = 0;
        long linesWritten = 0;

        (bytesWritten, linesWritten) = await GenerateLinesLoopAsync(
            request,
            textPool,
            random,
            writer,
            writeBuffer,
            bytesWritten,
            linesWritten,
            progress,
            cancellationToken);

        (bytesWritten, linesWritten) = await FlushRemainingLinesAsync(
            writeBuffer,
            writer,
            bytesWritten,
            linesWritten,
            cancellationToken);

        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            await writer.FlushAsync(cancellationToken);
        }
        catch (TaskCanceledException)
        {
            cancellationToken.ThrowIfCancellationRequested();
            throw new OperationCanceledException("Operation was cancelled.", cancellationToken);
        }
    }

    private static void EnsureOutputDirectoryExists(string filePath)
    {
        string? directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }

    private static TextPool.TextPool CreateTextPool(GeneratorRequest request)
    {
        return new TextPool.TextPool(
            request.DuplicateRatioPercent,
            request.MaxWordsPerString,
            request.Seed);
    }

    private static Random CreateRandom(int? seed)
    {
        return seed.HasValue ? new Random(seed.Value) : new Random();
    }

    private static StreamWriter CreateFileWriter(string filePath, int bufferSize)
    {
        var fileStream = new FileStream(
            filePath,
            FileMode.Create,
            FileAccess.Write,
            FileShare.None,
            bufferSize,
            FileOptions.SequentialScan | FileOptions.Asynchronous);

        return new StreamWriter(fileStream, FileEncoding, bufferSize);
    }

    private static string GenerateLine(Random random, TextPool.TextPool textPool, int minNumber, int maxNumber)
    {
        int number = random.Next(minNumber, maxNumber + 1);
        string text = textPool.GetNextText();
        return $"{number}. {text}";
    }

    private static long CalculateLineBytes(string line)
    {
        return FileEncoding.GetByteCount(line) + FileEncoding.GetByteCount(Environment.NewLine);
    }

    private static bool ShouldFlushBuffer(
        List<string> writeBuffer,
        long currentBytes,
        long estimatedBytes,
        long targetBytes)
    {
        return writeBuffer.Count >= WriteBufferCapacity
            || currentBytes + estimatedBytes >= targetBytes;
    }

    private static bool HasReachedTarget(long bytesWritten, long targetBytes)
    {
        return bytesWritten >= targetBytes * TargetSizeTolerance;
    }

    private static async Task<(long bytesWritten, long linesWritten)> GenerateLinesLoopAsync(
        GeneratorRequest request,
        TextPool.TextPool textPool,
        Random random,
        StreamWriter writer,
        List<string> writeBuffer,
        long bytesWritten,
        long linesWritten,
        IProgress<GeneratorProgress>? progress,
        CancellationToken cancellationToken)
    {
        while (bytesWritten < request.TargetSizeBytes && !cancellationToken.IsCancellationRequested)
        {
            string line = GenerateLine(random, textPool, request.MinNumber, request.MaxNumber);
            writeBuffer.Add(line);

            long estimatedBytes = CalculateLineBytes(line);

            if (ShouldFlushBuffer(writeBuffer, bytesWritten, estimatedBytes, request.TargetSizeBytes))
            {
                (bytesWritten, linesWritten) = await FlushWriteBufferAsync(
                    writeBuffer,
                    writer,
                    bytesWritten,
                    linesWritten,
                    cancellationToken);

                ReportProgress(progress, bytesWritten, request.TargetSizeBytes, linesWritten);

                if (HasReachedTarget(bytesWritten, request.TargetSizeBytes))
                    break;
            }
        }

        return (bytesWritten, linesWritten);
    }

    private static async Task<(long bytesWritten, long linesWritten)> FlushWriteBufferAsync(
        List<string> writeBuffer,
        StreamWriter writer,
        long bytesWritten,
        long linesWritten,
        CancellationToken cancellationToken)
    {
        foreach (string line in writeBuffer)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                await writer.WriteLineAsync(line);
            }
            catch (TaskCanceledException)
            {
                cancellationToken.ThrowIfCancellationRequested();
                throw new OperationCanceledException("Operation was cancelled.", cancellationToken);
            }
            
            long lineBytes = CalculateLineBytes(line);
            bytesWritten += lineBytes;
            linesWritten++;
        }
        writeBuffer.Clear();
        
        return (bytesWritten, linesWritten);
    }

    private static async Task<(long bytesWritten, long linesWritten)> FlushRemainingLinesAsync(
        List<string> writeBuffer,
        StreamWriter writer,
        long bytesWritten,
        long linesWritten,
        CancellationToken cancellationToken)
    {
        if (writeBuffer.Count > 0)
        {
            (bytesWritten, linesWritten) = await FlushWriteBufferAsync(
                writeBuffer,
                writer,
                bytesWritten,
                linesWritten,
                cancellationToken);
        }
        
        return (bytesWritten, linesWritten);
    }

    private static void ReportProgress(
        IProgress<GeneratorProgress>? progress,
        long bytesWritten,
        long targetBytes,
        long linesWritten)
    {
        progress?.Report(new GeneratorProgress
        {
            BytesWritten = bytesWritten,
            TargetBytes = targetBytes,
            LinesWritten = linesWritten
        });
    }
}
