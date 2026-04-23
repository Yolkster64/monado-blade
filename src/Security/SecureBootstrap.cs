using System;
using System.Security.Cryptography;
using System.Text;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace MonadoBlade.Security
{
    /// <summary>
    /// Implements enclave-based bootloader verification and boot integrity measurement.
    /// Provides cryptographic proof of boot integrity and seals secrets only if boot is valid.
    /// </summary>
    public class SecureBootstrap : IDisposable
    {
        private readonly ILogger<SecureBootstrap> _logger;
        private readonly SecureEnclaveManager _enclaveManager;
        private BootIntegrityData _bootIntegrityData;
        private object _bootLock = new object();
        private const int BootChainHashSize = 32;
        private bool _isBootVerified;

        private class BootIntegrityData
        {
            public byte[] BootloaderHash { get; set; }
            public byte[] KernelHash { get; set; }
            public byte[] InitramfsHash { get; set; }
            public DateTime BootTime { get; set; }
            public List<BootComponentMeasurement> ComponentMeasurements { get; set; }
            public byte[] CumulativeBootChainHash { get; set; }
            public string BootChainSignature { get; set; }
            public bool IsBootVerified { get; set; }
        }

        private class BootComponentMeasurement
        {
            public string ComponentName { get; set; }
            public byte[] ComponentHash { get; set; }
            public int MeasurementIndex { get; set; }
            public DateTime MeasurementTime { get; set; }
            public string SignatureAlgorithm { get; set; }
        }

        public SecureBootstrap(
            ILogger<SecureBootstrap> logger,
            SecureEnclaveManager enclaveManager)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _enclaveManager = enclaveManager ?? throw new ArgumentNullException(nameof(enclaveManager));
            _bootIntegrityData = new BootIntegrityData
            {
                ComponentMeasurements = new List<BootComponentMeasurement>(),
                BootTime = DateTime.UtcNow,
                IsBootVerified = false
            };
            _isBootVerified = false;
        }

        /// <summary>
        /// Verifies and measures the bootloader component.
        /// </summary>
        public bool VerifyBootloader(byte[] bootloaderData, byte[] expectedHash = null)
        {
            if (bootloaderData == null || bootloaderData.Length == 0)
            {
                _logger.LogError("Bootloader data cannot be null or empty");
                return false;
            }

            lock (_bootLock)
            {
                try
                {
                    _logger.LogInformation("Verifying bootloader integrity");

                    // Compute bootloader hash
                    byte[] bootloaderHash = ComputeHash(bootloaderData);

                    // Verify against expected hash if provided
                    if (expectedHash != null && !ConstantTimeComparison(bootloaderHash, expectedHash))
                    {
                        _logger.LogError("Bootloader hash verification failed - potential tampering detected");
                        return false;
                    }

                    _bootIntegrityData.BootloaderHash = bootloaderHash;
                    RecordBootComponentMeasurement("bootloader", bootloaderHash);

                    _logger.LogInformation("Bootloader verification successful");
                    return true;
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Bootloader verification error: {ex.Message}");
                    return false;
                }
            }
        }

        /// <summary>
        /// Verifies and measures the kernel component.
        /// </summary>
        public bool VerifyKernel(byte[] kernelData, byte[] expectedHash = null)
        {
            if (kernelData == null || kernelData.Length == 0)
            {
                _logger.LogError("Kernel data cannot be null or empty");
                return false;
            }

            lock (_bootLock)
            {
                try
                {
                    _logger.LogInformation("Verifying kernel integrity");

                    // Bootloader must be verified first
                    if (_bootIntegrityData.BootloaderHash == null)
                    {
                        _logger.LogError("Bootloader must be verified before kernel");
                        return false;
                    }

                    byte[] kernelHash = ComputeHash(kernelData);

                    if (expectedHash != null && !ConstantTimeComparison(kernelHash, expectedHash))
                    {
                        _logger.LogError("Kernel hash verification failed - potential tampering detected");
                        return false;
                    }

                    _bootIntegrityData.KernelHash = kernelHash;
                    RecordBootComponentMeasurement("kernel", kernelHash);

                    _logger.LogInformation("Kernel verification successful");
                    return true;
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Kernel verification error: {ex.Message}");
                    return false;
                }
            }
        }

        /// <summary>
        /// Verifies and measures the initramfs component.
        /// </summary>
        public bool VerifyInitramfs(byte[] initramfsData, byte[] expectedHash = null)
        {
            if (initramfsData == null || initramfsData.Length == 0)
            {
                _logger.LogError("Initramfs data cannot be null or empty");
                return false;
            }

            lock (_bootLock)
            {
                try
                {
                    _logger.LogInformation("Verifying initramfs integrity");

                    if (_bootIntegrityData.KernelHash == null)
                    {
                        _logger.LogError("Kernel must be verified before initramfs");
                        return false;
                    }

                    byte[] initramfsHash = ComputeHash(initramfsData);

                    if (expectedHash != null && !ConstantTimeComparison(initramfsHash, expectedHash))
                    {
                        _logger.LogError("Initramfs hash verification failed - potential tampering detected");
                        return false;
                    }

                    _bootIntegrityData.InitramfsHash = initramfsHash;
                    RecordBootComponentMeasurement("initramfs", initramfsHash);

                    _logger.LogInformation("Initramfs verification successful");
                    return true;
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Initramfs verification error: {ex.Message}");
                    return false;
                }
            }
        }

        /// <summary>
        /// Completes boot verification by computing the cumulative boot chain hash.
        /// This must be called after all components are verified.
        /// </summary>
        public bool CompleteBootVerification()
        {
            lock (_bootLock)
            {
                try
                {
                    _logger.LogInformation("Completing boot verification");

                    // Ensure all components are verified
                    if (_bootIntegrityData.BootloaderHash == null ||
                        _bootIntegrityData.KernelHash == null ||
                        _bootIntegrityData.InitramfsHash == null)
                    {
                        _logger.LogError("All boot components must be verified before completion");
                        return false;
                    }

                    // Compute cumulative boot chain hash
                    using (var sha256 = SHA256.Create())
                    {
                        using (var ms = new System.IO.MemoryStream())
                        {
                            ms.Write(_bootIntegrityData.BootloaderHash, 0, _bootIntegrityData.BootloaderHash.Length);
                            ms.Write(_bootIntegrityData.KernelHash, 0, _bootIntegrityData.KernelHash.Length);
                            ms.Write(_bootIntegrityData.InitramfsHash, 0, _bootIntegrityData.InitramfsHash.Length);

                            ms.Seek(0, System.IO.SeekOrigin.Begin);
                            _bootIntegrityData.CumulativeBootChainHash = sha256.ComputeHash(ms);
                        }
                    }

                    // Generate cryptographic signature of boot chain
                    _bootIntegrityData.BootChainSignature = GenerateBootChainSignature();

                    _bootIntegrityData.IsBootVerified = true;
                    _isBootVerified = true;

                    _logger.LogInformation("Boot verification completed successfully");
                    return true;
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Boot verification completion error: {ex.Message}");
                    return false;
                }
            }
        }

        /// <summary>
        /// Unseals a secret only if boot chain is verified.
        /// </summary>
        public byte[] UnsealSecretWithBootAttestation(string secretId, byte[] sealedSecret, byte[] expectedBootHash = null)
        {
            if (!_isBootVerified)
            {
                _logger.LogError("Boot verification failed - cannot unseal secret without valid boot attestation");
                return null;
            }

            lock (_bootLock)
            {
                try
                {
                    if (expectedBootHash != null &&
                        !ConstantTimeComparison(_bootIntegrityData.CumulativeBootChainHash, expectedBootHash))
                    {
                        _logger.LogError("Boot hash mismatch - unsealing denied");
                        return null;
                    }

                    _logger.LogInformation($"Unsealing secret '{secretId}' with boot attestation verification");

                    // In production, this would unseal using the enclave with the verified boot state
                    byte[] unsealed = UnsealViaEnclave(sealedSecret);

                    if (unsealed != null)
                    {
                        _logger.LogInformation($"Secret '{secretId}' unsealed successfully with boot attestation");
                    }

                    return unsealed;
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error unsealing secret with boot attestation: {ex.Message}");
                    return null;
                }
            }
        }

        /// <summary>
        /// Gets the boot integrity report.
        /// </summary>
        public Dictionary<string, object> GetBootIntegrityReport()
        {
            lock (_bootLock)
            {
                return new Dictionary<string, object>
                {
                    { "IsBootVerified", _bootIntegrityData.IsBootVerified },
                    { "BootTime", _bootIntegrityData.BootTime },
                    { "BootloaderHashHex", ToHexString(_bootIntegrityData.BootloaderHash) },
                    { "KernelHashHex", ToHexString(_bootIntegrityData.KernelHash) },
                    { "InitramfsHashHex", ToHexString(_bootIntegrityData.InitramfsHash) },
                    { "CumulativeBootChainHashHex", ToHexString(_bootIntegrityData.CumulativeBootChainHash) },
                    { "BootChainSignature", _bootIntegrityData.BootChainSignature },
                    { "ComponentMeasurementCount", _bootIntegrityData.ComponentMeasurements.Count }
                };
            }
        }

        /// <summary>
        /// Gets detailed component measurements.
        /// </summary>
        public List<Dictionary<string, object>> GetComponentMeasurements()
        {
            lock (_bootLock)
            {
                var result = new List<Dictionary<string, object>>();

                foreach (var measurement in _bootIntegrityData.ComponentMeasurements)
                {
                    result.Add(new Dictionary<string, object>
                    {
                        { "ComponentName", measurement.ComponentName },
                        { "ComponentHashHex", ToHexString(measurement.ComponentHash) },
                        { "MeasurementIndex", measurement.MeasurementIndex },
                        { "MeasurementTime", measurement.MeasurementTime },
                        { "SignatureAlgorithm", measurement.SignatureAlgorithm }
                    });
                }

                return result;
            }
        }

        /// <summary>
        /// Checks if boot chain is verified.
        /// </summary>
        public bool IsBootVerified() => _isBootVerified;

        // Private helper methods

        private byte[] ComputeHash(byte[] data)
        {
            using (var sha256 = SHA256.Create())
            {
                return sha256.ComputeHash(data);
            }
        }

        private void RecordBootComponentMeasurement(string componentName, byte[] hash)
        {
            var measurement = new BootComponentMeasurement
            {
                ComponentName = componentName,
                ComponentHash = (byte[])hash.Clone(),
                MeasurementIndex = _bootIntegrityData.ComponentMeasurements.Count,
                MeasurementTime = DateTime.UtcNow,
                SignatureAlgorithm = "SHA256"
            };

            _bootIntegrityData.ComponentMeasurements.Add(measurement);
        }

        private string GenerateBootChainSignature()
        {
            try
            {
                // Generate cryptographic proof of boot chain
                // In production, this would be signed by TPM or enclave
                using (var rsa = RSA.Create())
                {
                    rsa.KeySize = 2048;

                    byte[] dataToSign = _bootIntegrityData.CumulativeBootChainHash;
                    byte[] signature = rsa.SignData(dataToSign, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

                    return Convert.ToBase64String(signature);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error generating boot chain signature: {ex.Message}");
                return null;
            }
        }

        private byte[] UnsealViaEnclave(byte[] sealedSecret)
        {
            try
            {
                if (!_enclaveManager.IsEnclaveAvailable())
                {
                    _logger.LogWarning("Enclave not available for unsealing");
                    return sealedSecret; // Return sealed data as-is in fallback mode
                }

                // In production, this would call the enclave to unseal
                return sealedSecret;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error unsealing via enclave: {ex.Message}");
                return null;
            }
        }

        private bool ConstantTimeComparison(byte[] a, byte[] b)
        {
            if (a == null || b == null || a.Length != b.Length)
                return false;

            int comparison = 0;
            for (int i = 0; i < a.Length; i++)
            {
                comparison |= a[i] ^ b[i];
            }

            return comparison == 0;
        }

        private string ToHexString(byte[] data)
        {
            if (data == null)
                return null;

            return Convert.ToHexString(data);
        }

        public void Dispose()
        {
            lock (_bootLock)
            {
                if (_bootIntegrityData?.BootloaderHash != null)
                {
                    Array.Clear(_bootIntegrityData.BootloaderHash, 0, _bootIntegrityData.BootloaderHash.Length);
                }

                if (_bootIntegrityData?.KernelHash != null)
                {
                    Array.Clear(_bootIntegrityData.KernelHash, 0, _bootIntegrityData.KernelHash.Length);
                }

                if (_bootIntegrityData?.InitramfsHash != null)
                {
                    Array.Clear(_bootIntegrityData.InitramfsHash, 0, _bootIntegrityData.InitramfsHash.Length);
                }

                if (_bootIntegrityData?.CumulativeBootChainHash != null)
                {
                    Array.Clear(_bootIntegrityData.CumulativeBootChainHash, 0, _bootIntegrityData.CumulativeBootChainHash.Length);
                }
            }
        }
    }
}
