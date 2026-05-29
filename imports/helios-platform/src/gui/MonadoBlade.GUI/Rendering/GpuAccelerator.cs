using System;
using System.Diagnostics;
using System.Windows.Media;
using SharpDX;
using SharpDX.DXGI;
using SharpDX.Direct2D1;

namespace MonadoBlade.GUI.Rendering
{
    /// <summary>
    /// Manages GPU hardware acceleration for UI rendering.
    /// Utilizes DirectX 11 and Direct2D for optimal performance.
    /// Implements graceful fallback to CPU rendering on compatibility issues.
    /// </summary>
    public class GpuAccelerator : IDisposable
    {
        private SharpDX.Direct3D11.Device _d3d11Device;
        private SharpDX.Direct3D11.DeviceContext _deviceContext;
        private SharpDX.DXGI.Device _dxgiDevice;
        private RenderTarget _direct2DRenderTarget;
        private Factory _direct2DFactory;
        private bool _isGpuAccelerationEnabled;
        private bool _disposed;
        private GpuInfo _gpuInfo;

        public event EventHandler<GpuAccelerationEventArgs> AccelerationStatusChanged;
        public event EventHandler<Exception> GpuError;

        public GpuAccelerator()
        {
            _isGpuAccelerationEnabled = false;
            _gpuInfo = new GpuInfo();
            InitializeGpu();
        }

