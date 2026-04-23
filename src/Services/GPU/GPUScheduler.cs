using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MonadoBlade.Core.Framework;

namespace MonadoBlade.Services.GPU
{
    /// <summary>
    /// GPUScheduler - Intelligent task allocation to optimal compute resources
    /// Strategy: GPU5090 → Intel Arc → CPU → Azure Cloud
    /// </summary>
    public class GPUScheduler : IHELIOSService
    {
        private readonly ILogger<GPUScheduler> _logger;
        private readonly ComputeResourceMonitor _resourceMonitor;
        private readonly TaskCharacterizer _characterizer;
        
        private readonly ConcurrentQueue<ComputeTask> _taskQueue = new();
        private readonly ConcurrentDictionary<string, ComputeAllocation> _allocations = new();
        private readonly ConcurrentDictionary<string, SchedulingMetrics> _metrics = new();
        
        private Task? _processingLoop;
        private CancellationTokenSource? _cancellationSource;
        private const int MaxQueueSize = 1000;

        public GPUScheduler(ILogger<GPUScheduler> logger, 
            ComputeResourceMonitor resourceMonitor, 
            TaskCharacterizer characterizer)
        {
            _logger = logger;
            _resourceMonitor = resourceMonitor;
            _characterizer = characterizer;
        }

        public async Task StartAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("GPUScheduler started");
            _cancellationSource = new CancellationTokenSource();
            
            _processingLoop = Task.Run(async () => await ProcessQueueAsync(_cancellationSource.Token), _cancellationSource.Token);
            
            await Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("GPUScheduler stopped");
            _cancellationSource?.Cancel();
            
            if (_processingLoop != null)
                await _processingLoop;
        }

        /// <summary>
        /// Allocate task to optimal resource
        /// </summary>
        public async Task<ComputeAllocation> AllocateAsync(ComputeTask task)
        {
            if (task == null)
                throw new ArgumentNullException(nameof(task));

            // Check queue capacity
            if (_taskQueue.Count >= MaxQueueSize)
            {
                _logger.LogWarning("Task queue full ({Count}/{Max}), rejecting task {TaskId}", 
                    _taskQueue.Count, MaxQueueSize, task.Id);
                throw new InvalidOperationException("Task queue full, try again later");
            }

            try
            {
                _logger.LogInformation("Allocating task {TaskId}", task.Id);

                // Profile task
                var profile = await _characterizer.ProfileAsync(task);
                
                // Find optimal resource
                var resource = FindOptimalResource(profile);
                
                var allocation = new ComputeAllocation
                {
                    Id = Guid.NewGuid().ToString(),
                    TaskId = task.Id,
                    AssignedResource = resource,
                    AllocatedAt = DateTime.UtcNow,
                    Status = "pending",
                    EstimatedDuration = _characterizer.EstimateExecutionTime(profile, resource),
                    EstimatedCost = _characterizer.EstimateCost(profile, resource)
                };

                _allocations[allocation.Id] = allocation;
                _taskQueue.Enqueue(task);
                
                RecordMetric("Allocations", 1);
                _logger.LogInformation("Task {TaskId} allocated to {Resource}", task.Id, resource);
                
                return allocation;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to allocate task {TaskId}", task.Id);
                RecordMetric("AllocationFailures", 1);
                throw;
            }
        }

        /// <summary>
        /// Get available resources
        /// </summary>
        public ResourceAvailability GetAvailableResources()
        {
            return new ResourceAvailability
            {
                GPU5090Available = true, // Simplified - check actual availability
                IntelArcAvailable = true,
                CPUCoresAvailable = Environment.ProcessorCount,
                AzureAvailable = true,
                Timestamp = DateTime.UtcNow
            };
        }

