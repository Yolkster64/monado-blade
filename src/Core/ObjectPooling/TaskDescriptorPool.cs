using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;

namespace HELIOS.Platform.Architecture.ObjectPooling
{
    /// <summary>
    /// Pool for task descriptors to reduce allocation overhead.
    /// Reuses TaskDescriptor objects across scheduler cycles.
    /// 
    /// OPTIMIZATION (opt-002): Object pooling for scheduled tasks
    /// - Expected improvement: 8-12% reduction in task-related allocations
    /// - Maintains task identity while reusing object memory
    /// - Thread-safe concurrent bag for high-throughput scheduling
    /// </summary>
    public class TaskDescriptorPool : IDisposable
    {
        private readonly ConcurrentBag<TaskDescriptor> _pool;
        private readonly int _initialSize;
        private readonly int _maxSize;
        private volatile int _poolSize;
        private volatile long _allocations;
        private volatile long _hits;
        private volatile long _misses;
        private volatile long _resets;
        private bool _disposed;

        private readonly Stopwatch _metrics = Stopwatch.StartNew();

        /// <summary>
        /// Creates a new TaskDescriptorPool
        /// </summary>
        /// <param name="initialSize">Number of descriptors to pre-allocate (default: 50)</param>
        /// <param name="maxSize">Maximum pool size (default: 250)</param>
        public TaskDescriptorPool(int initialSize = 50, int maxSize = 250)
        {
            if (initialSize < 0) throw new ArgumentException("initialSize must be >= 0", nameof(initialSize));
            if (maxSize < initialSize) throw new ArgumentException("maxSize must be >= initialSize", nameof(maxSize));

            _initialSize = initialSize;
            _maxSize = maxSize;
            _pool = new ConcurrentBag<TaskDescriptor>();

            // Pre-allocate
            for (int i = 0; i < initialSize; i++)
            {
                _pool.Add(new TaskDescriptor());
                Interlocked.Increment(ref _poolSize);
                Interlocked.Increment(ref _allocations);
            }
        }

        /// <summary>
        /// Gets a task descriptor from the pool
        /// </summary>
        public TaskDescriptor GetDescriptor()
        {
            ThrowIfDisposed();

            TaskDescriptor descriptor;
            if (_pool.TryTake(out descriptor))
            {
                Interlocked.Increment(ref _hits);
                Interlocked.Decrement(ref _poolSize);
                return descriptor;
            }

            // Miss: allocate new
            Interlocked.Increment(ref _misses);
            Interlocked.Increment(ref _allocations);
            return new TaskDescriptor();
        }

        /// <summary>
        /// Returns a descriptor to the pool after resetting it
        /// </summary>
        public void ReturnDescriptor(TaskDescriptor descriptor)
        {
            ThrowIfDisposed();

            if (descriptor == null) return;
            if (_poolSize >= _maxSize) return;

            // Reset state
            descriptor.Reset();
            Interlocked.Increment(ref _resets);

            // Return to pool
            _pool.Add(descriptor);
            Interlocked.Increment(ref _poolSize);
        }

        /// <summary>
        /// Gets current statistics
        /// </summary>
        public TaskPoolStatistics GetStatistics()
        {
            long totalOps = _hits + _misses;
            return new TaskPoolStatistics
            {
                PooledDescriptorCount = _poolSize,
                TotalAllocations = (int)_allocations,
                Hits = (int)_hits,
                Misses = (int)_misses,
                HitRate = totalOps > 0 ? (_hits * 100.0) / totalOps : 0,
                Resets = (int)_resets,
                PoolCapacityPercent = (_poolSize * 100.0) / _maxSize,
                ElapsedSeconds = _metrics.Elapsed.TotalSeconds
            };
        }

        /// <summary>
        /// Clears the pool
        /// </summary>
        public void Clear()
        {
            _pool.Clear();
            _poolSize = 0;
            _allocations = 0;
            _hits = 0;
            _misses = 0;
            _resets = 0;
            _metrics.Restart();
        }

        /// <summary>
        /// Pre-fills the pool
        /// </summary>
        public void Prefill(int targetSize)
        {
            while (_poolSize < targetSize && _poolSize < _maxSize)
            {
                _pool.Add(new TaskDescriptor());
                Interlocked.Increment(ref _poolSize);
                Interlocked.Increment(ref _allocations);
            }
        }

        private void ThrowIfDisposed()
        {
            if (_disposed) throw new ObjectDisposedException(nameof(TaskDescriptorPool));
        }

        public void Dispose()
        {
            _disposed = true;
            _pool.Clear();
            _metrics.Stop();
        }
    }

    /// <summary>
    /// Reusable task descriptor (lightweight wrapper for ScheduledTask)
    /// </summary>
    public class TaskDescriptor
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public int Priority { get; set; }
        public TaskStatusValue Status { get; set; }
        public DateTime ScheduledFor { get; set; }
        public TimeSpan? RecurrenceInterval { get; set; }
        public int[] AffinityMask { get; set; }
        public long MaxMemoryBytes { get; set; }

        public TaskDescriptor()
        {
            Reset();
        }

        public void Reset()
        {
            Id = Guid.NewGuid().ToString();
            Name = null;
            Priority = 0;
            Status = TaskStatusValue.Pending;
            ScheduledFor = DateTime.MinValue;
            RecurrenceInterval = null;
            AffinityMask = null;
            MaxMemoryBytes = 0;
        }

        public override string ToString()
        {
            return $"[{Status}] {Name} (Priority: {Priority})";
        }
    }

    /// <summary>
    /// Task status enumeration
    /// </summary>
    public enum TaskStatusValue
    {
        Pending,
        Running,
        Completed,
        Failed,
        Cancelled
    }

    /// <summary>
    /// Statistics for TaskDescriptorPool
    /// </summary>
    public class TaskPoolStatistics
    {
        public int PooledDescriptorCount { get; set; }
        public int TotalAllocations { get; set; }
        public int Hits { get; set; }
        public int Misses { get; set; }
        public double HitRate { get; set; }
        public int Resets { get; set; }
        public double PoolCapacityPercent { get; set; }
        public double ElapsedSeconds { get; set; }

        public override string ToString()
        {
            return $"TaskPool Stats | Pooled: {PooledDescriptorCount} | Hits: {Hits} | Misses: {Misses} | " +
                   $"HitRate: {HitRate:F2}% | Resets: {Resets} | Capacity: {PoolCapacityPercent:F1}%";
        }
    }
}
