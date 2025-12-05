using FileSort.Core.Interfaces;
using FileSort.Generator.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FileSort.Generator;

public static class DependencyInjection
{
    public static IServiceCollection AddFileSortGenerator(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services
            .AddOptions<GeneratorOptions>()
            .Bind(configuration.GetSection(GeneratorOptions.SectionName));
        services.AddSingleton<ITestFileGenerator, TestFileGenerator>();

        return services;
    }
}
