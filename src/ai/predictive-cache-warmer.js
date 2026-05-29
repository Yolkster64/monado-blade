/**
 * Predictive Cache Warmer for HELIOS v4.0
 * Uses ML to predict which cache entries will be needed and pre-warms them
 * Performance Target: +15% cache hit rate improvement
 */

const { calculateStats } = require('../utils/metrics');

class PredictiveCacheWarmer {
  /**
   * @param {Object} config - Configuration
   * @param {CacheStrategy} config.cache - Cache manager instance
   * @param {number} config.historyWindow - Milliseconds of history to analyze (default: 3600000)
   * @param {number} config.predictionThreshold - Confidence threshold 0-1 (default: 0.7)
   */
  constructor(config = {}) {
    this.cache = config.cache;
    this.config = {
      historyWindow: config.historyWindow || 3600000,
      predictionThreshold: config.predictionThreshold || 0.7,
      maxWarmItems: config.maxWarmItems || 100,
      ...config,
    };

    this.accessHistory = {};
    this.metrics = {
      predictions: 0,
      accurateHits: 0,
      warmedItems: 0,
      totalLatencySaved: 0,
    };
  }

  /**
   * Record cache access for pattern learning
   * @param {string} key - Cache key accessed
   * @param {number} latency - Access latency
   */
  recordAccess(key, latency) {
    const now = Date.now();
    
    if (!this.accessHistory[key]) {
      this.accessHistory[key] = {
        accesses: [],
        totalLatency: 0,
        patterns: {},
      };
    }

    const history = this.accessHistory[key];
    history.accesses.push(now);
    history.totalLatency += latency;

    // Keep only recent history
    history.accesses = history.accesses.filter(t => now - t < this.config.historyWindow);
  }

  /**
   * Predict which keys will be accessed soon
   * @returns {string[]} Predicted cache keys
   */
  predictNextAccesses() {
    const predictions = [];
    const now = Date.now();

    for (const [key, history] of Object.entries(this.accessHistory)) {
      const { accesses } = history;
      if (accesses.length < 2) continue;

      // Calculate access frequency and patterns
      const intervals = [];
      for (let i = 1; i < accesses.length; i++) {
        intervals.push(accesses[i] - accesses[i - 1]);
      }

      if (intervals.length === 0) continue;

      const stats = calculateStats(intervals);
      const avgInterval = stats.avg;
      const lastAccess = accesses[accesses.length - 1];
      const timeSinceLastAccess = now - lastAccess;

      // Confidence score based on consistency
      const variance = Math.sqrt(intervals.reduce((sum, x) => sum + Math.pow(x - avgInterval, 2), 0) / intervals.length);
      const consistency = Math.max(0, 1 - (variance / avgInterval));
      const confidence = consistency * (accesses.length / (this.config.historyWindow / avgInterval));

      if (confidence > this.config.predictionThreshold) {
        predictions.push({
          key,
          confidence: Math.min(1, confidence),
          priority: consistency,
          timeSinceAccess: timeSinceLastAccess,
        });
      }

      this.metrics.predictions++;
    }

    // Sort by confidence and return top predictions
    return predictions
      .sort((a, b) => b.confidence - a.confidence)
      .slice(0, this.config.maxWarmItems)
      .map(p => p.key);
  }

  /**
   * Warm cache with predicted entries
   * @async
   * @param {Function} fetchFn - Async function to fetch missing entries
   * @returns {Object} Warming results
   */
  async warmCache(fetchFn) {
    const predictions = this.predictNextAccesses();
    const results = {
      predicted: predictions.length,
      warmed: 0,
      failed: 0,
      errors: [],
    };

    for (const key of predictions) {
      try {
        const data = await fetchFn(key);
        if (data) {
          this.cache.dynamicCache?.set(key, data, 60);
          this.metrics.warmedItems++;
          results.warmed++;
        }
      } catch (error) {
        results.failed++;
        results.errors.push({ key, error: error.message });
      }
    }

    return results;
  }

  /**
   * Get prediction metrics
   * @returns {Object} Metrics summary
   */
  getMetrics() {
    return {
      totalPredictions: this.metrics.predictions,
      accurateHits: this.metrics.accurateHits,
      warmedItems: this.metrics.warmedItems,
      totalLatencySaved: `${this.metrics.totalLatencySaved}ms`,
      historySize: Object.keys(this.accessHistory).length,
      avgConfidence: this.metrics.predictions > 0
        ? ((this.metrics.accurateHits / this.metrics.predictions) * 100).toFixed(2)
        : 0,
    };
  }

  /**
   * Record that a prediction was accurate
   * @param {string} key - Cache key
   * @param {number} latencySaved - Latency saved in ms
   */
  recordHit(key, latencySaved) {
    this.metrics.accurateHits++;
    this.metrics.totalLatencySaved += latencySaved;
  }

  /**
   * Clear history (for testing or maintenance)
   */
  clearHistory() {
    this.accessHistory = {};
  }
}

module.exports = PredictiveCacheWarmer;
