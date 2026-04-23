# Implementation Guide: Implementing a New Partition Type

## Overview

Monado Blade supports different partition types for different boot scenarios (MBR, GPT, custom). This guide covers adding support for new partition types.

## Partition System Architecture

The partition system abstracts disk operations:

```
IPartitionService (Interface)
    ├── PartitionProvider (Factory)
    │   ├── MBRPartitionProvider
    │   ├── GPTPartitionProvider
    │   └── CustomPartitionProvider (New)
    └── IPartition (Interface)
        └── Implementations...
```

## Step 1: Define Partition Interface

Create in `Core/Partitioning/`:

```csharp
namespace MonadoBlade.Core.Partitioning
{
    /// <summary>
    /// Represents a disk partition.
    /// </summary>
    public interface IPartition
    {
        /// <summary>
        /// Partition type.
        /// </summary>
        PartitionType Type { get; }

        /// <summary>
        /// Partition number/index.
        /// </summary>
        int Number { get; }

        /// <summary>
        /// Starting sector.
        /// </summary>
        ulong StartSector { get; }

        /// <summary>
        /// Partition size in bytes.
        /// </summary>
        ulong SizeBytes { get; }

        /// <summary>
        /// Partition label/name.
        /// </summary>
        string Label { get; }

        /// <summary>
        /// Whether partition is bootable.
        /// </summary>
        bool IsBootable { get; }

        /// <summary>
        /// Partition GUID (GPT only).
        /// </summary>
        Guid? PartitionGuid { get; }

        /// <summary>
        /// Writes data to partition.
        /// </summary>
        Task WriteAsync(byte[] data, ulong offsetBytes = 0);

        /// <summary>
        /// Reads data from partition.
        /// </summary>
        Task<byte[]> ReadAsync(ulong offsetBytes, uint lengthBytes);

        /// <summary>
        /// Formats partition.
        /// </summary>
        Task FormatAsync(FileSystemType fileSystem);

        /// <summary>
        /// Gets partition statistics.
        /// </summary>
        Task<PartitionStats> GetStatsAsync();
    }

    /// <summary>
    /// Partition type enumeration.
    /// </summary>
    public enum PartitionType
    {
        /// <summary>MBR/DOS partition</summary>
        MBR,

        /// <summary>GPT partition</summary>
        GPT,

        /// <summary>Custom partition type</summary>
        Custom
    }

    /// <summary>
    /// Partition statistics.
    /// </summary>
    public class PartitionStats
    {
        public ulong UsedBytes { get; set; }
        public ulong FreeBytes { get; set; }
        public FileSystemType FileSystem { get; set; }
        public bool IsHealthy { get; set; }
    }
}
```

## Step 2: Implement Custom Partition Type

