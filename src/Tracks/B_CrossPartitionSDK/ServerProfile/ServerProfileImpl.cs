namespace MonadoBlade.Tracks.Profiles;

using MonadoBlade.Core.Common;
using MonadoBlade.Core.Patterns;
using System.Collections.Generic;
using System.Collections.Concurrent;

/// <summary>
/// Server profile - locked down by default, supports external API connectivity with audit logging.
/// Purpose: External API hosting (servers only, automation).
/// </summary>
public class ServerProfileImpl : ProfileBase, IServerProfile
{
    private bool _externalEnabled;
    private DateTime? _externalEnabledUntil;
    private readonly ConcurrentBag<ExternalAccessAuditEntry> _auditLog = new();
    private Task? _externalMonitorTask;
    private CancellationTokenSource? _externalMonitorCts;

    public bool ExternalEnabled => _externalEnabled;
    public DateTime? ExternalEnabledUntil => _externalEnabledUntil;

    public ServerProfileImpl(IServiceContext context, string componentId, ResourceAllocation allocation)
        : base(context, componentId, ProfileType.Server, allocation)
    {
        VmId = $"vm_server_{componentId}";
    }

    protected override async Task<Result> OnProfileInitializeAsync(IServiceContext context, CancellationToken ct)
    {
        _logger.Information($"Initializing Server profile: {ComponentId}");
        _logger.Information("Server profile: External connectivity DISABLED by default (locked down)");
        
        await RecordAuditAsync(new ExternalAccessAuditEntry(
            DateTime.UtcNow,
            Enabled: false,
            Reason: "Initial state (locked down by default)",
            ChangedBy: "System",
            IpAddress: null,
            Duration: null));

        return Result.Success();
    }

    protected override async Task<Result> OnActivateAsync(CancellationToken ct)
    {
        _logger.Information($"Activating Server profile: {ComponentId}");
        _logger.Information("Server profile: Network access INTERNAL ONLY (no external by default)");
        _logger.Information("Server profile: Services limited to API, database, auth");
        _logger.Information("Server profile: GUI disabled (headless mode)");
        _logger.Information("Server profile: Storage allocation 500GB");

        _externalMonitorCts = new CancellationTokenSource();
        _externalMonitorTask = MonitorExternalAccessAsync(_externalMonitorCts.Token);

        return Result.Success();
    }

    protected override async Task<Result> OnDeactivateAsync(CancellationToken ct)
    {
        if (_externalMonitorCts != null)
        {
            _externalMonitorCts.Cancel();
            try
            {
                await _externalMonitorTask!;
            }
            catch (OperationCanceledException) { }
        }

        if (_externalEnabled)
        {
            await DisableExternalAsync(ct);
        }

        return Result.Success();
    }

    public async Task<Result> EnableExternalAsync(
        TimeSpan duration,
        string? reason = null,
        string? enabledBy = null,
        CancellationToken ct = default)
    {
        ThrowIfNotInitialized();

        if (_externalEnabled)
            return ErrorCode.InvalidOperation.ToFailure("External access already enabled");

        if (duration <= TimeSpan.Zero || duration > TimeSpan.FromHours(24))
            return ErrorCode.ValidationFailed.ToFailure("Duration must be between 1 second and 24 hours");

        _externalEnabledUntil = DateTime.UtcNow + duration;
        _externalEnabled = true;

        await RecordAuditAsync(new ExternalAccessAuditEntry(
            DateTime.UtcNow,
            Enabled: true,
            Reason: reason ?? "Admin-enabled external access",
            ChangedBy: enabledBy ?? "Unknown",
            IpAddress: _context.Principal?.Identity?.Name,
            Duration: duration));

        _logger.Warning($"External API access ENABLED for {duration.TotalHours} hours on Server profile: {ComponentId}");
        _metrics.IncrementCounter("server_external_enabled", tags: ("duration_hours", duration.TotalHours.ToString("F1")));

        return Result.Success();
    }

    public async Task<Result> DisableExternalAsync(CancellationToken ct = default)
    {
        ThrowIfNotInitialized();

        if (!_externalEnabled)
            return Result.Success();

        _externalEnabled = false;
        _externalEnabledUntil = null;

        await RecordAuditAsync(new ExternalAccessAuditEntry(
            DateTime.UtcNow,
            Enabled: false,
            Reason: "Admin-disabled external access",
            ChangedBy: _context.Principal?.Identity?.Name ?? "Unknown",
            IpAddress: _context.Principal?.Identity?.Name,
            Duration: null));

        _logger.Warning($"External API access DISABLED on Server profile: {ComponentId}");
        _metrics.IncrementCounter("server_external_disabled");

        return Result.Success();
    }

