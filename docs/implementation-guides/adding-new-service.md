# Implementation Guide: Adding a New Service

## Overview

This guide walks through adding a new service to Monado Blade. Services are singleton components that provide specific functionality.

## Prerequisites

- Understanding of Monado Blade architecture (see ARCHITECTURE.md)
- Familiarity with dependency injection pattern
- Review of ADR-001 (Service Architecture)

## Step-by-Step Implementation

### 1. Define the Service Interface

Create interface in `Core/Services/Interfaces/`:

```csharp
namespace MonadoBlade.Core.Services.Interfaces
{
    /// <summary>
    /// Service for managing device firmware.
    /// </summary>
    public interface IFirmwareService
    {
        /// <summary>
        /// Gets current firmware version.
        /// </summary>
        /// <returns>Firmware version string</returns>
        Task<string> GetVersionAsync();

        /// <summary>
        /// Checks firmware integrity.
        /// </summary>
        /// <returns>True if firmware valid, false otherwise</returns>
        Task<bool> VerifyIntegrityAsync();

        /// <summary>
        /// Raised when firmware update detected.
        /// </summary>
        event EventHandler<FirmwareEventArgs> UpdateDetected;
    }

    /// <summary>
    /// Arguments for firmware events.
    /// </summary>
    public class FirmwareEventArgs : EventArgs
    {
        public string Version { get; set; }
        public DateTime DetectedAt { get; set; }
    }
}
```

**Key Points**:
- Document all public methods with XML comments
- Include exception documentation
- Define domain-specific events
- Keep interface focused on single responsibility

### 2. Implement the Service

Create implementation in `Core/Services/Implementation/`:

```csharp
namespace MonadoBlade.Core.Services.Implementation
{
    /// <summary>
    /// Implements firmware management service.
    /// </summary>
    public class FirmwareService : IFirmwareService
    {
        private readonly ILogger _logger;
        private readonly IUSBService _usbService;
        private readonly IEventBus _eventBus;
        private string _cachedVersion;
        private DateTime _versionCacheTime;
        private const int CacheValidityMs = 30000;

        public event EventHandler<FirmwareEventArgs> UpdateDetected;

        /// <summary>
        /// Initializes firmware service.
        /// </summary>
        public FirmwareService(
            ILogger logger,
            IUSBService usbService,
            IEventBus eventBus)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _usbService = usbService ?? throw new ArgumentNullException(nameof(usbService));
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));

            // Subscribe to relevant events
            _usbService.DeviceConnected += OnDeviceConnected;
        }

        public async Task<string> GetVersionAsync()
        {
            // Check cache
            if (!string.IsNullOrEmpty(_cachedVersion) &&
                (DateTime.UtcNow - _versionCacheTime).TotalMilliseconds < CacheValidityMs)
            {
                return _cachedVersion;
            }

            try
            {
                _logger.LogDebug("Retrieving firmware version");
                var devices = await _usbService.EnumerateDevicesAsync();
                var device = devices.FirstOrDefault();

                if (device == null)
                {
                    throw new FirmwareException(
                        "No USB device found",
                        FirmwareErrorCode.DeviceNotFound);
                }

                // Read firmware version from device
                byte[] versionBytes = await device.ReadAsync(64, timeoutMs: 5000);
                _cachedVersion = Encoding.UTF8.GetString(versionBytes).TrimEnd('\0');
                _versionCacheTime = DateTime.UtcNow;

                _logger.LogInfo("Firmware version retrieved: {Version}", _cachedVersion);
                return _cachedVersion;
            }
            catch (USBException ex)
            {
                _logger.LogError("Failed to read firmware version", ex);
                throw new FirmwareException(
                    "Failed to read from device",
                    FirmwareErrorCode.ReadFailed,
                    ex);
            }
        }

        public async Task<bool> VerifyIntegrityAsync()
        {
            try
            {
                _logger.LogDebug("Verifying firmware integrity");
                var devices = await _usbService.EnumerateDevicesAsync();
                var device = devices.FirstOrDefault();

                if (device == null)
                    return false;

                // Send integrity check command
                byte[] command = new byte[] { 0x01, 0x02, 0x03 }; // Example command
                await device.WriteAsync(command, timeoutMs: 5000);

                // Read result
                byte[] result = await device.ReadAsync(1, timeoutMs: 5000);
                bool isValid = result[0] == 0x00;

                _logger.LogInfo("Firmware integrity check: {Valid}", isValid ? "Valid" : "Invalid");
                return isValid;
            }
            catch (Exception ex)
            {
                _logger.LogError("Firmware integrity check failed", ex);
                throw new FirmwareException(
                    "Integrity verification failed",
                    FirmwareErrorCode.VerificationFailed,
                    ex);
            }
        }

        private void OnDeviceConnected(object sender, USBDeviceEventArgs e)
        {
            _logger.LogDebug("Device connected: {DeviceName}", e.Device.Name);
            
            // Invalidate cache
            _cachedVersion = null;

            // Check for firmware updates
            _ = CheckForUpdatesAsync();
        }

        private async Task CheckForUpdatesAsync()
        {
            try
            {
                string version = await GetVersionAsync();
                // Check against latest version
                // If different, raise UpdateDetected event
                UpdateDetected?.Invoke(this, new FirmwareEventArgs 
                { 
                    Version = version,
                    DetectedAt = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Failed to check for updates", ex);
            }
        }

        public void Dispose()
        {
            _usbService.DeviceConnected -= OnDeviceConnected;
        }
    }
}
```