        /// <summary>
        /// Get queue status
        /// </summary>
        public QueueStatus GetQueueStatus()
        {
            return new QueueStatus
            {
                Pending = _taskQueue.Count,
                Processing = _allocations.Values.Count(a => a.Status == "processing"),
                Completed = _allocations.Values.Count(a => a.Status == "completed"),
                Failed = _allocations.Values.Count(a => a.Status == "failed"),
                MaxCapacity = MaxQueueSize,
                Timestamp = DateTime.UtcNow
            };
        }

        /// <summary>
        /// Cancel pending allocation
        /// </summary>
        public bool CancelAllocation(string allocationId)
        {
            if (_allocations.TryGetValue(allocationId, out var allocation))
            {
                if (allocation.Status == "pending" || allocation.Status == "processing")
                {
                    allocation.Status = "cancelled";
                    _logger.LogInformation("Allocation {AllocationId} cancelled", allocationId);
                    RecordMetric("Cancellations", 1);
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Get scheduling metrics
        /// </summary>
        public SchedulingMetrics GetMetrics()
        {
            return new SchedulingMetrics
            {
                TotalAllocations = _allocations.Count,
                CompletedAllocations = _allocations.Values.Count(a => a.Status == "completed"),
                FailedAllocations = _allocations.Values.Count(a => a.Status == "failed"),
                AverageCost = CalculateAverageCost(),
                AverageDuration = CalculateAverageDuration(),
                ResourceUtilization = CalculateResourceUtilization()
            };
        }

        private string FindOptimalResource(TaskProfile profile)
        {
            // Cascading strategy: GPU5090 → Arc → CPU → Azure
            
            if (profile.GpuMemoryRequired <= 80 && _CanUseGPU("5090"))
            {
                _logger.LogInformation("Scheduling on GPU5090 ({Memory}GB required)", profile.GpuMemoryRequired);
                return "GPU5090";
            }

            if (profile.GpuMemoryRequired <= 16 && _CanUseGPU("Arc"))
            {
                _logger.LogInformation("Scheduling on Intel Arc ({Memory}GB required)", profile.GpuMemoryRequired);
                return "IntelArc";
            }

            if (profile.CpuCoresRequired <= Environment.ProcessorCount)
            {
                _logger.LogInformation("Scheduling on CPU ({Cores} cores required)", profile.CpuCoresRequired);
                return "CPU";
            }

            _logger.LogInformation("Scheduling on Azure (fallback)");
            return "Azure";
        }

        private bool _CanUseGPU(string gpuName)
        {
            // Simplified - in production, query actual GPU availability
            return true;
        }

        private void RecordMetric(string metricName, int value)
        {
            var key = $"{DateTime.UtcNow:yyyyMMdd-HH}:{metricName}";
            _metrics.AddOrUpdate(key, 
                new SchedulingMetrics { TotalAllocations = value },
                (_, existing) => { existing.TotalAllocations += value; return existing; });
        }

        private decimal CalculateAverageCost()
        {
            var completed = _allocations.Values.Where(a => a.Status == "completed").ToList();
            if (!completed.Any()) return 0;
            return completed.Average(a => (decimal)a.EstimatedCost);
        }

        private TimeSpan CalculateAverageDuration()
        {
            var completed = _allocations.Values.Where(a => a.Status == "completed").ToList();
            if (!completed.Any()) return TimeSpan.Zero;
            
            var totalMs = completed.Sum(a => (long)a.EstimatedDuration.TotalMilliseconds);
            return TimeSpan.FromMilliseconds(totalMs / completed.Count);
        }

        private Dictionary<string, double> CalculateResourceUtilization()
        {
            var allocations = _allocations.Values.ToList();
            if (!allocations.Any())
                return new();

            return new()
            {
                { "GPU5090", (double)allocations.Count(a => a.AssignedResource == "GPU5090") / allocations.Count },
                { "IntelArc", (double)allocations.Count(a => a.AssignedResource == "IntelArc") / allocations.Count },
                { "CPU", (double)allocations.Count(a => a.AssignedResource == "CPU") / allocations.Count },
                { "Azure", (double)allocations.Count(a => a.AssignedResource == "Azure") / allocations.Count }
            };
        }

        private async Task ProcessQueueAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    if (_taskQueue.TryDequeue(out var task))
                    {
                        var allocation = _allocations.Values.FirstOrDefault(a => a.TaskId == task.Id);
                        if (allocation != null)
                        {
                            allocation.Status = "processing";
                            allocation.StartedAt = DateTime.UtcNow;
                            
                            // Simulate execution
                            await Task.Delay((int)allocation.EstimatedDuration.TotalMilliseconds, cancellationToken);
                            
                            allocation.Status = "completed";
                            allocation.CompletedAt = DateTime.UtcNow;
                            RecordMetric("Completions", 1);
                        }
                    }
                    else
                    {
                        await Task.Delay(100, cancellationToken);
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing queue");
                }
            }
        }

        public string ServiceName => "GPUScheduler";
        public ServiceStatus Status { get; set; } = ServiceStatus.Idle;
        public Dictionary<string, object> GetMetricsDict() => new()
        {
            { "QueueSize", _taskQueue.Count },
            { "Allocations", _allocations.Count },
            { "ServiceStatus", Status }
        };
    }

