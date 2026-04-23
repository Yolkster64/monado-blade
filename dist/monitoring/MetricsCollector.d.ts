/**
 * Metrics Collector
 * Collects and aggregates health metrics from providers
 * Generates dashboards and exports metrics in Prometheus format
 */
import { MetricsSnapshot, MetricsExport, MetricsSummary, ProviderHealth, HealthCheckResult, DegradationMode, CircuitBreakerState } from './types';
export declare class MetricsCollector {
    private providerMetrics;
    private metricsHistory;
    private readonly maxHistorySize;
    private systemDegradationMode;
    private activatedFailovers;
    private recoveredProviders;
    constructor(maxHistorySize?: number);
    /**
     * Initialize metrics for a provider
     */
    initializeProvider(providerId: string): void;
    /**
     * Record a health check result
     */
    recordHealthCheckResult(result: HealthCheckResult): void;
    /**
     * Update circuit breaker state for a provider
     */
    updateCircuitBreakerState(providerId: string, state: CircuitBreakerState): void;
    /**
     * Update degradation mode for a provider
     */
    updateDegradationMode(providerId: string, mode: DegradationMode): void;
    /**
     * Update system degradation mode
     */
    setSystemDegradationMode(mode: DegradationMode): void;
    /**
     * Get current metrics snapshot
     */
    getCurrentSnapshot(): MetricsSnapshot;
    /**
     * Record a metrics snapshot
     */
    recordSnapshot(): void;
    /**
     * Get metrics history
     */
    getHistory(maxAge?: number): MetricsSnapshot[];
    /**
     * Get metrics export in Prometheus format
     */
    exportMetrics(): MetricsExport;
    /**
     * Export metrics as Prometheus text format
     */
    exportPrometheusMetrics(): string;
    /**
     * Calculate summary metrics
     */
    calculateSummary(): MetricsSummary;
    /**
     * Get metrics for a specific provider
     */
    getProviderMetrics(providerId: string): ProviderHealth | null;
    /**
     * Get all provider metrics
     */
    getAllMetrics(): ProviderHealth[];
    /**
     * Get dashboard data
     */
    getDashboardData(): {
        snapshot: MetricsSnapshot;
        summary: MetricsSummary;
        healthTrends: Map<string, number[]>;
        latencyTrends: Map<string, number[]>;
    };
    /**
     * Reset all metrics
     */
    reset(): void;
    /**
     * Get statistics
     */
    getStatistics(): {
        totalProviders: number;
        metricsAge: number;
        historySize: number;
    };
}
//# sourceMappingURL=MetricsCollector.d.ts.map