using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MonadoBlade.Security;

/// <summary>Unified security engine with encryption, audit, and malware detection</summary>
public interface ISecurityEngine
{
    Task<SecurityStatus> GetStatusAsync(CancellationToken ct = default);
    Task<bool> ValidateSystemIntegrityAsync(CancellationToken ct = default);
}

/// <summary>Security status information</summary>
public record SecurityStatus
{
    public bool IsEncrypted { get; init; }
    public bool HasActiveThreats { get; init; }
    public int ThreatCount { get; init; }
    public DateTime LastScanTime { get; init; }
    public float SecurityScore { get; init; } // 0-100
}

/// <summary>Vault manager for encrypted credential storage</summary>
public interface IVaultManager
{
    /// <summary>Store encrypted credential</summary>
    Task StoreSecretAsync(string key, string value, VaultSecretType type, CancellationToken ct = default);
    
    /// <summary>Retrieve decrypted credential</summary>
    Task<string?> GetSecretAsync(string key, CancellationToken ct = default);
    
    /// <summary>Delete secret from vault</summary>
    Task DeleteSecretAsync(string key, CancellationToken ct = default);
    
    /// <summary>List all stored secrets (names only, not values)</summary>
    Task<IEnumerable<string>> ListSecretsAsync(CancellationToken ct = default);
    
    /// <summary>Rotate secrets (re-encrypt with new key)</summary>
    Task RotateSecretsAsync(CancellationToken ct = default);
    
    /// <summary>Check vault integrity</summary>
    Task<bool> ValidateVaultIntegrityAsync(CancellationToken ct = default);
}

/// <summary>Vault secret type enumeration</summary>
public enum VaultSecretType
{
    APIKey,
    ConnectionString,
    Certificate,
    PrivateKey,
    Password,
    Token,
    Other
}

/// <summary>Quarantine manager for isolated malware storage</summary>
public interface IQuarantineManager
{
    /// <summary>Quarantine suspicious file (immutable, encrypted)</summary>
    Task<string> QuarantineFileAsync(string filePath, string reason, CancellationToken ct = default);
    
    /// <summary>List quarantined files</summary>
    Task<IEnumerable<QuarantinedFile>> ListQuarantinedFilesAsync(CancellationToken ct = default);
    
    /// <summary>Get detailed info about quarantined file</summary>
    Task<QuarantinedFileDetails?> GetFileDetailsAsync(string quarantineId, CancellationToken ct = default);
    
    /// <summary>Remove quarantined file permanently</summary>
    Task PermanentlyDeleteAsync(string quarantineId, CancellationToken ct = default);
    
    /// <summary>Check quarantine integrity (ensure no tampering)</summary>
    Task<bool> ValidateQuarantineIntegrityAsync(CancellationToken ct = default);
}

/// <summary>Quarantined file record</summary>
public record QuarantinedFile
{
    public string QuarantineId { get; init; } = string.Empty;
    public string OriginalFileName { get; init; } = string.Empty;
    public string QuarantineReason { get; init; } = string.Empty;
    public DateTime QuarantinedAt { get; init; }
    public long FileSizeBytes { get; init; }
    public string FileHash { get; init; } = string.Empty;
}

/// <summary>Detailed information about quarantined file</summary>
public record QuarantinedFileDetails : QuarantinedFile
{
    public List<SecurityThreat> DetectedThreats { get; init; } = new();
    public string VirusScannerVersion { get; init; } = string.Empty;
    public DateTime LastAnalyzedAt { get; init; }
}

/// <summary>Malware defense engine with multi-engine scanning</summary>
public interface IMalwareDefenseEngine
{
    /// <summary>Scan file with multiple malware engines</summary>
    Task<FileScanResult> ScanFileAsync(string filePath, CancellationToken ct = default);
    
    /// <summary>Scan directory recursively</summary>
    Task<DirectoryScanResult> ScanDirectoryAsync(string directoryPath, bool recursive = true, CancellationToken ct = default);
    