```csharp
namespace MonadoBlade.Core.Partitioning.Implementations
{
    /// <summary>
    /// Custom partition type implementation for specialized boot scenarios.
    /// </summary>
    public class CustomPartition : IPartition
    {
        private readonly string _diskPath;
        private readonly PartitionDescriptor _descriptor;
        private readonly ILogger _logger;

        public PartitionType Type => PartitionType.Custom;
        public int Number => _descriptor.Number;
        public ulong StartSector => _descriptor.StartSector;
        public ulong SizeBytes => _descriptor.SizeBytes;
        public string Label => _descriptor.Label;
        public bool IsBootable => _descriptor.IsBootable;
        public Guid? PartitionGuid => _descriptor.PartitionGuid;

        public CustomPartition(
            string diskPath,
            PartitionDescriptor descriptor,
            ILogger logger)
        {
            _diskPath = diskPath ?? throw new ArgumentNullException(nameof(diskPath));
            _descriptor = descriptor ?? throw new ArgumentNullException(nameof(descriptor));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task WriteAsync(byte[] data, ulong offsetBytes = 0)
        {
            try
            {
                _logger.LogDebug(
                    "Writing {Length} bytes to partition {PartitionNumber} at offset {Offset}",
                    data.Length, Number, offsetBytes);

                using (var diskHandle = OpenDiskForWrite())
                {
                    // Calculate absolute position
                    var absoluteOffset = (StartSector * 512) + offsetBytes;

                    // Seek and write
                    diskHandle.Seek((long)absoluteOffset, SeekOrigin.Begin);
                    await diskHandle.WriteAsync(data, 0, data.Length);

                    _logger.LogInfo(
                        "Successfully wrote {Length} bytes to partition {PartitionNumber}",
                        data.Length, Number);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    "Failed to write to partition {PartitionNumber}", ex, Number);
                throw new PartitionException(
                    $"Write to partition failed: {ex.Message}",
                    PartitionErrorCode.WriteFailed,
                    ex);
            }
        }

        public async Task<byte[]> ReadAsync(ulong offsetBytes, uint lengthBytes)
        {
            try
            {
                _logger.LogDebug(
                    "Reading {Length} bytes from partition {PartitionNumber} at offset {Offset}",
                    lengthBytes, Number, offsetBytes);

                var buffer = new byte[lengthBytes];

                using (var diskHandle = OpenDiskForRead())
                {
                    // Calculate absolute position
                    var absoluteOffset = (StartSector * 512) + offsetBytes;

                    // Seek and read
                    diskHandle.Seek((long)absoluteOffset, SeekOrigin.Begin);
                    int bytesRead = await diskHandle.ReadAsync(buffer, 0, (int)lengthBytes);

                    if (bytesRead != lengthBytes)
                    {
                        _logger.LogWarning(
                            "Read fewer bytes than requested: {BytesRead}/{Requested}",
                            bytesRead, lengthBytes);
                    }

                    _logger.LogDebug(
                        "Successfully read {Length} bytes from partition {PartitionNumber}",
                        bytesRead, Number);

                    return buffer;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    "Failed to read from partition {PartitionNumber}", ex, Number);
                throw new PartitionException(
                    $"Read from partition failed: {ex.Message}",
                    PartitionErrorCode.ReadFailed,
                    ex);
            }
        }

        public async Task FormatAsync(FileSystemType fileSystem)
        {
            try
            {
                _logger.LogInfo(
                    "Formatting partition {PartitionNumber} with {FileSystem}",
                    Number, fileSystem);

                // Use Windows API to format
                var process = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = FormatCommand(fileSystem),
                    UseShellExecute = false,
                    RedirectStandardOutput = true
                };

                using (var proc = Process.Start(process))
                {
                    await proc.WaitForExitAsync();

                    if (proc.ExitCode != 0)
                    {
                        throw new PartitionException(
                            $"Format failed with exit code {proc.ExitCode}",
                            PartitionErrorCode.FormatFailed);
                    }
                }

                _logger.LogInfo(
                    "Partition {PartitionNumber} formatted successfully",
                    Number);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    "Failed to format partition {PartitionNumber}", ex, Number);
                throw new PartitionException(
                    $"Format partition failed: {ex.Message}",
                    PartitionErrorCode.FormatFailed,
                    ex);
            }
        }

        public async Task<PartitionStats> GetStatsAsync()
        {
            try
            {
                var driveInfo = new DriveInfo(_diskPath);

                return new PartitionStats
                {
                    UsedBytes = (ulong)(driveInfo.TotalSize - driveInfo.AvailableFreeSpace),
                    FreeBytes = (ulong)driveInfo.AvailableFreeSpace,
                    FileSystem = ParseFileSystem(driveInfo.DriveFormat),
                    IsHealthy = await VerifyHealthAsync()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to get partition stats", ex);
                throw new PartitionException(
                    $"Get stats failed: {ex.Message}",
                    PartitionErrorCode.StatsFailed,
                    ex);
            }
        }

        private FileHandle OpenDiskForRead()
        {
            // Open disk for reading with appropriate flags
            var handle = NativeMethods.CreateFileA(
                _diskPath,
                NativeMethods.GENERIC_READ,
                NativeMethods.FILE_SHARE_READ,
                IntPtr.Zero,
                NativeMethods.OPEN_EXISTING,
                0,
                IntPtr.Zero);

            if (handle.IsInvalid)
            {
                throw new PartitionException(
                    "Cannot open disk for reading",
                    PartitionErrorCode.AccessDenied);
            }

            return handle;
        }

        private FileHandle OpenDiskForWrite()
        {
            // Open disk for writing (requires elevated privileges)
            var handle = NativeMethods.CreateFileA(
                _diskPath,
                NativeMethods.GENERIC_READ | NativeMethods.GENERIC_WRITE,
                0, // No sharing
                IntPtr.Zero,
                NativeMethods.OPEN_EXISTING,
                0,
                IntPtr.Zero);

            if (handle.IsInvalid)
            {
                throw new PartitionException(
                    "Cannot open disk for writing - access denied",
                    PartitionErrorCode.AccessDenied);
            }

            return handle;
        }

        private string FormatCommand(FileSystemType fileSystem)
        {
            return fileSystem switch
            {
                FileSystemType.FAT32 => $"/C format {_diskPath} /FS:FAT32 /X /Q",
                FileSystemType.NTFS => $"/C format {_diskPath} /FS:NTFS /X /Q",
                _ => throw new NotSupportedException($"File system {fileSystem} not supported")
            };
        }

        private FileSystemType ParseFileSystem(string driveFormat)
        {
            return driveFormat?.ToUpperInvariant() switch
            {
                "FAT32" => FileSystemType.FAT32,
                "NTFS" => FileSystemType.NTFS,
                "EXFAT" => FileSystemType.ExFAT,
                _ => FileSystemType.Unknown
            };
        }

        private async Task<bool> VerifyHealthAsync()
        {
            try
            {
                // Read boot sector and verify magic bytes
                var bootSector = await ReadAsync(0, 512);
                return bootSector.Length >= 2; // Basic check
            }
            catch
            {
                return false;
            }
        }
    }

    /// <summary>
    /// Partition descriptor for custom partitions.
    /// </summary>
    public class PartitionDescriptor
    {
        public int Number { get; set; }
        public ulong StartSector { get; set; }
        public ulong SizeBytes { get; set; }
        public string Label { get; set; }
        public bool IsBootable { get; set; }
        public Guid? PartitionGuid { get; set; }
    }
}
```

