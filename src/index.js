/**
 * HELIOS v4.0 - Unified Entry Point & Dependency Injection Container
 * Production-ready performance optimization platform
 * 
 * Exports all public APIs and manages system initialization
 */

// Core modules
const CacheStrategy = require('./cache/cache-strategy');
const DatabaseOptimizer = require('./db/query-optimizer');
const ResponseOptimizer = require('./gateway/response-optimizer');
const PerformanceMonitor = require('./monitoring/perf-monitor');

// AI modules
const PredictiveCacheWarmer = require('./ai/predictive-cache-warmer');
const AutoScalingAdvisor = require('./ai/auto-scaling-advisor');
const AnomalyDetectorV2 = require('./ai/anomaly-v2');
const RequestPredictor = require('./ai/request-predictor');
const ErrorClustering = require('./ai/error-clustering');

// Integration modules
const LoggingIntegration = require('./integrations/logging');
const MetricsIntegration = require('./integrations/metrics');
const TracingIntegration = require('./integrations/tracing');
const AlertingIntegration = require('./integrations/alerting');
const WebhookManager = require('./integrations/webhooks');
const ConfigManagement = require('./integrations/config');
const SecretsManager = require('./integrations/secrets');
const HealthEndpoints = require('./integrations/health');

// Utilities
const { HeliosError, ErrorClassifier, classifyError, safeHandler } = require('./utils/error-handler');
const { patternToRegex, matches, filterByPattern } = require('./utils/pattern-matcher');
const { calculateStats, calculateRate, formatBytes, mergeMetrics } = require('./utils/metrics');

/**
 * HELIOS v4.0 - Main Platform Class
 * Unified dependency injection container and orchestration layer
 */
class HeliosV4 {
  /**
   * Initialize HELIOS v4.0 platform
   * @param {Object} config - Configuration
   */
  constructor(config = {}) {
    this.config = {
      version: '4.0.0',
      environment: config.environment || 'production',
      enableCache: config.enableCache !== false,
      enableDB: config.enableDB !== false,
      enableAI: config.enableAI !== false,
      enableIntegrations: config.enableIntegrations !== false,
      ...config,
    };

    // Core modules
    this.cache = new CacheStrategy(config.cache);
    this.db = new DatabaseOptimizer(config.db);
    this.gateway = new ResponseOptimizer(config.gateway);
    this.monitoring = new PerformanceMonitor(config.monitoring);

    // AI modules
    if (this.config.enableAI) {
      this.ai = {
        cacheWarmer: new PredictiveCacheWarmer({ cache: this.cache, ...config.ai }),
        scalingAdvisor: new AutoScalingAdvisor(config.scaling),
        anomalyDetector: new AnomalyDetectorV2(config.anomaly),
        trafficPredictor: new RequestPredictor(config.traffic),
        errorClustering: new ErrorClustering(config.errors),
      };
    }

    // Integration modules
    if (this.config.enableIntegrations) {
      this.integrations = {
        logging: new LoggingIntegration(config.logging),
        metrics: new MetricsIntegration(config.metrics),
        tracing: new TracingIntegration(config.tracing),
        alerting: new AlertingIntegration(config.alerting),
        webhooks: new WebhookManager(config.webhooks),
        config: new ConfigManagement(config.config),
        secrets: new SecretsManager(config.secrets),
        health: new HealthEndpoints(config.health),
      };
    }

    this.isInitialized = false;
  }

  /**
   * Initialize all systems
   * @async
   */
  async initialize() {
    try {
      this.integrations?.logging?.info('HELIOS v4.0 initializing...');

      // Register health components
      if (this.config.enableIntegrations) {
        this.integrations.health?.registerComponent('cache', async () => true);
        this.integrations.health?.registerComponent('database', async () => true);
        this.integrations.health?.registerComponent('monitoring', async () => true);
      }

      // Register default alert rules
      if (this.integrations?.alerting) {
        this.integrations.alerting.registerRule('cache_miss_rate', {
          name: 'High Cache Miss Rate',
          severity: 'warning',
          condition: { gt: 30 },
        });

        this.integrations.alerting.registerRule('api_latency', {
          name: 'API Latency High',
          severity: 'critical',
          condition: { gt: 500 },
        });
      }

      this.isInitialized = true;
      this.integrations?.logging?.info('HELIOS v4.0 initialized successfully');

      return {
        status: 'initialized',
        version: this.config.version,
        timestamp: Date.now(),
      };
    } catch (error) {
      const classified = classifyError(error, 'initialization');
      this.integrations?.logging?.error('Initialization failed', classified);
      throw new HeliosError('Initialization failed', classified.code, classified.statusCode);
    }
  }

  /**
   * Graceful shutdown
   * @async
   */
  async shutdown() {
    try {
      this.integrations?.logging?.info('HELIOS v4.0 shutting down...');

      // Flush pending operations
      if (this.integrations?.metrics) {
        await this.integrations.metrics.export();
        this.integrations.metrics.shutdown();
      }

      if (this.integrations?.webhooks) {
        await this.integrations.webhooks.processQueue(100);
      }

      if (this.integrations?.tracing) {
        await this.integrations.tracing.export();
      }

      this.isInitialized = false;
      this.integrations?.logging?.info('HELIOS v4.0 shutdown complete');

      return { status: 'shutdown', timestamp: Date.now() };
    } catch (error) {
      console.error('Shutdown error:', error.message);
      throw error;
    }
  }

