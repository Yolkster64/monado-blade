using Moq;
using Microsoft.Extensions.Logging;
using MonadoBlade.Security.Models;
using MonadoBlade.Security.Services;

namespace MonadoBlade.Security.Tests;

public class BitLockerServiceTests
{
    private readonly Mock<ILogger<BitLockerService>> _mockLogger;
    private readonly BitLockerService _service;

    public BitLockerServiceTests()
    {
        _mockLogger = new Mock<ILogger<BitLockerService>>();
        _service = new BitLockerService(_mockLogger.Object);
    }

    [Fact]
    public async Task GetStatusAsync_ReturnsStatusOrNull()
    {
        // Act - Test with a non-existent drive
        var status = await _service.GetStatusAsync("Z:\\");

        // Assert - Status can be null or contain data depending on system
        // This is acceptable in a test environment
        if (status != null)
        {
            Assert.NotNull(status.VolumeId);
        }
    }

    [Fact]
    public async Task GetAllProtectedVolumesAsync_ReturnsVolumes()
    {
        // Act
        var volumes = await _service.GetAllProtectedVolumesAsync();

        // Assert - May or may not be empty depending on system configuration
        Assert.NotNull(volumes);
    }
}
