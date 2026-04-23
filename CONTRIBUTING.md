# CONTRIBUTING.md

## Welcome

Thank you for your interest in contributing to Monado Blade! This document provides guidelines for contributing code, documentation, and other improvements.

## Code of Conduct

Be respectful and professional. We're building an inclusive community for everyone.

## Getting Started

### Prerequisites

- .NET 6.0 or higher
- Visual Studio 2022 or VS Code
- Git
- Understanding of boot systems or USB management (helpful but not required)

### Development Setup

```bash
# Clone repository
git clone https://github.com/yourusername/monado-blade.git
cd monado-blade

# Install dependencies
dotnet restore

# Build solution
dotnet build

# Run tests
dotnet test
```

## Contribution Process

### 1. Fork and Branch

```bash
# Create feature branch
git checkout -b feature/your-feature-name

# Or for bug fixes
git checkout -b fix/issue-description
```

Branch naming conventions:
- `feature/descriptive-name`: New features
- `fix/issue-description`: Bug fixes
- `docs/what-changed`: Documentation updates
- `refactor/what-improved`: Code improvements
- `test/what-tested`: Test additions

### 2. Development

**Code Style Guide**:

```csharp
// Use meaningful names
private readonly IUSBService _usbService; // Good
private IUS _us; // Bad

// Add XML documentation to public members
/// <summary>
/// Initializes the boot service.
/// </summary>
public BootService(ILogger logger)
{
    _logger = logger ?? throw new ArgumentNullException(nameof(logger));
}

// Follow single responsibility principle
public class BootService { } // Good, focused
public class BootAndUSBAndUpdateService { } // Bad, too many responsibilities

// Use dependency injection
public MyClass(IService service) { } // Good
var service = new Service(); // Bad in production code

// Error handling
try
{
    // Operation
}
catch (USBException ex) when (ex.ErrorCode == USBErrorCode.DeviceNotFound)
{
    _logger.LogWarning("Device not found", ex);
    // Handle specific error
}
catch (MonadoBladeException ex)
{
    _logger.LogError("Operation failed", ex);
    throw;
}

// Async patterns
public async Task<Result> DoWorkAsync()
{
    return await _service.ProcessAsync();
}

// LINQ where appropriate
var devices = _devices
    .Where(d => d.IsConnected)
    .OrderBy(d => d.Name)
    .ToList();
```

**Commit Message Format**:

```
<type>(<scope>): <subject>

<body>

<footer>

Co-authored-by: Copilot <223556219+Copilot@users.noreply.github.com>
```

Types:
- `feat`: New feature
- `fix`: Bug fix
- `docs`: Documentation
- `style`: Code style (no functional change)
- `refactor`: Code refactoring
- `test`: Test additions
- `chore`: Build, dependencies

Examples:
```
feat(usb): Add support for MTP protocol

- Implement MTP protocol handler
- Add device enumeration for MTP devices
- Update USB service to support MTP

Relates to: ADR-006

Co-authored-by: Copilot <223556219+Copilot@users.noreply.github.com>
```

```
fix(boot): Prevent invalid state transitions

- Add validation for boot state transitions
- Fix race condition in state machine
- Add tests for state transitions

Fixes: #123

Co-authored-by: Copilot <223556219+Copilot@users.noreply.github.com>
```

### 3. Testing Requirements

**Test Coverage**:
- Minimum 80% code coverage for new code
- 100% coverage for critical paths
- 100% coverage for error handling
- 100% coverage for security code

**Test Types**:
1. Unit tests (fast, isolated)
2. Integration tests (service interactions)
3. System tests (full workflows)
4. Performance tests (for optimization PRs)

