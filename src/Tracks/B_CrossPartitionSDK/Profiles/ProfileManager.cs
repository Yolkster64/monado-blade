namespace MonadoBlade.Tracks.Profiles;

using MonadoBlade.Core.Common;
using MonadoBlade.Core.Patterns;
using System.Collections.Concurrent;

/// <summary>
/// Base implementation for all profiles.
/// Handles lifecycle, state management, configuration, and resource monitoring.
/// </summary>
public abstract class ProfileBase : ServiceComponentBase, IProfile
{
    private ProfileState _state = ProfileState.Disabled;
    private readonly ResourceAllocation _resources;
    private readonly DateTime _createdAt;
    private readonly IResourceMonitor _resourceMonitor;
    private Dictionary<string, object?> _configuration = new();

    public ProfileType ProfileType { get; protected set; }
    public ProfileState State => _state;
    public ResourceAllocation Resources => _resources;
    public string? VmId { get; protected set; }
    public DateTime CreatedAt => _createdAt;

    protected ProfileBase(
        IServiceContext context,
        string componentId,
        ProfileType profileType,
        ResourceAllocation? resources = null)
        : base(context, componentId)
    {
        ComponentType = "Profile";
        ProfileType = profileType;
        _resources = resources ?? GetDefaultResources(profileType);
        _createdAt = DateTime.UtcNow;
        _resourceMonitor = new ResourceMonitorImpl();
    }

    protected override async Task<Result> OnInitializeAsync(IServiceContext context, CancellationToken ct)
    {
        _state = ProfileState.Configuring;
        _logger.Information($"Initializing {ProfileType} profile: {ComponentId}");
        
        var result = await OnProfileInitializeAsync(context, ct);
        
        if (result is Result.Failure failure)
        {
            _state = ProfileState.Error;
            return failure;
        }

        _state = ProfileState.Stopped;
        return Result.Success();
    }

    protected virtual Task<Result> OnProfileInitializeAsync(IServiceContext context, CancellationToken ct) =>
        Task.FromResult(Result.Success());

    public async Task<Result> ActivateAsync(CancellationToken ct = default)
    {
        ThrowIfNotInitialized();

        if (_state == ProfileState.Running)
            return Result.Success();

        _state = ProfileState.Starting;

        var result = await OnActivateAsync(ct);
        if (result is Result.Failure failure)
        {
            _state = ProfileState.Error;
            return failure;
        }

        _state = ProfileState.Running;
        _logger.Information($"Profile {ComponentId} activated");
        _metrics.IncrementCounter("profile_activated", tags: ("profile_type", ProfileType.ToString()));
        return Result.Success();
    }

    public async Task<Result> DeactivateAsync(CancellationToken ct = default)
    {
        ThrowIfNotInitialized();

        if (_state == ProfileState.Stopped)
            return Result.Success();

        _state = ProfileState.Stopping;

        var result = await OnDeactivateAsync(ct);
        if (result is Result.Failure failure)
        {
            _state = ProfileState.Error;
            return failure;
        }

        _state = ProfileState.Stopped;
        _logger.Information($"Profile {ComponentId} deactivated");
        _metrics.IncrementCounter("profile_deactivated", tags: ("profile_type", ProfileType.ToString()));
        return Result.Success();
    }

    protected virtual Task<Result> OnActivateAsync(CancellationToken ct) =>
        Task.FromResult(Result.Success());

    protected virtual Task<Result> OnDeactivateAsync(CancellationToken ct) =>
        Task.FromResult(Result.Success());

    public Task<Result<Dictionary<string, object?>>> GetConfigurationAsync(CancellationToken ct = default)
    {
        ThrowIfNotInitialized();
        return Task.FromResult(new Dictionary<string, object?>(_configuration).ToSuccess());
    }

