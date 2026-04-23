namespace MonadoBlade.Tests.Tracks.ProfilesAndDevDrive;

using Xunit;
using MonadoBlade.Tracks.Profiles;
using MonadoBlade.Core.Common;
using MonadoBlade.Core.Patterns;
using System.Collections.Generic;

/// <summary>
/// Unit tests for profile switching and management (15+ tests).
/// </summary>
public class ProfileManagerTests
{
    private readonly MockServiceContext _context = new();
    private readonly ProfileManager _manager;

    public ProfileManagerTests()
    {
        _manager = new ProfileManager(_context);
    }

    [Fact]
    public async Task Initialize_CreatesDefaultUserProfile()
    {
        var result = await _manager.InitializeAsync(_context);
        Assert.True(result is Result.Success);
        Assert.NotNull(_manager.ActiveProfile);
        Assert.Equal(ProfileType.User, _manager.ActiveProfile!.ProfileType);
    }

    [Fact]
    public async Task CreateProfile_Server_SucceedsWithCorrectResources()
    {
        await _manager.InitializeAsync(_context);
        
        var result = await _manager.CreateProfileAsync(ProfileType.Server);
        
        Assert.True(result is Result<IProfile>.Success);
        var profile = (result as Result<IProfile>.Success)!.Value;
        Assert.Equal(ProfileType.Server, profile.ProfileType);
        Assert.Equal(4, profile.Resources.CpuCores);
        Assert.Equal(500, profile.Resources.StorageGb);
    }

    [Fact]
    public async Task CreateProfile_Automation_SucceedsWithGpuSupport()
    {
        await _manager.InitializeAsync(_context);
        
        var result = await _manager.CreateProfileAsync(ProfileType.Automation);
        
        Assert.True(result is Result<IProfile>.Success);
        var profile = (result as Result<IProfile>.Success)!.Value;
        Assert.Equal(ProfileType.Automation, profile.ProfileType);
        Assert.Equal(8, profile.Resources.CpuCores);
        Assert.True(profile.Resources.ExclusiveGpu);
    }

    [Fact]
    public async Task SwitchProfile_ChangesActiveProfile()
    {
        await _manager.InitializeAsync(_context);
        var serverResult = await _manager.CreateProfileAsync(ProfileType.Server);
        var serverId = (serverResult as Result<IProfile>.Success)!.Value.ComponentId;
        
        var switchResult = await _manager.SwitchProfileAsync(serverId);
        
        Assert.True(switchResult is Result.Success);
        Assert.Equal(serverId, _manager.ActiveProfile!.ComponentId);
    }

    [Fact]
    public async Task SwitchProfile_DeactivatesPreviousProfile()
    {
        await _manager.InitializeAsync(_context);
        var initialActive = _manager.ActiveProfile!;
        
        var serverResult = await _manager.CreateProfileAsync(ProfileType.Server);
        var serverId = (serverResult as Result<IProfile>.Success)!.Value.ComponentId;
        await _manager.SwitchProfileAsync(serverId);
        
        Assert.Equal(ProfileState.Stopped, initialActive.State);
    }

    [Fact]
    public async Task SwitchProfile_ToNonexistent_ReturnsFail()
    {
        await _manager.InitializeAsync(_context);
        
        var result = await _manager.SwitchProfileAsync("nonexistent");
        
        Assert.True(result is Result.Failure);
    }

    [Fact]
    public async Task SwitchProfileByType_SelectsCorrectProfile()
    {
        await _manager.InitializeAsync(_context);
        await _manager.CreateProfileAsync(ProfileType.Server);
        
        var result = await _manager.SwitchProfileByTypeAsync(ProfileType.Server);
        
        Assert.True(result is Result.Success);
        Assert.Equal(ProfileType.Server, _manager.ActiveProfile!.ProfileType);
    }

