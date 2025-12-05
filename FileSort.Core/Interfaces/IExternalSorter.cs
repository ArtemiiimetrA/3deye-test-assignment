using FileSort.Core.Models;
using FileSort.Core.Requests;

namespace FileSort.Core.Interfaces;

public interface IExternalSorter
{
    Task SortAsync(SortRequest request, IProgress<SortProgress>? progress = null, CancellationToken cancellationToken = default);
}
