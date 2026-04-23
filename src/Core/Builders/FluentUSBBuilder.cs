using System;
using System.Collections.Generic;

namespace MonadoBlade.Core.Builders
{
    /// <summary>
    /// Fluent builder for USB device deployment configuration.
    /// Provides chainable API for configuring USB-based deployments.
    /// </summary>
    public class FluentUSBBuilder
    {
        private string _deviceName;
        private string _profileName;
        private bool _buildInBackground;
        private string _usbDrivePath;
        private long _timeoutSeconds;
        private bool _verifyIntegrity;
        private Dictionary<string, string> _deploymentOptions;

        public FluentUSBBuilder()
        {
            _deploymentOptions = new Dictionary<string, string>();
            _timeoutSeconds = 300;
            _verifyIntegrity = true;
        }

        /// <summary>
        /// Specifies target device for USB deployment.
        /// </summary>
        public FluentUSBBuilder ForDevice(string deviceName)
        {
            if (string.IsNullOrWhiteSpace(deviceName))
                throw new ArgumentException("Device name cannot be empty", nameof(deviceName));
            _deviceName = deviceName;
            return this;
        }

        /// <summary>
        /// Specifies profile to deploy to USB.
        /// </summary>
        public FluentUSBBuilder WithProfile(string profileName)
        {
            if (string.IsNullOrWhiteSpace(profileName))
                throw new ArgumentException("Profile name cannot be empty", nameof(profileName));
            _profileName = profileName;
            return this;
        }

        /// <summary>
        /// Enables background building of USB media.
        /// </summary>
        public FluentUSBBuilder BuildInBackground()
        {
            _buildInBackground = true;
            return this;
        }

        /// <summary>
        /// Disables background building of USB media.
        /// </summary>
        public FluentUSBBuilder BuildInForeground()
        {
            _buildInBackground = false;
            return this;
        }

        /// <summary>
        /// Specifies USB drive path for deployment.
        /// </summary>
        public FluentUSBBuilder ToUSBDrive(string drivePath)
        {
            if (string.IsNullOrWhiteSpace(drivePath))
                throw new ArgumentException("USB drive path cannot be empty", nameof(drivePath));
            _usbDrivePath = drivePath;
            return this;
        }

        /// <summary>
        /// Sets timeout for deployment operation in seconds.
        /// </summary>
        public FluentUSBBuilder WithTimeout(long seconds)
        {
            if (seconds <= 0)
                throw new ArgumentException("Timeout must be greater than 0", nameof(seconds));
            _timeoutSeconds = seconds;
            return this;
        }

        /// <summary>
        /// Enables integrity verification after deployment.
        /// </summary>
        public FluentUSBBuilder VerifyIntegrity()
        {
            _verifyIntegrity = true;
            return this;
        }

        /// <summary>
        /// Disables integrity verification after deployment.
        /// </summary>
        public FluentUSBBuilder SkipIntegrityVerification()
        {
            _verifyIntegrity = false;
            return this;
        }

        /// <summary>
        /// Adds deployment option.
        /// </summary>
        public FluentUSBBuilder WithOption(string key, string value)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Option key cannot be empty", nameof(key));
            _deploymentOptions[key] = value ?? string.Empty;
            return this;
        }

        /// <summary>
        /// Enables fast deployment mode (skips some verifications).
        /// </summary>
        public FluentUSBBuilder FastMode()
        {
            _deploymentOptions["FastMode"] = "true";
            return this;
        }

        /// <summary>
        /// Enables verbose logging during deployment.
        /// </summary>
        public FluentUSBBuilder WithVerboseLogging()
        {
            _deploymentOptions["VerboseLogging"] = "true";
            return this;
        }

        /// <summary>
        /// Validates configuration and builds USB deployment configuration.
        /// </summary>
        public USBDeploymentConfiguration Build()
        {
            if (string.IsNullOrWhiteSpace(_deviceName))
                throw new InvalidOperationException("Device name is required");
            if (string.IsNullOrWhiteSpace(_profileName))
                throw new InvalidOperationException("Profile name is required");
            if (string.IsNullOrWhiteSpace(_usbDrivePath))
                throw new InvalidOperationException("USB drive path is required");

            return new USBDeploymentConfiguration
            {
                DeviceName = _deviceName,
                ProfileName = _profileName,
                BuildInBackground = _buildInBackground,
                USBDrivePath = _usbDrivePath,
                TimeoutSeconds = _timeoutSeconds,
                VerifyIntegrity = _verifyIntegrity,
                DeploymentOptions = new Dictionary<string, string>(_deploymentOptions)
            };
        }
    }

    /// <summary>
    /// Immutable USB deployment configuration object.
    /// </summary>
    public class USBDeploymentConfiguration
    {
        public string DeviceName { get; set; }
        public string ProfileName { get; set; }
        public bool BuildInBackground { get; set; }
        public string USBDrivePath { get; set; }
        public long TimeoutSeconds { get; set; }
        public bool VerifyIntegrity { get; set; }
        public Dictionary<string, string> DeploymentOptions { get; set; }

        public override string ToString() 
            => $"Deploy {ProfileName} to {DeviceName} on {USBDrivePath} | Background: {BuildInBackground} | Verify: {VerifyIntegrity}";
    }
}
