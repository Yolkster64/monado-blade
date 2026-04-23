# MonadoBlade - Core Infrastructure & Testing Framework

## Overview

MonadoBlade is a comprehensive .NET 8.0 application with a solid foundation for multi-team development. This document covers the infrastructure, testing framework, and project structure.

## 🏗️ Project Structure

```
MonadoBlade/
├── src/                              # Source code
│   ├── MonadoBlade.Core/            # Core infrastructure & logging
│   ├── MonadoBlade.Security/        # Security team modules
│   ├── MonadoBlade.Graphics/        # Graphics team modules
│   ├── MonadoBlade.Boot/            # Boot team modules
│   ├── MonadoBlade.Dashboard/       # Dashboard team modules
│   ├── MonadoBlade.Audio/           # Audio team modules
│   ├── MonadoBlade.Developer/       # Developer tools team modules
│   ├── MonadoBlade.Tools/           # Tools team modules
│   └── MonadoBlade.GUI/             # Main WPF application
├── tests/                            # Test suites
│   ├── MonadoBlade.Tests.Unit/      # Unit tests
│   ├── MonadoBlade.Tests.Integration/ # Integration tests
│   └── MonadoBlade.Tests.Performance/ # Performance tests
├── build/                            # Build scripts
│   ├── build.ps1                    # Build script
│   ├── test.ps1                     # Test runner
│   └── publish.ps1                  # Publication script
├── .github/workflows/               # CI/CD pipelines
│   ├── build.yml                    # Build workflow
│   ├── test.yml                     # Test workflow
│   └── release.yml                  # Release workflow
├── docs/                             # Documentation
├── appsettings.json                 # Main configuration
├── appsettings.Development.json     # Development configuration
├── appsettings.Production.json      # Production configuration
└── MonadoBlade.sln                  # Visual Studio solution
```

## 🚀 Getting Started

### Prerequisites

- .NET 8.0 SDK
- Visual Studio 2022 Community+ or VS Code
- Git
- PowerShell 7+ (for build scripts)

### Installation

1. Clone the repository:
```bash
git clone https://github.com/monadoBlade/monadoBlade.git
cd MonadoBlade
```

2. Restore dependencies:
```bash
dotnet restore
```

3. Build the solution:
```bash
# Using build script
.\build\build.ps1 -Configuration Debug

# Or with dotnet
dotnet build MonadoBlade.sln --configuration Debug
```

## 🧪 Testing

### Run All Tests

```bash
# Using test script
.\build\test.ps1 -TestType All

# Or with dotnet
dotnet test MonadoBlade.sln --configuration Debug
```

### Run Specific Test Suites

```bash
# Unit tests only
.\build\test.ps1 -TestType Unit

# Integration tests only
.\build\test.ps1 -TestType Integration

# Performance tests only
.\build\test.ps1 -TestType Performance
```

### Generate Coverage Reports

```bash
.\build\test.ps1 -Coverage
```

## 📊 Infrastructure Components

### Core Module (MonadoBlade.Core)

The core module provides essential infrastructure:

#### Logging
- **Serilog** integration for structured logging
- Console and file sinks with rolling intervals
- Configurable log levels per environment

```csharp
using MonadoBlade.Core.Logging;

var logger = LoggingConfiguration.CreateLogger("MyApp");
logger.Information("Application started");
```

#### Configuration
- Environment-based configuration loading
- JSON-based appsettings files
- Strongly-typed configuration binding

```csharp
using MonadoBlade.Core.Configuration;

var config = ConfigurationExtensions.BuildConfiguration();
var value = config["Section:Key"];
```

#### Dependency Injection
- Microsoft.Extensions.DependencyInjection
- Automatic core infrastructure registration
- Service lifecycle management

```csharp
using MonadoBlade.Core.DependencyInjection;

var services = new ServiceCollection();
services.AddCoreInfrastructure(configuration, "MyApp");
var provider = services.BuildServiceProvider();
```

### Service Abstractions

Base interfaces for all modules:

- **IService**: Marker interface for all services
- **IInitializable**: For services requiring initialization
- **ILifecycleService**: Complete lifecycle management with async disposal

## 📚 Dependency Management

### NuGet Packages

Core dependencies:

| Package | Version | Purpose |
|---------|---------|---------|
| Serilog | 3.1.1 | Structured logging |
| Microsoft.Extensions.* | 8.0.0 | DI & Configuration |
| xUnit | 2.6.6 | Unit testing |
| Moq | 4.20.70 | Mocking framework |
| FluentAssertions | 6.12.0 | Assertion library |
| BenchmarkDotNet | 0.13.12 | Performance benchmarking |

