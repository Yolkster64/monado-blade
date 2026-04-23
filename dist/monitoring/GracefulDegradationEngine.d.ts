/**
 * Graceful Degradation Engine
 * Reduces service levels under load to maintain core functionality
 * Implements task prioritization and quality reduction strategies
 */
import { DegradationMode, DegradationConfig } from './types';
export interface DegradationStrategy {
    mode: DegradationMode;
    timeoutMultiplier: number;
    shedNonCriticalTasks: boolean;
    qualityReduction: number;
}
export declare class GracefulDegradationEngine {
    private currentMode;
    private readonly config;
    private degradationStartTime;
    private failedProviderCount;
    private totalProviderCount;
    private listeners;
    constructor(config?: Partial<DegradationConfig>);
    /**
     * Evaluate system health and determine degradation mode
     */
    evaluateSystemHealth(healthyProviders: number, totalProviders: number, averageErrorRate: number): DegradationMode;
    /**
     * Update current degradation mode
     */
    private updateDegradationMode;
    /**
     * Get current degradation mode
     */
    getCurrentMode(): DegradationMode;
    /**
     * Get degradation strategy for current mode
     */
    getStrategy(): DegradationStrategy;
    /**
     * Calculate timeout multiplier based on degradation mode
     */
    private getTimeoutMultiplier;
    /**
     * Check if non-critical tasks should be shed
     */
    private shouldShedNonCriticalTasks;
    /**
     * Get quality reduction factor (0-1)
     */
    private getQualityReduction;
    /**
     * Adjust timeout based on degradation mode
     */
    adjustTimeout(baseTimeout: number): number;
    /**
     * Determine if a task should be executed
     */
    shouldExecuteTask(taskPriority: 'CRITICAL' | 'HIGH' | 'NORMAL' | 'LOW'): boolean;
    /**
     * Get duration of current degradation (in milliseconds)
     */
    getDegradationDuration(): number;
    /**
     * Get system health status
     */
    getHealthStatus(): {
        mode: DegradationMode;
        healthRatio: number;
        failedProviders: number;
        totalProviders: number;
        duration: number;
        avgErrorRate: number;
    };
    /**
     * Recovery plan when transitioning out of degradation
     */
    getRecoveryPlan(): {
        steps: string[];
        estimatedRecoveryTime: number;
    };
    /**
     * Subscribe to degradation mode changes
     */
    onModeChange(listener: (mode: DegradationMode) => void): void;
    /**
     * Notify listeners of mode change
     */
    private notifyModeChange;
    /**
     * Get detailed state information
     */
    getState(): {
        currentMode: DegradationMode;
        strategy: DegradationStrategy;
        healthStatus: ReturnType<GracefulDegradationEngine['getHealthStatus']>;
        recoveryPlan: ReturnType<GracefulDegradationEngine['getRecoveryPlan']>;
    };
    /**
     * Reset degradation state
     */
    reset(): void;
}
//# sourceMappingURL=GracefulDegradationEngine.d.ts.map