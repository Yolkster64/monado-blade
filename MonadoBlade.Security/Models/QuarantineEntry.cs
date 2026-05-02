namespace MonadoBlade.Security.Models;

/// <summary>
/// Represents a quarantined item for threat containment
/// </summary>
public class QuarantineEntry
{
    public required string Id { get; set; }
    public required string FilePath { get; set; }
    public required string OriginalFileName { get; set; }
    public required string ThreatLevel { get; set; }
    public required string Reason { get; set; }
    public DateTime QuarantinedAt { get; set; }
    public string? DetectionSignature { get; set; }
    public long FileSizeBytes { get; set; }
    public string? FileHash { get; set; }
    public QuarantineStatus Status { get; set; } = QuarantineStatus.Isolated;
    public Dictionary<string, object> Metadata { get; set; } = new();
}

public enum QuarantineStatus
{
    Isolated = 0,
    AwaitingAnalysis = 1,
    Analyzed = 2,
    Approved = 3,
    Deleted = 4,
    Restored = 5,
}
