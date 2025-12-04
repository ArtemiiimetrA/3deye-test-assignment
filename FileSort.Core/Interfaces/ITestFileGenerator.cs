using FileSort.Core.Models;
using FileSort.Core.Options;

namespace FileSort.Core.Interfaces;

public interface ITestFileGenerator
{
    Task GenerateAsync(GeneratorOptions options, IProgress<GeneratorProgress>? progress = null, CancellationToken cancellationToken = default);
}