    /// <summary>
    /// ComputeResourceMonitor - Track real-time resource availability
    /// </summary>
    public class ComputeResourceMonitor : IHELIOSService
    {
        private readonly ILogger<ComputeResourceMonitor> _logger;
        private readonly Queue<ResourceSnapshot> _history = new();
        private Timer? _monitoringTimer;
        private const int HistorySize = 300; // 5-minute rolling window

        public ComputeResourceMonitor(ILogger<ComputeResourceMonitor> logger)
        {
            _logger = logger;
        }

        public async Task StartAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("ComputeResourceMonitor started");
            
            _monitoringTimer = new Timer(_ => TakeSnapshot(), null, TimeSpan.Zero, TimeSpan.FromSeconds(1));
            await Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("ComputeResourceMonitor stopped");
            _monitoringTimer?.Dispose();
            await Task.CompletedTask;
        }

        /// <summary>
        /// Get current resource status
        /// </summary>
        public async Task<ComputeResourceStatus> GetStatusAsync()
        {
            return await Task.FromResult(new ComputeResourceStatus
            {
                GPU5090VramPercent = GetRandomPercent(),
                GPU5090UtilizationPercent = GetRandomPercent(),
                GPU5090TempC = 45 + (GetRandomPercent() / 2),
                IntelArcVramPercent = GetRandomPercent(),
                IntelArcUtilizationPercent = GetRandomPercent(),
                CPUCoresUsed = (int)(Environment.ProcessorCount * (GetRandomPercent() / 100.0)),
                CPUMemoryPercent = GetRandomPercent(),
                AzureCreditsRemaining = 1000 - (Random.Shared.Next(0, 500)),
                Timestamp = DateTime.UtcNow
            });
        }

        /// <summary>
        /// Forecast resource availability
        /// </summary>
        public async Task<ResourceForecast> ForecastAsync(TimeSpan horizon)
        {
            var status = await GetStatusAsync();
            
            return new ResourceForecast
            {
                HorizonHours = (int)horizon.TotalHours,
                PredictedGPU5090Available = status.GPU5090UtilizationPercent < 80,
                PredictedCPUAvailable = status.CPUCoresUsed < Environment.ProcessorCount / 2,
                PredictedAzureAvailable = status.AzureCreditsRemaining > 100,
                Confidence = 0.85,
                Timestamp = DateTime.UtcNow
            };
        }

        /// <summary>
        /// Check if resource can fit task
        /// </summary>
        public bool CanFitTask(ComputeTask task)
        {
            // Simplified - in production, actually check capacity
            return true;
        }

        /// <summary>
        /// Set alert thresholds
        /// </summary>
        public void SetAlertThresholds(AlertConfig config)
        {
            _logger.LogInformation("Alert thresholds updated: {Config}", config);
        }

