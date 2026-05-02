using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MonadoBlade.Optimization;

/// <summary>Performance optimization manager for cache, pooling, and profiling</summary>
public interface IPerformanceOptimizer
{
    Task<PerformanceBaseline> EstablishBaselineAsync(CancellationToken ct = default);
    Task<PerformanceReport> ProfileApplicationAsync(TimeSpan duration, CancellationToken ct = default);
    Task ApplyOptimizationsAsync(PerformanceOptimizationStrategy strategy, CancellationToken ct = default);
}

/// <summary>Performance baseline measurements</summary>
public record PerformanceBaseline
{
    public TimeSpan BootTime { get; init; }
    public TimeSpan AppLaunchTime { get; init; }
    public long PeakMemoryUsage { get; init; }
    public float CPUUsageIdle { get; init; }
    public int QueryResponseTimeMs { get; init; }
    public DateTime BaselinedAt { get; init; }
}

/// <summary>Performance report</summary>
public record PerformanceReport
{
    public PerformanceBaseline CurrentMetrics { get; init; } = null!;
    public PerformanceBaseline? PreviousBaseline { get; init; }
    public Dictionary<string, float> ImprovementPercentages { get; init; } = new();
    public List<PerformanceBottleneck> BottlenecksIdentified { get; init; } = new();
    public List<OptimizationRecommendation> Recommendations { get; init; } = new();
}

/// <summary>Performance bottleneck</summary>
public record PerformanceBottleneck
{
    public string ComponentName { get; init; } = string.Empty;
    public string BottleneckType { get; init; } = string.Empty; // CPU, Memory, IO, Network
    public float Impact { get; init; } // 0-100, percentage of total time/resources
    public string Description { get; init; } = string.Empty;
}

/// <summary>Optimization recommendation</summary>
public record OptimizationRecommendation
{
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public float EstimatedImprovement { get; init; } // percentage
    public string Priority { get; init; } = string.Empty; // High, Medium, Low
    public string ImplementationDifficulty { get; init; } = string.Empty; // Easy, Medium, Hard
}

/// <summary>Performance optimization strategy</summary>
public enum PerformanceOptimizationStrategy
{
    Conservative,  // Small, safe improvements
    Balanced,      // Mix of improvements
    Aggressive     // Maximize performance (some risk)
}

/// <summary>L1, L2, L3 cache manager</summary>
public interface ICacheManager
{
    /// <summary>Get or create L1 cache (in-memory, per-process)</summary>
    ICache<K, V> GetL1Cache<K, V>(string cacheKey, int maxSizeItems = 1000)
        where K : notnull;
    
    /// <summary>Get or create L2 cache (local disk, process-wide)</summary>
    ICache<K, V> GetL2Cache<K, V>(string cacheKey, int maxSizeMB = 100)
        where K : notnull;
    
    /// <summary>Get or create L3 cache (distributed, cross-process)</summary>
    IDistributedCache<K, V> GetL3Cache<K, V>(string cacheKey, int maxSizeMB = 1000)
        where K : notnull;
    
    /// <summary>Clear all caches</summary>
    Task ClearAllCachesAsync(CancellationToken ct = default);
    
    /// <summary>Get cache statistics</summary>
    Task<CacheStatistics> GetCacheStatisticsAsync(CancellationToken ct = default);
}

/// <summary>Generic cache interface</summary>
public interface ICache<K, V> where K : notnull
{
    Task<bool> TryGetAsync(K key, out V value, CancellationToken ct = default);
    Task SetAsync(K key, V value, TimeSpan? ttl = null, CancellationToken ct = default);
    Task RemoveAsync(K key, CancellationToken ct = default);
    Task<V> GetOrCreateAsync(K key, Func<CancellationToken, Task<V>> factory, CancellationToken ct = default);
}

