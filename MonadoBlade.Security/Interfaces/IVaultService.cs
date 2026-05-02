namespace MonadoBlade.Security.Interfaces;

using MonadoBlade.Security.Models;

/// <summary>
/// Interface for encrypted vault container management
/// </summary>
public interface IVaultService
{
    /// <summary>
    /// Creates a new vault container
    /// </summary>
    Task<VaultContainer> CreateVaultAsync(string name, long sizeGb, VhdxEncryptionType encryptionType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Opens and mounts an existing vault container
    /// </summary>
    Task<VaultContainer> OpenVaultAsync(string vaultId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Closes and unmounts a vault container
    /// </summary>
    Task<bool> CloseVaultAsync(string vaultId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets information about a vault container
    /// </summary>
    Task<VaultContainer?> GetVaultInfoAsync(string vaultId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists all available vault containers
    /// </summary>
    Task<IEnumerable<VaultContainer>> ListVaultsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a vault container
    /// </summary>
    Task<bool> DeleteVaultAsync(string vaultId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifies vault integrity
    /// </summary>
    Task<bool> VerifyVaultIntegrityAsync(string vaultId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Locks a vault container (unmounts and secures it)
    /// </summary>
    Task<bool> LockVaultAsync(string vaultId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets available space in a vault container
    /// </summary>
    Task<long> GetAvailableSpaceAsync(string vaultId, CancellationToken cancellationToken = default);
}