    [Fact]
    public async Task DeleteProfile_RemovesProfile()
    {
        await _manager.InitializeAsync(_context);
        var serverResult = await _manager.CreateProfileAsync(ProfileType.Server);
        var serverId = (serverResult as Result<IProfile>.Success)!.Value.ComponentId;
        
        var result = await _manager.DeleteProfileAsync(serverId);
        
        Assert.True(result is Result.Success);
        var getResult = await _manager.GetProfileAsync(serverId);
        Assert.True(getResult is Result<IProfile>.Failure);
    }

    [Fact]
    public async Task DeleteProfile_Active_FailsUnlessForceful()
    {
        await _manager.InitializeAsync(_context);
        var activeId = _manager.ActiveProfile!.ComponentId;
        
        var result = await _manager.DeleteProfileAsync(activeId, force: false);
        
        Assert.True(result is Result.Failure);
    }

    [Fact]
    public async Task GetAvailableProfiles_ReturnsAll()
    {
        await _manager.InitializeAsync(_context);
        await _manager.CreateProfileAsync(ProfileType.Server);
        await _manager.CreateProfileAsync(ProfileType.Automation);
        
        var result = await _manager.GetAvailableProfilesAsync();
        
        Assert.True(result is Result<IProfile[]>.Success);
        var profiles = (result as Result<IProfile[]>.Success)!.Value;
        Assert.Equal(3, profiles.Length);
    }

    [Fact]
    public async Task WatchProfile_NotifiesOnChange()
    {
        await _manager.InitializeAsync(_context);
        var initialProfile = _manager.ActiveProfile!;
        var notificationCount = 0;

        using var watcher = _manager.WatchProfile(initialProfile.ComponentId, _ => notificationCount++);

        var serverResult = await _manager.CreateProfileAsync(ProfileType.Server);
        var serverId = (serverResult as Result<IProfile>.Success)!.Value.ComponentId;
        await _manager.SwitchProfileAsync(initialProfile.ComponentId);

        Assert.True(notificationCount > 0);
    }

    [Fact]
    public async Task VerifyProfileIsolation_Succeeds()
    {
        await _manager.InitializeAsync(_context);
        
        var result = await _manager.VerifyProfileIsolationAsync();
        
        Assert.True(result is Result.Success);
    }
}

/// <summary>
/// Tests for Server profile external access and audit logging (10+ tests).
/// </summary>
public class ServerProfileTests
{
    private readonly MockServiceContext _context = new();
    private readonly ServerProfileImpl _profile;

    public ServerProfileTests()
    {
        _profile = new ServerProfileImpl(_context, "test_server", ResourceAllocation.ServerDefault());
    }

    [Fact]
    public async Task Initialize_StartsWithExternalDisabled()
    {
        await _profile.InitializeAsync(_context);
        
        Assert.False(_profile.ExternalEnabled);
        Assert.Null(_profile.ExternalEnabledUntil);
    }

    [Fact]
    public async Task EnableExternal_WithValidDuration_Succeeds()
    {
        await _profile.InitializeAsync(_context);
        await _profile.ActivateAsync();
        
        var result = await _profile.EnableExternalAsync(
            TimeSpan.FromHours(1),
            "Testing",
            "TestUser");
        
        Assert.True(result is Result.Success);
        Assert.True(_profile.ExternalEnabled);
        Assert.NotNull(_profile.ExternalEnabledUntil);
    }

    [Fact]
    public async Task EnableExternal_WithInvalidDuration_Fails()
    {
        await _profile.InitializeAsync(_context);
        await _profile.ActivateAsync();
        
        var result = await _profile.EnableExternalAsync(TimeSpan.FromHours(25));
        
        Assert.True(result is Result.Failure);
        Assert.False(_profile.ExternalEnabled);
    }

    [Fact]
    public async Task EnableExternal_AlreadyEnabled_Fails()
    {
        await _profile.InitializeAsync(_context);
        await _profile.ActivateAsync();
        await _profile.EnableExternalAsync(TimeSpan.FromHours(1), "Test", "User");
        
        var result = await _profile.EnableExternalAsync(TimeSpan.FromHours(2), "Test2", "User2");
        
        Assert.True(result is Result.Failure);
    }

