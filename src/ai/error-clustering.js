/**
 * Error Clustering for HELIOS v4.0
 * Automatically groups similar errors for better debugging and alerting
 * Performance Target: 90% accuracy
 */

const { calculateStats } = require('../utils/metrics');

class ErrorClustering {
  /**
   * @param {Object} config - Configuration
   * @param {number} config.similarityThreshold - 0-1, default 0.75
   * @param {number} config.maxClusters - Maximum clusters (default: 50)
   * @param {number} config.cleanupWindow - Milliseconds before cleanup (default: 3600000)
   */
  constructor(config = {}) {
    this.config = {
      similarityThreshold: config.similarityThreshold || 0.75,
      maxClusters: config.maxClusters || 50,
      cleanupWindow: config.cleanupWindow || 3600000,
      ...config,
    };

    this.clusters = [];
    this.metrics = {
      errorsProcessed: 0,
      clustersCreated: 0,
      duplicatesDetected: 0,
    };
  }

  /**
   * Process error and add to cluster
   * @param {Error|Object} error - Error to process
   * @param {Object} context - Error context
   */
  processError(error, context = {}) {
    const now = Date.now();
    
    const errorRecord = {
      timestamp: now,
      message: typeof error === 'string' ? error : error.message,
      stack: error.stack || '',
      code: error.code || 'UNKNOWN',
      context,
      fingerprint: this._generateFingerprint(error, context),
    };

    this.metrics.errorsProcessed++;

    // Find matching cluster
    let matchedCluster = this._findMatchingCluster(errorRecord);

    if (matchedCluster) {
      matchedCluster.errors.push(errorRecord);
      matchedCluster.lastSeen = now;
      matchedCluster.count++;
      this.metrics.duplicatesDetected++;
    } else {
      // Create new cluster
      const newCluster = {
        id: this._generateClusterId(),
        pattern: errorRecord.message,
        code: errorRecord.code,
        errors: [errorRecord],
        created: now,
        lastSeen: now,
        count: 1,
        fingerprints: new Set([errorRecord.fingerprint]),
      };

      this.clusters.push(newCluster);
      this.metrics.clustersCreated++;

      // Enforce max clusters
      if (this.clusters.length > this.config.maxClusters) {
        this.clusters.sort((a, b) => b.lastSeen - a.lastSeen);
        this.clusters = this.clusters.slice(0, this.config.maxClusters);
      }
    }

    // Cleanup old data
    this._cleanup(now);
  }

  /**
   * Get all clusters
   * @param {Object} options - Options
   * @returns {Array} Clusters
   */
  getClusters(options = {}) {
    const { sort = 'count', limit = 20, activeOnly = false } = options;

    let clusters = [...this.clusters];

    if (activeOnly) {
      const now = Date.now();
      clusters = clusters.filter(c => now - c.lastSeen < this.config.cleanupWindow);
    }

    clusters.sort((a, b) => {
      if (sort === 'count') return b.count - a.count;
      if (sort === 'recent') return b.lastSeen - a.lastSeen;
      return b.count - a.count;
    });

    return clusters.slice(0, limit).map(c => ({
      id: c.id,
      pattern: c.pattern,
      code: c.code,
      count: c.count,
      created: new Date(c.created).toISOString(),
      lastSeen: new Date(c.lastSeen).toISOString(),
      firstError: c.errors[0],
      latestError: c.errors[c.errors.length - 1],
    }));
  }

  /**
   * Get detailed cluster info
   * @param {string} clusterId - Cluster ID
   * @returns {Object} Cluster details
   */
  getClusterDetails(clusterId) {
    const cluster = this.clusters.find(c => c.id === clusterId);
    if (!cluster) return null;

    // Analyze error frequency
    const timestamps = cluster.errors.map(e => e.timestamp);
    const intervals = [];
    for (let i = 1; i < timestamps.length; i++) {
      intervals.push(timestamps[i] - timestamps[i - 1]);
    }

    const stats = calculateStats(intervals);

    return {
      id: cluster.id,
      pattern: cluster.pattern,
      code: cluster.code,
      count: cluster.count,
      created: new Date(cluster.created).toISOString(),
      lastSeen: new Date(cluster.lastSeen).toISOString(),
      frequency: {
        avgInterval: `${(stats.avg / 1000).toFixed(1)}s`,
        minInterval: `${(stats.min / 1000).toFixed(1)}s`,
        maxInterval: `${(stats.max / 1000).toFixed(1)}s`,
        p95Interval: `${(stats.p95 / 1000).toFixed(1)}s`,
      },
      contexts: [...new Set(cluster.errors.map(e => JSON.stringify(e.context)))].slice(0, 5),
      samples: cluster.errors.slice(-5),
    };
  }

