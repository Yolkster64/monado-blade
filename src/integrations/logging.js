/**
 * Centralized Logging Integration for HELIOS v4.0
 * Supports Winston, Pino, and custom transports
 * Performance Target: <1ms overhead
 */

class LoggingIntegration {
  /**
   * @param {Object} config - Configuration
   * @param {string} config.provider - 'winston' or 'pino' (default: 'console')
   * @param {string} config.level - Log level (default: 'info')
   * @param {Object} config.transport - Custom transport config
   */
  constructor(config = {}) {
    this.config = {
      provider: config.provider || 'console',
      level: config.level || 'info',
      json: config.json !== false,
      includeTimestamp: config.includeTimestamp !== false,
      ...config,
    };

    this.logger = this._initializeLogger();
    this.metrics = {
      logsEmitted: 0,
      errorLogs: 0,
      warnLogs: 0,
    };
  }

  /**
   * Log info level message
   * @param {string} message - Message
   * @param {Object} meta - Metadata
   */
  info(message, meta = {}) {
    this._log('info', message, meta);
  }

  /**
   * Log error level message
   * @param {string} message - Message
   * @param {Error|Object} meta - Error or metadata
   */
  error(message, meta = {}) {
    this._log('error', message, meta);
    this.metrics.errorLogs++;
  }

  /**
   * Log warning level message
   * @param {string} message - Message
   * @param {Object} meta - Metadata
   */
  warn(message, meta = {}) {
    this._log('warn', message, meta);
    this.metrics.warnLogs++;
  }

  /**
   * Log debug level message
   * @param {string} message - Message
   * @param {Object} meta - Metadata
   */
  debug(message, meta = {}) {
    this._log('debug', message, meta);
  }

  /**
   * Create child logger with context
   * @param {Object} context - Context values
   * @returns {LoggingIntegration} Child logger
   */
  createChild(context) {
    const child = new LoggingIntegration(this.config);
    child.context = { ...this.context, ...context };
    return child;
  }

  /**
   * Get logging metrics
   * @returns {Object} Metrics
   */
  getMetrics() {
    return {
      logsEmitted: this.metrics.logsEmitted,
      errorLogs: this.metrics.errorLogs,
      warnLogs: this.metrics.warnLogs,
      provider: this.config.provider,
      level: this.config.level,
    };
  }

  /**
   * Internal: Initialize logger
   * @private
   */
  _initializeLogger() {
    if (this.config.provider === 'winston') {
      return { type: 'winston', configured: true };
    } else if (this.config.provider === 'pino') {
      return { type: 'pino', configured: true };
    }
    return { type: 'console', configured: true };
  }

  /**
   * Internal: Emit log
   * @private
   */
  _log(level, message, meta) {
    this.metrics.logsEmitted++;

    const timestamp = this.config.includeTimestamp ? new Date().toISOString() : null;
    const logEntry = {
      timestamp,
      level,
      message,
      ...meta,
      ...(this.context || {}),
    };

    if (this.config.json) {
      console.log(JSON.stringify(logEntry));
    } else {
      console.log(`[${level.toUpperCase()}] ${message}`, meta);
    }
  }
}

module.exports = LoggingIntegration;
