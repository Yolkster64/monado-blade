/**
 * Circuit Breaker Pattern Implementation
 * Prevents cascading failures by monitoring provider health
 * States: Closed (normal) -> Open (failing) -> Half-Open (recovery test) -> Closed
 */
import { CircuitBreakerState, CircuitBreakerConfig, CircuitBreakerStateChange, HealthCheckResult } from './types';
export declare class CircuitBreaker {
    private readonly providerId;
    private state;
    private failureCount;
    private successCount;
    private lastFailureTime;
    private nextRetryTime;
    private backoffMultiplier;
    private readonly config;
    private stateChangeListeners;
    constructor(providerId: string, config?: Partial<CircuitBreakerConfig>);
    /**
     * Record a health check result and update circuit breaker state
     */
    recordResult(result: HealthCheckResult): void;
    /**
     * Record a successful check
     */
    recordSuccess(): void;
    /**
     * Record a failed check
     */
    recordFailure(reason: string): void;
    /**
     * Check if the circuit breaker allows requests
     */
    canExecute(): boolean;
    /**
     * Schedule the next retry attempt with exponential backoff
     */
    private scheduleNextRetry;
    /**
     * Transition to a new circuit breaker state
     */
    private transitionTo;
    /**
     * Get current state
     */
    getState(): CircuitBreakerState;
    /**
     * Reset the circuit breaker
     */
    reset(): void;
    /**
     * Get remaining time until next retry (in milliseconds)
     */
    getTimeUntilRetry(): number;
    /**
     * Subscribe to state change events
     */
    onStateChange(listener: (change: CircuitBreakerStateChange) => void): void;
    /**
     * Notify all listeners of state change
     */
    private notifyStateChange;
    /**
     * Get detailed state information
     */
    getDetailedState(): {
        state: CircuitBreakerState;
        failureCount: number;
        successCount: number;
        lastFailureTime: number;
        nextRetryTime: number;
        timeUntilRetry: number;
        backoffMultiplier: number;
    };
}
//# sourceMappingURL=CircuitBreaker.d.ts.map