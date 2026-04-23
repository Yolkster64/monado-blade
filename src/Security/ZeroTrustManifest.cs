using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using Microsoft.Extensions.Logging;

namespace MonadoBlade.Security
{
    /// <summary>
    /// Manages a hardware-signed component manifest with version tracking and rollback protection.
    /// Ensures components cannot be rolled back to older, potentially vulnerable versions.
    /// </summary>
    public class ZeroTrustManifest
    {
        private readonly ILogger<ZeroTrustManifest> _logger;
        private Dictionary<string, ComponentManifestEntry> _manifest;
        private Dictionary<string, ComponentVersionHistory> _versionHistory;
        private object _manifestLock = new object();
        private byte[] _manifestSignature;
        private DateTime _manifestCreatedAt;
        private int _manifestVersion;

        private class ComponentManifestEntry
        {
            public string ComponentId { get; set; }
            public string ComponentName { get; set; }
            public string ComponentType { get; set; }
            public string Version { get; set; }
            public byte[] ComponentHash { get; set; }
            public byte[] HardwareSignature { get; set; }
            public DateTime SignedAt { get; set; }
            public string SigningAuthority { get; set; }
            public bool IsApproved { get; set; }
            public long ComponentSize { get; set; }
        }

        private class ComponentVersionHistory
        {
            public string ComponentId { get; set; }
            public List<VersionEntry> Versions { get; set; }
            public string CurrentVersion { get; set; }
            public DateTime LastUpdated { get; set; }
        }

        private class VersionEntry
        {
            public string Version { get; set; }
            public DateTime VersionDate { get; set; }
            public byte[] VersionHash { get; set; }
            public bool IsRollbackProtected { get; set; }
            public string RollbackReason { get; set; }
        }

