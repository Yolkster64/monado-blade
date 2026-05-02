using Moq;
using Microsoft.Extensions.Logging;
using MonadoBlade.Security.Services;

namespace MonadoBlade.Security.Tests;

public class RegistrySecurityServiceTests
{
    private readonly Mock<ILogger<RegistrySecurityService>> _mockLogger;
    private readonly RegistrySecurityService _service;

    public RegistrySecurityServiceTests()
    {
        _mockLogger = new Mock<ILogger<RegistrySecurityService>>();
        _service = new RegistrySecurityService(_mockLogger.Object);
    }

    [Fact]
    public async Task EnforceUacAsync_ReturnsTrue()
    {
        var result = await _service.EnforceUacAsync();
        Assert.True(result);
    }

    [Fact]
    public async Task HardenServiceConfigurationAsync_ReturnsTrue()
    {
        var result = await _service.HardenServiceConfigurationAsync();
        Assert.True(result);
    }

    [Fact]
    public async Task GetCurrentSecurityConfigAsync_ReturnsConfig()
    {
        var config = await _service.GetCurrentSecurityConfigAsync();
        Assert.NotNull(config);
        Assert.NotEmpty(config);
    }

    [Fact]
    public async Task AuditSecurityComplianceAsync_ReturnsCompliance()
    {
        var compliance = await _service.AuditSecurityComplianceAsync();
        Assert.NotNull(compliance);
        Assert.NotEmpty(compliance);
    }
}