    /// <summary>Perform system-wide security scan</summary>
    Task<SystemScanResult> ScanSystemAsync(CancellationToken ct = default);
    
    /// <summary>Update malware definitions</summary>
    Task UpdateDefinitionsAsync(CancellationToken ct = default);
    
    /// <summary>Enable real-time protection (monitoring)</summary>
    Task EnableRealtimeProtectionAsync(CancellationToken ct = default);
    
    /// <summary>Get malware engine status</summary>
    Task<MalwareEngineStatus> GetStatusAsync(CancellationToken ct = default);
}

/// <summary>File scan result</summary>
public record FileScanResult
{
    public string FilePath { get; init; } = string.Empty;
    public bool IsThreatDetected { get; init; }
    public List<SecurityThreat> Threats { get; init; } = new();
    public DateTime ScanTime { get; init; }
    public TimeSpan ScanDuration { get; init; }
}

/// <summary>Directory scan result</summary>
public record DirectoryScanResult
{
    public string DirectoryPath { get; init; } = string.Empty;
    public int FilesScanned { get; init; }
    public int ThreatsFound { get; init; }
    public List<FileScanResult> FileResults { get; init; } = new();
    public DateTime ScanStartTime { get; init; }
    public TimeSpan TotalScanDuration { get; init; }
}

/// <summary>System-wide scan result</summary>
public record SystemScanResult
{
    public int TotalFilesScanned { get; init; }
    public int TotalThreatsFound { get; init; }
    public List<SecurityThreat> CriticalThreats { get; init; } = new();
    public DateTime ScanStartTime { get; init; }
    public TimeSpan TotalScanDuration { get; init; }
    public float SystemHealthScore { get; init; } // 0-100
}

/// <summary>Security threat representation</summary>
public record SecurityThreat
{
    public string ThreatId { get; init; } = string.Empty;
    public string ThreatName { get; init; } = string.Empty;
    public ThreatSeverity Severity { get; init; }
    public string ThreatType { get; init; } = string.Empty; // malware, PUP, exploit, etc.
    public string DetectionEngine { get; init; } = string.Empty;
    public string RecommendedAction { get; init; } = string.Empty;
    public DateTime DetectedAt { get; init; }
}

/// <summary>Threat severity levels</summary>
public enum ThreatSeverity
{
    Low = 0,
    Medium = 1,
    High = 2,
    Critical = 3
}

/// <summary>Malware engine status</summary>
public record MalwareEngineStatus
{
    public bool IsEnabled { get; init; }
    public bool IsRealtimeProtectionActive { get; init; }
    public DateTime LastDefinitionUpdate { get; init; }
    public string EngineVersion { get; init; } = string.Empty;
    public int DefinitionCount { get; init; }
}

/// <summary>NTFS ACL enforcer for user isolation</summary>
public interface IACLEnforcer
{
    /// <summary>Grant user access to resource</summary>
    Task GrantAccessAsync(string userSID, string resourcePath, AccessPermission permission, CancellationToken ct = default);
    
    /// <summary>Revoke user access from resource</summary>
    Task RevokeAccessAsync(string userSID, string resourcePath, CancellationToken ct = default);
    
    /// <summary>Set explicit deny for resource (prevents access)</summary>
    Task SetExplicitDenyAsync(string userSID, string resourcePath, CancellationToken ct = default);
    
    /// <summary>Get ACL for resource</summary>
    Task<ResourceACL> GetResourceACLAsync(string resourcePath, CancellationToken ct = default);
    
    /// <summary>Validate ACL compliance</summary>
    Task<bool> ValidateComplianceAsync(CancellationToken ct = default);
}

/// <summary>Access permission levels</summary>
[Flags]
public enum AccessPermission
{
    Read = 1,
    Write = 2,
    Execute = 4,
    Modify = 8,
    FullControl = 15
}

