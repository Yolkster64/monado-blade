/**
 * Metrics Aggregator Utility for HELIOS v4.0
 * Shared metrics calculation and reporting logic
 * Used by: cache, db, gateway, monitoring
 */

/**
 * Calculate percentile from array of values
 * @param {number[]} values - Array of numeric values
 * @param {number} percentile - Percentile (0-100)
 * @returns {number} Percentile value
 */
function calculatePercentile(values, percentile) {
  if (values.length === 0) return 0;
  
  const sorted = [...values].sort((a, b) => a - b);
  const index = Math.ceil((percentile / 100) * sorted.length) - 1;
  return sorted[Math.max(0, index)];
}

/**
 * Calculate basic statistics
 * @param {number[]} values - Array of values
 * @returns {Object} Statistics object
 */
function calculateStats(values) {
  if (values.length === 0) {
    return { min: 0, max: 0, avg: 0, p50: 0, p95: 0, p99: 0, count: 0 };
  }

  const sorted = [...values].sort((a, b) => a - b);
  const sum = values.reduce((a, b) => a + b, 0);
  const avg = sum / values.length;

  return {
    min: sorted[0],
    max: sorted[sorted.length - 1],
    avg: Number(avg.toFixed(2)),
    p50: calculatePercentile(values, 50),
    p95: calculatePercentile(values, 95),
    p99: calculatePercentile(values, 99),
    count: values.length,
  };
}

/**
 * Calculate rate (percentage)
 * @param {number} hits - Hit count
 * @param {number} total - Total count
 * @returns {string} Formatted rate percentage
 */
function calculateRate(hits, total) {
  if (total === 0) return '0.00%';
  return `${((hits / total) * 100).toFixed(2)}%`;
}

/**
 * Format bytes to human-readable format
 * @param {number} bytes - Number of bytes
 * @returns {string} Formatted size
 */
function formatBytes(bytes) {
  if (bytes === 0) return '0 B';
  const sizes = ['B', 'KB', 'MB', 'GB'];
  const i = Math.floor(Math.log(bytes) / Math.log(1024));
  return `${(bytes / Math.pow(1024, i)).toFixed(2)} ${sizes[i]}`;
}

/**
 * Merge metrics objects recursively
 * @param {...Object} objects - Objects to merge
 * @returns {Object} Merged object
 */
function mergeMetrics(...objects) {
  const result = {};
  
  for (const obj of objects) {
    for (const key in obj) {
      if (typeof obj[key] === 'object' && obj[key] !== null && !Array.isArray(obj[key])) {
        result[key] = mergeMetrics(result[key] || {}, obj[key]);
      } else {
        result[key] = obj[key];
      }
    }
  }
  
  return result;
}

module.exports = {
  calculatePercentile,
  calculateStats,
  calculateRate,
  formatBytes,
  mergeMetrics,
};
