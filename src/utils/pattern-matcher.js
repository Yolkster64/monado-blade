/**
 * Pattern Matcher Utility for HELIOS v4.0
 * Shared pattern matching logic for cache invalidation, filtering, and notifications
 * Used by: cache manager, monitoring, integrations
 */

/**
 * Converts glob pattern to regex
 * @param {string} pattern - Glob pattern (e.g., 'user:*', 'analytics:*:data')
 * @returns {RegExp} Compiled regex pattern
 */
function patternToRegex(pattern) {
  if (pattern === '*') return /.*?/;
  
  const escaped = pattern
    .replace(/\./g, '\\.')
    .replace(/\*/g, '[^:]*');
  
  return new RegExp(`^${escaped}$`);
}

/**
 * Test if key matches pattern
 * @param {string} key - Key to test
 * @param {string} pattern - Glob pattern
 * @returns {boolean} Whether key matches pattern
 */
function matches(key, pattern) {
  const regex = patternToRegex(pattern);
  return regex.test(key);
}

/**
 * Filter array of keys by pattern
 * @param {string[]} keys - Array of keys
 * @param {string|string[]} patterns - Pattern(s) to match
 * @returns {string[]} Filtered keys
 */
function filterByPattern(keys, patterns) {
  const patternList = Array.isArray(patterns) ? patterns : [patterns];
  return keys.filter(key =>
    patternList.some(pattern => matches(key, pattern))
  );
}

module.exports = {
  patternToRegex,
  matches,
  filterByPattern,
};
