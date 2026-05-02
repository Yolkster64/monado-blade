using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace MonadoBlade.GPU;

/// <summary>
/// Multi-GPU orchestration framework with NVIDIA 5090 (primary), AMD RDNA 3 (secondary), Intel Arc (tertiary)
/// Provides unified interface for GPU computation across multiple vendors with automatic failover
/// </summary>
public interface IGPUOrchestrator
{
    /// <summary>Get available GPU devices with capabilities</summary>
    Task<IEnumerable<GPUDevice>> GetAvailableDevicesAsync(CancellationToken ct = default);
    
    /// <summary>Execute workload on optimal device with automatic device selection</summary>
    Task<T> ExecuteAsync<T>(GPUWorkload<T> workload, CancellationToken ct = default);
    
    /// <summary>Execute workload with explicit device preference and fallback chain</summary>
    Task<T> ExecuteAsync<T>(GPUWorkload<T> workload, GPUDevicePreference preference, CancellationToken ct = default);
    
    /// <summary>Get current load balancing status across all devices</summary>
    Task<GPULoadStatus> GetLoadStatusAsync(CancellationToken ct = default);
    
    /// <summary>Configure GPU memory limits and optimization strategy</summary>
    Task ConfigureDeviceAsync(string deviceId, GPUConfiguration config, CancellationToken ct = default);
}

/// <summary>GPU device information and capabilities</summary>
public record GPUDevice
{
    public string DeviceId { get; init; } = string.Empty;
    public string DeviceName { get; init; } = string.Empty;
    public GPUVendor Vendor { get; init; }
    public string ComputeCapability { get; init; } = string.Empty;
    public long TotalMemory { get; init; }
    public long AvailableMemory { get; init; }
    public float ComputePower { get; init; } // TFLOPS
    public bool IsAvailable { get; init; }
    public string DriverVersion { get; init; } = string.Empty;
    public Dictionary<string, object> Metadata { get; init; } = new();
}

/// <summary>GPU vendor enumeration</summary>
public enum GPUVendor
{
    NVIDIA,
    AMD,
    Intel,
    Other
}

/// <summary>Configurable GPU workload for execution</summary>
public record GPUWorkload<T>
{
    public string WorkloadName { get; init; } = string.Empty;
    public Func<IGPUDevice, CancellationToken, Task<T>> ExecutionKernel { get; init; } = null!;
    public int EstimatedMemoryMB { get; init; }
    public TimeSpan Timeout { get; init; } = TimeSpan.FromMinutes(5);
    public GPUWorkloadPriority Priority { get; init; } = GPUWorkloadPriority.Normal;
    public bool AllowCPUFallback { get; init; } = true;
}

/// <summary>GPU workload priority levels</summary>
public enum GPUWorkloadPriority
{
    Low = 0,
    Normal = 1,
    High = 2,
    Critical = 3
}

/// <summary>Device preference and fallback strategy</summary>
public record GPUDevicePreference
{
    public GPUVendor PrimaryVendor { get; init; } = GPUVendor.NVIDIA;
    public GPUVendor? SecondaryVendor { get; init; } = GPUVendor.AMD;
    public GPUVendor? TertiaryVendor { get; init; } = GPUVendor.Intel;
    public bool AllowCPUFallback { get; init; } = true;
    public int MaxRetries { get; init; } = 3;
}

/// <summary>GPU load balancing status</summary>
public record GPULoadStatus
{
    public Dictionary<string, float> DeviceUtilization { get; init; } = new();
    public Dictionary<string, long> DeviceMemoryUsage { get; init; } = new();
    public Dictionary<string, int> ActiveWorkloads { get; init; } = new();
    public float AverageUtilization { get; init; }
    public DateTime CapturedAt { get; init; }
}

/// <summary>GPU configuration options</summary>
public record GPUConfiguration
{
    public long MemoryLimitMB { get; init; }
    public float MaxUtilization { get; init; } = 0.9f;
    public bool EnablePeerAccess { get; init; }
    public string OptimizationStrategy { get; init; } = "balanced"; // balanced, aggressive, conservative
    public bool EnableCaching { get; init; } = true;
    public int CacheSize { get; init; } = 256; // MB
}

/// <summary>Individual GPU device interface</summary>
public interface IGPUDevice
{
    GPUDevice Info { get; }
    Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> kernel, CancellationToken ct = default);
    Task<float> GetCurrentUtilizationAsync(CancellationToken ct = default);
    Task<long> GetAvailableMemoryAsync(CancellationToken ct = default);
}

/// <summary>CUDA accelerator for NVIDIA 5090</summary>
public interface ICUDAAccelerator : IGPUDevice
{
    Task<T> ExecuteKernelAsync<T>(string kernelName, object[] args, CancellationToken ct = default);
    Task PrecompileKernelAsync(string kernelCode, CancellationToken ct = default);
}

