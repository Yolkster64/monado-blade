/**
 * Health Endpoints Integration for HELIOS v4.0
 * Provides Kubernetes readiness/liveness probes and health checks
 * Performance Target: <5ms response time
 */

class HealthEndpoints {
  /**
   * @param {Object} config - Configuration
   * @param {Function} config.readinessCheck - Custom readiness checker
   * @param {Function} config.livenessCheck - Custom liveness checker
   */
  constructor(config = {}) {
    this.config = {
      readinessCheck: config.readinessCheck,
      livenessCheck: config.livenessCheck,
      ...config,
    };

    this.componentStatus = new Map();
    this.metrics = {
      healthChecksRun: 0,
      healthy: 0,
      unhealthy: 0,
    };
  }

  /**
   * Register health component
   * @param {string} name - Component name
   * @param {Function} checker - Async health check function
   */
  registerComponent(name, checker) {
    this.componentStatus.set(name, {
      name,
      checker,
      lastCheck: null,
      status: 'unknown',
    });
  }

  /**
   * Kubernetes readiness probe endpoint
   * Checks if service is ready to accept traffic
   * @async
   * @returns {Object} Readiness status
   */
  async readiness() {
    this.metrics.healthChecksRun++;

    const checks = [];
    let allReady = true;

    // Run component checks
    for (const [name, component] of this.componentStatus.entries()) {
      try {
        const ready = await component.checker();
        const status = { name, ready, checked: Date.now() };

        if (!ready) {
          allReady = false;
        }

        checks.push(status);
        component.status = ready ? 'ready' : 'not_ready';
        component.lastCheck = Date.now();
      } catch (error) {
        checks.push({
          name,
          ready: false,
          error: error.message,
          checked: Date.now(),
        });
        allReady = false;
        component.status = 'error';
      }
    }

    // Custom readiness check
    if (this.config.readinessCheck) {
      try {
        const customReady = await this.config.readinessCheck();
        if (!customReady) {
          allReady = false;
        }
      } catch (error) {
        allReady = false;
      }
    }

    if (allReady) {
      this.metrics.healthy++;
    } else {
      this.metrics.unhealthy++;
    }

    return {
      status: allReady ? 'ready' : 'not_ready',
      statusCode: allReady ? 200 : 503,
      timestamp: Date.now(),
      components: checks,
    };
  }

  /**
   * Kubernetes liveness probe endpoint
   * Checks if service is alive (can be restarted)
   * @async
   * @returns {Object} Liveness status
   */
  async liveness() {
    this.metrics.healthChecksRun++;

    try {
      if (this.config.livenessCheck) {
        const alive = await this.config.livenessCheck();
        this.metrics.healthy++;

        return {
          status: alive ? 'alive' : 'dead',
          statusCode: alive ? 200 : 503,
          timestamp: Date.now(),
        };
      }

      // Default: service is alive if it can respond
      this.metrics.healthy++;
      return {
        status: 'alive',
        statusCode: 200,
        timestamp: Date.now(),
      };
    } catch (error) {
      this.metrics.unhealthy++;
      return {
        status: 'dead',
        statusCode: 503,
        error: error.message,
        timestamp: Date.now(),
      };
    }
  }

  /**
   * Full health check endpoint
   * Returns comprehensive health information
   * @async
   * @returns {Object} Full health status
   */
  async health() {
    const readiness = await this.readiness();
    const liveness = await this.liveness();

    const components = [];
    for (const [name, component] of this.componentStatus.entries()) {
      components.push({
        name,
        status: component.status,
        lastCheck: component.lastCheck,
      });
    }

    return {
      status: readiness.status === 'ready' && liveness.status === 'alive' ? 'healthy' : 'unhealthy',
      statusCode: readiness.statusCode === 200 && liveness.statusCode === 200 ? 200 : 503,
      readiness,
      liveness,
      components,
      timestamp: Date.now(),
    };
  }

  /**
   * Startup probe endpoint (for slow-starting services)
   * @async
   * @returns {Object} Startup status
   */
  async startup() {
    try {
      const readiness = await this.readiness();
      return {
        status: readiness.status === 'ready' ? 'started' : 'starting',
        statusCode: readiness.status === 'ready' ? 200 : 503,
        timestamp: Date.now(),
      };
    } catch (error) {
      return {
        status: 'not_started',
        statusCode: 503,
        error: error.message,
        timestamp: Date.now(),
      };
    }
  }

  /**
   * Get health metrics
   * @returns {Object} Metrics
   */
  getMetrics() {
    return {
      healthChecksRun: this.metrics.healthChecksRun,
      healthy: this.metrics.healthy,
      unhealthy: this.metrics.unhealthy,
      healthRate: this.metrics.healthChecksRun > 0
        ? ((this.metrics.healthy / this.metrics.healthChecksRun) * 100).toFixed(2)
        : 0,
      registeredComponents: this.componentStatus.size,
    };
  }

  /**
   * Get component status
   * @param {string} name - Component name
   * @returns {Object} Component status
   */
  getComponentStatus(name) {
    const component = this.componentStatus.get(name);
    if (!component) return null;

    return {
      name: component.name,
      status: component.status,
      lastCheck: component.lastCheck,
    };
  }

  /**
   * Get all components status
   * @returns {Array} All components
   */
  getAllComponents() {
    return Array.from(this.componentStatus.values()).map(c => ({
      name: c.name,
      status: c.status,
      lastCheck: c.lastCheck,
    }));
  }
}

module.exports = HealthEndpoints;
