namespace MonadoBlade.Security.Models;

/// <summary>
/// Represents the status of TPM 2.0 module
/// </summary>
public class TpmStatus
{
    public bool IsAvailable { get; set; }
    public bool IsReady { get; set; }
    public string? SpecVersion { get; set; }
    public string? FirmwareVersion { get; set; }
    public bool IsManagedBySystem { get; set; }
    public bool IsOwned { get; set; }
    public List<TpmPcr> PcrValues { get; set; } = new();
    public DateTime LastUpdated { get; set; }
}

/// <summary>
/// Represents a TPM Platform Configuration Register (PCR) value
/// </summary>
public class TpmPcr
{
    public int Index { get; set; }
    public string? HashAlgorithm { get; set; }
    public string? Value { get; set; }
}