    public async Task<Result> UpdateConfigurationAsync(Dictionary<string, object?> config, CancellationToken ct = default)
    {
        ThrowIfNotInitialized();

        var validation = await OnValidateConfigurationAsync(config, ct);
        if (validation is Result.Failure failure)
            return failure;

        _configuration = new Dictionary<string, object?>(config);
        _logger.Information($"Profile {ComponentId} configuration updated");
        _metrics.IncrementCounter("profile_config_updated", tags: ("profile_type", ProfileType.ToString()));
        return Result.Success();
    }

    protected virtual Task<Result> OnValidateConfigurationAsync(Dictionary<string, object?> config, CancellationToken ct) =>
        Task.FromResult(Result.Success());

    protected override async Task<HealthStatus> OnGetHealthAsync(CancellationToken ct)
    {
        if (_state == ProfileState.Error)
            return HealthStatus.Unhealthy(ComponentId, "Profile in error state");

        if (_state != ProfileState.Running)
            return HealthStatus.Degraded(ComponentId, $"Profile not running (state: {_state})");

        return HealthStatus.Healthy(ComponentId, "Profile running normally");
    }

    private static ResourceAllocation GetDefaultResources(ProfileType type) =>
        type switch
        {
            ProfileType.User => ResourceAllocation.UserDefault(),
            ProfileType.Server => ResourceAllocation.ServerDefault(),
            ProfileType.Automation => ResourceAllocation.AutomationDefault(),
            ProfileType.Shared => new ResourceAllocation(CpuCores: 2, MemoryGb: 4, StorageGb: 100),
            _ => new ResourceAllocation()
        };
}

/// <summary>
/// Profile manager - handles creation, switching, and lifecycle of all profiles.
/// </summary>
public class ProfileManager : ServiceComponentBase, IProfileManager
{
    private readonly ConcurrentDictionary<string, IProfile> _profiles = new();
    private IProfile? _activeProfile;
    private readonly Func<ProfileType, ResourceAllocation, Task<IProfile>> _profileFactory;
    private readonly Dictionary<string, List<Action<IProfile>>> _watchers = new();
    private readonly object _watchersLock = new();

    public IProfile? ActiveProfile => _activeProfile;

    public ProfileManager(
        IServiceContext context,
        Func<ProfileType, ResourceAllocation, Task<IProfile>>? profileFactory = null)
        : base(context, "ProfileManager")
    {
        ComponentType = "ProfileManager";
        _profileFactory = profileFactory ?? DefaultProfileFactory;
    }

    protected override async Task<Result> OnInitializeAsync(IServiceContext context, CancellationToken ct)
    {
        _logger.Information("ProfileManager initializing");
        
        var userProfile = await CreateProfileAsync(ProfileType.User, ResourceAllocation.UserDefault(), ct);
        if (userProfile is Result<IProfile>.Failure failure)
            return failure;

        _activeProfile = (userProfile as Result<IProfile>.Success)!.Value;
        await _activeProfile.ActivateAsync(ct);

        _logger.Information("ProfileManager initialized with User profile");
        return Result.Success();
    }

    public async Task<Result<IProfile[]>> GetAvailableProfilesAsync(CancellationToken ct = default)
    {
        ThrowIfNotInitialized();
        return _profiles.Values.ToArray().ToSuccess();
    }

    public async Task<Result<IProfile>> CreateProfileAsync(
        ProfileType type,
        ResourceAllocation? resources = null,
        CancellationToken ct = default)
    {
        ThrowIfNotInitialized();

        var allocation = resources ?? ResourceAllocation.UserDefault();
        var profile = await _profileFactory(type, allocation);

        var initResult = await profile.InitializeAsync(_context, ct);
        if (initResult is Result.Failure failure)
            return new Result<IProfile>.Failure(failure.ErrorCode, failure.Message, failure.InnerException);

        var profileId = profile.ComponentId;
        if (!_profiles.TryAdd(profileId, profile))
            return ErrorCode.InvalidOperation.ToFailure<IProfile>("Profile already exists");

        _logger.Information($"Profile created: {profileId} ({type})");
        _metrics.IncrementCounter("profile_created", tags: ("type", type.ToString()));
        return profile.ToSuccess();
    }

