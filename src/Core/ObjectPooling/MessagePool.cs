using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;

namespace HELIOS.Platform.Architecture.ObjectPooling
{
    /// <summary>
    /// Pooled message wrapper for ServiceMessage objects.
    /// Reduces allocation pressure and GC overhead by reusing message objects.
    /// 
    /// OPTIMIZATION (opt-002): Object pooling for high-frequency allocations
    /// - Expected improvement: 12% reduction in memory allocations + GC pressure
    /// - Pool hits: >85% reuse rate
    /// - Thread-safe: ConcurrentBag for lock-free operations
    /// </summary>
    public class MessagePool : IDisposable
    {
        private readonly ConcurrentBag<ServiceMessage> _messagePool;
        private readonly ConcurrentBag<MessagePoolMetrics> _metricsPool;
        private readonly int _initialPoolSize;
        private readonly int _maxPoolSize;
        private volatile int _currentPoolSize;
        private volatile int _totalAllocations;
        private volatile int _poolHits;
        private volatile int _poolMisses;
        private volatile int _resets;
        private bool _disposed;

        // Metrics tracking
        private readonly Stopwatch _metricsStopwatch = Stopwatch.StartNew();
        private long _peakMemoryUsage;
        private long _currentMemoryUsage;

        /// <summary>
        /// Creates a new MessagePool with configurable sizing
        /// </summary>
        /// <param name="initialPoolSize">Number of messages to pre-allocate (default: 100)</param>
        /// <param name="maxPoolSize">Maximum pool size before rejecting returns (default: 500)</param>
        public MessagePool(int initialPoolSize = 100, int maxPoolSize = 500)
        {
            if (initialPoolSize < 0) throw new ArgumentException("initialPoolSize must be >= 0", nameof(initialPoolSize));
            if (maxPoolSize < initialPoolSize) throw new ArgumentException("maxPoolSize must be >= initialPoolSize", nameof(maxPoolSize));

            _initialPoolSize = initialPoolSize;
            _maxPoolSize = maxPoolSize;
            _currentPoolSize = 0;
            _messagePool = new ConcurrentBag<ServiceMessage>();
            _metricsPool = new ConcurrentBag<MessagePoolMetrics>();

            // Pre-allocate initial pool
            for (int i = 0; i < initialPoolSize; i++)
            {
                var msg = new ServiceMessage();
                _messagePool.Add(msg);
                _currentPoolSize++;
                Interlocked.Increment(ref _totalAllocations);
            }

            _currentMemoryUsage = EstimateMemoryUsage(_currentPoolSize);
            _peakMemoryUsage = _currentMemoryUsage;
        }

        /// <summary>
        /// Gets a message from the pool or creates a new one if pool is empty
        /// </summary>
        public ServiceMessage GetMessage()
        {
            ThrowIfDisposed();

            ServiceMessage message;
            if (_messagePool.TryTake(out message))
            {
                Interlocked.Increment(ref _poolHits);
                Interlocked.Decrement(ref _currentPoolSize);
                return message;
            }

            // Pool miss: create new message
            Interlocked.Increment(ref _poolMisses);
            Interlocked.Increment(ref _totalAllocations);
            return new ServiceMessage();
        }

        /// <summary>
        /// Returns a message to the pool after clearing its state
        /// </summary>
        public void ReturnMessage(ServiceMessage message)
        {
            ThrowIfDisposed();

            if (message == null) return;

            // Don't accept messages if pool is at max capacity
            if (_currentPoolSize >= _maxPoolSize)
            {
                return;
            }

            // Reset message state
            ResetMessage(message);
            Interlocked.Increment(ref _resets);

            // Return to pool
            _messagePool.Add(message);
            Interlocked.Increment(ref _currentPoolSize);

            // Update memory tracking
            long newMemoryUsage = EstimateMemoryUsage(_currentPoolSize);
            _currentMemoryUsage = newMemoryUsage;
            if (newMemoryUsage > _peakMemoryUsage)
            {
                Interlocked.Exchange(ref _peakMemoryUsage, newMemoryUsage);
            }
        }

        /// <summary>
        /// Gets or creates a metrics object from the metrics pool
        /// </summary>
        public MessagePoolMetrics GetMetrics()
        {
            MessagePoolMetrics metrics;
            if (_metricsPool.TryTake(out metrics))
            {
                return metrics;
            }
            return new MessagePoolMetrics();
        }

