using System;
using System.Collections.Generic;

namespace MonadoBlade.Core.Builders
{
    /// <summary>
    /// Fluent builder for boot configuration with comprehensive security options.
    /// Provides chainable API for configuring boot parameters and security settings.
    /// </summary>
    public class FluentBootConfigBuilder
    {
        private int _bootTimeout;
        private bool _secureBoot;
        private string _defaultOS;
        private bool _uefiEnabled;
        private bool _tpm2Enabled;
        private string _bootOrder;
        private Dictionary<string, string> _bootParams;

        public FluentBootConfigBuilder()
        {
            _bootParams = new Dictionary<string, string>();
            _bootTimeout = 10;
            _secureBoot = true;
            _uefiEnabled = true;
            _tpm2Enabled = false;
        }

        /// <summary>
        /// Sets boot timeout in seconds.
        /// </summary>
        public FluentBootConfigBuilder SetBootTimeout(int seconds)
        {
            if (seconds < 0)
                throw new ArgumentException("Boot timeout cannot be negative", nameof(seconds));
            _bootTimeout = seconds;
            return this;
        }

        /// <summary>
        /// Enables UEFI Secure Boot.
        /// </summary>
        public FluentBootConfigBuilder EnableSecureBoot()
        {
            _secureBoot = true;
            return this;
        }

        /// <summary>
        /// Disables UEFI Secure Boot.
        /// </summary>
        public FluentBootConfigBuilder DisableSecureBoot()
        {
            _secureBoot = false;
            return this;
        }

        /// <summary>
        /// Sets default OS to boot.
        /// </summary>
        public FluentBootConfigBuilder SetDefaultOS(string osName)
        {
            if (string.IsNullOrWhiteSpace(osName))
                throw new ArgumentException("OS name cannot be empty", nameof(osName));
            _defaultOS = osName;
            return this;
        }

        /// <summary>
        /// Enables UEFI boot mode.
        /// </summary>
        public FluentBootConfigBuilder EnableUEFI()
        {
            _uefiEnabled = true;
            return this;
        }

        /// <summary>
        /// Disables UEFI boot mode (legacy BIOS).
        /// </summary>
        public FluentBootConfigBuilder DisableUEFI()
        {
            _uefiEnabled = false;
            return this;
        }

        /// <summary>
        /// Enables TPM 2.0 measured boot.
        /// </summary>
        public FluentBootConfigBuilder EnableTPM2()
        {
            _tpm2Enabled = true;
            return this;
        }

        /// <summary>
        /// Disables TPM 2.0 measured boot.
        /// </summary>
        public FluentBootConfigBuilder DisableTPM2()
        {
            _tpm2Enabled = false;
            return this;
        }

        /// <summary>
        /// Sets boot device order (comma-separated).
        /// </summary>
        public FluentBootConfigBuilder SetBootOrder(string order)
        {
            if (string.IsNullOrWhiteSpace(order))
                throw new ArgumentException("Boot order cannot be empty", nameof(order));
            _bootOrder = order;
            return this;
        }

        /// <summary>
        /// Adds custom boot parameter.
        /// </summary>
        public FluentBootConfigBuilder WithParameter(string key, string value)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Parameter key cannot be empty", nameof(key));
            _bootParams[key] = value ?? string.Empty;
            return this;
        }

        /// <summary>
        /// Enables fast boot mode.
        /// </summary>
        public FluentBootConfigBuilder FastBoot()
        {
            _bootParams["FastBoot"] = "true";
            return this;
        }

        /// <summary>
        /// Enables verbose boot output.
        /// </summary>
        public FluentBootConfigBuilder VerboseMode()
        {
            _bootParams["VerboseMode"] = "true";
            return this;
        }

        /// <summary>
        /// Enables quiet mode (minimal output).
        /// </summary>
        public FluentBootConfigBuilder QuietMode()
        {
            _bootParams["QuietMode"] = "true";
            return this;
        }

        /// <summary>
        /// Validates configuration and builds boot configuration.
        /// </summary>
        public BootConfiguration Build()
        {
            if (string.IsNullOrWhiteSpace(_defaultOS))
                throw new InvalidOperationException("Default OS is required");

            return new BootConfiguration
            {
                BootTimeout = _bootTimeout,
                SecureBootEnabled = _secureBoot,
                DefaultOS = _defaultOS,
                UEFIEnabled = _uefiEnabled,
                TPM2Enabled = _tpm2Enabled,
                BootOrder = _bootOrder,
                BootParameters = new Dictionary<string, string>(_bootParams)
            };
        }
    }

    /// <summary>
    /// Immutable boot configuration object.
    /// </summary>
    public class BootConfiguration
    {
        public int BootTimeout { get; set; }
        public bool SecureBootEnabled { get; set; }
        public string DefaultOS { get; set; }
        public bool UEFIEnabled { get; set; }
        public bool TPM2Enabled { get; set; }
        public string BootOrder { get; set; }
        public Dictionary<string, string> BootParameters { get; set; }

        public override string ToString() 
            => $"Boot: {DefaultOS} | Timeout: {BootTimeout}s | SecureBoot: {SecureBootEnabled} | UEFI: {UEFIEnabled} | TPM2: {TPM2Enabled}";
    }
}