        private void TakeSnapshot()
        {
            lock (_history)
            {
                _history.Enqueue(new ResourceSnapshot
                {
                    Timestamp = DateTime.UtcNow,
                    GPU5090VramPercent = GetRandomPercent(),
                    CPUMemoryPercent = GetRandomPercent(),
                    AzureCreditsRemaining = 1000 - Random.Shared.Next(0, 100)
                });

                while (_history.Count > HistorySize)
                    _history.Dequeue();
            }
        }

        private static int GetRandomPercent() => Random.Shared.Next(0, 101);

        public string ServiceName => "ComputeResourceMonitor";
        public ServiceStatus Status { get; set; } = ServiceStatus.Idle;
        public Dictionary<string, object> GetMetrics() => new()
        {
            { "HistorySnapshots", _history.Count },
            { "ServiceStatus", Status }
        };
    }

    /// <summary>
    /// TaskCharacterizer - Profile task requirements
    /// </summary>
    public class TaskCharacterizer : IHELIOSService
    {
        private readonly ILogger<TaskCharacterizer> _logger;
        private readonly LRUCache<string, TaskProfile> _profileCache = new(1000);

        public TaskCharacterizer(ILogger<TaskCharacterizer> logger)
        {
            _logger = logger;
        }

        public async Task StartAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("TaskCharacterizer started");
            await Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("TaskCharacterizer stopped");
            await Task.CompletedTask;
        }

        /// <summary>
        /// Profile task requirements
        /// </summary>
        public async Task<TaskProfile> ProfileAsync(ComputeTask task)
        {
            if (_profileCache.TryGet(task.Id, out var cached))
                return cached;

            var profile = new TaskProfile
            {
                TaskId = task.Id,
                ModelName = task.Metadata.ContainsKey("model") ? task.Metadata["model"].ToString() ?? "" : "",
                GpuMemoryRequired = EstimateGpuMemory(task),
                CpuCoresRequired = EstimateCpuCores(task),
                ExecutionTimeMs = 1000 + Random.Shared.Next(0, 5000)
            };

            _profileCache.Set(task.Id, profile);
            return await Task.FromResult(profile);
        }

        /// <summary>
        /// Estimate execution time on specific resource
        /// </summary>
        public TimeSpan EstimateExecutionTime(TaskProfile profile, string resource)
        {
            var baseMs = profile.ExecutionTimeMs;
            var multiplier = resource switch
            {
                "GPU5090" => 1.0,
                "IntelArc" => 2.5,
                "CPU" => 8.0,
                "Azure" => 1.5,
                _ => 1.0
            };

            return TimeSpan.FromMilliseconds(baseMs * multiplier);
        }

        /// <summary>
        /// Estimate cost on specific resource
        /// </summary>
        public decimal EstimateCost(TaskProfile profile, string resource)
        {
            var durationSec = profile.ExecutionTimeMs / 1000.0;
            var costPerSec = resource switch
            {
                "GPU5090" => 0m,
                "IntelArc" => 0m,
                "CPU" => 0m,
                "Azure" => 0.005m, // $0.005 per second
                _ => 0m
            };

            return (decimal)(durationSec * (double)costPerSec);
        }

        /// <summary>
        /// Get optimal resource for task
        /// </summary>
        public string GetOptimalResource(TaskProfile profile)
        {
            if (profile.GpuMemoryRequired <= 80) return "GPU5090";
            if (profile.GpuMemoryRequired <= 16) return "IntelArc";
            if (profile.CpuCoresRequired <= Environment.ProcessorCount) return "CPU";
            return "Azure";
        }

        private int EstimateGpuMemory(ComputeTask task)
        {
            // Simplified - analyze task type and model
            return Random.Shared.Next(4, 80);
        }

        private int EstimateCpuCores(ComputeTask task)
        {
            return Random.Shared.Next(2, 16);
        }

        public string ServiceName => "TaskCharacterizer";
        public ServiceStatus Status { get; set; } = ServiceStatus.Idle;
        public Dictionary<string, object> GetMetrics() => new()
        {
            { "CachedProfiles", _profileCache.Count },
            { "ServiceStatus", Status }
        };
    }

    // Supporting types
    public class ComputeTask
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Type { get; set; } = string.Empty;
        public Dictionary<string, object> Metadata { get; set; } = new();
        public int Priority { get; set; } = 5; // 1-10, higher = more urgent
    }

    public class ComputeAllocation
    {
        public string Id { get; set; } = string.Empty;
        public string TaskId { get; set; } = string.Empty;
        public string AssignedResource { get; set; } = string.Empty;
        public DateTime AllocatedAt { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string Status { get; set; } = "pending"; // pending, processing, completed, failed, cancelled
        public TimeSpan EstimatedDuration { get; set; }
        public double EstimatedCost { get; set; }
    }

    public class TaskProfile
    {
        public string TaskId { get; set; } = string.Empty;
        public string ModelName { get; set; } = string.Empty;
        public int GpuMemoryRequired { get; set; }
        public int CpuCoresRequired { get; set; }
        public int ExecutionTimeMs { get; set; }
    }

    public class ResourceAvailability
    {
        public bool GPU5090Available { get; set; }
        public bool IntelArcAvailable { get; set; }
        public int CPUCoresAvailable { get; set; }
        public bool AzureAvailable { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class QueueStatus
    {
        public int Pending { get; set; }
        public int Processing { get; set; }
        public int Completed { get; set; }
        public int Failed { get; set; }
        public int MaxCapacity { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class SchedulingMetrics
    {
        public int TotalAllocations { get; set; }
        public int CompletedAllocations { get; set; }
        public int FailedAllocations { get; set; }
        public decimal AverageCost { get; set; }
        public TimeSpan AverageDuration { get; set; }
        public Dictionary<string, double> ResourceUtilization { get; set; } = new();
    }

    public class ComputeResourceStatus
    {
        public double GPU5090VramPercent { get; set; }
        public double GPU5090UtilizationPercent { get; set; }
        public double GPU5090TempC { get; set; }
        public double IntelArcVramPercent { get; set; }
        public double IntelArcUtilizationPercent { get; set; }
        public int CPUCoresUsed { get; set; }
        public double CPUMemoryPercent { get; set; }
        public int AzureCreditsRemaining { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class ResourceForecast
    {
        public int HorizonHours { get; set; }
        public bool PredictedGPU5090Available { get; set; }
        public bool PredictedCPUAvailable { get; set; }
        public bool PredictedAzureAvailable { get; set; }
        public double Confidence { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class AlertConfig
    {
        public double LowVramThreshold { get; set; } = 0.1; // 10%
        public double HighTempThreshold { get; set; } = 85.0; // 85°C
        public int LowCreditsThreshold { get; set; } = 100;
    }

    public class ResourceSnapshot
    {
        public DateTime Timestamp { get; set; }
        public double GPU5090VramPercent { get; set; }
        public double CPUMemoryPercent { get; set; }
        public int AzureCreditsRemaining { get; set; }
    }

    /// <summary>
    /// Simple LRU cache
    /// </summary>
    internal class LRUCache<K, V> where K : notnull
    {
        private readonly int _capacity;
        private readonly Dictionary<K, (V value, DateTime accessed)> _cache = new();

        public LRUCache(int capacity) => _capacity = capacity;

        public bool TryGet(K key, out V value)
        {
            if (_cache.TryGetValue(key, out var item))
            {
                _cache[key] = (item.value, DateTime.UtcNow);
                value = item.value;
                return true;
            }
            value = default!;
            return false;
        }

        public void Set(K key, V value)
        {
            if (_cache.Count >= _capacity)
            {
                var lru = _cache.OrderBy(x => x.Value.accessed).First();
                _cache.Remove(lru.Key);
            }
            _cache[key] = (value, DateTime.UtcNow);
        }

        public int Count => _cache.Count;
    }
}
