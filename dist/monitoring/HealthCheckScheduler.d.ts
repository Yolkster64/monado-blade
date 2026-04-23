/**
 * Health Check Scheduler
 * Manages periodic health checks for all providers with adaptive intervals
 * Tracks health history and detects degradation patterns
 */
import { HealthCheckConfig, HealthCheckResult, ProviderAdapter } from './types';
import { CircuitBreaker } from './CircuitBreaker';
export declare class HealthCheckScheduler {
    private readonly config;
    private readonly circuitBreakers;
    private readonly healthHistory;
    private readonly activeTimers;
    private readonly lastCheckTime;
    private readonly nextCheckTime;
    private readonly checkIntervals;
    private isRunning;
    private listeners;
    constructor(config?: Partial<HealthCheckConfig>);
    /**
     * Register a provider for health checks
     */
    registerProvider(provider: ProviderAdapter, circuitBreaker: CircuitBreaker): void;
    /**
     * Unregister a provider from health checks
     */
    unregisterProvider(providerId: string): void;
    /**
     * Start health check scheduler
     */
    start(providers: ProviderAdapter[]): Promise<void>;
    /**
     * Stop health check scheduler
     */
    stop(): void;
    /**
     * Schedule the next health check for a provider
     */
    private scheduleNextCheck;
    /**
     * Execute a single health check for a provider
     */
    private executeHealthCheck;
    /**
     * Process a health check result
     */
    private processHealthCheckResult;
    /**
     * Detect degradation patterns in recent check history
     */
    private detectDegradation;
    /**
     * Update check interval based on provider health
     */
    private updateCheckInterval;
    /**
     * Stop health check for a specific provider
     */
    private stopCheckForProvider;
    /**
     * Get health history for a provider
     */
    getHealthHistory(providerId: string): HealthCheckResult[];
    /**
     * Get current check interval for a provider
     */
    getCheckInterval(providerId: string): number;
    /**
     * Subscribe to health check events
     */
    on(event: 'healthCheckResult' | 'degradationDetected', listener: Function): void;
    /**
     * Get scheduler statistics
     */
    getStatistics(): {
        isRunning: boolean;
        registeredProviders: number;
        averageCheckInterval: number;
        lastCheckTimes: Map<string, number>;
    };
}
//# sourceMappingURL=HealthCheckScheduler.d.ts.map