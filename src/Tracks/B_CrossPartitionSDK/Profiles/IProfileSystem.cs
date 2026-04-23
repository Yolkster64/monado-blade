namespace MonadoBlade.Tracks.Profiles;

using MonadoBlade.Core.Common;
using System.Collections.Generic;

/// <summary>
/// Profile type enumeration. Defines different system profiles for specialized workloads.
/// User = daily use with full features, Server = external API hosting, Automation = batch jobs.
/// </summary>
public enum ProfileType
{
    User,
    Server,
    Automation,
    Shared
}

/// <summary>
/// Profile lifecycle states.
/// </summary>
public enum ProfileState
{
    Disabled,
    Configuring,
    Starting,
    Running,
    Stopping,
    Stopped,
    Error
}

/// <summary>
/// Resource allocation configuration for profiles.
/// </summary>
public record ResourceAllocation(
    int CpuCores = 4,
    int MemoryGb = 8,
    int StorageGb = 500,
    bool? ExclusiveGpu = null)
{
    public static ResourceAllocation UserDefault() =>
        new(CpuCores: 8, MemoryGb: 16, StorageGb: 3000);
    
    public static ResourceAllocation ServerDefault() =>
        new(CpuCores: 4, MemoryGb: 8, StorageGb: 500);
    
    public static ResourceAllocation AutomationDefault() =>
        new(CpuCores: 8, MemoryGb: 16, StorageGb: 2000, ExclusiveGpu: true);
}

/// <summary>
/// Core profile interface. All profiles implement this.
/// </summary>
public interface IProfile : IServiceComponent
{
    /// <summary>Gets the profile type.</summary>
    ProfileType ProfileType { get; }
    
    /// <summary>Gets the current profile state.</summary>
    ProfileState State { get; }
    
    /// <summary>Gets resource allocation for this profile.</summary>
    ResourceAllocation Resources { get; }
    
    /// <summary>Gets the associated VM ID (if applicable).</summary>
    string? VmId { get; }
    
    /// <summary>Gets the profile creation timestamp.</summary>
    DateTime CreatedAt { get; }
    
    /// <summary>Activates the profile (transitions to Running state).</summary>
    Task<Result> ActivateAsync(CancellationToken ct = default);
    
    /// <summary>Deactivates the profile (transitions to Stopped state).</summary>
    Task<Result> DeactivateAsync(CancellationToken ct = default);
    
    /// <summary>Retrieves profile-specific configuration.</summary>
    Task<Result<Dictionary<string, object?>>> GetConfigurationAsync(CancellationToken ct = default);
    
    /// <summary>Updates profile-specific configuration.</summary>
    Task<Result> UpdateConfigurationAsync(Dictionary<string, object?> config, CancellationToken ct = default);
}

/// <summary>
/// Profile management interface. Handles profile lifecycle and switching.
/// </summary>
public interface IProfileManager : IServiceComponent
{
    /// <summary>Gets the currently active profile.</summary>
    IProfile? ActiveProfile { get; }
    
    /// <summary>Gets all available profiles.</summary>
    Task<Result<IProfile[]>> GetAvailableProfilesAsync(CancellationToken ct = default);
    
    /// <summary>Creates a new profile with specified type and resources.</summary>
    Task<Result<IProfile>> CreateProfileAsync(
        ProfileType type,
        ResourceAllocation? resources = null,
        CancellationToken ct = default);
    
    /// <summary>Switches to the specified profile (activates it, deactivates current).</summary>
    Task<Result> SwitchProfileAsync(string profileId, CancellationToken ct = default);
    
    /// <summary>Switches profile by type (if multiple profiles of same type, uses first).</summary>
    Task<Result> SwitchProfileByTypeAsync(ProfileType type, CancellationToken ct = default);
    
    /// <summary>Deletes a profile and its associated resources.</summary>
    Task<Result> DeleteProfileAsync(string profileId, bool force = false, CancellationToken ct = default);
    
    /// <summary>Gets profile by ID.</summary>
    Task<Result<IProfile>> GetProfileAsync(string profileId, CancellationToken ct = default);
    
    /// <summary>Listens for profile state changes.</summary>
    IDisposable WatchProfile(string profileId, Action<IProfile> onChanged);
    
    /// <summary>Performs profile isolation verification (ensures data not leaked between profiles).</summary>
    Task<Result> VerifyProfileIsolationAsync(CancellationToken ct = default);
}

/// <summary>
/// Provides context information for profile operations.
/// </summary>
public interface IProfileContext
{
    /// <summary>Gets the profile associated with this context.</summary>
    IProfile Profile { get; }
    
    /// <summary>Gets the service context for the profile VM.</summary>
    IServiceContext VmContext { get; }
    
    /// <summary>Gets the profile configuration provider.</summary>
    IConfigurationProvider ProfileConfig { get; }
    
    /// <summary>Gets the profile's resource monitor.</summary>
    IResourceMonitor ResourceMonitor { get; }
}

/// <summary>
/// Monitors profile resource usage in real-time.
/// </summary>
public interface IResourceMonitor : IAsyncDisposable
{
    /// <summary>Gets current CPU usage percentage.</summary>
    double CurrentCpuUsage { get; }
    
    /// <summary>Gets current memory usage in MB.</summary>
    long CurrentMemoryMb { get; }
    
    /// <summary>Gets current storage usage in MB.</summary>
    long CurrentStorageMb { get; }
    
