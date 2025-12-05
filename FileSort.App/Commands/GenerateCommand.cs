using FileSort.Core.Interfaces;
using FileSort.Core.Models;
using FileSort.Core.Requests;
using FileSort.Generator.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.CommandLine;

namespace FileSort.App.Commands;

public static class GenerateCommand
{
    public static Command Create(IHost host)
    {
        var command = new Command("generate", "Generate a test file");
        
        var outputOption = new Option<string?>("--output", "Output file path");
        var sizeOption = new Option<long?>("--size", "Target size in bytes");
        var duplicatesOption = new Option<int?>("--duplicates", "Duplicate ratio percentage (0-100)");
        var seedOption = new Option<int?>("--seed", "Random seed (0 for random)");

        command.AddOption(outputOption);
        command.AddOption(sizeOption);
        command.AddOption(duplicatesOption);
        command.AddOption(seedOption);

        command.SetHandler(async (string? output, long? size, int? duplicates, int? seed) =>
        {
            using var scope = host.Services.CreateScope();
            var baseOptions = scope.ServiceProvider.GetRequiredService<IOptionsSnapshot<GeneratorOptions>>().Value;
            var generator = scope.ServiceProvider.GetRequiredService<ITestFileGenerator>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

            // Merge configuration with command-line arguments into DTO
            var request = new GeneratorRequest
            {
                OutputFilePath = output ?? baseOptions.OutputFilePath ?? throw new InvalidOperationException("OutputFilePath must be specified either in configuration or via --output option"),
                TargetSizeBytes = size ?? baseOptions.TargetSizeBytes,
                MinNumber = baseOptions.MinNumber,
                MaxNumber = baseOptions.MaxNumber,
                DuplicateRatioPercent = duplicates ?? baseOptions.DuplicateRatioPercent,
                BufferSizeBytes = baseOptions.BufferSizeBytes,
                Seed = seed ?? baseOptions.Seed
            };

            logger.LogInformation("Generating test file: {OutputPath}, Target size: {Size} bytes", request.OutputFilePath, request.TargetSizeBytes);

            var progress = new Progress<GeneratorProgress>(p =>
            {
                if (p.LinesWritten % 100000 == 0 || p.BytesWritten >= p.TargetBytes)
                {
                    double percent = (double)p.BytesWritten / p.TargetBytes * 100;
                    logger.LogInformation("Progress: {Percent:F2}% ({BytesWritten:N0} / {TargetBytes:N0} bytes, {LinesWritten:N0} lines)",
                        percent, p.BytesWritten, p.TargetBytes, p.LinesWritten);
                }
            });

            try
            {
                await generator.GenerateAsync(request, progress);
                logger.LogInformation("File generation completed: {OutputPath}", request.OutputFilePath);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error generating file");
                Environment.Exit(1);
            }
        }, outputOption, sizeOption, duplicatesOption, seedOption);

        return command;
    }
}

