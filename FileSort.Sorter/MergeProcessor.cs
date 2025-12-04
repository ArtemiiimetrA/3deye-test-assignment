using FileSort.Core.Models;
using FileSort.Core.Parsing;

namespace FileSort.Sorter;

/// <summary>
/// Handles k-way merge of sorted chunk files.
/// Supports both single-pass and multi-pass cascading merge strategies.
/// </summary>
internal sealed class MergeProcessor
{
    private readonly int _maxOpenFiles;
    private readonly int _bufferSize;

    public MergeProcessor(int maxOpenFiles, int bufferSize)
    {
        _maxOpenFiles = maxOpenFiles;
        _bufferSize = bufferSize;
    }

    public async Task MergeChunksAsync(
        IReadOnlyList<string> chunkFilePaths,
        string outputFilePath,
        IProgress<SortProgress>? progress,
        CancellationToken cancellationToken)
    {
        if (chunkFilePaths.Count == 0)
        {
            // Create empty output file
            File.Create(outputFilePath).Dispose();
            return;
        }

        if (chunkFilePaths.Count == 1)
        {
            // Single chunk - just copy to output
            File.Copy(chunkFilePaths[0], outputFilePath, overwrite: true);
            return;
        }

        if (chunkFilePaths.Count <= _maxOpenFiles)
        {
            // Single-pass merge
            await MergeSinglePassAsync(chunkFilePaths, outputFilePath, progress, cancellationToken);
        }
        else
        {
            // Multi-pass cascading merge
            await MergeMultiPassAsync(chunkFilePaths, outputFilePath, progress, cancellationToken);
        }
    }

