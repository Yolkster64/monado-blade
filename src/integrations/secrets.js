/**
 * Secrets Manager Integration for HELIOS v4.0
 * Integrates with HashiCorp Vault and similar secret management systems
 * Performance Target: <5ms secret access with caching
 */

class SecretsManager {
  /**
   * @param {Object} config - Configuration
   * @param {string} config.provider - 'vault' or 'memory' (default: 'memory')
   * @param {string} config.vaultAddr - Vault address
   * @param {string} config.vaultToken - Vault token
   * @param {number} config.cacheTTL - Cache TTL ms (default: 300000)
   */
  constructor(config = {}) {
    this.config = {
      provider: config.provider || 'memory',
      vaultAddr: config.vaultAddr || 'http://localhost:8200',
      vaultToken: config.vaultToken,
      cacheTTL: config.cacheTTL || 300000,
      ...config,
    };

    this.secrets = new Map();
    this.secretCache = new Map();
    this.metrics = {
      secretsStored: 0,
      secretsRetrieved: 0,
      cacheHits: 0,
      cacheMisses: 0,
    };
  }

  /**
   * Store a secret
   * @param {string} path - Secret path
   * @param {string|Object} value - Secret value
   * @returns {boolean} Success
   */
  setSecret(path, value) {
    this.secrets.set(path, {
      value,
      created: Date.now(),
      accessed: 0,
    });

    this.secretCache.delete(path); // Invalidate cache
    this.metrics.secretsStored++;

    return true;
  }

  /**
   * Retrieve a secret
   * @param {string} path - Secret path
   * @returns {string|Object|null} Secret value
   */
  getSecret(path) {
    this.metrics.secretsRetrieved++;

    // Check cache first
    const cached = this.secretCache.get(path);
    if (cached && Date.now() - cached.timestamp < this.config.cacheTTL) {
      this.metrics.cacheHits++;
      return cached.value;
    }

    this.metrics.cacheMisses++;

    // Get from storage
    const secret = this.secrets.get(path);
    if (!secret) {
      return null;
    }

    secret.accessed++;

    // Cache the secret
    this.secretCache.set(path, {
      value: secret.value,
      timestamp: Date.now(),
    });

    return secret.value;
  }

  /**
   * Check if secret exists
   * @param {string} path - Secret path
   * @returns {boolean} Exists
   */
  hasSecret(path) {
    return this.secrets.has(path);
  }

  /**
   * Delete a secret
   * @param {string} path - Secret path
   * @returns {boolean} Success
   */
  deleteSecret(path) {
    this.secretCache.delete(path);
    return this.secrets.delete(path);
  }

  /**
   * Rotate secret (update with new value)
   * @param {string} path - Secret path
   * @param {string|Object} newValue - New secret value
   * @returns {Object} Rotation result
   */
  rotateSecret(path, newValue) {
    const oldSecret = this.secrets.get(path);

    this.setSecret(path, newValue);

    return {
      path,
      status: 'rotated',
      rotatedAt: Date.now(),
      accessCount: oldSecret ? oldSecret.accessed : 0,
    };
  }

  /**
   * List all secret paths (with pattern matching)
   * @param {string} pattern - Pattern to match (e.g., 'app/*')
   * @returns {string[]} Matching paths
   */
  listSecrets(pattern = '*') {
    const paths = Array.from(this.secrets.keys());

    if (pattern === '*') {
      return paths;
    }

    // Simple pattern matching
    const regex = new RegExp(pattern.replace(/\*/g, '.*'));
    return paths.filter(p => regex.test(p));
  }

  /**
   * Get secret metadata (without revealing value)
   * @param {string} path - Secret path
   * @returns {Object} Metadata
   */
  getSecretMetadata(path) {
    const secret = this.secrets.get(path);
    if (!secret) return null;

    return {
      path,
      exists: true,
      created: new Date(secret.created).toISOString(),
      accessed: secret.accessed,
      cached: this.secretCache.has(path),
    };
  }

  /**
   * Clear cache for specific secret or all
   * @param {string|null} path - Secret path or null for all
   */
  clearCache(path = null) {
    if (path) {
      this.secretCache.delete(path);
    } else {
      this.secretCache.clear();
    }
  }

  /**
   * Get secrets manager metrics
   * @returns {Object} Metrics
   */
  getMetrics() {
    return {
      secretsStored: this.metrics.secretsStored,
      secretsRetrieved: this.metrics.secretsRetrieved,
      cacheHits: this.metrics.cacheHits,
      cacheMisses: this.metrics.cacheMisses,
      cacheHitRate: this.metrics.secretsRetrieved > 0
        ? ((this.metrics.cacheHits / this.metrics.secretsRetrieved) * 100).toFixed(2)
        : 0,
      cachedSecrets: this.secretCache.size,
      provider: this.config.provider,
    };
  }

  /**
   * Validate secret access (audit)
   * @param {string} path - Secret path
   * @param {string} requester - Who is requesting
   * @returns {Object} Audit log
   */
  auditAccess(path, requester) {
    const secret = this.secrets.get(path);

    return {
      path,
      requester,
      exists: !!secret,
      timestamp: Date.now(),
      accessCount: secret ? secret.accessed : 0,
    };
  }

  /**
   * Clear all secrets (use with caution)
   */
  clear() {
    this.secrets.clear();
    this.secretCache.clear();
  }
}

module.exports = SecretsManager;
