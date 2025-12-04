using FileSort.Core.Interfaces;
using FileSort.Generator;
using FileSort.Sorter;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.CommandLine;
using FileSort.Core.Options;
using FileSort.Core.Validation;
using Microsoft.Extensions.Options;

var builder = Host.CreateApplicationBuilder(args);

// Configure services using extension methods
builder.Services.AddFileSortGenerator(builder.Configuration);
builder.Services.AddFileSortSorter(builder.Configuration);

var host = builder.Build();

// Note: Services will be obtained from scope within each command handler

// Set up command-line interface
var rootCommand = new RootCommand("FileSort - High-performance external merge sort for large text files");

// Generate command
var generateCommand = new Command("generate", "Generate a test file");
var outputOption = new Option<string?>("--output", "Output file path");
var sizeOption = new Option<long?>("--size", "Target size in bytes");
var duplicatesOption = new Option<int?>("--duplicates", "Duplicate ratio percentage (0-100)");
var seedOption = new Option<int?>("--seed", "Random seed (0 for random)");

generateCommand.AddOption(outputOption);
generateCommand.AddOption(sizeOption);
generateCommand.AddOption(duplicatesOption);
generateCommand.AddOption(seedOption);

generateCommand.SetHandler(async (string? output, long? size, int? duplicates, int? seed) =>
{
    using var scope = host.Services.CreateScope();
    var options = scope.ServiceProvider.GetRequiredService<IOptionsSnapshot<GeneratorOptions>>().Value;
    var generator = scope.ServiceProvider.GetRequiredService<ITestFileGenerator>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    if (output != null)
        options.OutputFilePath = output;
    if (size.HasValue)
        options.TargetSizeBytes = size.Value;
    if (duplicates.HasValue)
        options.DuplicateRatioPercent = duplicates.Value;
    if (seed.HasValue)
        options.Seed = seed.Value;

    // Validate options after command-line overrides
    GeneratorOptionsValidator.Validate(options);

    logger.LogInformation("Generating test file: {OutputPath}, Target size: {Size} bytes", options.OutputFilePath, options.TargetSizeBytes);

    var progress = new Progress<FileSort.Core.Models.GeneratorProgress>(p =>
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
        await generator.GenerateAsync(options, progress);
        logger.LogInformation("File generation completed: {OutputPath}", options.OutputFilePath);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error generating file");
        Environment.Exit(1);
    }
}, outputOption, sizeOption, duplicatesOption, seedOption);

// Sort command
var sortCommand = new Command("sort", "Sort a file");
var inputOption = new Option<string?>("--input", "Input file path");
var sortOutputOption = new Option<string?>("--output", "Output file path");
var chunkSizeOption = new Option<int?>("--chunk-size", "Chunk size in MB");

sortCommand.AddOption(inputOption);
sortCommand.AddOption(sortOutputOption);
sortCommand.AddOption(chunkSizeOption);

sortCommand.SetHandler(async (string? input, string? output, int? chunkSize) =>
{
    using var scope = host.Services.CreateScope();
    var options = scope.ServiceProvider.GetRequiredService<IOptionsSnapshot<SortOptions>>().Value;
    var sorter = scope.ServiceProvider.GetRequiredService<IExternalSorter>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    if (input != null)
        options.InputFilePath = input;
    if (output != null)
        options.OutputFilePath = output;
    if (chunkSize.HasValue)
        options.ChunkSizeMb = chunkSize.Value;

    // Validate options after command-line overrides
    SortOptionsValidator.Validate(options);

    logger.LogInformation("Sorting file: {InputPath} -> {OutputPath}", options.InputFilePath, options.OutputFilePath);

    var progress = new Progress<FileSort.Core.Models.SortProgress>(p =>
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
        await sorter.SortAsync(options, progress);
        logger.LogInformation("Sorting completed: {OutputPath}", options.OutputFilePath);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error sorting file");
        Environment.Exit(1);
    }
}, inputOption, sortOutputOption, chunkSizeOption);

rootCommand.AddCommand(generateCommand);
rootCommand.AddCommand(sortCommand);

// Parse and execute
await rootCommand.InvokeAsync(args);
