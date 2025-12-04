using FileSort.Core.Interfaces;
using FileSort.Core.Options;
using FileSort.Core.Validation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FileSort.Generator;

public static class DependencyInjection
{
    public static IServiceCollection AddFileSortGenerator(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Bind configuration (validation happens on access, not on start)
        services
            .AddOptions<GeneratorOptions>()
            .Bind(configuration.GetSection(GeneratorOptions.SectionName));

        // Register validator
        services.AddSingleton<GeneratorOptionsValidator>();

        // Register services
        services.AddSingleton<ITestFileGenerator, TestFileGenerator>();

        return services;
    }
}
