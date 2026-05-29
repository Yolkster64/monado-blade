/**
 * Dynamic Configuration Management for HELIOS v4.0
 * Manages runtime configuration with hot-reload and validation
 * Performance Target: <1ms config access
 */

class ConfigManagement {
  /**
   * @param {Object} config - Initial configuration
   */
  constructor(config = {}) {
    this.config = { ...config };
    this.defaults = { ...config };
    this.overrides = new Map();
    this.validators = new Map();
    this.watchers = [];
    this.metrics = {
      configReads: 0,
      configWrites: 0,
      validationFailures: 0,
    };
  }

  /**
   * Get configuration value
   * @param {string} key - Configuration key (dot-notation supported)
   * @param {*} defaultValue - Default if not found
   * @returns {*} Configuration value
   */
  get(key, defaultValue = undefined) {
    this.metrics.configReads++;

    // Check overrides first
    if (this.overrides.has(key)) {
      return this.overrides.get(key);
    }

    // Support dot notation
    if (key.includes('.')) {
      return this._getNestedValue(this.config, key, defaultValue);
    }

    return this.config[key] !== undefined ? this.config[key] : defaultValue;
  }

  /**
   * Set configuration value
   * @param {string} key - Configuration key (dot-notation supported)
   * @param {*} value - Value to set
   * @returns {boolean} Success
   */
  set(key, value) {
    this.metrics.configWrites++;

    // Validate if validator exists
    if (this.validators.has(key)) {
      const validator = this.validators.get(key);
      if (!validator(value)) {
        this.metrics.validationFailures++;
        return false;
      }
    }

    const oldValue = this.get(key);
    this.overrides.set(key, value);

    // Notify watchers
    this._notifyWatchers(key, value, oldValue);

    return true;
  }

  /**
   * Set multiple config values
   * @param {Object} updates - Updates object
   * @returns {Object} Results
   */
  setMultiple(updates) {
    const results = {};

    for (const [key, value] of Object.entries(updates)) {
      results[key] = this.set(key, value);
    }

    return results;
  }

  /**
   * Register validator for config key
   * @param {string} key - Configuration key
   * @param {Function} validator - Validation function
   */
  registerValidator(key, validator) {
    this.validators.set(key, validator);
  }

  /**
   * Watch for config changes
   * @param {string} key - Configuration key
   * @param {Function} callback - Callback function
   * @returns {Function} Unwatch function
   */
  watch(key, callback) {
    const watcher = { key, callback };
    this.watchers.push(watcher);

    // Return unwatch function
    return () => {
      const index = this.watchers.indexOf(watcher);
      if (index > -1) {
        this.watchers.splice(index, 1);
      }
    };
  }

  /**
   * Get all current configuration
   * @returns {Object} Full config
   */
  getAll() {
    const result = { ...this.config };

    for (const [key, value] of this.overrides.entries()) {
      if (key.includes('.')) {
        this._setNestedValue(result, key, value);
      } else {
        result[key] = value;
      }
    }

    return result;
  }

  /**
   * Reset to defaults
   * @param {string|null} key - Specific key or null for all
   */
  reset(key = null) {
    if (key) {
      this.overrides.delete(key);
    } else {
      this.overrides.clear();
    }
  }

  /**
   * Export configuration (excluding sensitive values)
   * @param {string[]} excludeKeys - Keys to exclude
   * @returns {Object} Exportable config
   */
  export(excludeKeys = []) {
    const config = this.getAll();
    const result = {};

    for (const [key, value] of Object.entries(config)) {
      if (!excludeKeys.includes(key)) {
        result[key] = value;
      }
    }

    return result;
  }

  /**
   * Import configuration
   * @param {Object} config - Configuration to import
   * @returns {Object} Import results
   */
  import(config) {
    return this.setMultiple(config);
  }

  /**
   * Get configuration metrics
   * @returns {Object} Metrics
   */
  getMetrics() {
    return {
      configReads: this.metrics.configReads,
      configWrites: this.metrics.configWrites,
      validationFailures: this.metrics.validationFailures,
      overridesActive: this.overrides.size,
      watchersRegistered: this.watchers.length,
    };
  }

  /**
   * Internal: Get nested value with dot notation
   * @private
   */
  _getNestedValue(obj, path, defaultValue) {
    const keys = path.split('.');
    let current = obj;

    for (const key of keys) {
      if (current && typeof current === 'object' && key in current) {
        current = current[key];
      } else {
        return defaultValue;
      }
    }

    return current;
  }

  /**
   * Internal: Set nested value with dot notation
   * @private
   */
  _setNestedValue(obj, path, value) {
    const keys = path.split('.');
    let current = obj;

    for (let i = 0; i < keys.length - 1; i++) {
      const key = keys[i];
      if (!(key in current)) {
        current[key] = {};
      }
      current = current[key];
    }

    current[keys[keys.length - 1]] = value;
  }

  /**
   * Internal: Notify watchers of changes
   * @private
   */
  _notifyWatchers(key, newValue, oldValue) {
    for (const watcher of this.watchers) {
      if (watcher.key === key || watcher.key === '*') {
        try {
          watcher.callback(newValue, oldValue, key);
        } catch (error) {
          console.error('Watcher error:', error.message);
        }
      }
    }
  }
}

module.exports = ConfigManagement;
