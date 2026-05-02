namespace MonadoBlade.Security.Interfaces;

using MonadoBlade.Security.Models;

/// <summary>
/// Interface for TPM 2.0 (Trusted Platform Module) operations
/// </summary>
public interface ITpmService
{
    /// <summary>
    /// Gets the current status of the TPM module
    /// </summary>
    Task<TpmStatus?> GetStatusAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if TPM 2.0 is available and functional
    /// </summary>
    Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Initializes the TPM module
    /// </summary>
    Task<bool> InitializeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears TPM data (dangerous operation, typically for testing)
    /// </summary>
    Task<bool> ClearAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a TPM PCR (Platform Configuration Register) value
    /// </summary>
    Task<TpmPcr?> GetPcrAsync(int pcrIndex, string hashAlgorithm = "SHA256", CancellationToken cancellationToken = default);

    /// <summary>
    /// Seals data to TPM with specified PCR values
    /// </summary>
    Task<byte[]> SealDataAsync(byte[] data, int[] pcrIndices, CancellationToken cancellationToken = default);

    /// <summary>
    /// Unseals data from TPM
    /// </summary>
    Task<byte[]> UnsealDataAsync(byte[] sealedData, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the TPM firmware version
    /// </summary>
    Task<string?> GetFirmwareVersionAsync(CancellationToken cancellationToken = default);
}
