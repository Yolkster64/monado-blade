namespace MonadoBlade.Security.Services;

using System.Diagnostics;
using System.Management;
using Microsoft.Extensions.Logging;
using MonadoBlade.Security.Exceptions;
using MonadoBlade.Security.Interfaces;
using MonadoBlade.Security.Models;

/// <summary>
/// Implementation of BitLocker encryption management
/// </summary>
public class BitLockerService : IBitLockerService
{
    private readonly ILogger<BitLockerService> _logger;

    public BitLockerService(ILogger<BitLockerService> logger)
    {
        _logger = logger;
    }

    public async Task<bool> EnableBitLockerAsync(string volumePath, VhdxEncryptionType encryptionType, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Enabling BitLocker on volume {Volume} with encryption type {Type}", volumePath, encryptionType);
            
            string algorithm = encryptionType switch
            {
                VhdxEncryptionType.BitLockerAes256 => "AES 256",
                VhdxEncryptionType.BitLockerXts => "XTS-AES 128",
                _ => "AES 256"
            };

            string cmdCommand = $"manage-bde -on {volumePath} -encryptionMethod {algorithm}";
            
            bool success = await ExecuteCommandAsync(cmdCommand, cancellationToken);

            if (success)
            {
                _logger.LogInformation("Successfully enabled BitLocker on {Volume}", volumePath);
            }

            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error enabling BitLocker on {Volume}", volumePath);
            throw new BitLockerEncryptionException($"Failed to enable BitLocker on {volumePath}", ex);
        }
    }

    public async Task<bool> DisableBitLockerAsync(string volumePath, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Disabling BitLocker on volume {Volume}", volumePath);
            
            string cmdCommand = $"manage-bde -off {volumePath}";
            
            bool success = await ExecuteCommandAsync(cmdCommand, cancellationToken);

            if (success)
            {
                _logger.LogInformation("Successfully disabled BitLocker on {Volume}", volumePath);
            }

            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disabling BitLocker on {Volume}", volumePath);
            throw new BitLockerException($"Failed to disable BitLocker on {volumePath}", ex);
        }
    }

    public async Task<BitLockerStatus?> GetStatusAsync(string volumePath, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting BitLocker status for volume {Volume}", volumePath);
            
            string cmdCommand = $"manage-bde -status {volumePath}";
            string output = await ExecuteCommandWithOutputAsync(cmdCommand, cancellationToken);

            if (string.IsNullOrEmpty(output))
                return null;

            var status = new BitLockerStatus
            {
                VolumeId = volumePath,
                LastUpdated = DateTime.UtcNow,
                EncryptionState = ParseEncryptionState(output),
                ConversionStatus = ParseConversionStatus(output),
                PercentageEncrypted = ParsePercentageEncrypted(output),
                ProtectionStatus = ExtractValue(output, "Protection Status:"),
                KeyProtectors = ParseKeyProtectors(output)
            };

            return status;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting BitLocker status for {Volume}", volumePath);
            return null;
        }
    }

    public async Task<bool> SuspendAsync(string volumePath, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Suspending BitLocker on volume {Volume}", volumePath);
            
            string cmdCommand = $"manage-bde -status {volumePath} /serviceactions pause";
            
            bool success = await ExecuteCommandAsync(cmdCommand, cancellationToken);

            if (success)
            {
                _logger.LogInformation("Successfully suspended BitLocker on {Volume}", volumePath);
            }

            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error suspending BitLocker on {Volume}", volumePath);
            throw new BitLockerException($"Failed to suspend BitLocker on {volumePath}", ex);
        }
    }

    public async Task<bool> ResumeAsync(string volumePath, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Resuming BitLocker on volume {Volume}", volumePath);
            
            string cmdCommand = $"manage-bde -status {volumePath} /serviceactions resume";
            
            bool success = await ExecuteCommandAsync(cmdCommand, cancellationToken);

            if (success)
            {
                _logger.LogInformation("Successfully resumed BitLocker on {Volume}", volumePath);
            }

            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resuming BitLocker on {Volume}", volumePath);
            throw new BitLockerException($"Failed to resume BitLocker on {volumePath}", ex);
        }
    }

    public async Task<string?> GetRecoveryKeyAsync(string volumePath, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting BitLocker recovery key for volume {Volume}", volumePath);
            
            string cmdCommand = $"manage-bde -protectors -get {volumePath}";
            string output = await ExecuteCommandWithOutputAsync(cmdCommand, cancellationToken);

            return ExtractRecoveryKey(output);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting BitLocker recovery key for {Volume}", volumePath);
            return null;
        }
    }

    public async Task<IEnumerable<BitLockerStatus>> GetAllProtectedVolumesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting all BitLocker-protected volumes");
            
            var volumes = new List<BitLockerStatus>();

            foreach (var drive in DriveInfo.GetDrives())
            {
                if (drive.DriveType == DriveType.Fixed && drive.IsReady)
                {
                    string volumePath = drive.Name.TrimEnd('\\');
                    var status = await GetStatusAsync(volumePath, cancellationToken);
                    
                    if (status != null && status.EncryptionState == BitLockerEncryptionState.Encrypted)
                    {
                        volumes.Add(status);
                    }
                }
            }

            return volumes;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting protected volumes");
            return Enumerable.Empty<BitLockerStatus>();
        }
    }

    public async Task<bool> AddTpmProtectorAsync(string volumePath, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Adding TPM protector to BitLocker volume {Volume}", volumePath);
            
            string cmdCommand = $"manage-bde -protectors -adm -tpm {volumePath}";
            
            bool success = await ExecuteCommandAsync(cmdCommand, cancellationToken);

            if (success)
            {
                _logger.LogInformation("Successfully added TPM protector to {Volume}", volumePath);
            }

            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding TPM protector to {Volume}", volumePath);
            throw new BitLockerException($"Failed to add TPM protector to {volumePath}", ex);
        }
    }

    private BitLockerEncryptionState ParseEncryptionState(string output)
    {
        if (output.Contains("Encryption Method", StringComparison.OrdinalIgnoreCase))
            return BitLockerEncryptionState.Encrypted;
        if (output.Contains("Encryption in progress", StringComparison.OrdinalIgnoreCase))
            return BitLockerEncryptionState.Encrypting;
        if (output.Contains("Decryption in progress", StringComparison.OrdinalIgnoreCase))
            return BitLockerEncryptionState.Decrypting;

        return BitLockerEncryptionState.Decrypted;
    }

    private BitLockerConversionStatus ParseConversionStatus(string output)
    {
        if (output.Contains("Fully encrypted", StringComparison.OrdinalIgnoreCase))
            return BitLockerConversionStatus.FullyEncrypted;
        if (output.Contains("Fully decrypted", StringComparison.OrdinalIgnoreCase))
            return BitLockerConversionStatus.FullyDecrypted;

        return BitLockerConversionStatus.Unknown;
    }

    private double ParsePercentageEncrypted(string output)
    {
        var match = System.Text.RegularExpressions.Regex.Match(output, @"(\d+(?:\.\d+)?)%");
        if (match.Success && double.TryParse(match.Groups[1].Value, out var percentage))
        {
            return percentage;
        }

        return 0.0;
    }

    private List<string> ParseKeyProtectors(string output)
    {
        var protectors = new List<string>();
        var lines = output.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);

        foreach (var line in lines)
        {
            if (line.Contains("Protector", StringComparison.OrdinalIgnoreCase))
            {
                protectors.Add(line.Trim());
            }
        }

        return protectors;
    }

    private string? ExtractRecoveryKey(string output)
    {
        var lines = output.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
        
        foreach (var line in lines)
        {
            if (line.Contains("Recovery Key", StringComparison.OrdinalIgnoreCase) || 
                line.Contains("Numerical Password", StringComparison.OrdinalIgnoreCase))
            {
                return line.Trim();
            }
        }

        return null;
    }

    private string? ExtractValue(string output, string key)
    {
        var lines = output.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
        
        foreach (var line in lines)
        {
            if (line.StartsWith(key, StringComparison.OrdinalIgnoreCase))
            {
                return line.Substring(key.Length).Trim();
            }
        }

        return null;
    }

    private async Task<bool> ExecuteCommandAsync(string command, CancellationToken cancellationToken)
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/c {command}",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            };

            using (var process = Process.Start(startInfo))
            {
                if (process == null)
                    return false;

                await process.WaitForExitAsync(cancellationToken);
                return process.ExitCode == 0;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing command: {Command}", command);
            return false;
        }
    }

    private async Task<string> ExecuteCommandWithOutputAsync(string command, CancellationToken cancellationToken)
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/c {command}",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            };

            using (var process = Process.Start(startInfo))
            {
                if (process == null)
                    return string.Empty;

                string output = await process.StandardOutput.ReadToEndAsync(cancellationToken);
                await process.WaitForExitAsync(cancellationToken);

                return output;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing command with output: {Command}", command);
            return string.Empty;
        }
    }
}
