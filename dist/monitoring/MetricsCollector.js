"use strict";
/**
 * Metrics Collector
 * Collects and aggregates health metrics from providers
 * Generates dashboards and exports metrics in Prometheus format
 */
Object.defineProperty(exports, "__esModule", { value: true });
exports.MetricsCollector = void 0;
const types_1 = require("./types");
class MetricsCollector {
    constructor(maxHistorySize = 1440) {
        this.providerMetrics = new Map();
        this.metricsHistory = [];
        this.systemDegradationMode = types_1.DegradationMode.NORMAL;
        this.activatedFailovers = 0;
        this.recoveredProviders = 0;
        this.maxHistorySize = maxHistorySize;
    }
    /**
     * Initialize metrics for a provider
     */
    initializeProvider(providerId) {
        if (!this.providerMetrics.has(providerId)) {
            this.providerMetrics.set(providerId, {
                providerId,
                currentStatus: types_1.HealthStatus.HEALTHY,
                circuitBreakerState: types_1.CircuitBreakerState.CLOSED,
                lastCheckTime: 0,
                consecutiveFailures: 0,
                consecutiveSuccesses: 0,
                latency: {
                    min: Number.MAX_VALUE,
                    max: 0,
                    avg: 0
                },
                errorRate: 0,
                availability: 100,
                isActive: true,
                degradationMode: types_1.DegradationMode.NORMAL
            });
        }
    }
    /**
     * Record a health check result
     */
    recordHealthCheckResult(result) {
        this.initializeProvider(result.providerId);
        const metrics = this.providerMetrics.get(result.providerId);
        metrics.lastCheckTime = result.timestamp;
        metrics.currentStatus = result.status;
        metrics.errorRate = result.errorRate;
        // Update latency metrics
        if (result.latency > 0) {
            metrics.latency.min = Math.min(metrics.latency.min, result.latency);
            metrics.latency.max = Math.max(metrics.latency.max, result.latency);
            metrics.latency.avg = (metrics.latency.avg + result.latency) / 2;
        }
        // Update availability
        if (result.successRate > 0) {
            metrics.availability = result.successRate * 100;
        }
        // Update consecutive success/failure counts
        if (result.status === types_1.HealthStatus.HEALTHY) {
            metrics.consecutiveSuccesses++;
            metrics.consecutiveFailures = 0;
        }
        else {
            metrics.consecutiveFailures++;
            metrics.consecutiveSuccesses = 0;
        }
    }
    /**
     * Update circuit breaker state for a provider
     */
    updateCircuitBreakerState(providerId, state) {
        this.initializeProvider(providerId);
        const metrics = this.providerMetrics.get(providerId);
        metrics.circuitBreakerState = state;
        if (state === types_1.CircuitBreakerState.OPEN) {
            this.activatedFailovers++;
        }
        else if (state === types_1.CircuitBreakerState.CLOSED) {
            this.recoveredProviders++;
        }
    }
    /**
     * Update degradation mode for a provider
     */
    updateDegradationMode(providerId, mode) {
        this.initializeProvider(providerId);
        const metrics = this.providerMetrics.get(providerId);
        metrics.degradationMode = mode;
    }
    /**
     * Update system degradation mode
     */
    setSystemDegradationMode(mode) {
        this.systemDegradationMode = mode;
    }
    /**
     * Get current metrics snapshot
     */
    getCurrentSnapshot() {
        return {
            timestamp: Date.now(),
            providers: new Map(this.providerMetrics),
            systemDegradationMode: this.systemDegradationMode,
            activatedFailovers: this.activatedFailovers,
            recoveredProviders: this.recoveredProviders
        };
    }
    /**
     * Record a metrics snapshot
     */
    recordSnapshot() {
        const snapshot = this.getCurrentSnapshot();
        this.metricsHistory.push(snapshot);
        if (this.metricsHistory.length > this.maxHistorySize) {
            this.metricsHistory.shift();
        }
    }
    /**
     * Get metrics history
     */
    getHistory(maxAge = 3600000) {
        const cutoffTime = Date.now() - maxAge;
        return this.metricsHistory.filter(snapshot => snapshot.timestamp >= cutoffTime);
    }
    /**
     * Get metrics export in Prometheus format
     */
    exportMetrics() {
        const providers = [];
        for (const [, health] of this.providerMetrics) {
            providers.push({
                providerId: health.providerId,
                status: health.currentStatus,
                circuitBreakerState: health.circuitBreakerState,
                latencyMs: health.latency.avg,
                errorRate: health.errorRate,
                availability: health.availability,
                consecutiveFailures: health.consecutiveFailures,
                consecutiveSuccesses: health.consecutiveSuccesses
            });
        }
        const summary = this.calculateSummary();
        return {
            timestamp: Date.now(),
            providers,
            summary
        };
    }
    /**
     * Export metrics as Prometheus text format
     */
    exportPrometheusMetrics() {
        let output = '# HELP monado_blade_provider_health Provider health status\n';
        output += '# TYPE monado_blade_provider_health gauge\n';
        for (const [providerId, health] of this.providerMetrics) {
            const statusValue = health.currentStatus === types_1.HealthStatus.HEALTHY ? 1 : 0;
            output += `monado_blade_provider_health{provider="${providerId}"} ${statusValue}\n`;
            output += `monado_blade_provider_latency_ms{provider="${providerId}"} ${health.latency.avg}\n`;
            output += `monado_blade_provider_error_rate{provider="${providerId}"} ${health.errorRate}\n`;
            output += `monado_blade_provider_availability{provider="${providerId}"} ${health.availability}\n`;
            output += `monado_blade_provider_consecutive_failures{provider="${providerId}"} ${health.consecutiveFailures}\n`;
            output += `monado_blade_provider_consecutive_successes{provider="${providerId}"} ${health.consecutiveSuccesses}\n`;
            const cbStateValue = health.circuitBreakerState === types_1.CircuitBreakerState.CLOSED ? 0 :
                health.circuitBreakerState === types_1.CircuitBreakerState.HALF_OPEN ? 1 : 2;
            output += `monado_blade_circuit_breaker_state{provider="${providerId}"} ${cbStateValue}\n`;
        }
        output += '\n# HELP monado_blade_system_degradation_mode System degradation mode\n';
        output += '# TYPE monado_blade_system_degradation_mode gauge\n';
        const modeValue = this.systemDegradationMode === types_1.DegradationMode.NORMAL ? 0 :
            this.systemDegradationMode === types_1.DegradationMode.REDUCED ? 1 : 2;
        output += `monado_blade_system_degradation_mode ${modeValue}\n`;
        output += '\n# HELP monado_blade_activated_failovers Total activated failovers\n';
        output += '# TYPE monado_blade_activated_failovers counter\n';
        output += `monado_blade_activated_failovers ${this.activatedFailovers}\n`;
        output += '\n# HELP monado_blade_recovered_providers Total recovered providers\n';
        output += '# TYPE monado_blade_recovered_providers counter\n';
        output += `monado_blade_recovered_providers ${this.recoveredProviders}\n`;
        return output;
    }
    /**
     * Calculate summary metrics
     */
    calculateSummary() {
        let healthyCount = 0;
        let degradedCount = 0;
        let unhealthyCount = 0;
        let totalLatency = 0;
        let totalErrorRate = 0;
        let totalAvailability = 0;
        for (const health of this.providerMetrics.values()) {
            if (health.currentStatus === types_1.HealthStatus.HEALTHY) {
                healthyCount++;
            }
            else if (health.currentStatus === types_1.HealthStatus.DEGRADED) {
                degradedCount++;
            }
            else {
                unhealthyCount++;
            }
            totalLatency += health.latency.avg;
            totalErrorRate += health.errorRate;
            totalAvailability += health.availability;
        }
        const providerCount = this.providerMetrics.size;
        return {
            healthyProviders: healthyCount,
            degradedProviders: degradedCount,
            unhealthyProviders: unhealthyCount,
            systemAvailability: providerCount > 0 ? totalAvailability / providerCount : 0,
            averageLatency: providerCount > 0 ? totalLatency / providerCount : 0,
            averageErrorRate: providerCount > 0 ? totalErrorRate / providerCount : 0
        };
    }
    /**
     * Get metrics for a specific provider
     */
    getProviderMetrics(providerId) {
        return this.providerMetrics.get(providerId) ?? null;
    }
    /**
     * Get all provider metrics
     */
    getAllMetrics() {
        return Array.from(this.providerMetrics.values());
    }
    /**
     * Get dashboard data
     */
    getDashboardData() {
        const snapshot = this.getCurrentSnapshot();
        const summary = this.calculateSummary();
        // Calculate trends from history
        const healthTrends = new Map();
        const latencyTrends = new Map();
        const recentHistory = this.metricsHistory.slice(-60);
        for (const snap of recentHistory) {
            for (const [providerId, health] of snap.providers) {
                if (!healthTrends.has(providerId)) {
                    healthTrends.set(providerId, []);
                    latencyTrends.set(providerId, []);
                }
                const healthValue = health.currentStatus === types_1.HealthStatus.HEALTHY ? 1 : 0;
                healthTrends.get(providerId).push(healthValue);
                latencyTrends.get(providerId).push(health.latency.avg);
            }
        }
        return {
            snapshot,
            summary,
            healthTrends,
            latencyTrends
        };
    }
    /**
     * Reset all metrics
     */
    reset() {
        this.providerMetrics.clear();
        this.metricsHistory = [];
        this.systemDegradationMode = types_1.DegradationMode.NORMAL;
        this.activatedFailovers = 0;
        this.recoveredProviders = 0;
    }
    /**
     * Get statistics
     */
    getStatistics() {
        const oldestSnapshot = this.metricsHistory[0];
        const metricsAge = oldestSnapshot
            ? Date.now() - oldestSnapshot.timestamp
            : 0;
        return {
            totalProviders: this.providerMetrics.size,
            metricsAge,
            historySize: this.metricsHistory.length
        };
    }
}
exports.MetricsCollector = MetricsCollector;
//# sourceMappingURL=MetricsCollector.js.map