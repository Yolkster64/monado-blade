namespace MonadoBlade.Security.Models;

/// <summary>
/// Represents the status of BitLocker encryption
/// </summary>
public class BitLockerStatus
{
    public required string VolumeId { get; set; }
    public BitLockerEncryptionState EncryptionState { get; set; }
    public BitLockerConversionStatus ConversionStatus { get; set; }
    public double PercentageEncrypted { get; set; }
    public DateTime LastUpdated { get; set; }
    public string? ProtectionStatus { get; set; }
    public List<string> KeyProtectors { get; set; } = new();
}

public enum BitLockerEncryptionState
{
    Unknown = 0,
    Decrypted = 1,
    Decrypting = 2,
    Encrypted = 3,
    Encrypting = 4,
    SuspendedEncryption = 5,
    SuspendedDecryption = 6,
}

public enum BitLockerConversionStatus
{
    Unknown = 0,
    FullyDecrypted = 1,
    FullyEncrypted = 2,
    EncryptionInProgress = 3,
    DecryptionInProgress = 4,
    EncryptionPaused = 5,
    DecryptionPaused = 6,
}
