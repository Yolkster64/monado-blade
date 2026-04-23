# Implementation Guide: Adding a New Profile

## Overview

This guide covers creating and integrating new boot profiles in Monado Blade.

## What is a Profile?

A profile defines boot behavior: which services run, configuration settings, boot sequences, and target systems.

## Creating a Profile

### 1. Profile Structure

Create profile file (JSON format):

```json
{
  "id": "enterprise-deployment",
  "name": "Enterprise Deployment Profile",
  "version": "1.0.0",
  "description": "Profile for enterprise environment deployments",
  "type": "boot",
  "targetSystems": ["Windows10", "Windows11"],
  "bootSequence": [
    {
      "stage": "initialization",
      "timeout": 10000,
      "critical": true
    },
    {
      "stage": "usb-detection",
      "timeout": 5000,
      "critical": true
    },
    {
      "stage": "profile-application",
      "timeout": 30000,
      "critical": true
    }
  ],
  "services": {
    "usb": {
      "enabled": true,
      "config": {
        "autoDetect": true,
        "timeout": 5000
      }
    },
    "update": {
      "enabled": true,
      "config": {
        "checkInterval": 3600000
      }
    }
  },
  "settings": {
    "logging.level": "Information",
    "retry.maxAttempts": 3,
    "retry.backoffMs": 1000
  },
  "requirements": {
    "minimumDiskSpaceMb": 1024,
    "requiresNetwork": false,
    "requiresUSB": true
  }
}
```

### 2. Profile Validation Schema

Create schema file (`profile-schema.json`):

```json
{
  "$schema": "http://json-schema.org/draft-07/schema#",
  "type": "object",
  "required": ["id", "name", "version", "type"],
  "properties": {
    "id": {
      "type": "string",
      "pattern": "^[a-z0-9-]+$",
      "description": "Unique profile identifier"
    },
    "name": {
      "type": "string",
      "minLength": 1,
      "maxLength": 100
    },
    "version": {
      "type": "string",
      "pattern": "^\\d+\\.\\d+\\.\\d+$"
    },
    "description": {
      "type": "string"
    },
    "type": {
      "enum": ["boot", "recovery", "diagnostic", "custom"]
    },
    "targetSystems": {
      "type": "array",
      "items": { "type": "string" }
    },
    "bootSequence": {
      "type": "array",
      "items": {
        "type": "object",
        "required": ["stage"],
        "properties": {
          "stage": { "type": "string" },
          "timeout": { "type": "integer", "minimum": 1000 },
          "critical": { "type": "boolean" }
        }
      }
    },
    "settings": {
      "type": "object",
      "additionalProperties": true
    }
  }
}
```

### 3. Implement Custom Profile Class

Create in `Core/Profiles/`:

```csharp
namespace MonadoBlade.Core.Profiles
{
    /// <summary>
    /// Enterprise deployment profile.
    /// </summary>
    public class EnterpriseDeploymentProfile : Profile
    {
        public EnterpriseDeploymentProfile()
        {
            Id = "enterprise-deployment";
            Name = "Enterprise Deployment Profile";
            Version = "1.0.0";
            Description = "Profile for enterprise environment deployments";
            Type = ProfileType.Boot;
            TargetSystems = new[] { "Windows10", "Windows11" };
        }

        /// <summary>
        /// Validates system meets profile requirements.
        /// </summary>
        public override bool ValidateSystemRequirements()
        {
            // Check disk space
            var diskInfo = DriveInfo.GetDrives()
                .FirstOrDefault(d => d.IsReady && d.DriveFormat == "NTFS");
            
            if (diskInfo?.AvailableFreeSpace < 1024 * 1024 * 1024)
                return false;

            // Check connectivity
            // Check permissions
            
            return true;
        }

        /// <summary>
        /// Initializes profile for current system.
        /// </summary>
        public override async Task InitializeAsync()
        {
            // Setup enterprise-specific services
            // Configure security policies
            // Set enterprise logging
            await Task.CompletedTask;
        }

        /// <summary>
        /// Applies profile configuration.
        /// </summary>
        public override async Task ApplyAsync()
        {
            // Apply boot sequence
            // Configure services
            // Apply settings
            await Task.CompletedTask;
        }

        /// <summary>
        /// Cleans up profile resources.
        /// </summary>
        public override async Task CleanupAsync()
        {
            // Clean temporary files
            // Reset temporary settings
            await Task.CompletedTask;
        }
    }
}
```

### 4. Register Profile

Create profile loader in `Core/Profiles/`:

