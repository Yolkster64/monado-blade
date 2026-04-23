using System;
using System.Collections.Generic;

namespace MonadoBlade.Core.Builders
{
    /// <summary>
    /// Fluent builder for GPU profile configuration.
    /// Provides chainable, type-safe API for configuring GPU settings.
    /// </summary>
    public class FluentProfileBuilder
    {
        private bool _gpuEnabled;
        private bool _secureModeEnabled;
        private bool _dualBootEnabled;
        private string _profileName;
        private int _cpuCores;
        private long _ramMB;
        private string _bootDevice;
        private Dictionary<string, string> _customSettings;

        public FluentProfileBuilder()
        {
            _customSettings = new Dictionary<string, string>();
            _cpuCores = Environment.ProcessorCount;
            _ramMB = 8192;
        }

        /// <summary>
        /// Sets profile name.
        /// </summary>
        public FluentProfileBuilder WithName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Profile name cannot be empty", nameof(name));
            _profileName = name;
            return this;
        }

        /// <summary>
        /// Enables GPU acceleration for the profile.
        /// </summary>
        public FluentProfileBuilder WithGPU()
        {
            _gpuEnabled = true;
            return this;
        }

        /// <summary>
        /// Disables GPU acceleration for the profile.
        /// </summary>
        public FluentProfileBuilder WithoutGPU()
        {
            _gpuEnabled = false;
            return this;
        }

        /// <summary>
        /// Enables secure boot mode with TPM verification.
        /// </summary>
        public FluentProfileBuilder WithSecureMode()
        {
            _secureModeEnabled = true;
            return this;
        }

        /// <summary>
        /// Disables secure boot mode.
        /// </summary>
        public FluentProfileBuilder WithoutSecureMode()
        {
            _secureModeEnabled = false;
            return this;
        }

        /// <summary>
        /// Enables dual boot configuration.
        /// </summary>
        public FluentProfileBuilder WithDualBoot()
        {
            _dualBootEnabled = true;
            return this;
        }

        /// <summary>
        /// Disables dual boot configuration.
        /// </summary>
        public FluentProfileBuilder WithoutDualBoot()
        {
            _dualBootEnabled = false;
            return this;
        }

        /// <summary>
        /// Sets CPU core allocation.
        /// </summary>
        public FluentProfileBuilder WithCPUCores(int cores)
        {
            if (cores <= 0)
                throw new ArgumentException("CPU cores must be greater than 0", nameof(cores));
            _cpuCores = cores;
            return this;
        }

        /// <summary>
        /// Sets RAM allocation in MB.
        /// </summary>
        public FluentProfileBuilder WithRAM(long megabytes)
        {
            if (megabytes <= 0)
                throw new ArgumentException("RAM must be greater than 0", nameof(megabytes));
            _ramMB = megabytes;
            return this;
        }

        /// <summary>
        /// Sets boot device path.
        /// </summary>
        public FluentProfileBuilder WithBootDevice(string devicePath)
        {
            if (string.IsNullOrWhiteSpace(devicePath))
                throw new ArgumentException("Boot device cannot be empty", nameof(devicePath));
            _bootDevice = devicePath;
            return this;
        }

        /// <summary>
        /// Adds custom setting to profile.
        /// </summary>
        public FluentProfileBuilder WithSetting(string key, string value)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Setting key cannot be empty", nameof(key));
            _customSettings[key] = value ?? string.Empty;
            return this;
        }

        /// <summary>
        /// Validates configuration and builds profile.
        /// </summary>
        public ProfileConfiguration Build()
        {
            if (string.IsNullOrWhiteSpace(_profileName))
                throw new InvalidOperationException("Profile name is required");

            return new ProfileConfiguration
            {
                Name = _profileName,
                GPUEnabled = _gpuEnabled,
                SecureModeEnabled = _secureModeEnabled,
                DualBootEnabled = _dualBootEnabled,
                CPUCores = _cpuCores,
                RAMMB = _ramMB,
                BootDevice = _bootDevice,
                CustomSettings = new Dictionary<string, string>(_customSettings)
            };
        }
    }

    /// <summary>
    /// Immutable profile configuration object.
    /// </summary>
    public class ProfileConfiguration
    {
        public string Name { get; set; }
        public bool GPUEnabled { get; set; }
        public bool SecureModeEnabled { get; set; }
        public bool DualBootEnabled { get; set; }
        public int CPUCores { get; set; }
        public long RAMMB { get; set; }
        public string BootDevice { get; set; }
        public Dictionary<string, string> CustomSettings { get; set; }

        public override string ToString() => $"Profile: {Name} | GPU: {GPUEnabled} | Secure: {SecureModeEnabled} | DualBoot: {DualBootEnabled}";
    }
}
