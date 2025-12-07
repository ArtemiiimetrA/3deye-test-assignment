using FileSort.Sorter.Helpers;
using Xunit;

namespace FileSort.Sorter.Tests;

public class HelperTests
{
    [Theory]
    [InlineData(1, 1048576)]
    [InlineData(10, 10485760)]
    [InlineData(100, 104857600)]
    [InlineData(0, 0)]
    public void SizeHelpers_ConvertMegabytesToBytes_ReturnsCorrectValue(int megabytes, long expectedBytes)
    {
        long result = SizeHelpers.ConvertMegabytesToBytes(megabytes);
        Assert.Equal(expectedBytes, result);
    }

    [Fact]
    public void SizeHelpers_CalculateLineBytes_ReturnsCorrectValue()
    {
        string line = "123. Test";
        long bytes = SizeHelpers.CalculateLineBytes(line);

        // Should include line bytes + newline bytes
        Assert.True(bytes > line.Length);
    }

    [Fact]
    public void SizeHelpers_GetFileSize_ReturnsCorrectValue()
    {
        string filePath = Path.GetTempFileName();
        try
        {
            File.WriteAllText(filePath, "Test content");
            long size = SizeHelpers.GetFileSize(filePath);

            Assert.True(size > 0);
        }
        finally
        {
            if (File.Exists(filePath))
                File.Delete(filePath);
        }
    }

    [Fact]
    public void WriteBufferHelpers_ShouldFlushBuffer_ReturnsFalseWhenBelowCapacity()
    {
        var buffer = new List<string> { "line1", "line2" };
        bool shouldFlush = WriteBufferHelpers.ShouldFlushBuffer(buffer);

        Assert.False(shouldFlush);
    }

    [Fact]
    public void WriteBufferHelpers_ShouldFlushBuffer_ReturnsTrueWhenAtCapacity()
    {
        var buffer = new List<string>();
        // Fill to capacity (10000)
        for (int i = 0; i < 10000; i++)
        {
            buffer.Add($"line{i}");
        }

        bool shouldFlush = WriteBufferHelpers.ShouldFlushBuffer(buffer);
        Assert.True(shouldFlush);
    }

    [Fact]
    public async Task WriteBufferHelpers_FlushWriteBufferAsync_WritesAllLines()
    {
        string filePath = Path.GetTempFileName();
        try
        {
            var buffer = new List<string> { "line1", "line2", "line3" };
            await using (var writer = FileIoHelpers.CreateFileWriter(filePath, 4096))
            {
                await WriteBufferHelpers.FlushWriteBufferAsync(buffer, writer);
                await writer.FlushAsync();
            } // Writer is disposed here

            string[] lines = await File.ReadAllLinesAsync(filePath);
            Assert.Equal(3, lines.Length);
            Assert.Equal("line1", lines[0]);
            Assert.Equal("line2", lines[1]);
            Assert.Equal("line3", lines[2]);
        }
        finally
        {
            if (File.Exists(filePath))
                File.Delete(filePath);
        }
    }

    [Fact]
    public async Task WriteBufferHelpers_FlushWriteBufferAsync_EmptyBuffer_DoesNothing()
    {
        string filePath = Path.GetTempFileName();
        try
        {
            var buffer = new List<string>();
            await using (var writer = FileIoHelpers.CreateFileWriter(filePath, 4096))
            {
                await WriteBufferHelpers.FlushWriteBufferAsync(buffer, writer);
                await writer.FlushAsync();
            } // Writer is disposed here

            string[] lines = await File.ReadAllLinesAsync(filePath);
            Assert.Empty(lines);
        }
        finally
        {
            if (File.Exists(filePath))
                File.Delete(filePath);
        }
    }

    [Fact]
    public void FileIoHelpers_EnsureDirectoryExists_CreatesDirectory()
    {
        string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        try
        {
            FileIoHelpers.EnsureDirectoryExists(tempDir);

            Assert.True(Directory.Exists(tempDir));
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir);
        }
    }

    [Fact]
    public void FileIoHelpers_GenerateFilePath_ReturnsCorrectPath()
    {
        string directory = Path.GetTempPath();
        string template = "chunk_{0}.tmp";
        int index = 5;

        string path = FileIoHelpers.GenerateFilePath(directory, template, index);

        Assert.Contains("chunk_5.tmp", path);
        Assert.Contains(directory, path);
    }

    [Fact]
    public void FileIoHelpers_GenerateIntermediateFilePath_ReturnsCorrectPath()
    {
        string directory = Path.GetTempPath();
        int passNumber = 2;
        int batchIndex = 10;

        string path = FileIoHelpers.GenerateIntermediateFilePath(directory, passNumber, batchIndex);

        Assert.Contains("merge_pass2", path);
        Assert.Contains("batch0010", path);
    }

    [Fact]
    public void FileIoHelpers_SafeDeleteFile_NonExistentFile_DoesNotThrow()
    {
        string nonExistentPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".txt");

        var exception = Record.Exception(() => FileIoHelpers.SafeDeleteFile(nonExistentPath));
        Assert.Null(exception);
    }

    [Fact]
    public void FileIoHelpers_SafeDeleteFile_ExistingFile_DeletesFile()
    {
        string filePath = Path.GetTempFileName();
        try
        {
            File.WriteAllText(filePath, "test");
            FileIoHelpers.SafeDeleteFile(filePath);

            Assert.False(File.Exists(filePath));
        }
        finally
        {
            if (File.Exists(filePath))
                File.Delete(filePath);
        }
    }

    [Fact]
    public void FileIoHelpers_SafeDeleteFiles_DeletesAllFiles()
    {
        var files = new List<string>();
        for (int i = 0; i < 3; i++)
        {
            string file = Path.GetTempFileName();
            File.WriteAllText(file, "test");
            files.Add(file);
        }

        try
        {
            FileIoHelpers.SafeDeleteFiles(files);

            foreach (var file in files)
            {
                Assert.False(File.Exists(file));
            }
        }
        finally
        {
            foreach (var file in files)
            {
                if (File.Exists(file))
                    File.Delete(file);
            }
        }
    }

    [Fact]
    public async Task FileIoHelpers_CopyFileAsync_CopiesFile()
    {
        string sourceFile = Path.GetTempFileName();
        string destFile = Path.GetTempFileName();
        try
        {
            await File.WriteAllTextAsync(sourceFile, "test content");
            await FileIoHelpers.CopyFileAsync(sourceFile, destFile);

            Assert.True(File.Exists(destFile));
            string content = await File.ReadAllTextAsync(destFile);
            Assert.Equal("test content", content);
        }
        finally
        {
            if (File.Exists(sourceFile))
                File.Delete(sourceFile);
            if (File.Exists(destFile))
                File.Delete(destFile);
        }
    }

    [Fact]
    public void FileIoHelpers_CopyFile_CopiesFile()
    {
        string sourceFile = Path.GetTempFileName();
        string destFile = Path.GetTempFileName();
        try
        {
            File.WriteAllText(sourceFile, "test content");
            FileIoHelpers.CopyFile(sourceFile, destFile);

            Assert.True(File.Exists(destFile));
            string content = File.ReadAllText(destFile);
            Assert.Equal("test content", content);
        }
        finally
        {
            if (File.Exists(sourceFile))
                File.Delete(sourceFile);
            if (File.Exists(destFile))
                File.Delete(destFile);
        }
    }
}

