/**
 * Auto-Scaling AI Advisor for HELIOS v4.0
 * Analyzes metrics and recommends scaling actions for auto-scaling systems
 * Performance Target: +20% resource efficiency
 */

const { calculateStats } = require('../utils/metrics');

class AutoScalingAdvisor {
  /**
   * @param {Object} config - Configuration
   * @param {number} config.cpuThreshold - CPU % to trigger scale-up (default: 80)
   * @param {number} config.memoryThreshold - Memory % to trigger scale-up (default: 75)
   * @param {number} config.latencyThreshold - Latency ms to trigger scale-up (default: 300)
   * @param {number} config.evaluationWindow - Window for analysis (default: 300000)
   */
  constructor(config = {}) {
    this.config = {
      cpuThreshold: config.cpuThreshold || 80,
      memoryThreshold: config.memoryThreshold || 75,
      latencyThreshold: config.latencyThreshold || 300,
      evaluationWindow: config.evaluationWindow || 300000,
      scaleUpMultiplier: config.scaleUpMultiplier || 1.5,
      scaleDownMultiplier: config.scaleDownMultiplier || 0.7,
      ...config,
    };

    this.metrics = {
      evaluations: 0,
      scaleUpRecommendations: 0,
      scaleDownRecommendations: 0,
      recommendations: [],
    };

    this.metricHistory = [];
  }

  /**
   * Record system metrics for analysis
   * @param {Object} snapshot - Metrics snapshot
   */
  recordMetrics(snapshot) {
    const now = Date.now();
    
    this.metricHistory.push({
      timestamp: now,
      cpu: snapshot.cpu || 0,
      memory: snapshot.memory || 0,
      latency: snapshot.latency || 0,
      requestsPerSecond: snapshot.requestsPerSecond || 0,
      activeConnections: snapshot.activeConnections || 0,
    });

    // Keep only recent history
    this.metricHistory = this.metricHistory.filter(
      m => now - m.timestamp < this.config.evaluationWindow * 2
    );
  }

  /**
   * Analyze metrics and generate scaling recommendations
   * @param {Object} currentCapacity - Current system capacity
   * @returns {Object} Recommendations
   */
  analyze(currentCapacity = {}) {
    this.metrics.evaluations++;

    if (this.metricHistory.length < 2) {
      return { recommendation: 'INSUFFICIENT_DATA', confidence: 0 };
    }

    const recentMetrics = this.metricHistory.slice(-20);
    const cpuValues = recentMetrics.map(m => m.cpu);
    const memoryValues = recentMetrics.map(m => m.memory);
    const latencyValues = recentMetrics.map(m => m.latency);

    const cpuStats = calculateStats(cpuValues);
    const memoryStats = calculateStats(memoryValues);
    const latencyStats = calculateStats(latencyValues);

    let recommendation = 'MAINTAIN';
    let confidence = 0;
    const reasons = [];

    // Scale-up signals
    if (cpuStats.p95 > this.config.cpuThreshold) {
      reasons.push(`High CPU (p95: ${cpuStats.p95.toFixed(1)}%)`);
      confidence += 0.3;
    }

    if (memoryStats.p95 > this.config.memoryThreshold) {
      reasons.push(`High Memory (p95: ${memoryStats.p95.toFixed(1)}%)`);
      confidence += 0.3;
    }

    if (latencyStats.p99 > this.config.latencyThreshold) {
      reasons.push(`High Latency (p99: ${latencyStats.p99.toFixed(0)}ms)`);
      confidence += 0.4;
    }

    if (confidence > 0.5) {
      recommendation = 'SCALE_UP';
      this.metrics.scaleUpRecommendations++;
    } else if (cpuStats.p50 < 20 && memoryStats.p50 < 30 && latencyStats.p50 < 100) {
      recommendation = 'SCALE_DOWN';
      this.metrics.scaleDownRecommendations++;
      confidence = 0.6;
      reasons.push('Sustained low utilization');
    }

    const rec = {
      recommendation,
      confidence: Math.min(1, confidence),
      reasons,
      metrics: {
        cpu: cpuStats,
        memory: memoryStats,
        latency: latencyStats,
      },
      suggestedCapacity: recommendation === 'SCALE_UP'
        ? {
          instances: Math.ceil((currentCapacity.instances || 1) * this.config.scaleUpMultiplier),
          workers: Math.ceil((currentCapacity.workers || 4) * this.config.scaleUpMultiplier),
        }
        : recommendation === 'SCALE_DOWN'
          ? {
            instances: Math.max(1, Math.floor((currentCapacity.instances || 1) * this.config.scaleDownMultiplier)),
            workers: Math.max(1, Math.floor((currentCapacity.workers || 4) * this.config.scaleDownMultiplier)),
          }
          : currentCapacity,
    };

    this.metrics.recommendations.push(rec);
    return rec;
  }

  /**
   * Get historical recommendations
   * @param {number} limit - Max results to return
   * @returns {Array} Recent recommendations
   */
  getRecommendationHistory(limit = 10) {
    return this.metrics.recommendations.slice(-limit);
  }

  /**
   * Get advisor metrics
   * @returns {Object} Metrics summary
   */
  getMetrics() {
    return {
      totalEvaluations: this.metrics.evaluations,
      scaleUpRecommendations: this.metrics.scaleUpRecommendations,
      scaleDownRecommendations: this.metrics.scaleDownRecommendations,
      maintainRecommendations: this.metrics.evaluations
        - this.metrics.scaleUpRecommendations
        - this.metrics.scaleDownRecommendations,
      scaleUpRate: this.metrics.evaluations > 0
        ? ((this.metrics.scaleUpRecommendations / this.metrics.evaluations) * 100).toFixed(2)
        : 0,
      historySize: this.metricHistory.length,
    };
  }

  /**
   * Clear history
   */
  clearHistory() {
    this.metricHistory = [];
  }
}

module.exports = AutoScalingAdvisor;
