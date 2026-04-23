"use strict";
/**
 * Graceful Degradation Engine
 * Reduces service levels under load to maintain core functionality
 * Implements task prioritization and quality reduction strategies
 */
Object.defineProperty(exports, "__esModule", { value: true });
exports.GracefulDegradationEngine = void 0;
const types_1 = require("./types");
class GracefulDegradationEngine {
    constructor(config = {}) {
        this.currentMode = types_1.DegradationMode.NORMAL;
        this.degradationStartTime = 0;
        this.failedProviderCount = 0;
        this.totalProviderCount = 0;
        this.listeners = [];
        this.config = {
            enabledAt: config.enabledAt ?? 0.5,
            timeoutMultiplier: config.timeoutMultiplier ?? 2,
            maxTimeoutMs: config.maxTimeoutMs ?? 30000,
            shedNonCriticalTasks: config.shedNonCriticalTasks ?? true
        };
    }
    /**
     * Evaluate system health and determine degradation mode
     */
    evaluateSystemHealth(healthyProviders, totalProviders, averageErrorRate) {
        this.totalProviderCount = totalProviders;
        this.failedProviderCount = totalProviders - healthyProviders;
        const healthRatio = totalProviders > 0 ? healthyProviders / totalProviders : 1;
        let newMode = types_1.DegradationMode.NORMAL;
        // Check provider health ratio first (most critical)
        if (healthRatio < this.config.enabledAt * 0.5) {
            newMode = types_1.DegradationMode.CRITICAL;
        }
        else if (healthRatio < this.config.enabledAt) {
            newMode = types_1.DegradationMode.REDUCED;
        }
        // Check error rate (can escalate but not downgrade from what health ratio set)
        if (averageErrorRate > 0.3) {
            newMode = types_1.DegradationMode.CRITICAL;
        }
        else if (averageErrorRate > 0.1 && newMode === types_1.DegradationMode.NORMAL) {
            newMode = types_1.DegradationMode.REDUCED;
        }
        this.updateDegradationMode(newMode);
        return newMode;
    }
    /**
     * Update current degradation mode
     */
    updateDegradationMode(mode) {
        if (mode === this.currentMode) {
            return;
        }
        this.currentMode = mode;
        if (mode !== types_1.DegradationMode.NORMAL && this.degradationStartTime === 0) {
            this.degradationStartTime = Date.now();
        }
        else if (mode === types_1.DegradationMode.NORMAL) {
            this.degradationStartTime = 0;
        }
        this.notifyModeChange(mode);
    }
    /**
     * Get current degradation mode
     */
    getCurrentMode() {
        return this.currentMode;
    }
    /**
     * Get degradation strategy for current mode
     */
    getStrategy() {
        return {
            mode: this.currentMode,
            timeoutMultiplier: this.getTimeoutMultiplier(),
            shedNonCriticalTasks: this.shouldShedNonCriticalTasks(),
            qualityReduction: this.getQualityReduction()
        };
    }
    /**
     * Calculate timeout multiplier based on degradation mode
     */
    getTimeoutMultiplier() {
        switch (this.currentMode) {
            case types_1.DegradationMode.NORMAL:
                return 1;
            case types_1.DegradationMode.REDUCED:
                return this.config.timeoutMultiplier;
            case types_1.DegradationMode.CRITICAL:
                return this.config.timeoutMultiplier * 2;
            default:
                return 1;
        }
    }
    /**
     * Check if non-critical tasks should be shed
     */
    shouldShedNonCriticalTasks() {
        if (!this.config.shedNonCriticalTasks) {
            return false;
        }
        return this.currentMode === types_1.DegradationMode.REDUCED ||
            this.currentMode === types_1.DegradationMode.CRITICAL;
    }
    /**
     * Get quality reduction factor (0-1)
     */
    getQualityReduction() {
        switch (this.currentMode) {
            case types_1.DegradationMode.NORMAL:
                return 0;
            case types_1.DegradationMode.REDUCED:
                return 0.2;
            case types_1.DegradationMode.CRITICAL:
                return 0.5;
            default:
                return 0;
        }
    }
    /**
     * Adjust timeout based on degradation mode
     */
    adjustTimeout(baseTimeout) {
        const multiplier = this.getTimeoutMultiplier();
        const adjusted = baseTimeout * multiplier;
        return Math.min(adjusted, this.config.maxTimeoutMs);
    }
    /**
     * Determine if a task should be executed
     */
    shouldExecuteTask(taskPriority) {
        if (this.currentMode === types_1.DegradationMode.NORMAL) {
            return true;
        }
        if (this.currentMode === types_1.DegradationMode.REDUCED) {
            return taskPriority === 'CRITICAL' || taskPriority === 'HIGH';
        }
        if (this.currentMode === types_1.DegradationMode.CRITICAL) {
            return taskPriority === 'CRITICAL';
        }
        return true;
    }
    /**
     * Get duration of current degradation (in milliseconds)
     */
    getDegradationDuration() {
        if (this.currentMode === types_1.DegradationMode.NORMAL || this.degradationStartTime === 0) {
            return 0;
        }
        return Date.now() - this.degradationStartTime;
    }
    /**
     * Get system health status
     */
    getHealthStatus() {
        const healthRatio = this.totalProviderCount > 0
            ? (this.totalProviderCount - this.failedProviderCount) / this.totalProviderCount
            : 1;
        return {
            mode: this.currentMode,
            healthRatio,
            failedProviders: this.failedProviderCount,
            totalProviders: this.totalProviderCount,
            duration: this.getDegradationDuration(),
            avgErrorRate: 0
        };
    }
    /**
     * Recovery plan when transitioning out of degradation
     */
    getRecoveryPlan() {
        const steps = [];
        let estimatedTime = 0;
        if (this.currentMode === types_1.DegradationMode.CRITICAL) {
            steps.push('Restore primary provider connection');
            steps.push('Increase timeout values gradually');
            steps.push('Re-enable non-critical task processing');
            steps.push('Monitor error rates for stability');
            estimatedTime = 60000;
        }
        else if (this.currentMode === types_1.DegradationMode.REDUCED) {
            steps.push('Monitor system stability');
            steps.push('Gradually reduce timeout multipliers');
            steps.push('Resume normal task priorities');
            estimatedTime = 30000;
        }
        return {
            steps,
            estimatedRecoveryTime: estimatedTime
        };
    }
    /**
     * Subscribe to degradation mode changes
     */
    onModeChange(listener) {
        this.listeners.push(listener);
    }
    /**
     * Notify listeners of mode change
     */
    notifyModeChange(mode) {
        for (const listener of this.listeners) {
            listener(mode);
        }
    }
    /**
     * Get detailed state information
     */
    getState() {
        return {
            currentMode: this.currentMode,
            strategy: this.getStrategy(),
            healthStatus: this.getHealthStatus(),
            recoveryPlan: this.getRecoveryPlan()
        };
    }
    /**
     * Reset degradation state
     */
    reset() {
        this.currentMode = types_1.DegradationMode.NORMAL;
        this.degradationStartTime = 0;
        this.failedProviderCount = 0;
        this.totalProviderCount = 0;
    }
}
exports.GracefulDegradationEngine = GracefulDegradationEngine;
//# sourceMappingURL=GracefulDegradationEngine.js.map