  /**
   * Get comprehensive system status
   * @async
   * @returns {Object} System status report
   */
  async getStatus() {
    const status = {
      version: this.config.version,
      initialized: this.isInitialized,
      timestamp: Date.now(),
      modules: {},
      metrics: {},
    };

    // Core modules
    status.modules.cache = { enabled: this.config.enableCache, metrics: this.cache?.getStatistics?.() };
    status.modules.database = { enabled: this.config.enableDB, metrics: this.db?.getIndexMetrics?.() };
    status.modules.gateway = { enabled: true, metrics: this.gateway?.getMetrics?.() };
    status.modules.monitoring = { enabled: true, metrics: this.monitoring?.getMetrics?.() };

    // AI modules
    if (this.config.enableAI) {
      status.modules.ai = {
        cacheWarmer: this.ai.cacheWarmer?.getMetrics?.(),
        scalingAdvisor: this.ai.scalingAdvisor?.getMetrics?.(),
        anomalyDetector: this.ai.anomalyDetector?.getMetrics?.(),
        trafficPredictor: this.ai.trafficPredictor?.getMetrics?.(),
        errorClustering: this.ai.errorClustering?.getMetrics?.(),
      };
    }

    // Integration modules
    if (this.config.enableIntegrations) {
      status.integrations = {
        logging: this.integrations.logging?.getMetrics?.(),
        metrics: this.integrations.metrics?.getMetrics?.(),
        tracing: this.integrations.tracing?.getMetrics?.(),
        alerting: this.integrations.alerting?.getMetrics?.(),
        webhooks: this.integrations.webhooks?.getMetrics?.(),
        config: this.integrations.config?.getMetrics?.(),
        secrets: this.integrations.secrets?.getMetrics?.(),
        health: this.integrations.health?.getMetrics?.(),
      };

      // Health check
      status.health = await this.integrations.health?.health?.();
    }

    return status;
  }

  /**
   * Get all available APIs for documentation
   * @returns {Object} API reference
   */
  getAPIs() {
    return {
      cache: {
        cacheFirst: 'async (key, fetchFn, options) - Cache-first strategy',
        staleWhileRevalidate: 'async (key, fetchFn, options) - Serve stale, revalidate async',
        invalidate: '(pattern, namespace) - Invalidate cache entries',
        getStatistics: '() - Get cache hit rate and metrics',
      },
      database: {
        executeQuery: 'async (query, params, options) - Execute cached query',
        batchQuery: 'async (table, ids, relationships) - Batch query with joins',
        getIndexMetrics: '() - Get query performance metrics',
      },
      gateway: {
        compressResponse: 'async (data, headers) - Gzip compress response',
        paginate: '(data, query) - Paginate large datasets',
        selectFields: '(data, fields) - Project specific fields',
        buildOptimizedResponse: 'async (data, options) - Build fully optimized response',
      },
      ai: {
        cacheWarmer: { predictNextAccesses: '() - Predict cache needs', warmCache: 'async (fetchFn)' },
        scalingAdvisor: { analyze: '(currentCapacity) - Recommend scaling' },
        anomalyDetector: { detectAnomaly: '(metric, value) - Detect anomalies' },
        trafficPredictor: { predictTraffic: '() - Predict traffic' },
        errorClustering: { processError: '(error, context) - Group similar errors' },
      },
      integrations: {
        logging: { info: '(msg, meta)', error: '(msg, meta)', warn: '(msg, meta)' },
        metrics: { incrementCounter: '(name, value, tags)', setGauge: '(name, value, tags)' },
        tracing: { startTrace: '(traceId, op)', endTrace: '(traceId, status)' },
        alerting: { fireAlert: '(alert) - Fire alert', getActiveAlerts: '()' },
        webhooks: { dispatch: 'async (eventType, data)', processQueue: 'async (batchSize)' },
        config: { get: '(key)', set: '(key, value)', watch: '(key, callback)' },
        secrets: { getSecret: '(path)', setSecret: '(path, value)' },
        health: { readiness: 'async ()', liveness: 'async ()', health: 'async ()' },
      },
    };
  }
}

/**
 * Export all public APIs and utilities
 */
module.exports = {
  // Main platform
  HeliosV4,

  // Core modules
  CacheStrategy,
  DatabaseOptimizer,
  ResponseOptimizer,
  PerformanceMonitor,

  // AI modules
  PredictiveCacheWarmer,
  AutoScalingAdvisor,
  AnomalyDetectorV2,
  RequestPredictor,
  ErrorClustering,

  // Integration modules
  LoggingIntegration,
  MetricsIntegration,
  TracingIntegration,
  AlertingIntegration,
  WebhookManager,
  ConfigManagement,
  SecretsManager,
  HealthEndpoints,

  // Utilities
  HeliosError,
  ErrorClassifier,
  classifyError,
  safeHandler,
  patternToRegex,
  matches,
  filterByPattern,
  calculateStats,
  calculateRate,
  formatBytes,
  mergeMetrics,

  // Factory
  createHelios: (config) => new HeliosV4(config),
};
