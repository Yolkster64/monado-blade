/**
 * Failover Controller
 * Automatically detects provider failures and triggers failover
 * Implements graceful degradation and rate limiting
 */
import { FailoverConfig, FailoverEvent, ProviderAdapter, ProviderHealth } from './types';
import { CircuitBreaker } from './CircuitBreaker';
export declare class FailoverController {
    private readonly config;
    private readonly circuitBreakers;
    private readonly failoverTimestamps;
    private activeFailovers;
    private lastCascadePreventionTime;
    private failoverEvents;
    private listeners;
    private providers;
    constructor(config?: Partial<FailoverConfig>);
    /**
     * Register providers and circuit breakers
     */
    registerProviders(providers: ProviderAdapter[], circuitBreakers: Map<string, CircuitBreaker>): void;
    /**
     * Check if failover should be triggered based on provider health
     */
    shouldTriggerFailover(healthData: ProviderHealth): boolean;
    /**
     * Execute failover from one provider to another
     */
    executeFailover(fromProvider: ProviderAdapter, toProvider: ProviderAdapter | null, reason: string): Promise<boolean>;
    /**
     * Get the next healthy provider
     */
    getNextHealthyProvider(currentProvider: ProviderAdapter, allProviders: ProviderAdapter[]): ProviderAdapter | null;
    /**
     * Manually trigger a failover
     */
    manualFailover(fromProviderId: string, toProviderId: string | null, reason: string): Promise<boolean>;
    /**
     * Check if a provider recently failed over
     */
    hasRecentlyFailedOver(providerId: string, windowMs?: number): boolean;
    /**
     * Get recent failover events
     */
    getRecentFailovers(maxAge?: number): FailoverEvent[];
    /**
     * Get failover statistics
     */
    getFailoverStats(): {
        totalFailovers: number;
        activeFailovers: number;
        recentFailovers: number;
        failoversByProvider: Map<string, number>;
    };
    /**
     * Reset failover statistics for a provider
     */
    resetFailoverStats(providerId: string): void;
    /**
     * Subscribe to failover events
     */
    onFailover(listener: (event: FailoverEvent) => void): void;
    /**
     * Notify all listeners of failover event
     */
    private notifyFailover;
    /**
     * Helper to add delay
     */
    private delay;
    /**
     * Get current failover state
     */
    getState(): {
        enabled: boolean;
        activeFailovers: number;
        maxConcurrentFailovers: number;
        lastCascadePreventionTime: number;
        totalEvents: number;
    };
}
//# sourceMappingURL=FailoverController.d.ts.map