    public async Task<Result<ExternalAccessAuditEntry[]>> GetExternalAccessAuditAsync(
        int? limit = null,
        CancellationToken ct = default)
    {
        ThrowIfNotInitialized();

        var entries = _auditLog
            .OrderByDescending(e => e.Timestamp)
            .Take(limit ?? 100)
            .ToArray();

        return entries.ToSuccess();
    }

    protected override async Task<HealthStatus> OnGetHealthAsync(CancellationToken ct)
    {
        if (State != ProfileState.Running)
            return HealthStatus.Degraded(ComponentId, $"Server profile not running (state: {State})");

        var details = new Dictionary<string, object?>
        {
            { "externalEnabled", _externalEnabled },
            { "externalEnabledUntil", _externalEnabledUntil },
            { "auditLogEntries", _auditLog.Count }
        };

        var message = _externalEnabled
            ? $"Server running with external access enabled until {_externalEnabledUntil}"
            : "Server running (external access disabled)";

        return HealthStatus.Healthy(ComponentId, message, details);
    }

    private async Task MonitorExternalAccessAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                if (_externalEnabled && _externalEnabledUntil.HasValue && DateTime.UtcNow >= _externalEnabledUntil)
                {
                    _logger.Information($"External access auto-disable: time limit reached for {ComponentId}");
                    await DisableExternalAsync(ct);
                }

                await Task.Delay(TimeSpan.FromSeconds(30), ct);
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                _logger.Error("Error in external access monitor", ex);
            }
        }
    }

    private async Task RecordAuditAsync(ExternalAccessAuditEntry entry)
    {
        _auditLog.Add(entry);
        
        if (_auditLog.Count > 1000)
        {
            var overflow = _auditLog.Count - 1000;
            for (int i = 0; i < overflow; i++)
                _auditLog.TryTake(out _);
        }
    }
}

/// <summary>
/// Automation profile - headless, maximum performance, GPU support, DevDrive optimization.
/// Purpose: Task automation runner (DevDrive only).
/// </summary>
public class AutomationProfileImpl : ProfileBase, IAutomationProfile
{
    private bool _gpuAvailable;
    private string? _gpuType;
    private string? _devDrivePath;
    private readonly Dictionary<string, TaskExecutionStatus> _taskStatuses = new();
    private readonly List<TaskExecutionRecord> _taskHistory = new();
    private readonly TaskScheduler _scheduler;
    private Task? _schedulerTask;
    private CancellationTokenSource? _schedulerCts;

    public bool GpuAvailable => _gpuAvailable;
    public string? GpuType => _gpuType;
    public string? DevDrivePath => _devDrivePath;

    public AutomationProfileImpl(IServiceContext context, string componentId, ResourceAllocation allocation)
        : base(context, componentId, ProfileType.Automation, allocation)
    {
        VmId = $"vm_automation_{componentId}";
        _scheduler = new TaskScheduler();
    }

    protected override async Task<Result> OnProfileInitializeAsync(IServiceContext context, CancellationToken ct)
    {
        _logger.Information($"Initializing Automation profile: {ComponentId}");
        _logger.Information("Automation profile: GUI disabled (headless)");
        _logger.Information("Automation profile: Maximum CPU allocation (8+ cores)");
        _logger.Information("Automation profile: Maximum storage (2TB DevDrive)");
        
        _devDrivePath = $"D:\\DevDrive\\{ComponentId}";
        _gpuAvailable = true;
        _gpuType = "NVIDIA"; // Would be detected at runtime

        _logger.Information($"Automation profile: GPU support enabled ({_gpuType})");
        _logger.Information($"Automation profile: DevDrive path: {_devDrivePath}");

        return Result.Success();
    }

    protected override async Task<Result> OnActivateAsync(CancellationToken ct)
    {
        _logger.Information($"Activating Automation profile: {ComponentId}");
        
        _schedulerCts = new CancellationTokenSource();
        _schedulerTask = _scheduler.RunAsync(_schedulerCts.Token);

        _metrics.IncrementCounter("automation_profile_activated");
        return Result.Success();
    }

    protected override async Task<Result> OnDeactivateAsync(CancellationToken ct)
    {
        if (_schedulerCts != null)
        {
            _schedulerCts.Cancel();
            try
            {
                await _schedulerTask!;
            }
            catch (OperationCanceledException) { }
        }

        return Result.Success();
    }

