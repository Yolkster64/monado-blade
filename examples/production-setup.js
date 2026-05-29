/**
 * Production Setup Example
 * Complete HELIOS v4.0 configuration for production deployment
 */

const { HeliosV4 } = require('../src/index.js');

async function productionSetup() {
  const helios = new HeliosV4({
    environment: 'production',
    version: '4.0.0',
    enableCache: true,
    enableDB: true,
    enableAI: true,
    enableIntegrations: true,

    // Cache configuration
    cache: {
      enableStatistics: true,
    },

    // Database configuration
    db: {
      poolSize: 30,
      connectionTimeout: 5000,
      idleTimeout: 30000,
      cacheTTL: 600,
    },

    // Gateway configuration
    gateway: {
      gzipThreshold: 1024,
      defaultPageSize: 20,
      maxPageSize: 100,
      compressionLevel: 6,
      cacheDuration: 3600,
    },

    // Monitoring
    monitoring: {
      enableMetrics: true,
      alertThresholds: {
        apiLatency: { p50: 100, p95: 200, p99: 300 },
        dbQuery: { p50: 20, p95: 40, p99: 50 },
        cacheHitRate: 80,
      },
      reportingInterval: 60000,
    },

    // AI configuration
    ai: {
      predictorThreshold: 0.7,
      sensitivityLevel: 6,
      minDataPoints: 20,
    },

    // Logging configuration
    logging: {
      provider: 'console',
      level: 'info',
      json: true,
      includeTimestamp: true,
    },

    // Metrics configuration
    metrics: {
      provider: 'prometheus',
      endpoint: process.env.METRICS_ENDPOINT || 'http://localhost:9090',
      flushInterval: 60000,
      batchSize: 100,
    },

    // Tracing configuration
    tracing: {
      provider: 'jaeger',
      serviceName: 'helios-api',
      samplingRate: 0.1,
    },

    // Alerting configuration
    alerting: {
      provider: 'alertmanager',
      deduplicationWindow: 300000,
      notificationHandler: async (alert) => {
        console.log('Alert fired:', alert);
      },
    },

    // Webhook configuration
    webhooks: {
      timeout: 5000,
      maxRetries: 3,
      queueEnabled: true,
    },

    // Secrets management
    secrets: {
      provider: 'vault',
      vaultAddr: process.env.VAULT_ADDR || 'http://localhost:8200',
      vaultToken: process.env.VAULT_TOKEN,
      cacheTTL: 300000,
    },

    // Dynamic configuration
    config: {
      cache: {
        ttl: 600,
      },
      database: {
        poolSize: 30,
      },
    },

    // Health checks
    health: {
      readinessCheck: async () => {
        // Check all dependencies
        return true;
      },
      livenessCheck: async () => {
        // Check if service is alive
        return true;
      },
    },
  });

  // Initialize
  console.log('Initializing HELIOS v4.0 for production...');
  await helios.initialize();

  // Register alert rules
  helios.integrations.alerting.registerRule('api_latency_high', {
    name: 'API Latency High',
    severity: 'critical',
    condition: { gt: 500 },
    message: 'API latency exceeded 500ms',
  });

  helios.integrations.alerting.registerRule('cache_miss_rate_high', {
    name: 'Cache Miss Rate High',
    severity: 'warning',
    condition: { gt: 30 },
    message: 'Cache miss rate exceeded 30%',
  });

  helios.integrations.alerting.registerRule('db_latency_high', {
    name: 'Database Latency High',
    severity: 'warning',
    condition: { gt: 100 },
    message: 'Database latency exceeded 100ms',
  });

  // Register webhook for alerts
  helios.integrations.webhooks.registerWebhook('slack', {
    url: process.env.SLACK_WEBHOOK_URL || 'https://hooks.slack.com/...',
    events: ['critical_alert', 'deployment'],
    active: true,
  });

  // Register health components
  helios.integrations.health.registerComponent('database', async () => {
    // Check database connectivity
    return true;
  });

  helios.integrations.health.registerComponent('cache', async () => {
    // Check cache connectivity
    return true;
  });

  helios.integrations.health.registerComponent('messaging', async () => {
    // Check message queue connectivity
    return true;
  });

  // Start metrics export interval
  setInterval(async () => {
    await helios.integrations.metrics.export();
  }, 60000);

  // Start health check interval
  setInterval(async () => {
    const health = await helios.integrations.health.health();
    if (health.status !== 'healthy') {
      helios.integrations.logging.warn('System health degraded', health);
    }
  }, 30000);

  // Start webhook processing
  setInterval(async () => {
    const result = await helios.integrations.webhooks.processQueue(50);
    if (result.processed > 0) {
      helios.integrations.logging.debug(`Processed ${result.processed} webhooks`);
    }
  }, 5000);

  // Log system status
  const status = await helios.getStatus();
  console.log('System initialized:', {
    version: status.version,
    environment: 'production',
    modules: Object.keys(status.modules),
    health: status.health.status,
  });

  // Handle graceful shutdown
  process.on('SIGTERM', async () => {
    console.log('SIGTERM received, shutting down gracefully...');

    // Stop accepting new requests
    // app.close()

    // Process remaining work
    await helios.integrations.webhooks.processQueue(100);
    await helios.integrations.metrics.export();

    // Shutdown HELIOS
    await helios.shutdown();

    process.exit(0);
  });

  process.on('SIGINT', async () => {
    console.log('SIGINT received, shutting down...');
    await helios.shutdown();
    process.exit(0);
  });

  return helios;
}

// Run setup
productionSetup()
  .then((helios) => {
    console.log('HELIOS v4.0 production setup complete');
    console.log('Ready to handle requests...');
  })
  .catch((error) => {
    console.error('Production setup failed:', error);
    process.exit(1);
  });

module.exports = { productionSetup };