  /**
   * Get clustering metrics
   * @returns {Object} Metrics
   */
  getMetrics() {
    return {
      errorsProcessed: this.metrics.errorsProcessed,
      clustersCreated: this.metrics.clustersCreated,
      duplicatesDetected: this.metrics.duplicatesDetected,
      activeClusters: this.clusters.length,
      averageClusterSize: this.clusters.length > 0
        ? (this.metrics.errorsProcessed / this.clusters.length).toFixed(1)
        : 0,
      clusteringEfficiency: this.metrics.errorsProcessed > 0
        ? ((this.metrics.duplicatesDetected / this.metrics.errorsProcessed) * 100).toFixed(2)
        : 0,
    };
  }

  /**
   * Clear all clusters
   */
  clear() {
    this.clusters = [];
  }

  /**
   * Internal: Generate error fingerprint
   * @private
   */
  _generateFingerprint(error, context) {
    const message = typeof error === 'string' ? error : error.message;
    const code = error.code || 'UNKNOWN';
    
    // Extract line from stack trace if available
    let stackLine = '';
    if (error.stack) {
      const lines = error.stack.split('\n');
      if (lines.length > 1) {
        stackLine = lines[1].trim();
      }
    }

    return `${code}:${message}:${stackLine}`;
  }

  /**
   * Internal: Generate cluster ID
   * @private
   */
  _generateClusterId() {
    return `cluster_${Date.now()}_${Math.random().toString(36).substr(2, 9)}`;
  }

  /**
   * Internal: Find matching cluster for error
   * @private
   */
  _findMatchingCluster(errorRecord) {
    for (const cluster of this.clusters) {
      const similarity = this._calculateSimilarity(errorRecord.message, cluster.pattern);
      
      if (similarity > this.config.similarityThreshold && errorRecord.code === cluster.code) {
        return cluster;
      }
    }
    return null;
  }

  /**
   * Internal: Calculate string similarity (Levenshtein-like)
   * @private
   */
  _calculateSimilarity(str1, str2) {
    if (str1 === str2) return 1;
    
    const longer = str1.length > str2.length ? str1 : str2;
    const shorter = str1.length > str2.length ? str2 : str1;
    
    if (longer.length === 0) return 1;

    const editDistance = this._levenshteinDistance(longer, shorter);
    return (longer.length - editDistance) / longer.length;
  }

  /**
   * Internal: Levenshtein distance
   * @private
   */
  _levenshteinDistance(str1, str2) {
    const matrix = [];

    for (let i = 0; i <= str2.length; i++) {
      matrix[i] = [i];
    }

    for (let j = 0; j <= str1.length; j++) {
      matrix[0][j] = j;
    }

    for (let i = 1; i <= str2.length; i++) {
      for (let j = 1; j <= str1.length; j++) {
        if (str2.charAt(i - 1) === str1.charAt(j - 1)) {
          matrix[i][j] = matrix[i - 1][j - 1];
        } else {
          matrix[i][j] = Math.min(
            matrix[i - 1][j - 1] + 1,
            matrix[i][j - 1] + 1,
            matrix[i - 1][j] + 1
          );
        }
      }
    }

    return matrix[str2.length][str1.length];
  }

  /**
   * Internal: Cleanup old clusters and errors
   * @private
   */
  _cleanup(now) {
    // Remove old errors from clusters
    this.clusters.forEach(cluster => {
      cluster.errors = cluster.errors.filter(e => now - e.timestamp < this.config.cleanupWindow);
    });

    // Remove empty clusters
    this.clusters = this.clusters.filter(c => c.errors.length > 0);
  }
}

module.exports = ErrorClustering;
