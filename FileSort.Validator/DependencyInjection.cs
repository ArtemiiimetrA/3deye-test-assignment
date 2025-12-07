using FileSort.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace FileSort.Validator;

public static class DependencyInjection
{
    public static IServiceCollection AddFileSortValidator(this IServiceCollection services)
    {
        services.AddSingleton<IFileValidator, FileValidator>();
        return services;
    }
}