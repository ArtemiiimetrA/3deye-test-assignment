using FileSort.Core.Models;
using FileSort.Core.Options;

namespace FileSort.Core.Interfaces;

public interface IExternalSorter
{
    Task SortAsync(SortOptions options, IProgress<SortProgress>? progress = null, CancellationToken cancellationToken = default);
}