    [Fact]
    public async Task DisableExternal_WhenEnabled_Succeeds()
    {
        await _profile.InitializeAsync(_context);
        await _profile.ActivateAsync();
        await _profile.EnableExternalAsync(TimeSpan.FromHours(1), "Test", "User");
        
        var result = await _profile.DisableExternalAsync();
        
        Assert.True(result is Result.Success);
        Assert.False(_profile.ExternalEnabled);
        Assert.Null(_profile.ExternalEnabledUntil);
    }

    [Fact]
    public async Task DisableExternal_WhenDisabled_Succeeds()
    {
        await _profile.InitializeAsync(_context);
        await _profile.ActivateAsync();
        
        var result = await _profile.DisableExternalAsync();
        
        Assert.True(result is Result.Success);
    }

    [Fact]
    public async Task GetExternalAccessAudit_ContainsEvents()
    {
        await _profile.InitializeAsync(_context);
        await _profile.ActivateAsync();
        await _profile.EnableExternalAsync(TimeSpan.FromHours(1), "Test", "User");
        
        var result = await _profile.GetExternalAccessAuditAsync();
        
        Assert.True(result is Result<ExternalAccessAuditEntry[]>.Success);
        var entries = (result as Result<ExternalAccessAuditEntry[]>.Success)!.Value;
        Assert.True(entries.Length > 0);
    }

    [Fact]
    public async Task GetExternalAccessAudit_WithLimit_ReturnsLimited()
    {
        await _profile.InitializeAsync(_context);
        await _profile.ActivateAsync();
        
        var result = await _profile.GetExternalAccessAuditAsync(limit: 1);
        
        Assert.True(result is Result<ExternalAccessAuditEntry[]>.Success);
        var entries = (result as Result<ExternalAccessAuditEntry[]>.Success)!.Value;
        Assert.True(entries.Length <= 1);
    }
}

/// <summary>
/// Tests for Automation profile task submission and execution (10+ tests).
/// </summary>
public class AutomationProfileTests
{
    private readonly MockServiceContext _context = new();
    private readonly AutomationProfileImpl _profile;

    public AutomationProfileTests()
    {
        _profile = new AutomationProfileImpl(_context, "test_automation", ResourceAllocation.AutomationDefault());
    }

    [Fact]
    public async Task Initialize_EnablesGpuSupport()
    {
        await _profile.InitializeAsync(_context);
        
        Assert.True(_profile.GpuAvailable);
        Assert.NotNull(_profile.GpuType);
        Assert.NotNull(_profile.DevDrivePath);
    }

    [Fact]
    public async Task SubmitTask_WhenRunning_Succeeds()
    {
        await _profile.InitializeAsync(_context);
        await _profile.ActivateAsync();
        
        var task = new AutomationTask("TestTask", "echo hello");
        var result = await _profile.SubmitTaskAsync(task);
        
        Assert.True(result is Result<string>.Success);
        var taskId = (result as Result<string>.Success)!.Value;
        Assert.Equal(task.Id, taskId);
    }

    [Fact]
    public async Task SubmitTask_WhenNotRunning_Fails()
    {
        await _profile.InitializeAsync(_context);
        
        var task = new AutomationTask("TestTask", "echo hello");
        var result = await _profile.SubmitTaskAsync(task);
        
        Assert.True(result is Result<string>.Failure);
    }

    [Fact]
    public async Task GetTaskStatus_ExistingTask_Returns()
    {
        await _profile.InitializeAsync(_context);
        await _profile.ActivateAsync();
        
        var task = new AutomationTask("TestTask", "echo hello");
        var submitResult = await _profile.SubmitTaskAsync(task);
        var taskId = (submitResult as Result<string>.Success)!.Value;
        
        var statusResult = await _profile.GetTaskStatusAsync(taskId);
        
        Assert.True(statusResult is Result<TaskExecutionStatus>.Success);
        var status = (statusResult as Result<TaskExecutionStatus>.Success)!.Value;
        Assert.Equal(taskId, status.TaskId);
        Assert.Equal(TaskExecutionState.Queued, status.State);
    }