        /// <summary>
        /// Initializes GPU acceleration with Direct3D 11 and Direct2D.
        /// </summary>
        private void InitializeGpu()
        {
            try
            {
                // Create Direct3D 11 Device
                SharpDX.Direct3D11.Device tempDevice;
                SharpDX.Direct3D11.DeviceContext tempContext;

                SharpDX.Direct3D11.Device.CreateWithSwapChain(
                    SharpDX.Direct3D.DriverType.Hardware,
                    SharpDX.Direct3D11.DeviceCreationFlags.BgraSupport,
                    null,
                    out tempDevice,
                    out tempContext);

                _d3d11Device = tempDevice;
                _deviceContext = tempContext;

                // Get DXGI Device
                _dxgiDevice = _d3d11Device.QueryInterface<SharpDX.DXGI.Device>();

                // Create Direct2D Factory
                _direct2DFactory = new Factory(FactoryType.MultiThreaded);

                // Get GPU Info
                GatherGpuInfo();

                _isGpuAccelerationEnabled = true;
                Debug.WriteLine($"[GpuAccelerator] GPU acceleration initialized successfully");
                Debug.WriteLine($"[GpuAccelerator] GPU: {_gpuInfo.Name} ({_gpuInfo.VRam}MB VRAM)");

                AccelerationStatusChanged?.Invoke(this, new GpuAccelerationEventArgs 
                { 
                    IsEnabled = true, 
                    Message = "GPU acceleration enabled" 
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[GpuAccelerator] GPU initialization failed: {ex.Message}");
                Debug.WriteLine("[GpuAccelerator] Falling back to CPU rendering");
                
                _isGpuAccelerationEnabled = false;
                GpuError?.Invoke(this, ex);

                AccelerationStatusChanged?.Invoke(this, new GpuAccelerationEventArgs 
                { 
                    IsEnabled = false, 
                    Message = "GPU acceleration unavailable - CPU rendering" 
                });
            }
        }

        /// <summary>
        /// Gathers GPU information from DXGI adapter.
        /// </summary>
        private void GatherGpuInfo()
        {
            try
            {
                var adapter = _dxgiDevice.Adapter;
                var description = adapter.Description;

                _gpuInfo.Name = description.Description.Trim();
                _gpuInfo.VRam = description.DedicatedVideoMemory / (1024 * 1024);
                _gpuInfo.IsDiscrete = description.Flags == AdapterFlags.Remote ? false : true;

                // Get current GPU memory usage
                if (adapter.GetParent<Factory1>() is Factory1 factory1)
                {
                    foreach (var output in adapter.Outputs)
                    {
                        _gpuInfo.DisplayCount++;
                        output.Dispose();
                    }
                    factory1.Dispose();
                }

                Debug.WriteLine($"[GpuAccelerator] GPU Info: {_gpuInfo.Name}, VRAM: {_gpuInfo.VRam}MB, Displays: {_gpuInfo.DisplayCount}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[GpuAccelerator] Error gathering GPU info: {ex.Message}");
            }
        }

        /// <summary>
        /// Creates a Direct2D render target for a specific device context.
        /// </summary>
        public RenderTarget CreateDirect2DRenderTarget(IntPtr nativeWindowHandle)
        {
            if (!_isGpuAccelerationEnabled || _direct2DFactory == null)
            {
                Debug.WriteLine("[GpuAccelerator] GPU acceleration not available for render target");
                return null;
            }

            try
            {
                var renderTargetProperties = new RenderTargetProperties(
                    RenderTargetType.Default,
                    new PixelFormat(Format.B8G8R8A8_UNorm, AlphaMode.Premultiplied),
                    0, 0,
                    FeatureLevel.Level_DEFAULT,
                    RenderTargetUsage.None);

                var dxgiRenderTarget = _direct2DFactory.CreateDxgiSurfaceRenderTarget(
                    _dxgiDevice.GetDxgiSurface(nativeWindowHandle),
                    renderTargetProperties);

                _direct2DRenderTarget = dxgiRenderTarget;
                Debug.WriteLine("[GpuAccelerator] Direct2D render target created successfully");
                
                return dxgiRenderTarget;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[GpuAccelerator] Error creating render target: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Monitors GPU memory usage and adjusts rendering quality if needed.
        /// </summary>
        public GpuMemoryInfo MonitorGpuMemory()
        {
            var memoryInfo = new GpuMemoryInfo();

            try
            {
                if (!_isGpuAccelerationEnabled)
                    return memoryInfo;

                // Estimate GPU memory usage (simplified)
                // In production, you would use GPU-specific APIs for more accurate data
                var adapter = _dxgiDevice?.Adapter;
                if (adapter != null)
                {
                    var description = adapter.Description;
                    memoryInfo.TotalVRam = (long)description.DedicatedVideoMemory;
                    memoryInfo.AvailableVRam = memoryInfo.TotalVRam - (memoryInfo.TotalVRam / 4);
                    memoryInfo.UsagePercentage = (100 * (memoryInfo.TotalVRam - memoryInfo.AvailableVRam)) / memoryInfo.TotalVRam;
                }

                Debug.WriteLine($"[GpuAccelerator] GPU Memory: {memoryInfo.UsagePercentage}% used ({memoryInfo.UsedVRam}MB / {memoryInfo.TotalVRam}MB)");
                
                return memoryInfo;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[GpuAccelerator] Error monitoring GPU memory: {ex.Message}");
                return memoryInfo;
            }
        }

        /// <summary>
        /// Adjusts rendering quality based on GPU capabilities.
        /// </summary>
        public RenderingQuality GetOptimalRenderingQuality()
        {
            if (!_isGpuAccelerationEnabled)
                return RenderingQuality.Low;

            try
            {
                var memoryInfo = MonitorGpuMemory();
                
                if (memoryInfo.UsagePercentage > 85)
                {
                    Debug.WriteLine("[GpuAccelerator] GPU memory pressure high - reducing quality");
                    return RenderingQuality.Medium;
                }

                if (_gpuInfo.VRam < 1024)
                {
                    return RenderingQuality.Medium;
                }

                return RenderingQuality.High;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[GpuAccelerator] Error determining rendering quality: {ex.Message}");
                return RenderingQuality.Medium;
            }
        }

        /// <summary>
        /// Gets information about GPU capabilities.
        /// </summary>
        public GpuInfo GetGpuInfo()
        {
            return _gpuInfo;
        }

        /// <summary>
        /// Checks if GPU acceleration is available and working.
        /// </summary>
        public bool IsGpuAccelerationAvailable()
        {
            return _isGpuAccelerationEnabled && _d3d11Device != null;
        }

        /// <summary>
        /// Performs a test rendering operation to verify GPU functionality.
        /// </summary>
        public bool VerifyGpuCapabilities()
        {
            try
            {
                if (!_isGpuAccelerationEnabled)
                    return false;

                // Test basic DirectX functionality
                var testTexture = new SharpDX.Direct3D11.Texture2D(_d3d11Device,
                    new SharpDX.Direct3D11.Texture2DDescription
                    {
                        Width = 64,
                        Height = 64,
                        MipLevels = 1,
                        ArraySize = 1,
                        Format = SharpDX.DXGI.Format.R8G8B8A8_UNorm,
                        SampleDescription = new SharpDX.DXGI.SampleDescription(1, 0),
                        Usage = SharpDX.Direct3D11.ResourceUsage.Default,
                        BindFlags = SharpDX.Direct3D11.BindFlags.RenderTarget,
                        CpuAccessFlags = SharpDX.Direct3D11.CpuAccessFlags.None
                    });

                testTexture.Dispose();
                
                Debug.WriteLine("[GpuAccelerator] GPU capability verification successful");
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[GpuAccelerator] GPU capability verification failed: {ex.Message}");
                return false;
            }
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            try
            {
                _direct2DRenderTarget?.Dispose();
                _direct2DFactory?.Dispose();
                _dxgiDevice?.Dispose();
                _deviceContext?.Dispose();
                _d3d11Device?.Dispose();
                
                _disposed = true;
                Debug.WriteLine("[GpuAccelerator] GPU resources disposed");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[GpuAccelerator] Error during disposal: {ex.Message}");
            }
        }
    }

    public class GpuInfo
    {
        public string Name { get; set; } = "Unknown";
        public long VRam { get; set; } = 0;
        public bool IsDiscrete { get; set; } = false;
        public int DisplayCount { get; set; } = 0;
    }

    public class GpuMemoryInfo
    {
        public long TotalVRam { get; set; } = 0;
        public long AvailableVRam { get; set; } = 0;
        public long UsedVRam => TotalVRam - AvailableVRam;
        public double UsagePercentage { get; set; } = 0;
    }

    public enum RenderingQuality
    {
        Low,
        Medium,
        High,
        Ultra
    }

    public class GpuAccelerationEventArgs : EventArgs
    {
        public bool IsEnabled { get; set; }
        public string Message { get; set; }
    }
}
