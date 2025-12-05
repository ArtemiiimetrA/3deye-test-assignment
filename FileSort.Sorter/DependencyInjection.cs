using FileSort.Core.Interfaces;
using FileSort.Sorter.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

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

        return services;
    }
}