```csharp
public class ProfileRegistry
{
    private readonly ILogger _logger;
    private readonly Dictionary<string, Type> _profiles;

    public ProfileRegistry(ILogger logger)
    {
        _logger = logger;
        _profiles = new Dictionary<string, Type>();
    }

    /// <summary>
    /// Registers profile type.
    /// </summary>
    public void RegisterProfile<T>(string profileId) where T : Profile
    {
        _profiles[profileId] = typeof(T);
        _logger.LogInfo("Profile registered: {ProfileId}", profileId);
    }

    /// <summary>
    /// Creates profile instance.
    /// </summary>
    public Profile Create(string profileId)
    {
        if (_profiles.TryGetValue(profileId, out var type))
        {
            return (Profile)Activator.CreateInstance(type);
        }

        throw new ProfileNotFoundException($"Profile not found: {profileId}");
    }

    /// <summary>
    /// Loads profile from JSON file.
    /// </summary>
    public Profile LoadFromFile(string filePath)
    {
        var json = File.ReadAllText(filePath);
        var document = JsonDocument.Parse(json);

        var profileId = document.RootElement
            .GetProperty("id").GetString();

        // Validate against schema
        ValidateProfileSchema(document);

        // Create and configure profile
        var profile = Create(profileId);
        profile.ConfigureFromJson(document);

        return profile;
    }

    private void ValidateProfileSchema(JsonDocument document)
    {
        // Load schema
        var schemaJson = File.ReadAllText(
            "Profiles/profile-schema.json");
        var schema = JsonSchema.Parse(schemaJson);

        // Validate document against schema
        // Throw ValidationException if invalid
    }
}
```

### 5. Register in Service Container

In `Core/DependencyInjection/ServiceBootstrapper.cs`:

```csharp
public static void RegisterProfiles(IServiceContainer container)
{
    var registry = new ProfileRegistry(
        container.Resolve<ILogger>());

    // Register built-in profiles
    registry.RegisterProfile<EnterpriseDeploymentProfile>(
        "enterprise-deployment");
    
    registry.RegisterProfile<StandardBootProfile>(
        "standard-boot");
    
    registry.RegisterProfile<RecoveryProfile>(
        "recovery");

    // Load custom profiles from directory
    var profileDir = Path.Combine(
        AppContext.BaseDirectory, "Profiles");
    
    if (Directory.Exists(profileDir))
    {
        foreach (var file in Directory.GetFiles(
            profileDir, "*.json"))
        {
            try
            {
                registry.LoadFromFile(file);
            }
            catch (Exception ex)
            {
                container.Resolve<ILogger>()
                    .LogWarning("Failed to load profile", ex);
            }
        }
    }

    container.RegisterSingleton(registry);
}
```

### 6. Test the Profile

Create tests in `Tests/Profiles/`:

```csharp
[TestFixture]
public class EnterpriseDeploymentProfileTests
{
    private EnterpriseDeploymentProfile _profile;

    [SetUp]
    public void Setup()
    {
        _profile = new EnterpriseDeploymentProfile();
    }

    [Test]
    public void Profile_HasCorrectMetadata()
    {
        Assert.That(_profile.Id, Is.EqualTo("enterprise-deployment"));
        Assert.That(_profile.Type, Is.EqualTo(ProfileType.Boot));
    }

    [Test]
    public void ValidateSystemRequirements_ReturnsTrue()
    {
        var result = _profile.ValidateSystemRequirements();
        Assert.That(result, Is.True);
    }

    [Test]
    public async Task InitializeAsync_Succeeds()
    {
        Assert.DoesNotThrowAsync(
            async () => await _profile.InitializeAsync());
    }

    [Test]
    public async Task ApplyAsync_Succeeds()
    {
        await _profile.InitializeAsync();
        Assert.DoesNotThrowAsync(
            async () => await _profile.ApplyAsync());
    }
}
```

## Profile Distribution

### 1. Package Profile

```bash
# Create profile package
zip -r enterprise-deployment-1.0.0.zip manifest.json profile.json
```

### 2. Create Manifest

```json
{
  "id": "enterprise-deployment",
  "name": "Enterprise Deployment Profile",
  "version": "1.0.0",
  "author": "Organization",
  "description": "Profile for enterprise deployments",
  "targetVersion": "1.0.0+",
  "profiles": ["enterprise-deployment"],
  "signature": "..."
}
```

### 3. Sign Profile (Optional)

```csharp
var signer = new ProfileSigner();
var signature = signer.Sign(profileData, privateKey);
```

## Configuration Best Practices

1. **Naming**: Use lowercase with hyphens (e.g., `enterprise-deployment`)
2. **Versioning**: Use semantic versioning
3. **Documentation**: Include detailed description
4. **Validation**: Specify system requirements
5. **Defaults**: Provide sensible defaults
6. **Flexibility**: Allow service configuration per profile
7. **Testing**: Test on target systems before deployment

## Profile Discovery

Profiles are discovered and loaded automatically:
1. Built-in profiles from registry
2. JSON files in `Profiles/` directory
3. Plugin profiles (if enabled)

```csharp
// Usage
var profileService = container.Resolve<IProfileService>();
var profiles = await profileService.DiscoverProfilesAsync();
var profile = await profileService.GetProfileAsync("enterprise-deployment");
```

## Related Documentation

- ADR-011: Profile Extension Points
- ARCHITECTURE.md: Profile system overview
- API_REFERENCE_COMPREHENSIVE.md: Profile APIs
