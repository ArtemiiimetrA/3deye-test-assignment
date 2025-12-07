using FileSort.Progress.Interfaces;
using FileSort.Progress.Reporters;
using Microsoft.Extensions.Logging;

namespace FileSort.Progress.Helpers;

/// <summary>
/// Service implementation of progress reporter factory.
/// </summary>
public sealed class ProgressReporterFactoryService<TProgress> : IProgressReporterFactory<TProgress> where TProgress : class
{
    private readonly Func<TProgress, string> _formatter;
    private readonly ILogger? _logger;

    /// <summary>
    /// Creates a factory service for console reporters.
    /// </summary>
    public ProgressReporterFactoryService(Func<TProgress, string> formatter)
    {
        _formatter = formatter ?? throw new ArgumentNullException(nameof(formatter));
    }

    /// <summary>
    /// Creates a factory service for logger reporters.
    /// </summary>
    public ProgressReporterFactoryService(Func<TProgress, string> formatter, ILogger logger)
    {
        _formatter = formatter ?? throw new ArgumentNullException(nameof(formatter));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    //<inheritdoc />
    public IProgress<TProgress> CreateConsoleReporter(
        Func<TProgress, bool>? shouldReport = null,
        bool showInline = true)
    {
        return ProgressReporter<TProgress>.CreateConsole(_formatter, shouldReport, showInline);
    }

    //<inheritdoc />
    public IProgress<TProgress> CreateLoggerReporter(
        Func<TProgress, bool>? shouldReport = null)
    {
        if (_logger == null)
            throw new InvalidOperationException("Logger is required for logger-based reporters. Use the constructor with ILogger parameter.");

        return ProgressReporter<TProgress>.CreateLogger(_logger, _formatter, shouldReport);
    }
}

