namespace MonadoBlade.Security.Models;

/// <summary>
/// Represents an encrypted vault container
/// </summary>
public class VaultContainer
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public required string VhdxPath { get; set; }
    public char MountDrive { get; set; }
    public VaultContainerState State { get; set; } = VaultContainerState.Unmounted;
    public VhdxEncryptionType EncryptionType { get; set; }
    public long SizeBytes { get; set; }
    public long UsedSpaceBytes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime LastMountedAt { get; set; }
    public bool IsMounted { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}

public enum VaultContainerState
{
    Unmounted = 0,
    Mounting = 1,
    Mounted = 2,
    Unmounting = 3,
    Corrupted = 4,
    Locked = 5,
}
