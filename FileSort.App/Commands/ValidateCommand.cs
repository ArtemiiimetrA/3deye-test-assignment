using FileSort.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.CommandLine;

namespace FileSort.App.Commands;

public static class ValidateCommand
{
    public static Command Create(IHost host)
    {
        var command = new Command("validate", "Validate that a sorted file is properly sorted");
        
        var fileOption = new Option<string?>("--file", "Path to the file to validate (default: sorted.txt)");
        fileOption.SetDefaultValue("sorted.txt");

        command.AddOption(fileOption);

        command.SetHandler(async (string? file) =>
        {
            using var scope = host.Services.CreateScope();
            var serviceProvider = scope.ServiceProvider;
            var validator = serviceProvider.GetRequiredService<IFileValidator>();
            var logger = serviceProvider.GetRequiredService<ILogger<Program>>();

            var filePath = file ?? "sorted.txt";

            logger.LogInformation("Validating file: {FilePath}", filePath);

            try
            {
                var result = await validator.ValidateAsync(filePath);

                if (result.IsValid)
                {
                    Console.WriteLine($"✓ File is valid. Total records: {result.TotalRecords:N0}");
                    logger.LogInformation("Validation completed successfully. Total records: {TotalRecords}", result.TotalRecords);
                }
                else
                {
                    Console.WriteLine($"✗ File is invalid. Found {result.InvalidRecords:N0} invalid record(s) out of {result.TotalRecords:N0} total records.");
                    logger.LogWarning("Validation failed. Invalid records: {InvalidRecords}/{TotalRecords}", result.InvalidRecords, result.TotalRecords);

                    // Show first 10 errors
                    var errorsToShow = result.Errors.Take(10).ToList();
                    Console.WriteLine("\nFirst {0} error(s):", errorsToShow.Count);
                    foreach (var error in errorsToShow)
                    {
                        Console.WriteLine($"  Line {error.LineNumber}: {error.Message}");
                        Console.WriteLine($"    Content: {error.Line}");
                    }

                    if (result.Errors.Count > 10)
                    {
                        Console.WriteLine($"  ... and {result.Errors.Count - 10} more error(s)");
                    }

                    Environment.Exit(1);
                }
            }
            catch (FileNotFoundException ex)
            {
                logger.LogError(ex, "File not found: {FilePath}", filePath);
                Console.WriteLine($"Error: File not found: {filePath}");
                Environment.Exit(1);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error validating file");
                Console.WriteLine($"Error: {ex.Message}");
                Environment.Exit(1);
            }
        }, fileOption);

        return command;
    }
}