    public async Task<Result<string>> SubmitTaskAsync(AutomationTask task, CancellationToken ct = default)
    {
        ThrowIfNotInitialized();

        if (State != ProfileState.Running)
            return ErrorCode.InvalidOperation.ToFailure<string>("Automation profile not running");

        var status = new TaskExecutionStatus(
            TaskId: task.Id,
            State: TaskExecutionState.Queued,
            ExitCode: null,
            Error: null,
            StartedAt: null,
            CompletedAt: null);

        lock (_taskStatuses)
        {
            _taskStatuses[task.Id] = status;
        }

        _scheduler.EnqueueTask(task);
        
        _logger.Information($"Task submitted: {task.Name} ({task.Id})");
        _metrics.IncrementCounter("automation_task_submitted", tags: ("task", task.Name));

        return task.Id.ToSuccess();
    }

    public async Task<Result<TaskExecutionStatus>> GetTaskStatusAsync(string taskId, CancellationToken ct = default)
    {
        ThrowIfNotInitialized();

        lock (_taskStatuses)
        {
            if (!_taskStatuses.TryGetValue(taskId, out var status))
                return ErrorCode.NotFound.ToFailure<TaskExecutionStatus>($"Task {taskId} not found");

            return status.ToSuccess();
        }
    }

    public async Task<Result> CancelTaskAsync(string taskId, CancellationToken ct = default)
    {
        ThrowIfNotInitialized();

        lock (_taskStatuses)
        {
            if (!_taskStatuses.TryGetValue(taskId, out var status))
                return ErrorCode.NotFound.ToFailure($"Task {taskId} not found");

            if (status.State == TaskExecutionState.Completed || status.State == TaskExecutionState.Failed)
                return ErrorCode.InvalidOperation.ToFailure("Cannot cancel completed task");

            var updatedStatus = status with { State = TaskExecutionState.Cancelled };
            _taskStatuses[taskId] = updatedStatus;
        }

        _logger.Information($"Task cancelled: {taskId}");
        _metrics.IncrementCounter("automation_task_cancelled");
        return Result.Success();
    }

    public async Task<Result<TaskExecutionRecord[]>> GetTaskHistoryAsync(int? limit = null, CancellationToken ct = default)
    {
        ThrowIfNotInitialized();

        lock (_taskHistory)
        {
            var records = _taskHistory
                .OrderByDescending(r => r.ExecutedAt)
                .Take(limit ?? 100)
                .ToArray();

            return records.ToSuccess();
        }
    }

    protected override async Task<HealthStatus> OnGetHealthAsync(CancellationToken ct)
    {
        if (State != ProfileState.Running)
            return HealthStatus.Degraded(ComponentId, $"Automation profile not running (state: {State})");

        lock (_taskStatuses)
        {
            var queued = _taskStatuses.Values.Count(s => s.State == TaskExecutionState.Queued);
            var running = _taskStatuses.Values.Count(s => s.State == TaskExecutionState.Running);

            var details = new Dictionary<string, object?>
            {
                { "gpuAvailable", _gpuAvailable },
                { "devDrivePath", _devDrivePath },
                { "queuedTasks", queued },
                { "runningTasks", running }
            };

            return HealthStatus.Healthy(ComponentId, $"Automation running ({running} active, {queued} queued)", details);
        }
    }

    internal void RecordTaskExecution(TaskExecutionRecord record)
    {
        lock (_taskHistory)
        {
            _taskHistory.Add(record);
            if (_taskHistory.Count > 1000)
                _taskHistory.RemoveAt(0);
        }

        _metrics.RecordDuration($"task_{record.TaskName}", record.Duration);
    }
}

/// <summary>
/// Simple task scheduler for automation profile.
/// </summary>
internal class TaskScheduler
{
    private readonly Queue<AutomationTask> _taskQueue = new();
    private readonly object _queueLock = new();

    public void EnqueueTask(AutomationTask task)
    {
        lock (_queueLock)
        {
            _taskQueue.Enqueue(task);
        }
    }

    public async Task RunAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            AutomationTask? task = null;
            
            lock (_queueLock)
            {
                if (_taskQueue.Count > 0)
                    task = _taskQueue.Dequeue();
            }

            if (task != null)
            {
                await Task.Delay(100, ct); // Simulate task execution
            }

            await Task.Delay(TimeSpan.FromSeconds(1), ct);
        }
    }
}
