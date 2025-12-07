using FileSort.Core.Interfaces;
using FileSort.Core.Models;
using FileSort.Progress.Helpers;
using FileSort.Progress.Interfaces;
using FileSort.Sorter.Formatters;
using FileSort.Sorter.Helpers;
using FileSort.Sorter.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FileSort.Sorter;

public static class DependencyInjection
{
    public static IServiceCollection AddFileSortSorter(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services
            .AddOptions<SortOptions>()
            .Bind(configuration.GetSection(SortOptions.SectionName));

        services.AddSingleton<IExternalSorter, ExternalFileSorter>();

        services.AddSingleton<IProgressReporterFactory<SortProgress>>(_ =>
            new ProgressReporterFactoryService<SortProgress>(SortProgressFormatter.Format));

        return services;
    }
}
