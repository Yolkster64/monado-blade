using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace MonadoBlade.Security
{
    /// <summary>
    /// Measures and monitors the Trusted Computing Base (TCB) at each boot.
    /// Detects unauthorized modifications and alerts on any divergence from baseline.
    /// </summary>
    public class TrustedComputingBase
    {
        private readonly ILogger<TrustedComputingBase> _logger;
        private Dictionary<string, TCBMeasurement> _tcbMeasurements;
        private Dictionary<string, TCBMeasurement> _baselineMeasurements;
        private object _tcbLock = new object();
        private byte[] _cumulativeTCBHash;
        private bool _baselineEstablished;
        private List<TCBAnomalyEvent> _anomalyEvents;

        private class TCBMeasurement
        {
            public string ComponentName { get; set; }
            public byte[] ComponentHash { get; set; }
            public long ComponentSize { get; set; }
            public DateTime MeasurementTime { get; set; }
            public string ComponentType { get; set; }
            public bool HasDiverged { get; set; }
            public string DivergenceReason { get; set; }
        }

        private class TCBAnomalyEvent
        {
            public string AnomalyId { get; set; }
            public string ComponentName { get; set; }
            public DateTime DetectionTime { get; set; }
            public string AnomalyType { get; set; }
            public string Description { get; set; }
            public int Severity { get; set; }
            public bool HasBeenAcknowledged { get; set; }
        }

        public enum AnomalyType
        {
            UnauthorizedModification,
            UnexpectedRemoval,
            UnexpectedAddition,
            SizeChange,
            TimingAnomalyIndicator
        }

        public TrustedComputingBase(ILogger<TrustedComputingBase> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _tcbMeasurements = new Dictionary<string, TCBMeasurement>();
            _baselineMeasurements = new Dictionary<string, TCBMeasurement>();
            _anomalyEvents = new List<TCBAnomalyEvent>();
            _baselineEstablished = false;
        }

        /// <summary>
        /// Establishes the TCB baseline by measuring all components.
        /// </summary>
        public bool EstablishBaseline(Dictionary<string, (byte[] data, string type)> components)
        {
            if (components == null || components.Count == 0)
            {
                _logger.LogError("Components dictionary cannot be null or empty");
                return false;
            }

            lock (_tcbLock)
            {
                try
                {
                    _logger.LogInformation($"Establishing TCB baseline with {components.Count} components");

                    foreach (var kvp in components)
                    {
                        string componentName = kvp.Key;
                        var (componentData, componentType) = kvp.Value;

                        byte[] componentHash = ComputeComponentHash(componentData);

                        var measurement = new TCBMeasurement
                        {
                            ComponentName = componentName,
                            ComponentHash = componentHash,
                            ComponentSize = componentData.Length,
                            MeasurementTime = DateTime.UtcNow,
                            ComponentType = componentType,
                            HasDiverged = false
                        };

                        _baselineMeasurements[componentName] = measurement;
                        _tcbMeasurements[componentName] = new TCBMeasurement
                        {
                            ComponentName = measurement.ComponentName,
                            ComponentHash = (byte[])measurement.ComponentHash.Clone(),
                            ComponentSize = measurement.ComponentSize,
                            MeasurementTime = measurement.MeasurementTime,
                            ComponentType = measurement.ComponentType,
                            HasDiverged = measurement.HasDiverged,
                            DivergenceReason = measurement.DivergenceReason
                        };
                    }

                    // Calculate cumulative TCB hash
                    _cumulativeTCBHash = CalculateCumulativeTCBHash();
                    _baselineEstablished = true;

                    _logger.LogInformation($"TCB baseline established successfully with hash: {Convert.ToHexString(_cumulativeTCBHash)}");
                    return true;
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error establishing TCB baseline: {ex.Message}");
                    return false;
                }
            }
        }

        /// <summary>
        /// Measures the current TCB state.
        /// </summary>
        public bool MeasureTCB(Dictionary<string, (byte[] data, string type)> currentComponents)
        {
            if (currentComponents == null || currentComponents.Count == 0)
            {
                _logger.LogError("Components dictionary cannot be null or empty");
                return false;
            }

            if (!_baselineEstablished)
            {
                _logger.LogError("TCB baseline not established");
                return false;
            }

            lock (_tcbLock)
            {
                try
                {
                    _logger.LogInformation("Measuring current TCB state");

                    bool measurementValid = true;

                    // Check for removed components
                    foreach (var baselineComponent in _baselineMeasurements.Keys)
                    {
                        if (!currentComponents.ContainsKey(baselineComponent))
                        {
                            _logger.LogError($"TCB component removed: {baselineComponent}");
                            RecordAnomaly(
                                baselineComponent,
                                AnomalyType.UnexpectedRemoval,
                                $"Component '{baselineComponent}' is missing from TCB",
                                AnomalySeverity.Critical
                            );
                            measurementValid = false;
                        }
                    }

                    // Check for added components
                    foreach (var currentComponent in currentComponents.Keys)
                    {
                        if (!_baselineMeasurements.ContainsKey(currentComponent))
                        {
                            _logger.LogWarning($"Unexpected TCB component added: {currentComponent}");
                            RecordAnomaly(
                                currentComponent,
                                AnomalyType.UnexpectedAddition,
                                $"New component '{currentComponent}' detected in TCB",
                                AnomalySeverity.High
                            );
                        }
                    }

                    // Check for modified components
                    foreach (var kvp in currentComponents)
                    {
                        string componentName = kvp.Key;
                        var (componentData, componentType) = kvp.Value;

                        byte[] currentHash = ComputeComponentHash(componentData);

                        var measurement = new TCBMeasurement
                        {
                            ComponentName = componentName,
                            ComponentHash = currentHash,
                            ComponentSize = componentData.Length,
                            MeasurementTime = DateTime.UtcNow,
                            ComponentType = componentType,
                            HasDiverged = false
                        };

                        if (_baselineMeasurements.ContainsKey(componentName))
                        {
                            var baselineMeasurement = _baselineMeasurements[componentName];

                            if (!ConstantTimeComparison(currentHash, baselineMeasurement.ComponentHash))
                            {
                                _logger.LogError($"TCB component modified: {componentName}");
                                measurement.HasDiverged = true;
                                measurement.DivergenceReason = "Hash mismatch";

                                RecordAnomaly(
                                    componentName,
                                    AnomalyType.UnauthorizedModification,
                                    $"Component '{componentName}' hash diverged from baseline",
                                    AnomalySeverity.Critical
                                );
                                measurementValid = false;
                            }

                            if (currentHash.Length != baselineMeasurement.ComponentSize)
                            {
                                _logger.LogWarning($"TCB component size changed: {componentName}");
                                measurement.HasDiverged = true;
                                measurement.DivergenceReason = "Size changed from " +
                                    $"{baselineMeasurement.ComponentSize} to {currentHash.Length}";

                                RecordAnomaly(
                                    componentName,
                                    AnomalyType.SizeChange,
                                    $"Component '{componentName}' size changed",
                                    AnomalySeverity.High
                                );
                            }
                        }

                        _tcbMeasurements[componentName] = measurement;
                    }

                    // Update cumulative hash
                    byte[] newCumulativeHash = CalculateCumulativeTCBHash();

                    if (!ConstantTimeComparison(newCumulativeHash, _cumulativeTCBHash))
                    {
                        _logger.LogError("Cumulative TCB hash diverged from baseline");
                        measurementValid = false;
                    }

                    _cumulativeTCBHash = newCumulativeHash;

                    _logger.LogInformation($"TCB measurement completed. Valid: {measurementValid}. Current cumulative hash: {Convert.ToHexString(_cumulativeTCBHash)}");

                    return measurementValid;
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error measuring TCB: {ex.Message}");
                    return false;
                }
            }
        }

        /// <summary>
        /// Gets the current TCB state.
        /// </summary>
        public Dictionary<string, object> GetTCBState()
        {
            lock (_tcbLock)
            {
                var measurements = new List<Dictionary<string, object>>();

                foreach (var measurement in _tcbMeasurements.Values)
                {
                    measurements.Add(new Dictionary<string, object>
                    {
                        { "ComponentName", measurement.ComponentName },
                        { "ComponentHashHex", Convert.ToHexString(measurement.ComponentHash) },
                        { "ComponentSize", measurement.ComponentSize },
                        { "MeasurementTime", measurement.MeasurementTime },
                        { "ComponentType", measurement.ComponentType },
                        { "HasDiverged", measurement.HasDiverged },
                        { "DivergenceReason", measurement.DivergenceReason ?? "N/A" }
                    });
                }

                return new Dictionary<string, object>
                {
                    { "CumulativeTCBHashHex", Convert.ToHexString(_cumulativeTCBHash ?? new byte[0]) },
                    { "BaselineEstablished", _baselineEstablished },
                    { "MeasurementCount", _tcbMeasurements.Count },
                    { "AnomaliesDetected", _anomalyEvents.Count },
                    { "Measurements", measurements }
                };
            }
        }

        /// <summary>
        /// Gets detected anomalies.
        /// </summary>
        public List<Dictionary<string, object>> GetDetectedAnomalies()
        {
            lock (_tcbLock)
            {
                var result = new List<Dictionary<string, object>>();

                foreach (var anomaly in _anomalyEvents)
                {
                    result.Add(new Dictionary<string, object>
                    {
                        { "AnomalyId", anomaly.AnomalyId },
                        { "ComponentName", anomaly.ComponentName },
                        { "DetectionTime", anomaly.DetectionTime },
                        { "AnomalyType", anomaly.AnomalyType },
                        { "Description", anomaly.Description },
                        { "Severity", anomaly.Severity },
                        { "HasBeenAcknowledged", anomaly.HasBeenAcknowledged }
                    });
                }

                return result;
            }
        }

        /// <summary>
        /// Gets critical anomalies that require immediate attention.
        /// </summary>
        public List<Dictionary<string, object>> GetCriticalAnomalies()
        {
            lock (_tcbLock)
            {
                var result = new List<Dictionary<string, object>>();

                foreach (var anomaly in _anomalyEvents.Where(a => a.Severity >= (int)AnomalySeverity.Critical))
                {
                    result.Add(new Dictionary<string, object>
                    {
                        { "AnomalyId", anomaly.AnomalyId },
                        { "ComponentName", anomaly.ComponentName },
                        { "DetectionTime", anomaly.DetectionTime },
                        { "AnomalyType", anomaly.AnomalyType },
                        { "Description", anomaly.Description },
                        { "Severity", anomaly.Severity },
                        { "RequiresImmediateAction", !anomaly.HasBeenAcknowledged }
                    });
                }

                return result;
            }
        }

        /// <summary>
        /// Acknowledges an anomaly.
        /// </summary>
        public bool AcknowledgeAnomaly(string anomalyId)
        {
            lock (_tcbLock)
            {
                var anomaly = _anomalyEvents.FirstOrDefault(a => a.AnomalyId == anomalyId);

                if (anomaly == null)
                {
                    _logger.LogWarning($"Anomaly '{anomalyId}' not found");
                    return false;
                }

                anomaly.HasBeenAcknowledged = true;
                _logger.LogInformation($"Anomaly '{anomalyId}' acknowledged");

                return true;
            }
        }

        /// <summary>
        /// Gets TCB statistics.
        /// </summary>
        public Dictionary<string, object> GetTCBStatistics()
        {
            lock (_tcbLock)
            {
                var criticalAnomalies = _anomalyEvents.Count(a => a.Severity >= (int)AnomalySeverity.Critical);
                var unacknowledgedAnomalies = _anomalyEvents.Count(a => !a.HasBeenAcknowledged);

                return new Dictionary<string, object>
                {
                    { "TotalComponents", _tcbMeasurements.Count },
                    { "ComponentsInBaseline", _baselineMeasurements.Count },
                    { "TotalAnomaliesDetected", _anomalyEvents.Count },
                    { "CriticalAnomalies", criticalAnomalies },
                    { "UnacknowledgedAnomalies", unacknowledgedAnomalies },
                    { "TCBHealthStatus", criticalAnomalies > 0 ? "COMPROMISED" : "HEALTHY" },
                    { "BaselineEstablished", _baselineEstablished }
                };
            }
        }

        // Private helper methods

        private byte[] ComputeComponentHash(byte[] componentData)
        {
            using (var sha256 = SHA256.Create())
            {
                return sha256.ComputeHash(componentData);
            }
        }

        private byte[] CalculateCumulativeTCBHash()
        {
            try
            {
                using (var sha256 = SHA256.Create())
                {
                    using (var ms = new System.IO.MemoryStream())
                    {
                        foreach (var measurement in _tcbMeasurements.Values.OrderBy(m => m.ComponentName))
                        {
                            ms.Write(measurement.ComponentHash, 0, measurement.ComponentHash.Length);
                        }

                        ms.Seek(0, System.IO.SeekOrigin.Begin);
                        return sha256.ComputeHash(ms);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error calculating cumulative TCB hash: {ex.Message}");
                return new byte[32]; // Return zero hash on error
            }
        }

        private void RecordAnomaly(
            string componentName,
            AnomalyType anomalyType,
            string description,
            AnomalySeverity severity)
        {
            var anomaly = new TCBAnomalyEvent
            {
                AnomalyId = Guid.NewGuid().ToString(),
                ComponentName = componentName,
                DetectionTime = DateTime.UtcNow,
                AnomalyType = anomalyType.ToString(),
                Description = description,
                Severity = (int)severity,
                HasBeenAcknowledged = false
            };

            _anomalyEvents.Add(anomaly);

            if ((int)severity >= (int)AnomalySeverity.Critical)
            {
                _logger.LogError($"CRITICAL TCB ANOMALY: {anomaly.AnomalyId} - {description}");
            }
            else if ((int)severity >= (int)AnomalySeverity.High)
            {
                _logger.LogWarning($"HIGH TCB ANOMALY: {anomaly.AnomalyId} - {description}");
            }
            else
            {
                _logger.LogWarning($"TCB ANOMALY: {anomaly.AnomalyId} - {description}");
            }

            // Keep anomaly log manageable (last 10000 entries)
            if (_anomalyEvents.Count > 10000)
            {
                _anomalyEvents.RemoveRange(0, _anomalyEvents.Count - 10000);
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

        private enum AnomalySeverity
        {
            Low = 1,
            Medium = 2,
            High = 3,
            Critical = 4
        }
    }
}
