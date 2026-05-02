namespace MonadoBlade.Security.Interfaces;

using MonadoBlade.Security.Models;

/// <summary>
/// Interface for encryption and cryptographic operations
/// </summary>
public interface IEncryptionService
{
    /// <summary>
    /// Encrypts data using AES-256
    /// </summary>
    Task<byte[]> EncryptAes256Async(byte[] data, string keyId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Decrypts data encrypted with AES-256
    /// </summary>
    Task<byte[]> DecryptAes256Async(byte[] encryptedData, string keyId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a new encryption key
    /// </summary>
    Task<EncryptionKey> GenerateKeyAsync(string keyType, int keySize = 256, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets an encryption key by ID
    /// </summary>
    Task<EncryptionKey?> GetKeyAsync(string keyId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes an encryption key
    /// </summary>
    Task<bool> DeleteKeyAsync(string keyId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Computes a SHA-256 hash of data
    /// </summary>
    Task<string> ComputeSha256Async(byte[] data, CancellationToken cancellationToken = default);

    /// <summary>
    /// Computes a SHA-256 hash of a file
    /// </summary>
    Task<string> ComputeFileHashAsync(string filePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists all available encryption keys
    /// </summary>
    Task<IEnumerable<EncryptionKey>> ListKeysAsync(CancellationToken cancellationToken = default);
}
