using FileSort.Core.Models;
using FileSort.Core.Requests;

namespace FileSort.Core.Interfaces;

public interface ITestFileGenerator
{
    Task GenerateAsync(GeneratorRequest request, IProgress<GeneratorProgress>? progress = null, CancellationToken cancellationToken = default);
}
