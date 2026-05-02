using Moq;
using Microsoft.Extensions.Logging;
using MonadoBlade.Security.Interfaces;
using MonadoBlade.Security.Models;
using MonadoBlade.Security.Services;

namespace MonadoBlade.Security.Tests;

public class EncryptionServiceTests
{
    private readonly Mock<ILogger<EncryptionService>> _mockLogger;
    private readonly EncryptionService _service;

    public EncryptionServiceTests()
    {
        _mockLogger = new Mock<ILogger<EncryptionService>>();
        _service = new EncryptionService(_mockLogger.Object);
    }

    [Fact]
    public async Task GenerateKeyAsync_CreatesValidKey()
    {
        // Act
        var key = await _service.GenerateKeyAsync("AES-256", 256);

        // Assert
        Assert.NotNull(key);
        Assert.NotEmpty(key.Id);
        Assert.Equal("AES-256", key.KeyType);
        Assert.Equal(256, key.KeySize);
        Assert.True(key.IsActive);
        Assert.Equal(32, key.KeyMaterial.Length);
    }

    [Fact]
    public async Task EncryptAes256Async_EncryptsDataSuccessfully()
    {
        // Arrange
        var key = await _service.GenerateKeyAsync("AES-256", 256);
        byte[] plaintext = System.Text.Encoding.UTF8.GetBytes("Test data to encrypt");

        // Act
        var encrypted = await _service.EncryptAes256Async(plaintext, key.Id);

        // Assert
        Assert.NotNull(encrypted);
        Assert.NotEmpty(encrypted);
        Assert.NotEqual(plaintext, encrypted);
    }

    [Fact]
    public async Task DecryptAes256Async_DecryptsDataSuccessfully()
    {
        // Arrange
        var key = await _service.GenerateKeyAsync("AES-256", 256);
        byte[] plaintext = System.Text.Encoding.UTF8.GetBytes("Test data to encrypt and decrypt");
        var encrypted = await _service.EncryptAes256Async(plaintext, key.Id);

        // Act
        var decrypted = await _service.DecryptAes256Async(encrypted, key.Id);

        // Assert
        Assert.NotNull(decrypted);
        Assert.Equal(plaintext, decrypted);
    }

    [Fact]
    public async Task ComputeSha256Async_GeneratesCorrectHash()
    {
        // Arrange
        byte[] data = System.Text.Encoding.UTF8.GetBytes("Test data");

        // Act
        var hash = await _service.ComputeSha256Async(data);

        // Assert
        Assert.NotNull(hash);
        Assert.NotEmpty(hash);
        Assert.True(hash.Length > 0);
    }

    [Fact]
    public async Task GetKeyAsync_ReturnsExistingKey()
    {
        // Arrange
        var key = await _service.GenerateKeyAsync("AES-256", 256);

        // Act
        var retrieved = await _service.GetKeyAsync(key.Id);

        // Assert
        Assert.NotNull(retrieved);
        Assert.Equal(key.Id, retrieved.Id);
    }

    [Fact]
    public async Task GetKeyAsync_ReturnsNullForNonExistentKey()
    {
        // Act
        var retrieved = await _service.GetKeyAsync("non-existent-key");

        // Assert
        Assert.Null(retrieved);
    }

    [Fact]
    public async Task DeleteKeyAsync_RemovesKey()
    {
        // Arrange
        var key = await _service.GenerateKeyAsync("AES-256", 256);

        // Act
        var deleted = await _service.DeleteKeyAsync(key.Id);
        var retrieved = await _service.GetKeyAsync(key.Id);

        // Assert
        Assert.True(deleted);
        Assert.Null(retrieved);
    }

    [Fact]
    public async Task ListKeysAsync_ReturnsAllKeys()
    {
        // Arrange
        await _service.GenerateKeyAsync("AES-256", 256);
        await _service.GenerateKeyAsync("AES-256", 256);

        // Act
        var keys = await _service.ListKeysAsync();

        // Assert
        Assert.NotNull(keys);
        Assert.True(keys.Count() >= 2);
    }

    [Fact]
    public async Task EncryptAes256Async_ThrowsWhenKeyNotFound()
    {
        // Arrange
        byte[] data = System.Text.Encoding.UTF8.GetBytes("Test");

        // Act & Assert
        await Assert.ThrowsAsync<MonadoBlade.Security.Exceptions.EncryptionException>(
            () => _service.EncryptAes256Async(data, "non-existent-key"));
    }
}