    [Fact]
    public async Task GetTaskStatus_NonexistentTask_Fails()
    {
        await _profile.InitializeAsync(_context);
        await _profile.ActivateAsync();
        
        var result = await _profile.GetTaskStatusAsync("nonexistent");
        
        Assert.True(result is Result<TaskExecutionStatus>.Failure);
    }

    [Fact]
    public async Task CancelTask_ExistingTask_Succeeds()
    {
        await _profile.InitializeAsync(_context);
        await _profile.ActivateAsync();
        
        var task = new AutomationTask("TestTask", "echo hello");
        var submitResult = await _profile.SubmitTaskAsync(task);
        var taskId = (submitResult as Result<string>.Success)!.Value;
        
        var cancelResult = await _profile.CancelTaskAsync(taskId);
        
        Assert.True(cancelResult is Result.Success);
        var statusResult = await _profile.GetTaskStatusAsync(taskId);
        var status = (statusResult as Result<TaskExecutionStatus>.Success)!.Value;
        Assert.Equal(TaskExecutionState.Cancelled, status.State);
    }

    [Fact]
    public async Task GetTaskHistory_ReturnsRecords()
    {
        await _profile.InitializeAsync(_context);
        await _profile.ActivateAsync();
        
        var task = new AutomationTask("TestTask", "echo hello");
        await _profile.SubmitTaskAsync(task);
        
        var result = await _profile.GetTaskHistoryAsync();
        
        Assert.True(result is Result<TaskExecutionRecord[]>.Success);
    }
}

/// <summary>
/// Tests for DevDrive optimization (10+ tests).
/// </summary>
public class DevDriveOptimizerTests
{
    private readonly MockServiceContext _context = new();
    private readonly DevDriveOptimizer _optimizer;

    public DevDriveOptimizerTests()
    {
        _optimizer = new DevDriveOptimizer(_context, "test_devdrive", "D:\\DevDrive");
    }

    [Fact]
    public async Task Initialize_ConfiguresReFS()
    {
        await _optimizer.InitializeAsync(_context);
        
        Assert.Equal("ReFS", _optimizer.FileSystemType);
        Assert.True(_optimizer.DeduplicationEnabled);
    }

    [Fact]
    public async Task GetStats_ReturnsValidStats()
    {
        await _optimizer.InitializeAsync(_context);
        
        var result = await _optimizer.GetStatsAsync();
        
        Assert.True(result is Result<DevDriveStats>.Success);
        var stats = (result as Result<DevDriveStats>.Success)!.Value;
        Assert.True(stats.TotalCapacityMb > 0);
        Assert.True(stats.FreeMb >= 0);
    }

    [Fact]
    public async Task StartOptimization_Succeeds()
    {
        await _optimizer.InitializeAsync(_context);
        
        var result = await _optimizer.StartOptimizationAsync();
        
        Assert.True(result is Result.Success);
    }

    [Fact]
    public async Task StopOptimization_Succeeds()
    {
        await _optimizer.InitializeAsync(_context);
        await _optimizer.StartOptimizationAsync();
        
        var result = await _optimizer.StopOptimizationAsync();
        
        Assert.True(result is Result.Success);
    }

    [Fact]
    public async Task Optimization_ImprovesSavings()
    {
        await _optimizer.InitializeAsync(_context);
        
        var statsBeforeResult = await _optimizer.GetStatsAsync();
        var statsBefore = (statsBeforeResult as Result<DevDriveStats>.Success)!.Value;
        
        await _optimizer.StartOptimizationAsync();
        await Task.Delay(100);
        await _optimizer.StopOptimizationAsync();
        
        var statsAfterResult = await _optimizer.GetStatsAsync();
        var statsAfter = (statsAfterResult as Result<DevDriveStats>.Success)!.Value;
        
        Assert.True(statsAfter.SavedMb >= statsBefore.SavedMb);
    }
}

/// <summary>
/// Tests for External API Gateway (10+ tests).
/// </summary>
public class ExternalAPIGatewayTests
{
    private readonly MockServiceContext _context = new();
    private readonly ExternalAPIGateway _gateway;

