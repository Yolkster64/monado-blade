namespace MonadoBlade.Security.Interfaces;

using MonadoBlade.Security.Models;

/// <summary>
/// Interface for quarantine and threat containment operations
/// </summary>
public interface IQuarantineService
{
    /// <summary>
    /// Quarantines a file for threat containment
    /// </summary>
    Task<QuarantineEntry> QuarantineFileAsync(string filePath, string threatLevel, string reason, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets information about a quarantined item
    /// </summary>
    Task<QuarantineEntry?> GetQuarantineEntryAsync(string entryId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists all quarantined items
    /// </summary>
    Task<IEnumerable<QuarantineEntry>> ListQuarantineAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a file from quarantine
    /// </summary>
    Task<bool> RestoreFromQuarantineAsync(string entryId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a quarantined file permanently
    /// </summary>
    Task<bool> DeleteFromQuarantineAsync(string entryId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Analyzes a quarantined file (simulated threat analysis)
    /// </summary>
    Task<string> AnalyzeQuarantineEntryAsync(string entryId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a file should be quarantined based on threat signature
    /// </summary>
    Task<bool> ShouldQuarantineAsync(string filePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets quarantine storage usage
    /// </summary>
    Task<long> GetQuarantineStorageSizeAsync(CancellationToken cancellationToken = default);
}
