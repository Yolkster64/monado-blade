"use strict";
/**
 * Health Check Scheduler
 * Manages periodic health checks for all providers with adaptive intervals
 * Tracks health history and detects degradation patterns
 */
Object.defineProperty(exports, "__esModule", { value: true });
exports.HealthCheckScheduler = void 0;
const types_1 = require("./types");
class HealthCheckScheduler {
    constructor(config = {}) {
        this.circuitBreakers = new Map();
        this.healthHistory = new Map();
        this.activeTimers = new Map();
        this.lastCheckTime = new Map();
        this.nextCheckTime = new Map();
        this.checkIntervals = new Map();
        this.isRunning = false;
        this.listeners = {};
        this.config = {
            interval: config.interval ?? 30000,
            timeout: config.timeout ?? 10000,
            adaptiveInterval: config.adaptiveInterval ?? true,
            degradingIntervalMultiplier: config.degradingIntervalMultiplier ?? 0.5,
            minInterval: config.minInterval ?? 5000,
            maxInterval: config.maxInterval ?? 120000
        };
    }
    /**
     * Register a provider for health checks
     */
    registerProvider(provider, circuitBreaker) {
        this.circuitBreakers.set(provider.providerId, circuitBreaker);
        this.healthHistory.set(provider.providerId, {
            results: [],
            maxSize: 100,
            degradationTrend: false,
            trendWindow: 10
        });
        this.checkIntervals.set(provider.providerId, this.config.interval);
        this.lastCheckTime.set(provider.providerId, 0);
        this.nextCheckTime.set(provider.providerId, Date.now());
    }
    /**
     * Unregister a provider from health checks
     */
    unregisterProvider(providerId) {
        this.stopCheckForProvider(providerId);
        this.circuitBreakers.delete(providerId);
        this.healthHistory.delete(providerId);
        this.checkIntervals.delete(providerId);
        this.lastCheckTime.delete(providerId);
        this.nextCheckTime.delete(providerId);
    }
    /**
     * Start health check scheduler
     */
    async start(providers) {
        if (this.isRunning) {
            return;
        }
        this.isRunning = true;
        for (const provider of providers) {
            const cb = this.circuitBreakers.get(provider.providerId);
            if (cb) {
                this.scheduleNextCheck(provider.providerId);
            }
        }
    }
    /**
     * Stop health check scheduler
     */
    stop() {
        this.isRunning = false;
        for (const [providerId] of this.activeTimers) {
            this.stopCheckForProvider(providerId);
        }
        this.activeTimers.clear();
    }
    /**
     * Schedule the next health check for a provider
     */
    scheduleNextCheck(providerId) {
        if (!this.isRunning) {
            return;
        }
        const timer = setTimeout(() => {
            this.executeHealthCheck(providerId).catch(err => {
                console.error(`Health check failed for ${providerId}:`, err);
            });
        }, Math.max(0, (this.nextCheckTime.get(providerId) ?? 0) - Date.now()));
        this.activeTimers.set(providerId, timer);
    }
    /**
     * Execute a single health check for a provider
     */
    async executeHealthCheck(providerId) {
        const circuitBreaker = this.circuitBreakers.get(providerId);
        if (!circuitBreaker) {
            return;
        }
        if (!circuitBreaker.canExecute()) {
            const retryTime = circuitBreaker.getTimeUntilRetry();
            this.nextCheckTime.set(providerId, Date.now() + retryTime);
            this.scheduleNextCheck(providerId);
            return;
        }
        this.lastCheckTime.set(providerId, Date.now());
        try {
            // Note: In production, this would call the actual provider
            // For now, create a mock result
            const result = {
                providerId,
                timestamp: Date.now(),
                status: types_1.HealthStatus.HEALTHY,
                latency: 0,
                errorRate: 0,
                successRate: 1,
                checksDone: 1
            };
            // Process the result
            this.processHealthCheckResult(result);
        }
        catch (error) {
            const result = {
                providerId,
                timestamp: Date.now(),
                status: types_1.HealthStatus.UNHEALTHY,
                latency: 0,
                errorRate: 1,
                successRate: 0,
                checksDone: 1,
                error: error instanceof Error ? error.message : 'Unknown error'
            };
            this.processHealthCheckResult(result);
        }
        // Schedule next check with adaptive interval
        this.updateCheckInterval(providerId);
        this.nextCheckTime.set(providerId, Date.now() + (this.checkIntervals.get(providerId) ?? this.config.interval));
        this.scheduleNextCheck(providerId);
    }
    /**
     * Process a health check result
     */
    processHealthCheckResult(result) {
        const history = this.healthHistory.get(result.providerId);
        if (!history) {
            return;
        }
        // Add to history
        history.results.push(result);
        if (history.results.length > history.maxSize) {
            history.results.shift();
        }
        // Update circuit breaker
        const circuitBreaker = this.circuitBreakers.get(result.providerId);
        if (circuitBreaker) {
            circuitBreaker.recordResult(result);
        }
        // Detect degradation patterns
        const isDegrading = this.detectDegradation(history);
        if (isDegrading !== history.degradationTrend) {
            history.degradationTrend = isDegrading;
            if (this.listeners.onDegradationDetected) {
                this.listeners.onDegradationDetected(result.providerId, isDegrading);
            }
        }
        // Notify listeners
        if (this.listeners.onHealthCheckResult) {
            this.listeners.onHealthCheckResult(result);
        }
    }
    /**
     * Detect degradation patterns in recent check history
     */
    detectDegradation(history) {
        if (history.results.length < history.trendWindow) {
            return false;
        }
        const recentResults = history.results.slice(-history.trendWindow);
        const unhealthyCount = recentResults.filter(r => r.status !== 'HEALTHY').length;
        const degradedCount = recentResults.filter(r => r.status === 'DEGRADED').length;
        return (unhealthyCount + degradedCount) > history.trendWindow * 0.5;
    }
    /**
     * Update check interval based on provider health
     */
    updateCheckInterval(providerId) {
        if (!this.config.adaptiveInterval) {
            this.checkIntervals.set(providerId, this.config.interval);
            return;
        }
        const history = this.healthHistory.get(providerId);
        const circuitBreaker = this.circuitBreakers.get(providerId);
        if (!history || !circuitBreaker) {
            return;
        }
        let newInterval = this.config.interval;
        // If degrading, check more frequently
        if (history.degradationTrend) {
            newInterval = Math.max(this.config.minInterval, this.config.interval * this.config.degradingIntervalMultiplier);
        }
        // If circuit is open or half-open, check more frequently for recovery
        const state = circuitBreaker.getState();
        if (state === types_1.CircuitBreakerState.HALF_OPEN || state === types_1.CircuitBreakerState.OPEN) {
            newInterval = Math.max(this.config.minInterval, this.config.interval * this.config.degradingIntervalMultiplier);
        }
        // If healthy, check less frequently
        if (!history.degradationTrend && state === types_1.CircuitBreakerState.CLOSED) {
            newInterval = Math.min(this.config.maxInterval, this.config.interval * 2);
        }
        this.checkIntervals.set(providerId, newInterval);
    }
    /**
     * Stop health check for a specific provider
     */
    stopCheckForProvider(providerId) {
        const timer = this.activeTimers.get(providerId);
        if (timer) {
            clearTimeout(timer);
            this.activeTimers.delete(providerId);
        }
    }
    /**
     * Get health history for a provider
     */
    getHealthHistory(providerId) {
        return this.healthHistory.get(providerId)?.results ?? [];
    }
    /**
     * Get current check interval for a provider
     */
    getCheckInterval(providerId) {
        return this.checkIntervals.get(providerId) ?? this.config.interval;
    }
    /**
     * Subscribe to health check events
     */
    on(event, listener) {
        if (event === 'healthCheckResult') {
            this.listeners.onHealthCheckResult = listener;
        }
        else if (event === 'degradationDetected') {
            this.listeners.onDegradationDetected = listener;
        }
    }
    /**
     * Get scheduler statistics
     */
    getStatistics() {
        const intervals = Array.from(this.checkIntervals.values());
        const averageCheckInterval = intervals.length > 0
            ? intervals.reduce((a, b) => a + b, 0) / intervals.length
            : 0;
        return {
            isRunning: this.isRunning,
            registeredProviders: this.circuitBreakers.size,
            averageCheckInterval,
            lastCheckTimes: new Map(this.lastCheckTime)
        };
    }
}
exports.HealthCheckScheduler = HealthCheckScheduler;
//# sourceMappingURL=HealthCheckScheduler.js.map