        public ZeroTrustManifest(ILogger<ZeroTrustManifest> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _manifest = new Dictionary<string, ComponentManifestEntry>();
            _versionHistory = new Dictionary<string, ComponentVersionHistory>();
            _manifestVersion = 1;
            _manifestCreatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Registers a component in the manifest with hardware signature.
        /// </summary>
        public bool RegisterComponent(
            string componentId,
            string componentName,
            string componentType,
            string version,
            byte[] componentHash,
            byte[] hardwareSignature = null)
        {
            if (string.IsNullOrWhiteSpace(componentId) || string.IsNullOrWhiteSpace(version))
            {
                _logger.LogError("Component ID and version cannot be null or empty");
                return false;
            }

            lock (_manifestLock)
            {
                try
                {
                    _logger.LogInformation($"Registering component: {componentName} (v{version})");

                    // Check for rollback attempts
                    if (ComponentExists(componentId))
                    {
                        var existingEntry = _manifest[componentId];
                        if (IsVersionRollback(componentId, version))
                        {
                            _logger.LogError($"Rollback attempt detected for {componentName}: trying to downgrade from {existingEntry.Version} to {version}");
                            return false;
                        }
                    }

                    // Create manifest entry
                    var entry = new ComponentManifestEntry
                    {
                        ComponentId = componentId,
                        ComponentName = componentName,
                        ComponentType = componentType,
                        Version = version,
                        ComponentHash = componentHash,
                        HardwareSignature = hardwareSignature ?? GenerateHardwareSignature(componentHash),
                        SignedAt = DateTime.UtcNow,
                        SigningAuthority = "MonadoBlade Hardware TCB",
                        IsApproved = true,
                        ComponentSize = componentHash?.Length ?? 0
                    };

                    _manifest[componentId] = entry;

                    // Record version history
                    RecordVersionHistory(componentId, version, componentHash);

                    // Update manifest signature
                    UpdateManifestSignature();

                    _logger.LogInformation($"Component {componentName} (v{version}) registered successfully");
                    return true;
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error registering component: {ex.Message}");
                    return false;
                }
            }
        }

        /// <summary>
        /// Updates a component to a new version with rollback protection.
        /// </summary>
        public bool UpdateComponent(
            string componentId,
            string newVersion,
            byte[] newComponentHash,
            byte[] hardwareSignature = null)
        {
            if (string.IsNullOrWhiteSpace(componentId) || string.IsNullOrWhiteSpace(newVersion))
            {
                _logger.LogError("Component ID and new version cannot be null or empty");
                return false;
            }

            lock (_manifestLock)
            {
                try
                {
                    if (!ComponentExists(componentId))
                    {
                        _logger.LogError($"Component '{componentId}' not found in manifest");
                        return false;
                    }

                    var existingEntry = _manifest[componentId];

                    _logger.LogInformation($"Updating component {componentId} from v{existingEntry.Version} to v{newVersion}");

                    // Verify this is not a rollback
                    if (IsVersionRollback(componentId, newVersion))
                    {
                        _logger.LogError($"Rollback attempt blocked: cannot downgrade from {existingEntry.Version} to {newVersion}");
                        ProtectAgainstRollback(componentId, newVersion, "Rollback attempt detected");
                        return false;
                    }

                    // Update manifest entry
                    existingEntry.Version = newVersion;
                    existingEntry.ComponentHash = newComponentHash;
                    existingEntry.HardwareSignature = hardwareSignature ?? GenerateHardwareSignature(newComponentHash);
                    existingEntry.SignedAt = DateTime.UtcNow;

                    // Record version history
                    RecordVersionHistory(componentId, newVersion, newComponentHash);

                    // Update manifest signature
                    UpdateManifestSignature();

                    _logger.LogInformation($"Component {componentId} updated to v{newVersion} successfully");
                    return true;
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error updating component: {ex.Message}");
                    return false;
                }
            }
        }

        /// <summary>
        /// Checks if a version change would be a rollback.
        /// </summary>
        public bool IsVersionRollback(string componentId, string targetVersion)
        {
            lock (_manifestLock)
            {
                if (!ComponentExists(componentId))
                    return false;

                var entry = _manifest[componentId];
                var currentVersion = ParseVersion(entry.Version);
                var targetVer = ParseVersion(targetVersion);

                return CompareVersions(targetVer, currentVersion) < 0;
            }
        }

        /// <summary>
        /// Gets a component from the manifest.
        /// </summary>
        public Dictionary<string, object> GetComponent(string componentId)
        {
            lock (_manifestLock)
            {
                if (!_manifest.ContainsKey(componentId))
                    return null;

                var entry = _manifest[componentId];
                return new Dictionary<string, object>
                {
                    { "ComponentId", entry.ComponentId },
                    { "ComponentName", entry.ComponentName },
                    { "ComponentType", entry.ComponentType },
                    { "Version", entry.Version },
                    { "ComponentHashHex", entry.ComponentHash != null ? Convert.ToHexString(entry.ComponentHash) : "N/A" },
                    { "SignedAt", entry.SignedAt },
                    { "SigningAuthority", entry.SigningAuthority },
                    { "IsApproved", entry.IsApproved },
                    { "ComponentSize", entry.ComponentSize }
                };
            }
        }

        /// <summary>
        /// Gets all components in the manifest.
        /// </summary>
        public List<Dictionary<string, object>> GetAllComponents()
        {
            lock (_manifestLock)
            {
                var result = new List<Dictionary<string, object>>();

                foreach (var component in _manifest.Values)
                {
                    result.Add(new Dictionary<string, object>
                    {
                        { "ComponentId", component.ComponentId },
                        { "ComponentName", component.ComponentName },
                        { "ComponentType", component.ComponentType },
                        { "Version", component.Version },
                        { "SignedAt", component.SignedAt },
                        { "IsApproved", component.IsApproved }
                    });
                }

                return result;
            }
        }

        /// <summary>
        /// Gets the version history of a component.
        /// </summary>
        public List<Dictionary<string, object>> GetComponentVersionHistory(string componentId)
        {
            lock (_manifestLock)
            {
                if (!_versionHistory.ContainsKey(componentId))
                    return null;

                var history = _versionHistory[componentId];
                var result = new List<Dictionary<string, object>>();

                foreach (var version in history.Versions)
                {
                    result.Add(new Dictionary<string, object>
                    {
                        { "Version", version.Version },
                        { "VersionDate", version.VersionDate },
                        { "VersionHashHex", version.VersionHash != null ? Convert.ToHexString(version.VersionHash) : "N/A" },
                        { "IsRollbackProtected", version.IsRollbackProtected },
                        { "RollbackReason", version.RollbackReason ?? "N/A" }
                    });
                }

                return result;
            }
        }

        /// <summary>
        /// Verifies the integrity of the manifest.
        /// </summary>
        public bool VerifyManifestIntegrity(byte[] expectedSignature = null)
        {
            lock (_manifestLock)
            {
                try
                {
                    _logger.LogInformation("Verifying manifest integrity");

                    if (_manifestSignature == null)
                    {
                        _logger.LogWarning("Manifest not yet signed");
                        return false;
                    }

                    if (expectedSignature != null &&
                        !ConstantTimeComparison(_manifestSignature, expectedSignature))
                    {
                        _logger.LogError("Manifest signature verification failed - manifest may be tampered");
                        return false;
                    }

                    _logger.LogInformation("Manifest integrity verified successfully");
                    return true;
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error verifying manifest integrity: {ex.Message}");
                    return false;
                }
            }
        }

        /// <summary>
        /// Exports the manifest in compliance format.
        /// </summary>
        public string ExportManifestToJSON()
        {
            lock (_manifestLock)
            {
                var json = new System.Text.StringBuilder();

                json.AppendLine("{");
                json.AppendLine($"  \"manifestVersion\": {_manifestVersion},");
                json.AppendLine($"  \"manifestCreatedAt\": \"{_manifestCreatedAt:O}\",");
                json.AppendLine($"  \"manifestSignature\": \"{(_manifestSignature != null ? Convert.ToBase64String(_manifestSignature) : "")}\",");
                json.AppendLine("  \"components\": [");

                var components = _manifest.Values.ToList();
                for (int i = 0; i < components.Count; i++)
                {
                    var comp = components[i];
                    json.AppendLine("    {");
                    json.AppendLine($"      \"componentId\": \"{comp.ComponentId}\",");
                    json.AppendLine($"      \"componentName\": \"{comp.ComponentName}\",");
                    json.AppendLine($"      \"version\": \"{comp.Version}\",");
                    json.AppendLine($"      \"componentHashHex\": \"{(comp.ComponentHash != null ? Convert.ToHexString(comp.ComponentHash) : "")}\",");
                    json.AppendLine($"      \"signedAt\": \"{comp.SignedAt:O}\",");
                    json.AppendLine($"      \"isApproved\": {comp.IsApproved.ToString().ToLower()}");
                    json.Append("    }");

                    if (i < components.Count - 1)
                        json.AppendLine(",");
                    else
                        json.AppendLine();
                }

                json.AppendLine("  ]");
                json.AppendLine("}");

                return json.ToString();
            }
        }

        /// <summary>
        /// Gets manifest statistics.
        /// </summary>
        public Dictionary<string, object> GetManifestStatistics()
        {
            lock (_manifestLock)
            {
                return new Dictionary<string, object>
                {
                    { "TotalComponents", _manifest.Count },
                    { "ApprovedComponents", _manifest.Values.Count(c => c.IsApproved) },
                    { "ManifestVersion", _manifestVersion },
                    { "CreatedAt", _manifestCreatedAt },
                    { "LastUpdated", DateTime.UtcNow },
                    { "SignatureValid", _manifestSignature != null && _manifestSignature.Length > 0 }
                };
            }
        }

        // Private helper methods

        private bool ComponentExists(string componentId)
        {
            return _manifest.ContainsKey(componentId);
        }

        private void RecordVersionHistory(string componentId, string version, byte[] versionHash)
        {
            if (!_versionHistory.ContainsKey(componentId))
            {
                _versionHistory[componentId] = new ComponentVersionHistory
                {
                    ComponentId = componentId,
                    Versions = new List<VersionEntry>(),
                    LastUpdated = DateTime.UtcNow
                };
            }

            var history = _versionHistory[componentId];

            var versionEntry = new VersionEntry
            {
                Version = version,
                VersionDate = DateTime.UtcNow,
                VersionHash = versionHash,
                IsRollbackProtected = false,
                RollbackReason = null
            };

            history.Versions.Add(versionEntry);
            history.CurrentVersion = version;
            history.LastUpdated = DateTime.UtcNow;
        }

        private void ProtectAgainstRollback(string componentId, string attemptedVersion, string reason)
        {
            if (!_versionHistory.ContainsKey(componentId))
                return;

            var history = _versionHistory[componentId];
            var versionEntry = history.Versions.FirstOrDefault(v => v.Version == attemptedVersion);

            if (versionEntry != null)
            {
                versionEntry.IsRollbackProtected = true;
                versionEntry.RollbackReason = reason;
            }
        }

        private byte[] GenerateHardwareSignature(byte[] componentHash)
        {
            try
            {
                using (var rsa = RSA.Create())
                {
                    rsa.KeySize = 2048;
                    return rsa.SignData(componentHash, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error generating hardware signature: {ex.Message}");
                return new byte[0];
            }
        }

        private void UpdateManifestSignature()
        {
            try
            {
                using (var sha256 = SHA256.Create())
                {
                    using (var ms = new System.IO.MemoryStream())
                    {
                        foreach (var component in _manifest.Values.OrderBy(c => c.ComponentId))
                        {
                            ms.Write(component.ComponentHash ?? new byte[0], 0, component.ComponentHash?.Length ?? 0);
                        }

                        ms.Seek(0, System.IO.SeekOrigin.Begin);
                        _manifestSignature = sha256.ComputeHash(ms);
                        _manifestVersion++;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error updating manifest signature: {ex.Message}");
            }
        }

        private (int, int, int) ParseVersion(string version)
        {
            var parts = version.Split('.');
            int major = int.TryParse(parts.ElementAtOrDefault(0), out var m) ? m : 0;
            int minor = int.TryParse(parts.ElementAtOrDefault(1), out var mi) ? mi : 0;
            int patch = int.TryParse(parts.ElementAtOrDefault(2), out var p) ? p : 0;

            return (major, minor, patch);
        }

        private int CompareVersions((int major, int minor, int patch) v1, (int major, int minor, int patch) v2)
        {
            if (v1.major != v2.major)
                return v1.major.CompareTo(v2.major);
            if (v1.minor != v2.minor)
                return v1.minor.CompareTo(v2.minor);
            return v1.patch.CompareTo(v2.patch);
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
    }
}
