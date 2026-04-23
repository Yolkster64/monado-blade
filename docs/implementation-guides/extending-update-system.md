# Implementation Guide: Extending the Update System

## Overview

The update system handles firmware, configuration, and application updates. This guide covers adding new update types and handlers.

## Architecture

Update system consists of:
- **Update Discovery**: Finding available updates
- **Update Download**: Downloading update packages
- **Update Validation**: Verifying integrity and signatures
- **Update Application**: Applying update to system
- **Update Verification**: Confirming successful application
- **Rollback**: Reverting if issues detected

## Creating Custom Update Type

### 1. Define Update Handler Interface

```csharp
namespace MonadoBlade.Core.Updates
{
    /// <summary>
    /// Handles specific update type.
    /// </summary>
    public interface IUpdateHandler
    {
        /// <summary>
        /// Update type this handler processes.
        /// </summary>
        UpdateType SupportedType { get; }

        /// <summary>
        /// Prepares for update (e.g., backs up current state).
        /// </summary>
        Task PrepareAsync(Update update);

        /// <summary>
        /// Applies the update.
        /// </summary>
        Task ApplyAsync(Update update);

        /// <summary>
        /// Verifies update applied successfully.
        /// </summary>
        Task<bool> VerifyAsync(Update update);

        /// <summary>
        /// Rolls back update.
        /// </summary>
        Task RollbackAsync(string backupId);

        /// <summary>
        /// Validates update package.
        /// </summary>
        Task<ValidationResult> ValidateUpdateAsync(Update update);
    }

    public enum UpdateType
    {
        Firmware,
        Configuration,
        Application,
        Custom
    }

    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; }
    }
}
```

### 2. Implement Update Handler

