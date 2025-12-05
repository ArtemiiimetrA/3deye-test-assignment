using FileSort.App.Commands;
using FileSort.Generator;
using FileSort.Sorter;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.CommandLine;

var builder = Host.CreateApplicationBuilder(args);

// Configure services using extension methods
builder.Services.AddFileSortGenerator(builder.Configuration);
builder.Services.AddFileSortSorter(builder.Configuration);

var host = builder.Build();

// Set up command-line interface
var rootCommand = new RootCommand("FileSort - High-performance external merge sort for large text files");

// Add commands
rootCommand.AddCommand(GenerateCommand.Create(host));
rootCommand.AddCommand(SortCommand.Create(host));

// Parse and execute
await rootCommand.InvokeAsync(args);
