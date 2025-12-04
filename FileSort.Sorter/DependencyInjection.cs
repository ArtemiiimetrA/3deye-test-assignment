using FileSort.Core.Interfaces;
using FileSort.Core.Options;
using FileSort.Core.Validation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FileSort.Sorter;

public static class DependencyInjection
{
    public static IServiceCollection AddFileSortSorter(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Bind configuration (validation happens on access, not on start)
        services
            .AddOptions<SortOptions>()
            .Bind(configuration.GetSection(SortOptions.SectionName));

        // Register validator
        services.AddSingleton<SortOptionsValidator>();

        // Register services
        services.AddSingleton<IExternalSorter, ExternalFileSorter>();

        return services;
    }
}
