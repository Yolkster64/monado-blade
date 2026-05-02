namespace MonadoBlade.Security.Models;

/// <summary>
/// Configuration for VHDX container specifications
/// </summary>
public class VhdxContainerConfig
{
    public required string Name { get; set; }
    public required string Path { get; set; }
    public required char DriveLetter { get; set; }
    public required long SizeGb { get; set; }
    public bool IsFixed { get; set; } = false;
    public bool IsReadOnly { get; set; } = false;
    public VhdxEncryptionType EncryptionType { get; set; } = VhdxEncryptionType.None;
    public bool RequireTpm { get; set; } = false;
    public bool AutoMount { get; set; } = false;
    public VhdxMountMode MountMode { get; set; } = VhdxMountMode.ReadWrite;
}

public enum VhdxEncryptionType
{
    None = 0,
    BitLockerAes256 = 1,
    BitLockerXts = 2,
    Tpm20Sealed = 3,
}

public enum VhdxMountMode
{
    ReadWrite = 0,
    ReadOnly = 1,
    VirtualDiskOnly = 2,
}