**Key Points**:
- Inject dependencies through constructor
- Validate parameters with null checks
- Log operations at appropriate levels
- Handle errors with domain-specific exceptions
- Implement caching with validation
- Subscribe to relevant events

### 3. Define Custom Exceptions

Create in `Core/Exceptions/`:

```csharp
namespace MonadoBlade.Core.Exceptions
{
    /// <summary>
    /// Firmware-related exception.
    /// </summary>
    public class FirmwareException : MonadoBladeException
    {
        public FirmwareErrorCode ErrorCode { get; }

        public FirmwareException(
            string message,
            FirmwareErrorCode errorCode,
            Exception innerException = null)
            : base(message, (int)errorCode, innerException)
        {
            ErrorCode = errorCode;
        }
    }

    /// <summary>
    /// Firmware error codes (5000 range).
    /// </summary>
    public enum FirmwareErrorCode
    {
        DeviceNotFound = 5001,
        ReadFailed = 5002,
        WriteFailed = 5003,
        VerificationFailed = 5004,
        VersionMismatch = 5005
    }
}
```

### 4. Register Service

In `Core/DependencyInjection/ServiceBootstrapper.cs`:

```csharp
public static void RegisterServices(IServiceContainer container)
{
    // ... existing registrations ...
    
    // Register firmware service
    container.Register<IFirmwareService, FirmwareService>(
        Lifetime.Singleton);
}
```

### 5. Create Tests

Create unit tests in `Tests/Services/`:

```csharp
[TestFixture]
public class FirmwareServiceTests
{
    private Mock<ILogger> _loggerMock;
    private Mock<IUSBService> _usbServiceMock;
    private Mock<IEventBus> _eventBusMock;
    private FirmwareService _service;

    [SetUp]
    public void Setup()
    {
        _loggerMock = new Mock<ILogger>();
        _usbServiceMock = new Mock<IUSBService>();
        _eventBusMock = new Mock<IEventBus>();

        _service = new FirmwareService(
            _loggerMock.Object,
            _usbServiceMock.Object,
            _eventBusMock.Object);
    }

    [Test]
    public async Task GetVersionAsync_WithDeviceConnected_ReturnsVersion()
    {
        // Arrange
        var mockDevice = new Mock<IUSBDevice>();
        mockDevice.Setup(d => d.ReadAsync(It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(Encoding.UTF8.GetBytes("1.2.3\0"));

        _usbServiceMock.Setup(s => s.EnumerateDevicesAsync())
            .ReturnsAsync(new[] { mockDevice.Object });

        // Act
        var version = await _service.GetVersionAsync();

        // Assert
        Assert.That(version, Is.EqualTo("1.2.3"));
        mockDevice.Verify(d => d.ReadAsync(It.IsAny<int>(), It.IsAny<int>()), Times.Once);
    }

    [Test]
    public void GetVersionAsync_WithNoDevice_ThrowsFirmwareException()
    {
        // Arrange
        _usbServiceMock.Setup(s => s.EnumerateDevicesAsync())
            .ReturnsAsync(Array.Empty<IUSBDevice>());

        // Act & Assert
        Assert.ThrowsAsync<FirmwareException>(
            async () => await _service.GetVersionAsync());
    }
}
```

### 6. Update Documentation

Add to relevant documentation files:
- Add service to architecture diagram
- Document service usage in implementation guides
- Add to API reference
- Document configuration requirements

## Best Practices

1. **Single Responsibility**: Service should handle one concern
2. **Dependency Injection**: All dependencies injected via constructor
3. **Error Handling**: Use domain-specific exceptions
4. **Logging**: Log at Debug/Info for normal flow, Warning/Error for issues
5. **Caching**: Implement with TTL and invalidation
6. **Testing**: Write tests for happy path, error conditions, edge cases
7. **Documentation**: Include XML comments for all public members
8. **Events**: Publish events for significant state changes

## Common Issues and Solutions

### Issue: Service doesn't initialize
**Solution**: Verify all dependencies are registered in ServiceBootstrapper

### Issue: Circular dependencies
**Solution**: Rethink service design; use events for loose coupling

### Issue: Service not injected
**Solution**: Verify interface is registered with container

## Related Documentation

- ADR-001: Service-Oriented Architecture
- ADR-007: Event-Driven Architecture
- ARCHITECTURE.md: System overview
- API_REFERENCE_COMPREHENSIVE.md: Service interfaces
