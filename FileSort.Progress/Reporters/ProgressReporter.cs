using Microsoft.Extensions.Logging;

namespace FileSort.Progress.Reporters;

/// <summary>
/// Generic progress reporter that can output to console or logger.
/// </summary>
public sealed class ProgressReporter<TProgress> : IProgress<TProgress> where TProgress : class
{
    private readonly Action<string> _output;
    private readonly Func<TProgress, string> _formatter;
    private readonly Func<TProgress, bool>? _shouldReport;

    /// <summary>
    /// Creates a progress reporter that outputs to console with inline updates.
    /// </summary>
    public static ProgressReporter<TProgress> CreateConsole(
        Func<TProgress, string> formatter,
        Func<TProgress, bool>? shouldReport = null,
        bool showInline = true)
    {
        return new ProgressReporter<TProgress>(
            output: message =>
            {
                try
                {
                    if (showInline)
                    {
                        int width = Console.WindowWidth > 0 ? Console.WindowWidth - 1 : message.Length;
                        Console.Write($"\r{message.PadRight(width)}");
                    }
                    else
                    {
                        Console.WriteLine(message);
                    }
                }
                catch
                {
                    Console.Write($"\r{message}");
                }
            },
            formatter: formatter,
            shouldReport: shouldReport);
    }

    /// <summary>
    /// Creates a progress reporter that outputs to logger.
    /// </summary>
    public static ProgressReporter<TProgress> CreateLogger(
        ILogger logger,
        Func<TProgress, string> formatter,
        Func<TProgress, bool>? shouldReport = null)
    {
        return new ProgressReporter<TProgress>(
            output: message => logger.LogInformation(message),
            formatter: formatter,
            shouldReport: shouldReport);
    }

    private ProgressReporter(
        Action<string> output,
        Func<TProgress, string> formatter,
        Func<TProgress, bool>? shouldReport)
    {
        _output = output ?? throw new ArgumentNullException(nameof(output));
        _formatter = formatter ?? throw new ArgumentNullException(nameof(formatter));
        _shouldReport = shouldReport;
    }

    public void Report(TProgress progress)
    {
        if (progress == null)
            return;

        // Check if we should report this progress update
        if (_shouldReport != null && !_shouldReport(progress))
        {
            return;
        }

        string message = _formatter(progress);
        _output(message);
    }
}