```csharp
public class ConfigurationUpdateHandler : IUpdateHandler
{
    private readonly ILogger _logger;
    private readonly IConfigurationService _configService;
    private readonly IBackupService _backupService;

    public UpdateType SupportedType => UpdateType.Configuration;

    public ConfigurationUpdateHandler(
        ILogger logger,
        IConfigurationService configService,
        IBackupService backupService)
    {
        _logger = logger;
        _configService = configService;
        _backupService = backupService;
    }

    public async Task PrepareAsync(Update update)
    {
        _logger.LogInfo("Preparing configuration update: {Update}", 
            update.Name);

        // Create backup of current configuration
        var backupId = await _backupService.CreateBackupAsync(
            BackupType.Configuration,
            "Pre-update backup");

        update.BackupId = backupId;
    }

    public async Task ApplyAsync(Update update)
    {
        _logger.LogInfo("Applying configuration update");

        try
        {
            // Extract configuration from update package
            var newConfig = await ExtractConfigurationAsync(update.PackagePath);

            // Validate new configuration
            var validation = ValidateConfiguration(newConfig);
            if (!validation.IsValid)
            {
                throw new UpdateException(
                    "Configuration validation failed",
                    UpdateErrorCode.InvalidConfiguration);
            }

            // Apply new configuration
            await _configService.ApplyConfigurationAsync(newConfig);

            _logger.LogInfo("Configuration update applied successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError("Configuration update failed", ex);
            throw;
        }
    }

    public async Task<bool> VerifyAsync(Update update)
    {
        _logger.LogInfo("Verifying configuration update");

        try
        {
            var currentConfig = await _configService.GetCurrentConfigAsync();
            var expectedConfig = await ExtractConfigurationAsync(
                update.PackagePath);

            return ConfigurationsMatch(currentConfig, expectedConfig);
        }
        catch (Exception ex)
        {
            _logger.LogError("Configuration verification failed", ex);
            return false;
        }
    }

    public async Task RollbackAsync(string backupId)
    {
        _logger.LogInfo("Rolling back configuration to backup: {BackupId}", 
            backupId);

        try
        {
            var backup = await _backupService.GetBackupAsync(backupId);
            await _configService.ApplyConfigurationAsync(backup.Data);

            _logger.LogInfo("Configuration rollback completed");
        }
        catch (Exception ex)
        {
            _logger.LogError("Configuration rollback failed", ex);
            throw;
        }
    }

    public async Task<ValidationResult> ValidateUpdateAsync(Update update)
    {
        var result = new ValidationResult { IsValid = true, Errors = new List<string>() };

        try
        {
            // Check update package integrity
            if (!await VerifyUpdatePackageAsync(update))
            {
                result.IsValid = false;
                result.Errors.Add("Update package integrity check failed");
                return result;
            }

            // Check update signature
            if (!await VerifySignatureAsync(update))
            {
                result.IsValid = false;
                result.Errors.Add("Update signature verification failed");
                return result;
            }

            // Check version compatibility
            if (!CheckVersionCompatibility(update))
            {
                result.IsValid = false;
                result.Errors.Add("Update is not compatible with current version");
                return result;
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError("Update validation error", ex);
            result.IsValid = false;
            result.Errors.Add(ex.Message);
            return result;
        }
    }

    private async Task<Dictionary<string, string>> ExtractConfigurationAsync(
        string packagePath)
    {
        var config = new Dictionary<string, string>();

        using (var zip = ZipFile.OpenRead(packagePath))
        {
            var configEntry = zip.GetEntry("configuration.json");
            if (configEntry == null)
                throw new UpdateException("Configuration not found in package",
                    UpdateErrorCode.InvalidPackage);

            using (var stream = configEntry.Open())
            using (var reader = new StreamReader(stream))
            {
                var json = await reader.ReadToEndAsync();
                var doc = JsonDocument.Parse(json);

                foreach (var prop in doc.RootElement.EnumerateObject())
                {
                    config[prop.Name] = prop.Value.GetString();
                }
            }
        }

        return config;
    }

    private ValidationResult ValidateConfiguration(
        Dictionary<string, string> config)
    {
        var result = new ValidationResult 
        { 
            IsValid = true, 
            Errors = new List<string>() 
        };

        // Validate required settings
        if (!config.ContainsKey("version"))
        {
            result.IsValid = false;
            result.Errors.Add("Configuration missing version");
        }

        // Validate setting values
        foreach (var setting in config)
        {
            if (string.IsNullOrEmpty(setting.Value))
            {
                result.IsValid = false;
                result.Errors.Add($"Configuration setting empty: {setting.Key}");
            }
        }

        return result;
    }

    private bool ConfigurationsMatch(
        Dictionary<string, string> current,
        Dictionary<string, string> expected)
    {
        if (current.Count != expected.Count)
            return false;

        foreach (var item in expected)
        {
            if (!current.TryGetValue(item.Key, out var value) ||
                value != item.Value)
                return false;
        }

        return true;
    }

    private async Task<bool> VerifyUpdatePackageAsync(Update update)
    {
        try
        {
            using (var zip = ZipFile.OpenRead(update.PackagePath))
            {
                // Check required files exist
                if (zip.GetEntry("configuration.json") == null)
                    return false;
                if (zip.GetEntry("manifest.json") == null)
                    return false;

                return true;
            }
        }
        catch
        {
            return false;
        }
    }

    private async Task<bool> VerifySignatureAsync(Update update)
    {
        // Verify cryptographic signature of update
        // Implementation depends on signing strategy
        return true;
    }

    private bool CheckVersionCompatibility(Update update)
    {
        var currentVersion = new Version("1.0.0");
        var minVersion = new Version(update.MinimumVersion ?? "1.0.0");
        var maxVersion = new Version(update.MaximumVersion ?? "2.0.0");

        return currentVersion >= minVersion && currentVersion <= maxVersion;
    }
}
```

### 3. Register Update Handler

In `Core/DependencyInjection/ServiceBootstrapper.cs`:

```csharp
public static void RegisterUpdateHandlers(IServiceContainer container)
{
    var registry = container.Resolve<IUpdateHandlerRegistry>();

    // Register built-in handlers
    registry.Register(new FirmwareUpdateHandler(...));
    registry.Register(new ConfigurationUpdateHandler(...));
    registry.Register(new ApplicationUpdateHandler(...));
}
```

### 4. Update Service Integration

Update service discovers and uses appropriate handler:

