/**
 * Enhanced Anomaly Detector v2 for HELIOS v4.0
 * Pattern-based detection of system anomalies and abnormal behaviors
 * Performance Target: 95% accuracy, <1ms overhead
 */

const { calculateStats } = require('../utils/metrics');

class AnomalyDetectorV2 {
  /**
   * @param {Object} config - Configuration
   * @param {number} config.sensitivityLevel - 1-10, default 5
   * @param {number} config.baselineWindow - Milliseconds for baseline (default: 3600000)
   * @param {number} config.stdDevThreshold - Std dev multiplier for anomaly (default: 2.5)
   */
  constructor(config = {}) {
    this.config = {
      sensitivityLevel: Math.max(1, Math.min(10, config.sensitivityLevel || 5)),
      baselineWindow: config.baselineWindow || 3600000,
      stdDevThreshold: config.stdDevThreshold || 2.5,
      minDataPoints: config.minDataPoints || 10,
      ...config,
    };

    this.baselines = {};
    this.metrics = {
      anomaliesDetected: 0,
      falsePositives: 0,
      patterns: {},
    };
  }

  /**
   * Learn baseline behavior from metric values
   * @param {string} metric - Metric name (e.g., 'api.latency', 'cache.hitRate')
   * @param {number[]} values - Array of observed values
   */
  learnBaseline(metric, values) {
    if (values.length < this.config.minDataPoints) return;

    const stats = calculateStats(values);
    
    this.baselines[metric] = {
      mean: stats.avg,
      stdDev: this._calculateStdDev(values, stats.avg),
      min: stats.min,
      max: stats.max,
      p50: stats.p50,
      p95: stats.p95,
      p99: stats.p99,
      learned: Date.now(),
      samples: values.length,
    };
  }

  /**
   * Detect if a value is anomalous
   * @param {string} metric - Metric name
   * @param {number} value - Value to check
   * @returns {Object} Anomaly report
   */
  detectAnomaly(metric, value) {
    if (!this.baselines[metric]) {
      return { isAnomaly: false, reason: 'NO_BASELINE', severity: 0 };
    }

    const baseline = this.baselines[metric];
    const threshold = baseline.stdDev * this.config.stdDevThreshold * (1 + (6 - this.config.sensitivityLevel) * 0.1);

    const deviation = Math.abs(value - baseline.mean);
    const deviationRatio = deviation / (baseline.stdDev || 1);

    let isAnomaly = false;
    let severity = 0;
    let reason = 'NORMAL';

    if (deviation > threshold) {
      isAnomaly = true;
      severity = Math.min(1, deviationRatio / (this.config.stdDevThreshold * 2));
      reason = value > baseline.mean ? 'SPIKE' : 'DIP';
      this.metrics.anomaliesDetected++;
    }

    // Store pattern
    if (isAnomaly) {
      if (!this.metrics.patterns[metric]) {
        this.metrics.patterns[metric] = [];
      }
      this.metrics.patterns[metric].push({
        timestamp: Date.now(),
        value,
        reason,
        severity,
      });
    }

    return {
      isAnomaly,
      reason,
      severity,
      value,
      expectedRange: {
        min: baseline.mean - threshold,
        max: baseline.mean + threshold,
      },
      deviation: deviation.toFixed(2),
      deviationRatio: deviationRatio.toFixed(2),
      baseline: baseline.mean.toFixed(2),
    };
  }

  /**
   * Get anomaly patterns for a metric
   * @param {string} metric - Metric name
   * @param {number} limit - Max patterns to return
   * @returns {Array} Recent anomalies
   */
  getAnomalies(metric, limit = 20) {
    if (!this.metrics.patterns[metric]) return [];
    return this.metrics.patterns[metric].slice(-limit);
  }

  /**
   * Predict next anomaly based on patterns
   * @param {string} metric - Metric name
   * @returns {Object} Prediction
   */
  predictAnomaly(metric) {
    const patterns = this.metrics.patterns[metric];
    if (!patterns || patterns.length < 2) {
      return { prediction: 'INSUFFICIENT_DATA', confidence: 0 };
    }

    // Analyze pattern frequency
    const now = Date.now();
    const recentPatterns = patterns.filter(p => now - p.timestamp < this.config.baselineWindow);
    
    if (recentPatterns.length === 0) return { prediction: 'NO_RECENT_PATTERNS', confidence: 0 };

    // Calculate interval between anomalies
    const intervals = [];
    for (let i = 1; i < recentPatterns.length; i++) {
      intervals.push(recentPatterns[i].timestamp - recentPatterns[i - 1].timestamp);
    }

    const intervalStats = calculateStats(intervals);
    const nextPredicted = recentPatterns[recentPatterns.length - 1].timestamp + intervalStats.avg;

    return {
      prediction: 'ANOMALY_LIKELY',
      predictedTime: new Date(nextPredicted).toISOString(),
      confidence: Math.min(0.95, 0.5 + (recentPatterns.length / 10)),
      frequency: `every ${(intervalStats.avg / 1000).toFixed(1)}s`,
      avgSeverity: (recentPatterns.reduce((s, p) => s + p.severity, 0) / recentPatterns.length).toFixed(2),
    };
  }

  /**
   * Get detector metrics
   * @returns {Object} Metrics summary
   */
  getMetrics() {
    return {
      anomaliesDetected: this.metrics.anomaliesDetected,
      metricsMonitored: Object.keys(this.baselines).length,
      patternsLearned: Object.keys(this.metrics.patterns).length,
      sensitivityLevel: this.config.sensitivityLevel,
      recentAnomalies: Object.entries(this.metrics.patterns).reduce((sum, [_, patterns]) => sum + patterns.length, 0),
    };
  }

  /**
   * Clear baseline for metric (e.g., for retraining)
   * @param {string} metric - Metric name or '*' to clear all
   */
  clearBaseline(metric = '*') {
    if (metric === '*') {
      this.baselines = {};
      this.metrics.patterns = {};
    } else {
      delete this.baselines[metric];
      delete this.metrics.patterns[metric];
    }
  }

  /**
   * Internal: Calculate standard deviation
   * @private
   */
  _calculateStdDev(values, mean) {
    if (values.length < 2) return 0;
    const variance = values.reduce((sum, v) => sum + Math.pow(v - mean, 2), 0) / values.length;
    return Math.sqrt(variance);
  }
}

module.exports = AnomalyDetectorV2;
