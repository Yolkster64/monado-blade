namespace MonadoBlade.Security.Interfaces;

using MonadoBlade.Security.Models;

/// <summary>
/// Interface for VHDX (Virtual Hard Disk) management operations
/// </summary>
public interface IVhdxService
{
    /// <summary>
    /// Creates a new VHDX file with specified configuration
    /// </summary>
    Task<bool> CreateVhdxAsync(VhdxContainerConfig config, CancellationToken cancellationToken = default);

    /// <summary>
    /// Mounts a VHDX file to the specified drive letter
    /// </summary>
    Task<bool> MountVhdxAsync(string vhdxPath, char driveLetter, CancellationToken cancellationToken = default);

    /// <summary>
    /// Unmounts a VHDX from the specified drive letter
    /// </summary>
    Task<bool> UnmountVhdxAsync(char driveLetter, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the status of a mounted VHDX
    /// </summary>
    Task<VhdxMountStatus?> GetMountStatusAsync(char driveLetter, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists all currently mounted VHDX containers
    /// </summary>
    Task<IEnumerable<VhdxMountStatus>> GetMountedVhdxListAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a VHDX file
    /// </summary>
    Task<bool> DeleteVhdxAsync(string vhdxPath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Compacts a VHDX file to reduce disk space usage
    /// </summary>
    Task<bool> CompactVhdxAsync(string vhdxPath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resizes a VHDX file to a new size
    /// </summary>
    Task<bool> ResizeVhdxAsync(string vhdxPath, long newSizeGb, CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents the mount status of a VHDX container
/// </summary>
public class VhdxMountStatus
{
    public required string VhdxPath { get; set; }
    public char DriveLetter { get; set; }
    public long SizeBytes { get; set; }
    public long AvailableSpaceBytes { get; set; }
    public DateTime MountedAt { get; set; }
    public bool IsReadOnly { get; set; }
    public string? FileSystem { get; set; }
}
