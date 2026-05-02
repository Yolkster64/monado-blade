using Moq;
using Microsoft.Extensions.Logging;
using MonadoBlade.Security.Models;
using MonadoBlade.Security.Services;

namespace MonadoBlade.Security.Tests;

public class QuarantineServiceTests
{
    private readonly Mock<ILogger<QuarantineService>> _mockLogger;
    private readonly QuarantineService _service;

    public QuarantineServiceTests()
    {
        _mockLogger = new Mock<ILogger<QuarantineService>>();
        _service = new QuarantineService(_mockLogger.Object);
    }

    [Fact]
    public async Task ShouldQuarantineAsync_ReturnsTrue_ForDangerousExtensions()
    {
        // Act
        var shouldQuarantine = await _service.ShouldQuarantineAsync("malware.exe");

        // Assert
        Assert.True(shouldQuarantine);
    }

    [Fact]
    public async Task ShouldQuarantineAsync_ReturnsFalse_ForSafeExtensions()
    {
        // Act
        var shouldQuarantine = await _service.ShouldQuarantineAsync("document.pdf");

        // Assert
        Assert.False(shouldQuarantine);
    }

    [Fact]
    public async Task GetQuarantineEntryAsync_ReturnsNullForNonExistentEntry()
    {
        // Act
        var entry = await _service.GetQuarantineEntryAsync("non-existent");

        // Assert
        Assert.Null(entry);
    }

    [Fact]
    public async Task ListQuarantineAsync_ReturnsEmptyListInitially()
    {
        // Act
        var entries = await _service.ListQuarantineAsync();

        // Assert
        Assert.NotNull(entries);
        Assert.Empty(entries);
    }

    [Fact]
    public async Task AnalyzeQuarantineEntryAsync_ReturnsUnknownForNonExistentEntry()
    {
        // Act
        var result = await _service.AnalyzeQuarantineEntryAsync("non-existent");

        // Assert
        Assert.Equal("UNKNOWN", result);
    }

    [Fact]
    public async Task GetQuarantineStorageSizeAsync_ReturnsSize()
    {
        // Act
        var size = await _service.GetQuarantineStorageSizeAsync();

        // Assert
        Assert.True(size >= 0);
    }
}
