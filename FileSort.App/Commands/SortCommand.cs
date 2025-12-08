using System.CommandLine;
using FileSort.Core.Interfaces;
using FileSort.Core.Models.Progress;
using FileSort.Core.Requests;
using FileSort.Progress.Interfaces;
using FileSort.Sorter.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FileSort.App.Commands;

public static class SortCommand
{
    public static Command Create(IHost host)
    {
        var command = new Command("sort", "Sort a file");

        var inputOption = new Option<string?>("--input", "Input file path");
        var sortOutputOption = new Option<string?>("--output", "Output file path");
        var chunkSizeOption = new Option<int?>("--chunk-size", "Chunk size in MB");

        command.AddOption(inputOption);
        command.AddOption(sortOutputOption);
        command.AddOption(chunkSizeOption);

        command.SetHandler(async (input, output, chunkSize) =>
        {
            using var scope = host.Services.CreateScope();
            var serviceProvider = scope.ServiceProvider;
            var baseOptions = serviceProvider.GetRequiredService<IOptionsSnapshot<SortOptions>>().Value;
            var sorter = serviceProvider.GetRequiredService<IExternalSorter>();
            var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
            var progressFactory = serviceProvider.GetRequiredService<IProgressReporterFactory<SortProgress>>();

            var request = new SortRequest
            {
                InputFilePath = input ?? baseOptions.Files.InputFilePath ??
                    throw new InvalidOperationException(
                        "InputFilePath must be specified either in configuration or via --input option"),
                OutputFilePath = output ?? baseOptions.Files.OutputFilePath ??
                    throw new InvalidOperationException(
                        "OutputFilePath must be specified either in configuration or via --output option"),
                TempDirectory = baseOptions.Files.TempDirectory ??
                                throw new InvalidOperationException("TempDirectory must be specified in configuration"),
                MaxRamMb = baseOptions.ChunkCreation.MaxRamMb,
                ChunkSizeMb = chunkSize ?? baseOptions.ChunkCreation.ChunkSizeMb,
                MaxDegreeOfParallelism = baseOptions.ChunkCreation.MaxDegreeOfParallelism,
                FileChunkTemplate = baseOptions.ChunkCreation.FileChunkTemplate ??
                                    throw new InvalidOperationException(
                                        "FileChunkTemplate must be specified in configuration"),
                BufferSizeBytes = baseOptions.Merge.BufferSizeBytes,
                DeleteTempFiles = baseOptions.Files.DeleteTempFiles,
                MaxOpenFiles = baseOptions.Merge.MaxOpenFiles,
                MaxMergeParallelism = baseOptions.Merge.MaxMergeParallelism,
                AdaptiveChunkSize = baseOptions.ChunkCreation.Adaptive.Enabled,
                MinChunkSizeMb = baseOptions.ChunkCreation.Adaptive.MinChunkSizeMb,
                MaxChunkSizeMb = baseOptions.ChunkCreation.Adaptive.MaxChunkSizeMb
            };

            logger.LogInformation("Sorting file: {InputPath} -> {OutputPath}", request.InputFilePath,
                request.OutputFilePath);

            var progress = progressFactory.CreateConsoleReporter(
                p => p.ChunksCreated > 0 || p.ChunksMerged > 0 || p.CurrentMergePass.HasValue);

            try
            {
                await sorter.SortAsync(request, progress);
                logger.LogInformation("Sorting completed: {OutputPath}", request.OutputFilePath);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error sorting file");
                Environment.Exit(1);
            }
        }, inputOption, sortOutputOption, chunkSizeOption);

        return command;
    }
}