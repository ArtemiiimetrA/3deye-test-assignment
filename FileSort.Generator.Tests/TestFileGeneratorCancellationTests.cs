using FileSort.Core.Interfaces;
using FileSort.Core.Requests;
using Xunit;

namespace FileSort.Generator.Tests;

public class TestFileGeneratorCancellationTests
{
    private readonly ITestFileGenerator _generator = new TestFileGenerator();

    [Fact]
    public async Task GenerateAsync_Cancellation_ThrowsOperationCanceledException()
    {
        var outputPath = Path.GetTempFileName();
        try
        {
            var request = new GeneratorRequest
            {
                OutputFilePath = outputPath,
                TargetSizeBytes = 100 * 1024 * 1024, // 100MB - large enough to allow cancellation
                MinNumber = 1,
                MaxNumber = 1000000,
                DuplicateRatioPercent = 20,
                BufferSizeBytes = 4 * 1024 * 1024,
                Seed = 42
            };

            using var cts = new CancellationTokenSource();
            cts.CancelAfter(10); // Cancel after 10ms

            await Assert.ThrowsAsync<OperationCanceledException>(
                () => _generator.GenerateAsync(request, cancellationToken: cts.Token));
        }
        finally
        {
            if (File.Exists(outputPath))
                File.Delete(outputPath);
        }
    }

    [Fact]
    public async Task GenerateAsync_AlreadyCancelled_ThrowsImmediately()
    {
        var outputPath = Path.GetTempFileName();
        try
        {
            var request = new GeneratorRequest
            {
                OutputFilePath = outputPath,
                TargetSizeBytes = 1024,
                MinNumber = 1,
                MaxNumber = 100,
                DuplicateRatioPercent = 20,
                BufferSizeBytes = 4 * 1024 * 1024,
                Seed = 42
            };

            using var cts = new CancellationTokenSource();
            cts.Cancel();

            await Assert.ThrowsAsync<OperationCanceledException>(
                () => _generator.GenerateAsync(request, cancellationToken: cts.Token));
        }
        finally
        {
            if (File.Exists(outputPath))
                File.Delete(outputPath);
        }
    }
}