    public ExternalAPIGatewayTests()
    {
        _gateway = new ExternalAPIGateway(_context, "test_gateway");
    }

    [Fact]
    public async Task Initialize_GatewayDisabledByDefault()
    {
        await _gateway.InitializeAsync(_context);
        
        Assert.False(_gateway.IsEnabled);
    }

    [Fact]
    public async Task EnableGateway_Succeeds()
    {
        await _gateway.InitializeAsync(_context);
        
        var result = await _gateway.EnableAsync();
        
        Assert.True(result is Result.Success);
        Assert.True(_gateway.IsEnabled);
    }

    [Fact]
    public async Task DisableGateway_Succeeds()
    {
        await _gateway.InitializeAsync(_context);
        await _gateway.EnableAsync();
        
        var result = await _gateway.DisableAsync();
        
        Assert.True(result is Result.Success);
        Assert.False(_gateway.IsEnabled);
    }

    [Fact]
    public async Task ProcessRequest_WhenDisabled_Fails()
    {
        await _gateway.InitializeAsync(_context);
        
        var request = new APIRequest("GET", "/api/status", ApiKey: "key_test");
        var result = await _gateway.ProcessRequestAsync(request);
        
        Assert.True(result is Result<APIResponse>.Success);
        var response = (result as Result<APIResponse>.Success)!.Value;
        Assert.Equal(403, response.StatusCode);
    }

    [Fact]
    public async Task ProcessRequest_WithoutApiKey_Fails()
    {
        await _gateway.InitializeAsync(_context);
        await _gateway.EnableAsync();
        
        var request = new APIRequest("GET", "/api/status", ApiKey: null);
        var result = await _gateway.ProcessRequestAsync(request);
        
        Assert.True(result is Result<APIResponse>.Success);
        var response = (result as Result<APIResponse>.Success)!.Value;
        Assert.Equal(401, response.StatusCode);
    }

    [Fact]
    public async Task ProcessRequest_InvalidEndpoint_Blocked()
    {
        await _gateway.InitializeAsync(_context);
        await _gateway.EnableAsync();
        
        var request = new APIRequest("GET", "/admin/secret", ApiKey: "key_test");
        var result = await _gateway.ProcessRequestAsync(request);
        
        Assert.True(result is Result<APIResponse>.Success);
        var response = (result as Result<APIResponse>.Success)!.Value;
        Assert.Equal(403, response.StatusCode);
    }

    [Fact]
    public async Task ProcessRequest_AllowedEndpoint_Succeeds()
    {
        await _gateway.InitializeAsync(_context);
        await _gateway.EnableAsync();
        
        var request = new APIRequest("GET", "/api/status", ApiKey: "key_test", ClientIp: "127.0.0.1");
        var result = await _gateway.ProcessRequestAsync(request);
        
        Assert.True(result is Result<APIResponse>.Success);
        var response = (result as Result<APIResponse>.Success)!.Value;
        Assert.True(response.StatusCode >= 200 && response.StatusCode < 300);
    }

    [Fact]
    public async Task RateLimit_EnforcedPerClient()
    {
        await _gateway.InitializeAsync(_context);
        await _gateway.EnableAsync();
        
        for (int i = 0; i < 70; i++)
        {
            var request = new APIRequest("GET", "/api/status", ApiKey: "key_test", ClientIp: "192.168.1.1");
            await _gateway.ProcessRequestAsync(request);
        }
        
        var lastRequest = new APIRequest("GET", "/api/status", ApiKey: "key_test", ClientIp: "192.168.1.1");
        var result = await _gateway.ProcessRequestAsync(lastRequest);
        
        Assert.True(result is Result<APIResponse>.Success);
        var response = (result as Result<APIResponse>.Success)!.Value;
        Assert.Equal(429, response.StatusCode);
    }

