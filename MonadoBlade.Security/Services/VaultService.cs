namespace MonadoBlade.Security.Services;

using Microsoft.Extensions.Logging;
using MonadoBlade.Security.Exceptions;
using MonadoBlade.Security.Interfaces;
using MonadoBlade.Security.Models;

/// <summary>
/// Implementation of encrypted vault container management
/// </summary>
public class VaultService : IVaultService
{
    private readonly ILogger<VaultService> _logger;
    private readonly IVhdxService _vhdxService;
    private readonly IBitLockerService _bitLockerService;
    private readonly Dictionary<string, VaultContainer> _vaults = new();
    private readonly string _vaultsBasePath;

    public VaultService(ILogger<VaultService> logger, IVhdxService vhdxService, IBitLockerService bitLockerService)
    {
        _logger = logger;
        _vhdxService = vhdxService;
        _bitLockerService = bitLockerService;
        _vaultsBasePath = @"D:\Monado\Containers\Vaults";
        
        if (!Directory.Exists(_vaultsBasePath))
        {
            Directory.CreateDirectory(_vaultsBasePath);
        }
    }

    public async Task<VaultContainer> CreateVaultAsync(string name, long sizeGb, VhdxEncryptionType encryptionType, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Creating vault: {Name} with size {SizeGb}GB and encryption {EncryptionType}", name, sizeGb, encryptionType);
            
            string vaultId = Guid.NewGuid().ToString();
            string vhdxPath = Path.Combine(_vaultsBasePath, $"{name}_{vaultId}.vhdx");
            
            var config = new VhdxContainerConfig
            {
                Name = $"{name}_{vaultId}",
                Path = vhdxPath,
                DriveLetter = 'V',
                SizeGb = sizeGb,
                IsFixed = false,
                EncryptionType = encryptionType,
                RequireTpm = encryptionType == VhdxEncryptionType.Tpm20Sealed,
                MountMode = VhdxMountMode.ReadWrite
            };

            bool created = await _vhdxService.CreateVhdxAsync(config, cancellationToken);
            
            if (!created)
            {
                throw new VaultException("Failed to create VHDX container");
            }

            var vault = new VaultContainer
            {
                Id = vaultId,
                Name = name,
                VhdxPath = vhdxPath,
                MountDrive = 'V',
                State = VaultContainerState.Unmounted,
                EncryptionType = encryptionType,
                SizeBytes = sizeGb * 1024 * 1024 * 1024,
                UsedSpaceBytes = 0,
                CreatedAt = DateTime.UtcNow,
                LastMountedAt = DateTime.UtcNow,
                IsMounted = false
            };

            _vaults[vaultId] = vault;
            
            _logger.LogInformation("Successfully created vault {VaultId}: {Name}", vaultId, name);
            return vault;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating vault: {Name}", name);
            throw new VaultException($"Failed to create vault: {name}", ex);
        }
    }

    public async Task<VaultContainer> OpenVaultAsync(string vaultId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Opening vault {VaultId}", vaultId);
            
            if (!_vaults.TryGetValue(vaultId, out var vault))
            {
                throw new VaultException($"Vault not found: {vaultId}");
            }

            if (vault.IsMounted)
            {
                _logger.LogWarning("Vault {VaultId} is already mounted", vaultId);
                return vault;
            }

            vault.State = VaultContainerState.Mounting;
            
            bool mounted = await _vhdxService.MountVhdxAsync(vault.VhdxPath, vault.MountDrive, cancellationToken);
            
            if (!mounted)
            {
                vault.State = VaultContainerState.Unmounted;
                throw new VaultException($"Failed to mount vault VHDX");
            }

            if (vault.EncryptionType != VhdxEncryptionType.None)
            {
                bool bitLockerEnabled = await _bitLockerService.EnableBitLockerAsync(
                    $"{vault.MountDrive}:\\",
                    vault.EncryptionType,
                    cancellationToken);
                
                if (!bitLockerEnabled)
                {
                    _logger.LogWarning("Failed to enable BitLocker on vault {VaultId}", vaultId);
                }
            }

            vault.IsMounted = true;
            vault.State = VaultContainerState.Mounted;
            vault.LastMountedAt = DateTime.UtcNow;
            
            _logger.LogInformation("Successfully opened vault {VaultId}", vaultId);
            return vault;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error opening vault {VaultId}", vaultId);
            throw new VaultException($"Failed to open vault: {vaultId}", ex);
        }
    }

    public async Task<bool> CloseVaultAsync(string vaultId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Closing vault {VaultId}", vaultId);
            
            if (!_vaults.TryGetValue(vaultId, out var vault))
            {
                return false;
            }

            vault.State = VaultContainerState.Unmounting;
            
            bool unmounted = await _vhdxService.UnmountVhdxAsync(vault.MountDrive, cancellationToken);
            
            vault.IsMounted = false;
            vault.State = VaultContainerState.Unmounted;
            
            _logger.LogInformation("Successfully closed vault {VaultId}", vaultId);
            return unmounted;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error closing vault {VaultId}", vaultId);
            throw new VaultException($"Failed to close vault: {vaultId}", ex);
        }
    }

    public async Task<VaultContainer?> GetVaultInfoAsync(string vaultId, CancellationToken cancellationToken = default)
    {
        try
        {
            if (_vaults.TryGetValue(vaultId, out var vault))
            {
                return await Task.FromResult(vault);
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting vault info for {VaultId}", vaultId);
            return null;
        }
    }

    public async Task<IEnumerable<VaultContainer>> ListVaultsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await Task.FromResult(_vaults.Values.AsEnumerable());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing vaults");
            return Enumerable.Empty<VaultContainer>();
        }
    }

    public async Task<bool> DeleteVaultAsync(string vaultId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Deleting vault {VaultId}", vaultId);
            
            if (!_vaults.TryGetValue(vaultId, out var vault))
            {
                return false;
            }

            if (vault.IsMounted)
            {
                await CloseVaultAsync(vaultId, cancellationToken);
            }

            bool deleted = await _vhdxService.DeleteVhdxAsync(vault.VhdxPath, cancellationToken);
            
            if (deleted)
            {
                _vaults.Remove(vaultId);
                _logger.LogInformation("Successfully deleted vault {VaultId}", vaultId);
            }

            return deleted;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting vault {VaultId}", vaultId);
            throw new VaultException($"Failed to delete vault: {vaultId}", ex);
        }
    }

    public async Task<bool> VerifyVaultIntegrityAsync(string vaultId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Verifying vault integrity for {VaultId}", vaultId);
            
            if (!_vaults.TryGetValue(vaultId, out var vault))
            {
                return false;
            }

            if (!File.Exists(vault.VhdxPath))
            {
                vault.State = VaultContainerState.Corrupted;
                return false;
            }

            var fileInfo = new FileInfo(vault.VhdxPath);
            if (fileInfo.Length == 0)
            {
                vault.State = VaultContainerState.Corrupted;
                return false;
            }

            _logger.LogInformation("Vault {VaultId} integrity verified", vaultId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying vault integrity for {VaultId}", vaultId);
            return false;
        }
    }

    public async Task<bool> LockVaultAsync(string vaultId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Locking vault {VaultId}", vaultId);
            
            if (!_vaults.TryGetValue(vaultId, out var vault))
            {
                return false;
            }

            vault.State = VaultContainerState.Locked;
            
            if (vault.IsMounted)
            {
                await CloseVaultAsync(vaultId, cancellationToken);
            }

            _logger.LogInformation("Successfully locked vault {VaultId}", vaultId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error locking vault {VaultId}", vaultId);
            return false;
        }
    }

    public async Task<long> GetAvailableSpaceAsync(string vaultId, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!_vaults.TryGetValue(vaultId, out var vault))
            {
                return 0;
            }

            if (!vault.IsMounted)
            {
                return 0;
            }

            var driveInfo = new DriveInfo($"{vault.MountDrive}:\\");
            return await Task.FromResult(driveInfo.AvailableFreeSpace);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting available space for vault {VaultId}", vaultId);
            return 0;
        }
    }
}
