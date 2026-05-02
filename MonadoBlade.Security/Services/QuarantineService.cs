namespace MonadoBlade.Security.Services;

using Microsoft.Extensions.Logging;
using MonadoBlade.Security.Exceptions;
using MonadoBlade.Security.Interfaces;
using MonadoBlade.Security.Models;

/// <summary>
/// Implementation of quarantine and threat containment operations
/// </summary>
public class QuarantineService : IQuarantineService
{
    private readonly ILogger<QuarantineService> _logger;
    private readonly Dictionary<string, QuarantineEntry> _quarantineItems = new();
    private readonly string _quarantinePath;

    public QuarantineService(ILogger<QuarantineService> logger, string quarantinePath = @"D:\Monado\Containers\Quarantine")
    {
        _logger = logger;
        _quarantinePath = quarantinePath;
        
        if (!Directory.Exists(_quarantinePath))
        {
            Directory.CreateDirectory(_quarantinePath);
        }
    }

    public async Task<QuarantineEntry> QuarantineFileAsync(string filePath, string threatLevel, string reason, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Quarantining file {FilePath} with threat level {ThreatLevel}", filePath, threatLevel);
            
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"File not found: {filePath}");
            }

            string entryId = Guid.NewGuid().ToString();
            string fileName = Path.GetFileName(filePath);
            string quarantinedPath = Path.Combine(_quarantinePath, $"{entryId}_{fileName}");

            File.Copy(filePath, quarantinedPath, true);
            File.Delete(filePath);

            var fileInfo = new FileInfo(quarantinedPath);
            var entry = new QuarantineEntry
            {
                Id = entryId,
                FilePath = quarantinedPath,
                OriginalFileName = fileName,
                ThreatLevel = threatLevel,
                Reason = reason,
                QuarantinedAt = DateTime.UtcNow,
                FileSizeBytes = fileInfo.Length,
                Status = QuarantineStatus.Isolated,
                FileHash = await ComputeFileHashAsync(quarantinedPath, cancellationToken)
            };

            _quarantineItems[entryId] = entry;
            
            _logger.LogInformation("Successfully quarantined file {FilePath} as entry {EntryId}", filePath, entryId);
            return entry;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error quarantining file {FilePath}", filePath);
            throw new QuarantineException($"Failed to quarantine file: {filePath}", ex);
        }
    }

    public async Task<QuarantineEntry?> GetQuarantineEntryAsync(string entryId, CancellationToken cancellationToken = default)
    {
        try
        {
            if (_quarantineItems.TryGetValue(entryId, out var entry))
            {
                return await Task.FromResult(entry);
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting quarantine entry {EntryId}", entryId);
            return null;
        }
    }

    public async Task<IEnumerable<QuarantineEntry>> ListQuarantineAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await Task.FromResult(_quarantineItems.Values.AsEnumerable());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing quarantine items");
            return Enumerable.Empty<QuarantineEntry>();
        }
    }

    public async Task<bool> RestoreFromQuarantineAsync(string entryId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Restoring file from quarantine {EntryId}", entryId);
            
            if (!_quarantineItems.TryGetValue(entryId, out var entry))
            {
                return false;
            }

            string restorePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), entry.OriginalFileName);
            
            if (File.Exists(entry.FilePath))
            {
                File.Copy(entry.FilePath, restorePath, true);
            }

            entry.Status = QuarantineStatus.Restored;
            
            _logger.LogInformation("Successfully restored file to {RestorePath}", restorePath);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error restoring file from quarantine {EntryId}", entryId);
            throw new QuarantineException($"Failed to restore from quarantine: {entryId}", ex);
        }
    }

    public async Task<bool> DeleteFromQuarantineAsync(string entryId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Deleting quarantined file {EntryId}", entryId);
            
            if (!_quarantineItems.TryGetValue(entryId, out var entry))
            {
                return false;
            }

            if (File.Exists(entry.FilePath))
            {
                File.Delete(entry.FilePath);
            }

            entry.Status = QuarantineStatus.Deleted;
            
            _logger.LogInformation("Successfully deleted quarantined file {EntryId}", entryId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting quarantined file {EntryId}", entryId);
            throw new QuarantineException($"Failed to delete quarantined file: {entryId}", ex);
        }
    }

    public async Task<string> AnalyzeQuarantineEntryAsync(string entryId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Analyzing quarantined entry {EntryId}", entryId);
            
            if (!_quarantineItems.TryGetValue(entryId, out var entry))
            {
                return "UNKNOWN";
            }

            entry.Status = QuarantineStatus.Analyzed;
            
            // Simulated threat analysis
            return entry.ThreatLevel switch
            {
                "CRITICAL" => "MALWARE",
                "HIGH" => "SUSPICIOUS",
                "MEDIUM" => "PUP",
                "LOW" => "SAFE",
                _ => "UNKNOWN"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing quarantine entry {EntryId}", entryId);
            throw new QuarantineException($"Failed to analyze quarantine entry: {entryId}", ex);
        }
    }

    public async Task<bool> ShouldQuarantineAsync(string filePath, CancellationToken cancellationToken = default)
    {
        try
        {
            string fileName = Path.GetFileName(filePath);
            string extension = Path.GetExtension(filePath).ToLowerInvariant();

            // Simulated threat detection
            var dangerousExtensions = new[] { ".exe", ".bat", ".cmd", ".scr", ".vbs", ".ps1" };
            
            return await Task.FromResult(dangerousExtensions.Contains(extension));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if file should be quarantined {FilePath}", filePath);
            return false;
        }
    }

    public async Task<long> GetQuarantineStorageSizeAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            long totalSize = 0;

            if (Directory.Exists(_quarantinePath))
            {
                var dir = new DirectoryInfo(_quarantinePath);
                totalSize = dir.EnumerateFiles("*", SearchOption.AllDirectories)
                    .Sum(f => f.Length);
            }

            return await Task.FromResult(totalSize);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting quarantine storage size");
            return 0;
        }
    }

    private async Task<string> ComputeFileHashAsync(string filePath, CancellationToken cancellationToken)
    {
        try
        {
            using (var sha256 = System.Security.Cryptography.SHA256.Create())
            {
                using (var fileStream = File.OpenRead(filePath))
                {
                    byte[] hash = await Task.Run(() => sha256.ComputeHash(fileStream), cancellationToken);
                    return Convert.ToHexString(hash);
                }
            }
        }
        catch
        {
            return "UNKNOWN";
        }
    }
}
