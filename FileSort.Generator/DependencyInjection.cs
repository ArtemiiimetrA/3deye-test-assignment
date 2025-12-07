using FileSort.Core.Interfaces;
using FileSort.Core.Models.Progress;
using FileSort.Generator.Formatters;
using FileSort.Generator.Options;
using FileSort.Progress.Helpers;
using FileSort.Progress.Interfaces;
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

        services.AddSingleton<IProgressReporterFactory<GeneratorProgress>>(_ =>
            new ProgressReporterFactoryService<GeneratorProgress>(GeneratorProgressFormatter.Format));

        return services;
    }
}