    private async Task MergeSinglePassAsync(
        IReadOnlyList<string> filePaths,
        string outputPath,
        IProgress<SortProgress>? progress,
        CancellationToken cancellationToken)
    {
        var readers = new StreamReader[filePaths.Count];
        var priorityQueue = new PriorityQueue<RecordWithSource, RecordKey>();

        try
        {
            // Open all files and read first line from each
            for (int i = 0; i < filePaths.Count; i++)
            {
                var fileStream = new FileStream(
                    filePaths[i],
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.Read,
                    _bufferSize,
                    FileOptions.SequentialScan | FileOptions.Asynchronous);

                // StreamReader will dispose the FileStream when disposed
                readers[i] = new StreamReader(fileStream, System.Text.Encoding.UTF8, bufferSize: _bufferSize, leaveOpen: false);

                string? line = await readers[i].ReadLineAsync();
                if (line != null && RecordParser.TryParse(line, out Record record))
                {
                    var key = new RecordKey(record.Text, record.Number);
                    priorityQueue.Enqueue(new RecordWithSource(record, i), key);
                }
            }

            // Open output file
            await using var outputStream = new FileStream(
                outputPath,
                FileMode.Create,
                FileAccess.Write,
                FileShare.None,
                _bufferSize,
                FileOptions.SequentialScan | FileOptions.Asynchronous);

            await using var writer = new StreamWriter(outputStream, System.Text.Encoding.UTF8, bufferSize: _bufferSize);

            var writeBuffer = new List<string>(capacity: 10000);
            int recordsWritten = 0;

            // Merge loop
            while (priorityQueue.Count > 0)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var (record, sourceIndex) = priorityQueue.Dequeue();
                writeBuffer.Add(record.ToLine());
                recordsWritten++;

                // Read next line from the source file
                if (readers[sourceIndex] != null)
                {
                    string? nextLine = await readers[sourceIndex]!.ReadLineAsync();
                    if (nextLine != null && RecordParser.TryParse(nextLine, out Record nextRecord))
                    {
                        var key = new RecordKey(nextRecord.Text, nextRecord.Number);
                        priorityQueue.Enqueue(new RecordWithSource(nextRecord, sourceIndex), key);
                    }
                    else
                    {
                        // EOF reached for this file - close it
                        readers[sourceIndex]!.Dispose();
                        readers[sourceIndex] = null!;
                    }
                }

                // Flush buffer periodically
                if (writeBuffer.Count >= 10000)
                {
                    foreach (string line in writeBuffer)
                    {
                        await writer.WriteLineAsync(line);
                    }
                    writeBuffer.Clear();
                }

                // Report progress periodically
                if (recordsWritten % 100000 == 0)
                {
                    progress?.Report(new SortProgress
                    {
                        ChunksMerged = recordsWritten
                    });
                }
            }

            // Flush remaining lines
            if (writeBuffer.Count > 0)
            {
                foreach (string line in writeBuffer)
                {
                    await writer.WriteLineAsync(line);
                }
            }

            await writer.FlushAsync();
        }
        finally
        {
            // Clean up readers
            foreach (var reader in readers)
            {
                if (reader != null)
                {
                    try
                    {
                        reader.Dispose();
                    }
                    catch
                    {
                        // Ignore disposal errors
                    }
                }
            }
        }
    }

    private async Task MergeMultiPassAsync(
        IReadOnlyList<string> initialFiles,
        string outputPath,
        IProgress<SortProgress>? progress,
        CancellationToken cancellationToken)
    {
        List<string> currentFiles = new(initialFiles);
        string tempDir = Path.GetDirectoryName(initialFiles[0]) ?? "temp";
        int passNumber = 1;

        // Calculate total passes needed
        int totalPasses = CalculateTotalPasses(initialFiles.Count, _maxOpenFiles);
        progress?.Report(new SortProgress
        {
            TotalMergePasses = totalPasses
        });

        while (currentFiles.Count > 1)
        {
            progress?.Report(new SortProgress
            {
                CurrentMergePass = passNumber,
                TotalMergePasses = totalPasses
            });

            List<string> nextPassFiles = new();
            int batchSize = _maxOpenFiles - 1; // Reserve 1 for output
            int batchIndex = 0;
            int totalBatches = (int)Math.Ceiling((double)currentFiles.Count / batchSize);

            // Process files in batches
            for (int i = 0; i < currentFiles.Count; i += batchSize)
            {
                cancellationToken.ThrowIfCancellationRequested();

                int batchEnd = Math.Min(i + batchSize, currentFiles.Count);
                var batch = currentFiles.Skip(i).Take(batchEnd - i).ToList();

                progress?.Report(new SortProgress
                {
                    CurrentMergePass = passNumber,
                    TotalMergePasses = totalPasses,
                    CurrentBatchInPass = batchIndex + 1,
                    TotalBatchesInPass = totalBatches
                });

                // Merge batch into intermediate file
                string intermediateFile = Path.Combine(
                    tempDir,
                    $"merge_pass{passNumber}_batch{batchIndex:0000}.tmp");

                if (batch.Count == 1)
                {
                    // Single file - just rename/copy
                    File.Copy(batch[0], intermediateFile, overwrite: true);
                }
                else
                {
                    await MergeSinglePassAsync(batch, intermediateFile, null, cancellationToken);
                }

                nextPassFiles.Add(intermediateFile);
                batchIndex++;
            }

            // Delete files from previous pass (except initial chunks which are handled separately)
            if (passNumber > 1)
            {
                foreach (string file in currentFiles)
                {
                    try
                    {
                        if (File.Exists(file))
                            File.Delete(file);
                    }
                    catch
                    {
                        // Ignore deletion errors
                    }
                }
            }

            currentFiles = nextPassFiles;
            passNumber++;
        }

        // Final file is the output
        if (currentFiles.Count == 1)
        {
            File.Copy(currentFiles[0], outputPath, overwrite: true);
            
            // Clean up last intermediate file
            try
            {
                if (File.Exists(currentFiles[0]))
                    File.Delete(currentFiles[0]);
            }
            catch
            {
                // Ignore deletion errors
            }
        }
    }

    private static int CalculateTotalPasses(int fileCount, int maxOpenFiles)
    {
        if (fileCount <= maxOpenFiles)
            return 1;

        int passes = 0;
        int currentCount = fileCount;
        int batchSize = maxOpenFiles - 1;

        while (currentCount > 1)
        {
            currentCount = (int)Math.Ceiling((double)currentCount / batchSize);
            passes++;
        }

        return passes;
    }
}
