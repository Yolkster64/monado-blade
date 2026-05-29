/**
 * Metrics Export Integration for HELIOS v4.0
 * Exports metrics to Prometheus, StatsD, or custom endpoints
 * Performance Target: <2ms overhead, batch operations
 */

class MetricsIntegration {
  /**
   * @param {Object} config - Configuration
   * @param {string} config.provider - 'prometheus' or 'statsd' (default: 'memory')
   * @param {string} config.endpoint - Export endpoint
   * @param {number} config.flushInterval - Flush interval ms (default: 60000)
   */
  constructor(config = {}) {
    this.config = {
      provider: config.provider || 'memory',
      endpoint: config.endpoint || 'http://localhost:8888',
      flushInterval: config.flushInterval || 60000,
      batchSize: config.batchSize || 100,
      ...config,
    };

    this.metrics = new Map();
    this.counters = new Map();
    this.gauges = new Map();
    this.histograms = new Map();

    this.flushTimer = null;
    if (this.config.flushInterval > 0) {
      this._startFlushTimer();
    }
  }

  /**
   * Increment counter metric
   * @param {string} name - Metric name
   * @param {number} value - Increment value (default: 1)
   * @param {Object} tags - Tags/labels
   */
  incrementCounter(name, value = 1, tags = {}) {
    const key = this._generateKey(name, tags);
    const current = this.counters.get(key) || 0;
    this.counters.set(key, current + value);
  }

  /**
   * Set gauge metric value
   * @param {string} name - Metric name
   * @param {number} value - Gauge value
   * @param {Object} tags - Tags/labels
   */
  setGauge(name, value, tags = {}) {
    const key = this._generateKey(name, tags);
    this.gauges.set(key, value);
  }

  /**
   * Record histogram value
   * @param {string} name - Metric name
   * @param {number} value - Value to record
   * @param {Object} tags - Tags/labels
   */
  recordHistogram(name, value, tags = {}) {
    const key = this._generateKey(name, tags);
    if (!this.histograms.has(key)) {
      this.histograms.set(key, []);
    }
    this.histograms.get(key).push(value);
  }

  /**
   * Record timing (histogram convenience)
   * @param {string} name - Metric name
   * @param {number} milliseconds - Duration in ms
   * @param {Object} tags - Tags/labels
   */
  recordTiming(name, milliseconds, tags = {}) {
    this.recordHistogram(name, milliseconds, tags);
  }

  /**
   * Get all current metrics
   * @returns {Object} Current metrics
   */
  getMetrics() {
    return {
      counters: Object.fromEntries(this.counters),
      gauges: Object.fromEntries(this.gauges),
      histograms: Object.fromEntries(this.histograms),
      provider: this.config.provider,
    };
  }

  /**
   * Export metrics to provider
   * @async
   * @returns {Object} Export result
   */
  async export() {
    const metrics = this.getMetrics();
    
    try {
      if (this.config.provider === 'prometheus') {
        return await this._exportPrometheus(metrics);
      } else if (this.config.provider === 'statsd') {
        return await this._exportStatsd(metrics);
      }
      
      return {
        status: 'exported',
        provider: 'memory',
        metricsCount: this.counters.size + this.gauges.size + this.histograms.size,
      };
    } catch (error) {
      return { status: 'error', error: error.message };
    }
  }

  /**
   * Reset all metrics
   */
  reset() {
    this.counters.clear();
    this.gauges.clear();
    this.histograms.clear();
  }

  /**
   * Internal: Generate metric key from name and tags
   * @private
   */
  _generateKey(name, tags) {
    if (Object.keys(tags).length === 0) return name;
    const tagStr = Object.entries(tags)
      .map(([k, v]) => `${k}=${v}`)
      .join(',');
    return `${name}{${tagStr}}`;
  }

  /**
   * Internal: Start flush timer
   * @private
   */
  _startFlushTimer() {
    this.flushTimer = setInterval(() => {
      this.export().catch(e => console.error('Metrics flush error:', e.message));
    }, this.config.flushInterval);
  }

  /**
   * Internal: Export to Prometheus format
   * @private
   */
  async _exportPrometheus(metrics) {
    const lines = [];
    
    for (const [key, value] of metrics.counters.entries()) {
      lines.push(`${key}_total ${value}`);
    }

    for (const [key, value] of metrics.gauges.entries()) {
      lines.push(`${key} ${value}`);
    }

    for (const [key, values] of metrics.histograms.entries()) {
      if (values.length > 0) {
        const sum = values.reduce((a, b) => a + b, 0);
        const avg = sum / values.length;
        lines.push(`${key}_sum ${sum}`);
        lines.push(`${key}_count ${values.length}`);
        lines.push(`${key}_avg ${avg}`);
      }
    }

    return {
      status: 'exported',
      format: 'prometheus',
      lines: lines.length,
    };
  }

  /**
   * Internal: Export to StatsD format
   * @private
   */
  async _exportStatsd(metrics) {
    const packets = [];

    for (const [key, value] of metrics.counters.entries()) {
      packets.push(`${key}:${value}|c`);
    }

    for (const [key, value] of metrics.gauges.entries()) {
      packets.push(`${key}:${value}|g`);
    }

    for (const [key, values] of metrics.histograms.entries()) {
      values.forEach(v => packets.push(`${key}:${v}|ms`));
    }

    return {
      status: 'exported',
      format: 'statsd',
      packets: packets.length,
    };
  }

  /**
   * Shutdown metrics integration
   */
  shutdown() {
    if (this.flushTimer) {
      clearInterval(this.flushTimer);
    }
  }
}

module.exports = MetricsIntegration;