## Step 3: Create Partition Provider

```csharp
namespace MonadoBlade.Core.Partitioning
{
    /// <summary>
    /// Factory for creating partition instances.
    /// </summary>
    public class PartitionProvider
    {
        private readonly ILogger _logger;
        private readonly Dictionary<PartitionType, Func<string, PartitionDescriptor, IPartition>> _factories;

        public PartitionProvider(ILogger logger)
        {
            _logger = logger;
            _factories = new Dictionary<PartitionType, Func<string, PartitionDescriptor, IPartition>>
            {
                { PartitionType.MBR, (disk, desc) => new MBRPartition(disk, desc, logger) },
                { PartitionType.GPT, (disk, desc) => new GPTPartition(disk, desc, logger) },
                { PartitionType.Custom, (disk, desc) => new CustomPartition(disk, desc, logger) }
            };
        }

        /// <summary>
        /// Creates partition instance of specified type.
        /// </summary>
        public IPartition Create(
            PartitionType type,
            string diskPath,
            PartitionDescriptor descriptor)
        {
            if (!_factories.TryGetValue(type, out var factory))
            {
                throw new PartitionException(
                    $"Unsupported partition type: {type}",
                    PartitionErrorCode.UnsupportedType);
            }

            _logger.LogDebug(
                "Creating partition of type {PartitionType} on disk {Disk}",
                type, diskPath);

            return factory(diskPath, descriptor);
        }

        /// <summary>
        /// Detects partition type from disk.
        /// </summary>
        public async Task<PartitionType> DetectTypeAsync(string diskPath)
        {
            _logger.LogDebug("Detecting partition type for disk: {Disk}", diskPath);

            try
            {
                using (var diskHandle = File.OpenRead(diskPath))
                {
                    var bootSector = new byte[512];
                    await diskHandle.ReadAsync(bootSector, 0, 512);

                    // Check for MBR signature
                    if (bootSector[510] == 0x55 && bootSector[511] == 0xAA)
                    {
                        // Could be MBR or GPT (check for GPT)
                        diskHandle.Seek(0x200, SeekOrigin.Begin);
                        var gptSig = new byte[8];
                        await diskHandle.ReadAsync(gptSig, 0, 8);

                        if (Encoding.ASCII.GetString(gptSig) == "EFI PART")
                        {
                            return PartitionType.GPT;
                        }

                        return PartitionType.MBR;
                    }

                    return PartitionType.Custom;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to detect partition type", ex);
                return PartitionType.Custom; // Default fallback
            }
        }
    }
}
```

