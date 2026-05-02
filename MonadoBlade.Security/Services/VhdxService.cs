namespace MonadoBlade.Security.Services;

using System.Diagnostics;
using System.Management;
using Microsoft.Extensions.Logging;
using MonadoBlade.Security.Exceptions;
using MonadoBlade.Security.Interfaces;
using MonadoBlade.Security.Models;

/// <summary>
/// Implementation of VHDX (Virtual Hard Disk) management operations
/// </summary>
public class VhdxService : IVhdxService
{
    private readonly ILogger<VhdxService> _logger;
    private readonly string _containerBasePath;

    public VhdxService(ILogger<VhdxService> logger, string containerBasePath = @"D:\Monado\Containers")
    {
        _logger = logger;
        _containerBasePath = containerBasePath;
        
        if (!Directory.Exists(_containerBasePath))
        {
            Directory.CreateDirectory(_containerBasePath);
        }
    }

    public async Task<bool> CreateVhdxAsync(VhdxContainerConfig config, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Creating VHDX container: {Name}", config.Name);
            
            string vhdxPath = Path.Combine(_containerBasePath, $"{config.Name}.vhdx");
            
            // Use DiskPart to create VHDX
            string diskPartScript = GenerateDiskPartCreateScript(vhdxPath, config);
            bool success = await ExecuteDiskPartScriptAsync(diskPartScript, cancellationToken);
            
            if (!success)
            {
                throw new VhdxCreationException($"Failed to create VHDX container: {config.Name}");
            }

            _logger.LogInformation("Successfully created VHDX container at {Path}", vhdxPath);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating VHDX container: {Name}", config.Name);
            throw new VhdxCreationException($"Failed to create VHDX: {config.Name}", ex);
        }
    }

    public async Task<bool> MountVhdxAsync(string vhdxPath, char driveLetter, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!File.Exists(vhdxPath))
            {
                throw new FileNotFoundException($"VHDX file not found: {vhdxPath}");
            }

            _logger.LogInformation("Mounting VHDX from {Path} to drive {DriveLetter}:", vhdxPath, driveLetter);
            
            string diskPartScript = $@"
