# API Reference - Monado Blade

Complete API reference for Monado Blade public interfaces, classes, and methods.

## Table of Contents
1. [Core Services](#core-services)
2. [USB Management](#usb-management)
3. [Boot Management](#boot-management)
4. [Update Management](#update-management)
5. [Profile Management](#profile-management)
6. [Event System](#event-system)
7. [Configuration](#configuration)
8. [Security](#security)
9. [Logging](#logging)

---

## Core Services

### IServiceContainer

Service container for dependency injection.

**Namespace**: `MonadoBlade.Core.DependencyInjection`

**Methods**:
- `T Resolve<T>() where T : class`
  - Resolves an instance of type T
  - Returns: Instance of T registered in container
  - Throws: ServiceNotRegisteredException if not registered

- `void Register<TInterface, TImplementation>(Lifetime lifetime = Lifetime.Transient) where TImplementation : TInterface`
  - Registers a service mapping
  - Parameters:
    - TInterface: Interface to register
    - TImplementation: Implementation class
    - lifetime: Object lifetime (Transient, Scoped, Singleton)
  - Example:
    ```csharp
    container.Register<IUSBService, USBService>(Lifetime.Singleton);
    ```

- `void RegisterSingleton<T>(T instance) where T : class`
  - Registers singleton instance
  - Parameters:
    - T: Service type and instance
  - Example:
    ```csharp
    container.RegisterSingleton<IConfiguration>(config);
    ```

---

## USB Management

### IUSBService

Main interface for USB device management.

**Namespace**: `MonadoBlade.Core.Services.Interfaces`

**Methods**:
- `Task<IEnumerable<IUSBDevice>> EnumerateDevicesAsync()`
  - Discovers all connected USB devices
  - Returns: List of detected USB devices
  - Throws: USBException on enumeration failure
  - Example:
    ```csharp
    var devices = await usbService.EnumerateDevicesAsync();
    foreach (var device in devices)
    {
        Console.WriteLine($"Found: {device.Name}");
    }
    ```

- `Task<bool> IsDeviceConnectedAsync(string deviceId)`
  - Checks if specific device is connected
  - Parameters:
    - deviceId: Unique device identifier
  - Returns: True if device connected, false otherwise
  - Example:
    ```csharp
    bool isConnected = await usbService.IsDeviceConnectedAsync("device-123");
    ```

- `event EventHandler<USBDeviceEventArgs> DeviceConnected`
  - Raised when USB device connected
  - Example:
    ```csharp
    usbService.DeviceConnected += (sender, args) => 
    {
        Console.WriteLine($"Device connected: {args.Device.Name}");
    };
    ```

### IUSBDevice

Represents a USB device.

**Namespace**: `MonadoBlade.Core.Services.Interfaces.USB`

**Properties**:
- `string Id` (read-only): Unique device identifier
- `string Name` (read-only): Device display name
- `USBDeviceType Type` (read-only): Device type (MTP, UMS, Custom)
- `int VendorId` (read-only): USB vendor ID
- `int ProductId` (read-only): USB product ID
- `bool IsConnected` (read-only): Current connection status

**Methods**:
- `Task<byte[]> ReadAsync(int length, int timeoutMs = 5000)`
  - Reads data from device
  - Parameters:
    - length: Bytes to read
    - timeoutMs: Operation timeout in milliseconds
  - Returns: Byte array of data read
  - Throws: USBException on read failure, TimeoutException on timeout

- `Task WriteAsync(byte[] data, int timeoutMs = 5000)`
  - Writes data to device
  - Parameters:
    - data: Bytes to write
    - timeoutMs: Operation timeout
  - Throws: USBException on write failure

---

## Boot Management

### IBootService

Manages boot sequence and state transitions.

**Namespace**: `MonadoBlade.Core.Services.Interfaces`

**Methods**:
- `Task InitializeBootAsync(Profile profile)`
  - Starts boot sequence with specified profile
  - Parameters:
    - profile: Profile to boot with
  - Returns: Completed when boot sequence starts
  - Throws: BootException on initialization failure
  - Example:
    ```csharp
    var profile = profileService.GetProfile("default");
    await bootService.InitializeBootAsync(profile);
    ```

- `BootState CurrentState` (property)
  - Gets current boot state
  - Returns: Current BootState value

- `IEnumerable<BootState> GetValidTransitions()`
  - Gets valid next states from current state
  - Returns: List of valid target states

**Events**:
- `event EventHandler<BootStateChangedEventArgs> StateChanged`
  - Raised when boot state changes
  - Example:
    ```csharp
    bootService.StateChanged += (s, e) =>
    {
        Console.WriteLine($"Boot: {e.PreviousState} -> {e.NewState}");
    };
    ```

### BootState (Enum)

**Values**:
- `Uninitialized = 0`: Initial state
- `Initializing = 1`: Boot initializing
- `WaitingForUSB = 2`: Waiting for device
- `USBDetected = 3`: Device detected
- `ApplicationStarted = 4`: Boot app started
- `Running = 5`: Boot running
- `Complete = 6`: Boot complete
- `Failed = 7`: Boot failed
- `Cancelled = 8`: Boot cancelled

---

## Update Management

### IUpdateService

Manages firmware and profile updates.

**Namespace**: `MonadoBlade.Core.Services.Interfaces`

**Methods**:
- `Task<UpdateCheckResult> CheckForUpdatesAsync()`
  - Checks for available updates
  - Returns: UpdateCheckResult with available updates
  - Throws: UpdateException on check failure
  - Example:
    ```csharp
    var result = await updateService.CheckForUpdatesAsync();
    if (result.UpdatesAvailable)
    {
        foreach (var update in result.Updates)
        {
            Console.WriteLine($"Available: {update.Name} v{update.Version}");
        }
    }
    ```

- `Task ApplyUpdateAsync(Update update, bool createBackup = true)`
  - Applies an update
  - Parameters:
    - update: Update to apply
    - createBackup: Whether to create rollback backup
  - Returns: Completed when update applied
  - Throws: UpdateException on failure

- `Task RollbackAsync(string backupId)`
  - Rolls back to previous state
  - Parameters:
    - backupId: Backup to restore from
  - Returns: Completed when rollback complete
  - Throws: RollbackException on failure

- `UpdateState CurrentState` (property)
  - Gets current update state
  - Returns: Current UpdateState value

---

## Profile Management

### IProfileService

Manages boot profiles.

**Namespace**: `MonadoBlade.Core.Services.Interfaces`

**Methods**:
- `Task<IEnumerable<Profile>> DiscoverProfilesAsync()`
  - Discovers available profiles
  - Returns: List of available profiles
  - Example:
    ```csharp
    var profiles = await profileService.DiscoverProfilesAsync();
    foreach (var profile in profiles)
    {
        Console.WriteLine($"Profile: {profile.Name}");
    }
    ```

- `Task<Profile> GetProfileAsync(string profileId)`
  - Gets specific profile by ID
  - Parameters:
    - profileId: Profile identifier
  - Returns: Profile instance
  - Throws: ProfileNotFoundException if not found

- `Task ApplyProfileAsync(Profile profile)`
  - Applies profile configuration
  - Parameters:
    - profile: Profile to apply
  - Returns: Completed when applied
  - Throws: ProfileException on failure

- `Profile CreateProfile(string name, ProfileType type)`
  - Creates new profile
  - Parameters:
    - name: Profile name
    - type: Profile type (Boot, Recovery, etc.)
  - Returns: New Profile instance
  - Example:
    ```csharp
    var profile = profileService.CreateProfile("CustomBoot", ProfileType.Boot);
    profile.Settings["timeout"] = "30";
    ```

---

## Event System

### IEventBus

Centralized event publishing and subscription.

**Namespace**: `MonadoBlade.Core.Events`

**Methods**:
- `void Subscribe<TEvent>(IEventHandler<TEvent> handler) where TEvent : DomainEvent`
  - Subscribes to event type
  - Parameters:
    - TEvent: Event type
    - handler: Event handler implementation
  - Example:
    ```csharp
    eventBus.Subscribe<USBDeviceConnectedEvent>(
        new MyUSBConnectedHandler()
    );
    ```

- `void Unsubscribe<TEvent>(IEventHandler<TEvent> handler) where TEvent : DomainEvent`
  - Unsubscribes from event
  - Parameters:
    - TEvent: Event type
    - handler: Handler to remove

- `Task PublishAsync<TEvent>(TEvent @event) where TEvent : DomainEvent`
  - Publishes event to all subscribers
  - Parameters:
    - @event: Event to publish
  - Example:
    ```csharp
    var evt = new BootStateChangedEvent(newState, oldState);
    await eventBus.PublishAsync(evt);
    ```

### DomainEvent

Base class for all domain events.

**Properties**:
- `Guid Id` (read-only): Event identifier
- `DateTime Timestamp` (read-only): When event occurred
- `string CorrelationId` (read-only): Correlation ID for tracing
- `Dictionary<string, object> Metadata` (read-only): Event metadata

---

## Configuration

### IConfiguration

Application configuration interface.

**Namespace**: `MonadoBlade.Core.Configuration`

**Methods**:
- `T Get<T>(string key, T defaultValue = default)`
  - Gets configuration value
  - Parameters:
    - key: Configuration key (dot notation)
    - defaultValue: Value if not found
  - Returns: Configuration value or default
  - Example:
    ```csharp
    var timeout = config.Get<int>("usb.timeout", 5000);
    var bootProfile = config.Get<string>("boot.defaultProfile");
    ```

- `void Set<T>(string key, T value)`
  - Sets configuration value (runtime only, not persisted)
  - Parameters:
    - key: Configuration key
    - value: Value to set

**Properties**:
- `string AppVersion` (read-only): Application version
- `string EnvironmentName` (read-only): Current environment (Development, Staging, Production)

---

## Security

### IAuthenticationService

User authentication and authorization.

**Namespace**: `MonadoBlade.Core.Security`

**Methods**:
- `Task<AuthenticationResult> AuthenticateAsync(string username, string password)`
  - Authenticates user
  - Parameters:
    - username: User identifier
    - password: User password
  - Returns: AuthenticationResult with token on success
  - Throws: AuthenticationException on failure

- `Task<bool> AuthorizeAsync(string userId, string permission)`
  - Checks if user has permission
  - Parameters:
    - userId: User identifier
    - permission: Permission to check
  - Returns: True if authorized, false otherwise

- `Task<ClaimsPrincipal> GetCurrentUserAsync()`
  - Gets current user principal
  - Returns: ClaimsPrincipal for current user

---

## Logging

### ILogger

Structured logging interface.

**Namespace**: `MonadoBlade.Core.Logging`

**Methods**:
- `void LogDebug(string message, params object[] args)`
  - Logs debug message
  - Example:
    ```csharp
    logger.LogDebug("Device enumeration starting for {DeviceType}", "MTP");
    ```

- `void LogInfo(string message, params object[] args)`
  - Logs information message
  - Example:
    ```csharp
    logger.LogInfo("Boot sequence completed in {Duration}ms", elapsed);
    ```

- `void LogWarning(string message, Exception ex = null, params object[] args)`
  - Logs warning message
  - Example:
    ```csharp
    logger.LogWarning("Device disconnected unexpectedly", null, deviceId);
    ```

- `void LogError(string message, Exception ex, params object[] args)`
  - Logs error message
  - Example:
    ```csharp
    logger.LogError("Update failed: {Error}", exception, errorCode);
    ```

---

## Common Usage Patterns

### Discovering and Using USB Devices

```csharp
var container = new ServiceContainer();
container.Register<IUSBService, USBService>(Lifetime.Singleton);

var usbService = container.Resolve<IUSBService>();
usbService.DeviceConnected += (s, e) => 
    Console.WriteLine($"Device connected: {e.Device.Name}");

var devices = await usbService.EnumerateDevicesAsync();
foreach (var device in devices)
{
    byte[] data = await device.ReadAsync(256);
    // Process data
}
```

### Boot with Profile

```csharp
var bootService = container.Resolve<IBootService>();
var profileService = container.Resolve<IProfileService>();

bootService.StateChanged += (s, e) =>
    Console.WriteLine($"Boot state: {e.NewState}");

var profile = await profileService.GetProfileAsync("default");
await bootService.InitializeBootAsync(profile);

while (bootService.CurrentState != BootState.Complete)
{
    await Task.Delay(100);
}
```

### Subscribing to Events

```csharp
var eventBus = container.Resolve<IEventBus>();

eventBus.Subscribe<USBDeviceConnectedEvent>(
    new MyUSBConnectedHandler());

eventBus.Subscribe<BootStateChangedEvent>(
    new MyBootStateHandler());

// Events published by services automatically
```

---

## Error Handling

All public methods throw documented exceptions. Use try-catch for error handling:

```csharp
try
{
    await bootService.InitializeBootAsync(profile);
}
catch (BootException ex)
{
    logger.LogError("Boot failed", ex);
    // Handle boot-specific error
}
catch (MonadoBladeException ex)
{
    logger.LogError("Operation failed", ex);
    // Handle general Monado Blade error
}
```

---

## Thread Safety

- **Synchronous methods**: Thread-safe, can be called from any thread
- **Async methods**: Safe to call from any thread
- **Event handlers**: Called on event publishing thread; keep logic simple
- **Configuration**: Immutable after initialization; safe to read from any thread

---

## Performance Considerations

- USB reads/writes: Add appropriate timeouts to prevent indefinite hangs
- Boot sequences: Can take 5-10 seconds; run on background task
- Profile discovery: Cached; use cache invalidation events for updates
- Large data transfers: Stream rather than load entirely in memory

