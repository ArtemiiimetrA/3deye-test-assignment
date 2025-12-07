namespace FileSort.Progress.Interfaces;

/// <summary>
///     Factory interface for creating progress reporters.
/// </summary>
public interface IProgressReporterFactory<TProgress> where TProgress : class
{
    /// <summary>
    ///     Creates a console progress reporter.
    /// </summary>
    /// <param name="shouldReport">
    ///     Optional function that determines whether to report the progress. If null, all progress
    ///     updates are reported.
    /// </param>
    /// <param name="showInline">Whether to show progress updates inline (overwriting the same line) or on new lines.</param>
    IProgress<TProgress> CreateConsoleReporter(
        Func<TProgress, bool>? shouldReport = null,
        bool showInline = true);

    /// <summary>
    ///     Creates a logger-based progress reporter.
    /// </summary>
    /// <param name="shouldReport">
    ///     Optional function that determines whether to report the progress. If null, all progress
    ///     updates are reported.
    /// </param>
    IProgress<TProgress> CreateLoggerReporter(
        Func<TProgress, bool>? shouldReport = null);
}