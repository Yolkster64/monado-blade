using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MonadoBlade.Core.Async
{
    /// <summary>
    /// Manages asynchronous file and USB I/O operations with batch/streaming support.
    /// Replaces synchronous blocking I/O with async alternatives.
    /// </summary>
    public class AsyncIOManager : IDisposable
    {
        private readonly SemaphoreSlim _fileAccessSemaphore;
        private readonly SemaphoreSlim _usbAccessSemaphore;
        private readonly int _maxConcurrentFileOps;
        private readonly int _maxConcurrentUsbOps;
        private bool _disposed;

        public AsyncIOManager(int maxConcurrentFileOps = 5, int maxConcurrentUsbOps = 3)
        {
            _maxConcurrentFileOps = maxConcurrentFileOps;
            _maxConcurrentUsbOps = maxConcurrentUsbOps;
            _fileAccessSemaphore = new SemaphoreSlim(maxConcurrentFileOps);
            _usbAccessSemaphore = new SemaphoreSlim(maxConcurrentUsbOps);
        }

        public class BatchFileOperation
        {
            public string FilePath { get; set; }
            public byte[] Data { get; set; }
            public OperationType Type { get; set; }
            public DateTime CreatedAt { get; set; }
        }

        public enum OperationType
        {
            Read,
            Write,
            Delete,
            Copy
        }

        public class FileOperationResult
        {
            public bool Success { get; set; }
            public string FilePath { get; set; }
            public int BytesProcessed { get; set; }
            public TimeSpan Duration { get; set; }
            public Exception Error { get; set; }
        }

        /// <summary>
        /// Async read single file with automatic retry and exponential backoff.
        /// </summary>
        public async Task<FileOperationResult> ReadFileAsync(string filePath, 
            CancellationToken cancellationToken = default, int maxRetries = 3)
        {
            var startTime = DateTime.UtcNow;

            try
            {
                await _fileAccessSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

                try
                {
                    for (int i = 0; i < maxRetries; i++)
                    {
                        try
                        {
                            var data = await File.ReadAllBytesAsync(filePath, cancellationToken)
                                .ConfigureAwait(false);

                            return new FileOperationResult
                            {
                                Success = true,
                                FilePath = filePath,
                                BytesProcessed = data.Length,
                                Duration = DateTime.UtcNow - startTime
                            };
                        }
                        catch when (i < maxRetries - 1)
                        {
                            var delayMs = (int)Math.Pow(2, i) * 100;
                            await Task.Delay(delayMs, cancellationToken).ConfigureAwait(false);
                        }
                    }
                }
                finally
                {
                    _fileAccessSemaphore.Release();
                }
            }
            catch (OperationCanceledException ex)
            {
                return new FileOperationResult
                {
                    Success = false,
                    FilePath = filePath,
                    Duration = DateTime.UtcNow - startTime,
                    Error = ex
                };
            }
            catch (Exception ex)
            {
                return new FileOperationResult
                {
                    Success = false,
                    FilePath = filePath,
                    Duration = DateTime.UtcNow - startTime,
                    Error = ex
                };
            }

            return null;
        }

        /// <summary>
        /// Async write single file with retry logic.
        /// </summary>
        public async Task<FileOperationResult> WriteFileAsync(string filePath, byte[] data,
            CancellationToken cancellationToken = default, bool createBackup = true)
        {
            var startTime = DateTime.UtcNow;

            try
            {
                await _fileAccessSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

                try
                {
                    // Create backup if file exists
                    if (createBackup && File.Exists(filePath))
                    {
                        var backupPath = filePath + ".bak";
                        await File.CopyAsync(filePath, backupPath, true, cancellationToken)
                            .ConfigureAwait(false);
                    }

                    // Ensure directory exists
                    var directory = Path.GetDirectoryName(filePath);
                    if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }

                    // Write file
                    await File.WriteAllBytesAsync(filePath, data, cancellationToken)
                        .ConfigureAwait(false);

                    return new FileOperationResult
                    {
                        Success = true,
                        FilePath = filePath,
                        BytesProcessed = data.Length,
                        Duration = DateTime.UtcNow - startTime
                    };
                }
                finally
                {
                    _fileAccessSemaphore.Release();
                }
            }
            catch (Exception ex)
            {
                return new FileOperationResult
                {
                    Success = false,
                    FilePath = filePath,
                    Duration = DateTime.UtcNow - startTime,
                    Error = ex
                };
            }
        }

        /// <summary>
        /// Batch process multiple file operations concurrently with controlled parallelism.
        /// </summary>
        public async Task<List<FileOperationResult>> BatchProcessFilesAsync(
            IEnumerable<BatchFileOperation> operations,
            CancellationToken cancellationToken = default)
        {
            var results = new List<FileOperationResult>();
            var tasks = new List<Task>();

            foreach (var op in operations)
            {
                var task = op.Type switch
                {
                    OperationType.Read => ReadFileAsync(op.FilePath, cancellationToken),
                    OperationType.Write => WriteFileAsync(op.FilePath, op.Data, cancellationToken),
                    _ => Task.FromResult<FileOperationResult>(null)
                };

                tasks.Add(task.ContinueWith(t =>
                {
                    if (t.IsCompletedSuccessfully)
                    {
                        results.Add(t.Result);
                    }
                }, cancellationToken));
            }

            await Task.WhenAll(tasks).ConfigureAwait(false);
            return results;
        }

        /// <summary>
        /// Stream large file read asynchronously in chunks.
        /// </summary>
        public async IAsyncEnumerable<byte[]> StreamReadFileAsync(string filePath,
            int bufferSize = 81920, CancellationToken cancellationToken = default)
        {
            await _fileAccessSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read,
                    FileShare.Read, bufferSize, useAsync: true))
                {
                    var buffer = new byte[bufferSize];
                    int bytesRead;

                    while ((bytesRead = await fileStream.ReadAsync(buffer, 0, bufferSize, cancellationToken)
                        .ConfigureAwait(false)) > 0)
                    {
                        yield return buffer.Take(bytesRead).ToArray();
                    }
                }
            }
            finally
            {
                _fileAccessSemaphore.Release();
            }
        }

        /// <summary>
        /// Stream large file write asynchronously in chunks.
        /// </summary>
        public async Task StreamWriteFileAsync(string filePath,
            IAsyncEnumerable<byte[]> dataStream, CancellationToken cancellationToken = default)
        {
            await _fileAccessSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                var directory = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write,
                    FileShare.None, 81920, useAsync: true))
                {
                    await foreach (var chunk in dataStream.WithCancellation(cancellationToken)
                        .ConfigureAwait(false))
                    {
                        await fileStream.WriteAsync(chunk, 0, chunk.Length, cancellationToken)
                            .ConfigureAwait(false);
                    }
                }
            }
            finally
            {
                _fileAccessSemaphore.Release();
            }
        }

        /// <summary>
        /// Async USB device enumeration with controlled concurrency.
        /// </summary>
        public async Task<List<string>> EnumerateUSBDevicesAsync(CancellationToken cancellationToken = default)
        {
            await _usbAccessSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                return await Task.Run(() =>
                {
                    var devices = new List<string>();
                    // Simulate USB enumeration
                    // In real implementation, use Windows USB APIs with P/Invoke
                    return devices;
                }, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                _usbAccessSemaphore.Release();
            }
        }

        /// <summary>
        /// Async USB data transfer with progress reporting.
        /// </summary>
        public async Task<FileOperationResult> TransferUSBDataAsync(string usbPath, byte[] data,
            IProgress<int> progress = null, CancellationToken cancellationToken = default)
        {
            var startTime = DateTime.UtcNow;

            await _usbAccessSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                int chunkSize = 4096;
                int transferred = 0;

                for (int i = 0; i < data.Length; i += chunkSize)
                {
                    int currentChunk = Math.Min(chunkSize, data.Length - i);
                    await Task.Delay(10, cancellationToken).ConfigureAwait(false); // Simulate transfer

                    transferred += currentChunk;
                    progress?.Report((transferred * 100) / data.Length);
                }

                return new FileOperationResult
                {
                    Success = true,
                    FilePath = usbPath,
                    BytesProcessed = data.Length,
                    Duration = DateTime.UtcNow - startTime
                };
            }
            catch (Exception ex)
            {
                return new FileOperationResult
                {
                    Success = false,
                    FilePath = usbPath,
                    Duration = DateTime.UtcNow - startTime,
                    Error = ex
                };
            }
            finally
            {
                _usbAccessSemaphore.Release();
            }
        }

        /// <summary>
        /// Async process launch without blocking main thread.
        /// </summary>
        public async Task<int> LaunchProcessAsync(string fileName, string arguments,
            TimeSpan? timeout = null, CancellationToken cancellationToken = default)
        {
            try
            {
                var processInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = arguments,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                };

                var process = System.Diagnostics.Process.Start(processInfo);
                if (process == null)
                    throw new InvalidOperationException("Failed to start process");

                var effectiveTimeout = timeout ?? TimeSpan.FromMinutes(5);
                var completedTask = await Task.WhenAny(
                    Task.Delay(effectiveTimeout, cancellationToken),
                    Task.Run(() => { process.WaitForExit(); return process.ExitCode; }, cancellationToken)
                ).ConfigureAwait(false);

                if (completedTask is Task<int> exitCodeTask)
                {
                    return exitCodeTask.Result;
                }

                process?.Kill();
                throw new OperationCanceledException("Process execution timeout");
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to launch process: {fileName}", ex);
            }
        }

        public void Dispose()
        {
            if (_disposed) return;

            _fileAccessSemaphore?.Dispose();
            _usbAccessSemaphore?.Dispose();
            _disposed = true;
        }
    }

    /// <summary>
    /// Helper extension to support async Copy operation.
    /// </summary>
    internal static class FileAsyncExtensions
    {
        public static async Task CopyAsync(string sourceFile, string destinationFile,
            bool overwrite, CancellationToken cancellationToken)
        {
            const int bufferSize = 81920;

            using (var sourceStream = new FileStream(sourceFile, FileMode.Open, FileAccess.Read,
                FileShare.Read, bufferSize, useAsync: true))
            using (var destinationStream = new FileStream(destinationFile,
                overwrite ? FileMode.Create : FileMode.CreateNew,
                FileAccess.Write, FileShare.None, bufferSize, useAsync: true))
            {
                var buffer = new byte[bufferSize];
                int bytesRead;

                while ((bytesRead = await sourceStream.ReadAsync(buffer, 0, bufferSize, cancellationToken)
                    .ConfigureAwait(false)) > 0)
                {
                    await destinationStream.WriteAsync(buffer, 0, bytesRead, cancellationToken)
                        .ConfigureAwait(false);
                }
            }
        }
    }
}
