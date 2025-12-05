using FileSort.Core.Interfaces;
using FileSort.Core.Models;
using FileSort.Core.Requests;
using FileSort.Sorter.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.CommandLine;

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

        command.SetHandler(async (string? input, string? output, int? chunkSize) =>
        {
            using var scope = host.Services.CreateScope();
            var baseOptions = scope.ServiceProvider.GetRequiredService<IOptionsSnapshot<SortOptions>>().Value;
            var sorter = scope.ServiceProvider.GetRequiredService<IExternalSorter>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

            // Merge configuration with command-line arguments into DTO
            var request = new SortRequest
            {
                InputFilePath = input ?? baseOptions.InputFilePath ?? throw new InvalidOperationException("InputFilePath must be specified either in configuration or via --input option"),
                OutputFilePath = output ?? baseOptions.OutputFilePath ?? throw new InvalidOperationException("OutputFilePath must be specified either in configuration or via --output option"),
                TempDirectory = baseOptions.TempDirectory ?? throw new InvalidOperationException("TempDirectory must be specified in configuration"),
                MaxRamMb = baseOptions.MaxRamMb,
                ChunkSizeMb = chunkSize ?? baseOptions.ChunkSizeMb,
                MaxDegreeOfParallelism = baseOptions.MaxDegreeOfParallelism,
                FileChunkTemplate = baseOptions.FileChunkTemplate ?? throw new InvalidOperationException("FileChunkTemplate must be specified in configuration"),
                BufferSizeBytes = baseOptions.BufferSizeBytes,
                DeleteTempFiles = baseOptions.DeleteTempFiles,
                MaxOpenFiles = baseOptions.MaxOpenFiles,
                AdaptiveChunkSize = baseOptions.AdaptiveChunkSize,
                MinChunkSizeMb = baseOptions.MinChunkSizeMb,
                MaxChunkSizeMb = baseOptions.MaxChunkSizeMb
            };

            logger.LogInformation("Sorting file: {InputPath} -> {OutputPath}", request.InputFilePath, request.OutputFilePath);

            var progress = new Progress<SortProgress>(p =>
            {
                if (p.ChunksCreated > 0 || p.ChunksMerged > 0)
                {
                    double percent = p.TotalBytes > 0 ? (double)p.BytesProcessed / p.TotalBytes * 100 : 0;
                    logger.LogInformation("Progress: {Percent:F2}% - Chunks: {ChunksCreated} created, {ChunksMerged} merged, Pass {CurrentPass}/{TotalPasses}",
                        percent, p.ChunksCreated, p.ChunksMerged, p.CurrentMergePass, p.TotalMergePasses);
                }
            });

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

