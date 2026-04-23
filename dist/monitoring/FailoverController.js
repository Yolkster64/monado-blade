"use strict";
/**
 * Failover Controller
 * Automatically detects provider failures and triggers failover
 * Implements graceful degradation and rate limiting
 */
Object.defineProperty(exports, "__esModule", { value: true });
exports.FailoverController = void 0;
const types_1 = require("./types");
class FailoverController {
    constructor(config = {}) {
        this.circuitBreakers = new Map();
        this.failoverTimestamps = new Map();
        this.activeFailovers = 0;
        this.lastCascadePreventionTime = 0;
        this.failoverEvents = [];
        this.listeners = [];
        this.providers = new Map();
        this.config = {
            enabled: config.enabled ?? true,
            maxConcurrentFailovers: config.maxConcurrentFailovers ?? 3,
            failoverDelay: config.failoverDelay ?? 1000,
            cascadePreventionDelay: config.cascadePreventionDelay ?? 5000
        };
    }
    /**
     * Register providers and circuit breakers
     */
    registerProviders(providers, circuitBreakers) {
        for (const provider of providers) {
            this.providers.set(provider.providerId, provider);
            const cb = circuitBreakers.get(provider.providerId);
            if (cb) {
                this.circuitBreakers.set(provider.providerId, cb);
            }
        }
    }
    /**
     * Check if failover should be triggered based on provider health
     */
    shouldTriggerFailover(healthData) {
        if (!this.config.enabled) {
            return false;
        }
        // Don't failover if provider is still healthy
        if (healthData.currentStatus === types_1.HealthStatus.HEALTHY) {
            return false;
        }
        // Check if circuit breaker is open
        const circuitBreaker = this.circuitBreakers.get(healthData.providerId);
        if (!circuitBreaker || circuitBreaker.getState() !== types_1.CircuitBreakerState.OPEN) {
            return false;
        }
        // Check concurrent failover limit
        if (this.activeFailovers >= this.config.maxConcurrentFailovers) {
            return false;
        }
        // Check cascade prevention delay
        const timeSinceLastCascade = Date.now() - this.lastCascadePreventionTime;
        if (timeSinceLastCascade < this.config.cascadePreventionDelay) {
            return false;
        }
        return true;
    }
    /**
     * Execute failover from one provider to another
     */
    async executeFailover(fromProvider, toProvider, reason) {
        if (!this.config.enabled) {
            return false;
        }
        if (this.activeFailovers >= this.config.maxConcurrentFailovers) {
            return false;
        }
        try {
            this.activeFailovers++;
            this.lastCascadePreventionTime = Date.now();
            // Add delay to prevent immediate cascade
            await this.delay(this.config.failoverDelay);
            const targetProviderId = toProvider?.providerId ?? 'NONE';
            // Check if target provider is healthy
            if (toProvider) {
                const canExecute = this.circuitBreakers
                    .get(toProvider.providerId)
                    ?.canExecute() ?? false;
                if (!canExecute) {
                    return false;
                }
            }
            const event = {
                timestamp: Date.now(),
                fromProviderId: fromProvider.providerId,
                toProviderId: targetProviderId,
                reason,
                success: true
            };
            this.failoverTimestamps.set(fromProvider.providerId, Date.now());
            this.failoverEvents.push(event);
            // Keep only recent events
            if (this.failoverEvents.length > 1000) {
                this.failoverEvents.shift();
            }
            this.notifyFailover(event);
            return true;
        }
        catch (error) {
            return false;
        }
        finally {
            this.activeFailovers--;
        }
    }
    /**
     * Get the next healthy provider
     */
    getNextHealthyProvider(currentProvider, allProviders) {
        for (const provider of allProviders) {
            if (provider.providerId === currentProvider.providerId) {
                continue;
            }
            const circuitBreaker = this.circuitBreakers.get(provider.providerId);
            if (circuitBreaker && circuitBreaker.canExecute()) {
                return provider;
            }
        }
        return null;
    }
    /**
     * Manually trigger a failover
     */
    async manualFailover(fromProviderId, toProviderId, reason) {
        const fromProvider = this.providers.get(fromProviderId);
        const toProvider = toProviderId ? this.providers.get(toProviderId) : null;
        if (!fromProvider) {
            return false;
        }
        return this.executeFailover(fromProvider, toProvider ?? null, reason);
    }
    /**
     * Check if a provider recently failed over
     */
    hasRecentlyFailedOver(providerId, windowMs = 30000) {
        const lastFailoverTime = this.failoverTimestamps.get(providerId);
        if (!lastFailoverTime) {
            return false;
        }
        return Date.now() - lastFailoverTime < windowMs;
    }
    /**
     * Get recent failover events
     */
    getRecentFailovers(maxAge = 300000) {
        const cutoffTime = Date.now() - maxAge;
        return this.failoverEvents.filter(event => event.timestamp >= cutoffTime);
    }
    /**
     * Get failover statistics
     */
    getFailoverStats() {
        const oneHourAgo = Date.now() - 3600000;
        const recentFailovers = this.failoverEvents.filter(e => e.timestamp >= oneHourAgo);
        const failoversByProvider = new Map();
        for (const event of this.failoverEvents) {
            const count = failoversByProvider.get(event.fromProviderId) ?? 0;
            failoversByProvider.set(event.fromProviderId, count + 1);
        }
        return {
            totalFailovers: this.failoverEvents.length,
            activeFailovers: this.activeFailovers,
            recentFailovers: recentFailovers.length,
            failoversByProvider
        };
    }
    /**
     * Reset failover statistics for a provider
     */
    resetFailoverStats(providerId) {
        this.failoverTimestamps.delete(providerId);
        this.failoverEvents = this.failoverEvents.filter(event => event.fromProviderId !== providerId);
    }
    /**
     * Subscribe to failover events
     */
    onFailover(listener) {
        this.listeners.push(listener);
    }
    /**
     * Notify all listeners of failover event
     */
    notifyFailover(event) {
        for (const listener of this.listeners) {
            listener(event);
        }
    }
    /**
     * Helper to add delay
     */
    delay(ms) {
        return new Promise(resolve => setTimeout(resolve, ms));
    }
    /**
     * Get current failover state
     */
    getState() {
        return {
            enabled: this.config.enabled,
            activeFailovers: this.activeFailovers,
            maxConcurrentFailovers: this.config.maxConcurrentFailovers,
            lastCascadePreventionTime: this.lastCascadePreventionTime,
            totalEvents: this.failoverEvents.length
        };
    }
}
exports.FailoverController = FailoverController;
//# sourceMappingURL=FailoverController.js.map