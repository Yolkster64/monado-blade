/**
 * Alerting System Integration for HELIOS v4.0
 * Manages alerts, integrates with alert managers and notification systems
 * Performance Target: <5ms alert routing
 */

class AlertingIntegration {
  /**
   * @param {Object} config - Configuration
   * @param {string} config.provider - 'alertmanager' or 'custom' (default: 'memory')
   * @param {Function} config.notificationHandler - Custom handler for alerts
   * @param {number} config.deduplicationWindow - Dedupe window ms (default: 300000)
   */
  constructor(config = {}) {
    this.config = {
      provider: config.provider || 'memory',
      notificationHandler: config.notificationHandler,
      deduplicationWindow: config.deduplicationWindow || 300000,
      ...config,
    };

    this.alerts = [];
    this.activeAlerts = new Map();
    this.rules = new Map();
    this.metrics = {
      alertsFired: 0,
      alertsResolved: 0,
      alertsDeduped: 0,
    };
  }

  /**
   * Register alert rule
   * @param {string} ruleId - Rule ID
   * @param {Object} rule - Rule definition
   */
  registerRule(ruleId, rule) {
    this.rules.set(ruleId, {
      id: ruleId,
      name: rule.name,
      condition: rule.condition,
      severity: rule.severity || 'warning',
      enabled: rule.enabled !== false,
      ...rule,
    });
  }

  /**
   * Fire an alert
   * @param {Object} alert - Alert object
   * @returns {Object} Alert with ID
   */
  fireAlert(alert) {
    const alertId = this._generateAlertId();
    const now = Date.now();

    // Check for duplicates
    const existing = Array.from(this.activeAlerts.values()).find(
      a => a.ruleId === alert.ruleId && now - a.timestamp < this.config.deduplicationWindow
    );

    if (existing) {
      this.metrics.alertsDeduped++;
      return { ...alert, alertId, deduplicated: true };
    }

    const alertRecord = {
      alertId,
      ...alert,
      timestamp: now,
      status: 'firing',
      acknowledged: false,
    };

    this.activeAlerts.set(alertId, alertRecord);
    this.alerts.push(alertRecord);
    this.metrics.alertsFired++;

    // Call notification handler if configured
    if (this.config.notificationHandler) {
      try {
        this.config.notificationHandler(alertRecord);
      } catch (error) {
        console.error('Notification handler error:', error.message);
      }
    }

    return alertRecord;
  }

  /**
   * Resolve an alert
   * @param {string} alertId - Alert ID
   * @param {string} reason - Resolution reason
   */
  resolveAlert(alertId, reason = 'auto-resolved') {
    const alert = this.activeAlerts.get(alertId);
    if (!alert) return null;

    alert.status = 'resolved';
    alert.resolvedAt = Date.now();
    alert.resolutionReason = reason;

    this.activeAlerts.delete(alertId);
    this.metrics.alertsResolved++;

    return alert;
  }

  /**
   * Acknowledge an alert
   * @param {string} alertId - Alert ID
   * @param {string} acknowledgedBy - User who acknowledged
   */
  acknowledgeAlert(alertId, acknowledgedBy) {
    const alert = this.activeAlerts.get(alertId);
    if (!alert) return null;

    alert.acknowledged = true;
    alert.acknowledgedAt = Date.now();
    alert.acknowledgedBy = acknowledgedBy;

    return alert;
  }

  /**
   * Get active alerts
   * @param {Object} filter - Filter options
   * @returns {Array} Active alerts
   */
  getActiveAlerts(filter = {}) {
    let alerts = Array.from(this.activeAlerts.values());

    if (filter.severity) {
      alerts = alerts.filter(a => a.severity === filter.severity);
    }

    if (filter.ruleId) {
      alerts = alerts.filter(a => a.ruleId === filter.ruleId);
    }

    if (filter.acknowledged !== undefined) {
      alerts = alerts.filter(a => a.acknowledged === filter.acknowledged);
    }

    return alerts;
  }

  /**
   * Get alert history
   * @param {Object} options - Query options
   * @returns {Array} Alert history
   */
  getHistory(options = {}) {
    const { limit = 50, ruleId = null } = options;
    
    let history = [...this.alerts];

    if (ruleId) {
      history = history.filter(a => a.ruleId === ruleId);
    }

    return history.slice(-limit);
  }

  /**
   * Evaluate a condition and fire alert if triggered
   * @param {string} ruleId - Rule ID
   * @param {*} value - Value to evaluate
   * @returns {Object|null} Fired alert or null
   */
  evaluateRule(ruleId, value) {
    const rule = this.rules.get(ruleId);
    if (!rule || !rule.enabled) return null;

    let conditionMet = false;

    // Support simple operators
    if (typeof rule.condition === 'object') {
      if (rule.condition.gt !== undefined && value > rule.condition.gt) {
        conditionMet = true;
      } else if (rule.condition.lt !== undefined && value < rule.condition.lt) {
        conditionMet = true;
      } else if (rule.condition.eq !== undefined && value === rule.condition.eq) {
        conditionMet = true;
      }
    } else if (typeof rule.condition === 'function') {
      conditionMet = rule.condition(value);
    }

    if (conditionMet) {
      return this.fireAlert({
        ruleId,
        ruleName: rule.name,
        severity: rule.severity,
        value,
        message: rule.message || `Alert: ${rule.name}`,
      });
    }

    return null;
  }

  /**
   * Get alerting metrics
   * @returns {Object} Metrics
   */
  getMetrics() {
    return {
      alertsFired: this.metrics.alertsFired,
      alertsResolved: this.metrics.alertsResolved,
      alertsDeduped: this.metrics.alertsDeduped,
      activeAlerts: this.activeAlerts.size,
      registeredRules: this.rules.size,
      avgTimeToResolve: this._calculateAvgResolveTime(),
    };
  }

  /**
   * Clear resolved alerts
   * @param {number} maxAge - Max age in ms (default: 24 hours)
   */
  cleanup(maxAge = 86400000) {
    const now = Date.now();
    this.alerts = this.alerts.filter(a => {
      if (a.status === 'resolved') {
        return now - a.resolvedAt < maxAge;
      }
      return true;
    });
  }

  /**
   * Internal: Generate alert ID
   * @private
   */
  _generateAlertId() {
    return `alert_${Date.now()}_${Math.random().toString(36).substr(2, 9)}`;
  }

  /**
   * Internal: Calculate average time to resolve
   * @private
   */
  _calculateAvgResolveTime() {
    const resolved = this.alerts.filter(a => a.status === 'resolved' && a.resolvedAt);
    if (resolved.length === 0) return 0;

    const times = resolved.map(a => a.resolvedAt - a.timestamp);
    const avg = times.reduce((a, b) => a + b, 0) / times.length;
    return `${(avg / 1000).toFixed(2)}s`;
  }
}

module.exports = AlertingIntegration;