```csharp
public class UpdateService : IUpdateService
{
    private readonly IUpdateHandlerRegistry _handlerRegistry;
    private readonly IEventBus _eventBus;
    private readonly ILogger _logger;

    public async Task ApplyUpdateAsync(Update update, 
        bool createBackup = true)
    {
        try
        {
            var handler = _handlerRegistry.Get(update.Type);
            if (handler == null)
                throw new UpdateException($"No handler for type {update.Type}",
                    UpdateErrorCode.NoHandlerFound);

            // Publish update started event
            await _eventBus.PublishAsync(
                new UpdateStartedEvent(update));

            // Validate update
            var validation = await handler.ValidateUpdateAsync(update);
            if (!validation.IsValid)
                throw new UpdateException(
                    $"Update validation failed: {string.Join(", ", validation.Errors)}",
                    UpdateErrorCode.ValidationFailed);

            // Prepare (backup if needed)
            if (createBackup)
                await handler.PrepareAsync(update);

            // Apply update
            await handler.ApplyAsync(update);

            // Verify
            if (!await handler.VerifyAsync(update))
                throw new UpdateException(
                    "Update verification failed",
                    UpdateErrorCode.VerificationFailed);

            _logger.LogInfo("Update applied successfully: {Update}", 
                update.Name);

            // Publish update completed event
            await _eventBus.PublishAsync(
                new UpdateCompletedEvent(update));
        }
        catch (Exception ex)
        {
            _logger.LogError("Update failed", ex);
            throw;
        }
    }
}
```

### 5. Testing Update Handler

```csharp
[TestFixture]
public class ConfigurationUpdateHandlerTests
{
    private Mock<ILogger> _loggerMock;
    private Mock<IConfigurationService> _configServiceMock;
    private Mock<IBackupService> _backupServiceMock;
    private ConfigurationUpdateHandler _handler;

    [SetUp]
    public void Setup()
    {
        _loggerMock = new Mock<ILogger>();
        _configServiceMock = new Mock<IConfigurationService>();
        _backupServiceMock = new Mock<IBackupService>();

        _handler = new ConfigurationUpdateHandler(
            _loggerMock.Object,
            _configServiceMock.Object,
            _backupServiceMock.Object);
    }

    [Test]
    public async Task ApplyAsync_WithValidConfig_Succeeds()
    {
        // Arrange
        var update = CreateTestUpdate();
        _configServiceMock.Setup(s => s.ApplyConfigurationAsync(It.IsAny<Dictionary<string, string>>()))
            .Returns(Task.CompletedTask);

        // Act
        await _handler.ApplyAsync(update);

        // Assert
        _configServiceMock.Verify(
            s => s.ApplyConfigurationAsync(It.IsAny<Dictionary<string, string>>()),
            Times.Once);
    }

    [Test]
    public async Task RollbackAsync_RestoresPreviousConfig()
    {
        // Arrange
        var backupId = "backup-123";
        var previousConfig = new Dictionary<string, string> { ["key"] = "value" };

        _backupServiceMock.Setup(b => b.GetBackupAsync(backupId))
            .ReturnsAsync(new Backup { Data = previousConfig });

        // Act
        await _handler.RollbackAsync(backupId);

        // Assert
        _configServiceMock.Verify(
            s => s.ApplyConfigurationAsync(previousConfig),
            Times.Once);
    }
}
```

## Update Package Format

Standardized update package format (ZIP):

```
update-package.zip
├── manifest.json          # Update metadata
├── configuration.json     # New configuration (if config update)
├── firmware.bin          # Firmware binary (if firmware update)
├── checksum.sha256       # File checksums
├── signature.sig         # Digital signature
└── readme.md            # Update notes
```

## Update Discovery

Implement custom discovery service:

```csharp
public interface IUpdateDiscovery
{
    Task<IEnumerable<Update>> DiscoverUpdatesAsync();
}

public class OnlineUpdateDiscovery : IUpdateDiscovery
{
    private readonly HttpClient _httpClient;
    private readonly ILogger _logger;

    public async Task<IEnumerable<Update>> DiscoverUpdatesAsync()
    {
        // Query update server
        var response = await _httpClient.GetAsync(
            "https://updates.monodoBlade.com/available");
        
        var json = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);

        // Parse and return available updates
        return ParseUpdates(doc);
    }
}
```

## Related Documentation

- ADR-012: Update Rollback Strategy
- ADR-010: Testing Strategy
- API_REFERENCE_COMPREHENSIVE.md: IUpdateService