/// <summary>Distributed cache interface</summary>
public interface IDistributedCache<K, V> : ICache<K, V> where K : notnull
{
    Task InvalidateAcrossNodesAsync(K key, CancellationToken ct = default);
}

/// <summary>Cache statistics</summary>
public record CacheStatistics
{
    public int L1HitCount { get; init; }
    public int L1MissCount { get; init; }
    public int L2HitCount { get; init; }
    public int L2MissCount { get; init; }
    public int L3HitCount { get; init; }
    public int L3MissCount { get; init; }
    
    public float L1HitRate => (L1HitCount + L1MissCount) > 0 
        ? (float)L1HitCount / (L1HitCount + L1MissCount) 
        : 0f;
    
    public float L2HitRate => (L2HitCount + L2MissCount) > 0 
        ? (float)L2HitCount / (L2HitCount + L2MissCount) 
        : 0f;
    
    public float L3HitRate => (L3HitCount + L3MissCount) > 0 
        ? (float)L3HitCount / (L3HitCount + L3MissCount) 
        : 0f;
}

/// <summary>Object pool for allocation-heavy operations</summary>
public interface IObjectPool<T> where T : class
{
    /// <summary>Rent object from pool</summary>
    T Rent();
    
    /// <summary>Return object to pool</summary>
    void Return(T item);
    
    /// <summary>Get pool statistics</summary>
    ObjectPoolStatistics GetStatistics();
}

/// <summary>Object pool statistics</summary>
public record ObjectPoolStatistics
{
    public int PoolSize { get; init; }
    public int AvailableObjects { get; init; }
    public int RentCount { get; init; }
    public int ReturnCount { get; init; }
    public long TotalAllocations { get; init; }
    public float AllocationReductionPercentage { get; init; }
}

/// <summary>LINQ query optimizer</summary>
public interface IQueryOptimizer
{
    /// <summary>Analyze query for optimization opportunities</summary>
    Task<QueryAnalysis> AnalyzeQueryAsync(string query, CancellationToken ct = default);
    
    /// <summary>Get suggested optimized query</summary>
    string GetOptimizedQuery(QueryAnalysis analysis);
    
    /// <summary>Profile query execution</summary>
    Task<QueryExecutionProfile> ProfileQueryAsync(string query, object[] parameters, CancellationToken ct = default);
}

/// <summary>Query analysis result</summary>
public record QueryAnalysis
{
    public string OriginalQuery { get; init; } = string.Empty;
    public List<QueryIssue> IdentifiedIssues { get; init; } = new();
    public List<string> OptimizationSuggestions { get; init; } = new();
    public float EstimatedImprovement { get; init; } // percentage
}

/// <summary>Query issue</summary>
public record QueryIssue
{
    public string IssueType { get; init; } = string.Empty; // N+1, MissingIndex, Inefficientjoin, etc.
    public string Description { get; init; } = string.Empty;
    public string Severity { get; init; } = string.Empty; // Low, Medium, High
}

/// <summary>Query execution profile</summary>
public record QueryExecutionProfile
{
    public string Query { get; init; } = string.Empty;
    public TimeSpan ExecutionTime { get; init; }
    public int RowsReturned { get; init; }
    public long MemoryUsedBytes { get; init; }
    public float CPUUsagePercent { get; init; }
    public List<string> ExecutionSteps { get; init; } = new();
}

/// <summary>Memory profiler</summary>
public interface IMemoryProfiler
{
    /// <summary>Start memory profiling session</summary>
    Task<string> StartProfilingAsync(CancellationToken ct = default);
    
    /// <summary>Stop profiling and get results</summary>
    Task<MemoryProfileResult> StopProfilingAsync(string sessionId, CancellationToken ct = default);
    
    /// <summary>Get current memory statistics</summary>
    MemoryStatistics GetCurrentStatistics();
    
    /// <summary>Detect memory leaks</summary>
    Task<List<MemoryLeak>> DetectLeaksAsync(CancellationToken ct = default);
}

