/**
 * Example with AI Capabilities
 * Demonstrates HELIOS v4.0 AI modules
 */

const { HeliosV4 } = require('../src/index.js');

async function withAIExample() {
  const helios = new HeliosV4({
    environment: 'production',
    enableCache: true,
    enableAI: true,
    enableIntegrations: true,
  });

  await helios.initialize();
  console.log('HELIOS v4.0 with AI initialized\n');

  // Predictive Cache Warmer
  console.log('=== Predictive Cache Warmer ===');
  
  // Simulate access patterns
  for (let i = 0; i < 20; i++) {
    helios.ai.cacheWarmer.recordAccess('user:1:profile', Math.random() * 100);
    helios.ai.cacheWarmer.recordAccess('user:1:settings', Math.random() * 80);
    helios.ai.cacheWarmer.recordAccess('user:2:profile', Math.random() * 120);
  }

  const predictions = helios.ai.cacheWarmer.predictNextAccesses();
  console.log('Predicted accesses:', predictions);

  const warmed = await helios.ai.cacheWarmer.warmCache(async (key) => {
    return { key, data: 'cached', timestamp: Date.now() };
  });
  console.log('Warming result:', warmed);
  console.log('AI Warmer metrics:', helios.ai.cacheWarmer.getMetrics());

  // Auto-Scaling Advisor
  console.log('\n=== Auto-Scaling Advisor ===');
  
  helios.ai.scalingAdvisor.recordMetrics({
    cpu: 85,
    memory: 70,
    latency: 400,
    requestsPerSecond: 1200,
    activeConnections: 150,
  });

  helios.ai.scalingAdvisor.recordMetrics({
    cpu: 88,
    memory: 75,
    latency: 420,
    requestsPerSecond: 1300,
    activeConnections: 160,
  });

  const scaling = helios.ai.scalingAdvisor.analyze({ instances: 3, workers: 8 });
  console.log('Scaling recommendation:', {
    recommendation: scaling.recommendation,
    confidence: scaling.confidence,
    reasons: scaling.reasons,
  });

  // Anomaly Detector
  console.log('\n=== Anomaly Detector ===');
  
  const normalLatencies = Array(50).fill(0).map(() => Math.random() * 100 + 100);
  helios.ai.anomalyDetector.learnBaseline('api.latency', normalLatencies);

  const anomaly = helios.ai.anomalyDetector.detectAnomaly('api.latency', 500);
  console.log('Anomaly detection:', {
    isAnomaly: anomaly.isAnomaly,
    reason: anomaly.reason,
    severity: anomaly.severity,
  });

  // Traffic Predictor
  console.log('\n=== Traffic Predictor ===');
  
  for (let i = 0; i < 100; i++) {
    helios.ai.trafficPredictor.recordRequest({
      endpoint: '/api/users',
      method: 'GET',
      statusCode: 200,
      latency: Math.random() * 100,
      size: 1024,
    });
  }

  const traffic = helios.ai.trafficPredictor.predictTraffic();
  console.log('Traffic prediction:', {
    prediction: traffic.prediction,
    confidence: traffic.confidence,
    trend: traffic.patterns?.trend,
  });

  // Error Clustering
  console.log('\n=== Error Clustering ===');
  
  for (let i = 0; i < 5; i++) {
    helios.ai.errorClustering.processError(
      new Error('Database connection timeout'),
      { endpoint: '/api/users', attempt: i + 1 }
    );
  }

  for (let i = 0; i < 3; i++) {
    helios.ai.errorClustering.processError(
      new Error('Cache miss - unable to retrieve'),
      { endpoint: '/api/posts', attempt: i + 1 }
    );
  }

  const clusters = helios.ai.errorClustering.getClusters({ limit: 5 });
  console.log('Error clusters:', clusters);
  console.log('Clustering metrics:', helios.ai.errorClustering.getMetrics());

  // Get AI metrics
  const status = await helios.getStatus();
  console.log('\n=== AI Module Metrics ===');
  console.log(JSON.stringify(status.modules.ai, null, 2));

  await helios.shutdown();
  console.log('\nShutdown complete');
}

// Run example
withAIExample().catch(error => {
  console.error('Example failed:', error);
  process.exit(1);
});
