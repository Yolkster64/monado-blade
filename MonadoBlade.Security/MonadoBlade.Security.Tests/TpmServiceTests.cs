using Moq;
using Microsoft.Extensions.Logging;
using MonadoBlade.Security.Services;

namespace MonadoBlade.Security.Tests;

public class TpmServiceTests
{
    private readonly Mock<ILogger<TpmService>> _mockLogger;
    private readonly TpmService _service;

    public TpmServiceTests()
    {
        _mockLogger = new Mock<ILogger<TpmService>>();
        _service = new TpmService(_mockLogger.Object);
    }

    [Fact]
    public async Task GetStatusAsync_ReturnsValidStatus()
    {
        var status = await _service.GetStatusAsync();
        Assert.NotNull(status);
        Assert.Equal("2.0", status.SpecVersion);
    }

    [Fact]
    public async Task GetFirmwareVersionAsync_ReturnsVersion()
    {
        var version = await _service.GetFirmwareVersionAsync();
        Assert.NotNull(version);
    }

    [Fact]
    public async Task GetPcrAsync_ReturnsValidPcr()
    {
        var pcr = await _service.GetPcrAsync(0);
        Assert.NotNull(pcr);
        Assert.Equal(0, pcr.Index);
        Assert.Equal("SHA256", pcr.HashAlgorithm);
    }

    [Fact]
    public async Task SealDataAsync_SealsBytesSuccessfully()
    {
        byte[] data = System.Text.Encoding.UTF8.GetBytes("Test data");
        var sealed_data = await _service.SealDataAsync(data, new[] { 0, 1, 2 });
        Assert.NotNull(sealed_data);
        Assert.True(sealed_data.Length > data.Length);
    }

    [Fact]
    public async Task UnsealDataAsync_UnsealsBytesSuccessfully()
    {
        byte[] data = System.Text.Encoding.UTF8.GetBytes("Test data");
        var sealed_data = await _service.SealDataAsync(data, new[] { 0, 1, 2 });
        var unsealed = await _service.UnsealDataAsync(sealed_data);
        Assert.NotNull(unsealed);
        Assert.Equal(data, unsealed);
    }

    [Fact]
    public async Task UnsealDataAsync_ThrowsOnInvalidData()
    {
        byte[] invalidData = new byte[16];
        await Assert.ThrowsAsync<MonadoBlade.Security.Exceptions.TpmException>(
            () => _service.UnsealDataAsync(invalidData));
    }
}
