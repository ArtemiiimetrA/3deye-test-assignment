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

### Merge Algorithms

#### Single-Pass Merge Algorithm

The Single-Pass Merge strategy performs a k-way merge of all sorted chunk files in a single pass. This algorithm is used when the number of chunks does not exceed the `MaxOpenFiles` limit.

**Algorithm Steps:**

1. **Initialization**: Open all chunk files simultaneously, creating a stream reader for each file.

2. **Priority Queue Setup**: Read the first record from each chunk file and insert them into a min-heap priority queue. Each queue entry contains:
   - The record itself
   - The source file index (to track which file the record came from)
   - The record's sort key (used for comparison)

3. **Merge Loop**: While the priority queue is not empty:
   - Dequeue the record with the smallest key (top of the min-heap)
   - Write the record to the output file (using buffered writes for performance)
   - Read the next record from the source file that provided the dequeued record
   - If a record was successfully read, enqueue it into the priority queue
   - If the source file is exhausted (end of file), close that file handle

4. **Completion**: Once all files are exhausted and the priority queue is empty, flush any remaining buffered writes and close the output file.

**Time Complexity**: O(N log k) where N is the total number of records and k is the number of chunks.

**Space Complexity**: O(k) for the priority queue and file handles.

**Advantages**:
- Single pass through all data
- Optimal for scenarios where all chunks can fit within file handle limits
- Minimal I/O operations

#### Multi-Pass Merge Algorithm

The Multi-Pass Merge strategy uses a cascading merge approach when the number of chunks exceeds the `MaxOpenFiles` limit. This algorithm divides chunks into batches and merges them in multiple passes.

**Algorithm Steps:**

1. **Pass Initialization**: Start with the initial set of sorted chunk files.

2. **Batch Processing** (for each pass):
   - Calculate batch size: `batchSize = MaxOpenFiles - 1` (reserving 1 file handle for output)
   - Divide current files into batches of size `batchSize`
   - For each batch:
     - If batch contains only 1 file, copy it directly to an intermediate file
     - If batch contains multiple files, merge them using the Single-Pass Merge algorithm into an intermediate file
     - Process batches in parallel (up to `MaxMergeParallelism` concurrent batches) if parallelism is enabled
   - Clean up files from the previous pass (except initial chunks)

3. **Pass Iteration**: Use the intermediate files from the current pass as input for the next pass. Repeat until only one file remains.

4. **Final Output**: Copy the remaining single file to the final output path.

**Example with 20 chunks and MaxOpenFiles = 5:**
- **Pass 1**: 20 chunks → 5 batches (4 chunks each) → 5 intermediate files
- **Pass 2**: 5 intermediate files → 1 batch (4 files) + 1 single file → 2 intermediate files
- **Pass 3**: 2 intermediate files → 1 batch (2 files) → 1 final file → output

**Time Complexity**: O(N log N) in the worst case, but typically better due to parallel batch processing.

**Space Complexity**: O(MaxOpenFiles) for file handles per batch.

**Advantages**:
- Handles any number of chunks regardless of file handle limits
- Supports parallel batch processing for improved performance
- Efficiently manages memory by processing in batches
- Automatically cleans up intermediate files between passes

### Configuration

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

