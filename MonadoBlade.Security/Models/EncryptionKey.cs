namespace MonadoBlade.Security.Models;

/// <summary>
/// Represents an encryption key with metadata
/// </summary>
public class EncryptionKey
{
    public required string Id { get; set; }
    public required string KeyType { get; set; }
    public required byte[] KeyMaterial { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public bool IsActive { get; set; } = true;
    public Dictionary<string, object> Metadata { get; set; } = new();
    public int KeySize { get; set; }
}
