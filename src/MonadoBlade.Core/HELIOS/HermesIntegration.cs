using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MonadoBlade.Core.HELIOS
{
    /// <summary>
    /// Deprecated: Use HELIOS.Platform.Integration.HermesIntegration instead.
    /// This class is retained for backward compatibility only.
    /// </summary>
    [Obsolete("Use HELIOS.Platform.Integration.HermesIntegration instead.")]

    /// <summary>Hermes AI learning and pattern optimization integration for HELIOS.</summary>
    // Deprecated: Use HELIOS.Platform.Integration.HermesIntegration instead.
    public class HermesIntegration
    {
        private readonly string _agentId;
        private readonly Dictionary<string, object> _patternCache = new();
        private readonly object _cacheLock = new();

        // This constructor is deprecated and should not be used.
        public HermesIntegration(string agentId = "MonadoBlade-Agent-1")
        {
            throw new NotSupportedException("Use HELIOS.Platform.Integration.HermesIntegration instead.");
        }

        // Deprecated method
        public async Task InitializeAsync(CancellationToken ct = default)
        {
            throw new NotSupportedException("Use HELIOS.Platform.Integration.HermesIntegration instead.");
        }

        // Deprecated method
        public async Task<object> QueryPatternAsync(string category, CancellationToken ct = default)
        {
            throw new NotSupportedException("Use HELIOS.Platform.Integration.HermesIntegration instead.");
        }

        // Deprecated method
        public async Task<bool> StorePatternAsync(object pattern, CancellationToken ct = default)
        {
            throw new NotSupportedException("Use HELIOS.Platform.Integration.HermesIntegration instead.");
        }

        // Deprecated method
        public async Task ReportMetricsAsync(object metrics, CancellationToken ct = default)
        {
            throw new NotSupportedException("Use HELIOS.Platform.Integration.HermesIntegration instead.");
        }

        // Deprecated method
        public async Task<object> GetLearningFeedbackAsync(string patternId, CancellationToken ct = default)
        {
            throw new NotSupportedException("Use HELIOS.Platform.Integration.HermesIntegration instead.");
        }
    }
}
