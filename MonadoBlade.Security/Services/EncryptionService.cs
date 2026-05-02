namespace MonadoBlade.Security.Services;

using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using MonadoBlade.Security.Exceptions;
using MonadoBlade.Security.Interfaces;
using MonadoBlade.Security.Models;

/// <summary>
/// Implementation of encryption and cryptographic operations
/// </summary>
public class EncryptionService : IEncryptionService
{
    private readonly ILogger<EncryptionService> _logger;
    private readonly Dictionary<string, EncryptionKey> _keys = new();

    public EncryptionService(ILogger<EncryptionService> logger)
    {
        _logger = logger;
    }

    public async Task<byte[]> EncryptAes256Async(byte[] data, string keyId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Encrypting data with AES-256 using key {KeyId}", keyId);
            
            if (!_keys.TryGetValue(keyId, out var key))
            {
                throw new EncryptionException($"Key not found: {keyId}");
            }

            using (var aes = Aes.Create())
            {
                aes.KeySize = 256;
                aes.Key = key.KeyMaterial;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                using (var encryptor = aes.CreateEncryptor(aes.Key, aes.IV))
                {
                    using (var ms = new MemoryStream())
                    {
                        ms.Write(aes.IV, 0, aes.IV.Length);

                        using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                        {
                            await cs.WriteAsync(data, 0, data.Length, cancellationToken);
                            cs.FlushFinalBlock();
                        }

                        return ms.ToArray();
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error encrypting data");
            throw new EncryptionException("Failed to encrypt data", ex);
        }
    }

    public async Task<byte[]> DecryptAes256Async(byte[] encryptedData, string keyId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Decrypting data with AES-256 using key {KeyId}", keyId);
            
            if (!_keys.TryGetValue(keyId, out var key))
            {
                throw new EncryptionException($"Key not found: {keyId}");
            }

            using (var aes = Aes.Create())
            {
                aes.KeySize = 256;
                aes.Key = key.KeyMaterial;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                using (var ms = new MemoryStream(encryptedData))
                {
                    byte[] iv = new byte[aes.IV.Length];
                    await ms.ReadExactlyAsync(iv, 0, iv.Length, cancellationToken);
                    aes.IV = iv;

                    using (var decryptor = aes.CreateDecryptor(aes.Key, aes.IV))
                    {
                        using (var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                        {
                            using (var resultMs = new MemoryStream())
                            {
                                await cs.CopyToAsync(resultMs, 81920, cancellationToken);
                                return resultMs.ToArray();
                            }
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error decrypting data");
            throw new EncryptionException("Failed to decrypt data", ex);
        }
    }

    public async Task<EncryptionKey> GenerateKeyAsync(string keyType, int keySize = 256, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Generating encryption key of type {Type} with size {Size}", keyType, keySize);
            
            byte[] keyMaterial = new byte[keySize / 8];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(keyMaterial);
            }

            var key = new EncryptionKey
            {
                Id = Guid.NewGuid().ToString(),
                KeyType = keyType,
                KeyMaterial = keyMaterial,
                KeySize = keySize,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            _keys[key.Id] = key;
            
            _logger.LogInformation("Successfully generated key {KeyId}", key.Id);
            return await Task.FromResult(key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating encryption key");
            throw new EncryptionException("Failed to generate encryption key", ex);
        }
    }

    public async Task<EncryptionKey?> GetKeyAsync(string keyId, CancellationToken cancellationToken = default)
    {
        try
        {
            if (_keys.TryGetValue(keyId, out var key))
            {
                return await Task.FromResult(key);
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting encryption key {KeyId}", keyId);
            return null;
        }
    }

    public async Task<bool> DeleteKeyAsync(string keyId, CancellationToken cancellationToken = default)
    {
        try
        {
            if (_keys.Remove(keyId))
            {
                _logger.LogInformation("Successfully deleted key {KeyId}", keyId);
                return await Task.FromResult(true);
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting encryption key {KeyId}", keyId);
            return false;
        }
    }

    public async Task<string> ComputeSha256Async(byte[] data, CancellationToken cancellationToken = default)
    {
        try
        {
            using (var sha256 = SHA256.Create())
            {
                byte[] hash = sha256.ComputeHash(data);
                return await Task.FromResult(Convert.ToHexString(hash));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error computing SHA256 hash");
            throw new EncryptionException("Failed to compute SHA256 hash", ex);
        }
    }

    public async Task<string> ComputeFileHashAsync(string filePath, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Computing SHA256 hash for file {FilePath}", filePath);
            
            using (var sha256 = SHA256.Create())
            {
                using (var fileStream = File.OpenRead(filePath))
                {
                    byte[] hash = await Task.Run(() => sha256.ComputeHash(fileStream), cancellationToken);
                    return Convert.ToHexString(hash);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error computing file hash for {FilePath}", filePath);
            throw new EncryptionException($"Failed to compute hash for {filePath}", ex);
        }
    }

    public async Task<IEnumerable<EncryptionKey>> ListKeysAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await Task.FromResult(_keys.Values.AsEnumerable());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing encryption keys");
            return Enumerable.Empty<EncryptionKey>();
        }
    }
}