### Circular Dependency Prevention

Architecture ensures no circular dependencies:

```
Core (no dependencies on other modules)
  ↑
  └─ All other modules depend on Core
Security, Graphics, Boot, Dashboard, Audio, Developer, Tools
  ↑
  └─ GUI depends on all modules
```

## 🔧 Build Targets

### Debug Build

```bash
dotnet build MonadoBlade.sln --configuration Debug
```

Features:
- Full debug symbols
- Optimizations disabled
- XML documentation generation
- Detailed logging enabled

### Release Build

```bash
dotnet build MonadoBlade.sln --configuration Release
```

Features:
- Optimizations enabled
- Smaller binaries
- Production logging levels
- Code analysis enabled

## 📦 Publishing

### Publish Application

```bash
# Using publish script
.\build\publish.ps1

# Or with dotnet
dotnet publish src/MonadoBlade.GUI/MonadoBlade.GUI.csproj `
  --configuration Release `
  --output ./publish `
  --runtime win-x64
```

## 🔄 CI/CD Pipelines

### GitHub Actions Workflows

#### Build Workflow (.github/workflows/build.yml)
- Triggers on push and PR
- Tests on Windows with .NET 8.0
- Generates Debug and Release builds
- Uploads build logs

#### Test Workflow (.github/workflows/test.yml)
- Runs parallel test suites (Unit, Integration, Performance)
- Collects code coverage
- Uploads coverage to Codecov
- Generates test reports

#### Release Workflow (.github/workflows/release.yml)
- Triggers on version tags (v*)
- Builds Release configuration
- Runs full test suite
- Creates GitHub release
- Uploads artifacts

## 🎯 Quality Metrics

### Testing Standards

- **Unit Tests**: 50+ tests covering core infrastructure
- **Integration Tests**: 20+ tests for system integration
- **Performance Tests**: Benchmarks for critical paths
- **Coverage Target**: >80% code coverage

### Build Validation

- ✅ Solution builds without warnings
- ✅ All tests pass
- ✅ No circular dependencies
- ✅ Code coverage meets threshold
- ✅ Documentation complete

## 📝 Module Integration Guide

### For Security Team

```csharp
// Implement ISecurityService
public class SecurityService : ISecurityService
{
    private readonly ILogger<SecurityService> _logger;
    
    public SecurityService(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<SecurityService>();
    }
}

// Register in DI
services.AddScoped<ISecurityService, SecurityService>();
```

### For Graphics Team

```csharp
// Implement IGraphicsService
public class GraphicsService : IGraphicsService
{
    // Access core infrastructure through DI
}

// Register in DI
services.AddScoped<IGraphicsService, GraphicsService>();
```

### For All Teams

1. Create implementations in your module
2. Implement base interfaces (IService, etc.)
3. Register in DependencyInjection extension methods
4. Add unit tests with 80%+ coverage
5. Create integration tests for system interactions

## 🧠 Best Practices

### Logging

```csharp
// ✅ Good: Structured logging with context
_logger.LogInformation("User {UserId} logged in", userId);

// ❌ Bad: String concatenation
_logger.LogInformation("User " + userId + " logged in");
```

### Configuration

```csharp
// ✅ Good: Strongly-typed
var settings = configuration.GetSection<AppSettings>("App");

// ❌ Bad: Magic strings
var value = configuration["App:Setting"];
```

### Dependency Injection

```csharp
// ✅ Good: Interface injection
public MyService(ILogger<MyService> logger) { }

// ❌ Bad: Service locator
var logger = serviceProvider.GetService<ILogger>();
```

### Testing

```csharp
// ✅ Good: Clear test names and arrange/act/assert
[Fact]
public void LoginService_WithValidCredentials_ReturnsToken()
{
    // Arrange
    // Act
    // Assert
}

// ❌ Bad: Ambiguous test names
[Fact]
public void Test1() { }
```

## 📖 Documentation

- **README.md**: This file
- **CONTRIBUTING.md**: Contribution guidelines
- **DEVELOPMENT.md**: Development setup and workflow
- **Module READMEs**: Individual module documentation

## 🤝 Contributing

1. Create a feature branch: `git checkout -b feature/description`
2. Make your changes with tests
3. Ensure all tests pass: `dotnet test`
4. Commit with descriptive messages
5. Push and create a pull request

## 📄 License

MIT License - see LICENSE file for details

## 🆘 Support

- **Issues**: GitHub Issues
- **Discussions**: GitHub Discussions
- **Documentation**: See /docs folder

---

**Version**: 1.0.0  
**Last Updated**: 2024  
**Status**: Stable
