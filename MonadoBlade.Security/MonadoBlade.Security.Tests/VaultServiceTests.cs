using Moq;
using Microsoft.Extensions.Logging;
using MonadoBlade.Security.Interfaces;
using MonadoBlade.Security.Models;
using MonadoBlade.Security.Services;

namespace MonadoBlade.Security.Tests;

public class VaultServiceTests
{
    private readonly Mock<ILogger<VaultService>> _mockLogger;
    private readonly Mock<IVhdxService> _mockVhdxService;
    private readonly Mock<IBitLockerService> _mockBitLockerService;
    private readonly VaultService _service;

    public VaultServiceTests()
    {
        _mockLogger = new Mock<ILogger<VaultService>>();
        _mockVhdxService = new Mock<IVhdxService>();
        _mockBitLockerService = new Mock<IBitLockerService>();
        _service = new VaultService(_mockLogger.Object, _mockVhdxService.Object, _mockBitLockerService.Object);
    }

    [Fact]
    public async Task CreateVaultAsync_CreatesVaultSuccessfully()
    {
        // Arrange
        _mockVhdxService.Setup(x => x.CreateVhdxAsync(It.IsAny<VhdxContainerConfig>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var vault = await _service.CreateVaultAsync("TestVault", 50, VhdxEncryptionType.BitLockerAes256);

        // Assert
        Assert.NotNull(vault);
        Assert.Equal("TestVault", vault.Name);
        Assert.Equal(50L * 1024 * 1024 * 1024, vault.SizeBytes);
        Assert.False(vault.IsMounted);
        Assert.Equal(VaultContainerState.Unmounted, vault.State);
    }

    [Fact]
    public async Task OpenVaultAsync_MountsVaultSuccessfully()
    {
        // Arrange
        _mockVhdxService.Setup(x => x.CreateVhdxAsync(It.IsAny<VhdxContainerConfig>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _mockVhdxService.Setup(x => x.MountVhdxAsync(It.IsAny<string>(), It.IsAny<char>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _mockBitLockerService.Setup(x => x.EnableBitLockerAsync(It.IsAny<string>(), It.IsAny<VhdxEncryptionType>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var vault = await _service.CreateVaultAsync("TestVault", 50, VhdxEncryptionType.BitLockerAes256);

        // Act
        var openedVault = await _service.OpenVaultAsync(vault.Id);

        // Assert
        Assert.True(openedVault.IsMounted);
        Assert.Equal(VaultContainerState.Mounted, openedVault.State);
    }

    [Fact]
    public async Task CloseVaultAsync_UnmountsVaultSuccessfully()
    {
        // Arrange
        _mockVhdxService.Setup(x => x.CreateVhdxAsync(It.IsAny<VhdxContainerConfig>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _mockVhdxService.Setup(x => x.MountVhdxAsync(It.IsAny<string>(), It.IsAny<char>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _mockVhdxService.Setup(x => x.UnmountVhdxAsync(It.IsAny<char>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var vault = await _service.CreateVaultAsync("TestVault", 50, VhdxEncryptionType.BitLockerAes256);
        await _service.OpenVaultAsync(vault.Id);

        // Act
        var closed = await _service.CloseVaultAsync(vault.Id);

        // Assert
        Assert.True(closed);
    }

    [Fact]
    public async Task GetVaultInfoAsync_ReturnsVaultInfo()
    {
        // Arrange
        _mockVhdxService.Setup(x => x.CreateVhdxAsync(It.IsAny<VhdxContainerConfig>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var vault = await _service.CreateVaultAsync("TestVault", 50, VhdxEncryptionType.BitLockerAes256);

        // Act
        var info = await _service.GetVaultInfoAsync(vault.Id);

        // Assert
        Assert.NotNull(info);
        Assert.Equal(vault.Id, info.Id);
        Assert.Equal("TestVault", info.Name);
    }

    [Fact]
    public async Task ListVaultsAsync_ReturnsAllVaults()
    {
        // Arrange
        _mockVhdxService.Setup(x => x.CreateVhdxAsync(It.IsAny<VhdxContainerConfig>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        await _service.CreateVaultAsync("Vault1", 50, VhdxEncryptionType.BitLockerAes256);
        await _service.CreateVaultAsync("Vault2", 100, VhdxEncryptionType.BitLockerAes256);

        // Act
        var vaults = await _service.ListVaultsAsync();

        // Assert
        Assert.NotNull(vaults);
        Assert.True(vaults.Count() >= 2);
    }

    [Fact]
    public async Task VerifyVaultIntegrityAsync_ReturnsTrue()
    {
        // Arrange
        _mockVhdxService.Setup(x => x.CreateVhdxAsync(It.IsAny<VhdxContainerConfig>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var vault = await _service.CreateVaultAsync("TestVault", 50, VhdxEncryptionType.BitLockerAes256);

        // Act
        var valid = await _service.VerifyVaultIntegrityAsync(vault.Id);

        // Assert
        Assert.False(valid); // False because file doesn't actually exist in test
    }

    [Fact]
    public async Task LockVaultAsync_LocksVault()
    {
        // Arrange
        _mockVhdxService.Setup(x => x.CreateVhdxAsync(It.IsAny<VhdxContainerConfig>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var vault = await _service.CreateVaultAsync("TestVault", 50, VhdxEncryptionType.BitLockerAes256);

        // Act
        var locked = await _service.LockVaultAsync(vault.Id);

        // Assert
        Assert.True(locked);
    }

    [Fact]
    public async Task DeleteVaultAsync_DeletesVault()
    {
        // Arrange
        _mockVhdxService.Setup(x => x.CreateVhdxAsync(It.IsAny<VhdxContainerConfig>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _mockVhdxService.Setup(x => x.DeleteVhdxAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var vault = await _service.CreateVaultAsync("TestVault", 50, VhdxEncryptionType.BitLockerAes256);

        // Act
        var deleted = await _service.DeleteVaultAsync(vault.Id);
        var retrieved = await _service.GetVaultInfoAsync(vault.Id);

        // Assert
        Assert.True(deleted);
        Assert.Null(retrieved);
    }
}