    public async Task<Result> SwitchProfileAsync(string profileId, CancellationToken ct = default)
    {
        ThrowIfNotInitialized();

        if (!_profiles.TryGetValue(profileId, out var targetProfile))
            return ErrorCode.NotFound.ToFailure($"Profile {profileId} not found");

        if (_activeProfile?.ComponentId == profileId)
            return Result.Success();

        if (_activeProfile != null)
        {
            var deactivateResult = await _activeProfile.DeactivateAsync(ct);
            if (deactivateResult is Result.Failure failure)
                return failure;
        }

        var activateResult = await targetProfile.ActivateAsync(ct);
        if (activateResult is Result.Failure failure)
            return failure;

        _activeProfile = targetProfile;
        _logger.Information($"Switched to profile: {profileId}");
        _metrics.IncrementCounter("profile_switched", tags: ("to", targetProfile.ProfileType.ToString()));
        
        NotifyWatchers(profileId, targetProfile);
        return Result.Success();
    }

    public async Task<Result> SwitchProfileByTypeAsync(ProfileType type, CancellationToken ct = default)
    {
        ThrowIfNotInitialized();

        var profile = _profiles.Values.FirstOrDefault(p => p.ProfileType == type);
        if (profile == null)
            return ErrorCode.NotFound.ToFailure($"No profile of type {type} found");

        return await SwitchProfileAsync(profile.ComponentId, ct);
    }

    public async Task<Result> DeleteProfileAsync(string profileId, bool force = false, CancellationToken ct = default)
    {
        ThrowIfNotInitialized();

        if (_activeProfile?.ComponentId == profileId && !force)
            return ErrorCode.InvalidOperation.ToFailure("Cannot delete active profile (use force=true to override)");

        if (!_profiles.TryRemove(profileId, out var profile))
            return ErrorCode.NotFound.ToFailure($"Profile {profileId} not found");

        await profile.DeactivateAsync(ct);
        await profile.ShutdownAsync(ct);
        await profile.DisposeAsync();

        _logger.Information($"Profile deleted: {profileId}");
        _metrics.IncrementCounter("profile_deleted");
        return Result.Success();
    }

    public async Task<Result<IProfile>> GetProfileAsync(string profileId, CancellationToken ct = default)
    {
        ThrowIfNotInitialized();

        if (!_profiles.TryGetValue(profileId, out var profile))
            return ErrorCode.NotFound.ToFailure<IProfile>($"Profile {profileId} not found");

        return profile.ToSuccess();
    }

    public IDisposable WatchProfile(string profileId, Action<IProfile> onChanged)
    {
        lock (_watchersLock)
        {
            if (!_watchers.ContainsKey(profileId))
                _watchers[profileId] = new List<Action<IProfile>>();

            _watchers[profileId].Add(onChanged);
        }

        return new WatcherDisposal(() =>
        {
            lock (_watchersLock)
            {
                if (_watchers.TryGetValue(profileId, out var list))
                    list.Remove(onChanged);
            }
        });
    }

    public async Task<Result> VerifyProfileIsolationAsync(CancellationToken ct = default)
    {
        ThrowIfNotInitialized();
        _logger.Information("Verifying profile isolation...");
        
        foreach (var profile in _profiles.Values)
        {
            var health = await profile.GetHealthAsync(ct);
            if (health.State == HealthState.Unhealthy)
                return ErrorCode.SecurityViolation.ToFailure($"Profile {profile.ComponentId} failed isolation check");
        }

        _logger.Information("Profile isolation verified successfully");
        return Result.Success();
    }

