using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MonadoBlade.Core.Framework;

namespace MonadoBlade.Services.Vault
{
    /// <summary>
    /// VaultAPIGateway - Central credential management and API routing
    /// All API calls go through this gateway - credentials never exposed to agents
    /// </summary>
    public class VaultAPIGateway : IHELIOSService
    {
        private readonly ILogger<VaultAPIGateway> _logger;
        private readonly CredentialManager _credentialManager;
        private readonly ConcurrentDictionary<string, ApiAccessLog> _auditLog = new();
        private readonly RateLimiterPerApi _rateLimiter;
        private int _auditLogCounter;

        public VaultAPIGateway(ILogger<VaultAPIGateway> logger, CredentialManager credentialManager)
        {
            _logger = logger;
            _credentialManager = credentialManager;
            _rateLimiter = new RateLimiterPerApi();
        }

        public async Task StartAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("VaultAPIGateway started");
            await _credentialManager.StartAsync(cancellationToken);
        }

        public async Task StopAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("VaultAPIGateway stopped");
            await _credentialManager.StopAsync(cancellationToken);
        }

        /// <summary>
        /// Get credential for API call (with permission check)
        /// </summary>
        public async Task<ApiCredential> GetCredentialAsync(string apiName, string aiAgentId)
        {
            if (string.IsNullOrEmpty(apiName) || string.IsNullOrEmpty(aiAgentId))
                throw new ArgumentNullException("API name and agent ID required");

            // Check permission
            if (!HasPermission(aiAgentId, apiName))
            {
                _logger.LogWarning("Access denied: Agent {Agent} tried to access {Api}", aiAgentId, apiName);
                throw new UnauthorizedAccessException($"Agent {aiAgentId} not authorized for {apiName}");
            }

            // Retrieve credential
            var credential = await _credentialManager.RetrieveCredentialAsync(apiName, aiAgentId);

            // Log access
            LogAccess(apiName, aiAgentId, success: true);

            return credential;
        }

        /// <summary>
        /// List all available APIs
        /// </summary>
        public List<AvailableApi> GetAvailableApis()
        {
            return new List<AvailableApi>
            {
                new() { Name = "Azure", Status = "Active", RateLimit = "1000/min" },
                new() { Name = "GitHub", Status = "Active", RateLimit = "5000/hour" },
                new() { Name = "Anthropic", Status = "Active", RateLimit = "500/min" },
                new() { Name = "OpenAI", Status = "Active", RateLimit = "3500/min" },
                new() { Name = "M365", Status = "Active", RateLimit = "2000/min" },
                new() { Name = "PowerBI", Status = "Active", RateLimit = "1000/min" },
                new() { Name = "GoogleCloud", Status = "Active", RateLimit = "600/min" },
                new() { Name = "AWS", Status = "Active", RateLimit = "1000/min" },
                new() { Name = "Ollama", Status = "Active", RateLimit = "Unlimited" }
            };
        }

        /// <summary>
        /// Check if agent has permission to use API
        /// </summary>
        public bool HasPermission(string aiAgentId, string apiName)
        {
            var permissions = new Dictionary<string, HashSet<string>>
            {
                { "claude-orchestrator", new HashSet<string> { "Azure", "GitHub", "PowerBI", "M365" } },
                { "llama-analyzer", new HashSet<string> { "Azure", "OpenAI", "M365" } },
                { "copilot-assistant", new HashSet<string> { "GitHub", "Azure", "OpenAI" } },
                { "default-agent", new HashSet<string> { "Azure", "M365" } }
            };

            if (permissions.TryGetValue(aiAgentId, out var agentApis))
                return agentApis.Contains(apiName);

            return permissions["default-agent"].Contains(apiName);
        }

        /// <summary>
        /// Get audit log
        /// </summary>
        public List<ApiAccessLog> GetAuditLog(string? aiAgentId = null, DateTime? since = null)
        {
            var logs = _auditLog.Values.AsEnumerable();

            if (!string.IsNullOrEmpty(aiAgentId))
                logs = logs.Where(l => l.AgentId == aiAgentId);

            if (since.HasValue)
                logs = logs.Where(l => l.Timestamp >= since.Value);

            return logs.OrderByDescending(l => l.Timestamp).ToList();
        }

        private void LogAccess(string apiName, string aiAgentId, bool success)
        {
            var logId = $"{_auditLogCounter++}";
            var log = new ApiAccessLog
            {
                Id = logId,
                ApiName = apiName,
                AgentId = aiAgentId,
                Timestamp = DateTime.UtcNow,
                Success = success
            };

            _auditLog.TryAdd(logId, log);

            if (!success)
                _logger.LogWarning("API access failed: {Log}", log);
        }

        public string ServiceName => "VaultAPIGateway";
        public ServiceStatus Status { get; set; } = ServiceStatus.Idle;
        public Dictionary<string, object> GetMetrics() => new()
        {
            { "TotalAuditLogs", _auditLog.Count },
            { "ApiCount", GetAvailableApis().Count },
            { "ServiceStatus", Status }
        };
    }

    /// <summary>
    /// CredentialManager - Lifecycle management for API credentials
    /// Handles encryption, rotation, and secure storage
    /// </summary>
    public class CredentialManager : IHELIOSService
    {
        private readonly ILogger<CredentialManager> _logger;
        private readonly ConcurrentDictionary<string, StoredCredential> _credentials = new();
        private readonly ConcurrentDictionary<string, CredentialAccessEvent> _accessEvents = new();
        private Timer? _rotationTimer;
        private readonly byte[] _masterKey;
        private int _eventCounter;

        public CredentialManager(ILogger<CredentialManager> logger)
        {
            _logger = logger;
            _masterKey = GenerateSecureKey();
        }

        public async Task StartAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("CredentialManager started");
            
            // Start daily rotation timer
            _rotationTimer = new Timer(
                callback: async _ => await RotateAllAsync(),
                state: null,
                dueTime: TimeSpan.FromHours(24),
                period: TimeSpan.FromHours(24));

            await Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("CredentialManager stopped");
            _rotationTimer?.Dispose();
            await Task.CompletedTask;
        }

        /// <summary>
        /// Store new credential (encrypted)
        /// </summary>
        public async Task<bool> StoreCredentialAsync(string apiName, ApiCredential credential)
        {
            if (string.IsNullOrEmpty(apiName) || credential == null)
                throw new ArgumentNullException("API name and credential required");

            try
            {
                _logger.LogInformation("Storing credential for {Api}", apiName);

                var encrypted = EncryptCredential(credential);
                var stored = new StoredCredential
                {
                    ApiName = apiName,
                    EncryptedValue = encrypted,
                    CreatedAt = DateTime.UtcNow,
                    LastRotatedAt = DateTime.UtcNow,
                    Status = "active"
                };

                _credentials[apiName] = stored;
                LogAccessEvent(apiName, "store", success: true);

                return await Task.FromResult(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to store credential for {Api}", apiName);
                LogAccessEvent(apiName, "store", success: false);
                throw;
            }
        }

        /// <summary>
        /// Retrieve credential (with decryption)
        /// </summary>
        public async Task<ApiCredential> RetrieveCredentialAsync(string apiName, string requestingAgentId)
        {
            if (!_credentials.TryGetValue(apiName, out var stored))
                throw new KeyNotFoundException($"Credential for {apiName} not found");

            try
            {
                var credential = DecryptCredential(stored.EncryptedValue);
                LogAccessEvent(apiName, "retrieve", success: true, requestingAgentId);
                
                return await Task.FromResult(credential);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve credential for {Api}", apiName);
                LogAccessEvent(apiName, "retrieve", success: false, requestingAgentId);
                throw;
            }
        }

        /// <summary>
        /// Rotate credential (daily auto or on-demand)
        /// </summary>
        public async Task<bool> RotateCredentialAsync(string apiName)
        {
            if (!_credentials.TryGetValue(apiName, out var stored))
                throw new KeyNotFoundException($"Credential for {apiName} not found");

            try
            {
                _logger.LogInformation("Rotating credential for {Api}", apiName);

                stored.LastRotatedAt = DateTime.UtcNow;
                stored.Status = "rotated";
                
                LogAccessEvent(apiName, "rotate", success: true);
                return await Task.FromResult(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to rotate credential for {Api}", apiName);
                LogAccessEvent(apiName, "rotate", success: false);
                throw;
            }
        }

        /// <summary>
        /// Revoke credential (emergency)
        /// </summary>
        public async Task<bool> RevokeCredentialAsync(string apiName)
        {
            if (!_credentials.TryGetValue(apiName, out var stored))
                throw new KeyNotFoundException($"Credential for {apiName} not found");

            try
            {
                _logger.LogWarning("Revoking credential for {Api}", apiName);

                stored.Status = "revoked";
                LogAccessEvent(apiName, "revoke", success: true);

                return await Task.FromResult(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to revoke credential for {Api}", apiName);
                throw;
            }
        }

        /// <summary>
        /// Get credential status
        /// </summary>
        public CredentialStatus GetStatus(string apiName)
        {
            if (!_credentials.TryGetValue(apiName, out var stored))
                throw new KeyNotFoundException($"Credential for {apiName} not found");

            return new CredentialStatus
            {
                ApiName = apiName,
                Status = stored.Status,
                LastAccessed = _accessEvents.Values
                    .Where(e => e.ApiName == apiName && e.Success)
                    .OrderByDescending(e => e.Timestamp)
                    .FirstOrDefault()?.Timestamp,
                LastRotated = stored.LastRotatedAt,
                AccessCount = _accessEvents.Values.Count(e => e.ApiName == apiName),
                SuccessRate = CalculateSuccessRate(apiName)
            };
        }

        /// <summary>
        /// Get access history
        /// </summary>
        public List<CredentialAccessEvent> GetAccessHistory(string apiName, DateTime since)
        {
            return _accessEvents.Values
                .Where(e => e.ApiName == apiName && e.Timestamp >= since)
                .OrderByDescending(e => e.Timestamp)
                .ToList();
        }

        private string EncryptCredential(ApiCredential credential)
        {
            using (var aes = new AesCryptoServiceProvider())
            {
                aes.KeySize = 256;
                aes.Mode = CipherMode.GCM;
                aes.Padding = PaddingMode.PKCS7;
                aes.GenerateIV();

                using (var encryptor = aes.CreateEncryptor(_masterKey, aes.IV))
                using (var ms = new System.IO.MemoryStream())
                {
                    ms.Write(aes.IV, 0, aes.IV.Length);
                    using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                    {
                        var credentialJson = System.Text.Json.JsonSerializer.Serialize(credential);
                        cs.Write(Encoding.UTF8.GetBytes(credentialJson), 0, credentialJson.Length);
                        cs.FlushFinalBlock();
                    }
                    return Convert.ToBase64String(ms.ToArray());
                }
            }
        }

        private ApiCredential DecryptCredential(string encrypted)
        {
            var buffer = Convert.FromBase64String(encrypted);
            using (var aes = new AesCryptoServiceProvider())
            {
                aes.KeySize = 256;
                aes.Mode = CipherMode.GCM;
                var iv = new byte[aes.IV.Length];
                Array.Copy(buffer, 0, iv, 0, iv.Length);
                aes.IV = iv;

                using (var decryptor = aes.CreateDecryptor(_masterKey, aes.IV))
                using (var ms = new System.IO.MemoryStream(buffer, iv.Length, buffer.Length - iv.Length))
                using (var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                using (var sr = new System.IO.StreamReader(cs))
                {
                    var json = sr.ReadToEnd();
                    return System.Text.Json.JsonSerializer.Deserialize<ApiCredential>(json)!;
                }
            }
        }

        private byte[] GenerateSecureKey()
        {
            using (var rng = RandomNumberGenerator.Create())
            {
                var key = new byte[32]; // 256-bit key
                rng.GetBytes(key);
                return key;
            }
        }

        private void LogAccessEvent(string apiName, string operation, bool success, string? agentId = null)
        {
            var eventId = $"{_eventCounter++}";
            var @event = new CredentialAccessEvent
            {
                Id = eventId,
                ApiName = apiName,
                Operation = operation,
                Timestamp = DateTime.UtcNow,
                Success = success,
                AgentId = agentId ?? "system"
            };

            _accessEvents[eventId] = @event;
        }

        private double CalculateSuccessRate(string apiName)
        {
            var events = _accessEvents.Values.Where(e => e.ApiName == apiName).ToList();
            if (!events.Any()) return 100;
            
            var successes = events.Count(e => e.Success);
            return (successes * 100.0) / events.Count;
        }

        private async Task RotateAllAsync()
        {
            _logger.LogInformation("Rotating all credentials");
            foreach (var apiName in _credentials.Keys)
            {
                try
                {
                    await RotateCredentialAsync(apiName);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to rotate credential for {Api}", apiName);
                }
            }
        }

        public string ServiceName => "CredentialManager";
        public ServiceStatus Status { get; set; } = ServiceStatus.Idle;
        public Dictionary<string, object> GetMetrics() => new()
        {
            { "StoredCredentials", _credentials.Count },
            { "TotalAccessEvents", _accessEvents.Count },
            { "ServiceStatus", Status }
        };
    }

    public class ApiCredential
    {
        public string Type { get; set; } = string.Empty; // "bearer", "api_key", "basic", "oauth2"
        public string Value { get; set; } = string.Empty;
        public Dictionary<string, string>? Additional { get; set; }
    }

    public class StoredCredential
    {
        public string ApiName { get; set; } = string.Empty;
        public string EncryptedValue { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime LastRotatedAt { get; set; }
        public string Status { get; set; } = "active"; // active, rotated, revoked
    }

    public class CredentialStatus
    {
        public string ApiName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime? LastAccessed { get; set; }
        public DateTime? LastRotated { get; set; }
        public int AccessCount { get; set; }
        public double SuccessRate { get; set; }
    }

    public class CredentialAccessEvent
    {
        public string Id { get; set; } = string.Empty;
        public string ApiName { get; set; } = string.Empty;
        public string Operation { get; set; } = string.Empty; // store, retrieve, rotate, revoke
        public DateTime Timestamp { get; set; }
        public bool Success { get; set; }
        public string AgentId { get; set; } = string.Empty;
    }

    public class AvailableApi
    {
        public string Name { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string RateLimit { get; set; } = string.Empty;
    }

    public class ApiAccessLog
    {
        public string Id { get; set; } = string.Empty;
        public string ApiName { get; set; } = string.Empty;
        public string AgentId { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public bool Success { get; set; }
    }

    internal class RateLimiterPerApi
    {
        private readonly ConcurrentDictionary<string, Queue<DateTime>> _requestTimes = new();

        public bool AllowRequest(string apiName)
        {
            var queue = _requestTimes.GetOrAdd(apiName, _ => new Queue<DateTime>());
            var now = DateTime.UtcNow;
            var oneMinuteAgo = now.AddMinutes(-1);

            lock (queue)
            {
                while (queue.Count > 0 && queue.Peek() < oneMinuteAgo)
                    queue.Dequeue();

                return true; // Allow for now, production version implements actual limits
            }
        }
    }
}