/// <summary>Resource ACL representation</summary>
public record ResourceACL
{
    public string ResourcePath { get; init; } = string.Empty;
    public Dictionary<string, AccessPermission> UserPermissions { get; init; } = new();
    public List<string> ExplicitDenies { get; init; } = new();
    public string Owner { get; init; } = string.Empty;
}

/// <summary>Audit logger for security events</summary>
public interface IAuditLogger
{
    /// <summary>Log security event</summary>
    Task LogSecurityEventAsync(SecurityAuditEvent auditEvent, CancellationToken ct = default);
    
    /// <summary>Query audit logs</summary>
    Task<IEnumerable<SecurityAuditEvent>> QueryAuditLogsAsync(
        AuditLogFilter filter,
        CancellationToken ct = default);
    
    /// <summary>Export audit logs (for compliance)</summary>
    Task ExportAuditLogsAsync(string exportPath, DateTime from, DateTime to, CancellationToken ct = default);
    
    /// <summary>Get audit log statistics</summary>
    Task<AuditLogStatistics> GetStatisticsAsync(DateTime from, DateTime to, CancellationToken ct = default);
}

/// <summary>Security audit event</summary>
public record SecurityAuditEvent
{
    public string EventId { get; init; } = Guid.NewGuid().ToString();
    public DateTime EventTime { get; init; } = DateTime.UtcNow;
    public string EventType { get; init; } = string.Empty; // FileAccess, Encryption, ThreatDetected, etc.
    public string UserId { get; init; } = string.Empty;
    public string ResourcePath { get; init; } = string.Empty;
    public string Action { get; init; } = string.Empty;
    public bool Success { get; init; }
    public string Details { get; init; } = string.Empty;
    public AuditEventSeverity Severity { get; init; }
}

/// <summary>Audit event severity</summary>
public enum AuditEventSeverity
{
    Information = 0,
    Warning = 1,
    Error = 2,
    Critical = 3
}

/// <summary>Audit log filter</summary>
public record AuditLogFilter
{
    public DateTime FromTime { get; init; }
    public DateTime ToTime { get; init; }
    public string? EventType { get; init; }
    public string? UserId { get; init; }
    public AuditEventSeverity? MinimumSeverity { get; init; }
    public bool? SuccessOnly { get; init; }
}

/// <summary>Audit log statistics</summary>
public record AuditLogStatistics
{
    public int TotalEvents { get; init; }
    public int FailedEvents { get; init; }
    public Dictionary<string, int> EventTypeCount { get; init; } = new();
    public Dictionary<string, int> UserActivityCount { get; init; } = new();
    public AuditEventSeverity MostSeverity { get; init; }
}

/// <summary>Ransomware detection and prevention</summary>
public interface IRansomwareDetector
{
    /// <summary>Start real-time ransomware monitoring</summary>
    Task StartMonitoringAsync(CancellationToken ct = default);
    
    /// <summary>Stop real-time monitoring</summary>
    Task StopMonitoringAsync(CancellationToken ct = default);
    
    /// <summary>Detect suspicious file activity patterns</summary>
    Task<RansomwareDetectionResult> AnalyzeActivityAsync(CancellationToken ct = default);
    
    /// <summary>Create file snapshot for recovery</summary>
    Task<string> CreateSnapshotAsync(string directoryPath, CancellationToken ct = default);
    
    /// <summary>Restore files from snapshot</summary>
    Task RestoreFromSnapshotAsync(string snapshotId, CancellationToken ct = default);
}

/// <summary>Ransomware detection result</summary>
public record RansomwareDetectionResult
{
    public bool IsRiskyActivityDetected { get; init; }
    public float SuspicionScore { get; init; } // 0-100
    public List<string> SuspiciousProcesses { get; init; } = new();
    public List<string> LockedFiles { get; init; } = new();
    public string RecommendedAction { get; init; } = string.Empty;
}
