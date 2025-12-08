# FileSort

## Assumptions

1. The pattern of the string will never be changed in the future.

2. Only integer numbers should be used in the number part of records. Any decimal numbers (e.g., "123.5. Text") will be treated as invalid and rejected during parsing.

3. Multiple consecutive whitespaces in the text part will be normalized to single spaces. The string will be normalized by collapsing all whitespace sequences (spaces, tabs, etc.) into single spaces.

4. If invalid string appears in input file the whole application stops.

## CLI Usage

The application provides two main commands: `generate` for creating test files and `sort` for sorting files. Command-line options override values from `appsettings.json`.

### Generate Command

Generates a test file with records in the format `{Number}. {Text}`.

**Basic usage:**
```bash
dotnet run -- generate
```

**With options:**
```bash
dotnet run -- generate --output input.txt --size 1073741824 --duplicates 20 --seed 42
```

**Options:**
- `--output` (string, optional): Output file path. Overrides `GeneratorOptions.OutputFilePath` from configuration.
- `--size` (long, optional): Target file size in bytes. Overrides `GeneratorOptions.TargetSizeBytes` from configuration.
- `--duplicates` (int, optional): Duplicate ratio percentage (0-100). Overrides `GeneratorOptions.DuplicateRatioPercent` from configuration.
- `--seed` (int, optional): Random seed for reproducible generation. Overrides `GeneratorOptions.Seed` from configuration. Omit for random generation.

**Examples:**
```bash
# Generate a 1GB file with default settings from appsettings.json
dotnet run -- generate

# Generate a 500MB file with 30% duplicates
dotnet run -- generate --output test.txt --size 524288000 --duplicates 30

# Generate a reproducible test file with seed
dotnet run -- generate --output test.txt --size 1073741824 --seed 12345
```

### Sort Command

Sorts a file using external merge sort algorithm.

**Basic usage:**
```bash
dotnet run -- sort
```

**With options:**
```bash
dotnet run -- sort --input input.txt --output sorted.txt --chunk-size 50
```

**Options:**
- `--input` (string, optional): Input file path. Overrides `SortOptions.InputFilePath` from configuration.
- `--output` (string, optional): Output file path. Overrides `SortOptions.OutputFilePath` from configuration.
- `--chunk-size` (int, optional): Chunk size in megabytes. Overrides `SortOptions.ChunkSizeMb` from configuration.

**Examples:**
```bash
# Sort using default settings from appsettings.json
dotnet run -- sort

# Sort a specific file with custom output
dotnet run -- sort --input data.txt --output sorted_data.txt

# Sort with custom chunk size
dotnet run -- sort --input large_file.txt --output sorted.txt --chunk-size 100
```

**Note:** All other sorting options (memory limits, parallelism, buffer sizes, etc.) must be configured in `appsettings.json` under the `SortOptions` section.

## Configuration

### SortOptions

Configuration settings for the file sorting operation.

The sorting process consists of two main phases:
1. **Chunk Creation**: Input file is divided into sorted chunks
2. **Merge Phase**: Chunks are merged into the final sorted output

The merge phase uses one of two strategies based on the number of chunks:
- **Single-Pass Merge**: Used when the number of chunks ≤ `Merge.MaxOpenFiles` (all chunks can be opened simultaneously)
- **Multi-Pass Merge**: Used when the number of chunks > `Merge.MaxOpenFiles` (requires cascading merge passes)

Configuration is organized into nested sections for better structure:

```json
{
  "SortOptions": {
    "Files": { ... },
    "ChunkCreation": { ... },
    "Merge": { ... }
  }
}
```

#### Files

Options for file paths and temporary file management.

- **InputFilePath** (string): Path to the input file that will be sorted. The file should contain records in the format `{Number}. {Text}`.

- **OutputFilePath** (string): Path where the sorted output file will be written.

- **TempDirectory** (string): Directory path where temporary chunk files will be created during the sorting process. The directory will be created if it doesn't exist.

- **DeleteTempFiles** (bool): Whether to automatically delete temporary chunk files after the sorting operation completes. Set to `false` to keep chunk files for debugging or inspection. Default: `true`.

#### ChunkCreation

Options used during the chunk creation phase.