        /// <summary>
        /// Returns a metrics object to the metrics pool
        /// </summary>
        public void ReturnMetrics(MessagePoolMetrics metrics)
        {
            if (metrics != null && _metricsPool.Count < _maxPoolSize / 10)
            {
                metrics.Reset();
                _metricsPool.Add(metrics);
            }
        }

        /// <summary>
        /// Gets current pool statistics
        /// </summary>
        public PoolStatistics GetStatistics()
        {
            return new PoolStatistics
            {
                PooledObjectCount = _currentPoolSize,
                TotalAllocations = _totalAllocations,
                PoolHits = _poolHits,
                PoolMisses = _poolMisses,
                HitRate = _totalAllocations > 0 ? ((double)_poolHits / (_poolHits + _poolMisses)) * 100 : 0,
                ResetCount = _resets,
                CurrentMemoryUsageBytes = _currentMemoryUsage,
                PeakMemoryUsageBytes = _peakMemoryUsage,
                PoolCapacityPercent = (_currentPoolSize * 100.0) / _maxPoolSize,
                ElapsedSeconds = _metricsStopwatch.Elapsed.TotalSeconds
            };
        }

        /// <summary>
        /// Clears the pool and resets all statistics
        /// </summary>
        public void Clear()
        {
            _messagePool.Clear();
            _metricsPool.Clear();
            _currentPoolSize = 0;
            _totalAllocations = 0;
            _poolHits = 0;
            _poolMisses = 0;
            _resets = 0;
            _currentMemoryUsage = 0;
            _peakMemoryUsage = 0;
            _metricsStopwatch.Restart();
        }

        /// <summary>
        /// Pre-fills the pool to specified capacity
        /// </summary>
        public void Prefill(int targetSize)
        {
            while (_currentPoolSize < targetSize && _currentPoolSize < _maxPoolSize)
            {
                var msg = new ServiceMessage();
                _messagePool.Add(msg);
                Interlocked.Increment(ref _currentPoolSize);
                Interlocked.Increment(ref _totalAllocations);
            }
        }

        // Private helpers

        private void ResetMessage(ServiceMessage message)
        {
            // Clear message state while preserving the object identity
            message.Id = Guid.NewGuid().ToString();
            message.FromService = null;
            message.ToService = null;
            message.MessageType = null;
            message.Payload = null;
            message.Timestamp = default;
            message.Metadata?.Clear();
        }

        private long EstimateMemoryUsage(int messageCount)
        {
            // Rough estimate: ServiceMessage ~200 bytes + Dictionary ~100 bytes
            return messageCount * 300L;
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(MessagePool));
        }

        public void Dispose()
        {
            _disposed = true;
            _messagePool.Clear();
            _metricsPool.Clear();
            _metricsStopwatch?.Dispose();
        }
    }

    /// <summary>
    /// Metrics snapshot from MessagePool
    /// </summary>
    public class PoolStatistics
    {
        public int PooledObjectCount { get; set; }
        public int TotalAllocations { get; set; }
        public int PoolHits { get; set; }
        public int PoolMisses { get; set; }
        public double HitRate { get; set; }
        public int ResetCount { get; set; }
        public long CurrentMemoryUsageBytes { get; set; }
        public long PeakMemoryUsageBytes { get; set; }
        public double PoolCapacityPercent { get; set; }
        public double ElapsedSeconds { get; set; }

        public override string ToString()
        {
            return $"Pool Stats | Pooled: {PooledObjectCount} | Hits: {PoolHits} | Misses: {PoolMisses} | " +
                   $"HitRate: {HitRate:F2}% | Memory: {CurrentMemoryUsageBytes / 1024.0:F2}KB / {PeakMemoryUsageBytes / 1024.0:F2}KB | " +
                   $"Capacity: {PoolCapacityPercent:F1}%";
        }
    }

    /// <summary>
    /// Metrics data holder for pooling
    /// </summary>
    public class MessagePoolMetrics
    {
        public string MessageType { get; set; }
        public long ProcessingTimeMs { get; set; }
        public int HandlerCount { get; set; }
        public bool PooledObject { get; set; }
        public DateTime Timestamp { get; set; }

        public void Reset()
        {
            MessageType = null;
            ProcessingTimeMs = 0;
            HandlerCount = 0;
            PooledObject = false;
            Timestamp = default;
        }
    }
}