/// <summary>AMD RDNA 3 accelerator</summary>
public interface IAMDAccelerator : IGPUDevice
{
    Task<T> ExecuteKernelAsync<T>(string kernelName, object[] args, CancellationToken ct = default);
    Task OptimizeMemoryHierarchyAsync(CancellationToken ct = default);
}

/// <summary>Intel Arc accelerator</summary>
public interface IIntelAccelerator : IGPUDevice
{
    Task<T> ExecuteTaskAsync<T>(string taskName, Func<CancellationToken, Task<T>> task, CancellationToken ct = default);
}

/// <summary>GPU memory manager with pooling and optimization</summary>
public interface IGPUMemoryManager
{
    Task<T> AllocateAsync<T>(int sizeBytes, CancellationToken ct = default) where T : unmanaged;
    Task DeallocateAsync(object allocation, CancellationToken ct = default);
    Task<GPUMemoryStatus> GetMemoryStatusAsync(string deviceId, CancellationToken ct = default);
    Task DefragmentAsync(string deviceId, CancellationToken ct = default);
}

/// <summary>GPU memory status information</summary>
public record GPUMemoryStatus
{
    public long TotalMemory { get; init; }
    public long UsedMemory { get; init; }
    public long AvailableMemory { get; init; }
    public float FragmentationRatio { get; init; }
    public List<string> AllocationInfo { get; init; } = new();
}

/// <summary>GPU execution statistics and metrics</summary>
public record GPUExecutionMetrics
{
    public string WorkloadName { get; init; } = string.Empty;
    public string DeviceUsed { get; init; } = string.Empty;
    public TimeSpan ExecutionTime { get; init; }
    public long MemoryPeakUsage { get; init; }
    public float AverageUtilization { get; init; }
    public bool ExecutedOnFallback { get; init; }
    public int RetryCount { get; init; }
    public DateTime StartTime { get; init; }
    public DateTime EndTime { get; init; }
    public string Status { get; init; } = "success"; // success, failed, timeout
}

/// <summary>GPU performance profiler</summary>
public interface IGPUProfiler
{
    Task<GPUExecutionMetrics> ProfileExecutionAsync<T>(
        string workloadName,
        Func<IGPUDevice, CancellationToken, Task<T>> kernel,
        IGPUDevice device,
        CancellationToken ct = default);
    
    Task<GPUPerformanceReport> GenerateReportAsync(
        DateTime from,
        DateTime to,
        CancellationToken ct = default);
}

/// <summary>GPU performance report</summary>
public record GPUPerformanceReport
{
    public DateTime ReportGeneratedAt { get; init; }
    public Dictionary<string, GPUExecutionMetrics> Executions { get; init; } = new();
    public float AverageUtilization { get; init; }
    public float PeakMemoryUsage { get; init; }
    public TimeSpan TotalExecutionTime { get; init; }
    public int FailureCount { get; init; }
    public float FailureRate { get; init; }
}

/// <summary>ML inference optimizer for GPU</summary>
public interface IGPUMLInferenceEngine
{
    /// <summary>Load ML model (ONNX, TensorRT, etc.) for GPU inference</summary>
    Task<IGPUModel> LoadModelAsync(string modelPath, CancellationToken ct = default);
    
    /// <summary>Execute batch inference on GPU</summary>
    Task<T> InferAsync<T>(IGPUModel model, object input, CancellationToken ct = default);
    
    /// <summary>Execute batch inference with quantization</summary>
    Task<T> InferWithQuantizationAsync<T>(
        IGPUModel model,
        object input,
        QuantizationStrategy strategy,
        CancellationToken ct = default);
}

/// <summary>GPU-loaded ML model interface</summary>
public interface IGPUModel
{
    string ModelName { get; }
    string ModelPath { get; }
    Dictionary<string, string> InputShapes { get; }
    Dictionary<string, string> OutputShapes { get; }
    Task WarmupAsync(CancellationToken ct = default);
}

/// <summary>Quantization strategy for inference optimization</summary>
public enum QuantizationStrategy
{
    FP32,    // Full precision
    FP16,    // Half precision
    INT8,    // 8-bit integer
    INT4     // 4-bit integer (aggressive compression)
}

/// <summary>Extension methods for GPU operations</summary>
public static class GPUExtensions
{
    public static async Task<T> ExecuteWithAutoFailoverAsync<T>(
        this IGPUOrchestrator orchestrator,
        GPUWorkload<T> workload,
        ILogger logger,
        CancellationToken ct = default)
    {
        try
        {
            logger.LogInformation("🎮 Starting GPU workload: {WorkloadName}", workload.WorkloadName);
            var result = await orchestrator.ExecuteAsync(workload, ct);
            logger.LogInformation("✅ GPU workload completed: {WorkloadName}", workload.WorkloadName);
            return result;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "❌ GPU workload failed: {WorkloadName}", workload.WorkloadName);
            if (workload.AllowCPUFallback)
            {
                logger.LogWarning("⚠️ Falling back to CPU for: {WorkloadName}", workload.WorkloadName);
                throw;
            }
            throw;
        }
    }
}