select vdisk file=""{vhdxPath}""
attach vdisk
create partition primary
format fs=NTFS quick
assign letter={driveLetter}
";
            
            var startInfo = new ProcessStartInfo
            {
                FileName = "diskpart.exe",
                UseShellExecute = false,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            };

            using (var process = Process.Start(startInfo))
            {
                if (process == null)
                    throw new VhdxMountException("Failed to start diskpart.exe");

                await process.StandardInput.WriteLineAsync(diskPartScript);
                await process.StandardInput.WriteLineAsync("exit");
                
                await process.WaitForExitAsync(cancellationToken);

                if (process.ExitCode != 0)
                {
                    throw new VhdxMountException($"Diskpart failed with exit code {process.ExitCode}");
                }
            }

            _logger.LogInformation("Successfully mounted VHDX to drive {DriveLetter}:", driveLetter);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error mounting VHDX: {Path}", vhdxPath);
            throw new VhdxMountException($"Failed to mount VHDX: {vhdxPath}", ex);
        }
    }

    public async Task<bool> UnmountVhdxAsync(char driveLetter, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Unmounting VHDX from drive {DriveLetter}:", driveLetter);
            
            string diskPartScript = $@"
select volume {driveLetter}
remove letter={driveLetter}
detach vdisk
";
            
            var startInfo = new ProcessStartInfo
            {
                FileName = "diskpart.exe",
                UseShellExecute = false,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            };

            using (var process = Process.Start(startInfo))
            {
                if (process == null)
                    throw new VhdxUnmountException("Failed to start diskpart.exe");

                await process.StandardInput.WriteLineAsync(diskPartScript);
                await process.StandardInput.WriteLineAsync("exit");
                
                await process.WaitForExitAsync(cancellationToken);

                if (process.ExitCode != 0)
                {
                    _logger.LogWarning("Diskpart exited with code {ExitCode}", process.ExitCode);
                }
            }

            _logger.LogInformation("Successfully unmounted VHDX from drive {DriveLetter}:", driveLetter);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unmounting VHDX from drive {DriveLetter}:", driveLetter);
            throw new VhdxUnmountException($"Failed to unmount VHDX from drive {driveLetter}", ex);
        }
    }

    public async Task<VhdxMountStatus?> GetMountStatusAsync(char driveLetter, CancellationToken cancellationToken = default)
    {
        try
        {
            string drivePath = $"{driveLetter}:\\";
            var driveInfo = new DriveInfo(drivePath);

            if (!driveInfo.IsReady)
                return null;

            return await Task.FromResult(new VhdxMountStatus
            {
                VhdxPath = drivePath,
                DriveLetter = driveLetter,
                SizeBytes = driveInfo.TotalSize,
                AvailableSpaceBytes = driveInfo.AvailableFreeSpace,
                MountedAt = DateTime.UtcNow,
                IsReadOnly = false,
                FileSystem = driveInfo.DriveFormat
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting mount status for drive {DriveLetter}:", driveLetter);
            return null;
        }
    }

    public async Task<IEnumerable<VhdxMountStatus>> GetMountedVhdxListAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var mounts = new List<VhdxMountStatus>();
            
            foreach (var drive in DriveInfo.GetDrives())
            {
                if (drive.DriveType == DriveType.Removable && drive.IsReady)
                {
                    mounts.Add(new VhdxMountStatus
                    {
                        VhdxPath = drive.Name,
                        DriveLetter = drive.Name[0],
                        SizeBytes = drive.TotalSize,
                        AvailableSpaceBytes = drive.AvailableFreeSpace,
                        MountedAt = DateTime.UtcNow,
                        IsReadOnly = false,
                        FileSystem = drive.DriveFormat
                    });
                }
            }

            return await Task.FromResult(mounts);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting mounted VHDX list");
            return Enumerable.Empty<VhdxMountStatus>();
        }
    }

    public async Task<bool> DeleteVhdxAsync(string vhdxPath, CancellationToken cancellationToken = default)
    {
        try
        {
            if (File.Exists(vhdxPath))
            {
                File.Delete(vhdxPath);
                _logger.LogInformation("Deleted VHDX file: {Path}", vhdxPath);
                return await Task.FromResult(true);
            }
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting VHDX: {Path}", vhdxPath);
            throw new VhdxOperationException($"Failed to delete VHDX: {vhdxPath}", ex);
        }
    }

    public async Task<bool> CompactVhdxAsync(string vhdxPath, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Compacting VHDX: {Path}", vhdxPath);
            
            string diskPartScript = $@"
select vdisk file=""{vhdxPath}""
compact vdisk
";
            
            bool success = await ExecuteDiskPartScriptAsync(diskPartScript, cancellationToken);
            
            if (success)
            {
                _logger.LogInformation("Successfully compacted VHDX: {Path}", vhdxPath);
            }

            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error compacting VHDX: {Path}", vhdxPath);
            throw new VhdxOperationException($"Failed to compact VHDX: {vhdxPath}", ex);
        }
    }

    public async Task<bool> ResizeVhdxAsync(string vhdxPath, long newSizeGb, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Resizing VHDX {Path} to {SizeGb}GB", vhdxPath, newSizeGb);
            
            long newSizeBytes = newSizeGb * 1024 * 1024 * 1024;
            
            string diskPartScript = $@"
select vdisk file=""{vhdxPath}""
expand vdisk maximum={newSizeBytes}
";
            
            bool success = await ExecuteDiskPartScriptAsync(diskPartScript, cancellationToken);
            
            if (success)
            {
                _logger.LogInformation("Successfully resized VHDX to {SizeGb}GB", newSizeGb);
            }

            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resizing VHDX: {Path}", vhdxPath);
            throw new VhdxOperationException($"Failed to resize VHDX: {vhdxPath}", ex);
        }
    }

    private string GenerateDiskPartCreateScript(string vhdxPath, VhdxContainerConfig config)
    {
        long sizeBytes = config.SizeGb * 1024 * 1024 * 1024;
        string type = config.IsFixed ? "fixed" : "expandable";
        
        return $@"
create vdisk file=""{vhdxPath}"" maximum={sizeBytes} type={type}
select vdisk file=""{vhdxPath}""
attach vdisk
create partition primary
format fs=NTFS quick
";
    }

    private async Task<bool> ExecuteDiskPartScriptAsync(string script, CancellationToken cancellationToken)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "diskpart.exe",
            UseShellExecute = false,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            CreateNoWindow = true
        };

        using (var process = Process.Start(startInfo))
        {
            if (process == null)
                return false;

            await process.StandardInput.WriteLineAsync(script);
            await process.StandardInput.WriteLineAsync("exit");
            
            await process.WaitForExitAsync(cancellationToken);

            return process.ExitCode == 0;
        }
    }
}