## Step 4: Integrate with Partition Service

```csharp
public class PartitionService : IPartitionService
{
    private readonly PartitionProvider _provider;
    private readonly ILogger _logger;
    private readonly Dictionary<string, IPartition> _partitionCache;

    public async Task<IPartition> GetPartitionAsync(string diskPath, int partitionNumber)
    {
        var cacheKey = $"{diskPath}:{partitionNumber}";

        if (_partitionCache.TryGetValue(cacheKey, out var partition))
            return partition;

        var partitionType = await _provider.DetectTypeAsync(diskPath);
        var descriptor = await ReadPartitionDescriptorAsync(diskPath, partitionNumber);

        partition = _provider.Create(partitionType, diskPath, descriptor);
        _partitionCache[cacheKey] = partition;

        return partition;
    }

    public async Task<IEnumerable<IPartition>> EnumeratePartitionsAsync(string diskPath)
    {
        var partitionType = await _provider.DetectTypeAsync(diskPath);
        var descriptors = await ReadAllPartitionDescriptorsAsync(diskPath, partitionType);

        var partitions = new List<IPartition>();
        foreach (var descriptor in descriptors)
        {
            var partition = _provider.Create(partitionType, diskPath, descriptor);
            partitions.Add(partition);
        }

        return partitions;
    }
}
```

## Step 5: Define Exception Types

```csharp
namespace MonadoBlade.Core.Exceptions
{
    public class PartitionException : MonadoBladeException
    {
        public PartitionErrorCode ErrorCode { get; }

        public PartitionException(
            string message,
            PartitionErrorCode errorCode,
            Exception innerException = null)
            : base(message, (int)errorCode, innerException)
        {
            ErrorCode = errorCode;
        }
    }

    public enum PartitionErrorCode
    {
        ReadFailed = 6001,
        WriteFailed = 6002,
        FormatFailed = 6003,
        AccessDenied = 6004,
        UnsupportedType = 6005,
        StatsFailed = 6006
    }
}
```

## Step 6: Testing

```csharp
[TestFixture]
public class CustomPartitionTests
{
    private Mock<ILogger> _loggerMock;
    private CustomPartition _partition;
    private PartitionDescriptor _descriptor;
    private string _testDiskPath;

    [SetUp]
    public void Setup()
    {
        _loggerMock = new Mock<ILogger>();
        _testDiskPath = @"\\.\PHYSICALDRIVE1";
        _descriptor = new PartitionDescriptor
        {
            Number = 1,
            StartSector = 2048,
            SizeBytes = 1073741824, // 1GB
            Label = "TestPartition",
            IsBootable = true
        };

        _partition = new CustomPartition(_testDiskPath, _descriptor, _loggerMock.Object);
    }

    [Test]
    public void Partition_HasCorrectProperties()
    {
        Assert.That(_partition.Type, Is.EqualTo(PartitionType.Custom));
        Assert.That(_partition.Number, Is.EqualTo(1));
        Assert.That(_partition.Label, Is.EqualTo("TestPartition"));
    }

    [Test]
    public async Task GetStatsAsync_ReturnsValidStats()
    {
        var stats = await _partition.GetStatsAsync();

        Assert.That(stats, Is.Not.Null);
        Assert.That(stats.UsedBytes, Is.GreaterThanOrEqualTo(0));
        Assert.That(stats.FreeBytes, Is.GreaterThanOrEqualTo(0));
    }
}
```

## Best Practices

1. **Access Control**: Always check permissions before disk operations
2. **Error Handling**: Provide specific error codes for different failures
3. **Logging**: Log all operations at appropriate levels
4. **Testing**: Mock disk operations for unit tests
5. **Documentation**: Document partition format and limitations
6. **Performance**: Cache partition descriptors to avoid repeated reads
7. **Validation**: Verify partition integrity before operations

## Related Documentation

- ADR-003: Error Handling Strategy
- ADR-009: Logging Strategy
- ARCHITECTURE.md: Partitioning system