**Test Structure**:
```csharp
[TestFixture]
public class MyServiceTests
{
    private Mock<IDependency> _dependencyMock;
    private MyService _service;

    [SetUp]
    public void Setup()
    {
        _dependencyMock = new Mock<IDependency>();
        _service = new MyService(_dependencyMock.Object);
    }

    // Arrange-Act-Assert pattern
    [Test]
    public async Task DoWork_WithValidInput_ReturnsResult()
    {
        // Arrange
        var input = CreateValidInput();
        _dependencyMock.Setup(d => d.Process(input))
            .ReturnsAsync(new Result());

        // Act
        var result = await _service.DoWorkAsync(input);

        // Assert
        Assert.That(result, Is.Not.Null);
        _dependencyMock.Verify(d => d.Process(input), Times.Once);
    }

    [Test]
    public void DoWork_WithInvalidInput_ThrowsException()
    {
        // Arrange
        var input = CreateInvalidInput();

        // Act & Assert
        Assert.ThrowsAsync<ArgumentException>(
            async () => await _service.DoWorkAsync(input));
    }
}
```

**Run Tests Before Push**:
```bash
# Run all tests
dotnet test

# Run tests with coverage
dotnet test /p:CollectCoverage=true

# Run specific test
dotnet test --filter "MethodName"
```

### 4. Create Pull Request

```bash
# Push your branch
git push origin feature/your-feature-name
```

**PR Description Template**:
```markdown
## Description
Brief description of changes

## Type
- [ ] Feature
- [ ] Bug fix
- [ ] Documentation
- [ ] Refactoring

## Related Issues
Fixes #123

## Changes
- Change 1
- Change 2
- Change 3

## Testing
- [ ] Unit tests added/updated
- [ ] Integration tests passing
- [ ] Test coverage maintained

## Documentation
- [ ] Updated relevant docs
- [ ] Updated API reference if needed
- [ ] Added ADR if design decision

## Checklist
- [ ] Code follows style guide
- [ ] No breaking changes (or documented)
- [ ] Commits follow format
- [ ] All tests passing
- [ ] No console errors
```

### 5. Review Process

- **Code Review**: At least 2 approvals required
- **Test Coverage**: Must maintain minimum 80%
- **Documentation**: ADRs updated if applicable
- **Performance**: No significant regression
- **Security**: Security review for sensitive code

Reviewers will:
- Check code quality
- Verify tests are adequate
- Review for security issues
- Ensure documentation updated

## Documentation Contributions

### Update Existing Documentation

1. Locate relevant file in `docs/`
2. Make changes
3. Follow Markdown conventions
4. Test links are valid
5. Submit PR with documentation changes

### Add New Documentation

1. Determine appropriate location
2. Use consistent formatting
3. Include examples
4. Link to related documents
5. Update TABLE OF CONTENTS

### Create ADR for Design Decisions

1. Check if ADR already exists
2. Copy ADR template
3. Number sequentially
4. Follow decision record format
5. Get consensus before merging

## Reporting Issues

Use GitHub Issues to report bugs:

**Bug Report Template**:
```markdown
## Description
What happened?

## Steps to Reproduce
1. Step 1
2. Step 2
3. Step 3

## Expected Behavior
What should happen?

## Actual Behavior
What actually happened?

## Environment
- OS: Windows 11
- .NET Version: 6.0
- Monado Blade Version: 1.0.0

## Logs
[Paste relevant logs here]

## Additional Context
Any other relevant information?
```

**Feature Request Template**:
```markdown
## Description
What feature would you like?

## Use Case
Why do you need this?

## Proposed Solution
How should it work?

## Alternatives Considered
Are there other approaches?
```

## Performance Contribution

For performance improvements:

1. Run performance tests before/after
2. Document performance impact
3. Provide benchmarks
4. Include profiling data if significant

## Security Contribution

For security issues:

1. **Do not** open public issue
2. Email: security@monado-blade.com
3. Include detailed description
4. Provide reproduction steps
5. Suggest fix if possible

## Related References

- [ADR Format](docs/adr/)
- [Code Style Guide](#code-style-guide)
- [Architecture](docs/ARCHITECTURE.md)
- [API Reference](docs/API_REFERENCE_COMPREHENSIVE.md)

## Questions?

- Check [Troubleshooting](docs/knowledge-base/troubleshooting.md)
- Review [Architecture](docs/ARCHITECTURE.md)
- Search existing issues
- Ask in discussions

Thank you for contributing!