/// <summary>Memory profiling result</summary>
public record MemoryProfileResult
{
    public long PeakMemoryUsage { get; init; }
    public long AverageMemoryUsage { get; init; }
    public List<AllocationHotspot> TopAllocators { get; init; } = new();
    public TimeSpan ProfilingDuration { get; init; }
}

/// <summary>Allocation hotspot</summary>
public record AllocationHotspot
{
    public string AllocatingCode { get; init; } = string.Empty;
    public long TotalAllocationBytes { get; init; }
    public int AllocationCount { get; init; }
}

/// <summary>Detected memory leak</summary>
public record MemoryLeak
{
    public string LeakLocation { get; init; } = string.Empty;
    public long EstimatedLeakBytes { get; init; }
    public string LeakedObjectType { get; init; } = string.Empty;
}

/// <summary>Memory statistics</summary>
public record MemoryStatistics
{
    public long TotalMemoryMB { get; init; }
    public long UsedMemoryMB { get; init; }
    public long AvailableMemoryMB { get; init; }
    public float GCPressure { get; init; } // 0-100
    public int Gen0Collections { get; init; }
    public int Gen1Collections { get; init; }
    public int Gen2Collections { get; init; }
}

/// <summary>CPU profiler</summary>
public interface ICPUProfiler
{
    /// <summary>Start CPU profiling session</summary>
    Task<string> StartProfilingAsync(CancellationToken ct = default);
    
    /// <summary>Stop profiling and get hotspots</summary>
    Task<CPUProfileResult> StopProfilingAsync(string sessionId, CancellationToken ct = default);
    
    /// <summary>Identify CPU hotspots</summary>
    Task<List<CPUHotspot>> IdentifyHotspotsAsync(CancellationToken ct = default);
}

/// <summary>CPU profiling result</summary>
public record CPUProfileResult
{
    public float AverageCPUUsage { get; init; } // percentage
    public float PeakCPUUsage { get; init; } // percentage
    public List<CPUHotspot> Hotspots { get; init; } = new();
    public TimeSpan ProfilingDuration { get; init; }
}

/// <summary>CPU hotspot</summary>
public record CPUHotspot
{
    public string FunctionName { get; init; } = string.Empty;
    public float CPUTimePercent { get; init; }
    public int CallCount { get; init; }
    public TimeSpan TotalTime { get; init; }
    public string SourceFile { get; init; } = string.Empty;
}

/// <summary>I/O profiler</summary>
public interface IIOProfiler
{
    /// <summary>Profile disk I/O performance</summary>
    Task<DiskIOProfile> ProfileDiskIOAsync(CancellationToken ct = default);
    
    /// <summary>Profile network I/O</summary>
    Task<NetworkIOProfile> ProfileNetworkIOAsync(CancellationToken ct = default);
    
    /// <summary>Identify I/O bottlenecks</summary>
    Task<List<IOBottleneck>> IdentifyBottlenecksAsync(CancellationToken ct = default);
}

/// <summary>Disk I/O profile</summary>
public record DiskIOProfile
{
    public float ReadSpeedMBps { get; init; }
    public float WriteSpeedMBps { get; init; }
    public int IOPS { get; init; }
    public float LatencyMs { get; init; }
    public float CPUUsageDuringIO { get; init; }
}

/// <summary>Network I/O profile</summary>
public record NetworkIOProfile
{
    public float UploadSpeedMBps { get; init; }
    public float DownloadSpeedMBps { get; init; }
    public float LatencyMs { get; init; }
    public int PacketsPerSecond { get; init; }
}

/// <summary>I/O bottleneck</summary>
public record IOBottleneck
{
    public string BottleneckType { get; init; } = string.Empty; // Disk, Network, Buffer
    public string Description { get; init; } = string.Empty;
    public float Impact { get; init; }
    public string RecommendedAction { get; init; } = string.Empty;
}