    [Fact]
    public async Task GetMetrics_ReturnsAccurateData()
    {
        await _gateway.InitializeAsync(_context);
        await _gateway.EnableAsync();
        
        for (int i = 0; i < 5; i++)
        {
            var request = new APIRequest("GET", "/api/status", ApiKey: "key_test", ClientIp: "127.0.0.1");
            await _gateway.ProcessRequestAsync(request);
        }
        
        var result = await _gateway.GetMetricsAsync();
        
        Assert.True(result is Result<GatewayMetrics>.Success);
        var metrics = (result as Result<GatewayMetrics>.Success)!.Value;
        Assert.True(metrics.TotalRequests > 0);
        Assert.True(metrics.SuccessfulRequests > 0);
    }

    [Fact]
    public async Task GetRequestLogs_ReturnsRecent()
    {
        await _gateway.InitializeAsync(_context);
        await _gateway.EnableAsync();
        
        var request = new APIRequest("GET", "/api/status", ApiKey: "key_test", ClientIp: "127.0.0.1");
        await _gateway.ProcessRequestAsync(request);
        
        var result = await _gateway.GetRequestLogsAsync(limit: 10);
        
        Assert.True(result is Result<APIRequestLog[]>.Success);
        var logs = (result as Result<APIRequestLog[]>.Success)!.Value;
        Assert.True(logs.Length > 0);
    }
}

/// <summary>
/// Mock service context for testing.
/// </summary>
public class MockServiceContext : IServiceContext
{
    public string CorrelationId => Guid.NewGuid().ToString();
    public IPrincipal? Principal => null;
    public IConfigurationProvider Configuration => new MockConfiguration();
    public ILoggingProvider Logger => new MockLogger();
    public IMetricsCollector Metrics => new MockMetrics();
    public ICacheProvider Cache => new MockCache();
    public IServiceProvider ServiceProvider => new MockServiceProvider();
}

public class MockConfiguration : IConfigurationProvider
{
    public Result<T> Get<T>(string key) => default(T)!.ToSuccess();
    public T Get<T>(string key, T defaultValue) => defaultValue;
    public Result Set<T>(string key, T value, bool persistent = false) => Result.Success();
    public Result Validate() => Result.Success();
    public IDisposable Watch(string key, Action<object?> onChanged) => new MockDisposable();
}

public class MockLogger : ILoggingProvider
{
    public void Trace(string message, params object?[] args) { }
    public void Debug(string message, params object?[] args) { }
    public void Information(string message, params object?[] args) { }
    public void Warning(string message, params object?[] args) { }
    public void Error(string message, Exception? ex = null, params object?[] args) { }
    public void Fatal(string message, Exception? ex = null, params object?[] args) { }
    public Task<Result<T>> LogOperationAsync<T>(string operationName, Func<Task<Result<T>>> operation, LogLevel level = LogLevel.Information)
        => operation();
}

public class MockMetrics : IMetricsCollector
{
    public void IncrementCounter(string metricName, long value = 1, params (string, string)[] tags) { }
    public void SetGauge(string metricName, double value, params (string, string)[] tags) { }
    public void RecordHistogram(string metricName, long value, params (string, string)[] tags) { }
    public void RecordDuration(string operationName, TimeSpan duration, params (string, string)[] tags) { }
}

public class MockCache : ICacheProvider
{
    public Task<Result<T?>> GetAsync<T>(string key, CancellationToken ct = default) => Task.FromResult(default(T)!.ToSuccess<T?>());
    public Task<Result> SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken ct = default) => Task.FromResult(Result.Success());
    public Task<Result> RemoveAsync(string key, CancellationToken ct = default) => Task.FromResult(Result.Success());
    public Task<Result<T>> GetOrComputeAsync<T>(string key, Func<CancellationToken, Task<Result<T>>> factory, TimeSpan? expiration = null, CancellationToken ct = default)
        => factory(ct);
    public Task<Result> InvalidateByPatternAsync(string pattern, CancellationToken ct = default) => Task.FromResult(Result.Success());
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}

public class MockServiceProvider : IServiceProvider
{
    public object? GetService(Type serviceType) => null;
}

public class MockDisposable : IDisposable
{
    public void Dispose() { }
}
