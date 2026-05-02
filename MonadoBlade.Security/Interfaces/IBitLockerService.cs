namespace MonadoBlade.Security.Interfaces;

using MonadoBlade.Security.Models;

/// <summary>
/// Interface for BitLocker encryption management
/// </summary>
public interface IBitLockerService
{
    /// <summary>
    /// Enables BitLocker encryption on a volume
    /// </summary>
    Task<bool> EnableBitLockerAsync(string volumePath, VhdxEncryptionType encryptionType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Disables BitLocker encryption on a volume
    /// </summary>
    Task<bool> DisableBitLockerAsync(string volumePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current BitLocker status of a volume
    /// </summary>
    Task<BitLockerStatus?> GetStatusAsync(string volumePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Suspends BitLocker encryption on a volume
    /// </summary>
    Task<bool> SuspendAsync(string volumePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resumes BitLocker encryption on a volume
    /// </summary>
    Task<bool> ResumeAsync(string volumePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the recovery key for a BitLocker-encrypted volume
    /// </summary>
    Task<string?> GetRecoveryKeyAsync(string volumePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists all BitLocker-protected volumes
    /// </summary>
    Task<IEnumerable<BitLockerStatus>> GetAllProtectedVolumesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a TPM-based key protector to a BitLocker volume
    /// </summary>
    Task<bool> AddTpmProtectorAsync(string volumePath, CancellationToken cancellationToken = default);
}
