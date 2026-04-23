using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HELIOS.Platform.Architecture
{
    /// <summary>
    /// Profile types
    /// </summary>
    public enum ProfileType
    {
        Gamer,
        Developer,
        AIResearch,
        Secure,
        Enterprise
    }

    /// <summary>
    /// CPU affinity settings
    /// </summary>
    public class CpuAffinitySettings
    {
        public int[] CoreIds { get; set; } // Which cores to use
        public bool HyperThreadingEnabled { get; set; }
        public int Priority { get; set; } // 0-10 (0=lowest, 10=highest)
    }

    /// <summary>
    /// Resource constraints for a service
    /// </summary>
    public class ResourceConstraints
    {
        public long MaxMemoryBytes { get; set; }
        public int MaxFileHandles { get; set; }
        public long MaxDiskQuotaBytes { get; set; }
        public int MaxThreads { get; set; }
        public int IoPriority { get; set; } // 0-7 (like ionice)
    }

    /// <summary>
    /// Network rules for a profile
    /// </summary>
    public class NetworkRules
    {
        public bool InternetAccessAllowed { get; set; }
        public string[] AllowedPorts { get; set; } = Array.Empty<string>();
        public string[] BlockedPorts { get; set; } = Array.Empty<string>();
        public long BandwidthLimitMbps { get; set; } // 0 = unlimited
    }

    /// <summary>
    /// Profile configuration
    /// </summary>
    public class ProfileConfiguration
    {
        public ProfileType Type { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public Dictionary<string, CpuAffinitySettings> ServiceCpuAffinity { get; set; } = new();
        public Dictionary<string, ResourceConstraints> ServiceResourceConstraints { get; set; } = new();
        public Dictionary<string, int> ServicePriorities { get; set; } = new();
        public bool GpuAccessEnabled { get; set; }
        public double GpuAllocationPercentage { get; set; } // 0-100
        public NetworkRules NetworkRules { get; set; } = new();
        public Dictionary<string, object> CustomSettings { get; set; } = new();
    }

    /// <summary>
    /// Predefined profiles factory
    /// </summary>
    public static class PredefinedProfiles
    {
        public static ProfileConfiguration CreateGamerProfile()
        {
            return new ProfileConfiguration
            {
                Type = ProfileType.Gamer,
                Name = "Gamer",
                Description = "High performance for games - GPU focused, high CPU priority",
                GpuAccessEnabled = true,
                GpuAllocationPercentage = 90,
                ServicePriorities = new()
                {
                    { "GPU", 10 },
                    { "Audio", 9 },
                    { "Input", 8 }
                },
                NetworkRules = new()
                {
                    InternetAccessAllowed = true,
                    BandwidthLimitMbps = 0
                }
            };
        }

        public static ProfileConfiguration CreateDeveloperProfile()
        {
            return new ProfileConfiguration
            {
                Type = ProfileType.Developer,
                Name = "Developer",
                Description = "Balanced profile - high CPU, moderate memory",
                GpuAccessEnabled = true,
                GpuAllocationPercentage = 50,
                ServicePriorities = new()
                {
                    { "CPU", 8 },
                    { "Memory", 7 },
                    { "Disk", 6 }
                },
                NetworkRules = new()
                {
                    InternetAccessAllowed = true,
                    BandwidthLimitMbps = 0
                }
            };
        }

        public static ProfileConfiguration CreateAIResearchProfile()
        {
            return new ProfileConfiguration
            {
                Type = ProfileType.AIResearch,
                Name = "AI Research",
                Description = "Maximum resources - high CPU, high memory, GPU",
                GpuAccessEnabled = true,
                GpuAllocationPercentage = 100,
                ServicePriorities = new()
                {
                    { "CPU", 10 },
                    { "Memory", 10 },
                    { "GPU", 10 }
                },
                NetworkRules = new()
                {
                    InternetAccessAllowed = true,
                    BandwidthLimitMbps = 0
                },
                CustomSettings = new()
                {
                    { "AllowMemoryOvercommit", true },
                    { "EnableSwap", true }
                }
            };
        }

        public static ProfileConfiguration CreateSecureProfile()
        {
            return new ProfileConfiguration
            {
                Type = ProfileType.Secure,
                Name = "Secure",
                Description = "Security focused - minimal network, restricted GPU",
                GpuAccessEnabled = false,
                GpuAllocationPercentage = 0,
                ServicePriorities = new()
                {
                    { "SecurityMonitor", 10 },
                    { "EncryptionService", 9 }
                },
                NetworkRules = new()
                {
                    InternetAccessAllowed = false,
                    AllowedPorts = new[] { "22", "80", "443" },
                    BandwidthLimitMbps = 10
                }
            };
        }

        public static ProfileConfiguration CreateEnterpriseProfile()
        {
            return new ProfileConfiguration
            {
                Type = ProfileType.Enterprise,
                Name = "Enterprise",
                Description = "Stability and monitoring - balanced resources, full logging",
                GpuAccessEnabled = true,
                GpuAllocationPercentage = 30,
                ServicePriorities = new()
                {
                    { "Monitoring", 10 },
                    { "Logging", 9 },
                    { "BackupService", 8 }
                },
                NetworkRules = new()
                {
                    InternetAccessAllowed = true,
                    BandwidthLimitMbps = 100
                },
                CustomSettings = new()
                {
                    { "EnableAuditLogging", true },
                    { "EnableMetricsExport", true }
                }
            };
        }
    }

    /// <summary>
    /// Profile manager interface
    /// </summary>
    public interface IProfileManager
    {
        Task<bool> SwitchProfileAsync(ProfileConfiguration newProfile);
        ProfileConfiguration GetCurrentProfile();
        ProfileConfiguration GetProfile(ProfileType type);
        IEnumerable<ProfileConfiguration> GetAllProfiles();
        Task<bool> ValidateProfileAsync(ProfileConfiguration profile);
    }

    /// <summary>
    /// Profile manager implementation
    /// </summary>
    public class ProfileManager : IProfileManager
    {
        private ProfileConfiguration _currentProfile;
        private readonly Dictionary<ProfileType, ProfileConfiguration> _profiles = new();
        private readonly IServiceRegistry _serviceRegistry;
        private readonly EventBus _eventBus;
        private readonly object _lock = new();

        public ProfileManager(IServiceRegistry serviceRegistry, EventBus eventBus)
        {
            _serviceRegistry = serviceRegistry;
            _eventBus = eventBus;
            InitializeDefaultProfiles();
            _currentProfile = _profiles[ProfileType.Developer]; // Default profile
        }

        public async Task<bool> SwitchProfileAsync(ProfileConfiguration newProfile)
        {
            lock (_lock)
            {
                if (!ValidateProfileAsync(newProfile).Result)
                {
                    return false;
                }

                var oldProfile = _currentProfile;
                _currentProfile = newProfile;

                _eventBus?.PublishEvent(new Event
                {
                    EventType = CommonEventTypes.ProfileChanged,
                    Data = new
                    {
                        FromProfile = oldProfile.Name,
                        ToProfile = newProfile.Name,
                        Timestamp = DateTime.UtcNow
                    }
                });

                return true;
            }
        }

        public ProfileConfiguration GetCurrentProfile()
        {
            lock (_lock)
            {
                return _currentProfile;
            }
        }

        public ProfileConfiguration GetProfile(ProfileType type)
        {
            lock (_lock)
            {
                _profiles.TryGetValue(type, out var profile);
                return profile;
            }
        }

        public IEnumerable<ProfileConfiguration> GetAllProfiles()
        {
            lock (_lock)
            {
                return new List<ProfileConfiguration>(_profiles.Values);
            }
        }

        public async Task<bool> ValidateProfileAsync(ProfileConfiguration profile)
        {
            if (profile == null)
                return false;

            if (profile.GpuAllocationPercentage < 0 || profile.GpuAllocationPercentage > 100)
                return false;

            if (profile.ServicePriorities != null)
            {
                foreach (var priority in profile.ServicePriorities.Values)
                {
                    if (priority < 0 || priority > 10)
                        return false;
                }
            }

            return true;
        }

        private void InitializeDefaultProfiles()
        {
            _profiles[ProfileType.Gamer] = PredefinedProfiles.CreateGamerProfile();
            _profiles[ProfileType.Developer] = PredefinedProfiles.CreateDeveloperProfile();
            _profiles[ProfileType.AIResearch] = PredefinedProfiles.CreateAIResearchProfile();
            _profiles[ProfileType.Secure] = PredefinedProfiles.CreateSecureProfile();
            _profiles[ProfileType.Enterprise] = PredefinedProfiles.CreateEnterpriseProfile();
        }

        /// <summary>
        /// Apply profile settings to services
        /// </summary>
        public async Task ApplyProfileAsync(ProfileConfiguration profile)
        {
            foreach (var service in _serviceRegistry.GetAllServices())
            {
                if (profile.ServicePriorities.TryGetValue(service.Name, out var priority))
                {
                    // Apply priority settings
                }

                if (profile.ServiceResourceConstraints.TryGetValue(service.Name, out var constraints))
                {
                    // Apply resource constraints
                }
            }
        }
    }
}