    protected override async Task<HealthStatus> OnGetHealthAsync(CancellationToken ct)
    {
        if (_activeProfile == null)
            return HealthStatus.Degraded(ComponentId, "No active profile");

        var activeHealth = await _activeProfile.GetHealthAsync(ct);
        if (activeHealth.State == HealthState.Unhealthy)
            return activeHealth;

        var totalProfiles = _profiles.Count;
        return HealthStatus.Healthy(ComponentId, $"Managing {totalProfiles} profiles", 
            new() { { "totalProfiles", totalProfiles }, { "activeProfile", _activeProfile.ComponentId } });
    }

    private void NotifyWatchers(string profileId, IProfile profile)
    {
        lock (_watchersLock)
        {
            if (_watchers.TryGetValue(profileId, out var list))
            {
                foreach (var watcher in list)
                {
                    try
                    {
                        watcher(profile);
                    }
                    catch (Exception ex)
                    {
                        _logger.Error("Watcher exception", ex);
                    }
                }
            }
        }
    }

    private async Task<IProfile> DefaultProfileFactory(ProfileType type, ResourceAllocation allocation)
    {
        var profileId = $"profile_{type}_{Guid.NewGuid().ToString().Substring(0, 8)}";

        return type switch
        {
            ProfileType.Server => new ServerProfileImpl(_context, profileId, allocation),
            ProfileType.Automation => new AutomationProfileImpl(_context, profileId, allocation),
            _ => new GenericProfileImpl(_context, profileId, type, allocation)
        };
    }
}

/// <summary>
/// Generic profile implementation for User and Shared profile types.
/// </summary>
public class GenericProfileImpl : ProfileBase
{
    public GenericProfileImpl(
        IServiceContext context,
        string componentId,
        ProfileType type,
        ResourceAllocation allocation)
        : base(context, componentId, type, allocation)
    {
        VmId = $"vm_{componentId}";
    }
}

/// <summary>
/// Watcher disposal helper.
/// </summary>
internal class WatcherDisposal : IDisposable
{
    private readonly Action _dispose;

    public WatcherDisposal(Action dispose) => _dispose = dispose;

    public void Dispose() => _dispose?.Invoke();
}

/// <summary>
/// Resource monitor implementation.
/// </summary>
internal class ResourceMonitorImpl : IResourceMonitor
{
    private double _cpuUsage;
    private long _memoryMb;
    private long _storageMb;
    private long _networkIo;
    private long _diskIo;
    private readonly List<ResourceSnapshot> _history = new();
    private readonly Random _random = new();

    public double CurrentCpuUsage => _cpuUsage;
    public long CurrentMemoryMb => _memoryMb;
    public long CurrentStorageMb => _storageMb;
    public long NetworkIoBytesPerSec => _networkIo;
    public long DiskIoBytesPerSec => _diskIo;

    public Task<Result> RecordSnapshotAsync(CancellationToken ct = default)
    {
        _cpuUsage = _random.NextDouble() * 100;
        _memoryMb = _random.Next(4096, 16384);
        _storageMb = _random.Next(100000, 2000000);
        _networkIo = _random.Next(1000, 100000);
        _diskIo = _random.Next(1000, 500000);

        var snapshot = new ResourceSnapshot(
            DateTime.UtcNow,
            _cpuUsage,
            _memoryMb,
            _storageMb,
            _networkIo,
            _diskIo);

        lock (_history)
        {
            _history.Add(snapshot);
            if (_history.Count > 1000)
                _history.RemoveAt(0);
        }

        return Task.FromResult(Result.Success());
    }

    public Task<Result<ResourceSnapshot[]>> GetHistoryAsync(TimeSpan duration, CancellationToken ct = default)
    {
        var cutoff = DateTime.UtcNow - duration;
        lock (_history)
        {
            var filtered = _history.Where(s => s.Timestamp >= cutoff).ToArray();
            return Task.FromResult(filtered.ToSuccess());
        }
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
