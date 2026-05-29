/**
 * Request Traffic Predictor for HELIOS v4.0
 * Predicts incoming traffic patterns for capacity planning
 * Performance Target: ±15% accuracy
 */

const { calculateStats } = require('../utils/metrics');

class RequestPredictor {
  /**
   * @param {Object} config - Configuration
   * @param {number} config.historyWindow - Milliseconds of history (default: 86400000 = 24 hours)
   * @param {number} config.predictionWindow - Seconds ahead to predict (default: 300 = 5 minutes)
   */
  constructor(config = {}) {
    this.config = {
      historyWindow: config.historyWindow || 86400000,
      predictionWindow: config.predictionWindow || 300,
      minSamples: config.minSamples || 20,
      ...config,
    };

    this.requestHistory = [];
    this.metrics = {
      predictions: 0,
      accuratePredictions: 0,
      predictions_hourly: {},
      predictions_daily: {},
    };
  }

  /**
   * Record incoming request
   * @param {Object} request - Request info
   */
  recordRequest(request) {
    const now = Date.now();
    
    this.requestHistory.push({
      timestamp: now,
      endpoint: request.endpoint || 'unknown',
      method: request.method || 'GET',
      statusCode: request.statusCode || 200,
      latency: request.latency || 0,
      size: request.size || 0,
    });

    // Keep only recent history
    this.requestHistory = this.requestHistory.filter(r => now - r.timestamp < this.config.historyWindow);
  }

  /**
   * Predict next request volume
   * @returns {Object} Prediction
   */
  predictTraffic() {
    if (this.requestHistory.length < this.config.minSamples) {
      return { prediction: 'INSUFFICIENT_DATA', confidence: 0 };
    }

    const now = Date.now();
    const hour = new Date(now).getHours();
    const dayOfWeek = new Date(now).getDay();

    // Group by hour
    const hourlyBuckets = {};
    for (let i = 0; i < 24; i++) {
      hourlyBuckets[i] = [];
    }

    this.requestHistory.forEach(r => {
      const h = new Date(r.timestamp).getHours();
      hourlyBuckets[h].push(r);
    });

    // Get requests for current hour and similar hours
    const currentHourRequests = hourlyBuckets[hour].length;
    const nextHourIndex = (hour + 1) % 24;
    const nextHourRequests = hourlyBuckets[nextHourIndex].map(r => r.timestamp).length;

    // Calculate patterns
    const allCounts = Object.values(hourlyBuckets).map(b => b.length).filter(c => c > 0);
    const stats = calculateStats(allCounts);

    // Predict based on patterns
    let prediction = stats.avg;
    let confidence = 0.6;

    if (nextHourRequests > 0) {
      prediction = nextHourRequests;
      confidence = 0.8;
    }

    this.metrics.predictions++;

    return {
      prediction: Math.ceil(prediction),
      timeWindow: `next ${this.config.predictionWindow}s`,
      confidence,
      current: currentHourRequests,
      patterns: {
        hourlyAverage: Math.ceil(stats.avg),
        peakHour: hourlyBuckets[hour].length,
        trend: nextHourRequests > currentHourRequests ? 'INCREASING' : 'DECREASING',
      },
      recommendations: this._getRecommendations(prediction),
    };
  }

  /**
   * Predict traffic for specific endpoint
   * @param {string} endpoint - Endpoint path
   * @returns {Object} Prediction
   */
  predictEndpointTraffic(endpoint) {
    const endpointRequests = this.requestHistory.filter(r => r.endpoint === endpoint);
    
    if (endpointRequests.length < this.config.minSamples / 2) {
      return { prediction: 'INSUFFICIENT_DATA', endpoint, samples: endpointRequests.length };
    }

    const latencies = endpointRequests.map(r => r.latency);
    const stats = calculateStats(latencies);

    const count = endpointRequests.length;
    const avgRequestsPerSecond = (count / (this.config.historyWindow / 1000)).toFixed(2);

    return {
      endpoint,
      samples: count,
      avgRequestsPerSecond: parseFloat(avgRequestsPerSecond),
      latency: stats,
      errorRate: this._calculateErrorRate(endpointRequests),
      trafficTrend: this._calculateTrend(endpointRequests),
    };
  }

  /**
   * Record actual traffic (for accuracy measurement)
   * @param {number} actual - Actual request count
   * @param {number} predicted - Predicted request count
   */
  recordActual(actual, predicted) {
    const error = Math.abs(actual - predicted) / predicted;
    if (error < 0.15) {
      this.metrics.accuratePredictions++;
    }
  }

  /**
   * Get predictor metrics
   * @returns {Object} Metrics summary
   */
  getMetrics() {
    return {
      totalPredictions: this.metrics.predictions,
      accuratePredictions: this.metrics.accuratePredictions,
      accuracy: this.metrics.predictions > 0
        ? ((this.metrics.accuratePredictions / this.metrics.predictions) * 100).toFixed(2)
        : 0,
      historySize: this.requestHistory.length,
      timespan: `${(this.config.historyWindow / 3600000).toFixed(1)} hours`,
    };
  }

  /**
   * Clear history
   */
  clearHistory() {
    this.requestHistory = [];
  }

  /**
   * Internal: Get recommendations based on prediction
   * @private
   */
  _getRecommendations(prediction) {
    const thresholds = {
      high: prediction * 1.3,
      medium: prediction,
      low: prediction * 0.7,
    };

    return {
      scaleInstances: prediction > thresholds.high ? 'UP' : prediction < thresholds.low ? 'DOWN' : 'MAINTAIN',
      reserveCapacity: Math.ceil(prediction * 0.2),
      expectedP95Latency: '200-300ms',
    };
  }

  /**
   * Internal: Calculate error rate
   * @private
   */
  _calculateErrorRate(requests) {
    if (requests.length === 0) return 0;
    const errors = requests.filter(r => r.statusCode >= 400).length;
    return ((errors / requests.length) * 100).toFixed(2);
  }

  /**
   * Internal: Calculate traffic trend
   * @private
   */
  _calculateTrend(requests) {
    if (requests.length < 2) return 'STABLE';
    
    const midpoint = Math.floor(requests.length / 2);
    const firstHalf = requests.slice(0, midpoint).length;
    const secondHalf = requests.slice(midpoint).length;
    
    const change = (secondHalf - firstHalf) / firstHalf;
    
    if (change > 0.1) return 'INCREASING';
    if (change < -0.1) return 'DECREASING';
    return 'STABLE';
  }
}

module.exports = RequestPredictor;