- **MaxRamMb** (int): Maximum amount of RAM (in megabytes) that the sorting process should use. This helps control memory consumption and prevents out-of-memory errors when processing large files.

- **ChunkSizeMb** (int): Size of each chunk (in megabytes) when using fixed chunk size mode. This determines how much data is read into memory, sorted, and written to a temporary file before processing the next chunk.

- **MaxDegreeOfParallelism** (int): Maximum number of concurrent chunk processing operations. Controls how many chunks can be sorted in parallel, which affects CPU and I/O utilization.

- **FileChunkTemplate** (string): Template string for naming temporary chunk files. Uses string formatting with `{0}` as the chunk index placeholder (e.g., `"chunk_{0:0000}.tmp"`).

- **Adaptive** (object): Options for adaptive chunk sizing.
  - **Enabled** (bool): Enables adaptive chunk sizing based on memory pressure. When enabled, chunk sizes will vary between `MinChunkSizeMb` and `MaxChunkSizeMb` based on available memory and data characteristics.
  - **MinChunkSizeMb** (int): Minimum chunk size (in megabytes) when using adaptive chunk sizing. Used only when `Enabled` is `true`.
  - **MaxChunkSizeMb** (int): Maximum chunk size (in megabytes) when using adaptive chunk sizing. Used only when `Enabled` is `true`.

#### Merge

Options used during the merge phase.

- **BufferSizeBytes** (int): Size of the I/O buffer (in bytes) used for reading and writing files. Larger buffers can improve I/O performance for sequential operations.
  - Used in: **Both Single-Pass and Multi-Pass merge strategies**

- **MaxOpenFiles** (int): Maximum number of file handles that can be open simultaneously during the merge phase. This determines which merge strategy is used:
  - If number of chunks ≤ `MaxOpenFiles`: **Single-Pass Merge** is used (all chunks merged in one pass)
  - If number of chunks > `MaxOpenFiles`: **Multi-Pass Merge** is used (chunks merged in cascading passes)
  - Used in: **Multi-Pass merge strategy only** (determines batch size and number of passes)

- **MaxMergeParallelism** (int): Maximum number of concurrent merge operations during the merge phase. Controls how many merge batches can be processed in parallel, which affects CPU and I/O utilization during the merge process. Default: `1`.
  - Used in: **Multi-Pass merge strategy only** (enables parallel batch processing)

**Example configuration:**
```json
{
  "SortOptions": {
    "Files": {
      "InputFilePath": "input.txt",
      "OutputFilePath": "sorted.txt",
      "TempDirectory": "temp",
      "DeleteTempFiles": true
    },
    "ChunkCreation": {
      "MaxRamMb": 50,
      "ChunkSizeMb": 20,
      "MaxDegreeOfParallelism": 4,
      "FileChunkTemplate": "chunk_{0:0000}.tmp",
      "Adaptive": {
        "Enabled": true,
        "MinChunkSizeMb": 1,
        "MaxChunkSizeMb": 30
      }
    },
    "Merge": {
      "BufferSizeBytes": 524288,
      "MaxOpenFiles": 5,
      "MaxMergeParallelism": 2
    }
  }
}
```

### GeneratorOptions

Configuration settings for generating test files.

- **OutputFilePath** (string): Path where the generated test file will be written. The file will be created or overwritten.

- **TargetSizeBytes** (long): Target size of the generated file in bytes. The generator will create records until the file reaches approximately this size.

- **MinNumber** (int): Minimum value for the numeric part of generated records. Must be non-negative.

- **MaxNumber** (int): Maximum value for the numeric part of generated records. Must be greater than or equal to `MinNumber`.

- **DuplicateRatioPercent** (int): Percentage (0-100) of records that should be duplicates. Higher values will result in more repeated text values in the generated file.

- **BufferSizeBytes** (int): Size of the I/O buffer (in bytes) used for writing the generated file. Larger buffers can improve write performance.

- **MaxWordsPerString** (int): Maximum number of words in the text part of generated records. Controls the length of the text portion of each record.

- **Seed** (int?, optional): Random seed value for generating reproducible test files. If not specified or set to `null`, a random seed will be used, resulting in different output each time.

