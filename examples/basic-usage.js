/**
 * Basic Usage Example
 * Demonstrates core HELIOS v4.0 features
 */

const { HeliosV4 } = require('../src/index.js');

async function basicExample() {
  // Initialize
  const helios = new HeliosV4({
    environment: 'development',
    enableCache: true,
    enableAI: false,
    enableIntegrations: true,
  });

  await helios.initialize();
  console.log('HELIOS v4.0 initialized\n');

  // Cache Example
  console.log('=== Cache Example ===');
  const cached = await helios.cache.staleWhileRevalidate(
    'example:data',
    async () => {
      console.log('Fetching data (1st call)...');
      return { id: 1, name: 'Example' };
    }
  );
  console.log('Result:', cached);

  // Second call (from cache)
  const cached2 = await helios.cache.staleWhileRevalidate(
    'example:data',
    async () => {
      console.log('This should not print (cached)');
      return { id: 1, name: 'Example' };
    }
  );
  console.log('Second call (from cache):', cached2.source);

  // Cache stats
  const stats = helios.cache.getStatistics();
  console.log('Cache hit rate:', stats.hitRate);

  // Response Optimization
  console.log('\n=== Response Optimization ===');
  const largeData = Array(100).fill({ id: 1, name: 'Item', email: 'item@example.com' });
  
  const optimized = await helios.gateway.buildOptimizedResponse(largeData, {
    compress: true,
    paginate: true,
    fields: 'id,name',
    pagination: { page: 1, limit: 10 },
  });

  console.log('Response size:', optimized.body.length, 'bytes');
  console.log('Compression ratio:', optimized.metadata.compressionRatio);

  // Logging
  console.log('\n=== Logging ===');
  helios.integrations.logging.info('Example operation', { operation: 'basic' });
  helios.integrations.logging.warn('This is a warning', {});

  // Get status
  const status = await helios.getStatus();
  console.log('\nSystem Status:', {
    version: status.version,
    initialized: status.initialized,
    modules: Object.keys(status.modules),
  });

  // Shutdown
  await helios.shutdown();
  console.log('\nShutdown complete');
}

// Run example
basicExample().catch(error => {
  console.error('Example failed:', error);
  process.exit(1);
});