    /// <summary>Gets network I/O in bytes per second.</summary>
    long NetworkIoBytesPerSec { get; }
    
    /// <summary>Gets disk I/O in bytes per second.</summary>
    long DiskIoBytesPerSec { get; }
    
    /// <summary>Records historical resource snapshot.</summary>
    Task<Result> RecordSnapshotAsync(CancellationToken ct = default);
    
    /// <summary>Gets resource usage history for specified duration.</summary>
    Task<Result<ResourceSnapshot[]>> GetHistoryAsync(
        TimeSpan duration,
        CancellationToken ct = default);
}

/// <summary>
/// Snapshot of resource usage at a point in time.
/// </summary>
public record ResourceSnapshot(
    DateTime Timestamp,
    double CpuUsagePercent,
    long MemoryMb,
    long StorageMb,
    long NetworkIoBytesPerSec,
    long DiskIoBytesPerSec);

/// <summary>
/// Server profile specific interface (extends IProfile).
/// Handles external API gateway and security features.
/// </summary>
public interface IServerProfile : IProfile
{
    /// <summary>Indicates if external connectivity is enabled.</summary>
    bool ExternalEnabled { get; }
    
    /// <summary>Gets the time when external access expires (null if never set).</summary>
    DateTime? ExternalEnabledUntil { get; }
    
    /// <summary>Enables external API connectivity with time limit.</summary>
    Task<Result> EnableExternalAsync(
        TimeSpan duration,
        string? reason = null,
        string? enabledBy = null,
        CancellationToken ct = default);
    
    /// <summary>Disables external API connectivity immediately.</summary>
    Task<Result> DisableExternalAsync(CancellationToken ct = default);
    
    /// <summary>Gets audit log of external access enable/disable events.</summary>
    Task<Result<ExternalAccessAuditEntry[]>> GetExternalAccessAuditAsync(
        int? limit = null,
        CancellationToken ct = default);
}

/// <summary>
/// Audit entry for external access enable/disable events.
/// </summary>
public record ExternalAccessAuditEntry(
    DateTime Timestamp,
    bool Enabled,
    string? Reason,
    string? ChangedBy,
    string? IpAddress,
    TimeSpan? Duration);

/// <summary>
/// Automation profile specific interface (extends IProfile).
/// Handles task scheduling and resource-intensive operations.
/// </summary>
public interface IAutomationProfile : IProfile
{
    /// <summary>Indicates if GPU access is available.</summary>
    bool GpuAvailable { get; }
    
    /// <summary>Gets the type of GPU (e.g., "NVIDIA", "Arc").</summary>
    string? GpuType { get; }
    
    /// <summary>Gets the DevDrive storage path for fast I/O operations.</summary>
    string? DevDrivePath { get; }
    
    /// <summary>Submits a task for execution on this automation profile.</summary>
    Task<Result<string>> SubmitTaskAsync(
        AutomationTask task,
        CancellationToken ct = default);
    
    /// <summary>Gets task execution status by task ID.</summary>
    Task<Result<TaskExecutionStatus>> GetTaskStatusAsync(
        string taskId,
        CancellationToken ct = default);
    
    /// <summary>Cancels a running task.</summary>
    Task<Result> CancelTaskAsync(string taskId, CancellationToken ct = default);
    
    /// <summary>Gets task execution history.</summary>
    Task<Result<TaskExecutionRecord[]>> GetTaskHistoryAsync(
        int? limit = null,
        CancellationToken ct = default);
}

/// <summary>
/// Automation task definition.
/// </summary>
public record AutomationTask(
    string Name,
    string Command,
    Dictionary<string, string>? Environment = null,
    TimeSpan? Timeout = null,
    ResourceAllocation? ResourceLimit = null,
    string[]? Dependencies = null,
    int? MaxRetries = 3)
{
    public string Id { get; } = Guid.NewGuid().ToString();
}

/// <summary>
/// Task execution status.
/// </summary>
public record TaskExecutionStatus(
    string TaskId,
    TaskExecutionState State,
    int? ExitCode = null,
    string? Error = null,
    DateTime? StartedAt = null,
    DateTime? CompletedAt = null,
    ResourceSnapshot? ResourceUsage = null);

public enum TaskExecutionState
{
    Queued,
    Running,
    Completed,
    Failed,
    Cancelled,
    TimedOut
}

/// <summary>
/// Historical record of task execution.
/// </summary>
public record TaskExecutionRecord(
    string TaskId,
    string TaskName,
    DateTime ExecutedAt,
    TaskExecutionState State,
    int? ExitCode,
    TimeSpan Duration,
    ResourceSnapshot? ResourceUsage);

/// <summary>
/// SDK interface for profile operations (used by SDKAggregator).
/// </summary>
public interface ISDKProvider : IServiceComponent
{
    /// <summary>Gets the SDK name.</summary>
    string SDKName { get; }
    
    /// <summary>Gets the target platform.</summary>
    string TargetPlatform { get; }
    
    /// <summary>Gets the SDK version.</summary>
    Version SDKVersion { get; }
    
    /// <summary>Executes an operation through the SDK.</summary>
    Task<Result<object?>> ExecuteAsync(
        string operation,
        Dictionary<string, object?> parameters,
        CancellationToken ct = default);
    
    /// <summary>Gets list of available operations.</summary>
    Task<Result<string[]>> GetAvailableOperationsAsync(CancellationToken ct = default);
}
